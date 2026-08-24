using System.Collections.Generic;
using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Player
{
    /// <summary>
    /// Las cinco vías del protagonista: por dónde va y qué le abre lo siguiente.
    /// </summary>
    /// <remarks>
    /// Cada vía enseña **lo próximo que desbloquea**, no solo la barra. Una barra sola
    /// dice cuánto falta pero no para qué, y entonces subir de nivel es leer un número
    /// más grande. Con lo que viene escrito debajo, la pantalla contesta la única
    /// pregunta que se viene a hacer aquí: qué gano si sigo por este lado.
    ///
    /// Y no hay ranking ni comparación con los vecinos. Las vías son cinco para que
    /// nadie tenga que subirlas todas; una tabla las volvería una lista de deberes.
    /// </remarks>
    public sealed class SkillsPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly Label _headline;
        private readonly VisualElement _lines;
        private readonly VisualElement _conduct;

        /// <summary>
        /// Lo que mandó la última vez que se levantó la pantalla. El ciclo lento de
        /// UiRoot llama a <see cref="Refresh"/> con el panel abierto; sin firma se
        /// reconstruiría cada 0,4 s aunque no se subiera ni un punto de experiencia.
        /// </summary>
        private string _signature;

        /// <summary>Lo que abre cada vía, en orden. Para poder decir qué viene después.</summary>
        private static readonly Dictionary<SkillKind, Unlock[]> Ladder = new()
        {
            [SkillKind.Farming] = new[] { Unlock.BiggerPlot, Unlock.WideWatering,
                                          Unlock.GenerousHarvest },
            [SkillKind.Gathering] = new[] { Unlock.ReadTheNode, Unlock.StrongArms,
                                            Unlock.FastRegrowth, Unlock.DeftHands,
                                            Unlock.NodesOnMap },
            [SkillKind.Crafting] = new[] { Unlock.MiddlingRecipes, Unlock.BatchCrafting,
                                           Unlock.FineRecipes, Unlock.BetterTools,
                                           Unlock.EngagementRing },
            [SkillKind.Social] = new[] { Unlock.Compliment, Unlock.ExtraGift,
                                         Unlock.WarmGestures, Unlock.Courtship,
                                         Unlock.AskFavour, Unlock.Mediate },
            [SkillKind.Village] = new[] { Unlock.AssignJobs, Unlock.UpgradeHomes,
                                          Unlock.HostEvents, Unlock.HostFestivals },
        };

        public SkillsPanel()
        {
            Root = UiTheme.Card("vias");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 460;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Lo que sabes hacer"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _headline = UiTheme.Body("", soft: true);
            _headline.style.marginBottom = 10;
            Root.Add(_headline);

            _lines = new VisualElement();
            Root.Add(_lines);

            // «Cómo te ve la aldea» (§14.3.9) va aquí debajo y no en un botón propio:
            // las dos cosas contestan a la misma pregunta —quién es el protagonista—,
            // una por lo que sabe hacer y otra por cómo lo hace. Separarlas en dos
            // pantallas obligaría a abrir dos para leerse entero.
            _conduct = new VisualElement();
            _conduct.style.marginTop = 12;
            Root.Add(_conduct);
        }

        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            Rebuild();

            // Lo pintado queda firmado: si no se sube nada, el primer Refresh no
            // reconstruye por sorpresa lo que acaba de levantarse.
            _signature = Firma();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Refresco barato: firma niveles, experiencia, candados y conducta, y solo
        /// reconstruye si cambió algo.
        /// </summary>
        /// <remarks>
        /// La experiencia entra redondeada a décimas porque es lo que mueve la barra:
        /// con el valor crudo, un <c>float</c> que gotee por debajo del punto
        /// redibujaría la pantalla entera sin mover ni un píxel. Los candados van
        /// porque el texto de «lo próximo que abre» depende de ellos, no del nivel.
        /// </remarks>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<IPlayerProgression>(out _))
            {
                _signature = null;
                return;
            }

            string candidata = Firma();
            if (candidata == _signature) return;
            _signature = candidata;
            Rebuild();
        }

        /// <summary>Lo que dicta el estado de la progresión y la conducta, en una cadena.</summary>
        private string Firma()
        {
            if (!ServiceRegistry.TryGet<IPlayerProgression>(out var progression))
                return "";

            var firma = new StringBuilder().Append(progression.VillagerLevel);

            foreach (SkillKind skill in System.Enum.GetValues(typeof(SkillKind)))
                firma.Append('|').Append(progression.LevelOf(skill))
                     .Append('|').Append(Mathf.RoundToInt(progression.XpOf(skill) * 10f))
                     .Append('/').Append(Mathf.RoundToInt(progression.XpNeededFor(skill) * 10f));

            foreach (Unlock[] escalones in Ladder.Values)
                for (int i = 0; i < escalones.Length; i++)
                    firma.Append('|').Append(progression.IsUnlocked(escalones[i]) ? '1' : '0');

            if (ServiceRegistry.TryGet<IConductService>(out var conduct))
            {
                var profile = conduct.Profile;
                foreach (Data.Islanders.PersonalityAxis axis in
                         System.Enum.GetValues(typeof(Data.Islanders.PersonalityAxis)))
                    firma.Append('|').Append(Mathf.RoundToInt(profile[axis] * 100f))
                         .Append('/')
                         .Append(Mathf.RoundToInt(conduct.ConfidenceOf(axis) * 100f));
            }

            return firma.ToString();
        }

        public void Rebuild()
        {
            _lines.Clear();

            if (!ServiceRegistry.TryGet<IPlayerProgression>(out var progression))
            {
                _headline.text = "";
                _lines.Add(UiTheme.Body("Esto no está disponible.", soft: true));
                return;
            }

            _headline.text = $"Nivel de aldeano {progression.VillagerLevel} — " +
                             "la media de las cinco, que no se gana por su cuenta";

            foreach (SkillKind skill in System.Enum.GetValues(typeof(SkillKind)))
                _lines.Add(Line(progression, skill));

            RebuildConduct();
        }

        /// <summary>
        /// Cómo te ve la aldea: cuatro rasgos en palabras, sacados de lo que haces.
        /// </summary>
        /// <remarks>
        /// **En palabras y no en números**, y con «todavía no está claro» cuando no hay
        /// muestras suficientes — que además es verdad. Un número invita a optimizarlo;
        /// una frase invita a reconocerse.
        ///
        /// Nadie la ha escrito a mano y describe al jugador de verdad, que es lo bonito
        /// de que la personalidad del protagonista salga de su conducta.
        /// </remarks>
        private void RebuildConduct()
        {
            _conduct.Clear();
            if (!ServiceRegistry.TryGet<IConductService>(out var conduct)) return;

            var title = UiTheme.Title("Cómo te ve la aldea");
            _conduct.Add(title);

            var profile = conduct.Profile;

            foreach (Data.Islanders.PersonalityAxis axis in
                     System.Enum.GetValues(typeof(Data.Islanders.PersonalityAxis)))
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 3;

                var name = UiTheme.Body(AxisName(axis), soft: true);
                name.style.width = 110;
                row.Add(name);

                float confidence = conduct.ConfidenceOf(axis);
                var word = UiTheme.Body(confidence < 0.5f
                    ? "todavía no está claro"
                    : Trait(axis, profile[axis]));
                row.Add(word);

                _conduct.Add(row);
            }
        }

        private static string AxisName(Data.Islanders.PersonalityAxis axis) => axis switch
        {
            Data.Islanders.PersonalityAxis.Energy => "Ritmo",
            Data.Islanders.PersonalityAxis.Expression => "Al hablar",
            Data.Islanders.PersonalityAxis.Attitude => "Con la gente",
            _ => "Cómo miras",
        };

        private static string Trait(Data.Islanders.PersonalityAxis axis, float value)
        {
            bool positive = value > 0f;

            return axis switch
            {
                Data.Islanders.PersonalityAxis.Energy =>
                    positive ? "no paras quieto" : "vas con calma",
                Data.Islanders.PersonalityAxis.Expression =>
                    positive ? "lo dices todo" : "te guardas las cosas",
                Data.Islanders.PersonalityAxis.Attitude =>
                    positive ? "siempre acompañado" : "vas a tu aire",
                _ => positive ? "estás en las nubes" : "tienes los pies en el suelo",
            };
        }

        private static VisualElement Line(IPlayerProgression progression, SkillKind skill)
        {
            var card = new VisualElement();
            card.style.paddingLeft = card.style.paddingRight = 12;
            card.style.paddingTop = card.style.paddingBottom = 10;
            card.style.marginBottom = 6;
            card.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(card, UiTheme.Radius);

            int level = progression.LevelOf(skill);

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 6;

            var name = UiTheme.Body(Gates.NameOf(skill));
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.flexGrow = 1;
            header.Add(name);
            header.Add(UiTheme.Chip($"nivel {level}", ColourOf(skill)));
            card.Add(header);

            card.Add(Bar(progression, skill, level));
            card.Add(NextStep(progression, skill, level));
            return card;
        }

        private static VisualElement Bar(IPlayerProgression progression, SkillKind skill, int level)
        {
            var track = new VisualElement();
            track.style.height = 8;
            track.style.marginBottom = 6;
            track.style.backgroundColor = UiTheme.Cream;
            UiTheme.SetRadius(track, UiTheme.RadiusPill);

            float needed = progression.XpNeededFor(skill);

            var fill = new VisualElement();
            fill.style.height = 8;
            fill.style.backgroundColor = ColourOf(skill);
            // Al tope la barra se pinta llena en vez de vacía: dividir entre cero daría
            // cero, y la vía terminada se leería como si estuviera empezando.
            fill.style.width = Length.Percent(
                needed <= 0f ? 100f : Mathf.Clamp01(progression.XpOf(skill) / needed) * 100f);
            UiTheme.SetRadius(fill, UiTheme.RadiusPill);
            track.Add(fill);

            return track;
        }

        /// <summary>Lo próximo que abre esta vía, que es la razón para seguirla.</summary>
        private static VisualElement NextStep(IPlayerProgression progression, SkillKind skill,
                                              int level)
        {
            if (level >= SkillSet.MaxLevel)
                return UiTheme.Body("Al tope. Ya no hay más que aprender por aquí.", soft: true);

            if (Ladder.TryGetValue(skill, out var ladder))
            {
                for (int i = 0; i < ladder.Length; i++)
                {
                    if (progression.IsUnlocked(ladder[i])) continue;

                    progression.RequirementFor(ladder[i], out _, out int at);
                    return UiTheme.Body($"En el {at}: {Describe(ladder[i])}", soft: true);
                }
            }

            return UiTheme.Body("Nada nuevo por delante; sube por el gusto de subir.", soft: true);
        }

        /// <summary>
        /// Qué es cada desbloqueo, en una frase.
        /// </summary>
        /// <remarks>
        /// En cristiano y no con el nombre del enum: lo que el jugador necesita saber es
        /// qué va a poder hacer, no cómo se llama por dentro.
        /// </remarks>
        public static string Describe(Unlock unlock) => unlock switch
        {
            Unlock.BiggerPlot => "el huerto se hace más grande",
            Unlock.GenerousHarvest => "una de cada cuatro cosechas rinde de más",
            Unlock.ReadTheNode => "sabrás qué da cada árbol antes de talarlo",
            Unlock.StrongArms => "un golpe menos en árboles y rocas",
            Unlock.FastRegrowth => "los nodos vuelven un día antes",
            Unlock.DeftHands => "flores y hierbas rinden el doble",
            Unlock.MiddlingRecipes => "recetas de nivel medio",
            Unlock.BatchCrafting => "hacer cinco cosas de una vez",
            Unlock.FineRecipes => "las recetas más finas del catálogo",
            Unlock.ExtraGift => "un regalo más al día",
            Unlock.Courtship => "declararte a un vecino",
            Unlock.AssignJobs => "repartir los trabajos de la aldea",
            Unlock.UpgradeHomes => "pagar la obra de las casas de los vecinos",
            Unlock.BetterTools => "fabricar las herramientas buenas",
            Unlock.Compliment => "halagar a un vecino",
            Unlock.WarmGestures => "abrazar y jugar con alguien",
            Unlock.EngagementRing => "fabricar el anillo de compromiso",
            Unlock.AskFavour => "pedirle un favor a un vecino",
            Unlock.Mediate => "mediar cuando dos se han enfadado",
            Unlock.HostEvents => "organizar una fiesta en la aldea",
            Unlock.HostFestivals => "organizar festivales",
            Unlock.WideWatering => "la regadera moja tres casillas de una vez",
            _ => "verás en el mapa lo que está listo para recoger",
        };

        private static Color ColourOf(SkillKind skill) => skill switch
        {
            SkillKind.Farming => UiTheme.Sage,
            SkillKind.Gathering => UiTheme.Mint,
            SkillKind.Crafting => UiTheme.Butter,
            SkillKind.Social => UiTheme.Rose,
            _ => UiTheme.Lavender,
        };
    }
}
