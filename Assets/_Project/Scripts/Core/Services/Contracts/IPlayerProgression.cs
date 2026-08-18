using Nimbo.Data.Player;

namespace Nimbo.Core.Services.Contracts
{
    /// <summary>
    /// Lo que el protagonista se ha ganado el derecho a hacer.
    /// </summary>
    /// <remarks>
    /// Cada uno cuelga de una vía y de un nivel, y todos son cosas que **se notan al
    /// usarlas**: un botón que aparece, un golpe menos, una fila más de huerto. Nada de
    /// porcentajes invisibles, que es lo que convierte subir de nivel en leer un
    /// número.
    ///
    /// Con su número escrito porque acaban en las partidas y en los avisos guardados.
    /// </remarks>
    public enum Unlock
    {
        // ── Cultivo ──────────────────────────────────────────────────────────
        /// <summary>La parcela útil crece: 4×3 al empezar, 8×6 al final.</summary>
        BiggerPlot = 0,

        /// <summary>Una de cada cuatro cosechas rinde una unidad de más.</summary>
        GenerousHarvest = 1,

        // ── Recolección ──────────────────────────────────────────────────────
        /// <summary>El cartel dice qué es y qué suelta antes de darle el primer golpe.</summary>
        ReadTheNode = 2,

        /// <summary>Un golpe menos en árboles y rocas.</summary>
        StrongArms = 3,

        /// <summary>Los nodos vuelven un día antes.</summary>
        FastRegrowth = 4,

        /// <summary>Flores y hierbas rinden el doble.</summary>
        DeftHands = 5,

        // ── Oficio ───────────────────────────────────────────────────────────
        /// <summary>Recetas de nivel medio.</summary>
        MiddlingRecipes = 6,

        /// <summary>Hacer cinco de una vez.</summary>
        BatchCrafting = 7,

        /// <summary>Las recetas más caras del catálogo.</summary>
        FineRecipes = 8,

        // ── Convivencia ──────────────────────────────────────────────────────
        /// <summary>Un regalo más al día.</summary>
        ExtraGift = 9,

        /// <summary>Declararse a un vecino (§14).</summary>
        Courtship = 10,

        // ── Aldea ────────────────────────────────────────────────────────────
        /// <summary>Repartir los trabajos de la aldea (§15.1).</summary>
        AssignJobs = 11,

        /// <summary>Pagar la obra de las casas de los vecinos (§15.2).</summary>
        UpgradeHomes = 12,

        // ── el segundo escalón ───────────────────────────────────────────────
        /// <summary>Fabricar las herramientas de nivel 2 (§12.4).</summary>
        BetterTools = 13,

        /// <summary>La regadera moja tres casillas en línea, sea del escalón que sea.</summary>
        WideWatering = 14,

        /// <summary>Los nodos listos para recoger salen en el mapa.</summary>
        NodesOnMap = 15,

        // ── el trato con la gente ────────────────────────────────────────────
        /// <summary>Halagar a un vecino.</summary>
        Compliment = 16,

        /// <summary>Abrazar y jugar con alguien.</summary>
        WarmGestures = 17,

        /// <summary>Fabricar el anillo de compromiso (§14.5).</summary>
        EngagementRing = 18,
    }

    /// <summary>
    /// Las cinco vías del protagonista y lo que abren.
    /// </summary>
    /// <remarks>
    /// El servicio **no llama a nadie**: se suscribe a los avisos que la isla ya
    /// publica —labrar, cosechar, talar, craftear, regalar, atender una petición— y
    /// cuenta. Es el <c>EventBus</c> haciendo lo que se diseñó para hacer, y por eso
    /// añadir la progresión no cambió ni una línea de cómo se labra o cómo se tala.
    ///
    /// Las puertas se preguntan desde arriba —desde la pantalla, o desde el sitio que
    /// reparte el premio— y no desde el fondo de cada sistema. Así una partida vieja
    /// carga igual y lo único que cambia es qué botones se ven.
    /// </remarks>
    public interface IPlayerProgression
    {
        int LevelOf(SkillKind skill);

        /// <summary>Experiencia dentro del nivel actual, y cuánta hace falta para el siguiente.</summary>
        float XpOf(SkillKind skill);
        float XpNeededFor(SkillKind skill);

        /// <summary>La media de las cinco, que es lo que sale en el HUD.</summary>
        int VillagerLevel { get; }

        bool IsUnlocked(Unlock unlock);

        /// <summary>Qué vía y qué nivel hacen falta para eso. Para poder explicarlo.</summary>
        void RequirementFor(Unlock unlock, out SkillKind skill, out int level);

        /// <summary>
        /// Suma experiencia a mano. La usan el tutorial y las pruebas; el juego normal
        /// no la llama, porque la progresión se entera sola de lo que pasa.
        /// </summary>
        void Grant(SkillKind skill, float xp);
    }
}
