using System;
using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Requests;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Requests
{
    /// <summary>
    /// Una petición pintada: quién la hace, qué pide y con qué se le puede pagar.
    /// </summary>
    /// <remarks>
    /// La misma fila la usan la ficha del vecino y el tablón de la plaza. Estaba escrita
    /// dentro de la ficha y se copió al hacer el tablón; dos copias de lo que puede
    /// atenderse y de lo que cuesta se separan al primer cambio, y entonces el jugador
    /// ve un botón en una pantalla y un aviso de que le falta material en la otra.
    ///
    /// **Nunca resuelve por su cuenta lo que el jugador tiene que elegir.** Cuando vale
    /// cualquier cosa de una familia —comida, ropa, un mueble— salen tantos botones como
    /// cosas lleva, y elige él. Coger «la primera que valga» acabaría gastando lo que
    /// guardaba para otro, y eso no se deshace.
    /// </remarks>
    public static class RequestRow
    {
        /// <summary>Cuántas opciones se enseñan antes de cortar. Más no se leen.</summary>
        private const int MaxOptions = 6;

        /// <param name="who">El nombre del vecino, para el tablón. Vacío en su propia ficha.</param>
        /// <param name="onChanged">Se llama cuando la petición deja de estar abierta.</param>
        public static VisualElement Build(IslanderRequest request, IRequestService service,
                                          Action onChanged, string who = null)
        {
            var row = new VisualElement();
            row.style.marginBottom = 12;

            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 4;

            if (!string.IsNullOrEmpty(who))
            {
                var name = UiTheme.Body(who);
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                name.style.marginRight = 8;
                header.Add(name);
            }

            header.Add(UiTheme.Chip(KindName(request.Kind), PriorityColor(request.Priority)));
            header.Add(UiTheme.Body($"+{request.CoinReward} nimbos", soft: true));
            row.Add(header);

            var line = UiTheme.Body($"«{request.Line}»");
            line.style.whiteSpace = WhiteSpace.Normal;
            line.style.marginBottom = 6;
            row.Add(line);

            var demand = service.DemandOf(request.RequestId);
            var options = service.OptionsFor(request.RequestId);

            if (demand.WantsAnItem)
            {
                var wants = UiTheme.Body(WantsText(demand), soft: true);
                wants.style.marginBottom = 6;
                row.Add(wants);
            }

            row.Add(Buttons(request, service, onChanged, demand, options));
            return row;
        }

        private static VisualElement Buttons(IslanderRequest request, IRequestService service,
                                             Action onChanged, in RequestDemand demand,
                                             IReadOnlyList<string> options)
        {
            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.flexWrap = Wrap.Wrap;

            string id = request.RequestId;

            if (!demand.WantsAnItem)
            {
                buttons.Add(Accept("Ayudarle", () => service.Resolve(id), onChanged));
            }
            else if (options.Count == 0)
            {
                // Se dice qué falta en vez de esconder el botón: un botón que no está no
                // manda a nadie a la isla, y «te falta madera» sí.
                var chip = UiTheme.Chip(ShortfallText(demand), UiTheme.Rose);
                chip.style.marginRight = 8;
                buttons.Add(chip);
            }
            else if (!string.IsNullOrEmpty(demand.CatalogId))
            {
                // Copiado a una local antes del lambda: `demand` llega por `in` y un
                // parámetro por referencia no se puede capturar.
                string wanted = demand.CatalogId;
                string label = demand.Quantity > 1
                    ? $"Darle {demand.Quantity} de {ItemNames.Of(wanted)}"
                    : $"Darle {ItemNames.Of(wanted)}";
                buttons.Add(Accept(label, () => service.Resolve(id, wanted), onChanged));
            }
            else
            {
                int shown = Mathf.Min(options.Count, MaxOptions);
                for (int i = 0; i < shown; i++)
                {
                    string payload = options[i];
                    buttons.Add(Accept(ItemNames.Of(payload),
                                       () => service.Resolve(id, payload), onChanged));
                }
            }

            var refuse = UiTheme.Action("Ahora no", () => { service.Refuse(id); onChanged?.Invoke(); });
            refuse.style.backgroundColor = UiTheme.InkSoft;
            refuse.style.marginBottom = 4;
            buttons.Add(refuse);

            return buttons;
        }

        /// <summary>
        /// Un botón que solo avisa si de verdad ha pasado algo.
        /// </summary>
        /// <remarks>
        /// <c>Resolve</c> puede decir que no —el material se gastó entre que se pintó la
        /// pantalla y se pulsó— y en ese caso no hay que refrescar como si se hubiera
        /// atendido. Aun así se refresca, porque lo que hay que enseñar entonces es lo
        /// que falta ahora.
        /// </remarks>
        private static Button Accept(string label, Func<bool> action, Action onChanged)
        {
            var button = UiTheme.Action(label, () => { action(); onChanged?.Invoke(); });
            button.style.marginRight = 8;
            button.style.marginBottom = 4;
            return button;
        }

        private static string WantsText(in RequestDemand demand)
        {
            if (!string.IsNullOrEmpty(demand.CatalogId))
                return $"Quiere {demand.Quantity} de {ItemNames.Of(demand.CatalogId)}.";

            return demand.Category switch
            {
                ItemCategory.Food => "Le vale cualquier cosa de comer.",
                ItemCategory.Clothing => "Le vale cualquier prenda.",
                _ => "Le vale cualquier mueble.",
            };
        }

        private static string ShortfallText(in RequestDemand demand) =>
            string.IsNullOrEmpty(demand.CatalogId)
                ? demand.Category switch
                {
                    ItemCategory.Food => "no llevas nada de comer",
                    ItemCategory.Clothing => "no tienes ropa que darle",
                    _ => "no tienes muebles que darle",
                }
                : $"te falta {ItemNames.Of(demand.CatalogId)}";

        private static string KindName(RequestKind kind) => kind switch
        {
            RequestKind.Food => "comida",
            RequestKind.Object => "un objeto",
            RequestKind.Clothes => "ropa",
            RequestKind.Advice => "consejo",
            RequestKind.Favor => "un favor",
            RequestKind.Complaint => "una queja",
            RequestKind.SocialIntro => "conocer a alguien",
            RequestKind.Activity => "hacer algo",
            RequestKind.IslandBuilding => "mejorar la isla",
            RequestKind.Confession => "declararse",
            RequestKind.Reconcile => "hacer las paces",
            RequestKind.Material => "un encargo",
            _ => "algo",
        };

        private static Color PriorityColor(RequestPriority priority) => priority switch
        {
            RequestPriority.Critical => UiTheme.Critical,
            RequestPriority.High => UiTheme.Low,
            RequestPriority.Normal => UiTheme.Sky,
            _ => UiTheme.InkFaint,
        };
    }
}
