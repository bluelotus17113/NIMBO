using System.Collections;
using Nimbo.Art.Materials;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// El lavado de cara, en la isla de verdad.
    /// </summary>
    /// <remarks>
    /// Las pruebas de editor comprueban que la paleta y las mallas salen bien. Esta
    /// comprueba lo otro, que es donde este proyecto se ha caído más veces: que lo que
    /// sale bien **está puesto**. Un prado sembrado que nadie monta, un shader que
    /// nadie asigna o unas bandas que se quedan en el color por defecto no fallan
    /// ningún test de lógica; simplemente el juego se ve como antes.
    /// </remarks>
    public class EstiloEnLaIslaTests
    {
        [UnityTest]
        public IEnumerator LaIslaTienePradoDeVerdad()
        {
            yield return Aldea.Cargar();

            var prado = GameObject.Find("Prado");
            Assert.That(prado, Is.Not.Null,
                        "nadie ha sembrado la isla: el prado vuelve a ser una alfombra lisa");

            int trozos = 0, briznas = 0;
            foreach (var filtro in prado.GetComponentsInChildren<MeshFilter>())
            {
                if (!filtro.name.StartsWith("hierba")) continue;
                trozos++;
                briznas += filtro.sharedMesh.vertexCount;
            }

            Assert.That(trozos, Is.GreaterThan(4),
                        "el prado va en un solo trozo: no se puede descartar por partes");
            Assert.That(briznas, Is.GreaterThan(20000), "hay cuatro briznas contadas");
        }

        /// <summary>
        /// La hierba lleva el shader de vegetación, no el de todo lo demás.
        /// </summary>
        /// <remarks>
        /// Si Nimbo/Foliage no se encuentra, <c>ToonPalette</c> devuelve el de la isla
        /// como paracaídas: la hierba se sigue viendo y del color correcto, pero
        /// quieta y por una sola cara. Es un fallo que no se ve en una captura fija y
        /// sí se nota jugando, así que hay que preguntarlo aquí.
        /// </remarks>
        [UnityTest]
        public IEnumerator LaHierbaSeMueveConElViento()
        {
            yield return Aldea.Cargar();

            var prado = GameObject.Find("Prado");
            Assert.That(prado, Is.Not.Null, "no hay prado");

            foreach (var renderer in prado.GetComponentsInChildren<MeshRenderer>())
            {
                if (!renderer.name.StartsWith("hierba")) continue;

                Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Nimbo/Foliage"),
                            $"{renderer.name} no lleva el shader de vegetación: no habrá viento");
                Assert.That(renderer.sharedMaterial.GetFloat("_WindStrength"), Is.GreaterThan(0f),
                            $"{renderer.name} lleva el viento a cero");
                Assert.That(renderer.shadowCastingMode,
                            Is.EqualTo(UnityEngine.Rendering.ShadowCastingMode.Off),
                            "el prado proyecta sombra: es el doble de coste para nada");
                Assert.That(renderer.receiveShadows, Is.True,
                            "el prado no recibe sombras: la del árbol se quedaría en el suelo");
                yield break;
            }

            Assert.Fail("no hay ni un trozo de hierba que mirar");
        }

        /// <summary>El suelo y la hierba comparten la mancha.</summary>
        /// <remarks>
        /// Es el punto 3 de la lámina de referencia: «terrain — synchronized with
        /// grass». Si el suelo y las briznas pintan sus manchas a escalas distintas,
        /// una zona sale fría en el suelo y cálida en la hierba, y las briznas se ven
        /// recortadas contra el terreno en vez de salir de él. Pasó, y en la captura
        /// las briznas salían blanquecinas sobre verde oscuro.
        /// </remarks>
        [UnityTest]
        public IEnumerator ElSueloYLaHierbaSeManchanIgual()
        {
            yield return Aldea.Cargar();

            Material suelo = null, hierba = null;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
            {
                if (suelo == null && renderer.name == "prado") suelo = renderer.sharedMaterial;
                if (hierba == null && renderer.name.StartsWith("hierba")) hierba = renderer.sharedMaterial;
            }

            Assert.That(suelo, Is.Not.Null, "no hay superficie de isla");
            Assert.That(hierba, Is.Not.Null, "no hay hierba");

            Assert.That(hierba.GetFloat("_VariationScale"),
                        Is.EqualTo(suelo.GetFloat("_VariationScale")).Within(0.01f),
                        "el suelo y la hierba pintan la mancha a escalas distintas");
        }

        /// <summary>El suelo lleva escritas las cuatro bandas, no las de fábrica.</summary>
        [UnityTest]
        public IEnumerator ElSueloLlevaLasBandasDelVerdeDeLaIsla()
        {
            yield return Aldea.Cargar();

            Material suelo = null;
            foreach (var renderer in Object.FindObjectsByType<MeshRenderer>(FindObjectsSortMode.None))
                if (renderer.name == "prado") { suelo = renderer.sharedMaterial; break; }

            Assert.That(suelo, Is.Not.Null, "no hay superficie de isla");
            Assert.That(suelo.shader.name, Is.EqualTo("Nimbo/Toon"), "el prado no lleva el shader de la isla");

            var bands = ToonPalette.BandsOf(ToonPalette.Grass);
            Cerca(bands.Lit, suelo.GetColor("_BandLit"), "la banda de luz");
            Cerca(bands.Shadow, suelo.GetColor("_BandShadow"), "la banda de sombra");
            Cerca(bands.Deep, suelo.GetColor("_BandDeep"), "la banda profunda");
        }

        /// <summary>La copa del Árbol Nimbo no recibe sombras proyectadas.</summary>
        /// <remarks>
        /// Con ellas puestas, la copa se hacía sombra a sí misma y salía cruzada por
        /// una escalera de dientes de sierra a la resolución del mapa de sombras. El
        /// envés lo oscurece ahora la oclusión del vértice. Ver GatheringView.AddPart.
        /// </remarks>
        [UnityTest]
        public IEnumerator LaCopaDelArbolNoSeHaceSombraASiMisma()
        {
            yield return Aldea.Cargar();

            var arbol = GameObject.Find("Árbol Nimbo");
            Assert.That(arbol, Is.Not.Null, "no está el Árbol Nimbo");

            var copa = arbol.transform.Find("copa");
            Assert.That(copa, Is.Not.Null, "el Árbol Nimbo no tiene copa");

            var renderer = copa.GetComponent<MeshRenderer>();
            Assert.That(renderer.sharedMaterial.shader.name, Is.EqualTo("Nimbo/Foliage"),
                        "la copa no lleva el shader de vegetación");
            Assert.That(renderer.receiveShadows, Is.False,
                        "la copa recibe sombras: volverán los dientes de sierra");
        }

        /// <summary>
        /// Ese color está escrito en el material, en el espacio que sea.
        /// </summary>
        /// <remarks>
        /// Se acepta el valor en gama o en lineal a propósito. Un color que se manda a
        /// un material puede convertirse por el camino según el espacio de color del
        /// proyecto, y **eso no es lo que esta prueba quiere vigilar**: lo que quiere
        /// saber es si la banda se escribió a partir del verde de la isla o se quedó
        /// con el valor de fábrica del shader, y esos dos se distinguen en los dos
        /// espacios. Atar la prueba a una conversión concreta sería atarla a un ajuste
        /// del proyecto que no tiene nada que ver con el estilo.
        /// </remarks>
        private static void Cerca(Color wanted, Color got, string what)
        {
            if (Coincide(wanted, got) || Coincide(wanted.linear, got)) return;
            Assert.Fail($"{what}: el material lleva {got}, y del verde de la isla " +
                        $"sale {wanted}. ¿Se han escrito las bandas?");
        }

        private static bool Coincide(Color a, Color b) =>
            Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f
                                         && Mathf.Abs(a.b - b.b) < 0.02f;
    }
}
