using System;
using System.Collections.Generic;

namespace Nimbo.Core.Util
{
    /// <summary>
    /// Azar con semilla propia, independiente de <c>UnityEngine.Random</c>.
    /// </summary>
    /// <remarks>
    /// Hace falta uno propio porque los gustos y los rasgos de un habitante se derivan
    /// de su id: si dos ejecuciones dan resultados distintos, el mismo habitante cambia
    /// de comida favorita entre partidas. Con <c>FromSeed(id)</c> eso no puede pasar.
    /// </remarks>
    public struct Rng
    {
        private uint _state;

        public Rng(uint seed) => _state = seed == 0 ? 0x9E3779B9u : seed;

        public static Rng FromSeed(string seed)
        {
            // FNV-1a: barato, sin colisiones a la vista para ids tipo GUID, y estable
            // entre plataformas — que es justo lo que string.GetHashCode() no garantiza.
            uint hash = 2166136261u;
            for (int i = 0; i < seed.Length; i++)
            {
                hash ^= seed[i];
                hash *= 16777619u;
            }
            return new Rng(hash);
        }

        public static Rng FromTime() => new Rng((uint)DateTime.UtcNow.Ticks);

        private uint NextBits()
        {
            // xorshift32
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        /// <summary>Float en [0, 1).</summary>
        public float NextFloat() => (NextBits() >> 8) * (1f / 16777216f);

        public float Range(float min, float max) => min + NextFloat() * (max - min);

        /// <summary>Entero en [min, max). Vacío si el rango no tiene sentido.</summary>
        public int Range(int min, int max) => max <= min ? min : min + (int)(NextBits() % (uint)(max - min));

        public bool Chance(float probability) => NextFloat() < probability;

        public T Pick<T>(IReadOnlyList<T> list) =>
            list == null || list.Count == 0 ? default : list[Range(0, list.Count)];

        /// <summary>Baraja en el sitio (Fisher-Yates).</summary>
        public void Shuffle<T>(IList<T> list)
        {
            if (list == null) return;
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }

        /// <summary>
        /// Elige un índice con pesos. Devuelve -1 si todos los pesos son cero, que es
        /// distinto de "elige el primero" y hay que tratarlo aparte.
        /// </summary>
        public int PickWeighted(IReadOnlyList<float> weights)
        {
            if (weights == null || weights.Count == 0) return -1;

            float total = 0f;
            for (int i = 0; i < weights.Count; i++)
                if (weights[i] > 0f) total += weights[i];

            if (total <= 0f) return -1;

            float roll = NextFloat() * total;
            for (int i = 0; i < weights.Count; i++)
            {
                if (weights[i] <= 0f) continue;
                roll -= weights[i];
                if (roll <= 0f) return i;
            }
            return weights.Count - 1;
        }
    }
}
