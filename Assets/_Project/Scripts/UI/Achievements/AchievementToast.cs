using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Achievements
{
    /// <summary>
    /// El cartelito que aparece cuando consigues algo.
    /// </summary>
    /// <remarks>
    /// Va en cola y de uno en uno. Al abrir la partida después de una semana fuera
    /// pueden caer cinco logros en el mismo segundo, y cinco carteles a la vez no se
    /// leen: se ven como un parpadeo y el jugador no se entera de ninguno.
    ///
    /// Entra deslizándose y se va desvaneciéndose, con el tiempo sin escalar: si el
    /// juego está en pausa el cartel tiene que terminar de irse igual, no quedarse
    /// clavado en mitad de la pantalla.
    /// </remarks>
    public sealed class AchievementToast
    {
        private const float SlideSeconds = 0.35f;
        private const float HoldSeconds = 3.2f;
        private const float FadeSeconds = 0.6f;
        private const float Total = SlideSeconds + HoldSeconds + FadeSeconds;

        public VisualElement Root { get; }

        /// <summary>
        /// Lo que queda por enseñar, ya resuelto en palabras.
        /// </summary>
        /// <remarks>
        /// Guardaba identificadores de logro y buscaba la ficha al sacarlos de la cola.
        /// Ahora guarda el texto ya hecho, porque el cartel lo comparten los logros y
        /// las cinco vías del protagonista, y una vía no tiene ficha que buscar.
        /// </remarks>
        private readonly Queue<(string Title, string Detail)> _pending = new();
        private readonly Label _title;
        private readonly Label _reward;

        private IAchievementService _service;
        private float _elapsed;
        private bool _showing;

        public AchievementToast()
        {
            Root = UiTheme.Card("logro_conseguido");
            Root.style.display = DisplayStyle.None;
            Root.style.position = Position.Absolute;
            Root.style.right = 16;
            Root.style.top = 84;
            Root.style.width = 300;
            Root.style.backgroundColor = UiTheme.Butter;

            // Decorativo: no puede tragarse los clics de lo que tape.
            Root.pickingMode = PickingMode.Ignore;

            var tag = UiTheme.Body("conseguido", soft: true);
            tag.style.fontSize = 12;
            Root.Add(tag);

            _title = UiTheme.Body("");
            _title.style.unityFontStyleAndWeight = FontStyle.Bold;
            _title.style.fontSize = 16;
            Root.Add(_title);

            _reward = UiTheme.Body("", soft: true);
            Root.Add(_reward);
        }

        public void Subscribe()
        {
            EventBus.Subscribe<AchievementUnlocked>(OnUnlocked);
            EventBus.Subscribe<SkillLeveledUp>(OnSkillLeveledUp);
            EventBus.Subscribe<UnlockGained>(OnUnlockGained);
        }

        public void Unsubscribe()
        {
            EventBus.Unsubscribe<AchievementUnlocked>(OnUnlocked);
            EventBus.Unsubscribe<SkillLeveledUp>(OnSkillLeveledUp);
            EventBus.Unsubscribe<UnlockGained>(OnUnlockGained);
        }

        /// <summary>Un cartel a mano. Lo usan las vías y cualquiera que tenga algo que decir.</summary>
        public void Push(string title, string detail) => _pending.Enqueue((title, detail));

        private void OnUnlocked(AchievementUnlocked evt)
        {
            if (_service == null && !ServiceRegistry.TryGet(out _service)) return;
            if (!_service.TryGetDefinition(evt.AchievementId, out var definition)) return;

            _pending.Enqueue((definition.DisplayName,
                              definition.Reward > 0
                                  ? $"{definition.Reward} nimbos"
                                  : definition.Description));
        }

        private void OnSkillLeveledUp(SkillLeveledUp evt) =>
            Push($"{Gates.NameOf(evt.Skill)} {evt.NewLevel}", "Se te da mejor que ayer.");

        /// <summary>
        /// Lo que se acaba de ganar el derecho a hacer, aparte del número.
        /// </summary>
        /// <remarks>
        /// Dos carteles seguidos cuando coinciden, y está bien que sean dos: subir de
        /// nivel pasa a menudo y se lee de un vistazo; desbloquear algo pasa poco y hay
        /// que pararse a leerlo. Metidos en el mismo cartel, lo segundo se perdería
        /// detrás de lo primero.
        /// </remarks>
        private void OnUnlockGained(UnlockGained evt) =>
            Push("Ya puedes", Player.SkillsPanel.Describe(evt.Unlock));

        /// <summary>Lo llama la interfaz cada fotograma, con el tiempo sin escalar.</summary>
        public void Tick(float unscaledDelta)
        {
            if (!_showing)
            {
                if (_pending.Count == 0) return;
                Begin(_pending.Dequeue());
                return;
            }

            _elapsed += unscaledDelta;

            if (_elapsed < SlideSeconds)
            {
                // Salida suave: empieza rápido y frena. Con una interpolación lineal
                // el cartel parece que lo empujan; así parece que se posa.
                float t = _elapsed / SlideSeconds;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Root.style.translate = new Translate(Length.Percent((1f - eased) * 120f), 0);
                Root.style.opacity = eased;
                return;
            }

            if (_elapsed < SlideSeconds + HoldSeconds)
            {
                Root.style.translate = new Translate(0, 0);
                Root.style.opacity = 1f;
                return;
            }

            if (_elapsed < Total)
            {
                float t = (_elapsed - SlideSeconds - HoldSeconds) / FadeSeconds;
                Root.style.opacity = 1f - t;
                return;
            }

            _showing = false;
            Root.style.display = DisplayStyle.None;
        }

        private void Begin((string Title, string Detail) card)
        {
            _title.text = card.Title;
            _reward.text = card.Detail;

            _elapsed = 0f;
            _showing = true;
            Root.style.display = DisplayStyle.Flex;
            Root.style.opacity = 0f;
            Root.style.translate = new Translate(Length.Percent(120f), 0);
        }
    }
}
