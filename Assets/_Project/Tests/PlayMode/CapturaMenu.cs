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
    /// Fotografía la interfaz y deja las imágenes en <c>Capturas/</c>.
    /// </summary>
    /// <remarks>
    /// La interfaz no sale en las fotos de <see cref="CapturaInterior"/>: aquello
    /// renderiza una cámara, y UI Toolkit no pinta a través de una cámara sino en su
    /// propio panel. Para verla hay que mandarle el panel a una textura y leerla.
    ///
    /// <c>[Explicit]</c> por lo mismo que la otra: escribe ficheros y tarda.
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaMenu</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaMenu
    {
        private const string Salida = "Capturas";
        private const int Ancho = 1920;
        private const int Alto = 1080;

        [UnityTest]
        public IEnumerator SacaFotosDelMenu()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));
            for (int i = 0; i < 8; i++) yield return null;

            // El de la partida, no el del menú principal: en la escena hay dos y
            // comparten los mismos ajustes de panel, así que buscar «el primer
            // UIDocument» devuelve tan pronto uno como el otro.
            var ui = Object.FindFirstObjectByType<Nimbo.UI.UiRoot>();
            Assert.NotNull(ui, "No hay UiRoot en la escena");

            var document = ui.GetComponent<UIDocument>();
            Assert.NotNull(document, "El UiRoot no tiene UIDocument");

            var root = document.rootVisualElement;

            // Lo que se ve jugando: reloj, barra y nada más.
            yield return Foto(document, "ui_juego.png");

            // Abrir el menú es pulsar el único botón que queda en pantalla.
            var boton = root.Q("boton-menu");
            Assert.NotNull(boton, "No está el botón del menú en el HUD");
            Pulsa(boton);
            yield return Reposa();

            var hub = root.Q("menu");
            Assert.NotNull(hub, "No se ha montado el menú");
            Assert.AreEqual(DisplayStyle.Flex, hub.resolvedStyle.display,
                            "El menú no se ha abierto al pulsar su botón");

            // Y el menú, sección por sección.
            foreach (var seccion in new[] { "Mochila", "Hacer", "Vecinos", "Mapa", "Logros" })
            {
                var pestana = root.Q($"pestana-{seccion}");
                if (pestana == null)
                {
                    Debug.LogWarning($"[captura] no está la pestaña {seccion}");
                    continue;
                }

                Pulsa(pestana);
                yield return Reposa();
                yield return Foto(document, $"ui_menu_{seccion.ToLowerInvariant()}.png");
            }

            // Y un modo, que no es una sección: se entra desde el pie de la columna,
            // cierra el menú y se queda con la pantalla.
            //
            // Se comprueba que el botón está y luego se publica su mismo aviso, en vez
            // de fingirle un clic: un <c>Button</c> de verdad no reacciona a un
            // <c>ClickEvent</c> a mano —su <c>Clickable</c> escucha el puntero— y la
            // foto salía con el menú tal cual, sin entrar en el modo.
            var modo = root.Q("modo-Decorar");
            Assert.NotNull(modo, "No está el modo Decorar en el pie de la columna");

            EventBus.Publish(new DecorModeChanged(true));
            yield return Reposa();
            yield return Foto(document, "ui_modo_decorar.png");

            EventBus.Publish(new DecorModeChanged(false));
            yield return Reposa();

            // Y la pausa, que vive en el otro UIDocument de la escena. Sale en la
            // misma foto porque los dos comparten los ajustes de panel, y es donde se
            // ve que Escape ya no cierra el menú y pausa a la vez.
            EventBus.Publish(new GamePaused(true));
            yield return Reposa();
            yield return Foto(document, "ui_pausa.png");

            EventBus.Publish(new GamePaused(false));

            Assert.Pass();
        }

        /// <summary>
        /// Espera a que terminen las transiciones antes de disparar la foto.
        /// </summary>
        /// <remarks>
        /// Contar fotogramas no vale: las transiciones van por tiempo y en batchmode
        /// los fotogramas pasan mucho más deprisa que en pantalla, así que doce
        /// fotogramas pillaban la pestaña a medio encender.
        /// </remarks>
        private static IEnumerator Reposa()
        {
            yield return new WaitForSecondsRealtime(0.4f);
            yield return null;
        }

        /// <summary>
        /// Un clic sobre el elemento. Tanto las pestañas como el botón del menú
        /// escuchan <c>ClickEvent</c>, que es lo que manda el ratón al soltar.
        /// </summary>
        private static void Pulsa(VisualElement elemento)
        {
            using (var clic = ClickEvent.GetPooled())
            {
                clic.target = elemento;
                elemento.SendEvent(clic);
            }
        }

        /// <summary>
        /// Manda el panel a una textura, espera a que lo pinte y escribe el PNG.
        /// </summary>
        /// <remarks>
        /// Nada de <c>WaitForEndOfFrame</c>: en batchmode ese punto del bucle no llega
        /// nunca. Con <c>targetTexture</c> puesta, el panel se repinta en los fotogramas
        /// siguientes, así que se le dan unos cuantos y luego se lee.
        /// </remarks>
        private static IEnumerator Foto(UIDocument document, string nombre)
        {
            var ajustes = document.panelSettings;
            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            rt.Create();

            var anterior = ajustes.targetTexture;
            ajustes.targetTexture = rt;
            document.rootVisualElement.MarkDirtyRepaint();

            for (int i = 0; i < 6; i++) yield return null;

            var texture = new Texture2D(Ancho, Alto, TextureFormat.RGBA32, false);
            var activa = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            texture.Apply();
            RenderTexture.active = activa;

            ajustes.targetTexture = anterior;

            Directory.CreateDirectory(Salida);
            File.WriteAllBytes(Path.Combine(Salida, nombre), texture.EncodeToPNG());
            Debug.Log($"[captura] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
