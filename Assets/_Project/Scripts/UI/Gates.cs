using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Player;

namespace Nimbo.UI
{
    /// <summary>
    /// Si el protagonista ya se ha ganado algo, y cómo se le dice que todavía no.
    /// </summary>
    /// <remarks>
    /// Las puertas se preguntan desde la pantalla y no desde el fondo de cada sistema
    /// (§12.5). Eso tiene una consecuencia buena y una que hay que tener presente:
    /// la buena es que una partida vieja carga igual y lo único que cambia es qué
    /// botones se ven; la otra es que el servicio de debajo **sigue aceptando** la
    /// llamada, así que si algún día algo que no sea la pantalla reparte trabajos, la
    /// puerta habrá que ponerla también allí.
    ///
    /// Y nunca se esconde el botón sin decir nada. Un hueco vacío parece un fallo; «te
    /// hace falta Aldea 2» es una razón para seguir jugando.
    /// </remarks>
    public static class Gates
    {
        public static bool Allows(Unlock unlock) =>
            !ServiceRegistry.TryGet<IPlayerProgression>(out var progression)
            || progression.IsUnlocked(unlock);

        /// <param name="missing">La frase que explica qué falta. Vacía si no falta nada.</param>
        public static bool Allows(Unlock unlock, out string missing)
        {
            missing = "";

            if (!ServiceRegistry.TryGet<IPlayerProgression>(out var progression)) return true;
            if (progression.IsUnlocked(unlock)) return true;

            progression.RequirementFor(unlock, out var skill, out int level);
            missing = $"Te hace falta {NameOf(skill)} {level}. Vas por el {progression.LevelOf(skill)}.";
            return false;
        }

        /// <summary>«Oficio 5», para cuando no cabe la frase entera.</summary>
        public static string Short(Unlock unlock)
        {
            if (!ServiceRegistry.TryGet<IPlayerProgression>(out var progression)) return "";

            progression.RequirementFor(unlock, out var skill, out int level);
            return $"{NameOf(skill)} {level}";
        }

        public static string NameOf(SkillKind skill) => skill switch
        {
            SkillKind.Farming => "Cultivo",
            SkillKind.Gathering => "Recolección",
            SkillKind.Crafting => "Oficio",
            SkillKind.Social => "Convivencia",
            _ => "Aldea",
        };
    }
}
