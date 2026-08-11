using System;
using Nimbo.Data.Islanders;

namespace Nimbo.Data.Player
{
    /// <summary>
    /// El protagonista: quién es, dónde está y cuánto vigor le queda.
    /// </summary>
    /// <remarks>
    /// Lleva un <see cref="AppearanceData"/> igual que un vecino porque sale del mismo
    /// creador de personajes y lo dibuja el mismo constructor de mallas. Lo que no
    /// tiene es <c>PersonalityProfile</c>: la personalidad del protagonista la pone
    /// quien juega con lo que hace, no un sorteo.
    ///
    /// El vigor **no mata**. A cero no puedes usar herramientas y andas más lento, y
    /// eso es todo (ver `Docs/04_ALDEA.md`, §2).
    /// </remarks>
    [Serializable]
    public class PlayerState
    {
        public string DisplayName = "";
        public AppearanceData Appearance = AppearanceData.Default;

        /// <summary>Dónde se quedó. Al cargar, ahí aparece.</summary>
        public float X;
        public float Y;
        public float Z;
        public float Yaw;

        public float Vigor = MaxVigor;
        public const float MaxVigor = 100f;

        /// <summary>Cierto en cuanto el creador de personajes ha terminado.</summary>
        public bool Created;

        /// <summary>La mochila. Los huecos vacíos se guardan como pilas de cantidad 0.</summary>
        public Economy.Inventory Bag = new Economy.Inventory();

        public int SelectedSlot;
    }

    /// <summary>
    /// Dónde está la casa del protagonista y sus cuatro muebles.
    /// </summary>
    /// <remarks>
    /// Igual que con el huerto: los números viven en Data porque los usan quien
    /// dibuja la casa y quien decide si estás delante de la cama, y son ensamblados
    /// que no se ven entre sí. Copiarlos a mano acabaría con el jugador plantado
    /// delante de una cama que el juego cree que está dos metros más allá.
    ///
    /// Está en tu isla, al este del huerto, y todo lo de trabajar queda fuera de
    /// momento: no hay interior que recorrer todavía. La cama es una hamaca en el porche, que en una isla que flota
    /// no chirría y evita fingir una casa por dentro que no existe.
    /// </remarks>
    public static class PlayerHome
    {
        public static readonly UnityEngine.Vector3 Cabin = new(12f, 0f, -168f);
        public static readonly UnityEngine.Vector3 Hammock = new(8.4f, 0f, -168f);
        public static readonly UnityEngine.Vector3 Bench = new(15.6f, 0f, -168f);
        public static readonly UnityEngine.Vector3 ShippingBox = new(12f, 0f, -172f);

        /// <summary>Donde apareces: al sur, con el huerto y la casa por delante.</summary>
        public static readonly UnityEngine.Vector3 Spawn = new(0f, 3f, -176f);

        /// <summary>A cuánto hay que estar para poder usar un mueble.</summary>
        public const float UseRange = 2.4f;
    }
}
