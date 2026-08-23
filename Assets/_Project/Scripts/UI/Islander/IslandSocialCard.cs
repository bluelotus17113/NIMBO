using System.Collections.Generic;
using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Social;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// Quién anda con quién en la isla, hoy: parejas, flechazos y riñas abiertas.
    /// </summary>
    /// <remarks>
    /// Hasta ahora contestar esa pregunta pedía abrir las fichas de uno en uno y
    /// recomponer el grafo de cabeza. Es exactamente la enfermedad que ya mató a las
    /// peticiones —solo se veían entrando en cada ficha— y que el tablón curó: el
    /// sistema estaba encendido y la única forma de mirarlo lo dejaba en nada.
    ///
    /// No calcula nada. Lee las agendas que ya escribe el servicio social y nombra cada
    /// estado con <see cref="SocialLabels"/>, las mismas palabras de «Con quién anda».
    /// La compatibilidad y las etapas viven en su módulo; aquí solo se mira lo que hoy
    /// es verdad, que es lo que la Crónica cuenta por días y esto contesta de un
    /// vistazo.
    /// </remarks>
    public sealed class IslandSocialCard
    {
        private readonly VisualElement _list;

        /// <summary>Lo que se enseñó la última vez, para no derribar la tarjeta en balde.</summary>
        private string _signature;

        public VisualElement Root { get; }

        public IslandSocialCard()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Quién anda con quién"));

            _list = new VisualElement();
            Root.Add(_list);
        }

        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry))
            {
                Root.style.display = DisplayStyle.None;
                return;
            }
            Root.style.display = DisplayStyle.Flex;

            var pairs = Collect(registry);

            var firma = new StringBuilder();
            for (int i = 0; i < pairs.Count; i++)
                firma.Append(pairs[i].Key).Append('|')
                     .Append(pairs[i].Record.Romance).Append('|')
                     .Append(pairs[i].Record.Conflict).Append('|')
                     .Append(pairs[i].Record.Friendship).Append('\n');

            string clave = firma.ToString();
            if (clave == _signature) return;
            _signature = clave;

            _list.Clear();

            if (pairs.Count == 0)
            {
                _list.Add(UiTheme.Body(
                    "Todavía nadie anda con nadie. La isla acaba de empezar.", soft: true));
                return;
            }

            for (int i = 0; i < pairs.Count; i++)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                row.style.marginBottom = 4;

                var names = UiTheme.Body(pairs[i].Label);
                names.style.flexGrow = 1;
                row.Add(names);

                row.Add(UiTheme.Chip(SocialLabels.StatusOf(pairs[i].Record),
                                     SocialLabels.StatusColor(pairs[i].Record)));
                _list.Add(row);
            }
        }

        private readonly struct Pair
        {
            public readonly string Key;
            public readonly string Label;
            public readonly RelationshipRecord Record;

            public Pair(string key, string label, RelationshipRecord record)
            {
                Key = key;
                Label = label;
                Record = record;
            }
        }

        /// <summary>
        /// Las historias vivas del grafo, una fila por pareja y ninguna repetida.
        /// </summary>
        private static List<Pair> Collect(IIslanderRegistry registry)
        {
            var pairs = new List<Pair>();
            var seen = new HashSet<string>();

            var all = registry.All;
            for (int i = 0; i < all.Count; i++)
            {
                var islander = all[i];
                var records = islander.Relationships.Records;

                for (int j = 0; j < records.Count; j++)
                {
                    var record = records[j];

                    // Lo cotidiano no va: amistades y tirantez son el ruido de fondo de
                    // una aldea. Aquí va lo que cambia el día de alguien.
                    bool story = record.Romance != RomanceStage.None
                                 || record.Conflict.IsSerious()
                                 || record.Conflict == ConflictStage.Rivalry;
                    if (!story) continue;

                    // Un flechazo es de uno hacia otro y las dos direcciones son
                    // historia distinta; lo demás es estado de la pareja o de la bronca,
                    // que es de dos. Mismo criterio que usa el tablón de noticias para
                    // no contar dos veces lo mismo.
                    bool directional = record.Romance is RomanceStage.Crush
                                       or RomanceStage.Confessed;
                    string key = directional
                        ? $"{islander.Id}>{record.OtherId}"
                        : PairKey(islander.Id, record.OtherId);
                    if (!seen.Add(key)) continue;

                    string a = NameOf(registry, islander.Id);
                    string b = NameOf(registry, record.OtherId);
                    if (a == null || b == null) continue;

                    pairs.Add(new Pair(key,
                                       directional ? $"{a} → {b}" : $"{a} y {b}",
                                       record));
                }
            }

            pairs.Sort((x, y) =>
            {
                int byRank =
                    SocialLabels.RankFor(x.Record).CompareTo(SocialLabels.RankFor(y.Record));
                return byRank != 0 ? byRank : string.CompareOrdinal(x.Label, y.Label);
            });
            return pairs;
        }

        private static string PairKey(string a, string b) =>
            string.CompareOrdinal(a, b) <= 0 ? $"{a}|{b}" : $"{b}|{a}";

        private static string NameOf(IIslanderRegistry registry, string islanderId)
        {
            if (islanderId == SocialIds.Player) return SocialLabels.PlayerName();
            return registry.TryGet(islanderId, out var islander)
                ? islander.Identity.ShortName
                : null;
        }
    }
}
