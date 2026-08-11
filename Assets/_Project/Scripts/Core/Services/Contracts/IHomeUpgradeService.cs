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
    }

    /// <summary>
    /// Ampliar la casa de un habitante. Dos niveles, y se pagan.
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
    /// </remarks>
    public interface IHomeUpgradeService
    {
        /// <summary>Cuántos niveles de ampliación hay por encima del inicial.</summary>
        int MaxLevel { get; }

        /// <summary>Nivel actual de la casa de ese habitante. 0 es la de serie.</summary>
        int LevelOf(string islanderId);

        /// <summary>Lo que cuesta pasar de ese nivel al siguiente.</summary>
        long PriceOf(int level);

        /// <summary>El tamaño de rejilla que corresponde a ese nivel.</summary>
        int SizeOfLevel(int level);

        /// <summary>No cambia nada; sirve para pintar el botón y su motivo.</summary>
        UpgradeRejection CanUpgrade(string islanderId);

        /// <summary>
        /// Amplía y cobra. Devuelve falso sin tocar nada si no se podía.
        /// </summary>
        bool Upgrade(string islanderId);
    }
}
