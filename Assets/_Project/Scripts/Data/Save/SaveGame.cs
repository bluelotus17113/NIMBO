using System;
using System.Collections.Generic;
using Nimbo.Data.Economy;
using Nimbo.Data.Housing;
using Nimbo.Data.Islanders;
using Nimbo.Data.Requests;
using Nimbo.Data.World;

namespace Nimbo.Data.Save
{
    /// <summary>El interior de una casa, guardado por apartamento y no por habitante.</summary>
    /// <remarks>
    /// Va así porque el habitante se puede mudar y la reforma se queda: el jugador
    /// pagó por esa cocina y esperaría encontrarla ahí.
    /// </remarks>
    [Serializable]
    public class HomeRecord
    {
        public string BuildingId;
        public int UnitIndex;
        public RoomLayout Layout = new RoomLayout();

        /// <summary>
        /// Ampliaciones compradas. 0 es la casa de serie.
        /// </summary>
        /// <remarks>
        /// Se guarda aparte del tamaño de la rejilla aunque se pueda deducir de él:
        /// el tamaño de cada nivel es un número de diseño y se retoca, y si mañana
        /// el nivel 1 pasara de 11 a 12 casillas, todas las casas compradas se
        /// leerían como del nivel de serie y se podrían volver a comprar.
        /// </remarks>
        public int UpgradeLevel;

        public string Key => $"{BuildingId}#{UnitIndex}";
    }

    /// <summary>
    /// La partida entera. Todo lo que hay aquí se serializa a JSON; nada de lo que
    /// hay aquí tiene lógica ni referencias a objetos de Unity.
    /// </summary>
    [Serializable]
    public class SaveGame
    {
        /// <summary>
        /// Sube cuando el formato cambia de forma incompatible. El cargador decide
        /// qué hacer con las partidas viejas; nunca las abre a ciegas.
        /// </summary>
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public string SaveId = "";
        public string CreatedUtc = "";
        public string SavedUtc = "";

        /// <summary>Minutos de juego transcurridos desde el primer día. El reloj vive de esto.</summary>
        public long ElapsedMinutes;

        public Wallet Wallet;
        public Inventory Inventory = new Inventory();
        public IslandState Island = new IslandState();

        public List<IslanderData> Islanders = new List<IslanderData>();
        public List<HomeRecord> Homes = new List<HomeRecord>();
        public List<IslanderRequest> Requests = new List<IslanderRequest>();

        /// <summary>Solo los logros que se han tocado alguna vez; el resto son cero.</summary>
        public List<AchievementRecord> Achievements = new List<AchievementRecord>();

        /// <summary>
        /// Las parejas que van camino de casarse, y las casadas que aún no han tenido
        /// hijos. Las lleva el planificador de bodas.
        /// </summary>
        /// <remarks>
        /// Hace falta guardarlo porque el compromiso se cuenta en días, y los días
        /// pasan con el juego cerrado. Sin esto, cada vez que se carga la partida las
        /// parejas volverían a empezar la cuenta y no se casarían nunca — que es
        /// exactamente lo que pasaba antes de que existiera esta lista.
        /// </remarks>
        public List<Social.WeddingBooking> Weddings = new List<Social.WeddingBooking>();

        /// <summary>
        /// Lo que ha ido pasando en la aldea, de lo más viejo a lo más reciente.
        /// </summary>
        /// <remarks>
        /// Se guarda porque la crónica es lo primero que se lee al volver, y lo que se
        /// quiere leer es lo que pasó **mientras no estabas**. Un tablón que solo viva
        /// en memoria se vacía justo en el momento en que hace falta.
        /// </remarks>
        public List<World.ChronicleEntry> Chronicle = new List<World.ChronicleEntry>();

        // ── La aldea ────────────────────────────────────────────────────────
        //
        // Lo que trajo el giro a aldea que se recorre. Va aquí y no en IslandState
        // porque es del jugador y de su partida, no del sitio: el día que haya una
        // segunda isla, el huerto y la mochila siguen siendo los mismos.

        public Player.PlayerState Player = new Player.PlayerState();
        public Farming.FarmState Farm = new Farming.FarmState();
        public World.GatheringState Gathering = new World.GatheringState();

        /// <summary>Recetas que ha aprendido. Las de nivel 1 valen sin estar aquí.</summary>
        public List<string> KnownRecipes = new List<string>();

        /// <summary>Contadores sueltos del juego (eventos vistos, tutoriales, rachas).</summary>
        public List<string> Flags = new List<string>();

        public IslanderData FindIslander(string id)
        {
            for (int i = 0; i < Islanders.Count; i++)
                if (Islanders[i].Id == id) return Islanders[i];
            return null;
        }

        public HomeRecord FindHome(string buildingId, int unitIndex)
        {
            for (int i = 0; i < Homes.Count; i++)
                if (Homes[i].BuildingId == buildingId && Homes[i].UnitIndex == unitIndex) return Homes[i];
            return null;
        }

        public bool HasFlag(string flag) => Flags.Contains(flag);

        public void SetFlag(string flag)
        {
            if (!Flags.Contains(flag)) Flags.Add(flag);
        }
    }
}
