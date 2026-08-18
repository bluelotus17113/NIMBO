using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Player;
using UnityEngine;

namespace Nimbo.Player
{
    /// <summary>
    /// Las cinco vías del protagonista: escucha lo que hace y le va subiendo.
    /// </summary>
    /// <remarks>
    /// **No lo llama nadie.** Se suscribe a los avisos que la isla ya publicaba desde
    /// antes de que existiera la progresión —labrar, cosechar, golpear un nodo,
    /// recogerlo, craftear, regalar, atender una petición, pagar una obra— y cuenta.
    /// Por eso añadir esto no cambió ni una línea de cómo se labra o cómo se tala: la
    /// progresión no es un sistema que se cuela en los demás, es uno que los escucha.
    ///
    /// La contrapartida es que la experiencia se reparte por lo que **ocurre**, no por
    /// lo que se pretendía: <c>TileChanged</c> no dice si fue labrar, sembrar o regar,
    /// así que las tres pagan igual. Distinguirlas obligaría a tocar el huerto, y no
    /// vale lo que cuesta.
    /// </remarks>
    public sealed class PlayerProgressionService : IPlayerProgression, IDisposable
    {
        private readonly PlayerState _player;
        private readonly PlayerProgressionConfig _config;

        private readonly Action<TileChanged> _onTile;
        private readonly Action<CropHarvested> _onHarvest;
        private readonly Action<NodeHit> _onNodeHit;
        private readonly Action<NodeGathered> _onNodeGathered;
        private readonly Action<ItemCrafted> _onCrafted;
        private readonly Action<AffinityChanged> _onAffinity;
        private readonly Action<ItemGifted> _onGifted;
        private readonly Action<RequestResolved> _onRequest;
        private readonly Action<HomeUpgraded> _onHomeUpgraded;

        /// <summary>
        /// Qué vía y qué nivel abre cada cosa.
        /// </summary>
        /// <remarks>
        /// Una tabla y no una cadena de <c>if</c>: es la lista de contenido de toda la
        /// progresión y hay que poder leerla de un vistazo para saber qué falta.
        /// </remarks>
        private static readonly (SkillKind Skill, int Level)[] Requirements =
        {
            (SkillKind.Farming, 2),      // BiggerPlot        — la primera fila de más
            (SkillKind.Farming, 5),      // GenerousHarvest
            (SkillKind.Gathering, 2),    // ReadTheNode
            (SkillKind.Gathering, 5),    // StrongArms
            (SkillKind.Gathering, 6),    // FastRegrowth
            (SkillKind.Gathering, 8),    // DeftHands
            (SkillKind.Crafting, 2),     // MiddlingRecipes
            (SkillKind.Crafting, 4),     // BatchCrafting
            (SkillKind.Crafting, 5),     // FineRecipes
            (SkillKind.Social, 3),       // ExtraGift
            (SkillKind.Social, 5),       // Courtship
            (SkillKind.Village, 2),      // AssignJobs
            (SkillKind.Village, 3),      // UpgradeHomes
            (SkillKind.Crafting, 6),     // BetterTools
            (SkillKind.Farming, 3),      // WideWatering
            (SkillKind.Gathering, 10),   // NodesOnMap
            (SkillKind.Social, 2),       // Compliment
            (SkillKind.Social, 4),       // WarmGestures
            (SkillKind.Crafting, 8),     // EngagementRing
            (SkillKind.Social, 7),       // AskFavour
            (SkillKind.Social, 9),       // Mediate
        };

        public PlayerProgressionService(PlayerState player, PlayerProgressionConfig config = null)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _config = config;

            _onTile = _ => Grant(SkillKind.Farming, Xp.TileWorked);
            _onHarvest = evt => Grant(SkillKind.Farming, Xp.Harvest * Math.Max(1, evt.Quantity));
            _onNodeHit = _ => Grant(SkillKind.Gathering, Xp.NodeHit);
            _onNodeGathered = evt => Grant(SkillKind.Gathering,
                                           Xp.NodeGathered * Math.Max(1, evt.Quantity));
            _onCrafted = _ => Grant(SkillKind.Crafting, Xp.Craft);
            _onAffinity = OnAffinityChanged;
            _onGifted = _ => Grant(SkillKind.Social, Xp.Gift);
            _onRequest = OnRequestResolved;
            _onHomeUpgraded = _ => Grant(SkillKind.Village, Xp.HomeUpgraded);

            EventBus.Subscribe(_onTile);
            EventBus.Subscribe(_onHarvest);
            EventBus.Subscribe(_onNodeHit);
            EventBus.Subscribe(_onNodeGathered);
            EventBus.Subscribe(_onCrafted);
            EventBus.Subscribe(_onAffinity);
            EventBus.Subscribe(_onGifted);
            EventBus.Subscribe(_onRequest);
            EventBus.Subscribe(_onHomeUpgraded);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe(_onTile);
            EventBus.Unsubscribe(_onHarvest);
            EventBus.Unsubscribe(_onNodeHit);
            EventBus.Unsubscribe(_onNodeGathered);
            EventBus.Unsubscribe(_onCrafted);
            EventBus.Unsubscribe(_onAffinity);
            EventBus.Unsubscribe(_onGifted);
            EventBus.Unsubscribe(_onRequest);
            EventBus.Unsubscribe(_onHomeUpgraded);
        }

        // ── lo que escucha ───────────────────────────────────────────────────

        /// <summary>
        /// Solo cuenta el cariño que se gana **el protagonista**.
        /// </summary>
        /// <remarks>
        /// <c>AffinityChanged</c> lo publican también los vecinos entre ellos, que se
        /// caen bien solos todo el día. Sin este filtro, Convivencia subiría sola
        /// mientras el jugador mira: dos vecinos charlando en la plaza le pagarían la
        /// vía social entera.
        /// </remarks>
        private void OnAffinityChanged(AffinityChanged evt)
        {
            if (evt.FromId != SocialIds.Player && evt.ToId != SocialIds.Player) return;
            if (evt.Delta <= 0f) return;

            Grant(SkillKind.Social, Xp.Affinity);
        }

        /// <summary>
        /// Atender una petición paga en dos vías, y a propósito.
        /// </summary>
        /// <remarks>
        /// Es a la vez un favor a una persona —Convivencia— y una cosa que hace quien
        /// lleva la aldea —Aldea—. Que pague en las dos es lo que hace que el jugador
        /// que solo atiende peticiones acabe pudiendo repartir trabajos, sin haber
        /// tenido que ir a buscar otra actividad distinta para eso.
        ///
        /// Negarse no paga nada. Tampoco resta: decir que no ya cuesta el ánimo del
        /// vecino, y cobrarlo dos veces sería castigo.
        /// </remarks>
        private void OnRequestResolved(RequestResolved evt)
        {
            if (!evt.Satisfied) return;

            Grant(SkillKind.Social, Xp.Request);
            Grant(SkillKind.Village, Xp.Request);
        }

        // ── IPlayerProgression ───────────────────────────────────────────────

        public int LevelOf(SkillKind skill) => _player.Skills.LevelOf(skill);

        public float XpOf(SkillKind skill) => _player.Skills[skill].Xp;

        public float XpNeededFor(SkillKind skill)
        {
            int level = LevelOf(skill);
            return level >= SkillSet.MaxLevel ? 0f : SkillSet.XpForLevel(level);
        }

        public int VillagerLevel => _player.Skills.VillagerLevel;

        public bool IsUnlocked(Unlock unlock)
        {
            RequirementFor(unlock, out var skill, out int level);
            return LevelOf(skill) >= level;
        }

        public void RequirementFor(Unlock unlock, out SkillKind skill, out int level)
        {
            int index = (int)unlock;
            if (index < 0 || index >= Requirements.Length)
            {
                // Un desbloqueo sin fila en la tabla queda cerrado, no abierto. Al revés
                // sería regalar contenido cada vez que alguien añade un valor al enum.
                skill = SkillKind.Farming;
                level = int.MaxValue;
                return;
            }

            skill = Requirements[index].Skill;
            level = Requirements[index].Level;
        }

        /// <summary>
        /// Suma experiencia y sube de nivel si toca.
        /// </summary>
        /// <remarks>
        /// En bucle y no con un solo salto: un premio gordo puede valer más de un
        /// nivel entero en las vías bajas, y comerse el sobrante sería perderlo.
        /// </remarks>
        public void Grant(SkillKind skill, float xp)
        {
            if (xp <= 0f) return;

            var line = _player.Skills[skill];
            if (line.Level >= SkillSet.MaxLevel) return;

            line.Xp += xp;

            while (line.Level < SkillSet.MaxLevel && line.Xp >= SkillSet.XpForLevel(line.Level))
            {
                line.Xp -= SkillSet.XpForLevel(line.Level);
                line.Level++;

                _player.Skills[skill] = line;
                Announce(skill, line.Level);
            }

            if (line.Level >= SkillSet.MaxLevel) line.Xp = 0f;
            _player.Skills[skill] = line;
        }

        /// <summary>Cuenta el nivel y, aparte, lo que se acaba de ganar el derecho a hacer.</summary>
        private void Announce(SkillKind skill, int level)
        {
            EventBus.Publish(new SkillLeveledUp(skill, level));

            for (int i = 0; i < Requirements.Length; i++)
            {
                if (Requirements[i].Skill != skill || Requirements[i].Level != level) continue;
                EventBus.Publish(new UnlockGained((Unlock)i));
            }
        }

        // ── los números ──────────────────────────────────────────────────────

        /// <summary>Lo que paga cada cosa, del ajuste si lo hay y si no de aquí.</summary>
        private XpTable Xp => _config != null ? _config.Table : XpTable.Default;

        /// <summary>
        /// Lo que vale cada acción, de 2 a 12.
        /// </summary>
        /// <remarks>
        /// Lo que cuesta más paga más, y nada paga tanto como para que convenga
        /// repetirlo en bucle: la diferencia entre lo más barato y lo más caro es de
        /// seis veces, no de cien. Un juego donde una acción rinde cien veces más que
        /// otra es un juego donde solo se hace esa.
        /// </remarks>
        public readonly struct XpTable
        {
            public readonly float TileWorked;
            public readonly float Harvest;
            public readonly float NodeHit;
            public readonly float NodeGathered;
            public readonly float Craft;
            public readonly float Affinity;
            public readonly float Gift;
            public readonly float Request;
            public readonly float HomeUpgraded;

            public XpTable(float tileWorked, float harvest, float nodeHit, float nodeGathered,
                           float craft, float affinity, float gift, float request,
                           float homeUpgraded)
            {
                TileWorked = tileWorked; Harvest = harvest; NodeHit = nodeHit;
                NodeGathered = nodeGathered; Craft = craft; Affinity = affinity;
                Gift = gift; Request = request; HomeUpgraded = homeUpgraded;
            }

            public static XpTable Default => new XpTable(
                tileWorked: 2f,     // labrar, sembrar o regar una casilla
                harvest: 5f,        // por unidad cosechada
                nodeHit: 2f,        // cada hachazo
                nodeGathered: 4f,   // por unidad que suelta
                craft: 8f,
                affinity: 3f,
                gift: 6f,
                request: 10f,
                homeUpgraded: 12f);
        }
    }

    /// <summary>Los números de la progresión, por si hay que moverlos sin recompilar.</summary>
    [CreateAssetMenu(fileName = "PlayerProgressionConfig",
                     menuName = "Isla Nimbo/Configuración de progresión")]
    public sealed class PlayerProgressionConfig : ScriptableObject
    {
        [Header("Experiencia por acción")]
        [Min(0f)] public float TileWorked = 2f;
        [Min(0f)] public float Harvest = 5f;
        [Min(0f)] public float NodeHit = 2f;
        [Min(0f)] public float NodeGathered = 4f;
        [Min(0f)] public float Craft = 8f;
        [Min(0f)] public float Affinity = 3f;
        [Min(0f)] public float Gift = 6f;
        [Min(0f)] public float Request = 10f;
        [Min(0f)] public float HomeUpgraded = 12f;

        public PlayerProgressionService.XpTable Table =>
            new PlayerProgressionService.XpTable(TileWorked, Harvest, NodeHit, NodeGathered,
                                                 Craft, Affinity, Gift, Request, HomeUpgraded);
    }
}
