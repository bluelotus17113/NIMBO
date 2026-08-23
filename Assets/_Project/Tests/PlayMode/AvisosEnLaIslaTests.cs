using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que las explicaciones de bloqueo tengan un canal real **en el juego**.
    /// </summary>
    /// <remarks>
    /// Siete sitios escriben por qué algo está cerrado (Gates.cs:36 y los .tooltip de
    /// SocialSection, JobSection, EventsPanel y CraftPanel) y hasta ahora nadie podía
    /// leerlos: medido en Informes/informe-tema.md §4, en runtime no se despacha ni
    /// un TooltipEvent. La franja (Nimbo.UI.GateNotice) es el canal.
    ///
    /// Lo que se exige aquí es lo que el tooltip no cumplía: que la explicación se
    /// alcance **sin puntero**. En la primera prueba el único gesto sintético va a un
    /// botón encendido (abrir la ficha de un vecino); el botón bloqueado no recibe
    /// ni un evento, y aun así la franja acaba diciendo lo que a él le escribieron.
    /// En la segunda se deja el puntero aparcado en una esquina y se comprueba,
    /// además, por qué no valdría un canal por foco: un botón apagado no puede
    /// tomarlo — y aun sin foco y sin hover, la explicación llega igual.
    ///
    /// El otro cuidado que no se puede romper: el botón bloqueado sigue viéndose
    /// bloqueado. Se comprueba sobre el elemento real de la ficha: sigue apagado,
    /// sigue con su clase de apagado y nunca recibe hover.
    /// </remarks>
    public class AvisosEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado. Es
            // el mismo cuidado que tiene CronicaEnLaIslaTests.
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
        public IEnumerator LaExplicacionDeUnBotonBloqueadoSeLeeSinPuntero()
        {
            yield return CargarYEmpezar();

            // ── abrir una ficha con UN clic, y sobre un botón encendido ──
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo),
                          "nadie registró el censo: no hay ficha que abrir");
            Assert.That(censo.Count, Is.GreaterThan(0), "la isla salió sin vecinos");

            var vecino = censo.All[0];
            var botonVecino = BuscarBoton(vecino.Identity.ShortName);
            Assert.That(botonVecino, Is.Not.Null,
                        $"no hay botón para abrir la ficha de {vecino.Identity.ShortName}");
            yield return EsperarLayout(botonVecino);
            Pulsar(botonVecino.panel.visualTree, Centro(botonVecino));
            yield return null;

            // ── dentro hay un botón bloqueado de verdad ──
            var bloqueado = BuscarBloqueadoConExplicacion();
            Assert.That(bloqueado, Is.Not.Null,
                        "con las vías a cero la ficha debería traer algún gesto " +
                        "cerrado: Halagar pide Convivencia 2 y el jugador va por 0");
            Assert.That(bloqueado.enabledSelf, Is.False,
                        "lo que la prueba mira no está apagado de verdad");
            Assert.That(bloqueado.ClassListContains(UiTheme.ClassDisabled), Is.True,
                        "el botón bloqueado perdió su clase de apagado: dejaría de " +
                        "verse bloqueado, y eso no se puede romper");

            string explicacion = bloqueado.tooltip;
            Assert.That(explicacion, Is.Not.Empty,
                        "el botón bloqueado llegó sin explicación escrita");

            var todas = RecogerExplicacionesBloqueadas();
            Assert.That(todas, Does.Contain(explicacion));

            // ── la franja se entera sola, sin recibir ni un evento del bloqueado ──
            var franjas = Franjas();
            Assert.That(franjas, Is.Not.Empty,
                        "la franja de avisos no llegó a colgarse de la interfaz");

            // El pase de la franja corre cada 0,25 s de tiempo REAL, y en batchmode
            // los fotogramas cuestan casi nada: esperar fotogramas aquí sería
            // esperar décimas de segundo. Se espera en tiempo real, como la sonda
            // del tooltip, con margen de sobra para varios pases.
            float t0 = Time.realtimeSinceStartup;
            while (!FranjaDiceAlgoDe(todas) && Time.realtimeSinceStartup - t0 < 8f)
                yield return null;

            Assert.That(AlgunaFranjaEncendida(), Is.True,
                        "la franja no se encendió: la explicación siguió sin canal. " +
                        "Estado: todas=" + todas.Count + "; " + VolcadoDocumentos());
            Assert.That(FranjaDiceAlgoDe(todas), Is.True,
                        $"la franja dice «{TextoDeFranja()}» y ninguna explicación " +
                        $"bloqueada en pantalla empieza así ({string.Join(" | ", todas)})");

            // Y el bloqueado sigue siéndolo después de todo esto.
            Assert.That(bloqueado.enabledSelf, Is.False,
                        "enseñar la explicación encendió el botón");
            Assert.That(bloqueado.hasHoverPseudoState, Is.False,
                        "algo dejó el puntero encima del bloqueado: volvió a depender " +
                        "del hover, que es justo lo que se venía a arreglar");
        }

        [UnityTest]
        public IEnumerator NiElFocoNiElHoverHacenFaltaParaLeerlo()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            var vecino = censo.All[0];
            var botonVecino = BuscarBoton(vecino.Identity.ShortName);
            Assume.That(botonVecino, Is.Not.Null, "no hay barra de habitantes");
            yield return EsperarLayout(botonVecino);
            Pulsar(botonVecino.panel.visualTree, Centro(botonVecino));
            yield return null;

            var bloqueado = BuscarBloqueadoConExplicacion();
            Assume.That(bloqueado, Is.Not.Null, "no hay gesto cerrado en la ficha");

            // Puntero aparcado en una esquina, lejos de la ficha y del bloqueado.
            var raiz = bloqueado.panel.visualTree;
            raiz.SendEvent(MoverA(new Vector2(6f, 6f)));
            yield return null;

            // Y foco pedido al apagado: si UI Toolkit lo concediera, un canal pegado
            // al botón por foco sería viable y habría que replantear el diseño.
            bloqueado.Focus();
            yield return null;

            Assert.That(raiz.focusController.focusedElement, Is.Not.EqualTo(bloqueado),
                        "un botón apagado tomó el foco: un canal pegado al botón por " +
                        "foco sería viable y esta franja tendría que replantearse");
            Assert.That(bloqueado.hasHoverPseudoState, Is.False,
                        "el bloqueado quedó bajo el puntero aparcado");

            var todas = RecogerExplicacionesBloqueadas();
            Assert.That(todas, Is.Not.Empty,
                        "la ficha se cerró sola antes de la espera: no queda ninguna " +
                        "explicación visible a quien la franja pueda leer");

            // Tiempo real y no fotogramas: el pase de la franja es temporizado y en
            // batchmode los fotogramas corren miles por segundo (mismo cuidado que
            // DiagnosticoTooltip con sus cinco segundos de quietud).
            float t0 = Time.realtimeSinceStartup;
            while (!FranjaDiceAlgoDe(todas) && Time.realtimeSinceStartup - t0 < 8f)
                yield return null;

            string estado = $"todas={todas.Count} [{string.Join(" | ", todas)}]; " +
                            $"franjas={Franjas().Count}";
            foreach (var franja in Franjas())
                estado += $" [display={franja.style.display.value}, " +
                          $"texto=«{TextoDe(franja)}»]";
            estado += "; " + VolcadoDocumentos();

            Assert.That(FranjaDiceAlgoDe(todas), Is.True,
                        "sin hover y sin foco la explicación no llegó: el canal exige " +
                        "algún gesto, y con teclado o mando no lo hay hoy. Estado: " + estado);
        }

        // ── aparejo ───────────────────────────────────────────────────────────

        private static Button BuscarBoton(string texto)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                var encontrado = BuscarBoton(document.rootVisualElement, texto);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        private static Button BuscarBoton(VisualElement element, string texto)
        {
            if (element is Button button && button.text == texto) return button;
            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = BuscarBoton(element[i], texto);
                if (hijo != null) return hijo;
            }
            return null;
        }

        /// <summary>El primer elemento visible, apagado y con explicación escrita.</summary>
        private static VisualElement BuscarBloqueadoConExplicacion()
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                var encontrado = BuscarBloqueado(document.rootVisualElement);
                if (encontrado != null) return encontrado;
            }
            return null;
        }

        private static VisualElement BuscarBloqueado(VisualElement element)
        {
            if (element.enabledSelf == false &&
                element.resolvedStyle.display != DisplayStyle.None &&
                !string.IsNullOrWhiteSpace(element.tooltip))
                return element;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = BuscarBloqueado(element[i]);
                if (hijo != null) return hijo;
            }
            return null;
        }

        /// <summary>Todas las explicaciones visibles de elementos apagados, en orden.</summary>
        private static List<string> RecogerExplicacionesBloqueadas()
        {
            var todas = new List<string>();
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Recoger(document.rootVisualElement, todas);
            }
            return todas;
        }

        private static void Recoger(VisualElement element, List<string> todas)
        {
            if (element.resolvedStyle.display == DisplayStyle.None) return;

            if (element.enabledSelf == false && !string.IsNullOrWhiteSpace(element.tooltip))
                todas.Add(element.tooltip);

            for (int i = 0; i < element.childCount; i++)
                Recoger(element[i], todas);
        }

        /// <summary>
        /// Todas las franjas montadas. Hay una por UIDocument — la escena Isla trae
        /// dos, juego y menú — y solo la del árbol con explicaciones se enciende.
        /// </summary>
        private static List<VisualElement> Franjas()
        {
            var franjas = new List<VisualElement>();
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                foreach (var franja in document.rootVisualElement.Query(Nimbo.UI.GateNotice.RootName).ToList())
                    franjas.Add(franja);
            }
            return franjas;
        }

        private static bool AlgunaFranjaEncendida()
        {
            foreach (var franja in Franjas())
                if (franja.style.display.value == DisplayStyle.Flex) return true;
            return false;
        }

        /// <summary>Por documento: display de la raíz, nodos y tooltips en total.</summary>
        private static string VolcadoDocumentos()
        {
            var partes = new List<string>();
            foreach (var document in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                var root = document.rootVisualElement;
                if (root == null) { partes.Add("(doc sin raíz)"); continue; }

                int nodos = 0, tooltips = 0;
                Contar(root, ref nodos, ref tooltips);
                partes.Add($"raíz[display={root.resolvedStyle.display}, nodos={nodos}, " +
                           $"tooltips={tooltips}]");
            }
            return string.Join(" ", partes);
        }

        private static void Contar(VisualElement element, ref int nodos, ref int tooltips)
        {
            nodos++;
            if (!string.IsNullOrWhiteSpace(element.tooltip)) tooltips++;
            for (int i = 0; i < element.childCount; i++)
                Contar(element[i], ref nodos, ref tooltips);
        }

        private static string TextoDeFranja()
        {
            foreach (var franja in Franjas())
            {
                string texto = TextoDe(franja);
                if (!string.IsNullOrEmpty(texto)) return texto;
            }
            return "";
        }

        private static string TextoDe(VisualElement franja)
        {
            for (int i = 0; i < franja.childCount; i++)
                if (franja[i] is Label label && !string.IsNullOrEmpty(label.text))
                    return label.text;
            return "";
        }

        private static bool FranjaDiceAlgoDe(List<string> explicaciones)
        {
            string leido = TextoDeFranja();
            if (string.IsNullOrEmpty(leido)) return false;

            // La franja añade « · N más» cuando hay varias; basta con que empiece
            // por una explicación real de las que hay en pantalla.
            foreach (var explicacion in explicaciones)
                if (leido.StartsWith(explicacion)) return true;
            return false;
        }

        /// <summary>Hasta que el panel le reparta sitio: sin layout no hay worldBound.</summary>
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

        /// <summary>
        /// El gesto completo de pulsar: mover, apretar y soltar sobre el centro.
        /// Mismas fábricas que TemaEnLaIslaTests: en 6000.5.5f1 la única vía pública
        /// para eventos de puntero sintéticos es GetPooled(UnityEngine.Event).
        /// </summary>
        private static void Pulsar(VisualElement raiz, Vector2 centro)
        {
            raiz.SendEvent(PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = centro,
            }));
            raiz.SendEvent(PointerDownEvent.GetPooled(new Event
            {
                type = EventType.MouseDown,
                mousePosition = centro,
                button = 0,
            }));
            raiz.SendEvent(PointerUpEvent.GetPooled(new Event
            {
                type = EventType.MouseUp,
                mousePosition = centro,
                button = 0,
            }));
        }

        private static PointerMoveEvent MoverA(Vector2 posicion) =>
            PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = posicion,
            });

        private static Vector2 Centro(VisualElement elemento) =>
            new Vector2(elemento.worldBound.center.x, elemento.worldBound.center.y);
    }
}
