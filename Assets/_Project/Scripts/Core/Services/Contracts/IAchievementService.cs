using System.Collections.Generic;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>De qué va un logro. Sirve para agruparlos en la lista.</summary>
    public enum AchievementKind
    {
        Life = 0,        // vivir en la isla: días, niveles, cumpleaños
        Social = 1,      // amistades, parejas, riñas arregladas
        Home = 2,        // decorar, ampliar, muebles
        Money = 3,       // ganar, gastar, ahorrar
        Play = 4,        // minijuegos
        Island = 5,      // zonas abiertas, adornos, el Árbol
        Odd = 6,         // rarezas: los que dan personalidad a la lista
    }

    /// <summary>
    /// Un logro del catálogo. Ficha muerta: sabe qué pide, no si está hecho.
    /// </summary>
    public readonly struct AchievementDefinition
    {
        public readonly string AchievementId;
        public readonly string DisplayName;

        /// <summary>Lo que se lee antes de conseguirlo. Puede ser una pista.</summary>
        public readonly string Description;

        public readonly AchievementKind Kind;

        /// <summary>Cuánto hay que acumular. 1 para los de «pasó o no pasó».</summary>
        public readonly int Goal;

        public readonly long Reward;

        /// <summary>
        /// Si es cierto, no se enseña la descripción hasta conseguirlo. Para los que
        /// se estropean si los lees antes.
        /// </summary>
        public readonly bool Hidden;

        public AchievementDefinition(string achievementId, string displayName,
                                     string description, AchievementKind kind,
                                     int goal, long reward, bool hidden)
        {
            AchievementId = achievementId;
            DisplayName = displayName;
            Description = description;
            Kind = kind;
            Goal = goal;
            Reward = reward;
            Hidden = hidden;
        }
    }

    /// <summary>Cómo va un logro concreto.</summary>
    public readonly struct AchievementProgress
    {
        public readonly string AchievementId;
        public readonly int Current;
        public readonly int Goal;
        public readonly bool Unlocked;

        public AchievementProgress(string achievementId, int current, int goal, bool unlocked)
        {
            AchievementId = achievementId;
            Current = current;
            Goal = goal;
            Unlocked = unlocked;
        }

        public float Normalized => Goal <= 0 ? 0f : Current / (float)Goal;
    }

    /// <summary>
    /// Los logros: lo que la isla se va apuntando de lo que haces.
    /// </summary>
    /// <remarks>
    /// El servicio no vigila el juego por su cuenta: se suscribe a los eventos que ya
    /// existen y cuenta. Esa es la condición para que añadir un logro nuevo no
    /// obligue a tocar el módulo del que habla — si hiciera falta que economía
    /// «avisara a los logros», cada sistema acabaría conociendo a este.
    ///
    /// Contar es acumulativo y se guarda: un logro de «cocina veinte veces» no puede
    /// perder la cuenta porque cierres el juego.
    /// </remarks>
    public interface IAchievementService
    {
        IReadOnlyList<AchievementDefinition> Catalog { get; }

        bool TryGetDefinition(string achievementId, out AchievementDefinition definition);

        AchievementProgress ProgressOf(string achievementId);

        /// <summary>Cuántos van de cuántos. Para la cabecera de la lista.</summary>
        int UnlockedCount { get; }

        /// <summary>
        /// Suma al contador de un logro y lo desbloquea si llega. Devuelve cierto
        /// solo si lo ha desbloqueado justo ahora, para que quien llame pueda
        /// celebrarlo sin volver a preguntar.
        /// </summary>
        bool Advance(string achievementId, int amount = 1);

        /// <summary>
        /// Deja el contador en ese valor si es mayor que el que había. Para los que
        /// miden un máximo —el día más largo, el mayor ahorro— y no una suma.
        /// </summary>
        bool Record(string achievementId, int value);
    }
}
