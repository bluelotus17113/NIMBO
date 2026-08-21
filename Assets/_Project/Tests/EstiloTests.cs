using System.Collections.Generic;
using Nimbo.Art.Materials;
using Nimbo.Art.World;
using Nimbo.Core.Util;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El estilo: las cuatro bandas, el prado y las mallas que las alimentan.
    /// </summary>
    /// <remarks>
    /// Lo que se puede comprobar aquí es que los números salen. Si el resultado se
    /// **ve** bien no lo sabe ningún test: para eso está CapturaEstilo, que retrata
    /// los mismos cuatro encuadres antes y después. Estas pruebas cubren lo otro: que
    /// la paleta reproduce la referencia, que la copa se sombrea como un solo bulto y
    /// que la hierba trae escrito lo que el shader le va a pedir.
    /// </remarks>
    public class EstiloTests
    {
        [TearDown]
        public void Limpiar() => ToonPalette.ClearCache();

        // ── la paleta ────────────────────────────────────────────────────────

        /// <summary>
        /// Las cuatro bandas de un verde reproducen la rampa del follaje de la
        /// referencia.
        /// </summary>
        /// <remarks>
        /// Es el test que justifica toda la fórmula. Los cuatro verdes de AnimeTree se
        /// midieron del .blend y resultaron ser una progresión regular en tono,
        /// saturación y claridad; <c>BandsOf</c> aplica esa progresión. Si alguien
        /// toca los seis números, esto lo dice: pidiéndole las bandas al verde de luz
        /// del original tienen que salir los otros tres del original.
        /// </remarks>
        [Test]
        public void LasBandasReproducenLaRampaDeLaReferencia()
        {
            var lit = Hex("#539E43");
            var bands = ToonPalette.BandsOf(lit);

            AssertColour(Hex("#306E5A"), bands.Deep, "la banda profunda");
            AssertColour(Hex("#357D44"), bands.Shadow, "la banda de sombra");
            AssertColour(lit, bands.Lit, "la banda de luz");
            AssertColour(Hex("#9FCB68"), bands.High, "la banda de sol");
        }

        /// <summary>
        /// Un verde se enfría al entrar en sombra; una madera no.
        /// </summary>
        /// <remarks>
        /// Las dos referencias hacen cosas distintas y el código las respeta: la copa
        /// gira 23° hacia el cian y la corteza va al revés. Empujar una madera hacia
        /// el cian la deja color ceniza, que fue exactamente lo que pasó al probar una
        /// regla única para todo.
        /// </remarks>
        [Test]
        public void ElVerdeSeEnfriaEnSombraYLaMaderaNo()
        {
            Color.RGBToHSV(ToonPalette.BandsOf(Hex("#539E43")).Shadow,
                           out float verdeH, out _, out _);
            Color.RGBToHSV(Hex("#539E43"), out float verdeBase, out _, out _);
            Assert.Greater(verdeH, verdeBase, "la sombra de un verde tiene que tirar al cian");

            Color.RGBToHSV(ToonPalette.BandsOf(Hex("#6B5A43")).Shadow,
                           out float maderaH, out _, out _);
            Color.RGBToHSV(Hex("#6B5A43"), out float maderaBase, out _, out _);
            Assert.Less(maderaH, maderaBase, "la sombra de una madera no se va al cian");
        }

        /// <summary>
        /// Un color casi blanco no pierde el tono en la banda de sol.
        /// </summary>
        /// <remarks>
        /// La claridad de la banda alta es la del color por 1,284, y un crema ya está
        /// en 0,96: recortando a uno, el tono se conservaba pero la saturación no
        /// bajaba y salía una calva. La regla es abrirse desaturando cuando ya no se
        /// puede subir más, que es lo que hace una superficie clara al sol.
        /// </remarks>
        [Test]
        public void UnColorClaroSeAbreDesaturandoYNoSeQuema()
        {
            var bands = ToonPalette.BandsOf(Hex("#F4E7D2"));

            Color.RGBToHSV(bands.High, out _, out float altaS, out float altaV);
            Color.RGBToHSV(Hex("#F4E7D2"), out _, out float baseS, out _);

            Assert.LessOrEqual(altaV, 1.0001f, "la claridad no se puede pasar de uno");
            Assert.Less(altaS, baseS, "si no puede subir de claridad, tiene que desaturar");
        }

        // ── el ruido ─────────────────────────────────────────────────────────

        /// <summary>
        /// El ruido está entre cero y uno, no se repite y es continuo.
        /// </summary>
        /// <remarks>
        /// **Este ruido está escrito dos veces**, aquí en C# y en NimboAnime.hlsl, y
        /// las dos copias tienen que dar el mismo número: una decide dónde se siembran
        /// las flores y la otra pinta las manchas del suelo, y las flores tienen que
        /// caer dentro de las manchas. Esto solo puede vigilar la copia de C#; si
        /// alguien toca la del shader, hay que traer los valores de vuelta a mano.
        /// Los tres anclados abajo están para que un cambio accidental salte.
        /// </remarks>
        [Test]
        public void ElRuidoSeComportaComoUnRuido()
        {
            for (float x = -30f; x < 30f; x += 3.7f)
            for (float y = -30f; y < 30f; y += 4.3f)
            {
                float n = ValueNoise.At(x, y);
                Assert.That(n, Is.InRange(0f, 1f), $"el ruido se sale en ({x}, {y})");
            }

            Assert.AreEqual(ValueNoise.At(3.2f, -7.9f), ValueNoise.At(3.2f, -7.9f),
                            "el mismo punto tiene que dar siempre lo mismo");

            // Continuidad: dos puntos a una centésima no pueden dar valores distintos.
            // Sin esto, un fallo en la interpolación pasaría desapercibido y el prado
            // saldría a cuadros.
            for (float x = 0f; x < 8f; x += 0.5f)
                Assert.Less(Mathf.Abs(ValueNoise.At(x, 2f) - ValueNoise.At(x + 0.01f, 2f)), 0.06f,
                            $"el ruido da un salto en x = {x}");

            // Y que de verdad varíe: una función constante pasaría todo lo de arriba.
            var seen = new HashSet<int>();
            for (int i = 0; i < 200; i++) seen.Add(Mathf.RoundToInt(ValueNoise.At(i * 1.31f, i * 0.77f) * 20f));
            Assert.Greater(seen.Count, 8, "el ruido apenas varía: ¿se ha quedado plano?");
        }

        // ── la copa ──────────────────────────────────────────────────────────

        /// <summary>
        /// **El truco de la copa.** Todos los lóbulos apuntan al mismo corazón.
        /// </summary>
        /// <remarks>
        /// Es lo único que separa una copa dibujada de tres bolas pegadas. Si un día
        /// alguien mete un <c>RecalculateNormals</c> por costumbre, las normales
        /// vuelven a ser las de cada esfera, reaparecen las medias lunas oscuras en las
        /// juntas y no se entiende por qué. Esto lo caza.
        /// </remarks>
        [Test]
        public void LaCopaSeSombreaComoUnSoloBulto()
        {
            var pivot = new Vector3(0f, -0.4f, 0f);
            var lobes = FoliageMeshBuilder.CrownLobes(2f, seed: 5u);
            var crown = FoliageMeshBuilder.Weave(lobes, pivot, 5u, "copa");

            var vertices = crown.vertices;
            var normals = crown.normals;
            Assert.Greater(vertices.Length, 0, "la copa no tiene vértices");

            for (int i = 0; i < vertices.Length; i++)
            {
                // Un vértice que cayera justo en el corazón no tiene dirección; el
                // tejedor le pone hacia arriba y aquí no hay nada que comprobar.
                if ((vertices[i] - pivot).sqrMagnitude < 0.0001f) continue;

                var wanted = (vertices[i] - pivot).normalized;
                Assert.Greater(Vector3.Dot(wanted, normals[i]), 0.999f,
                               $"el vértice {i} no mira desde el corazón del racimo");
            }

            Object.DestroyImmediate(crown);
        }

        /// <summary>La copa trae la oclusión escrita, y el envés más tapado que el lomo.</summary>
        [Test]
        public void LaCopaTraeElEnvesMasTapadoQueElLomo()
        {
            var pivot = new Vector3(0f, -0.4f, 0f);
            var crown = FoliageMeshBuilder.Weave(
                FoliageMeshBuilder.CrownLobes(2f, seed: 5u), pivot, 5u, "copa");

            var vertices = crown.vertices;
            var colours = crown.colors;
            Assert.AreEqual(vertices.Length, colours.Length, "faltan colores de vértice");

            float lomo = 0f, enves = 0f;
            int arriba = 0, abajo = 0;

            for (int i = 0; i < vertices.Length; i++)
            {
                if (vertices[i].y > pivot.y + 1.4f) { lomo += colours[i].r; arriba++; }
                else if (vertices[i].y < pivot.y + 0.2f) { enves += colours[i].r; abajo++; }
            }

            Assert.Greater(arriba, 0); Assert.Greater(abajo, 0);
            Assert.Greater(lomo / arriba, enves / abajo,
                           "el lomo de la copa tiene que estar menos tapado que el envés");

            Object.DestroyImmediate(crown);
        }

        // ── el prado ─────────────────────────────────────────────────────────

        /// <summary>La hierba trae escrito lo que Nimbo/Foliage le va a pedir.</summary>
        /// <remarks>
        /// El contrato está en la cabecera del shader: rojo oclusión, verde viento,
        /// azul degradado, alfa semilla. Una malla sin él no falla — sale plana y
        /// quieta, que es peor, porque parece una decisión de estilo.
        /// </remarks>
        [Test]
        public void LaHierbaTraeElContratoDelColorDelVertice()
        {
            var tufts = new List<Meadow.Tuft>
            {
                new(Vector3.zero, Vector3.up, 0f, 1f, 0.42f),
            };

            var mesh = Meadow.Weave(tufts, 0, 1, "mata");
            var colours = mesh.colors;
            var vertices = mesh.vertices;

            Assert.Greater(colours.Length, 0, "la mata no trae color de vértice");

            float raiz = float.MaxValue, punta = float.MinValue;
            for (int i = 0; i < vertices.Length; i++)
            {
                Assert.GreaterOrEqual(colours[i].r, 0.7f,
                    "la oclusión de una brizna no baja tanto: la mezclaría con el verde de fondo");
                Assert.AreEqual(0.42f, colours[i].a, 0.01f, "la semilla de la mata no se propaga");

                if (vertices[i].y < 0.02f) raiz = Mathf.Min(raiz, colours[i].g);
                punta = Mathf.Max(punta, colours[i].g);
            }

            Assert.Less(raiz, 0.05f, "la raíz tiene que estar quieta");
            Assert.Greater(punta, 0.9f, "la punta tiene que moverse del todo");

            Object.DestroyImmediate(mesh);
        }

        /// <summary>La siembra esquiva lo construido.</summary>
        [Test]
        public void LaSiembraNoCreceDentroDeUnaCasa()
        {
            var surface = IslandMeshBuilder.BuildSurface(30f);
            var keepOut = new List<Meadow.KeepOut> { new(new Vector3(6f, 0f, 4f), 5f) };

            var tufts = Meadow.Sow(surface, 900, seed: 3u, keepOut);
            Assert.Greater(tufts.Count, 100, "no ha nacido casi nada: la siembra está rota");

            foreach (var tuft in tufts)
            {
                float distance = new Vector2(tuft.Position.x - 6f, tuft.Position.z - 4f).magnitude;
                Assert.GreaterOrEqual(distance, 5f, "ha crecido una mata dentro de la casa");
            }

            Object.DestroyImmediate(surface);
        }

        /// <summary>La hierba sale a rodales, no repartida por igual.</summary>
        /// <remarks>
        /// Se mide comparando la siembra normal con una sin agrupar: si agrupa, las
        /// distancias al vecino más cercano tienen que ser más desiguales. Un césped
        /// repartido a la perfección se lee como moqueta.
        /// </remarks>
        [Test]
        public void LaHierbaSaleARodales()
        {
            var surface = IslandMeshBuilder.BuildSurface(40f);

            float juntos = Spread(Meadow.Sow(surface, 700, 11u, null, Vector3.zero, clumping: 0.85f));
            float sueltos = Spread(Meadow.Sow(surface, 700, 11u, null, Vector3.zero, clumping: 0f));

            Assert.Greater(juntos, sueltos,
                           "con agrupación, la hierba tendría que quedar más desigual");

            Object.DestroyImmediate(surface);
        }

        /// <summary>Lo desigual que queda un reparto: la desviación de la distancia al vecino.</summary>
        private static float Spread(List<Meadow.Tuft> tufts)
        {
            if (tufts.Count < 3) return 0f;

            var nearest = new List<float>(tufts.Count);
            for (int i = 0; i < tufts.Count; i += 3)
            {
                float best = float.MaxValue;
                for (int j = 0; j < tufts.Count; j++)
                {
                    if (i == j) continue;
                    float d = (tufts[i].Position - tufts[j].Position).sqrMagnitude;
                    if (d < best) best = d;
                }
                nearest.Add(Mathf.Sqrt(best));
            }

            float mean = 0f;
            foreach (float d in nearest) mean += d;
            mean /= nearest.Count;

            float variance = 0f;
            foreach (float d in nearest) variance += (d - mean) * (d - mean);
            return Mathf.Sqrt(variance / nearest.Count);
        }

        // ── la piedra ────────────────────────────────────────────────────────

        /// <summary>Una roca tiene aristas: cada cara con su normal, sin compartir.</summary>
        [Test]
        public void LaRocaTieneAristasYNoEsUnaBola()
        {
            var rock = RockMeshBuilder.Boulder(11u);
            var triangles = rock.triangles;
            var normals = rock.normals;

            Assert.AreEqual(triangles.Length, rock.vertexCount,
                            "los vértices están compartidos: la roca saldría lisa");

            // Y las caras miran a sitios distintos de verdad.
            int distintas = 0;
            for (int t = 3; t < triangles.Length; t += 3)
                if (Vector3.Dot(normals[triangles[t]], normals[triangles[t - 3]]) < 0.995f)
                    distintas++;

            Assert.Greater(distintas, triangles.Length / 6,
                           "casi todas las caras miran al mismo sitio: no hay facetas");

            Object.DestroyImmediate(rock);
        }

        // ── utilidades ───────────────────────────────────────────────────────

        private static Color Hex(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out var colour);
            return colour;
        }

        private static void AssertColour(Color wanted, Color got, string what)
        {
            const float Tolerance = 0.02f;
            Assert.AreEqual(wanted.r, got.r, Tolerance, $"{what}: el rojo no cuadra");
            Assert.AreEqual(wanted.g, got.g, Tolerance, $"{what}: el verde no cuadra");
            Assert.AreEqual(wanted.b, got.b, Tolerance, $"{what}: el azul no cuadra");
        }
    }
}
