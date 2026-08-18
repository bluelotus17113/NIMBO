using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.UI.Islander;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que el cortejo exista **en el juego**: servicios puestos y botones donde pulsar.
    /// </summary>
    /// <remarks>
    /// Las ocho interacciones sociales llevaban desde el principio escritas, con sus
    /// efectos y sus límites diarios, y la única forma de tratar con alguien era
    /// ponerse delante y pulsar — que siempre era «charlar». La vía de Convivencia
    /// subía sin abrir nada porque no había puerta donde poner la llave. Esto comprueba
    /// que ahora la hay.
    /// </remarks>
    public class CortejoEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
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
        public IEnumerator LaConductaSeMideDesdeElArranque()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IConductService>(),
                "sin esto el protagonista no tiene ejes y el cortejo no puede fallar " +
                "por incompatible, que es de lo que va");
        }

        [UnityTest]
        public IEnumerator AndarPorLaIslaVaDejandoHuella()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IConductService>(out var conducta));

            // La Actitud la apunta el buscador de objetivo en su cadencia lenta, así
            // que basta con dejar correr unos fotogramas de juego de verdad.
            yield return new WaitForSeconds(0.6f);

            Assert.That(conducta.ConfidenceOf(Data.Islanders.PersonalityAxis.Attitude),
                Is.GreaterThan(0f),
                "nadie está apuntando si andas solo o acompañado: el eje se queda en cero " +
                "para siempre y todos los vecinos te ven igual");
        }

        [UnityTest]
        public IEnumerator LaFichaDeUnVecinoTieneQueDecirle()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThan(0));

            var seccion = new SocialSection();
            seccion.Refresh(censo.All[0].Id);

            var textos = new List<string>();
            Recorrer(seccion.Root, textos);

            Assert.That(textos, Does.Contain("Charlar"));
            Assert.That(textos, Does.Contain("Declararte"),
                "el botón sale siempre, apagado y con el motivo puesto: «te falta un " +
                "ramo» es algo que hacer esta tarde, un hueco vacío no es nada");
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);
            else if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
