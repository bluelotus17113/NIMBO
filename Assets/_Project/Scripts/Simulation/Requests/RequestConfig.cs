using System;
using Nimbo.Data.Requests;
using UnityEngine;

namespace Nimbo.Simulation.Requests
{
    /// <summary>
    /// Los números del sistema de peticiones. Salen de <c>Docs/Contratos/peticiones.md</c>.
    /// </summary>
    [CreateAssetMenu(fileName = "RequestConfig", menuName = "Isla Nimbo/Configuración de peticiones")]
    public sealed class RequestConfig : ScriptableObject
    {
        [Serializable]
        public struct Reward
        {
            public RequestKind Kind;
            public int Coins;
            public float Experience;
        }

        [Header("Cada cuánto y cuántas")]
        [Tooltip("Probabilidad de que pida algo, cada media hora de juego.")]
        [Range(0f, 1f)] public float BaseChance = 0.40f;

        [Tooltip("Cuánto suma a esa probabilidad ser expresivo.")]
        [Range(0f, 0.5f)] public float ExpressionInfluence = 0.10f;

        [Tooltip("Cuánto suma a esa probabilidad ser sociable.")]
        [Range(0f, 0.5f)] public float AttitudeInfluence = 0.05f;

        [Tooltip("Más de esto a la vez agobia y deja de leerse.")]
        [Min(1)] public int MaxOpenPerIslander = 3;

        [Header("Cuánto aguanta sin que la atiendan, en horas de juego")]
        public int LifetimeLowHours = 24;
        public int LifetimeNormalHours = 12;
        public int LifetimeHighHours = 6;
        public int LifetimeCriticalHours = 3;

        [Header("Recompensas base")]
        [SerializeField]
        private Reward[] _rewards =
        {
            new Reward { Kind = RequestKind.Food,           Coins = 10, Experience = 20f },
            new Reward { Kind = RequestKind.Object,         Coins = 15, Experience = 30f },
            new Reward { Kind = RequestKind.Clothes,        Coins = 20, Experience = 25f },
            new Reward { Kind = RequestKind.Advice,         Coins = 5,  Experience = 40f },
            new Reward { Kind = RequestKind.Favor,          Coins = 25, Experience = 35f },
            new Reward { Kind = RequestKind.Complaint,      Coins = 15, Experience = 20f },
            new Reward { Kind = RequestKind.SocialIntro,    Coins = 10, Experience = 30f },
            new Reward { Kind = RequestKind.Activity,       Coins = 30, Experience = 50f },
            new Reward { Kind = RequestKind.IslandBuilding, Coins = 50, Experience = 100f },
            new Reward { Kind = RequestKind.Confession,     Coins = 20, Experience = 60f },
            new Reward { Kind = RequestKind.Reconcile,      Coins = 20, Experience = 45f },
        };

        [Header("Multiplicadores")]
        [Tooltip("Lo que suma resolver una petición que le pega a su personalidad.")]
        public float AlignedBonus = 1.25f;

        [Tooltip("Lo que multiplica la recompensa una petición urgente.")]
        public float UrgencyBonus = 1.5f;

        public int LifetimeMinutes(RequestPriority priority) => 60 * (priority switch
        {
            RequestPriority.Critical => LifetimeCriticalHours,
            RequestPriority.High => LifetimeHighHours,
            RequestPriority.Normal => LifetimeNormalHours,
            _ => LifetimeLowHours,
        });

        public int CoinsFor(RequestKind kind, RequestPriority priority)
        {
            float coins = Find(kind).Coins;
            if (priority >= RequestPriority.High) coins *= UrgencyBonus;
            return Mathf.RoundToInt(coins);
        }

        public float ExperienceFor(RequestKind kind, RequestPriority priority)
        {
            float xp = Find(kind).Experience;
            if (priority >= RequestPriority.High) xp *= UrgencyBonus;
            return xp;
        }

        private Reward Find(RequestKind kind)
        {
            for (int i = 0; i < _rewards.Length; i++)
                if (_rewards[i].Kind == kind) return _rewards[i];

            Debug.LogWarning($"RequestConfig: no hay recompensa para {kind}");
            return new Reward { Kind = kind, Coins = 10, Experience = 20f };
        }
    }
}
