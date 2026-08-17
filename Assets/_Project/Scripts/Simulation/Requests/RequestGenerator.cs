using System;
using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Data.Social;
using UnityEngine;

namespace Nimbo.Simulation.Requests
{
    /// <summary>
    /// Decide qué pide un habitante y cuándo. Es el motor del bucle central: sin esto
    /// la isla es un salvapantallas.
    /// </summary>
    /// <remarks>
    /// Los pesos base salen de <c>Docs/Contratos/peticiones.md</c> y se multiplican
    /// luego por el peso que ese tipo de personalidad le da a esa petición. Dos capas
    /// en vez de una: la primera dice qué necesita, la segunda cómo es él.
    /// </remarks>
    public sealed class RequestGenerator
    {
        private readonly IIslanderRegistry _registry;
        private readonly IPersonalityService _personalities;
        private readonly RequestConfig _config;
        private readonly IGatheringService _gathering;
        private readonly IEconomyService _economy;

        private readonly List<float> _weights = new List<float>(RequestKindCount);
        private readonly List<RequestKind> _kinds = new List<RequestKind>(RequestKindCount);

        private const int RequestKindCount = 12;

        /// <summary>Cuántas unidades pide un encargo de material, de menos a más.</summary>
        private const int MinMaterialAmount = 3;
        private const int MaxMaterialAmount = 7;   // exclusivo

        /// <summary>
        /// Lo que se paga por el material del encargo, sobre su precio de venta.
        /// </summary>
        /// <remarks>
        /// Por encima de uno a propósito: **dárselo a un vecino tiene que rentar más que
        /// venderlo en el cajón**. Si rentase menos, el encargo sería un impuesto al que
        /// se molesta en atenderlo, y nadie volvería a leer el tablón después de hacer
        /// la cuenta una vez. Es el enganche entre la recolección y la vida social, y un
        /// enganche que sale a perder no engancha nada.
        /// </remarks>
        private const float MaterialPayoutFactor = 1.35f;

        /// <param name="gathering">
        /// De dónde salen los materiales que se pueden pedir. Sin él no se generan
        /// encargos: pedir algo que ningún árbol ni ninguna roca de la isla suelta es
        /// mandar al jugador a por una cosa que no existe.
        /// </param>
        /// <param name="economy">Para saber lo que vale lo que piden. Sin él, la paga es la base.</param>
        public RequestGenerator(IIslanderRegistry registry, IPersonalityService personalities,
                                RequestConfig config, IGatheringService gathering = null,
                                IEconomyService economy = null)
        {
            _registry = registry;
            _personalities = personalities;
            _config = config;
            _gathering = gathering;
            _economy = economy;
        }

        /// <summary>¿Le toca pedir algo? Se pregunta cada media hora de juego.</summary>
        public bool ShouldGenerate(IslanderData islander, int openRequests, ref Rng rng)
        {
            if (openRequests >= _config.MaxOpenPerIslander) return false;

            // Un habitante dormido o hundido no pide nada: pedir es un acto social.
            if (islander.Activity == IslanderActivity.Sleeping) return false;

            float chance = _config.BaseChance
                         + islander.Personality.Expression * _config.ExpressionInfluence
                         + islander.Personality.Attitude * _config.AttitudeInfluence;

            return rng.Chance(Mathf.Clamp01(chance));
        }

        /// <summary>
        /// Elige el tipo por ruleta de pesos. Devuelve false solo si todos los pesos
        /// salieron a cero, que no debería pasar pero se comprueba igual.
        /// </summary>
        public bool PickKind(IslanderData islander, out RequestKind kind, ref Rng rng)
        {
            var behaviour = _personalities.For(islander.Personality);
            var p = islander.Personality;
            var needs = islander.Needs;

            _weights.Clear();
            _kinds.Clear();

            Add(RequestKind.Food, NeedWeight(needs.Hunger, 30f));
            Add(RequestKind.Object, 15f + p.Outlook * 5f);
            Add(RequestKind.Clothes, 10f + p.Expression * 5f);
            Add(RequestKind.Advice, NeedWeight(islander.Mood.Happiness, 20f) + p.Attitude * 3f);
            Add(RequestKind.Favor, 12f);
            Add(RequestKind.Complaint, HasConflict(islander) ? 20f : 2f);
            Add(RequestKind.SocialIntro, HasStrangers(islander) ? 18f : 3f);
            Add(RequestKind.Activity, 10f + p.Energy * 8f);
            Add(RequestKind.IslandBuilding, 5f);
            Add(RequestKind.Confession, HasCrush(islander) ? 14f : 0f);
            Add(RequestKind.Reconcile, HasConflict(islander) ? 12f : 0f);

            // Los encargos de material solo existen si hay de dónde sacarlo. Sin
            // servicio de recolección la isla no tiene nodos, y un encargo que no se
            // puede cumplir solo sirve para restar ánimo cuando caduca.
            Add(RequestKind.Material, _gathering != null ? 14f : 0f);

            int index = rng.PickWeighted(_weights);
            if (index < 0) { kind = RequestKind.Favor; return false; }

            kind = _kinds[index];
            return true;

            void Add(RequestKind k, float baseWeight)
            {
                _kinds.Add(k);
                _weights.Add(Mathf.Max(0f, baseWeight) * behaviour.RequestWeight(k));
            }
        }

        /// <summary>Una necesidad en rojo multiplica por cuatro las ganas de pedir eso.</summary>
        private static float NeedWeight(float value, float baseWeight)
        {
            if (value <= NeedState.CriticalThreshold) return baseWeight * 4f;
            if (value <= NeedState.LowThreshold) return baseWeight * 2f;
            return baseWeight;
        }

        /// <summary>Monta la petición ya rellena: objetivo, frase, caducidad y recompensa.</summary>
        public IslanderRequest Build(IslanderData islander, RequestKind kind, long nowMinute,
                                     ref Rng rng)
        {
            var priority = PriorityFor(islander, kind);
            var behaviour = _personalities.For(islander.Personality);

            string target = PickTarget(islander, kind, ref rng);
            int amount = kind == RequestKind.Material
                ? rng.Range(MinMaterialAmount, MaxMaterialAmount)
                : 0;

            return new IslanderRequest
            {
                RequestId = Guid.NewGuid().ToString("N"),
                IslanderId = islander.Id,
                Kind = kind,
                Priority = priority,
                Resolution = RequestResolution.Pending,
                TargetId = target,
                Amount = amount,
                Line = behaviour.PickLine(LineMoodFor(kind), ref rng),
                CreatedMinute = nowMinute,
                ExpiresMinute = nowMinute + _config.LifetimeMinutes(priority),
                ExperienceReward = _config.ExperienceFor(kind, priority),
                CoinReward = _config.CoinsFor(kind, priority) + PayloadBonus(target, amount),
            };
        }

        /// <summary>Lo que se paga aparte por el material que hay que traer.</summary>
        /// <remarks>
        /// Se suma a la recompensa base en vez de sustituirla: la base paga el favor y
        /// esto paga la caminata. Sin esto, un encargo de seis conchas —a catorce
        /// nimbos la unidad— se atendería a cambio de perder sesenta.
        /// </remarks>
        private int PayloadBonus(string catalogId, int amount)
        {
            if (amount <= 0 || _economy == null || string.IsNullOrEmpty(catalogId)) return 0;

            var item = _economy.GetItem(catalogId);
            if (item == null) return 0;

            return Mathf.RoundToInt(item.Price * amount * MaterialPayoutFactor);
        }

        private static LineMood LineMoodFor(RequestKind kind) => kind switch
        {
            RequestKind.Complaint => LineMood.Angry,
            RequestKind.SocialIntro => LineMood.Meeting,
            RequestKind.Confession => LineMood.Happy,
            RequestKind.Activity => LineMood.Happy,
            _ => LineMood.Bored,
        };

        private static RequestPriority PriorityFor(IslanderData islander, RequestKind kind)
        {
            if (kind == RequestKind.Food)
            {
                var band = islander.Needs.Band(NeedKind.Hunger);
                if (band == NeedBand.Critical) return RequestPriority.Critical;
                if (band == NeedBand.Low) return RequestPriority.High;
            }

            if (kind == RequestKind.Advice && islander.Mood.Happiness <= NeedState.CriticalThreshold)
                return RequestPriority.Critical;

            return kind switch
            {
                RequestKind.Complaint or RequestKind.Reconcile => RequestPriority.High,

                // El encargo de material va con la prioridad más baja **por la
                // caducidad**, no por lo poco que importe: es la que da un día entero
                // de juego para cumplirlo. Con doce horas habría que salir corriendo, y
                // un recado que hay que hacer corriendo deja de ser un recado.
                RequestKind.Object or RequestKind.Clothes or RequestKind.Material
                    => RequestPriority.Low,

                _ => RequestPriority.Normal,
            };
        }

        /// <summary>A quién se refiere la petición, cuando se refiere a alguien.</summary>
        private string PickTarget(IslanderData islander, RequestKind kind, ref Rng rng)
        {
            switch (kind)
            {
                // Va aparte: un desconocido de verdad todavía no tiene ficha en la
                // agenda, así que hay que buscarlo en el censo y no en las relaciones.
                case RequestKind.SocialIntro:
                    return PickUnknown(islander, ref rng);

                case RequestKind.Complaint:
                    return PickWhere(islander, r => r.Affinity < -10f, ref rng);

                case RequestKind.Reconcile:
                    return PickWhere(islander, r => r.Conflict != ConflictStage.None, ref rng);

                case RequestKind.Confession:
                    return PickWhere(islander, r => r.Romance == RomanceStage.Crush, ref rng);

                case RequestKind.Activity:
                    return PickWhere(islander, r => r.Friendship >= FriendshipStage.Friend, ref rng);

                case RequestKind.Material:
                    return PickMaterial(ref rng);

                default:
                    return null;
            }
        }

        /// <summary>
        /// Un material de los que suelta la isla.
        /// </summary>
        /// <remarks>
        /// Sale del catálogo de nodos y **no** del de objetos, y esa es toda la gracia:
        /// el catálogo de materiales tiene treinta entradas y la isla solo suelta unas
        /// pocas. Pidiendo del catálogo de objetos, la mitad de los encargos serían de
        /// cosas que no hay forma de conseguir.
        ///
        /// Sortea por nodos y no por materiales, así que lo que sueltan muchos árboles
        /// —la madera— se pide más que lo que suelta un solo sitio. Sale gratis y es lo
        /// que uno querría de todas formas: los recados corrientes son de cosas
        /// corrientes.
        /// </remarks>
        private string PickMaterial(ref Rng rng)
        {
            var nodes = _gathering?.Catalog;
            if (nodes == null || nodes.Count == 0) return null;

            string chosen = null;
            int seen = 0;
            for (int i = 0; i < nodes.Count; i++)
            {
                if (string.IsNullOrEmpty(nodes[i].DropId)) continue;
                seen++;
                if (rng.Range(0, seen) == 0) chosen = nodes[i].DropId;
            }
            return chosen;
        }

        private string PickWhere(IslanderData islander, Func<RelationshipRecord, bool> predicate,
                                 ref Rng rng)
        {
            var records = islander.Relationships?.Records;
            if (records == null) return null;

            // Reservoir sampling: una sola pasada y sin reservar lista aparte, que esto
            // corre para cada habitante cada media hora de juego.
            string chosen = null;
            int seen = 0;
            for (int i = 0; i < records.Count; i++)
            {
                if (!predicate(records[i])) continue;
                seen++;
                if (rng.Range(0, seen) == 0) chosen = records[i].OtherId;
            }
            return chosen;
        }

        private string PickUnknown(IslanderData islander, ref Rng rng)
        {
            var all = _registry.All;
            string chosen = null;
            int seen = 0;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == islander.Id) continue;
                if (islander.Relationships.TryGet(all[i].Id, out var record) &&
                    record.Friendship != FriendshipStage.Stranger) continue;

                seen++;
                if (rng.Range(0, seen) == 0) chosen = all[i].Id;
            }
            return chosen;
        }

        private static bool HasConflict(IslanderData islander)
        {
            var records = islander.Relationships?.Records;
            if (records == null) return false;
            for (int i = 0; i < records.Count; i++)
                if (records[i].Conflict != ConflictStage.None) return true;
            return false;
        }

        private static bool HasCrush(IslanderData islander)
        {
            var records = islander.Relationships?.Records;
            if (records == null) return false;
            for (int i = 0; i < records.Count; i++)
                if (records[i].Romance == RomanceStage.Crush) return true;
            return false;
        }

        private bool HasStrangers(IslanderData islander) =>
            _registry.Count - 1 > CountKnown(islander);

        private static int CountKnown(IslanderData islander)
        {
            var records = islander.Relationships?.Records;
            if (records == null) return 0;
            int known = 0;
            for (int i = 0; i < records.Count; i++)
                if (records[i].Friendship != FriendshipStage.Stranger) known++;
            return known;
        }
    }
}
