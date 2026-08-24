using System.Collections.Generic;
using UnityEngine;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// El único sitio donde vive una tecla del teclado. Quien atiende la pulsación
    /// lee de aquí, y la lista de controles del panel de opciones se pinta de aquí:
    /// cambiar una tecla cambia las dos cosas a la vez.
    /// </summary>
    /// <remarks>
    /// La auditoría encontró que Tab, M y B solo existían en el código de
    /// <c>UiRoot</c>: un jugador no puede descubrirlas. La cura clásica —escribir
    /// una lista a mano— tiene su propia enfermedad: el día que alguien mueve una
    /// tecla, la lista miente. Por eso este fichero es datos y no documentación, y
    /// por eso solo se apuntan aquí las teclas cuyo consumidor lee de aquí. Las de
    /// acción sobre el mundo (E, F, R, el botón derecho) ya las enseñan los
    /// carteles de la escena y sus lecturas viven en Nimbo.Art; apuntarlas sin
    /// mover sus lecturas sería recrear justo la lista que miente que esto evita.
    /// </remarks>
    public static class GameKeys
    {
        /// <summary>Una fila de la lista de controles: qué tecla y para qué.</summary>
        public readonly struct Binding
        {
            public readonly KeyCode Key;
            public readonly string Action;

            public Binding(KeyCode key, string action)
            {
                Key = key;
                Action = action;
            }
        }

        /// <summary>Pausa; y con algo abierto, cerrarlo antes de pausar.</summary>
        public static readonly KeyCode Pause = KeyCode.Escape;

        /// <summary>La mochila. Es la tecla que todo el mundo prueba primero.</summary>
        public static readonly KeyCode Bag = KeyCode.Tab;

        /// <summary>El mapa.</summary>
        public static readonly KeyCode Map = KeyCode.M;

        /// <summary>Amueblar, que solo existe dentro de casa.</summary>
        public static readonly KeyCode Furnish = KeyCode.B;

        /// <summary>Lo que pinta la lista de controles, en este orden.</summary>
        public static IReadOnlyList<Binding> Listed { get; } = new[]
        {
            new Binding(Pause, "Pausar, o cerrar lo que haya abierto"),
            new Binding(Bag, "Abrir o cerrar la mochila"),
            new Binding(Map, "Abrir o cerrar el mapa"),
            new Binding(Furnish, "Amueblar dentro de casa"),
        };

        /// <summary>El nombre corto de una tecla tal como sale en la lista.</summary>
        public static string Name(KeyCode key)
        {
            if (key is >= KeyCode.Alpha0 and <= KeyCode.Alpha9)
                return ((char)('0' + (key - KeyCode.Alpha0))).ToString();

            return key switch
            {
                KeyCode.Escape => "Esc",
                KeyCode.LeftShift => "Mayús izq.",
                KeyCode.RightShift => "Mayús der.",
                _ => key.ToString(),
            };
        }
    }
}
