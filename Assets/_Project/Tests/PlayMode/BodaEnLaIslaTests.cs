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
    /// Que la boda del protagonista exista **en el juego** (§14.5).
    /// </summary>
    /// <remarks>
    /// Era el sitio donde el juego se paraba: podías salir con alguien y ahí se
    /// acababa. Lo que se comprueba aquí no es que funcione —de eso van los tests de
    /// modo edición— sino que haya por dónde llegar: que tu cabaña se pueda ampliar,
    /// que el anillo esté en el catálogo de la partida de verdad y que la ficha del
    /// vecino tenga el botón.
    /// </remarks>
    public class BodaEnLaIslaTests
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
        public IEnumerator TuCabañaSePuedeAmpliar()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IHomeUpgradeService>(out var casas));
            Assert.That(casas.PlayerLevel, Is.Zero, "se empieza con la cabaña de serie");

            // Con dinero y obra de sobra tiene que dejar. Antes de esto el protagonista
            // no tenía niveles de casa y la boda pedía uno que no existía.
            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia));
            Assert.IsTrue(ServiceRegistry.TryGet<IInventoryService>(out var mochila));

            economia.AddCoins(9000, "prueba");
            foreach (var cost in casas.MaterialsFor(0))
                mochila.TryStore(cost.CatalogId, cost.Quantity, out _);

            Assert.That(casas.CanUpgradePlayerHome(), Is.EqualTo(UpgradeRejection.Ok));
            Assert.That(casas.UpgradePlayerHome(), Is.True);
            Assert.That(casas.PlayerLevel, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator ElAnilloYElCristalEstanEnLaPartidaDeVerdad()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia));

            Assert.That(economia.GetItem(RomanceItems.Ring), Is.Not.Null);
            Assert.That(economia.GetItem("mat_cristal_nimbo"), Is.Not.Null);

            // Y alguna geoda de la isla lo suelta, o el anillo no se puede fabricar.
            Assert.IsTrue(ServiceRegistry.TryGet<IGatheringService>(out var recoleccion));

            bool alguna = false;
            foreach (var def in recoleccion.Catalog)
                if (def.DropId == "mat_cristal_nimbo") alguna = true;

            Assert.That(alguna, Is.True,
                "el anillo pide un material que no suelta nada: sería una receta que no " +
                "se puede hacer nunca");
        }

        [UnityTest]
        public IEnumerator LaFichaTieneBotonParaDeclararseYLuegoParaPedirLaMano()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThan(0));

            var seccion = new SocialSection();
            seccion.Refresh(censo.All[0].Id);

            // Sin salir todavía, el botón es el de declararse. El de pedir la mano no
            // sale hasta que hay algo: enseñar el segundo escalón antes de subir el
            // primero solo sirve para que el jugador pulse y le digan que no.
            var textos = Botones(seccion.Root);
            Assert.That(textos, Does.Contain("Declararte"));
            Assert.That(textos, Does.Not.Contain("Pedirle la mano"));
        }

        private static List<string> Botones(VisualElement root)
        {
            var textos = new List<string>();
            Recorrer(root, textos);
            return textos;

            static void Recorrer(VisualElement element, List<string> into)
            {
                if (element is Button button && !string.IsNullOrEmpty(button.text))
                    into.Add(button.text);
                else if (element is Label label && !string.IsNullOrEmpty(label.text))
                    into.Add(label.text);

                for (int i = 0; i < element.childCount; i++) Recorrer(element[i], into);
            }
        }
    }
}
