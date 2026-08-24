using System;
using Nimbo.Core.Save;
using Nimbo.Core.Time;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// Lo que el menú necesita saber de una partida guardada para poder ofrecerla:
    /// qué día va, cuánta gente vive allí y cuánto hace que no la abres.
    /// </summary>
    /// <remarks>
    /// Lee el fichero y no lo toca. El menú se dibuja antes de que exista un solo
    /// servicio, así que no puede preguntarle a nadie: abre el JSON, saca cuatro
    /// datos y lo suelta. Si el guardado está roto o no existe, <see cref="Exists"/>
    /// sale en falso y el menú enseña «Partida nueva» y ya está — nunca un error.
    /// </remarks>
    public readonly struct SaveSummary
    {
        public readonly bool Exists;
        public readonly int Day;
        public readonly string Clock;
        public readonly int Islanders;
        public readonly long Coins;

        /// <summary>«hace 3 horas», «ayer»… en cristiano. Vacío si no se sabe.</summary>
        public readonly string LastPlayed;

        private SaveSummary(int day, string clock, int islanders, long coins, string lastPlayed)
        {
            Exists = true;
            Day = day;
            Clock = clock;
            Islanders = islanders;
            Coins = coins;
            LastPlayed = lastPlayed;
        }

        public static SaveSummary Read()
        {
            if (!SaveSystem.SaveExists) return default;

            var save = SaveSystem.Read();
            if (save == null) return default;

            // El reloj se construye solo para formatear la hora igual que el HUD.
            // Es un objeto de usar y tirar: no lo registra nadie y no avanza.
            var clock = new GameClock(save.ElapsedMinutes);

            return new SaveSummary(
                clock.Day,
                clock.FormatClock(),
                save.Islanders?.Count ?? 0,
                save.Wallet.Coins,
                Ago(save.SavedUtc));
        }

        /// <summary>Una línea que da la sensación de tiempo, no la fecha exacta.</summary>
        /// <remarks>
        /// Pública y pura a propósito: solo mira la fecha que le llega, así que las
        /// pruebas pueden clavar cada tramo sin partida ni guardado — aquí vivió
        /// «hace 1 minutos» y solo una prueba que mire el texto lo mantiene muerto.
        /// </remarks>
        public static string Ago(string savedUtc)
        {
            if (string.IsNullOrEmpty(savedUtc)) return "";
            if (!DateTime.TryParse(savedUtc, null,
                    System.Globalization.DateTimeStyles.RoundtripKind, out var saved)) return "";

            var away = DateTime.UtcNow - saved;
            if (away.TotalMinutes < 2) return "hace un momento";

            int minutos = (int)away.TotalMinutes;
            if (minutos < 60) return $"hace {minutos} {UiTheme.Plural(minutos, "minuto")}";

            if (away.TotalHours < 2) return "hace una hora";

            int horas = (int)away.TotalHours;
            if (horas < 24) return $"hace {horas} {UiTheme.Plural(horas, "hora")}";

            if (away.TotalDays < 2) return "ayer";

            int dias = (int)away.TotalDays;
            if (dias < 30) return $"hace {dias} {UiTheme.Plural(dias, "día", "días")}";

            return "hace bastante";
        }

        /// <summary>La línea que se lee bajo el botón de continuar.</summary>
        public string Line =>
            !Exists ? "" :
            $"Día {Day}, {Clock} · {Islanders} habitantes · {Coins} nimbos";
    }
}
