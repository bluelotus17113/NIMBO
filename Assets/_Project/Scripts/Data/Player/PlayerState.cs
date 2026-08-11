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
}
