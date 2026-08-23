using System.Collections.Generic;
using System.Text;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// Qué le gusta a este vecino, para saber qué regalarle antes de regalárselo.
    /// </summary>
    /// <remarks>
    /// Regalar funciona desde hace tiempo —se da lo que llevas en la mano, opina según
    /// el gusto de cada uno, da experiencia y abre logros— y la interfaz no lo nombraba
    /// en ningún sitio: había hasta un desbloqueo («un regalo más al día») apuntando a
    /// un sistema invisible.
    ///
    /// Enseñar los gustos no destroza el juego de descubrirlos dándole cosas y viendo
    /// la cara que pone: son dos comidas y dos fijadas al nacer, y esta tarjeta la lee
    /// quien ya las descubrió. A quien no, al menos le dice que regalar existe y por
    /// dónde se hace.
    /// </remarks>
    public sealed class TastesSection
    {
        private readonly Label _line;
        private readonly Label _how;

        private string _islanderId;

        public VisualElement Root { get; }

        public TastesSection()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Qué le gusta"));

            _line = UiTheme.Body("—");
            _line.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_line);

            _how = UiTheme.Body("", soft: true);
            _how.style.marginTop = 4;
            _how.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_how);
        }

        public void Refresh(string islanderId)
        {
            // Los gustos son estables a propósito: es lo que permite aprenderlos. Basta
            // con construir la frase una vez por habitante y no tocarla más.
            if (islanderId == _islanderId) return;
            _islanderId = islanderId;

            if (!ServiceRegistry.TryGet<IIslanderRegistry>(out var registry) ||
                !registry.TryGet(islanderId, out var islander))
            {
                Root.style.display = DisplayStyle.None;
                return;
            }
            Root.style.display = DisplayStyle.Flex;

            var loves = Named(islander.Tastes.LovedFoods);
            var hates = Named(islander.Tastes.HatedFoods);

            // Un habitante sin gustos escritos es posible (el catálogo de comida puede
            // no estar cuando nació). El vacío también habla: dice que el sistema
            // existe y cómo se usa.
            if (loves.Count == 0 && hates.Count == 0)
            {
                _line.text = "Nadie ha apuntado todavía qué le gusta.";
                _how.text = "Regálale algo y mira la cara que pone.";
                return;
            }

            var frase = new StringBuilder();
            if (loves.Count > 0) frase.Append("Le encanta ").Append(Join(loves)).Append('.');
            if (hates.Count > 0)
            {
                if (frase.Length > 0) frase.Append(' ');
                frase.Append("No soporta ").Append(Join(hates)).Append('.');
            }
            _line.text = frase.ToString();

            _how.text = "Llevándole eso en la mano y plantándote delante, se lo puedes regalar.";
        }

        private static List<string> Named(IEnumerable<string> catalogIds)
        {
            var names = new List<string>();
            foreach (string id in catalogIds)
            {
                string name = ItemNames.Of(id);
                if (!string.IsNullOrEmpty(name)) names.Add(name);
            }
            return names;
        }

        /// <summary>«peras y manzanas», «a, b y c»: coma y «y», sin serial final.</summary>
        private static string Join(List<string> names)
        {
            var text = new StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                if (i > 0) text.Append(i == names.Count - 1 ? " y " : ", ");
                text.Append(names[i]);
            }
            return text.ToString();
        }
    }
}
