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
    /// Que «Vender todo» del cajón de envíos **no venda hasta confirmar**, y que el
    /// cartel enseñe qué se va a perder antes de perderlo.
    /// </summary>
    /// <remarks>
    /// El botón vacía de un golpe materiales y cultivos que los encargos de material
    /// pagan por recibir: un clic equivocado fundía lo que un vecino esperaba, y la
    /// venta no se puede deshacer. Aquí se cruza la costura entera —botón real,
    /// mochila real, catálogo de verdad— porque lo que se prueba es justo el camino
    /// del clic.
    ///
    /// Va en modo juego porque el ensamblado de edición no ve <c>Nimbo.UI</c>, y
    /// porque pulsar de verdad exige la secuencia de puntero completa: invocar el
    /// interior de <c>Clickable</c> por reflexión no dispara sus callbacks y el clic
    /// se pierde sin excepción (lo pagó <see cref="TiendasEnLaIslaTests"/>).
    /// </remarks>
    public class CajonSeguroTests
    {
        /// <summary>Las ventas que llegaron al monedero, por su motivo «vendido …».</summary>
        private readonly List<long> _vendido = new List<long>();

        private void ContarVentas(CoinsChanged evt)
        {
            if (evt.Reason != null && evt.Reason.StartsWith("vendido"))
                _vendido.Add(evt.Delta);
        }

        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);

            _vendido.Clear();
            EventBus.Subscribe<CoinsChanged>(ContarVentas);
        }

        [TearDown]
        public void TearDown() => EventBus.Unsubscribe<CoinsChanged>(ContarVentas);

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        /// <summary>
        /// Mete madera en la mochila y abre el cajón, que es el recorrido del jugador:
        /// recoger en la isla y pararse delante del cajón de envíos.
        /// </summary>
        /// <remarks>
        /// Los servicios se piden **después** de cargar la escena: antes del arranque
        /// nadie los ha registrado todavía.
        /// </remarks>
        private static IEnumerator ConMaderaEnElCajon()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IInventoryService>(),
                "nadie registró la mochila al arrancar la isla");

            var mochila = ServiceRegistry.Get<IInventoryService>();

            // Cinco maderas: material de catálogo, vendible, y exactamente lo que pide
            // un encargo de material. Lo que traiga la partida del día 1 —azada,
            // regadera, semillas— no es vendible aquí, así que la firma de la venta
            // sale sola y la prueba deja de depender del sorteo inicial.
            Assert.That(mochila.TryStore("mat_madera", 5, out _),
                Is.EqualTo(StoreResult.Ok),
                "la mochila del primer día no admite cinco maderas");

            EventBus.Publish(new StationUsed(CraftStationKind.Shipping));
            yield return null;
        }

        [UnityTest]
        public IEnumerator VenderTodoNoVendeHastaConfirmar()
        {
            yield return ConMaderaEnElCajon();

            var mochila = ServiceRegistry.Get<IInventoryService>();
            long precio = ServiceRegistry.Get<IEconomyService>()
                .GetItem("mat_madera").Price;

            yield return Pulsar(Boton("Vender todo"),
                "el botón «Vender todo» del cajón de envíos");
            yield return null;

            Assert.That(mochila.CountOf("mat_madera"), Is.EqualTo(5),
                "«Vender todo» vació la mochila sin pasar por la confirmación");
            Assert.That(_vendido, Is.Empty,
                "el monedero cobró una venta que nadie confirmó");

            Assert.That(AlguienDice(Textos(), "¿Venderlo todo?"), Is.True,
                "no hay cartel de confirmación: el segundo clic no tiene contra qué pulsar");
            Assert.That(AlguienDice(Textos(), "Madera"), Is.True,
                "el cartel no enseña qué se pierde, y preguntar sin enseñar es un «¿seguro?»");

            var confirmar = BotonVisibleQueEmpiezaPor("Vender todo (");
            Assert.That(confirmar, Is.Not.Null,
                "el cartel de confirmación no trae el botón que vende de verdad");

            yield return PulsarCuandoSeVea(confirmar, "el botón de confirmar la venta");
            yield return null;

            Assert.That(mochila.CountOf("mat_madera"), Is.EqualTo(0),
                "confirmada la venta, la madera sigue en la mochila");
            Assert.That(_vendido, Is.Not.Empty,
                "confirmada la venta, el monedero no registró ningún ingreso");
            Assert.That(System.Linq.Enumerable.Sum(_vendido),
                Is.EqualTo(5 * precio),
                $"la venta confirmada no cuadra: 5 maderas a {precio} nimbos");
        }

        [UnityTest]
        public IEnumerator MejorNoDejaLaMochilaIntacta()
        {
            yield return ConMaderaEnElCajon();

            var mochila = ServiceRegistry.Get<IInventoryService>();

            yield return Pulsar(Boton("Vender todo"),
                "el botón «Vender todo» del cajón de envíos");
            yield return null;

            Assert.That(AlguienDice(Textos(), "¿Venderlo todo?"), Is.True);

            var mejorNo = BotonVisibleQueEmpiezaPor("Mejor no");
            Assert.That(mejorNo, Is.Not.Null,
                "el cartel del cajón no trae un «Mejor no» visible");
            yield return PulsarCuandoSeVea(mejorNo, "el botón «Mejor no» del cartel");
            yield return null;

            Assert.That(mochila.CountOf("mat_madera"), Is.EqualTo(5),
                "cancelar la confirmación costó madera");
            Assert.That(_vendido, Is.Empty,
                "cancelar la confirmación movió el monedero");
            Assert.That(AlguienDice(Textos(), "¿Venderlo todo?"), Is.False,
                "cancelado el cartel, sigue ahí prometiendo una venta que no se va a hacer");
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>
        /// Espera a que el botón tenga tamaño real y lo pulsa con la secuencia completa.
        /// </summary>
        /// <remarks>
        /// El cartel de confirmación se crea en el mismo fotograma en que se pulsa
        /// «Vender todo»: su layout puede llegar un par de fotogramas tarde, y pulsar
        /// sobre un rectángulo de cero sería disparar a quien caiga en ese punto. Son
        /// los mismos cuidados de <see cref="TiendasEnLaIslaTests"/> con sus botones.
        /// </remarks>
        private static IEnumerator PulsarCuandoSeVea(Button boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            for (int intento = 0; intento < 10 && boton.worldBound.width <= 1f; intento++)
                yield return null;

            if (boton.worldBound.width <= 1f)
            {
                var sitio = new System.Text.StringBuilder();
                Diagnosticar(boton, sitio);
                Assert.Fail($"{queEra} sigue sin tamaño tras 10 fotogramas.\n{sitio}");
            }

            yield return Pulsar(boton, queEra);
        }

        /// <summary>El camino de padres del elemento, con tamaños, para diagnosticar.</summary>
        private static void Diagnosticar(VisualElement element, System.Text.StringBuilder sitio)
        {
            sitio.AppendLine(
                $"{element.GetType().Name} «{element.name}» texto=«{(element as Button)?.text}» " +
                $"{element.resolvedStyle.width}×{element.resolvedStyle.height} " +
                mundo(element.worldBound));

            if (element.parent != null) Diagnosticar(element.parent, sitio);
        }

        private static string mundo(Rect r) =>
            $"mundo=({r.x:F0},{r.y:F0} {r.width:F0}×{r.height:F0})";

        /// <summary>
        /// Pulsa el botón con una secuencia real de puntero: mover, apretar, soltar.
        /// Mismo camino que <see cref="TiendasEnLaIslaTests"/>, que lo pagó.
        /// </summary>
        private static IEnumerator Pulsar(Button boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            var centro = new Vector2(boton.worldBound.center.x, boton.worldBound.center.y);

            // Que esté no basta: tiene que estar AL ALCANCE del puntero, o el clic
            // real se lo llevaría lo que haya debajo.
            Assert.That(boton.panel.Pick(centro), Is.SameAs(boton),
                $"{queEra} está tapado por otro elemento: el clic del jugador llegaría " +
                "a otra cosa");

            var raiz = boton.panel.visualTree;

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

        /// <summary>
        /// El botón con ese texto exacto **y a la vista**. El filtro de visibilidad no
        /// es un lujo: el documento del menú sigue en el árbol con su raíz a cero y
        /// guarda un «Mejor no» propio (el de empezar de nuevo), que es justo lo que
        /// encontró esta prueba antes de mirar el del cajón.
        /// </summary>
        private static Button Boton(string texto)
            => BotonQueEmpiezaPor(texto, exacto: true, soloVisibles: true);

        /// <summary>
        /// El botón cuyo texto empieza por ese prefijo **y se ve**: el original
        /// «Vender todo» sigue en el árbol mientras se confirma, pero apartado, y
        /// pulsarlo con eventos reales sería disparar a quien caiga en (0,0).
        /// </summary>
        private static Button BotonVisibleQueEmpiezaPor(string prefijo)
            => BotonQueEmpiezaPor(prefijo, exacto: false, soloVisibles: true);

        private static Button BotonQueEmpiezaPor(string prefijo, bool exacto,
                                                 bool soloVisibles = false)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                var encontrado = Buscar(document.rootVisualElement, prefijo,
                                        exacto, soloVisibles);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        private static Button Buscar(VisualElement element, string prefijo,
                                     bool exacto, bool soloVisibles)
        {
            if (element is Button button &&
                (exacto ? button.text == prefijo : button.text.StartsWith(prefijo)) &&
                (!soloVisibles || button.worldBound.width > 1f))
                return button;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = Buscar(element[i], prefijo, exacto, soloVisibles);
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
                Recorrer(document.rootVisualElement, textos);
            }
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }

        private static bool AlguienDice(List<string> textos, string fragmento)
        {
            foreach (var texto in textos)
                if (texto.Contains(fragmento)) return true;
            return false;
        }
    }
}
