using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Nimbo.Core.Events;
using Nimbo.UI;
using Nimbo.UI.Menu;
using Nimbo.UI.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que Escape exista **en el juego**: que cierre lo abierto y que no le robe
    /// la tecla al menú de pausa.
    /// </summary>
    /// <remarks>
    /// La auditoría lo midió: ESC no cerraba ningún panel de juego y el botón
    /// «Cerrar» estaba en cuatro sitios distintos — o no estaba, como en la ficha
    /// de habitante. Aquí se carga la isla de verdad y se pulsa Escape por el
    /// camino que recorre en una partida: <c>MainMenuView.HandleEscape</c>, que es
    /// quien lee la tecla en <c>Update</c>. Con el sistema de entrada antiguo no
    /// se puede inyectar una pulsación, así que la prueba llama a la decisión; el
    /// cable (<c>Input.GetKeyDown</c>) es una línea sin lógica propia.
    ///
    /// La primera prueba es el verificador del enchufe: falla, nombrando el paso
    /// del informe, hasta que UiRoot implemente <see cref="IEscapeCloser"/>.
    /// </remarks>
    public class TeclasEnLaIslaTests
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

        [TearDown]
        public void TearDown()
        {
            // Una prueba que pausa deja el reloj a cero para las siguientes: es
            // estado global de Unity, no de ninguna escena.
            Time.timeScale = 1f;
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
        public IEnumerator EscapeCierraElPanelAbiertoYSinAbrirPausa()
        {
            yield return CargarYEmpezar();

            var mochila = Mochila();
            mochila.Show();
            yield return null;

            Assert.IsTrue(mochila.IsShowing,
                "la mochila ni siquiera abrió: la prueba no estaría midiendo nada");

            Menu().HandleEscape();
            yield return null;

            Assert.IsFalse(mochila.IsShowing,
                "ESC no cerró la mochila. Falta aplicar el enganche del " +
                "informe-teclas.md §3: UiRoot : IEscapeCloser, el registro en Mount() " +
                "y el método CloseTopPanel.");
            Assert.AreEqual(1f, Time.timeScale,
                "cerrar un panel abrió la pausa: el reloj ha quedado parado");
        }

        [UnityTest]
        public IEnumerator ConElMenuDePausaDelanteEscapeSigueSiendoDelMenu()
        {
            yield return CargarYEmpezar();

            EventBus.Publish(new GamePaused(true));
            yield return null;

            Assert.AreEqual(0f, Time.timeScale,
                "la pausa no llegó al reloj: FlowController no está escuchando");

            // Un panel abierto detrás del velo de pausa. No es un estado raro:
            // antes de este arreglo, ESC abría la pausa con la mochila abierta
            // detrás, así que cualquier partida real puede estar así.
            Mochila().Show();
            yield return null;

            Menu().HandleEscape();
            yield return null;

            Assert.AreEqual(1f, Time.timeScale,
                "con la pausa delante, ESC debía ser «seguir jugando»: el menú " +
                "gastó la tecla en otra cosa");
            Assert.IsTrue(Mochila().IsShowing,
                "el menú de pausa cerró un panel que él mismo tapa: le robó el " +
                "Escape a la capa de juego");
        }

        [UnityTest]
        public IEnumerator SinNadaAbiertoEscapeAbreLaPausa()
        {
            yield return CargarYEmpezar();

            Menu().HandleEscape();
            yield return null;

            Assert.AreEqual(0f, Time.timeScale,
                "sin nada abierto, ESC debía abrir la pausa y no lo hizo");

            var textos = TextosEnPantalla();
            Assert.That(textos, Does.Contain("Seguir jugando"),
                "no hay tarjeta de pausa en pantalla. Encontrado: " +
                string.Join(", ", textos));
        }

        private static MainMenuView Menu() =>
            Object.FindFirstObjectByType<MainMenuView>();

        /// <summary>
        /// La mochila de verdad, la que guarda UiRoot.
        /// </summary>
        /// <remarks>
        /// Por reflexión porque el campo es suyo y no hay otra puerta: es el mismo
        /// recurso que usa CosturaDeLaIslaTests al invocar Act() en el
        /// interactuador. Si el monolito renombra el campo, esta prueba avisa.
        /// </remarks>
        private static BagPanel Mochila()
        {
            var uiRoot = Object.FindFirstObjectByType<UiRoot>();
            Assert.That(uiRoot, Is.Not.Null, "no hay UiRoot montado en Isla");

            var campo = typeof(UiRoot).GetField("_bag",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.That(campo, Is.Not.Null,
                "UiRoot ya no llama _bag a su mochila: actualiza esta prueba");

            return (BagPanel)campo.GetValue(uiRoot);
        }

        /// <summary>El texto de todo lo legible montado en los documentos de UI.</summary>
        private static List<string> TextosEnPantalla()
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Recorrer(document.rootVisualElement, textos);
            }
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            // «text» no está en VisualElement sino en TextElement: es la misma
            // razón por la que CronicaEnLaIslaTests pregunta primero por Button.
            if (element is TextElement legible && !string.IsNullOrEmpty(legible.text))
                textos.Add(legible.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
