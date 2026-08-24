using System.Collections;
using Nimbo.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que los atajos del HUD lleven a donde prometen **en el juego montado**.
    /// </summary>
    /// <remarks>
    /// El badge de peticiones llevaba años informando —cambiaba de color, contaba—
    /// sin abrir nada. Que la clase tenga un callback no significa nada: aquí se
    /// carga la isla, se pulsa el badge como lo pulsa el jugador y se comprueba que
    /// el tablón está en pantalla.
    ///
    /// Se envía un <see cref="ClickEvent"/> sintético y no una llamada al manejador:
    /// lo que interesa es que el elemento que hay montado reciba el gesto, no que el
    /// método funcione si alguien consigue llegar hasta él.
    /// </remarks>
    public class HudEnLaIslaTests
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

        [UnityTest]
        public IEnumerator PulsarElBadgeDePeticionesAbreElTablon()
        {
            yield return CargarYEmpezar();

            var badge = Buscar("hud-peticiones");
            Assert.That(badge, Is.Not.Null,
                "el HUD no tiene badge de peticiones: no hay atajo que probar");

            var tablon = Buscar("tablon-encargos");
            Assert.That(tablon, Is.Not.Null,
                "el tablón no está montado: el badge abriría una ventana que no existe");
            Assert.That(EstaAbierto(tablon), Is.False,
                "el tablón arranca abierto: la prueba no puede atribuirle al clic lo que ya estaba");

            Pulsar(badge);
            yield return null;

            Assert.That(EstaAbierto(tablon), Is.True,
                "se pulsó el badge y el tablón siguió cerrado: el atajo es decorado");
        }

        [UnityTest]
        public IEnumerator VolverAPulsarElBadgeCierraElTablon()
        {
            yield return CargarYEmpezar();

            var badge = Buscar("hud-peticiones");
            var tablon = Buscar("tablon-encargos");
            Assert.That(badge, Is.Not.Null);
            Assert.That(tablon, Is.Not.Null);

            Pulsar(badge);
            yield return null;
            Pulsar(badge);
            yield return null;

            Assert.That(EstaAbierto(tablon), Is.False,
                "el badge solo abre: con el tablón delante, el mismo sitio tiene que servir para cerrarlo");
        }

        // ── ayudas ───────────────────────────────────────────────────────────

        /// <summary>Busca un elemento por nombre en cualquier documento de la escena.</summary>
        private static VisualElement Buscar(string name)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                var found = document.rootVisualElement?.Q(name);
                if (found != null) return found;
            }

            return null;
        }

        private static bool EstaAbierto(VisualElement panel) =>
            panel.style.display == DisplayStyle.Flex;

        /// <summary>Un clic sintético sobre el elemento, como el gesto del jugador.</summary>
        private static void Pulsar(VisualElement element)
        {
            using (var evt = ClickEvent.GetPooled())
            {
                evt.target = element;
                element.SendEvent(evt);
            }
        }
    }
}
