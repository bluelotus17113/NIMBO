using UnityEngine;

namespace Nimbo.Data.World
{
    /// <summary>Cuál de las dos islas.</summary>
    public enum IslandSide
    {
        /// <summary>La aldea: donde viven los vecinos y donde se construye para ellos.</summary>
        Village = 0,

        /// <summary>La tuya: tu casa y tu huerto, y nadie más.</summary>
        Home = 1,
    }

    /// <summary>
    /// Dónde está cada isla y por dónde se pasa de una a otra.
    /// </summary>
    /// <remarks>
    /// Vive en Data por lo mismo que <see cref="Nimbo.Data.Farming.FarmPlot"/>: lo
    /// necesitan quien dibuja el mundo, quien decide en qué isla estás, la cámara y el
    /// mapa, y son ensamblados que no se ven entre sí. Con los números copiados a
    /// mano, el mapa te pintaría en un sitio y el juego te tendría en otro.
    ///
    /// La aldea se queda en el origen a propósito: las diez zonas ya estaban escritas
    /// en coordenadas alrededor del cero, y moverlas habría invalidado las partidas de
    /// quien ya tuviera edificios abiertos.
    /// </remarks>
    public static class Archipelago
    {
        public static readonly Vector3 VillageCentre = Vector3.zero;
        public const float VillageRadius = 100f;

        /// <summary>Tu isla, al sur. Más pequeña: es una casa y un huerto, no un pueblo.</summary>
        public static readonly Vector3 HomeCentre = new(0f, 0f, -165f);
        public const float HomeRadius = 45f;

        /// <summary>Ancho del puente de madera que las une.</summary>
        public const float BridgeWidth = 4.5f;

        /// <summary>
        /// Dónde arranca el puente en la aldea y dónde acaba en tu isla.
        /// </summary>
        /// <remarks>
        /// Los dos extremos se meten **dentro** de su isla a propósito, unos metros
        /// más allá del borde nominal. El contorno de un prado es irregular —va del
        /// 0,86 al 1,0 del radio— así que un puente que llegue justo al radio deja
        /// hasta dos metros y medio de vacío donde el borde se mete hacia dentro. Se
        /// vio jugando: se cruzaba y se caía por la junta.
        /// </remarks>
        public static readonly Vector3 BridgeFromVillage = new(0f, 0f, -80f);
        public static readonly Vector3 BridgeToHome = new(0f, 0f, -132f);

        /// <summary>En qué isla cae ese punto. Fuera de las dos, la más cercana.</summary>
        public static IslandSide SideOf(Vector3 position)
        {
            float toVillage = Flat(position, VillageCentre);
            float toHome = Flat(position, HomeCentre);
            return toHome < toVillage ? IslandSide.Home : IslandSide.Village;
        }

        public static Vector3 CentreOf(IslandSide side) =>
            side == IslandSide.Home ? HomeCentre : VillageCentre;

        public static float RadiusOf(IslandSide side) =>
            side == IslandSide.Home ? HomeRadius : VillageRadius;

        /// <summary>Está sobre el puente, entre las dos islas.</summary>
        public static bool OnBridge(Vector3 position) =>
            position.z <= BridgeFromVillage.z && position.z >= BridgeToHome.z &&
            Mathf.Abs(position.x - BridgeFromVillage.x) <= BridgeWidth;

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x;
            float dz = a.z - b.z;
            return dx * dx + dz * dz;
        }
    }
}
