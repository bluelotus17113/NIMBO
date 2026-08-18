using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Gathering;
using Nimbo.Island;
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Jobs;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Las tres cosas que convierten la aldea en gestión y no en una lista desplegable.
    /// </summary>
    /// <remarks>
    /// Negarse a un trabajo, mediar en una riña y pedir un favor. Las tres tienen la
    /// misma forma: el jugador **sugiere** y el vecino decide o cobra por ello, que es
    /// lo que hace que caerle bien a la gente sirva para algo concreto.
    /// </remarks>
    public class GestionAldeaTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SocialService _social;
        private JobService _jobs;
        private EconomyService _economia;
        private InventoryService _mochila;
        private GatheringService _recoleccion;

        private string _bea;
        private string _leo;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            var catalog = new ItemCatalog();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();

            var simulation = new SimulationService(_registry, personalities, needsConfig);
            _economia = new EconomyService(catalog, _save);
            _mochila = new InventoryService(_save.Player, _economia);
            var island = new IslandService(_registry, _save);
            _recoleccion = new GatheringService(new NodeCatalog(), _save.Gathering,
                                                _mochila, _clock);
            _jobs = new JobService(_registry, simulation, island, _clock);

            var factory = new IslanderFactory(_registry, personalities, _clock,
                                              new List<string>());
            _social = new SocialService(_registry, personalities, simulation, factory,
                                        _clock, socialConfig, _save.Triangles, _save.Weddings);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(simulation);
            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IInventoryService>(_mochila);
            ServiceRegistry.Register<IGatheringService>(_recoleccion);
            ServiceRegistry.Register<ISocialService>(_social);
            ServiceRegistry.Register<IJobService>(_jobs);

            var bea = factory.CreateRandom("bea");
            var leo = factory.CreateRandom("leo");
            _registry.Add(bea);
            _registry.Add(leo);
            _save.Islanders.Add(bea);
            _save.Islanders.Add(leo);
            _bea = bea.Id;
            _leo = leo.Id;
        }

        [TearDown]
        public void TearDown()
        {
            _jobs?.Dispose();
            _social?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── negarse a un trabajo (§15.1) ─────────────────────────────────────

        [Test]
        public void ElOficioQueLePegaLoCogeAunqueApenasTeConozca()
        {
            var mejor = _jobs.BestJobFor(_bea);
            if (mejor == JobKind.None) Assert.Ignore("no hay oficios abiertos en esta isla");

            Assert.That(_jobs.WouldAccept(_bea, mejor), Is.True,
                "nadie rechaza el trabajo de su vida porque el alcalde le caiga regular");
        }

        [Test]
        public void ElQueNoLePegaLoRechazaSiNoSoisAmigos()
        {
            if (!TryPeorOficio(out var peor)) Assert.Ignore("todos los oficios le pegan");

            Assert.That(_jobs.WouldAccept(_bea, peor), Is.False);
            Assert.That(_jobs.Assign(_bea, peor), Is.False,
                "y no basta con que el botón no salga: el servicio también dice que no");
        }

        [Test]
        public void PeroLoHaceSiSeLoHasGanado()
        {
            if (!TryPeorOficio(out var peor)) Assert.Ignore("todos los oficios le pegan");

            CaerleBien(_bea, 60f);

            Assert.That(_jobs.WouldAccept(_bea, peor), Is.True,
                "para colocar a la gente donde rinde, primero hay que caerle bien");
        }

        [Test]
        public void ElRepartoDeSalidaNoDejaANadieEnParo()
        {
            // Pasando por Assign, a quien no le cuadrara ningún oficio se quedaría sin
            // trabajo para siempre —nunca te va a coger aprecio si no sale de casa— y
            // la economía no cierra sin sueldos.
            _jobs.EmployEveryone();

            if (_jobs.AvailableJobs.Count == 0) Assert.Ignore("no hay oficios abiertos");

            foreach (var islander in _registry.All)
                Assert.That(islander.Job.HasJob, Is.True, $"{islander.Id} se quedó en paro");
        }

        // ── mediar (§13.2, Convivencia 9) ────────────────────────────────────

        [Test]
        public void MediarBajaUnEscalonYEnLosDosLados()
        {
            Reñir(ConflictStage.Feud);

            Assert.That(_social.PlayerMediate(_bea), Is.EqualTo(_leo));

            Assert.That(_social.GetRelationship(_bea, _leo).Conflict,
                Is.EqualTo(ConflictStage.Quarrel));
            Assert.That(_social.GetRelationship(_leo, _bea).Conflict,
                Is.EqualTo(ConflictStage.Quarrel),
                "una riña es de dos: arreglada en un solo lado, uno sigue guardando " +
                "rencor a alguien que ya no se lo guarda");
        }

        [Test]
        public void MediarNoBorraLaRiñaDeGolpe()
        {
            Reñir(ConflictStage.Feud);
            _social.PlayerMediate(_bea);

            Assert.That(_social.GetRelationship(_bea, _leo).Conflict,
                Is.Not.EqualTo(ConflictStage.None),
                "un botón que arregla una enemistad entera convierte las riñas de la " +
                "aldea en una tarea de mantenimiento");
        }

        [Test]
        public void MediarTambienSubeLaAfinidad()
        {
            Reñir(ConflictStage.Quarrel);
            float antes = _social.GetRelationship(_bea, _leo).Affinity;

            _social.PlayerMediate(_bea);

            Assert.That(_social.GetRelationship(_bea, _leo).Affinity, Is.GreaterThan(antes),
                "el escalón sale de la afinidad; sin tocarla, la reevaluación del día " +
                "siguiente los devuelve donde estaban");
        }

        [Test]
        public void SinRiñaNoHayNadaQueMediar()
        {
            Assert.That(_social.PlayerMediate(_bea), Is.Null);
        }

        // ── pedir un favor (§15.1, Convivencia 7) ────────────────────────────

        [Test]
        public void UnFavorTeTraeAlgoDeLaIsla()
        {
            string traido = _social.PlayerAskFavour(_bea);

            Assert.That(traido, Is.Not.Null.And.Not.Empty);
            Assert.That(_mochila.CountOf(traido), Is.GreaterThan(0));
            Assert.That(_economia.GetItem(traido), Is.Not.Null,
                "te ha traído algo que no está en el catálogo");
        }

        [Test]
        public void SoloUnoAlDiaPorVecino()
        {
            Assert.That(_social.PlayerAskFavour(_bea), Is.Not.Null);
            Assert.That(_social.PlayerAskFavour(_bea), Is.Null,
                "es un favor, no un empleado");
        }

        [Test]
        public void PeroCadaVecinoTieneElSuyo()
        {
            Assert.That(_social.PlayerAskFavour(_bea), Is.Not.Null);
            Assert.That(_social.PlayerAskFavour(_leo), Is.Not.Null);
        }

        [Test]
        public void PedirFavoresDesgasta()
        {
            CaerleBien(_bea, 50f);
            float antes = _social.PlayerRelationship(_bea).Affinity;

            _social.PlayerAskFavour(_bea);

            Assert.That(_social.PlayerRelationship(_bea).Affinity, Is.LessThan(antes),
                "si no costara nada, lo suyo sería pedirle a los doce todos los días y " +
                "la amistad no valdría para nada");
        }

        // ── utilidades ───────────────────────────────────────────────────────

        /// <summary>Un oficio abierto que a Bea no le pegue nada.</summary>
        private bool TryPeorOficio(out JobKind peor)
        {
            peor = JobKind.None;
            float worst = float.MaxValue;

            foreach (var job in _jobs.AvailableJobs)
            {
                float fit = _jobs.AffinityFor(_bea, job);
                if (fit >= worst) continue;
                worst = fit;
                peor = job;
            }

            return peor != JobKind.None && worst < 0.3f;
        }

        private void CaerleBien(string islanderId, float afinidad)
        {
            var islander = _registry.Get(islanderId);
            var record = islander.Relationships.GetOrCreate(SocialIds.Player);
            record.Affinity = afinidad;
            record.Friendship = FriendshipStage.Friend;
            record.Interactions = 10;
            islander.Relationships.Set(record);
        }

        private void Reñir(ConflictStage stage)
        {
            Poner(_bea, _leo, stage);
            Poner(_leo, _bea, stage);

            void Poner(string deQuien, string aQuien, ConflictStage etapa)
            {
                var who = _registry.Get(deQuien);
                var record = who.Relationships.GetOrCreate(aQuien);
                record.Conflict = etapa;
                record.Affinity = -60f;
                record.Interactions = 5;
                who.Relationships.Set(record);
            }
        }
    }
}
