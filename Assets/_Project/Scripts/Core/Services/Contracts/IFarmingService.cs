using System.Collections.Generic;
using Nimbo.Data.Farming;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se pudo hacer eso en esa casilla.</summary>
    public enum FarmError
    {
        Ok = 0,
        OutOfBounds,
        NotTilled,      // hay que labrar antes de sembrar
        AlreadyPlanted,
        NothingPlanted,
        UnknownSeed,
        NoSeed,         // no lleva semillas encima
        NotReady,       // aún no ha crecido
        WrongTool,
        InventoryFull,  // creció, pero no hay dónde meterlo
        NotYourLandYet, // esa esquina del huerto pide más nivel de Cultivo (§12.3)
    }

    /// <summary>Una semilla del catálogo y en qué se convierte.</summary>
    public readonly struct CropDefinition
    {
        public readonly string SeedId;
        public readonly string CropId;      // lo que sale al recoger
        public readonly string DisplayName;

        /// <summary>Días de juego que tarda en estar listo, regándolo cada día.</summary>
        public readonly int DaysToGrow;

        /// <summary>Cuántas unidades da al recogerlo.</summary>
        public readonly int Yield;

        /// <summary>
        /// Si es cierto, al recoger vuelve a una fase anterior y sigue dando.
        /// </summary>
        public readonly bool Regrows;

        public CropDefinition(string seedId, string cropId, string displayName,
                              int daysToGrow, int yield, bool regrows)
        {
            SeedId = seedId; CropId = cropId; DisplayName = displayName;
            DaysToGrow = daysToGrow; Yield = yield; Regrows = regrows;
        }
    }

    /// <summary>
    /// El huerto: una parcela pequeña junto a tu casa.
    /// </summary>
    /// <remarks>
    /// Pequeño a propósito y **sin estaciones**. Un calendario con ventanas de siembra
    /// convierte el huerto en una obligación con fecha, y este juego no castiga por no
    /// entrar (ver `Docs/04_ALDEA.md`, §2). Por lo mismo, **una planta sin regar no se
    /// muere**: deja de crecer ese día y ya. Volver tras una semana tiene que ser
    /// encontrarse el huerto parado, no muerto.
    ///
    /// El crecimiento se calcula por días, no por fotogramas: la simulación ya avanza
    /// en horas de juego y el recuperador de tiempo offline usa el mismo camino.
    /// </remarks>
    public interface IFarmingService
    {
        int Width { get; }
        int Height { get; }

        /// <summary>Cómo está esa casilla.</summary>
        FarmTile TileAt(int x, int y);

        IReadOnlyList<CropDefinition> Crops { get; }

        bool TryGetCrop(string seedId, out CropDefinition crop);

        /// <summary>Pasa la azada. Deja la tierra lista para sembrar.</summary>
        FarmError Till(int x, int y);

        /// <summary>Siembra. Gasta una semilla de la mochila.</summary>
        FarmError Plant(int x, int y, string seedId);

        /// <summary>Riega. Sin regar, ese día la planta no avanza.</summary>
        FarmError Water(int x, int y);

        /// <summary>
        /// Recoge lo que haya crecido y lo mete en la mochila. Devuelve cuántas
        /// unidades salieron, o 0 con el motivo en <paramref name="error"/>.
        /// </summary>
        int Harvest(int x, int y, out FarmError error);

        /// <summary>Arranca lo sembrado sin recoger nada. Para rectificar.</summary>
        FarmError Clear(int x, int y);

        /// <summary>
        /// Pasa un día en el huerto: lo regado crece, y el riego se seca.
        /// </summary>
        /// <remarks>
        /// Lo llama quien lleva el reloj, una vez por día de juego. Tiene que poder
        /// llamarse varias veces seguidas sin romperse: al volver tras estar fuera,
        /// el juego adelanta varios días de golpe por este mismo camino.
        /// </remarks>
        void AdvanceDay();
    }
}
