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
    /// <remarks>
    /// <see cref="Rivalry"/> va al final **con su número escrito** aunque en gravedad
    /// esté entre la tirantez y la riña: estos valores acaban en las partidas
    /// guardadas, y colarlo en medio convertiría las riñas de una partida vieja en
    /// enemistades y las enemistades en algo que no existe. Para ordenarlas por lo mal
    /// que están las cosas está <see cref="ConflictStages.Severity"/>.
    /// </remarks>
    public enum ConflictStage
    {
        None = 0,
        Tension = 1,
        Quarrel = 2,
        Feud = 3,

        /// <summary>Los dos quieren a la misma persona y ya lo saben.</summary>
        Rivalry = 4,
    }

    /// <summary>Cómo de mal están las cosas, en orden.</summary>
    public static class ConflictStages
    {
        /// <summary>De 0 a 4. Es el orden de verdad, que no es el del enum.</summary>
        public static int Severity(this ConflictStage stage) => stage switch
        {
            ConflictStage.None => 0,
            ConflictStage.Tension => 1,
            ConflictStage.Rivalry => 2,
            ConflictStage.Quarrel => 3,
            _ => 4,
        };

        /// <summary>
        /// Lo bastante grave como para esquivarse por la calle.
        /// </summary>
        /// <remarks>
        /// Una rivalidad no lo es: los dos siguen yendo a los mismos sitios, porque
        /// justamente van adonde está la persona que les gusta. Esquivarse es de los
        /// que ya no se hablan.
        /// </remarks>
        public static bool IsSerious(this ConflictStage stage) =>
            stage is ConflictStage.Quarrel or ConflictStage.Feud;
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

        /// <summary>
        /// Hasta qué día no se puede volver a intentar lo romántico por aquí.
        /// </summary>
        /// <remarks>
        /// Lo pone un rechazo (§14.3.5). La espera es la mitad del sentido de que te
        /// digan que no: sin ella, declararse otra vez al día siguiente convierte el
        /// cortejo en insistir hasta que salga.
        /// </remarks>
        public int BlockedUntilDay;

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
