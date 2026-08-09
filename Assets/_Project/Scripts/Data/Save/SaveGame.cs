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
