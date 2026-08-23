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
    /// Que las tiendas existan **en el juego**: pulsar el botón como lo pulsa el
    /// jugador y encontrar dentro algo comprable, y que comprar mueva el monedero.
    /// </summary>
    /// <remarks>
    /// Es la gemela de <c>CronicaEnLaIslaTests</c> para la economía. El fallo que caza
    /// no estaba en ninguna clase: la interfaz abría tiendas con unos identificadores
    /// («tienda_comida») que el catálogo no conocía («NimboMart»), así que todos los
    /// días de todas las partidas la tienda contestaba «Hoy no queda nada» y
    /// <c>TryBuy</c> —con un único llamador en todo el juego— no se ejecutó jamás. Ni
    /// un test en verde se enteró, porque ninguno cruzaba la costura.
    ///
    /// Va en modo juego porque el ensamblado de pruebas de edición no ve
    /// <c>Nimbo.UI</c>, y porque lo que interesa es el camino completo: botón real,
    /// panel real, stock real del catálogo de verdad.
    /// </remarks>
    public class TiendasEnLaIslaTests
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
            AbrirCandados();
        }

        /// <summary>
        /// Sube a todos los vecinos al nivel máximo para que ningún
        /// <c>UnlockLevel</c> tape el surtido.
        /// </summary>
        /// <remarks>
        /// Sin esto, «hay algo comprable» depende del sorteo del día: el catálogo de
        /// muebles tiene tres objetos de nivel 1 entre unos noventa, y con ocho huecos
        /// al día lo normal es que la rotación amanezca entera bajo candado («Nivel
        /// X») sin que haya ninguna costura rota detrás. Lo que se prueba aquí es la
        /// costura interfaz→catálogo, no la curva de progresión; con los niveles
        /// arriba, toda fila renderizada es comprable y la prueba deja de ser un
        /// dado. <c>ShopPanel</c> mira el vecino de nivel más alto
        /// (<c>ShopPanel.HighestIslanderLevel</c>) y <c>TryBuy</c> repite el mismo
        /// chequeo (<c>EconomyService.TryBuy</c>), así que basta con subirlos todos.
        /// </remarks>
        private static void AbrirCandados()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registro)) return;

            foreach (var vecino in registro.All)
                vecino.Progression.Level = Data.Islanders.ProgressionState.MaxLevel;
        }

        [UnityTest]
        public IEnumerator CadaBotonDeTiendaAbreUnaTiendaConSurtido()
        {
            yield return CargarYEmpezar();

            // El dinero no es lo que se prueba aquí: con el monedero lleno, cualquier
            // objeto del surtido es comprable y la prueba deja de depender de la
            // suerte que tuvo el sorteo del día 1 con los precios.
            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia),
                "nadie registró la economía al arrancar la isla");
            economia.AddCoins(100_000, "prueba de tienda");

            foreach (var nombre in new[] { "Comida", "Muebles", "Ropa" })
            {
                yield return CerrarLaTiendaSiEstaAbierta();

                yield return Pulsar(Boton(nombre),
                    $"el botón «{nombre}» de la barra de acciones");

                Assert.That(AlguienDice(Textos(), "Hoy no queda nada"), Is.False,
                    $"la tienda «{nombre}» abre vacía: o el id que pasa la interfaz no " +
                    "llega al catálogo, o la rotación amaneció sin surtido");

                var comprables = BotonesComprables();
                Assert.That(comprables, Is.Not.Empty,
                    $"en «{nombre}» no hay ni una fila que se pueda comprar ya");

                // Al alcance de verdad: que el panel señale al botón bajo su centro
                // y no a un texto superpuesto (ver ComprarEnLaTienda…).
                var primero = comprables[0];
                var centro = new Vector2(primero.worldBound.center.x,
                                         primero.worldBound.center.y);
                Assert.That(primero.panel.Pick(centro), Is.SameAs(primero),
                    $"en «{nombre}» el primer botón Comprar está tapado por otro " +
                    "elemento: el clic del jugador llegaría a otra cosa");
            }
        }

        [UnityTest]
        public IEnumerator ComprarEnLaTiendaDeComidaDescuentaDelMonedero()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia));
            economia.AddCoins(100_000, "prueba de tienda");

            yield return Pulsar(Boton("Comida"), "el botón «Comida» de la barra de acciones");

            // La isla vive mientras la prueba corre y suelta ingresos propios: el
            // saldo absoluto sube y baja solo. Lo que firma una compra de verdad es
            // el par de eventos que TryBuy publica y nadie más: ItemAcquired y un
            // CoinsChanged negativo con motivo «Comprar …». Eso, y no el saldo, es
            // lo que estuvo sin ocurrir jamás mientras la costura estuvo rota.
            var compras = new List<string>();
            var gastos = new List<long>();

            EventBus.Subscribe<ItemAcquired>(e => compras.Add(e.CatalogId));
            EventBus.Subscribe<CoinsChanged>(e =>
            {
                if (e.Delta < 0 && e.Reason != null && e.Reason.StartsWith("Comprar"))
                    gastos.Add(e.Delta);
            });

            var comprables = BotonesComprables();
            Assert.That(comprables, Is.Not.Empty,
                "no hay nada comprable en la tienda de comida: sin fila con botón " +
                "activo no hay compra que probar");

            // Que el botón esté ahí no basta: tiene que estar AL ALCANCE del
            // puntero. Lo estuvo sin dejar de serlo durante toda una mañana de
            // depuración: la lista sin scroll aplastaba las filas y la descripción
            // de la fila de abajo quedaba por encima del botón — el panel, ante
            // ese punto, señalaba a la etiqueta y el clic se lo comía el texto.
            var objetivo = comprables[0];
            var centro = new Vector2(objetivo.worldBound.center.x,
                                     objetivo.worldBound.center.y);
            Assert.That(objetivo.panel.Pick(centro), Is.SameAs(objetivo),
                "el primer botón Comprar no es lo que el panel señala bajo su propio " +
                "centro: algo se dibuja encima (filas aplastadas, un panel superpuesto) " +
                "y al jugador el clic le llega a otra cosa");

            yield return Pulsar(objetivo, "el primer objeto comprable de la tienda");
            yield return null;

            Assert.That(compras, Is.Not.Empty,
                "se pulsó Comprar y no se adquirió nada: TryBuy no llegó a " +
                "ejecutarse nunca, que es justo lo que llevaba haciendo el juego " +
                "entero desde el primer día sin que nadie lo viera");
            Assert.That(gastos, Is.Not.Empty,
                "la compra llegó a TryBuy pero el monedero no registró el gasto");
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>
        /// Pulsa el botón con una secuencia real de puntero: mover, apretar, soltar.
        /// </summary>
        /// <remarks>
        /// Invocar por reflexión el interior de <c>Clickable</c> no dispara sus
        /// callbacks — lo comprobó esta misma prueba a su costa: el clic se perdía
        /// sin excepción y el monedero ni se enteró—. El panel necesita la misma
        /// cadena de eventos que una mano: <c>PointerMove</c>, <c>PointerDown</c>,
        /// <c>PointerUp</c>, con un fotograma entre cada uno para que el estado
        /// pseudo (:hover, :active) asiente. Mismo camino que TemaEnLaIslaTests.
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
            // La barra alterna abrir/cerrar sobre cualquier tienda abierta, así que
            // entre una y otra hay que cerrar de verdad y no confiar en el toggle.
            // El botón Cerrar existe siempre en el árbol, también con el panel
            // oculto: si está fuera de pantalla su rectángulo colapsa a cero, y
            // pulsarlo con eventos reales sería disparar a quien caiga en (0,0).
            var cerrar = Boton("Cerrar");
            if (cerrar != null && cerrar.worldBound.width > 1f)
                yield return Pulsar(cerrar, "el botón Cerrar de la tienda");
        }

        /// <summary>El botón con ese texto exacto, buscando en toda la interfaz.</summary>
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
    }
}
