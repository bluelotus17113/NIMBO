using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Art.CameraWork
{
    /// <summary>
    /// Los bultos que solo la cámara respeta: troncos, rocas y adornos altos.
    /// </summary>
    /// <remarks>
    /// **No lleva colisionadores, a propósito**, y por eso existe como lista propia en
    /// vez de como capa física. Los nodos van sin colisionador para que un vecino no se
    /// pare donde al jugador sí le cuesta pasar (ver los remarks de
    /// <c>GatheringView</c>); una capa «solo cámara» arreglaría esa asimetría pero
    /// obligaría a repasar todos los rayos de la isla —el que busca el suelo bajo cada
    /// nodo, el de las puntas del puente—, que hoy chocan contra lo que haya y con
    /// ciento veinte colisionadores nuevos pasarían a considerarlos todos. Aquí nadie
    /// choca: la lista solo la lee el antiobstáculos de <c>IslandCamera</c>, el jugador
    /// sigue atravesando lo que siempre ha atravesado y los vecinos lo mismo.
    ///
    /// El coste por fotograma es una pasada por la lista haciendo tres productos
    /// escalares por esfera; con el recuento real de isla se mide en la prueba de juego
    /// y va en microsegundos. Ciento veinte colisionadores serían, en cambio, ciento
    /// veinte candidatos más para **cada** rayo de la escena.
    /// </remarks>
    public static class CameraObstacles
    {
        private struct Entry
        {
            // El dueño deja de baja todo lo suyo cuando se destruye o se muda.
            public Object Owner;
            public Vector3 Centre;
            public float Radius;
        }

        private static readonly List<Entry> Entries = new();

        /// <summary>Cuántas esferas hay registradas ahora mismo.</summary>
        public static int Count => Entries.Count;

        /// <summary>Registra una esfera a nombre de un dueño. Un dueño puede tener varias.</summary>
        public static void Add(Object owner, Vector3 centre, float radius) =>
            Entries.Add(new Entry { Owner = owner, Centre = centre, Radius = radius });

        /// <summary>Dá de baja TODAS las esferas de ese dueño.</summary>
        public static void Remove(Object owner) =>
            Entries.RemoveAll(entry => entry.Owner == owner);

        /// <summary>Vacía el registro entero. Lo llaman las pruebas entre cargas.</summary>
        public static void Clear() => Entries.Clear();

        /// <summary>
        /// A qué distancia del origen corta el primer bulto a un rayo de longitud
        /// limitada. Falso si no lo corta ninguno.
        /// </summary>
        /// <remarks>
        /// Intersección rayo-esfera de toda la vida sobre el segmento
        /// <paramref name="origin"/> → <paramref name="origin"/> +
        /// <paramref name="direction"/> · <paramref name="maxDistance"/>. De paso
        /// expulsa a los dueños destruidos: quien registra se olvida, y la lista no
        /// acumula cadáveres entre cargas de escena.
        /// </remarks>
        public static bool TryHit(Vector3 origin, Vector3 direction, float maxDistance,
                                  out float distance)
        {
            distance = float.MaxValue;
            bool found = false;

            for (int i = Entries.Count - 1; i >= 0; i--)
            {
                var entry = Entries[i];

                // Unity sobrecarga == para que un Object destruido compare contra null.
                if (entry.Owner == null)
                {
                    Entries.RemoveAt(i);
                    continue;
                }

                var toCentre = entry.Centre - origin;
                float along = Vector3.Dot(toCentre, direction);

                float radius = entry.Radius;
                if (along < 0f && toCentre.sqrMagnitude > radius * radius) continue;

                float halfChordSquared = radius * radius
                                       - (toCentre.sqrMagnitude - along * along);
                if (halfChordSquared < 0f) continue;

                float t = along - Mathf.Sqrt(halfChordSquared);
                if (t < 0f) t = 0f;   // el origen está dentro del bulto
                if (t > maxDistance || t >= distance) continue;

                distance = t;
                found = true;
            }

            return found;
        }

        /// <summary>
        /// ¿Hay algún bulto que contenga ese punto? Es la pregunta que hace la prueba
        /// de juego a la posición final de la cámara.
        /// </summary>
        public static bool Contains(Vector3 point)
        {
            foreach (var entry in Entries)
            {
                if (entry.Owner == null) continue;
                if ((point - entry.Centre).sqrMagnitude <= entry.Radius * entry.Radius)
                    return true;
            }
            return false;
        }
    }
}
