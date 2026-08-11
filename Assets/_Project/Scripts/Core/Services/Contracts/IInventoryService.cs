using System.Collections.Generic;
using Nimbo.Data.Economy;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no cabe. El aviso se le enseña al jugador.</summary>
    public enum StoreResult
    {
        Ok = 0,
        Full,          // no cabe nada más
        Partial,       // entró parte; el resto se queda fuera
        UnknownItem,
    }

    /// <summary>
    /// La mochila del protagonista: huecos, pilas y lo que lleva en la mano.
    /// </summary>
    /// <remarks>
    /// Es distinta del <c>Inventory</c> que ya existía, que era una despensa sin
    /// límite para regalar y colocar muebles. Ese se queda como está y lo sigue usando
    /// la economía; esto es la mochila que llevas encima mientras andas, y tiene
    /// hueco contado a propósito: sin límite no hay que decidir qué traer ni cuándo
    /// volver, y volver a casa es medio bucle del juego.
    ///
    /// Los huecos se cuentan desde 0 y **no se compactan solos**: si sacas lo del
    /// hueco 3, el 3 queda vacío y el 4 sigue donde estaba. Que la mochila se
    /// reordene sola mientras juegas es de las cosas que más molestan.
    /// </remarks>
    public interface IInventoryService
    {
        int SlotCount { get; }

        /// <summary>Lo que hay en ese hueco. Cantidad 0 significa vacío.</summary>
        ItemStack At(int slot);

        /// <summary>El hueco que tiene seleccionado, de 0 a <see cref="HotbarSize"/>-1.</summary>
        int SelectedSlot { get; }

        /// <summary>Cuántos huecos se ven en la barra de abajo. Los primeros de la mochila.</summary>
        int HotbarSize { get; }

        void Select(int slot);

        /// <summary>Lo que lleva en la mano ahora mismo. Vacío si el hueco lo está.</summary>
        ItemStack InHand { get; }

        /// <summary>La herramienta que lleva en la mano, o <c>None</c> si no es una.</summary>
        ToolKind ToolInHand { get; }

        /// <summary>
        /// Mete lo que pueda. <paramref name="leftover"/> sale con lo que no cupo.
        /// </summary>
        StoreResult TryStore(string catalogId, int quantity, out int leftover);

        /// <summary>Cuántos lleva encima, sumando todas las pilas.</summary>
        int CountOf(string catalogId);

        /// <summary>Saca esa cantidad. Falso y sin tocar nada si no la tenía.</summary>
        bool TryTake(string catalogId, int quantity = 1);

        /// <summary>Saca del hueco seleccionado. Para gastar lo que llevas en la mano.</summary>
        bool TryConsumeSelected(int quantity = 1);

        /// <summary>Intercambia dos huecos. Es cómo se ordena la mochila.</summary>
        void Swap(int a, int b);

        /// <summary>Tira lo que haya en ese hueco. No lo devuelve a ningún sitio.</summary>
        bool Drop(int slot);

        /// <summary>Cuántas unidades caben en una pila de ese objeto.</summary>
        int StackLimitOf(string catalogId);

        /// <summary>Todo lo que lleva, hueco a hueco. Para pintar la mochila.</summary>
        IReadOnlyList<ItemStack> Slots { get; }
    }
}
