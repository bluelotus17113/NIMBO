using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la voz y la ropa del creador existan **en el juego**: elegirlas como las
    /// elige el jugador —pulsando botones reales— y comprobar que el habitante que
    /// sale de «Listo» las lleva puestas, no que el panel las mostrara.
    /// </summary>
    /// <remarks>
    /// El GDD §17.1 marca el creador como completo desde hace tiempo y solo tenía
    /// cuerpo, cara y carácter. Estas pruebas son la condición para volver a marcarlo:
    /// si la voz elegida no llega al censo o la prenda se queda en la lista, aquí se
    /// cae. Gemelas de <c>TiendasEnLaIslaTests</c> y <c>CronicaEnLaIslaTests</c> en
    /// método: escena de verdad, clic de verdad, dato de verdad.
    /// </remarks>
    public class CreadorVozYRopaEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado.
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

        /// <summary>
        /// Abre el creador como lo abre el jugador y espera a que tenga layout.
        /// </summary>
        /// <remarks>
        /// En batchmode el primer fotograma tras display=Flex no siempre basta para
        /// que el árbol recién enseñado tenga rectángulos: TemaEnLaIslaTests se
        /// topó con ello y por eso existe su EsperarLayout. Sin esta espera, los
        /// botones de dentro existen pero miden cero y cualquier clic sintético
        /// caería en quien esté debajo.
        /// </remarks>
        private static IEnumerator AbrirCreador()
        {
            yield return Pulsar(Boton("Nuevo habitante"),
                "el botón «Nuevo habitante» de la barra de acciones");

            int frames = 0;
            while (!CreadorAbiertoYConLayout() && frames++ < 120)
                yield return null;

            Assert.That(CreadorAbiertoYConLayout(), Is.True,
                "el creador no llegó a abrirse con layout: o el clic no disparó " +
                "Show() o el panel sigue colapsado a cero píxeles");
        }

        private static bool CreadorAbiertoYConLayout()
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                var creador = BuscarElemento(document.rootVisualElement, "creador");
                if (creador == null) continue;

                return creador.style.display.value == DisplayStyle.Flex &&
                       !float.IsNaN(creador.worldBound.width) &&
                       creador.worldBound.width > 100f;
            }
            return false;
        }

        private static VisualElement BuscarElemento(VisualElement element, string nombre)
        {
            if (element.name == nombre) return element;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = BuscarElemento(element[i], nombre);
                if (hijo != null) return hijo;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator ElHabitanteNuevoLlevaLaVozYLaRopaElegidas()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo),
                "nadie registró el censo al arrancar la isla");
            int antes = censo.All.Count;
            var idsAntes = new HashSet<string>();
            foreach (var vecino in censo.All) idsAntes.Add(vecino.Id);

            yield return AbrirCreador();

            var timbre = Boton("Aguda");
            Assert.That(timbre, Is.Not.Null,
                "el creador no ofrece el timbre «Aguda»: la voz no se puede elegir");
            yield return PulsarALaVista(timbre, "el timbre «Aguda»");

            var prenda = Boton("Gorra nimbo clasica");
            Assert.That(prenda, Is.Not.Null,
                "el creador no ofrece la «Gorra nimbo clasica», que está desbloqueada " +
                "desde el nivel 1: o no hay lista de ropa o el filtro se equivoca");
            yield return PulsarALaVista(prenda, "la «Gorra nimbo clasica»");

            yield return Pulsar(Boton("Listo"), "el botón «Listo» del creador");
            yield return null;
            yield return null;

            Assert.That(censo.All.Count, Is.EqualTo(antes + 1),
                "se cerró el creador y no llegó nadie al censo");

            IslanderData nuevo = null;
            foreach (var vecino in censo.All)
                if (!idsAntes.Contains(vecino.Id)) nuevo = vecino;
            Assert.That(nuevo, Is.Not.Null, "no encuentro al recién llegado en el censo");

            Assert.That(nuevo.Voice.Pitch, Is.EqualTo(VoicePitch.High),
                "el timbre pulsado no llegó al habitante: la voz elegida se quedó en " +
                $"el panel (salió con pitch {nuevo.Voice.Pitch})");
            Assert.That(nuevo.EquippedOutfit, Is.EqualTo("cloth_gorra_nimbo_clasica"),
                "la prenda pulsada no llegó al habitante: se la puso el sorteo o nadie");
            Assert.That(nuevo.Wardrobe, Does.Contain("cloth_gorra_nimbo_clasica"),
                "el habitante lleva la gorra pero no la posee: algún día la simulación " +
                "le cambiará la ropa y desaparecerá la elección del jugador");
        }

        [UnityTest]
        public IEnumerator SoloSeOfreceLaRopaQueEstaDesbloqueadaAlEmpezar()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IEconomyService>(out var economia),
                "nadie registró la economía: no hay catálogo contra el que comparar");

            // Lo que el jugador de nivel 1 puede ponerse, según el catálogo de verdad.
            var permitidas = new Dictionary<string, int>();
            foreach (var item in economia.ItemsOfCategory(ItemCategory.Clothing))
                permitidas[item.DisplayName] = item.UnlockLevel;

            yield return AbrirCreador();

            bool hayAlguna = false;
            foreach (var texto in TextosDeLaTarjetaDeRopa())
            {
                if (!permitidas.TryGetValue(texto, out int nivel)) continue;

                hayAlguna = true;
                Assert.That(nivel, Is.LessThanOrEqualTo(1),
                    $"el creador ofrece «{texto}», que se desbloquea en el nivel {nivel}: " +
                    "estrenar el juego con ropa bloqueada rompe la progresión");
            }

            Assert.That(hayAlguna, Is.True,
                "el creador no ofrece ni una sola prenda del catálogo: la tarjeta de " +
                "ropa está vacía o leyendo de otro sitio");
        }

        [UnityTest]
        public IEnumerator TocarUnTimbrePideUnaMuestraDeVoz()
        {
            // La muestra viaja por aviso hacia el sonido (Nimbo.Art no ve Nimbo.UI).
            // Aquí se comprueba el tramo que toca a esta capa: que tocar un timbre
            // pide oír ESA voz. Si nadie contesta todavía, la prueba lo deja visto —
            // el pedido existe— y el informe señala al que falta por suscribirse.
            //
            // El manejador vive en un campo y no en una función local: cada conversión
            // de una función local con cierre crea un delegado distinto, y el
            // Unsubscribe del finally dejaría de quitar al mismo que entró.
            _pedidos.Clear();
            _anotar = evt => _pedidos.Add(evt.Voice);
            EventBus.Subscribe<VoicePreviewRequested>(_anotar);
            try
            {
                yield return CargarYEmpezar();
                yield return AbrirCreador();

                int antes = _pedidos.Count;
                yield return PulsarALaVista(Boton("Muy grave"), "el timbre «Muy grave»");
                yield return null;

                Assert.That(_pedidos.Count, Is.GreaterThan(antes),
                    "tocar un timbre no pidió ninguna muestra: el jugador elegiría la " +
                    "voz sin oírla ni una vez, que es un menú de nombres");
                Assert.That(_pedidos[_pedidos.Count - 1].Pitch,
                    Is.EqualTo(VoicePitch.VeryLow),
                    "llegó una petición de muestra, pero no del timbre pulsado");
            }
            finally
            {
                EventBus.Unsubscribe<VoicePreviewRequested>(_anotar);
            }
        }

        private readonly List<VoiceConfig> _pedidos = new List<VoiceConfig>();
        private System.Action<VoicePreviewRequested> _anotar;

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>
        /// Trae el botón a la vista con el scroll del creador y lo pulsa con una
        /// secuencia real de puntero.
        /// </summary>
        /// <remarks>
        /// Las tarjetas nuevas viven al final del scroll, fuera de pantalla al abrir.
        /// Enviar el clic sin traerlo antes haría que lo recibiera lo que haya bajo
        /// esas coordenadas, que no es el botón. Y como en TiendasEnLaIslaTests: la
        /// reflexión sobre Clickable no dispara callbacks; hacen falta PointerMove,
        /// PointerDown y PointerUp con su fotograma entre cada uno.
        /// </remarks>
        private static IEnumerator PulsarALaVista(Button boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            var scroll = boton.GetFirstAncestorOfType<ScrollView>();
            if (scroll != null)
            {
                scroll.ScrollTo(boton);
                yield return null;
            }

            // Mismo cuidado que TemaEnLaIslaTests: sin layout no hay centro con el
            // que pulsar, y un clic a ciegas se lo lleva quien esté debajo.
            int frames = 0;
            while ((!LayoutValido(boton) ||
                    !boton.worldBound.Overlaps(boton.panel.visualTree.layout)) &&
                   frames++ < 120)
            {
                if (scroll != null && frames % 30 == 29) scroll.ScrollTo(boton);
                yield return null;
            }

            var centro = PuntoAlcanzable(boton, boton.panel.visualTree, queEra);
            Assert.That(boton.panel.Pick(centro), Is.SameAs(boton),
                $"{queEra} no es lo que el panel señala bajo el punto pulsado: algo lo " +
                "tapa o el scroll no lo trajo del todo, y el clic del jugador llegaría " +
                "a otra cosa");

            yield return Pulsar(boton, queEra);
        }

        /// <summary>
        /// El punto del botón que el panel deja pulsar de verdad: el centro de la
        /// intersección entre el botón y el área del propio panel.
        /// </summary>
        /// <remarks>
        /// La fila de acciones desborda el panel por la derecha y «Nuevo habitante»,
        /// que es la última, queda con su centro fuera del área utilizable — medido
        /// en batchmode: botón en x=1397..1575 sobre un panel de 1440. Un clic ahí
        /// no llega a nadie aunque la consulta geométrica lo encuentre. Al jugador
        /// solo le queda pulsar la franja visible, y esta prueba hace lo mismo que
        /// él. Si un botón quedara entero fuera, se cae aquí con el motivo y no con
        /// un clic perdido sin explicación.
        /// </remarks>
        private static Vector2 PuntoAlcanzable(Button boton, VisualElement raiz,
                                               string queEra)
        {
            var b = boton.worldBound;
            var panel = raiz.layout;

            float xMin = Mathf.Max(b.xMin, panel.xMin);
            float yMin = Mathf.Max(b.yMin, panel.yMin);
            float xMax = Mathf.Min(b.xMax, panel.xMax);
            float yMax = Mathf.Min(b.yMax, panel.yMax);

            Assert.That(xMax > xMin && yMax > yMin, Is.True,
                $"{queEra} queda entero fuera del área del panel (botón {b}, panel " +
                $"{panel}): nadie puede pulsarlo, ni el jugador ni esta prueba");

            return new Vector2((xMin + xMax) * 0.5f, (yMin + yMax) * 0.5f);
        }

        private static bool LayoutValido(VisualElement elemento)
        {
            float ancho = elemento.worldBound.width;
            return !float.IsNaN(ancho) && ancho > 0f;
        }

        private static IEnumerator Pulsar(Button boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            var raiz = boton.panel.visualTree;
            var centro = PuntoAlcanzable(boton, raiz, queEra);

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
            if (element is Button button && button.text == texto &&
                button.enabledInHierarchy) return button;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = Buscar(element[i], texto);
                if (hijo != null) return hijo;
            }
            return null;
        }

        /// <summary>
        /// Los textos de los botones de la tarjeta de ropa del creador.
        /// </summary>
        /// <remarks>
        /// Solo esa tarjeta y no «todos los botones del juego»: hay más paneles
        /// montados fuera de pantalla —la tienda, el menú de inicio— cuyos botones
        /// siguen activos para una búsqueda por texto aunque nadie los vea, y una
        /// fila de tienda que se llamara como una prenda bloqueada sería un falso
        /// positivo ajeno a lo que el creador ofrece.
        /// </remarks>
        private static List<string> TextosDeLaTarjetaDeRopa()
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                var tarjeta = BuscarElemento(document.rootVisualElement, "creador-ropa");
                if (tarjeta == null) continue;

                Recorrer(tarjeta, textos);
            }
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text) &&
                button.enabledInHierarchy)
                textos.Add(button.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
