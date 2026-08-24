using System.Collections.Generic;
using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Requests;
using UnityEngine.UIElements;

namespace Nimbo.UI.Requests
{
    /// <summary>
    /// El tablón de encargos de la plaza: todo lo que la aldea está pidiendo.
    /// </summary>
    /// <remarks>
    /// Existe porque las peticiones solo se veían **entrando en la ficha de cada
    /// vecino**, y con doce vecinos eso son doce pantallas para averiguar si hay algo
    /// que hacer. Nadie hace ese recorrido dos veces: el sistema estaba encendido y la
    /// forma de leerlo lo dejaba en nada.
    ///
    /// Lo urgente arriba, y dentro de cada urgencia lo que está a punto de caducar. Es
    /// el orden en que se decide de verdad: primero qué no puede esperar, y entre lo que
    /// no puede esperar, lo que menos.
    ///
    /// **No es una lista de tareas.** No hay contador de completadas ni recompensa por
    /// vaciarlo, y por eso también se enseña lo que no puedes atender todavía —con el
    /// aviso de qué te falta— en vez de esconderlo: es un sitio donde mirar qué está
    /// pasando, no una cola que tacharse.
    /// </remarks>
    public sealed class RequestBoardPanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly ScrollView _list;
        private readonly Label _headline;

        /// <summary>
        /// Lo que mandó la última vez que se levantó el tablón. El ciclo lento de
        /// UiRoot llama a <see cref="Refresh"/> cada 0,4 s con el panel abierto, y
        /// derribar los botones bajo el cursor se comía los clics en la ficha: solo
        /// se reconstruye cuando la firma cambia.
        /// </summary>
        private string _signature;

        public RequestBoardPanel()
        {
            Root = UiTheme.Card("tablon-encargos");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 540;
            Root.style.maxHeight = Length.Percent(92);

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            head.Add(UiTheme.Title("Tablón de encargos"));
            head.Add(UiTheme.Secondary("Cerrar", Hide));
            Root.Add(head);

            _headline = UiTheme.Body("", soft: true);
            _headline.style.marginBottom = 10;
            Root.Add(_headline);

            _list = new ScrollView();
            _list.style.flexGrow = 1;
            Root.Add(_list);
        }

        public void Show()
        {
            Root.style.display = DisplayStyle.Flex;
            Rebuild();

            // Lo pintado queda firmado: si nada cambia detrás, el primer Refresh no
            // reconstruye por sorpresa lo que acaba de levantarse.
            _signature = Firma();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>
        /// Refresco barato: firma lo que se pinta y solo reconstruye si cambió.
        /// </summary>
        /// <remarks>
        /// Un encargo caduca, se atiende o deja de ser pagable mientras el tablón
        /// está delante —y vender de la mochila cambia los botones sin que el tablón
        /// se entere por otro lado—. El plazo entra en la firma siendo **el texto
        /// exacto que se pinta** (<see cref="UiTheme.Plazo"/>): si la firma usara un
        /// cálculo paralelo, bastaría que alguien ajustara uno de los dos para que
        /// el tablón enseñase una cuenta atrás vieja sin enterarse.
        /// </remarks>
        public void Refresh()
        {
            if (!ServiceRegistry.TryGet<IRequestService>(out _))
            {
                _signature = null;
                return;
            }

            string candidata = Firma();
            if (candidata == _signature) return;
            _signature = candidata;
            Rebuild();
        }

        /// <summary>Lo que dicta el estado del servicio, en una cadena.</summary>
        private string Firma()
        {
            var firma = new StringBuilder();
            if (!ServiceRegistry.TryGet<IRequestService>(out var service))
                return firma.ToString();

            ServiceRegistry.TryGet<GameClock>(out var clock);
            ServiceRegistry.TryGet<IIslanderRegistry>(out var registry);

            var open = service.Open;
            for (int i = 0; i < open.Count; i++)
            {
                var request = open[i];
                var demand = service.DemandOf(request.RequestId);

                firma.Append(request.RequestId)
                     .Append('|').Append((int)request.Priority)
                     .Append('|').Append(request.CoinReward)
                     .Append('|').Append(demand.CatalogId)
                     .Append('|').Append(demand.Quantity)
                     .Append('|').Append(demand.WantsAnItem ? 'p' : '-');

                // Lo pagable ahora mismo: decide si la fila enseña botón o aviso.
                var options = service.OptionsFor(request.RequestId);
                for (int j = 0; j < options.Count; j++)
                    firma.Append('|').Append(options[j]);

                long left = request.ExpiresMinute - (clock?.ElapsedMinutes ?? 0);
                firma.Append('|').Append(clock == null ? "-" : UiTheme.Plazo(left));

                if (registry != null && registry.TryGet(request.IslanderId, out var islander))
                    firma.Append('|').Append(islander.Identity.ShortName);
            }

            return firma.ToString();
        }

        public void Rebuild()
        {
            _list.Clear();

            if (!ServiceRegistry.TryGet<IRequestService>(out var service))
            {
                _headline.text = "";
                _list.Add(UiTheme.Body("El tablón no está disponible.", soft: true));
                return;
            }

            var open = Sorted(service.Open);
            if (open.Count == 0)
            {
                _headline.text = "";
                _list.Add(UiTheme.Body(
                    "El tablón está vacío. Hoy nadie necesita nada, que también está bien.",
                    soft: true));
                return;
            }

            _headline.text = open.Count == 1
                ? "Una cosa pendiente"
                : $"{open.Count} cosas pendientes, de lo más urgente a lo que puede esperar";

            ServiceRegistry.TryGet<IIslanderRegistry>(out var registry);

            for (int i = 0; i < open.Count; i++)
            {
                var request = open[i];
                _list.Add(Card(RequestRow.Build(request, service, Rebuild,
                                                NameOf(registry, request.IslanderId)),
                               Deadline(request)));
            }
        }

        /// <summary>Lo urgente primero y, a igual urgencia, lo que antes caduca.</summary>
        private static List<IslanderRequest> Sorted(IReadOnlyList<IslanderRequest> open)
        {
            var list = new List<IslanderRequest>(open);
            list.Sort((a, b) =>
            {
                int byPriority = b.Priority.CompareTo(a.Priority);
                return byPriority != 0 ? byPriority : a.ExpiresMinute.CompareTo(b.ExpiresMinute);
            });
            return list;
        }

        private static VisualElement Card(VisualElement row, string deadline)
        {
            var card = new VisualElement();
            card.style.paddingLeft = card.style.paddingRight = 12;
            card.style.paddingTop = card.style.paddingBottom = 10;
            card.style.marginBottom = 6;
            card.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(card, UiTheme.Radius);

            card.Add(row);

            if (!string.IsNullOrEmpty(deadline))
            {
                var label = UiTheme.Body(deadline, soft: true);
                label.style.marginTop = 2;
                card.Add(label);
            }

            return card;
        }

        /// <summary>
        /// Cuánto queda, en horas de juego.
        /// </summary>
        /// <remarks>
        /// En horas y no en minutos porque el reloj de la isla corre rápido —un día son
        /// veinticuatro minutos de verdad— y ver los minutos bajar convierte un recado en
        /// una cuenta atrás. Las palabras las pone <see cref="UiTheme.Plazo"/>, que
        /// compara minutos antes de redondear: con 61-89 minutos decía «menos de una
        /// hora» y quien se fiaba perdía el encargo. Sin reloj registrado no se enseña
        /// nada: mejor callarse que escribir un plazo inventado.
        /// </remarks>
        private static string Deadline(in IslanderRequest request)
        {
            if (!ServiceRegistry.TryGet<GameClock>(out var clock)) return "";
            return UiTheme.Plazo(request.ExpiresMinute - clock.ElapsedMinutes);
        }

        private static string NameOf(IIslanderRegistry registry, string islanderId)
        {
            if (registry != null && registry.TryGet(islanderId, out var islander))
                return islander.Identity.ShortName;
            return "Alguien";
        }
    }
}
