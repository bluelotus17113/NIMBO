using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// El bloque de casa de la ficha: en qué vive y qué hace falta para ampliarla.
    /// </summary>
    /// <remarks>
    /// Enseña la obra antes de que la tengas —no se esconde el botón, se dice qué
    /// falta— porque el material es lo que te manda a la isla. Un botón gris que no
    /// explica nada no le da a nadie una razón para coger el hacha; «te faltan 12 de
    /// madera», sí.
    /// </remarks>
    public sealed class HomeSection
    {
        private readonly Label _current;
        private readonly Label _cost;
        private readonly VisualElement _action;

        /// <summary>
        /// Lo que mandaba la última vez que se levantó la fila de obra. El refresco
        /// corre cada 0,4 s y el botón «Ampliársela» se reconstruía bajo el cursor en
        /// cada pasada; ahora solo cambia cuando cambia el veredicto, la puerta o lo
        /// que te falta de verdad.
        /// </summary>
        private string _actionSignature;

        public VisualElement Root { get; }

        public HomeSection()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Su casa"));

            _current = UiTheme.Body("—");
            Root.Add(_current);

            _cost = UiTheme.Body("", soft: true);
            _cost.style.marginBottom = 8;
            Root.Add(_cost);

            _action = new VisualElement();
            Root.Add(_action);
        }

        public void Refresh(string islanderId)
        {
            if (!ServiceRegistry.TryGet<IHomeUpgradeService>(out var upgrades))
            {
                Root.style.display = DisplayStyle.None;
                // Se invalida la firma para que la próxima pasada con servicio
                // reconstruya de verdad, igual que JobSection.Refresh. Sin esto,
                // una pasada sin servicio deja la firma vieja y la siguiente cree
                // que no ha cambiado nada.
                _actionSignature = null;
                return;
            }
            Root.style.display = DisplayStyle.Flex;

            int level = upgrades.LevelOf(islanderId);
            int size = upgrades.SizeOfLevel(level);
            var verdict = upgrades.CanUpgrade(islanderId);

            // Las etiquetas se tocan siempre: escribir texto no derriba nada.
            _current.text = level >= upgrades.MaxLevel
                ? $"{size}×{size} baldosas · no se puede ampliar más"
                : $"{size}×{size} baldosas · nivel {level} de {upgrades.MaxLevel}";

            string notice = null;
            bool withRow = false;

            if (verdict == UpgradeRejection.NoHome || verdict == UpgradeRejection.UnknownIslander)
            {
                _current.text = "Todavía no vive en ningún sitio.";
                _cost.text = "";
            }
            else if (verdict == UpgradeRejection.MaxedOut)
            {
                _cost.text = "Tiene la casa más grande de la isla.";
            }
            else
            {
                int next = upgrades.SizeOfLevel(level + 1);
                _cost.text = $"Ampliar a {next}×{next}: {upgrades.PriceOf(level)} nimbos " +
                             $"y {CostText(upgrades.MaterialsFor(level))}.";

                // Pagar la obra de la casa de otro es de las cosas que hace quien lleva
                // la aldea, y se gana (Aldea 3, §12.3). El coste se sigue enseñando:
                // saber a dónde vas es la mitad de la razón para llegar.
                if (Gates.Allows(Unlock.UpgradeHomes, out string falta)) withRow = true;
                else notice = falta;
            }

            // Lo que te falta entra en la firma: si recoges madera con la ficha abierta,
            // la fila tiene que cambiar aunque el veredicto siga siendo el mismo.
            string missing = withRow && verdict == UpgradeRejection.NotEnoughMaterials
                ? Missing(upgrades, islanderId)
                : "";

            string signature = $"{islanderId}|{verdict}|{notice}|{withRow}|{missing}";
            if (signature == _actionSignature) return;
            _actionSignature = signature;

            _action.Clear();
            if (withRow) _action.Add(BuildRow(upgrades, islanderId, verdict));
            else if (notice != null) _action.Add(UiTheme.Body(notice, soft: true));
        }

        private VisualElement BuildRow(IHomeUpgradeService upgrades, string islanderId,
                                       UpgradeRejection verdict)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            if (verdict != UpgradeRejection.Ok)
            {
                row.Add(UiTheme.Chip(ReasonLabel(verdict), UiTheme.Rose));
                if (verdict == UpgradeRejection.NotEnoughMaterials)
                    row.Add(UiTheme.Body(Missing(upgrades, islanderId), soft: true));
                return row;
            }

            var spacer = new VisualElement();
            spacer.style.flexGrow = 1;
            row.Add(spacer);

            var button = UiTheme.Action("Ampliársela", () =>
            {
                upgrades.Upgrade(islanderId);
                Refresh(islanderId);
            });
            button.style.backgroundColor = UiTheme.CreamDeep;
            row.Add(button);
            return row;
        }

        /// <summary>Lo que falta, contado. Es la frase que manda al jugador a la isla.</summary>
        private static string Missing(IHomeUpgradeService upgrades, string islanderId)
        {
            if (!ServiceRegistry.TryGet<IInventoryService>(out var bag)) return "";

            int level = upgrades.LevelOf(islanderId);
            var costs = upgrades.MaterialsFor(level);

            var text = new StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                int short_ = costs[i].Quantity - bag.CountOf(costs[i].CatalogId);
                if (short_ <= 0) continue;

                if (text.Length > 0) text.Append(", ");
                text.Append(short_).Append(' ').Append(NameOf(costs[i].CatalogId));
            }

            return text.Length > 0 ? $"Te falta: {text}." : "";
        }

        private static string CostText(System.Collections.Generic.IReadOnlyList<MaterialCost> costs)
        {
            if (costs.Count == 0) return "nada de obra";

            var text = new StringBuilder();
            for (int i = 0; i < costs.Count; i++)
            {
                if (i > 0) text.Append(i == costs.Count - 1 ? " y " : ", ");
                text.Append(costs[i].Quantity).Append(' ').Append(NameOf(costs[i].CatalogId));
            }
            return text.ToString();
        }

        /// <summary>
        /// El nombre corto del material. Vive en <see cref="ItemNames"/> desde que el
        /// tablón de encargos necesitó escribir lo mismo.
        /// </summary>
        private static string NameOf(string catalogId) => ItemNames.Of(catalogId);

        private static string ReasonLabel(UpgradeRejection verdict) => verdict switch
        {
            UpgradeRejection.NotEnoughCoins => "faltan nimbos",
            UpgradeRejection.NotEnoughMaterials => "falta obra",
            _ => "no se puede",
        };
    }
}
