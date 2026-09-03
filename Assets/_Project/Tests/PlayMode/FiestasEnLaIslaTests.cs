using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que organizar una fiesta exista **en el juego** (§15.3).
    /// </summary>
    /// <remarks>
    /// El módulo de eventos llevaba desde siempre sin estar registrado en ninguna
    /// parte: funcionaba solo, y nada de fuera podía hablarle. Esto comprueba que ahora
    /// sí, y que hay botón — que es la otra mitad de existir.
    /// </remarks>
    public class FiestasEnLaIslaTests
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
        public IEnumerator ElCalendarioDeLaAldeaEstaRegistrado()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IVillageEvents>(),
                "el módulo de eventos funcionaba solo y nada de fuera podía hablarle");
        }

        [UnityTest]
        public IEnumerator HayFiestasQuePedirYTodasDicenLoQueCuestan()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IVillageEvents>(out var fiestas));
            Assert.That(fiestas.Hostable, Is.Not.Empty);

            foreach (var fiesta in fiestas.Hostable)
            {
                Assert.That(fiesta.DisplayName, Is.Not.Empty);
                Assert.That(fiesta.Cost, Is.GreaterThan(0),
                    $"{fiesta.Id} sale gratis, y entonces habría fiesta todos los días");
            }
        }

        [UnityTest]
        public IEnumerator HayUnBotonParaOrganizar()
        {
            yield return CargarYEmpezar();

            var textos = TextosDeBotones();

            Assert.That(textos, Is.Not.Empty, "no encuentro la barra de acciones");
            Assert.That(textos, Does.Contain("Fiestas"),
                $"sin botón no hay forma de organizar nada. Botones: {string.Join(", ", textos)}");
        }

        private static List<string> TextosDeBotones() => Pulsables.Textos();

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
