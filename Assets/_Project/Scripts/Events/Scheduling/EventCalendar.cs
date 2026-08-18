using System.Collections.Generic;
using System.Linq;

namespace Nimbo.Events.Scheduling
{
    /// <summary>
    /// El catálogo de todos los eventos que pueden pasar en la isla.
    /// Están en código, no en JSON, porque son pocos y sus reglas no
    /// las toca un diseñador: las toca un programador.
    /// </summary>
    public static class EventCalendar
    {
        static readonly List<EventDefinition> _all;

        static EventCalendar()
        {
            _all = new List<EventDefinition>
            {
                // --- sorteo diario ---------------------------------------------------

                new EventDefinition(
                    "concert", "Concierto en el escenario",
                    "Un isleño canta o toca en el escenario. Los demás miran, aplauden… o abuchean.",
                    17, 22, "saturday", 3, 5, "zona_escenario", 2f),

                new EventDefinition(
                    "island_festival", "Festival de la isla",
                    "Todos los isleños se reúnen en la plaza. Música, baile y puestos de comida.",
                    14, 20, "sunday", 5, 5, "zona_plaza", 3f, isFestival: true),

                new EventDefinition(
                    "market_day", "Día de mercado",
                    "La tienda de muebles saca su catálogo completo. Los prácticos van de compras.",
                    8, 14, "monday", 3, 3, null, 2f),

                new EventDefinition(
                    "talent_show", "Concurso de talentos",
                    "Cada isleño muestra lo que sabe hacer. Unos cantan, otros hacen malabares, otros… lo intentan.",
                    16, 21, "saturday", 4, 5, "zona_escenario", 1.5f, isFestival: true),

                new EventDefinition(
                    "starry_night", "Noche de estrellas",
                    "El cielo se llena de estrellas fugaces. Los isleños piden deseos.",
                    20, 24, "any", 2, 4, "zona_parque", 2.5f),

                new EventDefinition(
                    "rainy_day", "Día de lluvia",
                    "Llovizna suave sobre la isla. Los isleños se quedan dentro, leyendo o cocinando.",
                    6, 18, "any", 1, 2, null, 3f),

                new EventDefinition(
                    "storm", "Tormenta",
                    "Truenos y viento. Los isleños se refugian y cuentan historias de miedo.",
                    18, 24, "any", 4, 4, null, 1f),

                new EventDefinition(
                    "aurora", "Aurora nimba",
                    "Luces de colores bailan en el cielo nocturno. Imposible no mirar.",
                    22, 24, "any", 3, 4, null, 1.5f),

                new EventDefinition(
                    "visitor_arrival", "Llegada de un visitante",
                    "Un viajero de otra isla aparece con objetos raros e historias nuevas.",
                    10, 18, "any", 4, 5, "zona_embarcadero", 1f),

                new EventDefinition(
                    "inspiration_day", "Día de inspiración",
                    "Los soñadores sienten un chispazo creativo. Pueden componer, pintar o escribir.",
                    6, 14, "wednesday", 2, 3, null, 1.5f),

                new EventDefinition(
                    "siesta_colectiva", "Siesta colectiva",
                    "Toda la isla se tumba un rato. Hasta los tenderos echan la persiana.",
                    14, 16, "sunday", 2, 3, "zona_parque", 2f),

                // --- disparados, no sorteados ----------------------------------------

                new EventDefinition(
                    "birthday", "Cumpleaños",
                    "Un isleño cumple años. Los demás le felicitan y le traen regalos.",
                    8, 20, "any", 1, 1, null, 0f, isTriggered: true),

                new EventDefinition(
                    "shared_dream", "Sueño compartido",
                    "Dos isleños sueñan el uno con el otro. Al despertar, algo ha cambiado.",
                    22, 6, "any", 2, 2, null, 0f, isTriggered: true),

                new EventDefinition(
                    "bridge_appears", "El puente aparece",
                    "Un puente de nubes conecta la isla con el embarcadero. Algo nuevo espera al otro lado.",
                    6, 18, "any", 12, 10, null, 0f, isTriggered: true),

                new EventDefinition(
                    "blind_date", "Cita a ciegas",
                    "Dos isleños que no se conocen son presentados. Puede salir bien, mal o regular.",
                    16, 21, "friday", 4, 4, "zona_parque", 1.5f),
            };
        }

        public static IReadOnlyList<EventDefinition> All => _all;

        public static EventDefinition ById(string id) => _all.FirstOrDefault(e => e.Id == id);

        /// <summary>Eventos que entran en el sorteo horario (los disparados van por otro lado).</summary>
        public static IEnumerable<EventDefinition> Schedulable()
        {
            for (int i = 0; i < _all.Count; i++)
                if (!_all[i].IsTriggered) yield return _all[i];
        }

        /// <summary>Eventos que saltan cuando su condición se cumple.</summary>
        public static IEnumerable<EventDefinition> Triggered()
        {
            for (int i = 0; i < _all.Count; i++)
                if (_all[i].IsTriggered) yield return _all[i];
        }
    }
}
