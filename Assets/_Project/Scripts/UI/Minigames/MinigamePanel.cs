using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Minigames
{
    /// <summary>
    /// La pantalla donde se juegan los tres minijuegos.
    /// </summary>
    /// <remarks>
    /// Una sola para los tres, y no por ahorrar: los tres son lo mismo por dentro —un
    /// estado, unas cuantas acciones y un final— y tres pantallas distintas para eso
    /// serían tres sitios donde arreglar el mismo fallo. Lo que cambia entre ellos son
    /// las palabras de los botones, y eso lo pone el servicio.
    ///
    /// La única diferencia de verdad es el ritmo, que va con reloj: lleva una aguja que
    /// corre hacia la marca del centro y hay que pulsar al pasar. Los otros dos esperan
    /// a que el jugador se decida, que es lo que les pega.
    ///
    /// **Se cobra solo al terminar.** En cuanto el juego dice que se acabó se llama a
    /// <c>Finish</c> y se enseña lo que ha caído; el jugador no puede cerrar la ventana
    /// y quedarse sin el pez que acababa de sacar.
    /// </remarks>
    public sealed class MinigamePanel
    {
        public VisualElement Root { get; }
        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        private readonly Label _title;
        private readonly Label _headline;
        private readonly Label _detail;
        private readonly VisualElement _progressFill;
        private readonly VisualElement _beat;
        private readonly VisualElement _needle;
        private readonly VisualElement _buttons;

        private MinigameResult _result;
        private bool _finished;

        /// <summary>Lo que abarca la aguja a cada lado de la marca, en milisegundos.</summary>
        private const float BeatWindowMs = 700f;

        public MinigamePanel()
        {
            Root = UiTheme.Card("minijuego");
            Root.style.display = DisplayStyle.None;
            Root.style.width = 480;

            var head = new VisualElement();
            head.style.flexDirection = FlexDirection.Row;
            head.style.justifyContent = Justify.SpaceBetween;
            head.style.alignItems = Align.Center;
            _title = UiTheme.Title("—");
            head.Add(_title);
            head.Add(UiTheme.Secondary("Dejarlo", Leave));
            Root.Add(head);

            // Estado del juego, no título: debajo de la cabecera ya hay un Title y dos
            // seguidos (20 px negrita los dos) se leían como un fallo de maquetación.
            // Negrita de cuerpo para que mande la jerarquía y no el grito.
            _headline = UiTheme.Body("—");
            _headline.style.unityFontStyleAndWeight = FontStyle.Bold;
            _headline.style.marginTop = 6;
            _headline.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_headline);

            _detail = UiTheme.Body("", soft: true);
            _detail.style.marginBottom = 10;
            _detail.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_detail);

            Root.Add(BuildProgress(out _progressFill));
            Root.Add(BuildBeat(out _beat, out _needle));

            _buttons = new VisualElement();
            _buttons.style.flexDirection = FlexDirection.Row;
            _buttons.style.flexWrap = Wrap.Wrap;
            _buttons.style.marginTop = 12;
            Root.Add(_buttons);
        }

        // ── abrir y cerrar ───────────────────────────────────────────────────

        /// <summary>Arranca uno. Si no se puede, no abre nada.</summary>
        public bool Show(MinigameKind kind, int difficulty, string context = null)
        {
            if (!ServiceRegistry.TryGet<IMinigameService>(out var service)) return false;
            if (!service.Start(kind, difficulty, context)) return false;

            _finished = false;
            _result = default;
            _title.text = TitleOf(kind);
            Root.style.display = DisplayStyle.Flex;
            Rebuild();
            return true;
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>Lo deja a medias. Sin premio, pero también sin haber gastado nada.</summary>
        private void Leave()
        {
            if (ServiceRegistry.TryGet<IMinigameService>(out var service) && !_finished)
                service.Abandon();

            Hide();
        }

        /// <summary>El reloj del ritmo. Lo llama la interfaz en cada fotograma.</summary>
        public void Tick(float deltaSeconds)
        {
            if (!IsShowing || _finished) return;
            if (!ServiceRegistry.TryGet<IMinigameService>(out var service)) return;
            if (service.Running == null) return;

            service.Tick(deltaSeconds);

            if (service.Running == MinigameKind.Rhythm) MoveNeedle(service);

            // Una nota que se pasa de largo se falla sola, así que el final puede llegar
            // sin que el jugador haya tocado nada: hay que mirarlo aquí también.
            if (service.IsOver) Rebuild();
        }

        // ── lo que se ve ─────────────────────────────────────────────────────

        private void Rebuild()
        {
            _buttons.Clear();

            if (!ServiceRegistry.TryGet<IMinigameService>(out var service))
            {
                _headline.text = "Esto no está disponible.";
                _detail.text = "";
                return;
            }

            if (service.IsOver && !_finished)
            {
                // Se cierra y se cobra en cuanto termina. Dejarlo para el botón de salir
                // significaría que cerrar la ventana con la equis te deja sin lo que
                // acabas de pescar, y eso no hay forma de explicarlo.
                _result = service.Finish();
                _finished = true;
            }

            if (_finished) { ShowEnding(service); return; }

            _headline.text = service.Headline;
            _detail.text = service.Detail;
            SetProgress(service.Progress);
            _beat.style.display = service.Running == MinigameKind.Rhythm
                ? DisplayStyle.Flex : DisplayStyle.None;

            var actions = service.Actions;
            for (int i = 0; i < actions.Count; i++)
            {
                int input = i;
                var button = UiTheme.Action(actions[i], () => Press(input));
                button.style.marginRight = 8;
                button.style.marginBottom = 4;
                _buttons.Add(button);
            }
        }

        private void Press(int input)
        {
            if (!ServiceRegistry.TryGet<IMinigameService>(out var service)) return;

            service.Step(input);
            Rebuild();
        }

        private void ShowEnding(IMinigameService service)
        {
            _beat.style.display = DisplayStyle.None;
            SetProgress(1f);

            _headline.text = _result.IsVictory ? "¡Hecho!" : "Otra vez será.";
            _detail.text = EndingDetail(service);

            var close = UiTheme.Action("Cerrar", Hide);
            close.style.marginRight = 8;
            _buttons.Add(close);
        }

        private string EndingDetail(IMinigameService service)
        {
            string prize = string.IsNullOrEmpty(service.LastPrize)
                ? ""
                : $"Te llevas {ItemNames.Of(service.LastPrize)}. ";

            string coins = _result.Coins > 0 ? $"Y {_result.Coins} nimbos. " : "";

            return $"{prize}{coins}{_result.Points} puntos, " +
                   $"{_result.Successes} bien y {_result.Failures} mal.";
        }

        // ── la aguja del ritmo ───────────────────────────────────────────────

        private void MoveNeedle(IMinigameService service)
        {
            float offset = Mathf.Clamp(service.MillisecondsToBeat, -BeatWindowMs, BeatWindowMs);

            // La marca está en el centro y la aguja llega por la derecha: cuando faltan
            // milisegundos va a la derecha, cuando ya se ha pasado sigue hacia la
            // izquierda. Así se ve venir el momento en vez de aparecer encima.
            float t = 0.5f + offset / (BeatWindowMs * 2f);
            _needle.style.left = Length.Percent(Mathf.Clamp01(t) * 100f);
            _needle.style.backgroundColor = Mathf.Abs(offset) < 60f ? UiTheme.Mint : UiTheme.Ink;
        }

        // ── piezas ───────────────────────────────────────────────────────────

        private static VisualElement BuildProgress(out VisualElement fill)
        {
            var track = new VisualElement();
            track.style.height = 10;
            track.style.backgroundColor = UiTheme.CreamDeep;
            UiTheme.SetRadius(track, UiTheme.RadiusPill);

            fill = new VisualElement();
            fill.style.height = 10;
            fill.style.width = Length.Percent(0f);
            fill.style.backgroundColor = UiTheme.Mint;
            UiTheme.SetRadius(fill, UiTheme.RadiusPill);
            track.Add(fill);

            return track;
        }

        private void SetProgress(float value) =>
            _progressFill.style.width = Length.Percent(Mathf.Clamp01(value) * 100f);

        private static VisualElement BuildBeat(out VisualElement track, out VisualElement needle)
        {
            track = new VisualElement();
            track.style.height = 34;
            track.style.marginTop = 12;
            track.style.backgroundColor = UiTheme.CreamDeep;
            track.style.display = DisplayStyle.None;
            UiTheme.SetRadius(track, UiTheme.Radius);

            var mark = new VisualElement();
            mark.style.position = Position.Absolute;
            mark.style.left = Length.Percent(50f);
            mark.style.top = 0;
            mark.style.width = 4;
            mark.style.height = 34;
            mark.style.backgroundColor = UiTheme.PeachDeep;
            track.Add(mark);

            needle = new VisualElement();
            needle.style.position = Position.Absolute;
            needle.style.top = 5;
            needle.style.width = 10;
            needle.style.height = 24;
            needle.style.backgroundColor = UiTheme.Ink;
            UiTheme.SetRadius(needle, UiTheme.RadiusPill);
            track.Add(needle);

            return track;
        }

        private static string TitleOf(MinigameKind kind) => kind switch
        {
            MinigameKind.Cooking => "En los fogones",
            MinigameKind.Fishing => "Pescando en las nubes",
            _ => "En el escenario",
        };
    }
}
