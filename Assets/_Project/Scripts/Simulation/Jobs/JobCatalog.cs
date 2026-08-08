using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Simulation.Jobs
{
    /// <summary>
    /// Qué es cada oficio: dónde se trabaja, cuánto paga y a quién le pega.
    /// </summary>
    /// <remarks>
    /// La afinidad se define como el perfil de personalidad ideal del puesto, y se
    /// mide por distancia. Así no hay que escribir a mano una tabla de 8 oficios × 16
    /// tipos: el jardinero quiere a alguien calmado y práctico, y quien más se le
    /// parezca es quien mejor lo hará, sea del tipo que sea.
    /// </remarks>
    public static class JobCatalog
    {
        public readonly struct Entry
        {
            public readonly JobKind Kind;
            public readonly string DisplayName;
            public readonly string ZoneId;
            public readonly int BaseWage;

            /// <summary>El perfil de quien haría este trabajo de maravilla.</summary>
            public readonly PersonalityProfile Ideal;

            /// <summary>Hora a la que empieza el turno, en hora de juego.</summary>
            public readonly int StartHour;
            public readonly int Hours;

            public Entry(JobKind kind, string name, string zoneId, int wage,
                         float energy, float expression, float attitude, float outlook,
                         int startHour, int hours)
            {
                Kind = kind;
                DisplayName = name;
                ZoneId = zoneId;
                BaseWage = wage;
                Ideal = new PersonalityProfile(energy, expression, attitude, outlook);
                StartHour = startHour;
                Hours = hours;
            }
        }

        /// <summary>
        /// Los ocho oficios. Los sueldos van de 18 a 34 nimbos por turno: con el bono
        /// diario de 20 y los premios de las peticiones, un habitante de rango 3
        /// mantiene a su dueño sin que la economía se desborde.
        /// </summary>
        public static readonly Entry[] All =
        {
            new Entry(JobKind.Cook, "Cocinero", "zona_tienda_comida", 24,
                      energy: 0.3f, expression: 0.1f, attitude: 0.5f, outlook: -0.4f,
                      startHour: 10, hours: 5),

            new Entry(JobKind.Shopkeeper, "Tendero", "zona_tienda_muebles", 22,
                      energy: 0.1f, expression: 0.6f, attitude: 0.8f, outlook: -0.2f,
                      startHour: 10, hours: 6),

            new Entry(JobKind.Gardener, "Jardinero", "zona_parque", 18,
                      energy: -0.3f, expression: -0.3f, attitude: -0.4f, outlook: -0.5f,
                      startHour: 8, hours: 5),

            new Entry(JobKind.Artist, "Artista", "zona_escenario", 26,
                      energy: -0.1f, expression: 0.7f, attitude: -0.2f, outlook: 0.9f,
                      startHour: 13, hours: 4),

            new Entry(JobKind.Builder, "Constructor", "zona_plaza", 28,
                      energy: 0.8f, expression: -0.2f, attitude: 0.2f, outlook: -0.7f,
                      startHour: 8, hours: 6),

            new Entry(JobKind.Musician, "Músico", "zona_escenario", 30,
                      energy: 0.4f, expression: 0.9f, attitude: 0.3f, outlook: 0.6f,
                      startHour: 17, hours: 4),

            new Entry(JobKind.Guide, "Guía", "zona_embarcadero", 34,
                      energy: 0.7f, expression: 0.5f, attitude: 0.8f, outlook: 0.2f,
                      startHour: 9, hours: 6),

            new Entry(JobKind.Librarian, "Bibliotecario", "zona_plaza", 20,
                      energy: -0.6f, expression: -0.6f, attitude: -0.3f, outlook: 0.3f,
                      startHour: 11, hours: 5),
        };

        public static Entry Get(JobKind kind)
        {
            for (int i = 0; i < All.Length; i++)
                if (All[i].Kind == kind) return All[i];

            Debug.LogWarning($"JobCatalog: no existe el oficio {kind}");
            return All[0];
        }

        public static string NameOf(JobKind kind) =>
            kind == JobKind.None ? "Sin trabajo" : Get(kind).DisplayName;

        /// <summary>
        /// Cuánto le pega ese oficio a esa personalidad, de 0 a 1. Uno es el trabajo
        /// de su vida; cero es el que le hará odiar los lunes.
        /// </summary>
        public static float Affinity(in PersonalityProfile profile, JobKind kind)
        {
            if (kind == JobKind.None) return 0f;
            return 1f - PersonalityProfile.Distance(profile, Get(kind).Ideal);
        }

        /// <summary>
        /// El sueldo de un turno. El rango pesa más que la afinidad a propósito: da
        /// igual lo que te guste tu trabajo, lo que sube el sueldo es la veteranía.
        /// </summary>
        public static int Wage(JobKind kind, int rank, float affinity)
        {
            if (kind == JobKind.None) return 0;

            float wage = Get(kind).BaseWage
                       * (1f + (rank - 1) * 0.35f)     // rango 5 paga el doble que el 1
                       * (0.75f + affinity * 0.5f);    // y el gusto mueve un ±25 %
            return Mathf.RoundToInt(wage);
        }
    }
}
