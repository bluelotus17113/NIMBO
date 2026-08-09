using Nimbo.Core.Util;

namespace Nimbo.CharacterCreator.Presets
{
    /// <summary>
    /// Nombres para los habitantes que llegan solos a la isla.
    /// </summary>
    /// <remarks>
    /// Van sin apellido y sin marcar género: en este juego el nombre lo cambia el
    /// jugador en dos segundos, y lo único que tiene que hacer la lista es no repetirse
    /// y sonar a gente, no a inventario.
    /// </remarks>
    public static class NameBank
    {
        private static readonly string[] Names =
        {
            "Marta", "Nico", "Elsa", "Bruno", "Vera", "Iker", "Lola", "Teo",
            "Cira", "Manu", "Nuria", "Álex", "Bea", "Dani", "Sole", "Hugo",
            "Rita", "Pablo", "Yaiza", "Óscar", "Delia", "Iván", "Mirta", "Leo",
            "Ainhoa", "Rubén", "Chelo", "Gael", "Noa", "Simón", "Ruth", "Ismael",
            "Paula", "Andrés", "Lucía", "Jorge", "Berta", "Adrián", "Elena", "Mateo",
            "Carmen", "Sergio", "Irene", "Damián", "Rosa", "Julián", "Alba", "Emilio",
        };

        /// <summary>Apodos que otros habitantes usan. Vacío significa «que use el nombre».</summary>
        private static readonly string[] Nicknames =
        {
            "", "", "", "", "", "", "", "",
            "Chispa", "Nube", "Tuerca", "Brisa", "Cometa", "Farol", "Ancla", "Musgo",
        };

        public static string RandomName(ref Rng rng) => Names[rng.Range(0, Names.Length)];

        public static string RandomNickname(ref Rng rng) => Nicknames[rng.Range(0, Nicknames.Length)];

        /// <summary>
        /// Un apodo libre, o vacío si a este habitante no le toca ninguno.
        /// </summary>
        /// <remarks>
        /// Hace falta porque el apodo tapa al nombre: la lista de habitantes enseña
        /// <c>ShortName</c>, que prefiere el apodo si lo hay. Sorteando el apodo sin
        /// mirar quién más lo tiene, una isla de tres podía enseñar «Cometa, Cometa,
        /// Nico» — dos vecinos distintos con el mismo botón. Que el nombre de pila
        /// fuera único no servía de nada porque no era el que se veía.
        ///
        /// Solo la mitad del banco lleva apodo, y eso se respeta: si sale hueco, se
        /// queda hueco. Un apodo que tiene todo el mundo deja de ser un apodo.
        /// </remarks>
        public static string UnusedNickname(ref Rng rng, System.Func<string, bool> isTaken)
        {
            int start = rng.Range(0, Nicknames.Length);
            if (string.IsNullOrEmpty(Nicknames[start])) return "";

            for (int i = 0; i < Nicknames.Length; i++)
            {
                string candidate = Nicknames[(start + i) % Nicknames.Length];
                if (string.IsNullOrEmpty(candidate)) continue;
                if (!isTaken(candidate)) return candidate;
            }

            // Todos cogidos: mejor sin apodo que repetido. Con doce habitantes como
            // mucho y ocho apodos, es una isla muy poblada y muy apodada.
            return "";
        }

        /// <summary>Un nombre que no esté cogido. Si se agotan, añade un número al final.</summary>
        public static string UnusedName(ref Rng rng, System.Func<string, bool> isTaken)
        {
            int start = rng.Range(0, Names.Length);
            for (int i = 0; i < Names.Length; i++)
            {
                string candidate = Names[(start + i) % Names.Length];
                if (!isTaken(candidate)) return candidate;
            }

            for (int suffix = 2; suffix < 100; suffix++)
            {
                string candidate = $"{Names[start]} {suffix}";
                if (!isTaken(candidate)) return candidate;
            }
            return Names[start];
        }

        /// <summary>Un cumpleaños al azar, en formato «MM-DD».</summary>
        public static string RandomBirthday(ref Rng rng)
        {
            int month = rng.Range(1, 13);
            int daysInMonth = month switch
            {
                2 => 28,
                4 or 6 or 9 or 11 => 30,
                _ => 31,
            };
            return $"{month:00}-{rng.Range(1, daysInMonth + 1):00}";
        }
    }
}
