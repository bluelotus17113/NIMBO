using System.Collections.Generic;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>Por qué no se puede ampliar todavía. El aviso se le enseña al jugador.</summary>
    public enum UpgradeRejection
    {
        Ok = 0,
        NoHome,        // ese habitante aún no vive en ningún sitio
        MaxedOut,      // ya está en el último nivel
        NotEnoughCoins,
        UnknownIslander,
        NotEnoughMaterials,  // falta madera, piedra o savia en la mochila
    }

    /// <summary>Tanto de un material hace falta para la obra.</summary>
    public readonly struct MaterialCost
    {
        public readonly string CatalogId;
        public readonly int Quantity;

        public MaterialCost(string catalogId, int quantity)
        {
            CatalogId = catalogId;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Ampliar la casa de un habitante. Dos niveles, y se pagan en monedas y en obra.
    /// </summary>
    /// <remarks>
    /// Va aparte de <c>IHousingService</c>, que decora lo que ya existe, porque esto
    /// cambia el tamaño de la habitación: es la operación que invalida la rejilla
    /// entera y hay que remapear el suelo. Mezclarla con «pon un sofá aquí» habría
    /// obligado a que cada colocación se preguntase si el cuarto acaba de crecer.
    ///
    /// Solo se crece, nunca se encoge. Encoger dejaría muebles fuera de la
    /// habitación y habría que decidir qué se tira, y esa no es una decisión que
    /// este juego deba pedirle a nadie.
    ///
    /// **Se paga también en material, y eso no es un detalle de equilibrio: es el
    /// puente entre las dos mitades del juego.** Con solo monedas, talar un árbol no le
    /// importaba a la aldea —el dinero entra igual de los sueldos— y la recolección se
    /// quedaba en un bucle cerrado que empezaba y acababa en el cajón de envíos. Pagar
    /// la obra con madera y piedra es lo que hace que salir a por material tenga
    /// consecuencias sobre cómo vive la gente. Ver `Docs/01_GDD.md` §15.2 y §16.
    /// </remarks>
    public interface IHomeUpgradeService
    {
        /// <summary>Cuántos niveles de ampliación hay por encima del inicial.</summary>
        int MaxLevel { get; }

        /// <summary>Nivel actual de la casa de ese habitante. 0 es la de serie.</summary>
        int LevelOf(string islanderId);

        /// <summary>Lo que cuesta en monedas pasar de ese nivel al siguiente.</summary>
        long PriceOf(int level);

        /// <summary>
        /// El material que hace falta para pasar de ese nivel al siguiente. Vacío si
        /// ese nivel no existe.
        /// </summary>
        IReadOnlyList<MaterialCost> MaterialsFor(int level);

        /// <summary>El tamaño de rejilla que corresponde a ese nivel.</summary>
        int SizeOfLevel(int level);

        /// <summary>No cambia nada; sirve para pintar el botón y su motivo.</summary>
        UpgradeRejection CanUpgrade(string islanderId);

        /// <summary>
        /// Amplía, cobra y gasta el material. Devuelve falso sin tocar nada si no se
        /// podía.
        /// </summary>
        bool Upgrade(string islanderId);
    }
}
