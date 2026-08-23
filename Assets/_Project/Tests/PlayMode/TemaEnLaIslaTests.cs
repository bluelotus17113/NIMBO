using System.Collections;
using Nimbo.Core.Events;
using Nimbo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que los estados de los botones existan **en la isla**, no solo en la hoja.
    /// </summary>
    /// <remarks>
    /// El fallo original fue colectivo: UiTheme pintaba fondo y borde inline, el
    /// inline gana a cualquier pseudoclase en UI Toolkit, y el :hover, :active y
    /// :focus del tema estaban muertos para toda la interfaz. La prueba de editor
    /// (TemaBotonesTests) vigila la estructura —clase sí, inline no—; esta mide lo
    /// que la auditoría pidió de verdad: que un botón real de la barra de acciones
    /// reacciona al puntero, se hunde al pulsarlo y enseña el foco de teclado,
    /// leído del resolved style y de los pseudosestados después de dejar al panel
    /// recalcular.
    ///
    /// Los eventos son sintéticos y su construcción no es la obvia: en 6000.5.5f1
    /// los Pointer*Event NO tienen un GetPooled(posición,…) público (verificado en
    /// el IL del módulo), así que cada evento nace de su equivalente IMGUI de ratón
    /// vía PointerMoveEvent.GetPooled(UnityEngine.Event). No hace falta backend de
    /// entrada ninguno: corren en batchmode sin tocar ajustes globales del proyecto.
    /// Prueban menos que un ratón físico —no hay driver de por medio— y esa
    /// limitación queda dicha en el informe.
    /// </remarks>
    public class TemaEnLaIslaTests
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
        }

        [UnityTest]
        public IEnumerator LosBotonesDeLaBarraLlevanLaClaseDelTema()
        {
            yield return CargarYEmpezar();

            var (total, conClase) = ContarBotones();

            Assert.That(total, Is.GreaterThan(0), "no hay ni un botón en pantalla");
            Assert.That(conClase, Is.EqualTo(total),
                        $"{total - conClase} de {total} botones no pasan por el tema: " +
                        "si mañana alguien vuelve a fabricarse un Button a mano, el tema " +
                        "no lo viste y sus estados no existen");

            var cronica = BuscarBoton("Crónica");
            Assert.That(cronica, Is.Not.Null, "no encuentro «Crónica» en la barra de acciones");
            Assert.That(cronica.ClassListContains(UiTheme.ClassAction), Is.True,
                        $"«Crónica» no lleva {UiTheme.ClassAction}");
        }

        [UnityTest]
        public IEnumerator ElBotonReaccionaAlPuntero()
        {
            yield return CargarYEmpezar();

            var boton = BuscarBoton("Crónica");
            Assert.That(boton, Is.Not.Null, "no hay barra de acciones");
            var raiz = boton.panel.visualTree;

            yield return EsperarLayout(boton);
            yield return ApartarPuntero(raiz, boton);
            var reposo = boton.resolvedStyle.backgroundColor;
            Assert.That(CoincideConTema(UiTheme.Peach, reposo), Is.True,
                        $"el botón está en reposo con fondo {reposo} y el tema pide " +
                        $"Peach ({UiTheme.Peach}): o el inline volvió a pisar la clase, " +
                        "o el tema no llega al panel");

            MoverPuntero(raiz, Centro(boton));
            yield return EsperarHasta(() => boton.hasHoverPseudoState);

            Assert.That(boton.hasHoverPseudoState, Is.True,
                        "el panel no marcó el botón como hover con el puntero encima");
            Assert.That(CoincideConTema(UiTheme.PeachDeep, boton.resolvedStyle.backgroundColor),
                        Is.True,
                        $"con el puntero encima el fondo es {boton.resolvedStyle.backgroundColor} " +
                        $"y el tema pide PeachDeep ({UiTheme.PeachDeep}): el :hover del tema " +
                        "no se aplica");

            MoverPuntero(raiz, Fuera(boton));
            yield return EsperarHasta(() => !boton.hasHoverPseudoState);

            Assert.That(Cerca(boton.resolvedStyle.backgroundColor, reposo), Is.True,
                        "al quitar el puntero el botón no vuelve a su color de reposo: " +
                        "el :hover se quedó pegado");
        }

        [UnityTest]
        public IEnumerator ElBotonSeHundeMientrasEstaPulsado()
        {
            yield return CargarYEmpezar();

            var boton = BuscarBoton("Crónica");
            Assert.That(boton, Is.Not.Null, "no hay barra de acciones");
            var raiz = boton.panel.visualTree;

            yield return EsperarLayout(boton);
            yield return ApartarPuntero(raiz, boton);
            var reposo = boton.resolvedStyle.backgroundColor;
            var centro = Centro(boton);

            // Primero el puntero llega al botón y después se aprieta: igual que una
            // mano de verdad. Pulsar desde fuera sin mover no es una secuencia real.
            MoverPuntero(raiz, centro);
            yield return EsperarHasta(() => boton.hasHoverPseudoState);

            raiz.SendEvent(Aprieta(centro));
            yield return EsperarHasta(() => boton.hasActivePseudoState);

            Assert.That(boton.hasActivePseudoState, Is.True,
                        "el panel no marcó el botón como pulsado con el botón izquierdo abajo");
            Assert.That(CoincideConTema(UiTheme.PeachDeep, boton.resolvedStyle.backgroundColor),
                        Is.True,
                        $"pulsado, el fondo es {boton.resolvedStyle.backgroundColor}: el " +
                        ":active del tema no se aplica");

            raiz.SendEvent(Suelta(centro));
            yield return EsperarHasta(() => !boton.hasActivePseudoState);

            Assert.That(boton.hasActivePseudoState, Is.False,
                        "al soltar el :active sigue puesto: se quedó pegado el hundido");
            Assert.That(Cerca(boton.resolvedStyle.backgroundColor, reposo) ||
                        CoincideConTema(UiTheme.PeachDeep, boton.resolvedStyle.backgroundColor),
                        Is.True,
                        "al soltar, el fondo no es ni reposo ni hover: estado residual raro");

            MoverPuntero(raiz, Fuera(boton));
            yield return EsperarHasta(() => !boton.hasHoverPseudoState);
            Assert.That(Cerca(boton.resolvedStyle.backgroundColor, reposo), Is.True,
                        "tras soltar y apartar el puntero el botón no vuelve al reposo");
        }

        [UnityTest]
        public IEnumerator ElFocoDeTecladoSeVe()
        {
            yield return CargarYEmpezar();

            var boton = BuscarBoton("Crónica");
            Assert.That(boton, Is.Not.Null, "no hay barra de acciones");

            yield return EsperarLayout(boton);

            boton.Focus();
            yield return EsperarHasta(() =>
                ReferenceEquals(boton.focusController.focusedElement, boton));

            Assert.That(ReferenceEquals(boton.focusController.focusedElement, boton),
                        Is.True,
                        "Focus() no dejó el foco en el botón: con mando o teclado no hay " +
                        "por dónde entrar a la interfaz");
            Assert.That(boton.hasFocusPseudoState, Is.True,
                        "el elemento tiene el foco pero el panel no pinta el pseudosestado");

            var anillo = boton.resolvedStyle.borderTopColor;
            Assert.That(CoincideConTema(UiTheme.Ink, anillo), Is.True,
                        $"el anillo de foco es {anillo} y el tema pide tinta ({UiTheme.Ink}): " +
                        "jugando sin ratón nadie sabe dónde está");
            Assert.That(boton.resolvedStyle.borderTopWidth, Is.EqualTo(2f).Within(0.5f),
                        "el borde de foco no está siempre presente a 2 px: si solo se " +
                        "pusiera al enfocar, el texto daría un salto justo cuando el " +
                        "jugador mira al teclado");
        }

        [UnityTest]
        public IEnumerator ElApagadoNoPrometeLoQueNoPuedeCumplir()
        {
            yield return CargarYEmpezar();

            var boton = BuscarBoton("Crónica");
            Assert.That(boton, Is.Not.Null, "no hay barra de acciones");
            var raiz = boton.panel.visualTree;

            // El apagado se monta con la fábrica sobre la interfaz viva: lo que se
            // prueba es fábrica + clase + tema sobre un panel de verdad. Los apagados
            // de los flujos reales (CraftPanel.cs:158, SocialSection.cs:156) viven
            // detrás de pantallas que exigen condiciones concretas; esto ejercita
            // exactamente lo mismo sin acoplarse a ninguna. Va a la fila de la barra
            // y no a la raíz del panel porque ahí es donde viven los botones de
            // verdad: colgado de la raíz su primer layout salió con ancho NaN y no
            // hay worldBound donde poner el puntero.
            var apagado = UiTheme.Disabled("prueba apagada");
            boton.parent.Add(apagado);
            yield return EsperarLayout(apagado);
            yield return ApartarPuntero(raiz, apagado);

            var reposo = apagado.resolvedStyle.backgroundColor;
            Assert.That(CoincideConTema(UiTheme.CreamDeep, reposo), Is.True,
                        $"el apagado descansa con fondo {reposo} y el tema pide " +
                        $"CreamDeep ({UiTheme.CreamDeep})");

            MoverPuntero(raiz, Centro(apagado));

            // Aquí no hay condición que esperar: lo que se comprueba es que NADA
            // llegue. Unos cuantos fotogramas dan al panel tiempo de equivocarse.
            for (int i = 0; i < 10; i++) yield return null;

            Assert.That(apagado.enabledSelf, Is.False, "el apagado se ha encendido solo");
            Assert.That(apagado.hasHoverPseudoState, Is.False,
                        "el apagado recibe :hover bajo el puntero: promete que se puede " +
                        "pulsar y no se puede");
            Assert.That(Cerca(apagado.resolvedStyle.backgroundColor, reposo), Is.True,
                        $"el apagado cambia de fondo bajo el puntero ({reposo} → " +
                        $"{apagado.resolvedStyle.backgroundColor}): promete pulsarse");

            apagado.RemoveFromHierarchy();
        }

        // ── aparejo ───────────────────────────────────────────────────────────

        private static Button BuscarBoton(string texto)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
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

        private static (int total, int conClase) ContarBotones()
        {
            int total = 0, conClase = 0;

            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Contar(document.rootVisualElement);
            }
            return (total, conClase);

            void Contar(VisualElement element)
            {
                if (element is Button)
                {
                    total++;
                    if (element.ClassListContains(UiTheme.ClassAction)
                        || element.ClassListContains(UiTheme.ClassSecondary)
                        || element.ClassListContains(UiTheme.ClassDisabled)) conClase++;
                }
                for (int i = 0; i < element.childCount; i++) Contar(element[i]);
            }
        }

        /// <summary>Hasta que el panel le reparta sitio: sin layout no hay worldBound.</summary>
        /// <remarks>
        /// Un ancho NaN cuenta como sin layout: es el valor que dio un botón colgado
        /// de la raíz antes de su primera pasada de medida, y leerlo como válido
        /// rompería todo lo que viene después.
        /// </remarks>
        private static IEnumerator EsperarLayout(VisualElement elemento)
        {
            int frames = 0;
            while (!LayoutValido(elemento) && frames++ < 120)
                yield return null;
            Assume.That(LayoutValido(elemento), Is.True,
                        "el panel nunca llegó a dar layout al elemento");
        }

        private static bool LayoutValido(VisualElement elemento)
        {
            float ancho = elemento.worldBound.width;
            return !float.IsNaN(ancho) && ancho > 0f;
        }

        /// <summary>Espera activa por fotogramas a que el panel resuelva un cambio.</summary>
        private static IEnumerator EsperarHasta(System.Func<bool> condicion)
        {
            int frames = 0;
            while (!condicion() && frames++ < 60)
                yield return null;
        }

        /// <summary>
        /// El puntero sintético se construye desde un evento IMGUI de ratón.
        /// </summary>
        /// <remarks>
        /// En 6000.5.5f1 los Pointer*Event no tienen ningún GetPooled con coordenadas
        /// público —los sobrecargos con posición son internal— y MouseEventBase no
        /// implementa IPointerEvent, así que no hay de donde derivarlos por vía
        /// pública. La única puerta abierta es GetPooled(UnityEngine.Event), que
        /// acepta eventos IMGUI de ratón y copia posición, botones y pointerId de
        /// ratón hacia el evento de puntero. Todo verificado contra el IL del módulo;
        /// si esto dejara de compilar o de despachar, es que Unity movió la API y las
        /// tres fábricas de abajo son lo único que hay que rehacer.
        /// </remarks>
        private static PointerMoveEvent MoverA(Vector2 posicion) =>
            PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = posicion,
            });

        private static PointerDownEvent Aprieta(Vector2 posicion) =>
            PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown,
                mousePosition = posicion,
                button = 0,
            });

        private static PointerUpEvent Suelta(Vector2 posicion) =>
            PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp,
                mousePosition = posicion,
                button = 0,
            });

        private static void MoverPuntero(VisualElement raiz, Vector2 posicion) =>
            raiz.SendEvent(MoverA(posicion));

        private static Vector2 Centro(VisualElement elemento) =>
            new Vector2(elemento.worldBound.center.x, elemento.worldBound.center.y);

        /// <summary>Un punto bien lejos, dentro del panel pero fuera del elemento.</summary>
        private static Vector2 Fuera(VisualElement elemento) =>
            Centro(elemento) + new Vector2(0f, -elemento.worldBound.height * 4f - 40f);

        /// <summary>
        /// Deja el botón sin hover antes de leer su color de reposo.
        /// </summary>
        /// <remarks>
        /// La posición del puntero sintético vive en PointerDeviceState, que es
        /// global y sobrevive a recargar la escena: si la prueba anterior dejó el
        /// puntero encima de donde ahora está este botón, arranca ya con hover y
        /// cualquier lectura de «reposo» mide en realidad el estado encendido.
        /// </remarks>
        private static IEnumerator ApartarPuntero(VisualElement raiz, VisualElement boton)
        {
            MoverPuntero(raiz, Fuera(boton));
            yield return EsperarHasta(() => !boton.hasHoverPseudoState);
        }

        /// <summary>Tolerancia de comparación de colores resueltos.</summary>
        private const float Tolerancia = 0.02f;

        private static bool Cerca(Color esperado, Color resuelto) =>
            Mathf.Abs(esperado.r - resuelto.r) < Tolerancia
            && Mathf.Abs(esperado.g - resuelto.g) < Tolerancia
            && Mathf.Abs(esperado.b - resuelto.b) < Tolerancia;

        /// <summary>
        /// El color del tema puede volver del resolved style en sRGB o en lineal
        /// según el espacio de color del proyecto: se acepta cualquiera de las dos
        /// formas porque lo que se vigila es qué regla ganó, no cómo la guarde
        /// esta versión de Unity.
        /// </summary>
        private static bool CoincideConTema(Color tema, Color resuelto) =>
            Cerca(tema, resuelto) || Cerca(tema.linear, resuelto);
    }
}
