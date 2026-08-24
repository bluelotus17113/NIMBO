using System.Collections;
using System.Collections.Generic;
using Nimbo.Art.PlayerView;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;
using Nimbo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// La prueba de la costura: seis agentes trabajaron a la vez sobre este árbol
    /// (tiendas, cultivos, ficha, tema, cámara y falda del borde) y cada uno dejó
    /// su pieza probada por separado. Lo que se rompe con agentes en paralelo no
    /// es el trabajo de cada uno: es la unión.
    /// </summary>
    /// <remarks>
    /// Un solo recorrido, el que haría un jugador que arranca la isla y vive un
    /// minuto: la cámara le encuadra de tercera persona, la isla cierra por debajo,
    /// los botones responden al tema, la tienda tiene surtido y compra de verdad,
    /// la ficha se abre desde la fila de abajo con la vida social a la vista, lo
    /// sembrado del catálogo sale dibujado con su silueta y no con la genérica, y
    /// el árbol de la plaza le contesta en pantalla cuando se pone delante.
    ///
    /// Si un eslabón falla aquí, las pruebas unitarias de cada pieza pueden seguir
    /// en verde para siempre: es exactamente cómo las tiendas pasaron semanas
    /// «funcionando» sin que se pudiera comprar nada.
    /// </remarks>
    public class CosturaDeLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena.
            // Mismo cuidado que CronicaEnLaIslaTests y todas sus gemelas.
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
        public IEnumerator LaCadenaEnteraDelJugadorFunciona()
        {
            // ══ 1. cámara (nimbo-camara): tercera persona, no maqueta ════════════
            yield return CargarYEmpezar();

            var camera = Camera.main;
            Assert.IsNotNull(camera, "la escena Isla no trae cámara principal");
            var body = GameObject.Find("Protagonista");
            Assert.IsNotNull(body, "sin protagonista no hay nada que encuadrar");

            float distancia = Vector3.Distance(camera.transform.position,
                                               body.transform.position);
            Assert.Less(distancia, 8f,
                $"la cámara arranca a {distancia:0.0} m del muñeco: eso sigue siendo " +
                "el plano general, no la tercera persona que trajo esta tanda");

            Vector3 haciaElCuerpo = (body.transform.position - camera.transform.position)
                                    .normalized;
            Assert.Greater(Vector3.Dot(camera.transform.forward, haciaElCuerpo), 0.7f,
                "la cámara no mira hacia el protagonista al arrancar");

            // ══ 2. falda (nimbo-falda): la roca nace pegada al labio del prado ═══
            //
            // Sobre las mallas VIVAS de la escena, no llamando al constructor: si
            // WorldView dejara de pedir prado y roca en el orden que exige el
            // depósito del labio, esta cadena lo vería y FaldaDelBordeTests no.
            var prado = GameObject.Find("Isla/prado");
            var roca = GameObject.Find("Isla/roca");
            Assert.IsNotNull(prado, "no hay prado en la isla de la aldea");
            Assert.IsNotNull(roca, "no hay roca bajo la isla de la aldea");

            Cose(prado.GetComponent<MeshFilter>().sharedMesh,
                 roca.GetComponent<MeshFilter>().sharedMesh);

            // ══ 3. tema (nimbo-tema): la barra viste el tema y nadie pinta encima ═
            var botonComida = Boton("Comida");
            Assert.IsNotNull(botonComida, "no encuentro el botón «Comida» de la barra");
            Assert.That(botonComida.ClassListContains(UiTheme.ClassAction), Is.True,
                "el botón de la barra no lleva la clase del tema: los estados " +
                ":hover/:active/:focus no tienen por donde llegarle");
            // Contra un botón virgen y no contra una palabra clave concreta: qué
            // devuelve el inline sin tocar ha cambiado entre versiones de Unity
            // (mismo criterio que TemaBotonesTests).
            var virgen = new Button();
            Assert.That(botonComida.style.backgroundColor.keyword,
                        Is.EqualTo(virgen.style.backgroundColor.keyword),
                "alguien vuelve a pintar el fondo inline sobre un botón del tema: " +
                "el inline gana a cualquier pseudoclase y mata los estados");

            // ══ 4. tiendas (nimbo-tiendas): abrir como el jugador y comprar ══════
            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia),
                "nadie registró la economía: sin ella no hay tienda que valga");
            economia.AddCoins(100_000, "prueba de costura");
            AbrirCandados();

            var compras = new List<string>();
            var gastos = new List<long>();
            EventBus.Subscribe<ItemAcquired>(e => compras.Add(e.CatalogId));
            EventBus.Subscribe<CoinsChanged>(e =>
            {
                if (e.Delta < 0 && e.Reason != null && e.Reason.StartsWith("Comprar"))
                    gastos.Add(e.Delta);
            });

            yield return Pulsar(botonComida, "el botón «Comida» de la barra");

            Assert.That(AlguienDice(Textos(), "Hoy no queda nada"), Is.False,
                "la tienda abre vacía: o el id que pasa la interfaz no llega al " +
                "catálogo, o la rotación amaneció sin surtido");

            var comprables = BotonesComprables();
            Assert.That(comprables, Is.Not.Empty,
                "la tienda de comida no tiene ni una fila comprable");

            var objetivo = comprables[0];
            var centro = new Vector2(objetivo.worldBound.center.x,
                                     objetivo.worldBound.center.y);
            Assert.That(objetivo.panel.Pick(centro), Is.SameAs(objetivo),
                "el botón Comprar está tapado por otro elemento: el clic del " +
                "jugador llegaría a otra cosa");

            yield return Pulsar(objetivo, "el primer objeto comprable");
            yield return null;

            Assert.That(compras, Is.Not.Empty,
                "se pulsó Comprar y no se adquirió nada: TryBuy volvió a quedarse " +
                "sin ejecutar jamás");
            Assert.That(gastos, Is.Not.Empty,
                "la compra llegó al servicio pero el monedero no registró el gasto");

            yield return CerrarLaTiendaSiEstaAbierta();

            // ══ 5. ficha (nimbo-ficha): abrirla desde la fila de abajo ═══════════
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo),
                "nadie registró el censo de habitantes");
            Assert.That(censo.Count, Is.GreaterThan(0),
                "la isla amaneció sin vecinos: no hay ficha que abrir");

            string vecino = censo.All[0].Identity.ShortName;
            var botonVecino = Boton(vecino);
            Assert.IsNotNull(botonVecino,
                $"no hay botón «{vecino}» en la fila de abajo: la ficha quedó sin " +
                "camino de entrada para el jugador");

            yield return Pulsar(botonVecino, $"el botón del vecino {vecino}");
            yield return null;

            // La ficha abierta se reconoce por su raíz, que lleva el id del
            // habitante (UiRoot.Toggle se lo pone al abrir). Acotar a ella evita
            // que un ScrollView de otro panel dé la prueba por buena.
            var ficha = ElementoPorNombre(censo.All[0].Id);
            Assert.IsNotNull(ficha,
                "pulsar el botón del vecino no abrió su ficha: UiRoot e " +
                "IslanderPanel quedaron desenchufados");

            Assert.That(BuscarTipo<ScrollView>(ficha), Is.Not.Null,
                "la ficha abrió sin scroll: mide más que la pantalla y lo que se " +
                "sale por abajo —las relaciones— no existe");

            var textosFicha = new List<string>();
            RecorrerTextos(ficha, textosFicha);
            Assert.That(textosFicha, Does.Contain("Quién anda con quién"),
                "la vida social de la isla entera no se lee en ningún sitio");
            Assert.That(textosFicha, Does.Contain("Qué le gusta"),
                "los gustos de regalo no salen en la ficha");

            // Se cierra como se abrió —mismo botón— para dejar el paso siguiente
            // en mundo despejado: el árbol se mira de pie en la plaza, no con la
            // ficha del vecino encima.
            yield return Pulsar(botonVecino, $"el botón del vecino {vecino} (para cerrar)");
            yield return null;

            // ══ 6. cultivos (nimbo-cultivos): lo del catálogo sale con su silueta ═
            //
            // Siembro por el servicio real lo primero del catálogo real y exijo
            // que la planta dibujada sea EXACTAMENTE la que FarmView dice saber
            // hacer para ese id — y que no sea la genérica, que es el fallo mudo:
            // caería ahí sin excepción ni aviso.
            Assert.IsTrue(ServiceRegistry.TryGet<IFarmingService>(out var farm),
                "nadie registró el huerto");
            var crop = farm.Crops[0];
            Assert.IsTrue(ServiceRegistry.TryGet<IInventoryService>(out var bolsa));
            Assert.AreEqual(StoreResult.Ok,
                            bolsa.TryStore(crop.SeedId, 1, out _),
                $"no hubo forma de tener la semilla {crop.SeedId}");

            Assert.AreEqual(FarmError.Ok, farm.Till(2, 1), "labrar (2,1) falló");
            Assert.AreEqual(FarmError.Ok, farm.Plant(2, 1, crop.SeedId),
                "sembrar la primera del catálogo falló");

            for (int dia = 0; dia < 12 && farm.TileAt(2, 1).State == TileState.Planted;
                 dia++)
            {
                farm.Water(2, 1);
                farm.AdvanceDay();
            }
            Assert.AreEqual(TileState.Ready, farm.TileAt(2, 1).State,
                $"{crop.DisplayName} no maduró en doce días");

            for (int i = 0; i < 4; i++) yield return null;

            var casilla = GameObject.Find("Huerto/casilla_2_1/planta");
            Assert.IsNotNull(casilla,
                "la casilla madura no tiene planta dibujada: FarmView está " +
                "desenchufado del servicio de farming");

            var nombresDibujados = new HashSet<string>();
            for (int i = 0; i < casilla.transform.childCount; i++)
                nombresDibujados.Add(casilla.transform.GetChild(i).name);

            string[] nombresEsperados = NombresDeLaSilueta(crop.SeedId);
            Assert.That(nombresDibujados, Is.EquivalentTo(nombresEsperados),
                $"lo que se dibuja para {crop.SeedId} no es la silueta que " +
                $"FarmView declara para ese id: o cayó en la genérica, o las dos " +
                "mitades (datos y arte) escribieron ids distintos");
            Assert.That(nombresEsperados.Length, Is.GreaterThanOrEqualTo(2),
                "la primera silueta del catálogo solo tiene una pieza: eso es la " +
                "planta genérica con otro nombre");

            // ══ 7. árbol (el enganche aplicado a mano sobre seis ficheros) ═══════
            //
            // GameEvents, GameBootstrap, PlayerInteractor, WorldView, UiRoot y el
            // contrato ITreeService los cruzó el orquestador en una sola pasada,
            // así que ninguna prueba de las piezas nuevas es dueña de esa unión.
            // ArbolEnLaIslaTests ya mira cartel y pulsación; aquí se recorre el
            // tramo que queda sin dueño: servicio → TreeSpoke → toast visible.
            Assert.IsTrue(ServiceRegistry.TryGet(out Island.NimboTree arbol),
                "nadie registró el Árbol Nimbo");
            Assert.IsTrue(ServiceRegistry.TryGet(out Core.Time.GameClock reloj),
                "nadie registró el reloj");

            // Nada debería habérselo llevado, pero la compra de antes pudo
            // desbloquear un logro y su carta ocupa la cola del toast: el texto se
            // espera con plazo (EsperaCartel) y no al fotograma siguiente.
            if (!arbol.CanTalkToday)
            {
                int hastaManana = (24 - reloj.Hour + 8) % 24 * 60 - reloj.Minute;
                if (hastaManana <= 0) hastaManana += 24 * 60;
                reloj.Advance(hastaManana);
            }
            Assert.IsTrue(arbol.CanTalkToday,
                "no hubo forma de que el árbol amaneciera disponible");

            var interactor = Object.FindFirstObjectByType<PlayerInteractor>();
            Assert.IsNotNull(interactor, "no hay interactuador en el protagonista");

            var mundo = Object.FindFirstObjectByType<Art.World.WorldView>();
            Assert.IsNotNull(mundo, "no hay isla levantada");

            // Al este del tronco, a cinco metros: dentro del alcance del cartel y
            // fuera del colisionador del pie. Mismo sitio que ArbolEnLaIslaTests.
            yield return ApuntarAlArbol(interactor, mundo, censo);

            // Act es privada porque es la única tecla de acción del juego, pero es
            // exactamente lo que el jugador dispara.
            var act = typeof(PlayerInteractor).GetMethod("Act",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            Assert.IsNotNull(act, "no encuentro Act en el interactuador");
            act.Invoke(interactor, null);

            yield return EsperaCartel("El Árbol Nimbo",
                "el árbol contestó pero su título no apareció en pantalla: falta " +
                "UiRoot escuchando TreeSpoke o el toast pintando lo empujado");

            // El segundo intento del mismo día también tiene que verse: un árbol
            // que se niega en silencio se lee como un árbol roto (NimboTree publica
            // el rechazo igual que el regalo). Y se reapunta antes de pulsar: la
            // primera corrida de esta cadena se puso roja porque, mientras el
            // primer cartel esperaba su turno en la cola del toast, un vecino
            // paseó de vuelta hasta la plaza y su cartel le ganó al del árbol.
            yield return ApuntarAlArbol(interactor, mundo, censo);
            act.Invoke(interactor, null);

            yield return EsperaCartel("Vuelve mañana",
                "el segundo intento no enseñó el «ya ha hablado hoy»: o el servicio " +
                "se calla al negarse, o ese texto tampoco llega a la interfaz");

            // ══ 8. avisos (bloqueos): la franja vive en el documento real ════════
            //
            // GateNoticeHost se autoinstala vía RuntimeInitializeOnLoadMethod: si
            // alguien cambia cómo carga la escena, desaparece sin romper nada ni
            // dar error. Aquí solo se exige que esté colgada; qué lee lo miden
            // AvisosEnLaIslaTests.
            Assert.IsNotNull(ElementoPorNombre(GateNotice.RootName),
                "la franja de avisos de bloqueo no está montada en ningún documento: " +
                "el host que se autoinstala no llegó a vivir");
        }

        // ── ayudas ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Sube a todos los vecinos al nivel máximo para que ningún UnlockLevel
        /// tape el surtido. Mismo criterio que TiendasEnLaIslaTests: aquí se prueba
        /// la costura, no la curva de progresión.
        /// </summary>
        private static void AbrirCandados()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registro)) return;

            foreach (var vecino in registro.All)
                vecino.Progression.Level = Data.Islanders.ProgressionState.MaxLevel;
        }

        /// <summary>
        /// Para cada sector, el vértice exterior del prado tiene que existir en la
        /// roca a menos de un centímetro. Es la soldadura de la falda medida sobre
        /// las mallas que de verdad cuelgan de la escena.
        /// </summary>
        private static void Cose(Mesh prado, Mesh roca)
        {
            int sectores = 48;
            Vector3[] verticesPrado = prado.vertices;
            Assert.Greater(verticesPrado.Length, sectores,
                "el prado no trae ni un anillo completo");

            Vector3[] verticesRoca = roca.vertices;
            float[] cuadrado = new float[verticesRoca.Length];
            for (int i = 0; i < verticesRoca.Length; i++)
                cuadrado[i] = verticesRoca[i].sqrMagnitude;

            int peorSector = -1;
            float peorHueco = 0f;
            for (int seg = 0; seg < sectores; seg++)
            {
                Vector3 labio = verticesPrado[verticesPrado.Length - sectores + seg];
                float mejor = float.MaxValue;
                for (int i = 0; i < verticesRoca.Length; i++)
                {
                    float d = (verticesRoca[i] - labio).sqrMagnitude;
                    if (d < mejor) mejor = d;
                }
                if (mejor > peorHueco)
                {
                    peorHueco = mejor;
                    peorSector = seg;
                }
            }

            Assert.Less(peorHueco, 0.01f * 0.01f,
                $"entre el prado y la roca hay un hueco de " +
                $"{Mathf.Sqrt(peorHueco):0.000} m en el sector {peorSector}: la " +
                "soldadura del borde se ha vuelto a abrir y a la altura de ojos " +
                "se ve a través de la isla");
        }

        /// <summary>
        /// Los nombres de las piezas que FarmView declara para ese seedId, leídos
        /// por reflexión de la misma tabla que usa el dibujo. Nada de copiar ids:
        /// si el arte renombra una pieza, esta prueba sigue al arte.
        /// </summary>
        private static string[] NombresDeLaSilueta(string seedId)
        {
            var farmViewType = System.Type.GetType(
                "Nimbo.Art.World.FarmView, Nimbo.Art");
            Assert.That(farmViewType, Is.Not.Null,
                "no encuentro FarmView: cambió de ensamblado o de nombre");

            var lookFor = farmViewType.GetMethod("LookFor",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Static);
            Assert.That(lookFor, Is.Not.Null, "FarmView ya no tiene LookFor");

            var look = lookFor.Invoke(null, new object[] { seedId });
            var readyField = look.GetType().GetField("Ready");
            Assert.That(readyField, Is.Not.Null, "CropLook ya no trae Ready");

            var partes = (System.Array)readyField.GetValue(look);
            var nombres = new string[partes.Length];
            for (int i = 0; i < partes.Length; i++)
            {
                var nameField = partes.GetType().GetElementType().GetField("Name");
                nombres[i] = (string)nameField.GetValue(partes.GetValue(i));
            }
            return nombres;
        }

        /// <summary>Pulsa un botón con la secuencia completa de puntero.</summary>
        /// <remarks>
        /// El panel necesita PointerMove → PointerDown → PointerUp con fotograma
        /// entre cada uno; invocar el Clickable a pelo pierde el clic sin ruido.
        /// Camino descubierto por TemaEnLaIslaTests y usado por TiendasEnLaIslaTests.
        /// </remarks>
        private static IEnumerator Pulsar(Button boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            var raiz = boton.panel.visualTree;
            var centro = new Vector2(boton.worldBound.center.x,
                                     boton.worldBound.center.y);

            raiz.SendEvent(PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = centro,
            }));
            yield return null;

            raiz.SendEvent(PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown,
                mousePosition = centro,
                button = 0,
            }));
            yield return null;

            raiz.SendEvent(PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp,
                mousePosition = centro,
                button = 0,
            }));
            yield return null;
        }

        private static IEnumerator CerrarLaTiendaSiEstaAbierta()
        {
            var cerrar = Boton("Cerrar");
            if (cerrar != null && cerrar.worldBound.width > 1f)
                yield return Pulsar(cerrar, "el botón Cerrar de la tienda");
        }

        /// <summary>
        /// Espera a que algún texto en pantalla diga eso, con plazo generoso.
        /// </summary>
        /// <remarks>
        /// La cola del toast saca una carta por vuelta y aguanta 3,55 s por carta
        /// (AchievementToast): los logros que la compra de antes pudo desbloquear
        /// van delante del árbol. Afirmar en el fotograma siguiente sería una
        /// carrera contra una cola ajena, no una comprobación de la costura.
        /// </remarks>
        /// <summary>
        /// Espera a que un cartel diga eso. El plazo cubre la cola del toast.
        /// </summary>
        /// <remarks>
        /// **Cuarenta segundos y no quince, y el número se puede razonar.** El toast
        /// enseña los avisos de uno en uno, 3,2 s cada uno (AchievementToast), y esta
        /// prueba corre con la isla viva: mientras el cartel del árbol espera su turno,
        /// los vecinos que pasean por la plaza van metiendo los suyos por delante. Con
        /// quince segundos el aviso del árbol llegaba cuarto y la prueba se ponía roja
        /// sin que nada estuviera mal.
        ///
        /// No es «subir el plazo hasta que pase», que es como se tapan las pruebas
        /// inestables: cuarenta segundos son doce turnos de cola, y por delante del
        /// árbol no puede haber doce avisos en el rato que dura esta prueba.
        /// </remarks>
        private static IEnumerator EsperaCartel(string fragmento, string mensaje)
        {
            float plazo = 40f;
            while (plazo > 0f)
            {
                foreach (var texto in Textos())
                    if (texto.Contains(fragmento)) yield break;

                plazo -= Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.Fail(mensaje);
        }

        /// <summary>
        /// Lleva al protagonista a un punto, de verdad: con el CharacterController
        /// encendido se come el cambio de posición. Lo descubrió
        /// MinijuegosEnLaIslaTests y lo usa ArbolEnLaIslaTests.
        /// </summary>
        private static void Teletransportar(Transform who, Vector3 to)
        {
            var controller = who.GetComponent<CharacterController>();

            if (controller != null) controller.enabled = false;
            who.position = to;
            if (controller != null) controller.enabled = true;
        }

        /// <summary>
        /// Deja al protagonista con el Árbol Nimbo en el cartel y espera a que lo
        /// esté de verdad.
        /// </summary>
        /// <remarks>
        /// No basta teletransportarlo y confiar: los vecinos pasean por la plaza —
        /// donde está el árbol— y el cartel de una persona siempre gana al de un
        /// objeto, así que entre pulsación y pulsación el objetivo se lo puede
        /// haber llevado alguien que volvió a pasar por ahí. Por eso manda a todos
        /// lejos CADA vez y espera al recálculo del objetivo en vez de darle un
        /// fotograma fijo.
        /// </remarks>
        private static IEnumerator ApuntarAlArbol(PlayerInteractor interactor,
                                                  Art.World.WorldView mundo,
                                                  IIslanderRegistry censo)
        {
            var lejos = Data.World.Archipelago.HomeCentre + Vector3.forward * 10f;
            foreach (var cadaUno in censo.All)
                if (mundo.TryGetIslander(cadaUno.Id, out var cuerpo))
                    cuerpo.position = lejos;

            Teletransportar(interactor.transform, new Vector3(5f, 2f, 0f));
            interactor.transform.rotation = Quaternion.LookRotation(Vector3.left);

            float plazo = 3f;   // «el objetivo se recalcula despacio» (PlayerInteractor)
            while (interactor.Kind != TargetKind.Tree && plazo > 0f)
            {
                plazo -= Time.unscaledDeltaTime;
                yield return null;
            }

            Assert.AreEqual(TargetKind.Tree, interactor.Kind,
                $"delante del árbol el cartel ofrece otra cosa: «{interactor.Prompt}»");
        }

        private static Button Boton(string texto)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                var encontrado = Buscar(document.rootVisualElement, texto);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        private static Button Buscar(VisualElement element, string texto)
        {
            if (element is Button button && button.text == texto) return button;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = Buscar(element[i], texto);
                if (hijo != null) return hijo;
            }
            return null;
        }

        private static VisualElement ElementoPorNombre(string nombre)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                var encontrado = BuscarNombre(document.rootVisualElement, nombre);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        private static VisualElement BuscarNombre(VisualElement element, string nombre)
        {
            if (element.name == nombre) return element;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = BuscarNombre(element[i], nombre);
                if (hijo != null) return hijo;
            }
            return null;
        }

        private static T BuscarTipo<T>(VisualElement element) where T : VisualElement
        {
            if (element is T encontrado) return encontrado;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = BuscarTipo<T>(element[i]);
                if (hijo != null) return hijo;
            }
            return null;
        }

        private static List<string> Textos()
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                RecorrerTextos(document.rootVisualElement, textos);
            }
            return textos;
        }

        private static void RecorrerTextos(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                RecorrerTextos(element[i], textos);
        }

        private static bool AlguienDice(List<string> textos, string fragmento)
        {
            foreach (var texto in textos)
                if (texto.Contains(fragmento)) return true;
            return false;
        }

        private static List<Button> BotonesComprables()
        {
            var comprables = new List<Button>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Recolectar(document.rootVisualElement, comprables);
            }
            return comprables;
        }

        private static void Recolectar(VisualElement element, List<Button> comprables)
        {
            if (element is Button button &&
                button.text == "Comprar" && button.enabledInHierarchy)
                comprables.Add(button);

            for (int i = 0; i < element.childCount; i++)
                Recolectar(element[i], comprables);
        }
    }
}
