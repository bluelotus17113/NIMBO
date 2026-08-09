using System;
using Nimbo.Core.Events;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// La primera pantalla. Enseña la partida que hay guardada y deja seguirla,
    /// empezar otra o irse.
    /// </summary>
    /// <remarks>
    /// No enciende nada: publica lo que el jugador ha pedido y ya. Es lo que
    /// permite que el menú viva en <c>Nimbo.UI</c> sin conocer a <c>Nimbo.Game</c>,
    /// que es quien sabe montar una partida.
    ///
    /// La partida guardada se enseña con sus datos —qué día va, cuánta gente,
    /// cuánto dinero— en vez de un «Continuar» pelado. Volver después de una
    /// semana y que la pantalla te recuerde a quién dejaste allí es medio motivo
    /// para volver a entrar.
    /// </remarks>
    public sealed class TitleScreen
    {
        public VisualElement Root { get; }

        private readonly Action _onOptions;
        private VisualElement _buttons;
        private VisualElement _confirm;

        public TitleScreen(Action onOptions)
        {
            _onOptions = onOptions;

            Root = new VisualElement();
            Root.style.alignItems = Align.Center;

            Root.Add(BuildTitle());

            var save = SaveSummary.Read();
            Root.Add(BuildButtons(save));

            _confirm = BuildConfirm();
            _confirm.style.display = DisplayStyle.None;
            Root.Add(_confirm);
        }

        private static VisualElement BuildTitle()
        {
            var block = new VisualElement();
            block.style.alignItems = Align.Center;
            block.style.marginBottom = 26;

            var title = new Label("Isla Nimbo");
            title.style.color = UiTheme.Ink;
            title.style.fontSize = 58;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            block.Add(title);

            // Una raya de color en vez de un logotipo. No hay ilustración y no la
            // va a haber: el juego se dibuja entero por código, y una barra de tres
            // pasteles dice «esto es cozy» sin mentir sobre lo que hay dentro.
            var stripe = new VisualElement();
            stripe.style.flexDirection = FlexDirection.Row;
            stripe.style.marginTop = 2;
            stripe.style.marginBottom = 10;
            foreach (var colour in new[] { UiTheme.Peach, UiTheme.Mint, UiTheme.Lavender })
            {
                var chunk = new VisualElement();
                chunk.style.width = 44;
                chunk.style.height = 7;
                chunk.style.marginLeft = chunk.style.marginRight = 3;
                chunk.style.backgroundColor = colour;
                UiTheme.SetRadius(chunk, UiTheme.RadiusPill);
                stripe.Add(chunk);
            }
            block.Add(stripe);

            var subtitle = new Label("Un sitio tranquilo que sigue vivo sin ti.");
            subtitle.style.color = UiTheme.InkSoft;
            subtitle.style.fontSize = 16;
            block.Add(subtitle);

            return block;
        }

        private VisualElement BuildButtons(SaveSummary save)
        {
            _buttons = new VisualElement();
            _buttons.style.width = 380;

            if (save.Exists)
            {
                var card = UiTheme.Card("partida");
                card.style.marginBottom = 12;

                var head = new VisualElement();
                head.style.flexDirection = FlexDirection.Row;
                head.style.justifyContent = Justify.SpaceBetween;
                head.style.alignItems = Align.Center;
                head.style.marginBottom = 6;

                var name = UiTheme.Body("Tu isla");
                name.style.unityFontStyleAndWeight = FontStyle.Bold;
                head.Add(name);

                if (!string.IsNullOrEmpty(save.LastPlayed))
                {
                    var when = UiTheme.Pill(save.LastPlayed, UiTheme.SkySoft);
                    when.style.marginRight = 0;
                    head.Add(when);
                }
                card.Add(head);

                card.Add(UiTheme.Body(save.Line, soft: true));
                _buttons.Add(card);

                _buttons.Add(Wide(UiTheme.Action("Continuar",
                    () => EventBus.Publish(new ContinueRequested()))));

                _buttons.Add(Wide(UiTheme.Secondary("Empezar de nuevo", AskConfirm)));
            }
            else
            {
                var card = UiTheme.Card("bienvenida");
                card.style.marginBottom = 12;
                card.Add(UiTheme.Body(
                    "Todavía no hay ninguna isla. La primera vez tarda un momento en " +
                    "levantarse: se sortean los vecinos, sus manías y a quién le cae " +
                    "bien quién.", soft: true));
                _buttons.Add(card);

                _buttons.Add(Wide(UiTheme.Action("Empezar",
                    () => EventBus.Publish(new NewGameRequested()))));
            }

            _buttons.Add(Wide(UiTheme.Secondary("Ajustes", () => _onOptions?.Invoke())));
            _buttons.Add(Wide(UiTheme.Secondary("Salir",
                () => EventBus.Publish(new QuitRequested()))));

            return _buttons;
        }

        /// <summary>
        /// Empezar de nuevo borra una isla con gente dentro. Se pregunta, y se
        /// pregunta enseñando lo que se va a perder, no con un «¿seguro?».
        /// </summary>
        private VisualElement BuildConfirm()
        {
            var card = UiTheme.Card("confirmar");
            card.style.width = 380;
            card.style.backgroundColor = UiTheme.Rose;

            card.Add(UiTheme.Title("¿Seguro?"));
            card.Add(UiTheme.Body(
                "Empezar otra isla deja atrás la que tienes. Se guarda una copia en " +
                "«partida.anterior.json» por si acaso, pero desde el juego ya no se " +
                "puede volver a ella."));

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 14;

            var cancel = UiTheme.Secondary("Mejor no", CancelConfirm);
            cancel.style.flexGrow = 1;
            cancel.style.marginRight = 8;
            row.Add(cancel);

            var go = UiTheme.Action("Empezar otra",
                () => EventBus.Publish(new NewGameRequested()));
            go.style.flexGrow = 1;
            row.Add(go);

            card.Add(row);
            return card;
        }

        private void AskConfirm()
        {
            _buttons.style.display = DisplayStyle.None;
            _confirm.style.display = DisplayStyle.Flex;
        }

        private void CancelConfirm()
        {
            _confirm.style.display = DisplayStyle.None;
            _buttons.style.display = DisplayStyle.Flex;
        }

        private static Button Wide(Button button)
        {
            button.style.width = Length.Percent(100);
            button.style.marginBottom = 8;
            button.style.paddingTop = button.style.paddingBottom = 11;
            return button;
        }
    }
}
