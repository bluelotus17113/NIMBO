using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Nimbo.Art.Materials;
using Nimbo.Art.World;
using Nimbo.Farming;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La costura entre el catálogo de cultivos y sus siluetas en FarmView.
    /// </summary>
    /// <remarks>
    /// La enfermedad que esta prueba vigila es la de siempre: un id escrito a mano
    /// en un switch que nada comprueba. Si mañana se añade un cultivo al JSON y
    /// nadie le da forma, caería en la planta genérica sin excepción y sin aviso:
    /// aquí se delata. Fija también que las siluetas del catálogo no colapsen una
    /// en otra —ni por nombre de piezas ni por número de vértices—, que es el fallo
    /// que doce funciones gemelas invitan a cometer. Que el resultado **se** vea
    /// bien no lo sabe ningún test: para eso está CapturaHuerto, que retrata las
    /// doce maduras en la escena Isla.
    ///
    /// <c>LookFor</c> y <c>CropLook</code> son privados y se llegan por reflejo,
    /// como <c>SimulateSingleClick</c> en TiendasEnLaIslaTests.
    /// </remarks>
    public class SiluetaDeCultivosTests
    {
        private CropCatalog _catalogo;
        private MethodInfo _lookFor;

        // LookFor construye mallas y materiales nuevos en cada llamada; se recogen
        // para destruirlos al acabar y no dejar basura en la sesión del editor.
        private readonly List<Mesh> _mallas = new();

        [SetUp]
        public void Preparar()
        {
            _catalogo = new CropCatalog();
            _lookFor = typeof(FarmView).GetMethod("LookFor",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(_lookFor, "FarmView.LookFor ya no existe o dejó de ser estática");
        }

        [TearDown]
        public void Limpiar()
        {
            foreach (var malla in _mallas)
                if (malla != null) Object.DestroyImmediate(malla);
            _mallas.Clear();

            var looks = (System.Collections.IDictionary)typeof(FarmView)
                .GetField("Looks", BindingFlags.NonPublic | BindingFlags.Static)
                .GetValue(null);
            looks.Clear();

            ToonPalette.ClearCache();
        }

        /// <summary>
        /// Cada cultivo del catálogo real resuelve a una silueta propia: ninguna
        /// cae en la planta genérica y ninguna se parece a otra.
        /// </summary>
        [Test]
        public void TodoCultivoDelCatalogoTieneSuPropiaSilueta()
        {
            Assert.GreaterOrEqual(_catalogo.Count, 12,
                "el catálogo perdió cultivos: revisar esta suite y CapturaHuerto");

            // Un id fuera del catálogo es lo único que legítimamente cae en la
            // genérica; su firma es la que no debe salir ninguna semilla real.
            var generica = Medir("seed_que_no_existe");

            var medidas = new List<(string id, string nombres, int vertices)>();
            foreach (var crop in _catalogo.Crops)
            {
                var medida = Medir(crop.SeedId);

                Assert.AreNotEqual(generica.nombres, medida.nombres,
                    $"{crop.SeedId} cayó en la planta genérica: le falta forma asignada");
                Assert.IsTrue(medida.seNota,
                    $"{crop.SeedId}: madurar no se ve — ni añade pieza ni cambia color");

                medidas.Add((crop.SeedId, medida.nombres, medida.vertices));
            }

            for (int i = 0; i < medidas.Count; i++)
                for (int j = i + 1; j < medidas.Count; j++)
                {
                    Assert.AreNotEqual(medidas[i].nombres, medidas[j].nombres,
                        $"{medidas[i].id} y {medidas[j].id} comparten nombres de pieza: " +
                        "misma silueta por copia");
                    Assert.AreNotEqual(medidas[i].vertices, medidas[j].vertices,
                        $"{medidas[i].id} y {medidas[j].id} suman los mismos vértices " +
                        $"({medidas[i].vertices}): misma silueta por geometría");
                }
        }

        /// <summary>
        /// Un id desconocido cae en la genérica en vez de en un hueco o una
        /// excepción: partidas viejas incluidas, el huerto siempre se dibuja.
        /// </summary>
        [Test]
        public void UnIdDesconocidoCaeEnLaPlantaGenerica()
        {
            var medida = Medir("seed_que_no_existe");

            Assert.AreEqual(1, medida.creciendo, "la genérica no es ya un tallo solo");
            Assert.AreEqual(2, medida.maduro, "la genérica no añade fruto al madurar");
        }

        // ── reflejo ──────────────────────────────────────────────────────────

        private (string nombres, int vertices, int creciendo, int maduro, bool seNota)
            Medir(string seedId)
        {
            var look = _lookFor.Invoke(null, new object[] { seedId });
            Assert.IsNotNull(look, $"{seedId}: LookFor devolvió null");

            var tipo = look.GetType();
            var creciendo = Piezas(tipo.GetField("Growing").GetValue(look));
            var maduro = Piezas(tipo.GetField("Ready").GetValue(look));
            _mallas.AddRange(creciendo.Select(p => p.malla));
            _mallas.AddRange(maduro.Select(p => p.malla));

            // Madurar se nota con una pieza nueva o con un material que cambia
            // (la nimbocalabaza es la misma malla con otra piel: la planta ES el
            // fruto y lo que avisa de la cosecha es el color).
            var seNota = maduro.Count > creciendo.Count ||
                         creciendo.Zip(maduro, (a, b) => (a, b))
                             .Any(par => par.a.malla != par.b.malla ||
                                         par.a.material != par.b.material);

            return (
                string.Join(",", creciendo.Select(p => p.nombre).OrderBy(n => n)),
                creciendo.Sum(p => p.malla.vertexCount),
                creciendo.Count,
                maduro.Count,
                seNota);
        }

        private static List<(string nombre, Mesh malla, Material material)> Piezas(object array)
        {
            // Part es un struct: su array no castea a object[], pero sí a Array,
            // que al iterar entrega cada elemento ya encajonado.
            var lista = new List<(string, Mesh, Material)>();
            foreach (var pieza in (System.Array)array)
            {
                var tipo = pieza.GetType();
                string nombre = (string)tipo.GetField("Name").GetValue(pieza);
                var malla = (Mesh)tipo.GetField("Mesh").GetValue(pieza);
                var material = (Material)tipo.GetField("Material").GetValue(pieza);
                Assert.IsNotNull(malla, $"pieza '{nombre}' sin malla");
                Assert.IsNotNull(material, $"pieza '{nombre}' sin material");
                lista.Add((nombre, malla, material));
            }
            return lista;
        }
    }
}
