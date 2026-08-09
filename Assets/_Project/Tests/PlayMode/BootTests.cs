using System.Collections;
using System.IO;
using Nimbo.Core.Events;
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
            // El arranque es DontDestroyOnLoad, así que sobrevive a cargar otra
            // escena: sin barrerlo, la segunda prueba se encontraría el de la
            // primera —ya montado, con sus servicios puestos— y estaría midiendo la
            // partida anterior sin enterarse. Al destruirlo corre su OnDestroy, que
            // es quien vacía el registro de servicios.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);

            // Partida limpia: si queda una guardada de otra prueba, la isla arrancaría
            // con sus habitantes y no con los que se esperan aquí. La ruta la ha
            // desviado ya AislarGuardadoEnPruebasDeJuego a una carpeta temporal.
            foreach (var path in new[] { SaveSystem.SavePath, SaveSystem.BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }

        /// <summary>
        /// Carga la escena y enciende la partida como lo haría el jugador.
        /// </summary>
        /// <remarks>
        /// Desde que hay menú principal, cargar la escena ya no arranca el juego: el
        /// arranque espera a que alguien diga por dónde empezar. Publicar el evento
        /// es exactamente lo que hace el botón «Empezar», así que estas pruebas
        /// recorren el mismo camino que el jugador en vez de un atajo.
        /// </remarks>
        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new NewGameRequested());
            yield return null;
            yield return null;
        }

        [UnityTest]
        public IEnumerator ElJuegoNoArrancaSoloSinPasarPorElMenu()
        {
            // Es la otra mitad del menú: que exista no sirve de nada si la isla se
            // monta igual por detrás mientras el jugador la está mirando.
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;
            yield return null;

            Assert.IsFalse(ServiceRegistry.IsRegistered<IIslanderRegistry>(),
                           "la partida se montó sola sin que nadie le diera a empezar");
        }

        [UnityTest]
        public IEnumerator LaEscenaArrancaYMontaLosServicios()
        {
            yield return CargarYEmpezar();

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
            yield return CargarYEmpezar();

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
            yield return CargarYEmpezar();

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
            yield return CargarYEmpezar();

            var clock = ServiceRegistry.Get<GameClock>();
            long before = clock.ElapsedMinutes;

            // A un minuto de juego por segundo real, dos segundos bastan de sobra.
            yield return new WaitForSeconds(2.2f);

            Assert.Greater(clock.ElapsedMinutes, before, "el reloj está parado");
        }

        [UnityTest]
        public IEnumerator LaCamaraEmpiezaEnPlanoGeneral()
        {
            // Lo primero que se ve de una partida es el encuadre. Una cámara que
            // arranca metida en el césped no la ve ninguna prueba de mesa: la malla
            // está bien, los servicios están bien, y la primera impresión es que el
            // juego está roto.
            yield return CargarYEmpezar();

            var camera = Camera.main;
            Assert.IsNotNull(camera, "no hay cámara principal en la escena");

            var position = camera.transform.position;
            Assert.Greater(position.y, 40f,
                           $"la cámara arranca a ras de suelo (y={position.y:0})");

            float distance = Vector3.Distance(position, Vector3.zero);
            Assert.That(distance, Is.EqualTo(150f).Within(20f),
                        $"la cámara no arranca en plano general (está a {distance:0} m)");

            float aim = Vector3.Dot(camera.transform.forward, (-position).normalized);
            Assert.Greater(aim, 0.97f, "la cámara no está mirando a la isla");
        }

        [UnityTest]
        public IEnumerator LaIslaTieneSuelo()
        {
            yield return CargarYEmpezar();

            var meadow = GameObject.Find("prado");
            Assert.IsNotNull(meadow, "no se generó la superficie de la isla");

            var mesh = meadow.GetComponent<MeshFilter>().sharedMesh;
            Assert.Greater(mesh.vertexCount, 100, "la isla salió con cuatro vértices");
            Assert.Greater(mesh.bounds.size.x, 100f, "la isla es más pequeña de lo que dice el diseño");
        }
    }
}
