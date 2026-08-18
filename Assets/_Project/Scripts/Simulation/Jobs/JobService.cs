using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Simulation.Progression;
using UnityEngine;

namespace Nimbo.Simulation.Jobs
{
    /// <summary>
    /// El trabajo de la isla: quién tiene empleo, cuándo hace su turno y cuánto entra.
    /// </summary>
    /// <remarks>
    /// Los turnos se disparan por hora de juego, no por fotograma, así que una partida
    /// que estuvo cerrada cobra sus turnos igual que si se hubiera jugado. El día del
    /// último turno se guarda para que ponerse al día no pague dos veces lo mismo.
    /// </remarks>
    public sealed class JobService : IJobService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly ISimulationService _simulation;
        private readonly IIslandService _island;
        private readonly GameClock _clock;

        private readonly List<JobKind> _available = new List<JobKind>(8);

        public JobService(IIslanderRegistry registry, ISimulationService simulation,
                          IIslandService island, GameClock clock)
        {
            _registry = registry;
            _simulation = simulation;
            _island = island;
            _clock = clock;

            RefreshAvailable();

            EventBus.Subscribe<HourPassed>(OnHourPassed);
            EventBus.Subscribe<BuildingUnlocked>(OnBuildingUnlocked);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<HourPassed>(OnHourPassed);
            EventBus.Unsubscribe<BuildingUnlocked>(OnBuildingUnlocked);
        }

        public IReadOnlyList<JobKind> AvailableJobs => _available;

        private void OnBuildingUnlocked(BuildingUnlocked _) => RefreshAvailable();

        /// <summary>Un oficio existe cuando su zona está abierta, y no antes.</summary>
        private void RefreshAvailable()
        {
            _available.Clear();
            foreach (var entry in JobCatalog.All)
                if (_island.IsUnlocked(entry.ZoneId)) _available.Add(entry.Kind);
        }

        private void OnHourPassed(HourPassed evt)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                if (!islander.Job.HasJob) continue;
                if (islander.Job.LastShiftDay >= evt.Day) continue;

                if (!WeeklyRhythm.IsWorkday(WeeklyRhythm.DayOf(evt.Day))) continue;

                var entry = JobCatalog.Get(islander.Job.Kind);
                if (evt.Hour != entry.StartHour) continue;

                // Nadie va a trabajar hecho polvo: si llega al turno sin energía, ese
                // día lo pierde. Es lo que hace que descuidar a un habitante cueste
                // dinero de verdad y no solo una cara triste.
                if (islander.Needs.Band(NeedKind.Energy) == NeedBand.Critical) continue;

                WorkShift(islander.Id);
            }
        }

        public float AffinityFor(string islanderId, JobKind job) =>
            _registry.TryGet(islanderId, out var islander)
                ? JobCatalog.Affinity(islander.Personality, job)
                : 0f;

        public JobKind BestJobFor(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return JobKind.None;

            JobKind best = JobKind.None;
            float bestScore = float.MinValue;

            for (int i = 0; i < _available.Count; i++)
            {
                float score = JobCatalog.Affinity(islander.Personality, _available[i]);
                if (score <= bestScore) continue;
                bestScore = score;
                best = _available[i];
            }
            return best;
        }

        /// <summary>Lo que hace falta para que no le dé por negarse (§15.1).</summary>
        /// <remarks>
        /// Números bajos a propósito: esto no es un muro, es lo que hace que caerle bien
        /// a la gente sirva para algo. Con el reparto de afinidades que dan los dieciséis
        /// tipos, un vecino se niega a dos o tres oficios de los ocho, y solo mientras no
        /// seáis amigos.
        /// </remarks>
        private const float MinJobAffinity = 0.3f;

        public bool WouldAccept(string islanderId, JobKind job)
        {
            if (job == JobKind.None) return true;
            if (!_registry.TryGet(islanderId, out var islander)) return false;

            // Si el oficio le pega, lo coge sin más: nadie rechaza el trabajo de su vida
            // porque el alcalde le caiga regular.
            if (AffinityFor(islanderId, job) >= MinJobAffinity) return true;

            // Y si no le pega, lo hace por ti — si te lo has ganado.
            if (!ServiceRegistry.TryGet<ISocialService>(out var social)) return false;

            return social.PlayerRelationship(islanderId).Friendship >= FriendshipStage.Friend;
        }

        public bool Assign(string islanderId, JobKind job)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return false;
            if (job != JobKind.None && !_available.Contains(job)) return false;
            if (!WouldAccept(islanderId, job)) return false;

            Place(islander, job);
            return true;
        }

        /// <summary>Le pone el puesto sin preguntar. Solo para el reparto de salida.</summary>
        private static void Place(IslanderData islander, JobKind job)
        {

            // Cambiar de oficio cuesta la veteranía: el rango es de ese puesto, no
            // del habitante. Si no, el jugador rotaría a todos por el mejor pagado.
            islander.Job = new JobState
            {
                Kind = job,
                Rank = 1,
                ShiftsWorked = 0,
                LastShiftDay = 0,
                TotalEarned = islander.Job.TotalEarned,
            };
        }

        public void Quit(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            islander.Job = JobState.Unemployed;
        }

        public int DailyWage(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return 0;
            if (!islander.Job.HasJob) return 0;

            int wage = JobCatalog.Wage(islander.Job.Kind, islander.Job.Rank,
                                       JobCatalog.Affinity(islander.Personality, islander.Job.Kind));

            // El lunes paga más y el domingo no se trabaja: es lo que hace que un
            // martes y un sábado se sientan distintos y no un bucle de días iguales.
            return Mathf.RoundToInt(wage * WeeklyRhythm.WageMultiplier(WeeklyRhythm.Today(_clock)));
        }

        public int WorkShift(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return 0;
            if (!islander.Job.HasJob) return 0;
            if (islander.Job.LastShiftDay >= _clock.Day) return 0;

            var entry = JobCatalog.Get(islander.Job.Kind);
            int wage = DailyWage(islanderId);

            var job = islander.Job;
            job.LastShiftDay = _clock.Day;
            job.ShiftsWorked++;
            job.TotalEarned += wage;

            bool promoted = job.CanPromote;
            if (promoted)
            {
                job.Rank++;
                job.ShiftsWorked = 0;
            }
            islander.Job = job;

            // Trabajar cansa y da hambre, pero un trabajo que le pega también entretiene.
            _simulation.ApplyNeed(islanderId, NeedKind.Energy, -entry.Hours * 3.5f);
            _simulation.ApplyNeed(islanderId, NeedKind.Hunger, -entry.Hours * 2f);

            float affinity = JobCatalog.Affinity(islander.Personality, job.Kind);
            _simulation.ApplyHappiness(islanderId, (affinity - 0.5f) * 8f);
            _simulation.GrantExperience(islanderId, 15f + job.Rank * 5f);

            islander.Activity = IslanderActivity.Working;
            _island.SendTo(islanderId, entry.ZoneId);

            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
                economy.AddCoins(wage, $"Turno de {entry.DisplayName}");

            if (promoted)
            {
                _simulation.ShowEmotion(islanderId, Emotion.Proud, 6f);
                _simulation.ApplyHappiness(islanderId, 12f);
            }

            return wage;
        }

        public string ZoneOf(JobKind job) =>
            job == JobKind.None ? null : JobCatalog.Get(job).ZoneId;

        /// <summary>
        /// Le busca trabajo a quien no tenga. Lo llama el arranque para que la isla no
        /// empiece con todo el mundo en paro.
        /// </summary>
        public void EmployEveryone()
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Job.HasJob) continue;

                var job = BestJobFor(all[i].Id);
                if (job == JobKind.None) continue;

                // Sin preguntar, y a propósito: esto es «la aldea ya estaba trabajando
                // cuando llegaste», no el jugador repartiendo puestos. Pasando por
                // Assign, a quien no le cuadrara ningún oficio se quedaría en paro para
                // siempre —nunca te va a coger aprecio si no sales de casa— y la
                // economía no cierra sin sueldos.
                Place(all[i], job);
            }
        }
    }
}
