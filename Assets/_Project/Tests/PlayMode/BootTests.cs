using System.Collections;
using System.IO;
using Nimbo.Core.Save;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Arranca la escena de verdad y comprueba que el juego se enciende.
    /// </summary>
    /// <remarks>
    /// Los tests de editor prueban las reglas; este prueba que el cableado existe.
    /// Son cosas distintas: la simulación puede estar perfecta y el juego no arrancar
    /// porque a un servicio le falte registrarse, y eso no lo ve ningún test de mesa.
    /// </remarks>
    public class BootTests
    {
        [SetUp]
        public void SetUp()
        {
            // Partida limpia: si queda una guardada de otra prueba, la isla arrancaría
            // con sus habitantes y no con los que se esperan aquí.
            foreach (var path in new[] { SaveSystem.SavePath, SaveSystem.BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }

        [UnityTest]
        public IEnumerator LaEscenaArrancaYMontaLosServicios()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.IsTrue(ServiceRegistry.IsRegistered<IIslanderRegistry>(), "falta el censo");
            Assert.IsTrue(ServiceRegistry.IsRegistered<IPersonalityService>(), "faltan las personalidades");
            Assert.IsTrue(ServiceRegistry.IsRegistered<ISimulationService>(), "falta la simulación");
            Assert.IsTrue(ServiceRegistry.IsRegistered<IRequestService>(), "faltan las peticiones");
            Assert.IsTrue(ServiceRegistry.IsRegistered<ISocialService>(), "falta lo social");
            Assert.IsTrue(ServiceRegistry.IsRegistered<IHousingService>(), "falta la vivienda");
            Assert.IsTrue(ServiceRegistry.IsRegistered<IEconomyService>(), "falta la economía");
            Assert.IsTrue(ServiceRegistry.IsRegistered<IIslandService>(), "falta la isla");
            Assert.IsTrue(ServiceRegistry.IsRegistered<GameClock>(), "falta el reloj");
        }

        [UnityTest]
        public IEnumerator LaIslaEmpiezaConHabitantesYConCasa()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            Assert.Greater(registry.Count, 0, "la isla arrancó vacía");

            foreach (var islander in registry.All)
            {
                Assert.IsNotEmpty(islander.Identity.DisplayName, "un habitante sin nombre");
                Assert.IsTrue(islander.Home.HasHome,
                              $"{islander.Identity.DisplayName} no tiene casa");
            }
        }

        [UnityTest]
        public IEnumerator ElMundoDibujaUnMunecoPorHabitante()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;
            yield return null;

            var registry = ServiceRegistry.Get<IIslanderRegistry>();
            var views = Object.FindObjectsByType<Art.Chibi.IslanderView>(FindObjectsSortMode.None);

            Assert.AreEqual(registry.Count, views.Length,
                            "no hay un muñeco por habitante");

            foreach (var view in views)
            {
                var renderers = view.GetComponentsInChildren<MeshRenderer>();
                Assert.GreaterOrEqual(renderers.Length, 4,
                    "a un habitante le faltan partes: piel, ropa, pelo y cara");

                foreach (var renderer in renderers)
                    Assert.IsNotNull(renderer.sharedMaterial, "una parte se quedó sin material");
            }
        }

        [UnityTest]
        public IEnumerator ElRelojCorreCuandoElJuegoCorre()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            var clock = ServiceRegistry.Get<GameClock>();
            long before = clock.ElapsedMinutes;

            // A un minuto de juego por segundo real, dos segundos bastan de sobra.
            yield return new WaitForSeconds(2.2f);

            Assert.Greater(clock.ElapsedMinutes, before, "el reloj está parado");
        }

        [UnityTest]
        public IEnumerator LaIslaTieneSuelo()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            var meadow = GameObject.Find("prado");
            Assert.IsNotNull(meadow, "no se generó la superficie de la isla");

            var mesh = meadow.GetComponent<MeshFilter>().sharedMesh;
            Assert.Greater(mesh.vertexCount, 100, "la isla salió con cuatro vértices");
            Assert.Greater(mesh.bounds.size.x, 100f, "la isla es más pequeña de lo que dice el diseño");
        }
    }
}
