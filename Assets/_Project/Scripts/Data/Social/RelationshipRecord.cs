using System;
using UnityEngine;

namespace Nimbo.Data.Social
{
    public enum FriendshipStage
    {
        Stranger = 0,
        Acquaintance = 1,
        Friend = 2,
        CloseFriend = 3,
        BestFriend = 4,
    }

    /// <summary>La rama de las riñas. Va aparte de la amistad a propósito.</summary>
    public enum ConflictStage
    {
        None = 0,
        Tension = 1,
        Quarrel = 2,
        Feud = 3,
    }

    public enum RomanceStage
    {
        None = 0,
        Crush = 1,       // le gusta, no lo ha dicho
        Confessed = 2,   // se ha declarado, falta respuesta
        Dating = 3,
        Engaged = 4,
        Married = 5,
        Separated = 6,
    }

    public enum FamilyTie
    {
        None = 0,
        Spouse = 1,
        Parent = 2,
        Child = 3,
        Sibling = 4,
    }

    /// <summary>
    /// Lo que un habitante siente por otro. Las cuatro ramas son independientes
    /// a propósito: se puede estar casado y reñido a la vez, y ahí está la gracia.
    /// </summary>
    [Serializable]
    public struct RelationshipRecord
    {
        public const float MinAffinity = -100f;
        public const float MaxAffinity = 100f;

        public string OtherId;

        [Range(MinAffinity, MaxAffinity)] public float Affinity;
        public FriendshipStage Friendship;
        public ConflictStage Conflict;
        public RomanceStage Romance;
        public FamilyTie Family;

        /// <summary>Interacciones acumuladas. Modula lo rápido que sube la afinidad.</summary>
        public int Interactions;

        /// <summary>Minuto de juego de la última interacción, para que la afinidad se enfríe sola.</summary>
        public long LastInteractionMinute;

        public static RelationshipRecord NewWith(string otherId) => new RelationshipRecord
        {
            OtherId = otherId,
            Affinity = 0f,
            Friendship = FriendshipStage.Stranger,
            Conflict = ConflictStage.None,
            Romance = RomanceStage.None,
            Family = FamilyTie.None,
            Interactions = 0,
            LastInteractionMinute = 0,
        };

        public bool IsFamily => Family != FamilyTie.None;
        public bool IsRomantic => Romance >= RomanceStage.Dating && Romance != RomanceStage.Separated;
    }
}
