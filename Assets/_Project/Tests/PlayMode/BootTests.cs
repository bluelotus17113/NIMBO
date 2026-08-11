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
        public IEnumerator ElProtagonistaNoSePuedeCaerDeLaIsla()
        {
            // Pasó de verdad en la primera partida jugada: el guardado quedó con el
            // protagonista a cincuenta metros por debajo de la isla, cayendo, y sin
            // forma de volver. La isla es un disco que flota: si andas hasta el borde
            // hay que pararte, no dejarte caer.
            yield return CargarYEmpezar();

            var body = GameObject.Find("Protagonista");
            Assert.IsNotNull(body);

            // Se le deja en el aire fuera de la isla y se comprueba que la red lo
            // devuelve, que es el caso que el guardado enseñó.
            var controller = body.GetComponent<CharacterController>();
            controller.enabled = false;
            body.transform.position = new Vector3(140f, -30f, 0f);
            controller.enabled = true;

            yield return new WaitForSeconds(0.6f);

            Assert.Greater(body.transform.position.y, -6f,
                $"el protagonista se quedó cayendo (y={body.transform.position.y:0})");

            // Con dos islas ya no vale medir contra el origen: estar a 176 metros de
            // él es estar en tu isla, no perdido. Lo que se comprueba es que haya
            // acabado sobre alguna de las dos.
            var landed = body.transform.position;
            var side = Data.World.Archipelago.SideOf(landed);
            var centre = Data.World.Archipelago.CentreOf(side);
            float radius = new Vector2(landed.x - centre.x, landed.z - centre.z).magnitude;

            Assert.Less(radius, Data.World.Archipelago.RadiusOf(side) + 12f,
                        $"acabó a {radius:0} m del centro de la isla {side}: fuera de ella");
        }

        [UnityTest]
        public IEnumerator ApareceAlLadoDeSuHuerto()
        {
            // La primera partida jugada acabó con cero casillas trabajadas y cero
            // nodos recogidos, y no por un fallo: aparecías a treinta y cuatro metros
            // de tu parcela, con los nodos aún más lejos, en una plaza vacía. Lo
            // primero que ves tiene que ser algo que puedas tocar.
            yield return CargarYEmpezar();

            var body = GameObject.Find("Protagonista");
            var farm = ServiceRegistry.Get<IFarmingService>();
            Assert.IsNotNull(body);

            Data.Farming.FarmPlot.CentreOf(farm.Width / 2, farm.Height / 2,
                                           farm.Width, farm.Height,
                                           out float fx, out float fz);

            float distance = Vector2.Distance(
                new Vector2(body.transform.position.x, body.transform.position.z),
                new Vector2(fx, fz));

            Assert.Less(distance, 16f,
                        $"apareció a {distance:0} m de su huerto: el primer minuto es andar");

            // Y que lo tenga DELANTE, no a la espalda. La distancia sola no basta: con
            // la cámara detrás del protagonista, apareciendo al otro lado de la parcela
            // la cámara caía encima de ella y salías mirando al lado contrario. Estabas
            // a cinco metros de tu huerto y no lo veías.
            yield return null;

            var camera = Camera.main;
            var toFarm = new Vector3(fx, camera.transform.position.y, fz) - camera.transform.position;
            Assert.Greater(Vector3.Dot(camera.transform.forward.normalized, toFarm.normalized), 0.2f,
                           "el huerto queda detrás de la cámara al empezar");
        }

        [UnityTest]
        public IEnumerator HayDosIslasYTuCasaEstaEnLaTuya()
        {
            yield return CargarYEmpezar();
            yield return null;

            Assert.IsNotNull(GameObject.Find("Isla del jugador"), "no se levantó tu isla");
            Assert.IsNotNull(GameObject.Find("Puente"), "no hay puente entre las islas");

            // Tu casa, tu huerto y tú, los tres en tu isla; los vecinos en la otra.
            Assert.AreEqual(Data.World.IslandSide.Home,
                            Data.World.Archipelago.SideOf(Data.Player.PlayerHome.Cabin),
                            "la cabaña se quedó en la isla de la aldea");

            var farm = ServiceRegistry.Get<IFarmingService>();
            Data.Farming.FarmPlot.CentreOf(0, 0, farm.Width, farm.Height,
                                           out float fx, out float fz);
            Assert.AreEqual(Data.World.IslandSide.Home,
                            Data.World.Archipelago.SideOf(new Vector3(fx, 0f, fz)),
                            "el huerto se quedó en la isla de la aldea");

            var body = GameObject.Find("Protagonista");
            Assert.AreEqual(Data.World.IslandSide.Home,
                            Data.World.Archipelago.SideOf(body.transform.position),
                            "apareces en la isla equivocada");
        }

        [UnityTest]
        public IEnumerator ElPuenteTieneSueloDePuntaAPunta()
        {
            // La comprobación que importa del puente: que se pueda cruzar. Un puente
            // dibujado sin suelo continuo tira al jugador al vacío a mitad de camino,
            // y la red de seguridad lo devuelve al principio — peor que no cruzar.
            yield return CargarYEmpezar();
            yield return null;
            yield return new WaitForFixedUpdate();

            var from = Data.World.Archipelago.BridgeFromVillage;
            var to = Data.World.Archipelago.BridgeToHome;

            for (int i = 0; i <= 20; i++)
            {
                var point = Vector3.Lerp(from, to, i / 20f);
                bool ground = Physics.Raycast(point + Vector3.up * 3f, Vector3.down,
                                              10f, ~0, QueryTriggerInteraction.Ignore);
                Assert.IsTrue(ground, $"no hay suelo en z={point.z:0}: ahí se cae");
            }
        }

        [UnityTest]
        public IEnumerator LaCamaraPuedeSeguirteHastaLaOtraIsla()
        {
            // El tope del pivote iba alrededor del origen. Con dos islas frenaba a
            // medio puente: el protagonista seguía andando y la cámara se quedaba
            // atrás, así que llegabas a tu isla viéndote de lejos y de espaldas.
            yield return CargarYEmpezar();

            var body = GameObject.Find("Protagonista");
            var controller = body.GetComponent<CharacterController>();

            // Se le planta en el centro de la aldea y se deja que la cámara le alcance.
            controller.enabled = false;
            body.transform.position = new Vector3(0f, 3f, 40f);
            controller.enabled = true;

            yield return new WaitForSeconds(1.5f);

            float distance = Vector3.Distance(Camera.main.transform.position,
                                              body.transform.position);
            Assert.Less(distance, 30f,
                        $"la cámara se quedó a {distance:0} m al cruzar a la otra isla");
        }

        [UnityTest]
        public IEnumerator LaCasaEstaPuestaYSeLlegaAndando()
        {
            // Los tres muebles tienen que estar donde el juego cree que están: si el
            // sitio dibujado y el sitio que se comprueba se separan, el jugador se
            // planta delante de la cama y el juego dice que no hay nada.
            yield return CargarYEmpezar();
            yield return null;

            Assert.IsNotNull(GameObject.Find("Casa del jugador"), "no se dibujó la casa");

            foreach (var nombre in new[] { "cabaña", "hamaca", "mesa", "cajon" })
            {
                var pieza = GameObject.Find(nombre);
                Assert.IsNotNull(pieza, $"falta «{nombre}»");
            }

            var hammock = GameObject.Find("hamaca");
            Assert.AreEqual(Data.Player.PlayerHome.Hammock.x, hammock.transform.position.x, 0.01f);
            Assert.AreEqual(Data.Player.PlayerHome.Hammock.z, hammock.transform.position.z, 0.01f);

            // Y que se llegue: apareces a un paseo corto, no al otro lado de la isla.
            var body = GameObject.Find("Protagonista");
            float distance = Vector3.Distance(body.transform.position, Data.Player.PlayerHome.Cabin);
            Assert.Less(distance, 20f, $"la casa queda a {distance:0} m de donde apareces");
        }

        [UnityTest]
        public IEnumerator DormirLlevaALaManianaSiguienteYReponeElVigor()
        {
            yield return CargarYEmpezar();

            var clock = ServiceRegistry.Get<GameClock>();
            var player = ServiceRegistry.Get<Nimbo.Player.PlayerService>();

            player.SpendVigor(70f);
            Assert.Less(player.Vigor, 40f);

            int dayBefore = clock.Day;

            // Lo mismo que hace la hamaca. Se comprueba el efecto, no el botón.
            int minutesToMorning = (24 - clock.Hour + 8) % 24 * 60 - clock.Minute;
            if (minutesToMorning <= 0) minutesToMorning += 24 * 60;
            clock.Advance(minutesToMorning);
            player.RestoreVigor(Data.Player.PlayerState.MaxVigor);

            Assert.AreEqual(8, clock.Hour, "no amaneció a las ocho");
            Assert.AreEqual(dayBefore + 1, clock.Day, "no pasó de día");
            Assert.AreEqual(Data.Player.PlayerState.MaxVigor, player.Vigor, 0.01f,
                            "dormir no repuso el vigor");
        }

        [UnityTest]
        public IEnumerator ElCajonNoVendeHerramientasNiSemillas()
        {
            // La regla que evita el desastre que no se puede deshacer: vender la azada
            // sin querer. Se deja fuera de la lista en vez de poner una confirmación,
            // porque una lista donde no está no se puede fallar.
            yield return CargarYEmpezar();

            var economy = ServiceRegistry.Get<IEconomyService>();

            var hoe = economy.GetItem("tool_azada");
            Assert.IsNotNull(hoe, "la azada no está en el catálogo");
            Assert.AreEqual(ItemCategory.Tool, hoe.Category,
                            "la azada no cuenta como herramienta: el cajón la vendería");

            var farm = ServiceRegistry.Get<IFarmingService>();
            foreach (var crop in farm.Crops)
            {
                var seed = economy.GetItem(crop.SeedId);
                if (seed == null) continue;
                Assert.AreEqual(ItemCategory.Seed, seed.Category,
                                $"{crop.SeedId} no cuenta como semilla: el cajón la vendería");
            }
        }

        [UnityTest]
        public IEnumerator ElKitInicialCubreElCicloEntero()
        {
            // Lo que se aprendió mirando una partida: labraste las 48 casillas y no
            // sembraste ninguna. Para que el ciclo se pueda completar sin salir a
            // comprar nada, lo que llevas el primer día tiene que dar para las cuatro
            // fases — y estar en la barra, donde se ve, no en el fondo de la mochila.
            yield return CargarYEmpezar();

            var bag = ServiceRegistry.Get<IInventoryService>();
            var economy = ServiceRegistry.Get<IEconomyService>();
            var farm = ServiceRegistry.Get<IFarmingService>();

            bool hoe = false, can = false, seeds = false;

            for (int i = 0; i < bag.HotbarSize; i++)
            {
                var stack = bag.At(i);
                if (stack.Quantity <= 0) continue;

                var item = economy.GetItem(stack.CatalogId);
                if (item != null && item.Tool == ToolKind.Hoe) hoe = true;
                if (item != null && item.Tool == ToolKind.WateringCan) can = true;
                if (farm.TryGetCrop(stack.CatalogId, out _)) seeds = true;
            }

            Assert.IsTrue(hoe, "la azada no está en la barra: no puedes labrar");
            Assert.IsTrue(can, "la regadera no está en la barra: no puedes regar");
            Assert.IsTrue(seeds, "las semillas no están en la barra: no puedes sembrar");
        }

        [UnityTest]
        public IEnumerator LasHerramientasDeVerdadSeReconocen()
        {
            // La que faltaba, y costó una partida entera. El tipo de herramienta se
            // deducía del identificador esperándolo en inglés («hoe», «axe») y los
            // del juego están en castellano («tool_azada»): TODAS se leían como
            // «ninguna», así que no se podía labrar, regar ni talar y no fallaba nada
            // — simplemente no pasaba nada al pulsar.
            //
            // Las pruebas del módulo no lo veían porque usaban sus propios
            // identificadores en inglés. Esta usa los del juego de verdad.
            yield return CargarYEmpezar();

            var bag = ServiceRegistry.Get<IInventoryService>();
            var economy = ServiceRegistry.Get<IEconomyService>();

            var esperado = new (string id, ToolKind kind)[]
            {
                ("tool_azada", ToolKind.Hoe),
                ("tool_regadera", ToolKind.WateringCan),
                ("tool_hacha", ToolKind.Axe),
                ("tool_pico", ToolKind.Pickaxe),
                ("tool_guadana", ToolKind.Scythe),
            };

            foreach (var (id, kind) in esperado)
            {
                var item = economy.GetItem(id);
                Assert.IsNotNull(item, $"{id} no está en el catálogo");
                Assert.AreEqual(kind, item.Tool, $"{id} no se reconoce como {kind}");
            }

            // Y de punta a punta: con la azada en el hueco elegido, la mano lleva azada.
            int slot = -1;
            for (int i = 0; i < bag.HotbarSize; i++)
                if (bag.At(i).CatalogId == "tool_azada") { slot = i; break; }

            Assert.GreaterOrEqual(slot, 0, "la azada no está en la barra");
            bag.Select(slot);
            Assert.AreEqual(ToolKind.Hoe, bag.ToolInHand,
                            "con la azada seleccionada, la mano no lleva azada");
        }

        [UnityTest]
        public IEnumerator ElHuertoSePuedeTrabajarDePrincipioAFin()
        {
            // El huerto llevaba escrito y probado desde el módulo, pero no había forma
            // de tocarlo desde el juego: era código muerto. Esto recorre el ciclo
            // entero por donde lo recorre el jugador — de pie sobre la casilla, con la
            // herramienta en la mano.
            yield return CargarYEmpezar();

            var farm = ServiceRegistry.Get<IFarmingService>();
            var bag = ServiceRegistry.Get<IInventoryService>();

            Assert.AreEqual(Data.Farming.TileState.Wild, farm.TileAt(0, 0).State);
            Assert.AreEqual(FarmError.Ok, farm.Till(0, 0));
            Assert.AreEqual(Data.Farming.TileState.Tilled, farm.TileAt(0, 0).State);

            string seed = null;
            foreach (var crop in farm.Crops)
                if (bag.CountOf(crop.SeedId) > 0) { seed = crop.SeedId; break; }
            Assert.IsNotNull(seed, "empezó sin semillas que sembrar");

            int before = bag.CountOf(seed);
            Assert.AreEqual(FarmError.Ok, farm.Plant(0, 0, seed));
            Assert.AreEqual(before - 1, bag.CountOf(seed), "sembrar no gastó la semilla");

            // Regar y pasar los días que pida: tiene que acabar listo para recoger.
            farm.TryGetCrop(seed, out var definition);
            for (int day = 0; day < definition.DaysToGrow; day++)
            {
                Assert.AreEqual(FarmError.Ok, farm.Water(0, 0));
                farm.AdvanceDay();
            }

            Assert.AreEqual(Data.Farming.TileState.Ready, farm.TileAt(0, 0).State,
                            "regado todos los días y no creció");

            int got = farm.Harvest(0, 0, out var error);
            Assert.AreEqual(FarmError.Ok, error);
            Assert.Greater(got, 0, "recoger no dio nada");
            Assert.Greater(bag.CountOf(definition.CropId), 0, "lo recogido no entró en la mochila");
        }

        [UnityTest]
        public IEnumerator ElHuertoSeDibujaDondeSeTrabaja()
        {
            // Las dos mitades tienen que estar de acuerdo: la casilla que pisas y la
            // losa que ves. Con el sitio copiado a mano en dos ficheros, labrarías una
            // y se pondría marrón otra.
            yield return CargarYEmpezar();
            yield return null;

            var farm = ServiceRegistry.Get<IFarmingService>();
            Assert.IsNotNull(GameObject.Find("Huerto"), "el huerto no se dibujó");

            var losa = GameObject.Find("casilla_0_0");
            Assert.IsNotNull(losa, "no se dibujó la casilla (0,0)");

            Data.Farming.FarmPlot.CentreOf(0, 0, farm.Width, farm.Height,
                                           out float x, out float z);
            Assert.AreEqual(x, losa.transform.position.x, 0.01f);
            Assert.AreEqual(z, losa.transform.position.z, 0.01f);

            // Y desde ese punto, el juego tiene que decir que estás en (0,0).
            Assert.IsTrue(Data.Farming.FarmPlot.TileAt(x, z, farm.Width, farm.Height,
                                                       out int tx, out int ty));
            Assert.AreEqual(0, tx);
            Assert.AreEqual(0, ty);
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
