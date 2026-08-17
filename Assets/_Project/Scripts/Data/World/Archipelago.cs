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

        /// <summary>
        /// El tablón de encargos, en el borde sur de la plaza.
        /// </summary>
        /// <remarks>
        /// Ahí y no en el centro por dos razones. La primera es que el centro lo ocupa
        /// el Árbol Nimbo. La segunda es el sitio por donde se llega: se vive en la isla
        /// de abajo y se entra a la aldea por el puente, que desemboca al sur, así que
        /// el borde sur de la plaza es lo primero que se cruza al venir de casa. Un
        /// tablón que hay que buscar no lo lee nadie.
        ///
        /// Desplazado en x para no quedar plantado en mitad del paso, igual que hubo que
        /// mover el cajón de envíos del porche.
        /// </remarks>
        public static readonly Vector3 RequestBoard = new(7f, 0f, -13f);

        /// <summary>A cuánto hay que estar para poder leerlo.</summary>
        public const float RequestBoardRange = 3f;

        /// <summary>El escenario: donde se dan los conciertos y donde el jugador toca.</summary>
        /// <remarks>
        /// Los identificadores de las dos zonas donde se hace algo —y no solo se
        /// entra— viven aquí y no en el plano de la isla, porque los usan los dos: quien
        /// coloca las zonas y quien pone la actividad encima. Escritos dos veces, un
        /// cambio de nombre dejaría el embarcadero en pie y sin pescar, sin que fallara
        /// nada.
        /// </remarks>
        public const string StageZone = "zona_escenario";

        /// <summary>El embarcadero: el borde donde las nubes son más hondas.</summary>
        public const string JettyZone = "zona_embarcadero";

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
