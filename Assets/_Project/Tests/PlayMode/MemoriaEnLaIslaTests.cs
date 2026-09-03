using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que el vecino te mencione lo de estos días **en el juego**: abriendo su ficha
    /// como se abre de verdad y pulsando Charlar como se pulsa de verdad.
    /// </summary>
    /// <remarks>
    /// El verificador aprobó la memoria conversacional con una coletilla que es el
    /// motivo de existir de esta clase: «NO llega al jugador todavía». El recuerdo
    /// estaba escrito, tenía trece pruebas de editor en verde, y no había forma
    /// humana de oírlo — <c>SocialSection</c> hacía <c>Say("")</c> al charlar bien,
    /// es decir, borraba el aviso. La conversación era muda por construcción.
    ///
    /// Es exactamente la enfermedad de §18 y por eso la prueba tiene que ser de
    /// juego. Una de editor sobre <c>ConversationRecall</c> pasaba ayer y seguiría
    /// pasando hoy con la interfaz desconectada.
    ///
    /// **Por qué se abre la ficha con el botón y no con <c>new IslanderPanel()</c>.**
    /// Las otras pruebas de la ficha construyen el panel a mano y leen sus etiquetas,
    /// que basta cuando lo que se comprueba es lo que pinta. Aquí hay que **pulsar**,
    /// y un botón sin panel de verdad no despacha eventos: haría falta invocar el
    /// <c>Clickable</c> por reflexión, que —lo aprendió TiendasEnLaIslaTests a su
    /// costa— no dispara los callbacks y deja pasar la prueba sin haber pulsado nada.
    ///
    /// **Aquí hubo una segunda prueba y la quité; conviene saber por qué.** Comprobaba
    /// que el vecino no cuenta dos historias el mismo día, y pasaba en solitario y
    /// fallaba en la suite entera. No era azar: en la suite corren antes decenas de
    /// pruebas de juego que comparten el mismo guardado —<c>AislarGuardadoEnPruebasDeJuego</c>
    /// desvía la carpeta una sola vez por ensamblado, con <c>[OneTimeSetUp]</c>— y también
    /// el reloj y el censo. Con ese estado por debajo, el vecino ya no contaba nada y la
    /// prueba no llegaba a comprobar lo suyo.
    ///
    /// La regla que quería sujetar ya está sujeta donde el estado sí se controla:
    /// <c>MemoriaConversacionalTests.ContarUnaCosaLeCierraElDia</c> y
    /// <c>NoTeRepiteLoQueYaTeConto</c>, de editor. Lo que sí necesita ser de juego —y es
    /// lo único que queda aquí— es la costura: que al pulsar Charlar salga en pantalla.
    /// Repetir una regla de dominio a través de una pila de estado global que la prueba no
    /// posee no añade cobertura, añade una prueba que a veces se pone roja sola. Y una
    /// prueba inestable acaba enseñando a ignorar los rojos, que es lo más caro de todo.
    /// </remarks>
    public class MemoriaEnLaIslaTests
    {
        /// <summary>
        /// Cuántos días se le dan al dado. No es un plazo de cortesía: es la cuenta.
        /// </summary>
        /// <remarks>
        /// Que el vecino cuente algo es un dado, y el primer comentario que escribí aquí
        /// decía que no lo era: «el dado se siembra con
        /// <c>Rng.FromSeed("recuerdo-dado:{id}:{día}")</c>, así que veinticinco días son
        /// veinticinco casillas de una tabla fija». La semilla sí es fija dado el id —
        /// pero **el id no lo es**: la isla nueva sortea vecinos y cada corrida trae otros.
        /// Eran sorteos de verdad.
        ///
        /// Por eso el número va acompañado de fijarle la Expresión al vecino
        /// (<c>HablarPorLosCodos</c>): <c>ConversationRecall</c> interpola la probabilidad
        /// entre 0,20 y 0,40 con ese eje, y con 0,40 fijo que fallen los veinticinco días
        /// es 0,6²⁵ ≈ 3 entre un millón. Sembrar la personalidad a mano tiene precedente en
        /// <c>AgendaEnLaFichaTests</c> y por el mismo motivo.
        /// </remarks>
        private const int DiasQueSeLeDan = 25;

        /// <summary>El eje que manda en el dado, puesto al tope para que no lo mande el azar.</summary>
        private static void HablarPorLosCodos(Data.Islanders.IslanderData vecino) =>
            vecino.Personality = new Data.Islanders.PersonalityProfile(0f, 1f, 0f, 0f);

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

        [UnityTest]
        public IEnumerator AlCharlarElVecinoAcabaContandoLoQuePasoEnLaAldea()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo),
                "nadie registró el censo");
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(2),
                "hace falta un segundo vecino: la historia que se cuenta es de dos");
            Assert.IsTrue(ServiceRegistry.TryGet<IChronicleService>(out var cronica),
                "sin crónica no hay nada que recordar");
            Assert.IsTrue(ServiceRegistry.TryGet(out GameClock reloj),
                "nadie registró el reloj");

            var vecino = censo.All[0];
            var otro = censo.All[1];
            HablarPorLosCodos(vecino);

            // Se abre la ficha por donde la abre el jugador: la fila de vecinos monta
            // un botón con el nombre corto de cada uno (UiRoot.cs:530).
            yield return Pulsar(Boton(vecino.Identity.ShortName),
                                $"el botón de «{vecino.Identity.ShortName}» en la fila de vecinos");

            string dicho = null;
            yield return CharlaHastaQueCuente(vecino, otro, cronica, reloj, l => dicho = l);

            Assert.IsNotNull(dicho,
                $"charlé {DiasQueSeLeDan} días seguidos con {vecino.Identity.ShortName} " +
                "teniendo un suceso fresco de la aldea entre manos y nunca lo mencionó. " +
                "El recuerdo está escrito y probado en ConversationRecall, así que lo que " +
                "falla es la costura: o ISocialService.RecallLine no llega, o " +
                "SocialSection volvió a borrar el aviso con Say(\"\").");
        }

        /// <summary>
        /// Charla día tras día hasta que el vecino suelta algo, y devuelve lo que dijo.
        /// </summary>
        private static IEnumerator CharlaHastaQueCuente(
            Data.Islanders.IslanderData vecino, Data.Islanders.IslanderData otro,
            IChronicleService cronica, GameClock reloj, System.Action<string> alContar)
        {
            for (int intento = 0; intento < DiasQueSeLeDan; intento++)
            {
                // Se siembra el suceso cada día en vez de una sola vez al principio:
                // un recuerdo caduca a los tres días (FreshDays) y esto dura veinticinco.
                // Es lo mismo que publica el evaluador de romance al prometerse dos.
                EventBus.Publish(new RomanceStageChanged(
                    vecino.Id, otro.Id, RomanceStage.Engaged));
                yield return null;

                Assert.That(cronica.Entries.Count, Is.GreaterThan(0),
                    "el suceso se publicó y la crónica no se enteró");

                yield return Pulsar(Boton("Charlar"), "el botón «Charlar» de la ficha");

                // `Compose` devuelve prefijo + el texto de la crónica tal cual, así que
                // si el recuerdo salió, alguna línea de la crónica está dentro del aviso
                // palabra por palabra. Comparar contra el texto de verdad y no contra un
                // fragmento inventado es lo que impide que esto dé verde por accidente.
                foreach (var texto in TextosDeLaFicha(vecino.Id))
                {
                    foreach (var linea in cronica.Entries)
                    {
                        if (!texto.Contains(linea.Text)) continue;
                        alContar(texto);
                        yield break;
                    }
                }

                reloj.Advance(GameClock.MinutesPerDay);
                for (int i = 0; i < 3; i++) yield return null;
            }
        }

        // ── ayudas ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Pulsa de verdad: la misma cadena de eventos que una mano.
        /// </summary>
        /// <remarks>
        /// Copiado de <see cref="TiendasEnLaIslaTests"/>, que lo pagó: invocar el
        /// interior de <c>Clickable</c> por reflexión no dispara sus callbacks y el
        /// clic se pierde sin excepción. Hacen falta PointerMove, PointerDown y
        /// PointerUp con un fotograma entre medias para que el estado pseudo asiente.
        /// </remarks>
        private static IEnumerator Pulsar(VisualElement boton, string queEra)
        {
            Assert.That(boton, Is.Not.Null, $"no encontré {queEra} en la interfaz");

            var raiz = boton.panel.visualTree;
            var centro = new Vector2(boton.worldBound.center.x, boton.worldBound.center.y);

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

        private static VisualElement Boton(string texto) => Pulsables.Buscar(texto);

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

        /// <summary>
        /// Los textos <b>de la ficha de ese vecino</b>, y de nada más.
        /// </summary>
        /// <remarks>
        /// **Y esto es media prueba, no una comodidad.** La primera versión buscaba la
        /// línea por todos los documentos y daba verde — pero el panel de la Crónica
        /// vive montado en el árbol aunque esté oculto (lo mismo que TiendasEnLaIslaTests
        /// descubrió con su botón Cerrar), y dentro lleva esa misma línea escrita. La
        /// prueba habría pasado igual con la costura desconectada: encontraba el texto
        /// en la Crónica y lo daba por dicho por el vecino.
        ///
        /// Se acota por el nombre del elemento porque <c>UiRoot.Toggle</c> bautiza la
        /// raíz de la ficha con el id del habitante justo antes de abrirla
        /// (UiRoot.cs:612). Es el único asidero público que hay para distinguir «lo que
        /// dice esta ficha» de «lo que hay escrito en pantalla».
        /// </remarks>
        private static List<string> TextosDeLaFicha(string islanderId)
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;

                var ficha = document.rootVisualElement.Q(islanderId);
                if (ficha != null) Recorrer(ficha, textos);
            }

            Assert.That(textos, Is.Not.Empty,
                $"no encuentro la ficha de «{islanderId}» abierta en pantalla: o no se " +
                "abrió al pulsar su botón, o UiRoot dejó de bautizar la raíz con el id");
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
