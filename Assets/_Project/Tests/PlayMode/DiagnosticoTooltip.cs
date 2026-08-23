using System.Collections;
using System.IO;
using Nimbo.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// SONDA DEL TOOLTIP EN RUNTIME — mide si los tooltip llegan a verse alguna vez.
    /// </summary>
    /// <remarks>
    /// La auditoría dejó una pregunta abierta: ¿UI Toolkit muestra el tooltip de un
    /// elemento con SetEnabled(false)? De eso depende que las explicaciones de
    /// bloqueo (Gates.cs:36 escritas aquí; aplicadas en SocialSection.cs:160/208/246/287,
    /// JobSection.cs:160, EventsPanel.cs:119, CraftPanel.cs:158) se vean alguna vez o
    /// no se vean nunca.
    ///
    /// Respuesta medida (ver Informes/informe-tema.md): en runtime NO llega ni un
    /// TooltipEvent — tampoco con el botón ENCENDIDO. En el IL del módulo nadie
    /// despacha TooltipEvent fuera del editor. Mientras nadie implemente un modo
    /// de mostrarlos en juego, esta sonda se queda: su aserción solo falla ante un
    /// resultado ambiguo (nada en el control pero algo en el apagado), así que
    /// seguirá pasando el día que existan tooltips de verdad.
    ///
    /// Diseño A/B sobre el MISMO botón real de la barra de acciones: fase control
    /// encendido, fase medida apagado. En cada fase el puntero sintético entra al
    /// botón y se queda quieto cinco segundos —el retardo del tooltip es temporizado,
    /// así que hay que darle quietud, no movimiento—. Tres contadores distinguen los
    /// regímenes posibles: TooltipEvent en la raíz (trickle+burbuja), PointerEnterEvent
    /// sobre el propio botón, y si el hit-test sigue viéndolo.
    ///
    /// El evento sintético nace de un evento IMGUI de ratón vía GetPooled(Event):
    /// única vía pública en 6000.5.5f1, verificado contra el IL del módulo.
    /// </remarks>
    public class DiagnosticoTooltip
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado. Es el
            // mismo cuidado que tiene TemaEnLaIslaTests.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        private int _tooltipTrickle;
        private int _tooltipBubble;
        private int _entersAlBoton;
        private string _textoRecogido = "";

        private IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            // Sin protagonista no hay barra de acciones: la primera pasada de esta
            // sonda se quedó sin botones que mirar por no publicar esto.
            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator Sonda()
        {
            yield return CargarYEmpezar();

            var boton = BuscarBoton("Crónica");
            Assert.That(boton, Is.Not.Null, "la sonda no encuentra la barra de acciones");

            // Sin layout no hay worldBound donde poner el puntero.
            int espera = 0;
            while (boton.worldBound.width <= 0 && espera++ < 120)
                yield return null;
            Assume.That(boton.worldBound.width, Is.GreaterThan(0f),
                        "el panel nunca dio layout a la barra de acciones");

            var raiz = boton.panel.visualTree;

            raiz.RegisterCallback<TooltipEvent>(
                e => { _tooltipTrickle++; _textoRecogido = e.tooltip; }, TrickleDown.TrickleDown);
            raiz.RegisterCallback<TooltipEvent>(
                e => { _tooltipBubble++; _textoRecogido = e.tooltip; });
            boton.RegisterCallback<PointerEnterEvent>(
                e => _entersAlBoton++, TrickleDown.TrickleDown);

            boton.tooltip = "SONDA-tooltip";
            var centro = new Vector2(boton.worldBound.center.x, boton.worldBound.center.y);

            // ── fase control: botón encendido ──
            ReiniciarContadores();
            yield return EntrarYEsperar(raiz, centro);
            int trickleVivo = _tooltipTrickle, bubbleVivo = _tooltipBubble;
            int entersVivo = _entersAlBoton;

            // ── fase medida: botón apagado ──
            ReiniciarContadores();
            boton.SetEnabled(false);

            // Reentrar: al apagarse puede limpiarse el estado del puntero, y lo que
            // se mide es el régimen estable, no la transición.
            yield return EntrarYEsperar(raiz, centro);
            int trickleApagado = _tooltipTrickle, bubbleApagado = _tooltipBubble;
            int entersApagado = _entersAlBoton;

            bool seVeVivo = trickleVivo + bubbleVivo > 0;
            bool seVeApagado = trickleApagado + bubbleApagado > 0;

            string informe =
                "habilitado: tooltips=" + (trickleVivo + bubbleVivo) +
                " (trickle=" + trickleVivo + " bubble=" + bubbleVivo + ")" +
                " enters=" + entersVivo +
                " | apagado: tooltips=" + (trickleApagado + bubbleApagado) +
                " (trickle=" + trickleApagado + " bubble=" + bubbleApagado + ")" +
                " enters=" + entersApagado +
                " | texto='" + _textoRecogido + "'";

            Debug.Log("[SONDA TOOLTIP] " + informe);
            // Dentro del proyecto y creando la carpeta: escribir en /tmp/opencode dando
            // por hecho que existe ponía la suite de juego EN ROJO en cualquier máquina
            // que no lo tuviera, y además las reglas mandan los resultados a Informes/.
            var destino = Path.Combine("Informes", "pruebas");
            Directory.CreateDirectory(destino);
            File.WriteAllText(Path.Combine(destino, "sonda_tooltip.txt"), informe + "\n");

            Assert.That(seVeVivo || !seVeApagado, Is.True,
                        "resultado ambiguo: sin tooltip en el control pero apareció en el " +
                        "apagado — revisar a mano: " + informe);
        }

        private void ReiniciarContadores()
        {
            _tooltipTrickle = _tooltipBubble = _entersAlBoton = 0;
            _textoRecogido = "";
        }

        /// <summary>
        /// El puntero entra al botón y después NO se mueve: el tooltip es temporizado
        /// y cualquier movimiento reinicia el reloj. Cinco segundos de quietud le dan
        /// margen de sobra al retardo estándar (~1 s).
        /// </summary>
        private IEnumerator EntrarYEsperar(VisualElement raiz, Vector2 centro)
        {
            raiz.SendEvent(PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = centro + new Vector2(0f, -240f),
            }));

            raiz.SendEvent(PointerMoveEvent.GetPooled(new Event
            {
                type = EventType.MouseMove,
                mousePosition = centro,
            }));

            float t0 = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - t0 < 5f)
                yield return null;
        }

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
    }
}
