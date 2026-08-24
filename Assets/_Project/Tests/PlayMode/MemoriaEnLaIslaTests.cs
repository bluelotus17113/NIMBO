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
    /// </remarks>
    public class MemoriaEnLaIslaTests
    {
        /// <summary>
        /// Cuántos días se le dan al dado. No es un plazo de cortesía: es la cuenta.
        /// </summary>
        /// <remarks>
        /// Que el vecino cuente algo es un dado de entre 0,20 y 0,40 según su
        /// Expresión (<c>ConversationRecall.ChanceFloor/ChanceCeiling</c>), y a primera
        /// vista esto parecería una prueba inestable. No lo es: el dado se siembra con
        /// <c>Rng.FromSeed("recuerdo-dado:{id}:{día}")</c>, así que para un vecino y un
        /// día **la respuesta es fija**. Doce días no son doce sorteos, son doce
        /// casillas de una tabla que no cambia entre corridas.
        ///
        /// Doce y no tres porque con el suelo de 0,20 una racha de tres noes cabe de
        /// sobra en cualquier tabla; y no cien porque cada día son cuatro fotogramas y
        /// una pulsación, y una prueba de costura que tarda un minuto deja de correrse.
        /// </remarks>
        private const int DiasQueSeLeDan = 12;

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

            // Se abre la ficha por donde la abre el jugador: la fila de vecinos monta
            // un botón con el nombre corto de cada uno (UiRoot.cs:530).
            yield return Pulsar(Boton(vecino.Identity.ShortName),
                                $"el botón de «{vecino.Identity.ShortName}» en la fila de vecinos");

            string dicho = null;

            for (int intento = 0; intento < DiasQueSeLeDan && dicho == null; intento++)
            {
                // Se siembra el suceso cada día en vez de una sola vez al principio:
                // un recuerdo caduca a los tres días (FreshDays) y la prueba dura doce.
                // Es lo mismo que publica el evaluador de romance al prometerse dos.
                EventBus.Publish(new RomanceStageChanged(
                    vecino.Id, otro.Id, RomanceStage.Engaged));
                yield return null;

                Assert.That(cronica.Entries.Count, Is.GreaterThan(0),
                    "el suceso se publicó y la crónica no se enteró");
                string suceso = cronica.Entries[cronica.Entries.Count - 1].Text;

                yield return Pulsar(Boton("Charlar"), "el botón «Charlar» de la ficha");

                // `Compose` devuelve prefijo + el texto de la crónica tal cual, así que
                // si el recuerdo salió, la línea de la crónica está dentro del aviso
                // palabra por palabra. Comparar contra el texto de verdad y no contra
                // un fragmento inventado es lo que hace que esto no pueda dar verde
                // por accidente.
                foreach (var texto in TextosDeLaFicha(vecino.Id))
                {
                    if (texto.Contains(suceso)) { dicho = texto; break; }
                }
                if (dicho != null) break;

                reloj.Advance(GameClock.MinutesPerDay);
                for (int i = 0; i < 3; i++) yield return null;
            }

            Assert.IsNotNull(dicho,
                $"charlé {DiasQueSeLeDan} días seguidos con {vecino.Identity.ShortName} " +
                "teniendo un suceso fresco de la aldea entre manos y nunca lo mencionó. " +
                "El recuerdo está escrito y probado en ConversationRecall, así que lo que " +
                "falla es la costura: o ISocialService.RecallLine no llega, o " +
                "SocialSection volvió a borrar el aviso con Say(\"\").");
        }

        /// <summary>
        /// Y no te lo cuenta dos veces el mismo día.
        /// </summary>
        /// <remarks>
        /// **Esta prueba nació de equivocarme y merece la pena contarlo.** La primera
        /// versión afirmaba que en una isla recién empezada el vecino no tenía nada que
        /// contar. Falso: la crónica arranca con las líneas de los edificios que se
        /// abren, y el vecino las cotillea con toda la razón. La premisa era mía, no un
        /// fallo del código.
        ///
        /// Lo que sí es cierto —y es la mitad del trato que hace que «se acuerdan» no
        /// degenere— es el tope de una historia por vecino y día. Un vecino que te suelta
        /// lo mismo cada vez que le hablas es peor que uno que calla: delata que no se
        /// acuerda de verdad, solo repite.
        ///
        /// Vale con cualquier tope diario de charla que ponga la configuración: si la
        /// segunda pulsación se pasa del tope, el aviso dice «por hoy ya está bien», y
        /// si no se pasa, <c>RecallLine</c> devuelve null por la bandera del día y el
        /// aviso queda limpio. En los dos casos lo que NO puede haber es otra línea de
        /// la crónica, que es exactamente lo que se afirma.
        /// </remarks>
        [UnityTest]
        public IEnumerator NoTeCuentaOtraHistoriaElMismoDia()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(2));
            Assert.IsTrue(ServiceRegistry.TryGet<IChronicleService>(out var cronica));
            Assert.IsTrue(ServiceRegistry.TryGet(out GameClock reloj));

            var vecino = censo.All[0];

            yield return Pulsar(Boton(vecino.Identity.ShortName),
                                $"el botón de «{vecino.Identity.ShortName}»");

            string primera = null;
            yield return CharlaHastaQueCuente(vecino, censo.All[1], cronica, reloj,
                                              linea => primera = linea);
            Assert.IsNotNull(primera, "no llegó a contar nada, así que no hay nada que repetir");

            // Segunda charla, mismo día.
            yield return Pulsar(Boton("Charlar"), "el botón «Charlar» de la ficha");

            foreach (var texto in TextosDeLaFicha(vecino.Id))
            {
                foreach (var linea in cronica.Entries)
                {
                    Assert.That(texto, Does.Not.Contain(linea.Text),
                        "le hablé dos veces el mismo día y volvió a contar algo de la " +
                        "crónica. El tope de una historia por vecino y día no está " +
                        $"llegando por el camino real. Primera vez dijo: «{primera}»");
                }
            }
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
                // un recuerdo caduca a los tres días (FreshDays) y esto dura doce.
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
        private static IEnumerator Pulsar(Button boton, string queEra)
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

        private static Button Boton(string texto)
        {
            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
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
