using System.Collections;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Simulation.Wardrobe;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la ropa equipada se vea en el muñeco **en la isla de verdad**.
    /// </summary>
    /// <remarks>
    /// <c>EquippedOutfit</c> se escribía desde el primer día y ninguna vista lo leía:
    /// regalar ropa cambiaba un dato invisible y el vecino seguía igual. La primera
    /// versión de estas pruebas comprobaba que el dato se guardaba, que es exactamente
    /// certificar el fallo que venían a arreglar. Aquí lo que se comprueba es la malla:
    /// que las piernas cambien de piel a ropa, que el sombrero cuelgue una pieza, y
    /// que todo eso aparezca solo, sin que nadie avise a la vista.
    /// </remarks>
    public class RopaEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado. Es el
            // mismo cuidado que tiene CronicaEnLaIslaTests.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        private static WardrobeService Armario()
        {
            Assert.IsTrue(ServiceRegistry.TryGet<WardrobeService>(out var armario),
                          "nadie registró el armario, así que en el juego no existe");
            return armario;
        }

        /// <summary>El primer vecino del censo que tenga muñeco en la isla.</summary>
        private static (IslanderData vecino, Art.Chibi.IslanderView vista) VecinoEnEscena()
        {
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(1), "la isla está vacía");

            foreach (var islander in censo.All)
            {
                foreach (var view in Object.FindObjectsByType<Art.Chibi.IslanderView>(
                             FindObjectsSortMode.None))
                {
                    if (view.IslanderId == islander.Id) return (islander, view);
                }
            }

            Assert.Fail("hay vecinos en el censo pero ninguno tiene muñeco en la isla");
            return (null, null);
        }

        private static Transform ParteDe(Art.Chibi.IslanderView vista, string nombre)
            => vista.transform.Find($"cuerpo/{nombre}");

        private static Mesh MallaDe(Transform parte)
            => parte == null ? null : parte.GetComponent<MeshFilter>().sharedMesh;

        [UnityTest]
        public IEnumerator PonerleUnPantalonAUnVecinoCambiaSuMalla()
        {
            yield return CargarYEmpezar();

            var armario = Armario();
            var (vecino, vista) = VecinoEnEscena();

            int pielAntes = MallaDe(ParteDe(vista, "piel")).vertexCount;
            int ropaAntes = MallaDe(ParteDe(vista, "ropa")).vertexCount;

            // Receive no devuelve nada: lo que se certifica es lo que deja escrito
            // y lo que la malla cuenta después.
            armario.Receive(vecino.Id, "cloth_pantalones_cargo_marrones");
            vista.RefreshOutfitIfChanged();

            Assert.That(vecino.EquippedOutfit, Is.EqualTo("cloth_pantalones_cargo_marrones"),
                        "el dato se cambió — esto solo ya lo probaban las pruebas viejas");

            int pielAhora = MallaDe(ParteDe(vista, "piel")).vertexCount;
            int ropaAhora = MallaDe(ParteDe(vista, "ropa")).vertexCount;

            Assert.That(pielAhora, Is.LessThan(pielAntes),
                "las piernas siguen siendo piel: el pantalón no llegó a la malla");
            Assert.That(ropaAhora, Is.GreaterThan(ropaAntes),
                "la ropa no creció: el pantalón no llegó a la malla");

            ColorUtility.TryParseHtmlString("#8B7355", out var marron);
            var material = ParteDe(vista, "ropa").GetComponent<MeshRenderer>().sharedMaterial;
            // El shader toon no tiene _Color: el base va por _BaseColor, y leer .color
            // además de fallar ensuciaría el registro con un error de shader.
            Assert.That(material.GetColor("_BaseColor"), Is.EqualTo(marron),
                        "la ropa sigue con su color de siempre: la paleta no llegó");
        }

        [UnityTest]
        public IEnumerator ElCambioDeRopaSeVeSoloSinQueNadieAvisaALaVista()
        {
            yield return CargarYEmpezar();

            var armario = Armario();
            var (vecino, vista) = VecinoEnEscena();

            var antes = MallaDe(ParteDe(vista, "ropa"));

            armario.Receive(vecino.Id, "cloth_vestido_de_algodon_celeste");

            // Nadie llama al refresco: la vista tiene que enterarse sola, como pasa
            // cuando el vecino decide cambiarse o llega un regalo con el juego corriendo.
            var ahora = antes;
            for (float t = 0f; t < 5f && ahora == antes; t += Time.deltaTime)
            {
                yield return null;
                ahora = MallaDe(ParteDe(vista, "ropa"));
            }

            Assert.AreNotEqual(antes, ahora,
                "cinco segundos después de equipar un vestido, la malla de la ropa " +
                "sigue siendo la misma: nadie está mirando el dato");
        }

        [UnityTest]
        public IEnumerator UnSombreroCuelgaUnaPiezaNuevaDelCuerpo()
        {
            yield return CargarYEmpezar();

            var armario = Armario();
            var (vecino, vista) = VecinoEnEscena();

            // Lo dejo sin prenda a propósito: así el color de la ropa es el de siempre
            // del vecino y se ve si el sombrero se lo roba.
            vecino.EquippedOutfit = "";
            vista.RefreshOutfitIfChanged();

            Assert.IsNull(ParteDe(vista, "prenda"),
                          "sin prenda equipada no puede haber pieza colgada");

            armario.Receive(vecino.Id, "cloth_gorra_nimbo_clasica");
            vista.RefreshOutfitIfChanged();

            var prenda = ParteDe(vista, "prenda");
            Assert.IsNotNull(prenda, "la gorra equipada no colgó ninguna pieza");

            var malla = prenda.GetComponent<MeshFilter>().sharedMesh;
            Assert.IsNotNull(malla);
            Assert.That(malla.vertexCount, Is.GreaterThan(0));

            // El defecto que esto fija: con el sombrero vistiendo el cuerpo, aquí se
            // leía el azul #3366AA de la gorra en el torso. Se relee el material de
            // la parte y no el capturado antes, porque SwapMesh puede colgar otro.
            var materialRopa = ParteDe(vista, "ropa").GetComponent<MeshRenderer>()
                                                     .sharedMaterial;
            ColorUtility.TryParseHtmlString("#3366AA", out var azulDeGorra);
            Assert.That(materialRopa.GetColor("_BaseColor"), Is.Not.EqualTo(azulDeGorra),
                        "la gorra tiñó la ropa del cuerpo: su color debía quedarse " +
                        "en la pieza");
        }

        [UnityTest]
        public IEnumerator UnAccesorioNoDejaAlVecinoSinRopa()
        {
            yield return CargarYEmpezar();

            var armario = Armario();
            var (vecino, vista) = VecinoEnEscena();

            var ropaAntes = MallaDe(ParteDe(vista, "ropa"));

            armario.Receive(vecino.Id, "cloth_pulsera_de_la_amistad");
            vista.RefreshOutfitIfChanged();

            var ropaAhora = MallaDe(ParteDe(vista, "ropa"));
            Assert.IsNotNull(ropaAhora,
                "equipar una pulsera no puede dejar al vecino sin malla de ropa");
            Assert.That(ropaAhora.vertexCount, Is.EqualTo(ropaAntes.vertexCount),
                        "un accesorio no cambia el cuerpo");
        }
    }
}
