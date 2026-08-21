using System.Collections.Generic;
using Nimbo.Core.Util;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Copas de árbol y matas: un racimo de lóbulos que se lee como **un** bulto.
    /// </summary>
    /// <remarks>
    /// La copa de antes eran tres esferas puestas juntas, y se veían las tres: cada
    /// una traía su propio sombreado y en las juntas salía una media luna oscura, esa
    /// marca de uña que se ve en la captura vieja. El problema no es que se corten:
    /// es que se **sombrean** por separado.
    ///
    /// Aquí las normales de todos los lóbulos se reescriben apuntando desde un único
    /// centro. Es lo que en la referencia hace un modificador de transferencia de
    /// datos desde una malla lisa auxiliar, y es la pieza que convierte un montón de
    /// bolas en una copa dibujada: la luz recorre el racimo entero de un lado a otro
    /// sin enterarse de dónde acaba una bola y empieza la siguiente. Lo que queda de
    /// las juntas es silueta, que es justo lo que sí queremos.
    ///
    /// La superficie se abolla con ruido para que el borde no sea un círculo. Un
    /// círculo perfecto se lee como una pelota; abollado se lee como hojas.
    ///
    /// Lo que se escribe en el color del vértice es el contrato de Nimbo/Foliage:
    /// rojo la oclusión, verde el viento, azul el degradado, alfa la semilla.
    /// </remarks>
    public static class FoliageMeshBuilder
    {
        /// <summary>Un lóbulo del racimo: dónde está y cómo de gordo.</summary>
        public readonly struct Lobe
        {
            public readonly Vector3 Centre;
            public readonly Vector3 Radii;
            public Lobe(Vector3 centre, Vector3 radii) { Centre = centre; Radii = radii; }
        }

        /// <summary>
        /// Teje el racimo en una malla.
        /// </summary>
        /// <param name="lobes">Los bultos, en coordenadas locales.</param>
        /// <param name="pivot">
        /// De dónde salen las normales. Es el corazón del bulto, no su centro
        /// geométrico: puesto un poco más abajo, la copa se ilumina como si la luz
        /// entrara por arriba y el envés queda en sombra, que es como se dibuja.
        /// </param>
        public static Mesh Weave(IReadOnlyList<Lobe> lobes, Vector3 pivot, uint seed,
                                 string name, int segments = 18, int rings = 12,
                                 float roughness = 0.16f)
        {
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var colours = new List<Color>();
            var uv = new List<Vector2>();
            var indices = new List<int>();

            var rng = new Rng(seed);
            float treeSeed = rng.NextFloat();

            // El alcance del racimo, para normalizar la oclusión y el viento. De la
            // malla y no escrito a mano: una mata y el Árbol Nimbo usan esto mismo.
            float reach = 0.001f;
            for (int i = 0; i < lobes.Count; i++)
                reach = Mathf.Max(reach, (lobes[i].Centre - pivot).magnitude
                                       + Mathf.Max(lobes[i].Radii.x,
                                         Mathf.Max(lobes[i].Radii.y, lobes[i].Radii.z)));

            for (int l = 0; l < lobes.Count; l++)
            {
                var lobe = lobes[l];
                float wobbleSeed = l * 37.13f + seed * 0.017f;
                int start = vertices.Count;

                for (int ring = 0; ring <= rings; ring++)
                {
                    float phi = Mathf.PI * ring / rings;
                    float sinPhi = Mathf.Sin(phi), cosPhi = Mathf.Cos(phi);

                    for (int seg = 0; seg <= segments; seg++)
                    {
                        float theta = Mathf.PI * 2f * seg / segments;
                        var dir = new Vector3(sinPhi * Mathf.Cos(theta), cosPhi,
                                              sinPhi * Mathf.Sin(theta));

                        // Abolladura: dos frecuencias sobre la propia dirección, así
                        // que los dos lados de la costura del cilindro coinciden.
                        float lumps = Wobble(dir, 2.3f, wobbleSeed) * 0.68f
                                    + Wobble(dir, 5.7f, wobbleSeed + 11f) * 0.32f;
                        float swell = 1f + (lumps - 0.5f) * 2f * roughness;

                        var position = lobe.Centre + Vector3.Scale(dir * swell, lobe.Radii);

                        // La normal, desde el corazón del racimo. Esta línea es todo
                        // el truco.
                        var normal = (position - pivot).normalized;
                        if (normal.sqrMagnitude < 0.5f) normal = Vector3.up;

                        float out01 = Mathf.Clamp01((position - pivot).magnitude / reach);
                        float up01 = Mathf.Clamp01((position.y - pivot.y) / reach * 0.5f + 0.5f);

                        // Oclusión: el envés y el corazón del racimo están tapados por
                        // el resto de la copa. Es lo que en la referencia sale de un
                        // nodo de oclusión ambiental multiplicando el color.
                        //
                        // Nunca baja de 0,58: la oclusión mezcla con la banda profunda,
                        // que es el verde azulado más oscuro de la paleta, y con el
                        // suelo bajo la copa entera se iba a ese verde en vez de solo
                        // el envés. Y pesa la altura, porque la copa ya no recibe
                        // sombras proyectadas —ver la nota de GatheringView— así que
                        // el envés oscuro tiene que salir de aquí.
                        float ao = Mathf.Clamp01(0.58f + out01 * 0.18f + up01 * 0.34f);

                        vertices.Add(position);
                        normals.Add(normal);
                        colours.Add(new Color(ao, out01 * up01, Mathf.Lerp(0.45f, 1f, up01),
                                              treeSeed));
                        uv.Add(new Vector2((float)seg / segments, (float)ring / rings));
                    }
                }

                for (int ring = 0; ring < rings; ring++)
                for (int seg = 0; seg < segments; seg++)
                {
                    int a = start + ring * (segments + 1) + seg;
                    int b = a + segments + 1;

                    indices.Add(a); indices.Add(b); indices.Add(a + 1);
                    indices.Add(a + 1); indices.Add(b); indices.Add(b + 1);
                }
            }

            var mesh = new Mesh { name = name };
            if (vertices.Count > 65000)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetColors(colours);
            mesh.SetUVs(0, uv);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>Ruido de valor sobre una dirección, para abollar la esfera.</summary>
        private static float Wobble(Vector3 dir, float frequency, float offset)
        {
            // Dos planos del ruido 2D combinados: sale bastante isótropo y evita
            // tener que escribir un ruido 3D entero para hacer bultos.
            float a = ValueNoise.At(dir.x * frequency + offset, dir.z * frequency - offset);
            float b = ValueNoise.At(dir.y * frequency - offset, dir.x * frequency + offset * 0.5f);
            return (a + b) * 0.5f;
        }

        /// <summary>
        /// El racimo de una copa de árbol: un lóbulo grande y varios colgando.
        /// </summary>
        /// <remarks>
        /// Se solapan mucho a propósito. Separados se ven las bolas; metidos unos
        /// dentro de otros, lo que queda fuera es una silueta de bultos redondos, que
        /// es la silueta de una copa dibujada.
        /// </remarks>
        public static List<Lobe> CrownLobes(float radius, uint seed, int count = 6)
        {
            var rng = new Rng(seed);
            var lobes = new List<Lobe>(count)
            {
                new(new Vector3(0f, radius * 0.16f, 0f),
                    new Vector3(radius, radius * 0.82f, radius)),
            };

            for (int i = 1; i < count; i++)
            {
                float angle = (i - 1) * (Mathf.PI * 2f / (count - 1)) + rng.Range(-0.4f, 0.4f);
                float distance = radius * rng.Range(0.52f, 0.78f);
                float size = radius * rng.Range(0.48f, 0.70f);

                lobes.Add(new Lobe(
                    new Vector3(Mathf.Cos(angle) * distance,
                                radius * rng.Range(-0.24f, 0.30f),
                                Mathf.Sin(angle) * distance),
                    new Vector3(size, size * rng.Range(0.78f, 0.98f), size)));
            }

            return lobes;
        }
    }
}
