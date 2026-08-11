using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Los peinados. Una prueba no puede decir si un peinado es bonito, pero sí puede
    /// decir que son cuarenta peinados y no veinte repetidos dos veces.
    /// </summary>
    /// <remarks>
    /// Es justo lo que fallaba antes: los estilos se deducían del número con
    /// condiciones sueltas y varios daban exactamente la misma malla. Sobre el papel
    /// había veinte peinados; en pantalla, bastantes menos, y nadie lo veía porque
    /// ningún test miraba la geometría.
    /// </remarks>
    public class HairStyleTests
    {
        private static AppearanceData WithStyle(int style)
        {
            var appearance = AppearanceData.Default;
            appearance.HairStyle = style;
            return appearance;
        }

        /// <summary>
        /// Una huella de la malla, sobre los vértices de verdad.
        /// </summary>
        /// <remarks>
        /// La primera versión usaba el número de vértices y la caja envolvente, y
        /// daba falsos positivos: dos peinados que solo se diferencian en el
        /// flequillo tienen la misma cuenta de vértices y la misma caja, porque el
        /// flequillo cae dentro de la silueta que marcan la melena y las trenzas.
        /// Se veían como iguales dos peinados que en pantalla no lo son.
        /// </remarks>
        private static string Fingerprint(Mesh mesh)
        {
            var vertices = mesh.vertices;
            unchecked
            {
                uint hash = 2166136261u;
                for (int i = 0; i < vertices.Length; i++)
                {
                    // Redondeado a milímetros: dos mallas iguales generadas por
                    // separado pueden diferir en el último bit de un float, y eso no
                    // es un peinado distinto.
                    hash = Mix(hash, Mathf.RoundToInt(vertices[i].x * 1000f));
                    hash = Mix(hash, Mathf.RoundToInt(vertices[i].y * 1000f));
                    hash = Mix(hash, Mathf.RoundToInt(vertices[i].z * 1000f));
                }
                return $"{vertices.Length}:{hash:x8}";
            }
        }

        private static uint Mix(uint hash, int value)
        {
            unchecked
            {
                hash ^= (uint)value;
                return hash * 16777619u;
            }
        }

        [Test]
        public void LosCuarentaPeinadosSonDistintos()
        {
            var seen = new Dictionary<string, int>();

            for (int style = 0; style < HairStyles.Count; style++)
            {
                var meshes = ChibiMeshBuilder.Build(WithStyle(style));
                string print = Fingerprint(meshes.Hair);

                Assert.IsFalse(seen.ContainsKey(print),
                    $"«{HairStyles.NameOf(style)}» ({style}) sale idéntico a " +
                    $"«{(seen.TryGetValue(print, out int other) ? HairStyles.NameOf(other) : "")}» ({other})");

                seen[print] = style;
                Discard(meshes);
            }

            Assert.AreEqual(HairStyles.Count, seen.Count);
        }

        [Test]
        public void SonCuarenta_TreintaYDosDeSalida()
        {
            // Los números del GDD. Si alguien añade peinados y se pasa, el creador
            // de personajes empezaría a ofrecer los que hay que ganarse.
            Assert.AreEqual(40, HairStyles.Count, "el diseño pide 32 + 8");
            Assert.AreEqual(32, HairStyles.BaseCount);
        }

        [Test]
        public void LosDeSalidaEstanAbiertosDesdeElPrimerDia()
        {
            for (int style = 0; style < HairStyles.BaseCount; style++)
                Assert.AreEqual(1, HairStyles.Get(style).UnlockLevel,
                    $"«{HairStyles.NameOf(style)}» está entre los de salida y pide nivel");

            for (int style = HairStyles.BaseCount; style < HairStyles.Count; style++)
                Assert.Greater(HairStyles.Get(style).UnlockLevel, 1,
                    $"«{HairStyles.NameOf(style)}» debería ganarse y está abierto");
        }

        [Test]
        public void ConLaIslaAlMaximoEstanTodos()
        {
            Assert.AreEqual(HairStyles.BaseCount, HairStyles.AvailableAt(1));
            Assert.AreEqual(HairStyles.Count, HairStyles.AvailableAt(10));
        }

        [Test]
        public void RecorrerLoAbiertoNoDejaHuecos()
        {
            // StyleAt existe para que el deslizador del creador recorra lo abierto sin
            // saltos. Si devolviera índices con huecos, el jugador vería peinados que
            // no puede elegir.
            const int level = 5;
            int available = HairStyles.AvailableAt(level);

            var seen = new HashSet<int>();
            for (int ordinal = 0; ordinal < available; ordinal++)
            {
                int style = HairStyles.StyleAt(ordinal, level);
                Assert.IsTrue(seen.Add(style), $"el estilo {style} sale dos veces");
                Assert.LessOrEqual(HairStyles.Get(style).UnlockLevel, level,
                    $"«{HairStyles.NameOf(style)}» no debería estar abierto en nivel {level}");
            }

            Assert.AreEqual(available, seen.Count);
        }

        [Test]
        public void UnEstiloFueraDeRangoNoRompe()
        {
            // El número de peinado va dentro del guardado. Una partida de una versión
            // con más peinados no puede tirar el juego al abrirla.
            Assert.DoesNotThrow(() => HairStyles.Get(9999));
            Assert.DoesNotThrow(() => HairStyles.Get(-1));

            var meshes = ChibiMeshBuilder.Build(WithStyle(9999));
            Assert.Greater(meshes.Hair.vertexCount, 0);
            Discard(meshes);
        }

        private static void Discard(ChibiMeshes meshes)
        {
            Object.DestroyImmediate(meshes.Hair);
            Object.DestroyImmediate(meshes.Skin);
            Object.DestroyImmediate(meshes.Clothes);
            Object.DestroyImmediate(meshes.Face);
        }
    }
}
