using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>Un bulto que hay que rodear al andar: un edificio, el Árbol Nimbo.</summary>
    public readonly struct Obstacle
    {
        public readonly Vector2 Centre;
        public readonly float Radius;

        public Obstacle(Vector3 centre, float radius)
        {
            Centre = new Vector2(centre.x, centre.z);
            Radius = radius;
        }
    }

    /// <summary>
    /// Hace que los vecinos rodeen los edificios en vez de atravesarlos.
    /// </summary>
    /// <remarks>
    /// No es un NavMesh, y es a propósito. La isla se genera entera en tiempo de
    /// ejecución, así que un NavMesh habría que hornearlo en caliente: paquete nuevo,
    /// superficie, y volver a hornear cada vez que se abre una zona. Para diez bultos
    /// redondos en un prado llano, eso es un cañón para matar moscas.
    ///
    /// Lo que hace es lo mínimo que se nota: si el siguiente paso te mete dentro de un
    /// bulto, te deslizas por su borde hacia el lado que te acerca al destino. No
    /// encuentra el camino óptimo ni sale de un callejón —aquí no hay callejones—,
    /// pero quita lo único que se veía mal, que era gente cruzando por dentro de la
    /// tienda.
    /// </remarks>
    public static class WalkAround
    {
        /// <summary>Margen que se deja al rodear, para no ir rozando la pared.</summary>
        private const float Clearance = 0.8f;

        /// <summary>
        /// La dirección en la que conviene dar el siguiente paso.
        /// </summary>
        /// <param name="from">Dónde está.</param>
        /// <param name="to">A dónde va.</param>
        /// <param name="obstacles">Los bultos de la isla.</param>
        /// <returns>Dirección normalizada en el plano, o cero si ya ha llegado.</returns>
        public static Vector3 Steer(Vector3 from, Vector3 to, IReadOnlyList<Obstacle> obstacles)
        {
            var here = new Vector2(from.x, from.z);
            var there = new Vector2(to.x, to.z);

            var direct = there - here;
            if (direct.sqrMagnitude < 0.0001f) return Vector3.zero;
            direct.Normalize();

            if (obstacles == null) return new Vector3(direct.x, 0f, direct.y);

            for (int i = 0; i < obstacles.Count; i++)
            {
                var obstacle = obstacles[i];
                float radius = obstacle.Radius + Clearance;

                var toCentre = obstacle.Centre - here;
                float distance = toCentre.magnitude;

                // Ya dentro del bulto: lo primero es salir, y en línea recta hacia
                // fuera. Pasa cuando se abre una zona encima de alguien que estaba ahí.
                if (distance < 0.001f) continue;
                if (distance < radius)
                {
                    var out2 = -toCentre / distance;
                    return new Vector3(out2.x, 0f, out2.y);
                }

                // Si el destino está más cerca que el bulto, no estorba: se llega antes.
                if ((there - here).magnitude < distance - radius) continue;

                // ¿El camino recto pasa por dentro? Se mide la distancia del centro a
                // la recta. Si es mayor que el radio, este bulto no molesta.
                float along = Vector2.Dot(toCentre, direct);
                if (along <= 0f) continue;

                float perpendicular = (toCentre - direct * along).magnitude;
                if (perpendicular >= radius) continue;

                // Molesta: se anda por la tangente, hacia el lado que deja el destino
                // más cerca. Elegir siempre el mismo lado hace que la mitad de la
                // gente dé la vuelta entera al edificio.
                var tangentA = new Vector2(-direct.y, direct.x);
                var tangentB = -tangentA;

                var chosen = Vector2.Dot(there - here, tangentA) >= 0f ? tangentA : tangentB;

                // Mezclado con la dirección recta: solo tangente daría vueltas
                // alrededor del bulto sin acercarse nunca.
                var blended = (chosen * 0.75f + direct * 0.25f).normalized;
                return new Vector3(blended.x, 0f, blended.y);
            }

            return new Vector3(direct.x, 0f, direct.y);
        }
    }
}
