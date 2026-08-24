using System.Collections.Generic;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Social.Memory
{
    /// <summary>
    /// Lo que un vecino te cuenta de memoria cuando hablas con él: «Ayer vi a Bea con
    /// Leo», «Todavía estoy enfadada con Mia». No guarda nada: lee la crónica que ya
    /// escribió el tablón y decide si hoy le apetece contarte algo de ella.
    /// </summary>
    /// <remarks>
    /// La vida de la aldea quedaba registrada —amistades, riñas, parejas, bebés— y al
    /// hablar con nadie te mencionaba nada de eso. La memoria existía y la conversación
    /// no la usaba. Este componente es solo el lector: el suceso ya lo guardó
    /// <see cref="IChronicleService"/> con su día y sus nombres puestos.
    ///
    /// Tres filtros deciden si hay historia:
    /// 1. **Frescura** — lo de ayer se cuenta; lo de hace cuatro días, no.
    /// 2. **Verdad presente** — una riña que ya se compuso dejó de ser noticia.
    /// 3. **Ya contado** — a ti no te repite lo que ya te dijo, ni hoy ni nunca.
    ///
    /// Y encima de todo, el dado de la personalidad: que haya cosa que contar no
    /// significa que te la cuente.
    /// </remarks>
    public sealed class ConversationRecall
    {
        /// <summary>
        /// Cuántos días sigue siendo contable lo que pasó. Tres.
        /// </summary>
        /// <remarks>
        /// Lo de ayer está caliente y lo de anteayer aún vale; lo de la semana pasada ya
        /// lo sabe toda la isla y contarlo no dice nada de quien lo cuenta. Con la
        /// ventana corta además el «ya te lo conté» deja de importar a largo plazo: lo
        /// viejo caduca solo.
        /// </remarks>
        public const int FreshDays = 3;

        /// <summary>
        /// Probabilidad de que te cuente algo teniendo algo que contar: 0,20–0,40 según
        /// su eje de Expresión.
        /// </summary>
        /// <remarks>
        /// La probabilidad manda solo en el primer encuentro del día, porque debajo hay
        /// un tope de una historia por vecino y día. A partir de ~0,5 el vecino se
        /// vuelve tablón de anuncios —cada vez que le hablas, el cotilleo del día— y por
        /// debajo de ~0,15 el sistema existe pero no se oye nunca. La media queda en una
        /// de cada tres primeras charlas del día, que es «a veces» y no «siempre».
        /// Públicos a propósito: las pruebas los clavan a 1 o a 0 para quitar el dado.
        /// </remarks>
        public float ChanceFloor = 0.20f;
        public float ChanceCeiling = 0.40f;

        private readonly IIslanderRegistry _registry;
        private readonly GameClock _clock;

        /// <summary>
        /// Donde vive el «ya te lo contó»: las banderas de la partida si la hay, y una
        /// lista propia si no — que caduca con la sesión, pero mientras dure también
        /// recuerda.
        /// </summary>
        private readonly List<string> _flags;

        public ConversationRecall(IIslanderRegistry registry, GameClock clock,
                                  List<string> flags = null)
        {
            _registry = registry;
            _clock = clock;
            _flags = flags ?? new List<string>();
        }

        /// <summary>
        /// Qué le saldría decirle a este vecino si ahora mismo se pusiera a contar,
        /// sin tirar el dado de la personalidad ni dar la cosa por contada.
        /// </summary>
        public string Peek(string islanderId) =>
            TryPick(islanderId, out ChronicleEntry entry, out bool involved)
                ? Compose(islanderId, entry, involved)
                : null;

        /// <summary>
        /// La frase de memoria, con el dado tirado y el suceso apuntado como contado.
        /// False significa «hoy no»: nada fresco, no le apetece, o ya te contó lo suyo.
        /// </summary>
        public bool TryGetRecall(string islanderId, out string line)
        {
            line = null;
            if (!TryPick(islanderId, out ChronicleEntry entry, out bool involved)) return false;
            if (!RollDice(islanderId)) return false;

            line = Compose(islanderId, entry, involved);
            MarkTold(islanderId, entry);
            MarkDay(islanderId);
            return true;
        }

        // ── elegir qué contar ───────────────────────────────────────────────

        /// <summary>
        /// El mejor suceso contable de hoy: propio antes que ajeno, nuevo antes que
        /// viejo, y ninguno que ya se contara o que el presente haya desmentido.
        /// </summary>
        private bool TryPick(string islanderId, out ChronicleEntry entry, out bool involved)
        {
            entry = null;
            involved = false;

            if (!ServiceRegistry.TryGet<IChronicleService>(out var chronicle)) return false;
            if (!_registry.TryGet(islanderId, out var speaker)) return false;

            string name = speaker.Identity.ShortName;
            if (string.IsNullOrEmpty(name)) return false;

            int today = _clock.Day;
            CleanOldFlags(today);

            // Una historia al día por vecino, aunque tenga material de sobra: quien
            // te cuenta tres cosas seguidas cada vez que le ves es un boletín informativo.
            if (_flags.Contains(DayFlag(islanderId, today))) return false;

            var entries = chronicle.Entries;
            int bestScore = int.MinValue;

            // De la más nueva a la más vieja: en cuanto una se pasa de fresca, todas
            // las de atrás son más viejas aún y se puede parar.
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var candidate = entries[i];
                int age = today - candidate.Day;
                if (age > FreshDays) break;
                if (age < 0) continue; // fecha futura: reloj detrás de la crónica

                if (IsTold(islanderId, candidate)) continue;

                bool own = candidate.Text.Contains(name);

                // El reservado habla de lo suyo y hasta ahí; lo de los demás lo cuentan
                // los expresivos, que para eso están.
                if (!own && speaker.Personality.Expression <= 0f) continue;

                if (!StillTrue(speaker, candidate, own)) continue;

                int score = (own ? 100 : 0) - age * 10;
                if (score <= bestScore) continue;

                bestScore = score;
                entry = candidate;
                involved = own;
            }

            return entry != null;
        }

        /// <summary>
        /// Que la crónica no cuente algo que el presente desmintió: la riña que ya se
        /// compuso y la pareja que se rompió dejaron de ser historia contable.
        /// </summary>
        /// <remarks>
        /// Solo veta cuando la agenda dice lo contrario; si no hay ficha no hay manera
        /// de contradecirlo y se permite. Y comprueba el par correcto: lo propio mira
        /// lo que el hablante siente por el otro; el cotilleo entre dos terceros mira
        /// lo que uno de ellos siente por el otro.
        /// </remarks>
        private bool StillTrue(IslanderData speaker, ChronicleEntry entry, bool involved)
        {
            var others = NamedOthers(speaker, entry.Text);
            if (others.Count == 0) return true;

            RelationshipRecord record;
            if (involved)
            {
                if (!speaker.Relationships.TryGet(others[0], out record)) return true;
            }
            else
            {
                if (others.Count < 2) return true;
                if (!_registry.TryGet(others[0], out var first)) return true;
                if (!first.Relationships.TryGet(others[1], out record)) return true;
            }

            Tone tone = ToneOf(entry.Text.ToLowerInvariant());
            if (tone == Tone.Negative && record.Conflict == ConflictStage.None) return false;
            if (tone == Tone.Romance && record.Romance == RomanceStage.Separated) return false;
            return true;
        }

        /// <summary>Los vecinos del censo que salen nombrados en la línea, sin el hablante.</remarks>
        private List<string> NamedOthers(IslanderData speaker, string text)
        {
            var others = new List<string>();
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].Id == speaker.Id) continue;
                string otherName = all[i].Identity.ShortName;
                if (string.IsNullOrEmpty(otherName)) continue;
                if (text.Contains(otherName)) others.Add(all[i].Id);
            }
            return others;
        }

        // ── cómo lo cuenta ──────────────────────────────────────────────────

        private string Compose(string islanderId, ChronicleEntry entry, bool involved)
        {
            var rng = Rng.FromSeed($"recuerdo-frase:{islanderId}:{_clock.Day}");

            // La voz sale de ejes y no de los dieciséis tipos: Expresión decide si
            // cuenta lo ajeno y con cuánta fanfarria lo cuenta. El texto del suceso va
            // tal cual lo escribió la crónica — ya trae nombres y verbo; el prefijo
            // solo pone el tono de quien lo cuenta.
            Tone tone = ToneOf(entry.Text.ToLowerInvariant());
            bool expressive = _registry.TryGet(islanderId, out var speaker)
                             && speaker.Personality.Expression > 0f;

            string[] bank = !involved ? GossipBank(tone)
                            : expressive ? OwnBank(tone)
                            : ReservedBank;

            return bank[rng.Range(0, bank.Length)] + entry.Text;
        }

        private static readonly string[] ReservedBank =
        {
            "Quería decírtelo: ",
            "Si te parece, te cuento: ",
            "Sobre lo del otro día: ",
        };

        private static string[] OwnBank(Tone tone) => tone switch
        {
            Tone.Negative => new[]
            {
                "Todavía me duele: ",
                "No se me pasa: ",
                "Aún estoy así por aquello: ",
            },
            Tone.Neutral => new[]
            {
                "Sobre lo del otro día: ",
                "Quería contártelo: ",
                "Esto te va a gustar: ",
            },
            _ => new[]
            {
                "¿Te has enterado? ",
                "Todavía me hace ilusión: ",
                "¡Qué días nos tocan! ",
            },
        };

        private static string[] GossipBank(Tone tone) => tone switch
        {
            Tone.Negative => new[]
            {
                "Corre que se entera todo el mundo: ",
                "No digas que te lo dije yo: ",
                "Entre nosotros: ",
            },
            Tone.Neutral => new[]
            {
                "Andan contando por ahí: ",
                "¿Te suena esto? ",
                "Anda la cosa movida: ",
            },
            _ => new[]
            {
                "¿Sabes qué? ",
                "Chsss, mira tú: ",
                "Tienes que oír esto: ",
            },
        };

        /// <summary>De qué va la línea, leyendo las palabras del propio titular.</summary>
        /// <remarks>
        /// Las palabras son las de las plantillas del tablón (<c>NewsBoard</c>), que es
        /// quien escribe la crónica. Si mañana cambia una plantilla y aquí no, el peor
        /// caso es un tono equivocado —nunca una frase rota—, porque el veto de verdad
        /// presente falla del lado de permitir.
        /// </remarks>
        private static Tone ToneOf(string lower)
        {
            if (ContainsAny(lower, "discut", "pelea", "gritos", "enemistad", "tirantez",
                            "no se miran", "no se hablan", "rival", "roto", "se acabó"))
                return Tone.Negative;
            if (ContainsAny(lower, "pareja", "casad", "casan", "promet", "colad",
                            "gusta", "junt"))
                return Tone.Romance;
            if (ContainsAny(lower, "nacido", "nace", "boda", "nivel", "estrena", "casa"))
                return Tone.Positive;
            return Tone.Neutral;
        }

        private static bool ContainsAny(string text, params string[] words)
        {
            for (int i = 0; i < words.Length; i++)
                if (text.Contains(words[i])) return true;
            return false;
        }

        private enum Tone { Negative, Romance, Positive, Neutral }

        // ── el dado y la memoria de lo contado ──────────────────────────────

        private bool RollDice(string islanderId)
        {
            if (!_registry.TryGet(islanderId, out var speaker)) return false;

            float t = Mathf.Clamp01((speaker.Personality.Expression + 1f) * 0.5f);
            float chance = Mathf.Lerp(ChanceFloor, ChanceCeiling, t);
            if (chance <= 0f) return false;

            var rng = Rng.FromSeed($"recuerdo-dado:{islanderId}:{_clock.Day}");
            return rng.Chance(chance);
        }

        /// <summary>
        /// El «ya te lo contó» vive en las banderas de la partida, no en memoria: si
        /// guardas y vuelves mañana, no te vuelve a soltar la misma historia.
        /// </summary>
        private void MarkTold(string islanderId, ChronicleEntry entry)
        {
            _flags?.Add(ToldFlag(islanderId, entry));
        }

        private void MarkDay(string islanderId)
        {
            _flags?.Add(DayFlag(islanderId, _clock.Day));
        }

        private bool IsTold(string islanderId, ChronicleEntry entry) =>
            _flags.Contains(ToldFlag(islanderId, entry));

        private static string ToldFlag(string islanderId, ChronicleEntry entry) =>
            $"recuerdo:{islanderId}:{entry.Day}:{Fnv(entry.Text)}";

        private static string DayFlag(string islanderId, int day) =>
            $"recuerdo_dia:{islanderId}:{day}";

        /// <summary>
        /// Las banderas de recuerdos caducan solas: cuando el suceso salió ya de la
        /// ventana, tanto da si se contó. Sin esta limpieza el guardado engordaría una
        /// línea por historia contada para siempre.
        /// </summary>
        private void CleanOldFlags(int today)
        {
            for (int i = _flags.Count - 1; i >= 0; i--)
            {
                string flag = _flags[i];
                if (!flag.StartsWith("recuerdo")) continue;

                var parts = flag.Split(':');
                if (parts.Length < 3 || !int.TryParse(parts[2], out int day)) continue;
                if (today - day <= FreshDays + 1) continue;

                _flags.RemoveAt(i);
            }
        }

        /// <summary>Hash estable entre ejecuciones: el de string no lo garantiza.</summary>
        private static uint Fnv(string text)
        {
            uint hash = 2166136261u;
            for (int i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619u;
            }
            return hash;
        }
    }
}
