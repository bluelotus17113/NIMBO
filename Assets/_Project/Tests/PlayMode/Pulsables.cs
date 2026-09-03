using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Lo que el jugador puede pulsar en pantalla, sea de la clase que sea.
    /// </summary>
    /// <remarks>
    /// **Por qué existe.** Media docena de pruebas de juego preguntaban lo mismo —«¿hay
    /// por dónde llegar a esto?»— y cada una lo hacía recorriendo el árbol en busca de
    /// <c>Button</c>. Funcionó mientras todo lo pulsable era un botón. Al entrar el menú
    /// nuevo dejó de serlo: sus pestañas las fabrica <c>UiTheme.RailTab</c> como
    /// <c>VisualElement</c> con una etiqueta dentro, así que veinte pruebas se pusieron
    /// rojas diciendo «no hay botón para la Crónica» cuando la Crónica estaba ahí, a un
    /// clic, con su nombre escrito.
    ///
    /// Lo que se comprueba no es de qué clase es el objeto: es si el jugador tiene dónde
    /// pulsar y si lo que pone se entiende. Eso es lo que mira esta clase.
    ///
    /// **Y un defecto de verdad que salió de aquí, anotado para que no se pierda.** Una
    /// pestaña que no es <c>Button</c> tampoco recibe foco de teclado ni la viste el tema
    /// —<c>LosBotonesDeLaBarraLlevanLaClaseDelTema</c> cuenta siete así—, de modo que ese
    /// menú solo se maneja con el ratón. No se arregla desde una utilidad de pruebas:
    /// hacerlas botones toca decidir dónde va el punto de color, que hoy es un hijo y
    /// competiría con el texto propio del botón. Queda dicho aquí y en el informe.
    /// </remarks>
    public static class Pulsables
    {
        /// <summary>Todo lo pulsable que hay montado, con lo que pone en ello.</summary>
        public static List<string> Textos()
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Recorrer(document.rootVisualElement, textos, null);
            }
            return textos;
        }

        /// <summary>
        /// El elemento pulsable que lleva ese texto, o null.
        /// </summary>
        /// <remarks>
        /// Devuelve <c>VisualElement</c> y no <c>Button</c> a propósito: quien lo pulse
        /// lo hará con eventos de puntero sobre su rectángulo, que es lo que hace una
        /// mano, y para eso da igual la clase.
        /// </remarks>
        public static VisualElement Buscar(string texto)
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

        private static VisualElement Buscar(VisualElement element, string texto)
        {
            if (TextoDe(element) == texto) return element;

            for (int i = 0; i < element.childCount; i++)
            {
                var hijo = Buscar(element[i], texto);
                if (hijo != null) return hijo;
            }
            return null;
        }

        private static void Recorrer(VisualElement element, List<string> textos, string _)
        {
            string texto = TextoDe(element);
            if (!string.IsNullOrEmpty(texto)) textos.Add(texto);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos, null);
        }

        /// <summary>
        /// Lo que pone en algo pulsable, o vacío si no es pulsable.
        /// </summary>
        /// <remarks>
        /// Dos formas y ninguna más, para no acabar recogiendo cualquier etiqueta de la
        /// pantalla: un <c>Button</c> con su texto, y una pestaña del menú —que se
        /// reconoce por el nombre que le pone <c>RailTab</c>, <c>pestana-{etiqueta}</c>—
        /// leyendo la etiqueta que lleva dentro. Si mañana aparece un tercer tipo de
        /// pulsable, se añade aquí y no en seis ficheros de pruebas.
        /// </remarks>
        private static string TextoDe(VisualElement element)
        {
            if (element is Button button) return button.text;

            if (element.name != null && element.name.StartsWith("pestana-"))
            {
                foreach (var hijo in element.Children())
                    if (hijo is Label etiqueta && !string.IsNullOrEmpty(etiqueta.text))
                        return etiqueta.text;
            }

            return null;
        }
    }
}
