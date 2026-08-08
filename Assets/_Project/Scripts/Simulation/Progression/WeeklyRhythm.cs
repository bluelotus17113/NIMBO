using Nimbo.Core.Time;
using UnityEngine;

namespace Nimbo.Simulation.Progression
{
    public enum Weekday
    {
        Monday = 0, Tuesday = 1, Wednesday = 2, Thursday = 3,
        Friday = 4, Saturday = 5, Sunday = 6,
    }

    /// <summary>
    /// Da forma a la semana: cada día tiene algo suyo.
    /// </summary>
    /// <remarks>
    /// Sin esto, todos los días de juego son idénticos y la sensación es de bucle. Con
    /// esto hay un motivo para entrar el martes distinto del motivo del sábado, y el
    /// jugador empieza a decir «hoy toca mercadillo» — que es exactamente el enganche
    /// que se busca en un juego de sesión corta.
    ///
    /// Los bonos son suaves a propósito: nadie debería sentir que ha perdido algo por
    /// no entrar un jueves.
    /// </remarks>
    public static class WeeklyRhythm
    {
        public static Weekday DayOf(int gameDay) => (Weekday)((gameDay - 1) % 7);

        public static Weekday Today(GameClock clock) => DayOf(clock.Day);

        public static string NameOf(Weekday day) => day switch
        {
            Weekday.Monday => "lunes",
            Weekday.Tuesday => "martes",
            Weekday.Wednesday => "miércoles",
            Weekday.Thursday => "jueves",
            Weekday.Friday => "viernes",
            Weekday.Saturday => "sábado",
            _ => "domingo",
        };

        /// <summary>La frase que sale en el HUD al empezar el día.</summary>
        public static string Headline(Weekday day) => day switch
        {
            Weekday.Monday => "Lunes de sueldo: hoy los turnos pagan más.",
            Weekday.Tuesday => "Martes tranquilo. Buen día para reformar una casa.",
            Weekday.Wednesday => "Miércoles de mercadillo: las tiendas traen género raro.",
            Weekday.Thursday => "Jueves de visitas. La gente se busca más que otros días.",
            Weekday.Friday => "Viernes: por la tarde hay música en el escenario.",
            Weekday.Saturday => "Sábado de fiesta. Todo el mundo está de mejor humor.",
            _ => "Domingo de descanso. Nadie trabaja y nadie tiene prisa.",
        };

        /// <summary>Multiplicador del sueldo. El lunes paga más y el domingo no se trabaja.</summary>
        public static float WageMultiplier(Weekday day) => day switch
        {
            Weekday.Monday => 1.35f,
            Weekday.Saturday => 1.15f,
            Weekday.Sunday => 0f,
            _ => 1f,
        };

        /// <summary>Cuánto más suben las amistades ese día.</summary>
        public static float SocialMultiplier(Weekday day) => day switch
        {
            Weekday.Thursday => 1.4f,
            Weekday.Saturday => 1.5f,
            Weekday.Sunday => 1.2f,
            _ => 1f,
        };

        /// <summary>Cuántos objetos extra saca cada tienda ese día.</summary>
        public static int ExtraStock(Weekday day) => day switch
        {
            Weekday.Wednesday => 4,
            Weekday.Saturday => 2,
            _ => 0,
        };

        /// <summary>El sábado la isla amanece contenta. Es el único empujón directo al ánimo.</summary>
        public static float MoodBonus(Weekday day) => day switch
        {
            Weekday.Saturday => 8f,
            Weekday.Sunday => 4f,
            _ => 0f,
        };

        /// <summary>El domingo nadie trabaja.</summary>
        public static bool IsWorkday(Weekday day) => day != Weekday.Sunday;

        /// <summary>El descanso del domingo recupera más energía.</summary>
        public static float RestMultiplier(Weekday day) =>
            day == Weekday.Sunday ? 1.3f : 1f;

        /// <summary>Un resumen de una línea del día, para el tablón de noticias.</summary>
        public static string Summary(int gameDay)
        {
            var day = DayOf(gameDay);
            return $"Día {gameDay}, {NameOf(day)}. {Headline(day)}";
        }
    }
}
