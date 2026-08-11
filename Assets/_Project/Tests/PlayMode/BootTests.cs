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
        private static IEnumerator CargarYEmpezar(
            string nombre = "Prueba", Data.Islanders.AppearanceData? cara = null)
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            // Es lo que publica el creador al pulsar «Este soy yo». `NewGameRequested`
            // ya no enciende nada: solo abre el creador, y desde un test no se puede
            // rellenar un formulario.
            EventBus.Publish(new ProtagonistCreated(
                nombre, cara ?? Data.Islanders.AppearanceData.Default));
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
        public IEnumerator PedirPartidaNuevaAbreElCreadorYNoLaIsla()
        {
            // Entre pedir partida y tener partida hay una pantalla. Si la isla se
            // encendiera aquí, el creador saldría encima de una partida ya empezada y
            // el protagonista nacería con la cara sorteada antes de que la eligieras.
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new NewGameRequested());
            yield return null;
            yield return null;

            Assert.IsFalse(ServiceRegistry.IsRegistered<IIslanderRegistry>(),
                           "la isla se encendió sin pasar por el creador");
        }

        [UnityTest]
        public IEnumerator LaCaraDelCreadorEsLaDelMuneco()
        {
            // La comprobación de que el creador sirve para algo: que lo que eliges
            // llegue hasta el muñeco que anda por la isla, y no se quede por el camino.
            var cara = Data.Islanders.AppearanceData.Default;
            cara.HairStyle = 17;
            cara.SkinTone = new Color32(210, 150, 110, 255);
            cara.HairColor = new Color32(190, 60, 60, 255);

            yield return CargarYEmpezar("Marisol", cara);

            var player = ServiceRegistry.Get<Nimbo.Player.PlayerService>();
            Assert.AreEqual("Marisol", player.State.DisplayName);
            Assert.AreEqual(17, player.State.Appearance.HairStyle,
                            "el peinado que elegiste no llegó al protagonista");
            Assert.AreEqual(cara.HairColor.r, player.State.Appearance.HairColor.r,
                            "el color de pelo no llegó al protagonista");

            Assert.IsNotNull(GameObject.Find("Protagonista"),
                             "se guardó la cara pero no se dibujó a nadie");
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
        public IEnumerator LosLogrosDelPrimerMinutoTambienPagan()
        {
            // Poblar una isla nueva ya desbloquea logros —el primer edificio, el
            // primer amigo— y esos avisos salen mientras se monta la partida. Si la
            // paga se engancha después, esos primeros logros se consiguen y no
            // pagan. Se veía en el guardado y en ningún test: dos logros hechos y
            // las monedas exactamente en las 200 de salida.
            yield return CargarYEmpezar();

            var achievements = ServiceRegistry.Get<IAchievementService>();
            var economy = ServiceRegistry.Get<IEconomyService>();

            if (achievements.UnlockedCount == 0)
                Assert.Ignore("esta partida no desbloqueó nada al arrancar");

            Assert.Greater(economy.Wallet.Coins, 200L,
                $"{achievements.UnlockedCount} logros conseguidos y las monedas " +
                $"siguen en {economy.Wallet.Coins}");
        }

        [UnityTest]
        public IEnumerator ElProtagonistaEmpiezaConCuerpoYHerramientas()
        {
            // La comprobación de la aldea: que exista alguien a quien mover y que
            // lleve con qué trabajar. La primera vez salió con la mochila vacía —las
            // herramientas eran salida de receta y drop de nodo, pero no estaban dadas
            // de alta como objetos, así que la mochila las rechazaba por desconocidas.
            // Es la clase de agujero que no ve ningún test de un módulo suelto: cada
            // uno pasaba, lo que estaba roto era la costura.
            yield return CargarYEmpezar();

            var player = ServiceRegistry.Get<Nimbo.Player.PlayerService>();
            Assert.IsTrue(player.Exists, "la partida empezó sin protagonista");

            var body = GameObject.Find("Protagonista");
            Assert.IsNotNull(body, "el protagonista no tiene cuerpo en el mundo");
            Assert.IsNotNull(body.GetComponent<CharacterController>(),
                             "el protagonista no puede andar");

            var bag = ServiceRegistry.Get<IInventoryService>();
            Assert.Greater(bag.CountOf("tool_azada"), 0, "empieza sin azada");
            Assert.Greater(bag.CountOf("tool_regadera"), 0, "empieza sin regadera");

            int seeds = 0;
            foreach (var crop in ServiceRegistry.Get<IFarmingService>().Crops)
                seeds += bag.CountOf(crop.SeedId);
            Assert.Greater(seeds, 0, "empieza sin semillas que sembrar");
        }

        [UnityTest]
        public IEnumerator LaIslaTieneCosasQueRecoger()
        {
            yield return CargarYEmpezar();

            var gathering = ServiceRegistry.Get<IGatheringService>();
            Assert.Greater(gathering.Nodes.Count, 50,
                           "la isla amaneció sin nada que recoger");

            // Y que no hayan caído todos encima de la plaza, que es donde se aparece.
            int enLaPlaza = 0;
            foreach (var node in gathering.Nodes)
                if (node.X * node.X + node.Z * node.Z < 30f * 30f) enLaPlaza++;

            Assert.AreEqual(0, enLaPlaza, $"{enLaPlaza} nodos cayeron en la plaza");
        }

        [UnityTest]
        public IEnumerator LaCamaraSigueAlProtagonista()
        {
            yield return CargarYEmpezar();

            // La cámara llega suavemente, no aparece: sin dejarla converger se mide
            // el camino y no el destino.
            yield return new WaitForSeconds(1.5f);

            var body = GameObject.Find("Protagonista");
            var camera = Camera.main;
            Assert.IsNotNull(body);
            Assert.IsNotNull(camera);

            float distance = Vector3.Distance(camera.transform.position, body.transform.position);
            // El tope es generoso a propósito: lo que se comprueba es que la cámara
            // le siga, no el encuadre exacto, que es una decisión de diseño y se
            // retoca. Un test que fija la distancia se rompe cada vez que se ajusta.
            Assert.Less(distance, 30f,
                        $"la cámara se quedó a {distance:0} m del protagonista");

            // Y que lo tenga delante, no a la espalda.
            var toPlayer = (body.transform.position - camera.transform.position).normalized;
            Assert.Greater(Vector3.Dot(camera.transform.forward, toPlayer), 0.7f,
                           "la cámara no está mirando al protagonista");
        }

        [UnityTest]
        public IEnumerator LaCamaraEmpiezaEncuadrandoAlProtagonista()
        {
            // Antes del giro a aldea esta prueba exigía el plano general, y estaba
            // bien entonces: no había a quien seguir. Ahora lo primero que se ve es tu
            // muñeco, y el plano general quedó para cuando todavía no existe.
            yield return CargarYEmpezar();
            yield return null;

            var camera = Camera.main;
            var body = GameObject.Find("Protagonista");
            Assert.IsNotNull(camera, "no hay cámara principal en la escena");
            Assert.IsNotNull(body, "no hay protagonista al que encuadrar");

            Assert.Greater(camera.transform.position.y, body.transform.position.y + 5f,
                           "la cámara arranca a la altura del suelo");

            float aim = Vector3.Dot(camera.transform.forward,
                                    (body.transform.position - camera.transform.position).normalized);
            Assert.Greater(aim, 0.7f, "la cámara no arranca mirando al protagonista");
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
