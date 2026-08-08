using System;
using System.Collections.Generic;
using Nimbo.Data.Social;

namespace Nimbo.Data.Islanders
{
    /// <summary>Dónde vive: qué edificio y qué apartamento dentro de él.</summary>
    [Serializable]
    public struct HomeAssignment
    {
        public string BuildingId;
        public int UnitIndex;
        public bool HasHome => !string.IsNullOrEmpty(BuildingId);
    }

    /// <summary>
    /// Gustos de un habitante. Se descubren jugando: el jugador le da comida y ve
    /// la cara que pone. Esa cara es la mitad del juego, así que los gustos son
    /// fijos desde la creación y no se recalculan nunca.
    /// </summary>
    [Serializable]
    public class TasteProfile
    {
        public List<string> LovedFoods = new List<string>();
        public List<string> HatedFoods = new List<string>();
        public string FavoriteColor = "";

        /// <summary>−1 lo odia, 0 indiferente, +1 le encanta.</summary>
        public int OpinionOf(string foodId)
        {
            if (LovedFoods.Contains(foodId)) return 1;
            if (HatedFoods.Contains(foodId)) return -1;
            return 0;
        }
    }

    /// <summary>
    /// Un habitante entero. Todo lo que hay que guardar de él está aquí dentro y
    /// ninguna de estas piezas tiene lógica: la lógica vive en su módulo.
    /// </summary>
    [Serializable]
    public class IslanderData
    {
        public IslanderIdentity Identity;
        public AppearanceData Appearance;
        public VoiceConfig Voice;
        public PersonalityProfile Personality;

        public NeedState Needs;
        public MoodState Mood;
        public ProgressionState Progression;

        public RelationshipBook Relationships = new RelationshipBook();
        public TasteProfile Tastes = new TasteProfile();
        public HomeAssignment Home;

        /// <summary>Ropa que posee. La que lleva puesta es <see cref="EquippedOutfit"/>.</summary>
        public List<string> Wardrobe = new List<string>();
        public string EquippedOutfit = "";

        /// <summary>Dónde está ahora mismo en la isla. Lo escribe solo el módulo de isla.</summary>
        public string CurrentZoneId = "";

        /// <summary>Minuto de juego en que llegó a la isla, para "lleva 12 días aquí".</summary>
        public long ArrivalMinute;

        public string Id => Identity.Id;

        public int PersonalityTypeIndex => Personality.TypeIndex;
    }
}
