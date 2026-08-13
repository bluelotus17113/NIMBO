using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Creator
{
    /// <summary>
    /// El creador de personajes: aspecto, personalidad, voz y nombre.
    /// </summary>
    /// <remarks>
    /// Los cuatro deslizadores de personalidad enseñan debajo el tipo que sale de
    /// ellos, y ese es todo el truco del sistema: el jugador no elige de una lista de
    /// dieciséis, mueve cuatro rasgos y el tipo cae solo. Ver el nombre cambiar al
    /// mover un deslizador es lo que hace entender que la personalidad es continua.
    ///
    /// El panel no crea al habitante ni lo mete en la isla: dispara
    /// <see cref="OnFinished"/> y quien lo abrió decide qué hacer con él.
    /// </remarks>
    public sealed class CreatorPanel
    {
        private readonly Label _typeName;
        private readonly Label _typeTagline;
        private readonly TextField _name;
        private readonly VisualElement _sections;

        private readonly Dictionary<string, Slider> _floats = new();
        private readonly Dictionary<string, SliderInt> _ints = new();
        private readonly Slider[] _axes = new Slider[4];

        /// <summary>El bloque de personalidad, para poder esconderlo al protagonista.</summary>
        private VisualElement _personalityBlock;
        private Button _done;
        private bool _forProtagonist;

        private IslanderData _draft;
        private Rng _rng = Rng.FromTime();

        /// <summary>Se dispara al pulsar «Listo», con el habitante ya montado.</summary>
        public event Action<IslanderData> OnFinished;

        public VisualElement Root { get; }

        public CreatorPanel()
        {
            Root = new VisualElement { name = "creador" };
            Root.style.width = 460;
            Root.style.display = DisplayStyle.None;

            var scroll = new ScrollView(ScrollViewMode.Vertical);
            UiTheme.StyleScroll(scroll);
            scroll.style.flexGrow = 1;

            var header = UiTheme.Card();
            header.Add(UiTheme.Title("Quién llega a la isla"));

            _name = new TextField("Nombre");
            _name.style.marginBottom = 10;
            header.Add(_name);

            // La personalidad va arriba del todo, antes que la cara: es lo que decide
            // cómo se va a comportar, y la cara es lo que decide cómo se ve.
            _personalityBlock = BuildPersonality(out _typeName, out _typeTagline);
            header.Add(_personalityBlock);
            scroll.Add(header);

            _sections = new VisualElement();
            scroll.Add(_sections);
            Root.Add(scroll);

            var buttons = new VisualElement();
            buttons.style.flexDirection = FlexDirection.Row;
            buttons.style.marginTop = UiTheme.Gap;

            var random = UiTheme.Action("Al azar", Randomize);
            random.style.backgroundColor = UiTheme.CreamDeep;
            random.style.marginRight = 8;
            random.style.flexGrow = 1;
            buttons.Add(random);

            _done = UiTheme.Action("Listo", Finish);
            _done.style.flexGrow = 1;
            buttons.Add(_done);
            Root.Add(buttons);

            BuildAppearanceSections();
        }

        public bool IsShowing => Root.style.display == DisplayStyle.Flex;

        /// <summary>Abre el creador con un habitante en blanco listo para modelar.</summary>
        public void Show()
        {
            _forProtagonist = false;
            if (_personalityBlock != null) _personalityBlock.style.display = DisplayStyle.Flex;
            if (_done != null) _done.text = "Listo";

            OpenWithDraft();
        }

        /// <summary>
        /// Abre el creador para hacerte a ti, no a un vecino.
        /// </summary>
        /// <remarks>
        /// Se esconde la personalidad, y no es por ahorrar pantalla: la de un vecino
        /// decide cómo se va a comportar solo, y al protagonista lo mueves tú. Poner
        /// cuatro deslizadores que no hacen nada sería mentir sobre lo que el juego
        /// hace con ellos.
        ///
        /// También funciona antes de que exista la partida —el creador sale nada más
        /// darle a Empezar—, por eso todo lo que pide a los servicios tiene camino
        /// alternativo.
        /// </remarks>
        public void ShowForProtagonist()
        {
            _forProtagonist = true;
            if (_personalityBlock != null) _personalityBlock.style.display = DisplayStyle.None;
            if (_done != null) _done.text = "Este soy yo";

            OpenWithDraft();
        }

        private void OpenWithDraft()
        {
            _draft = ServiceRegistry.TryGet<IIslanderFactory>(out var factory)
                ? factory.CreateBlank()
                : NewBlankDraft();

            // Sin servicios el borrador en blanco sale con la cara por defecto, que es
            // la misma para todo el mundo. Se sortea para que la primera pantalla del
            // juego no sea un maniquí gris.
            if (_forProtagonist) Randomize();

            PushToControls();
            Root.style.display = DisplayStyle.Flex;
        }

        public void Hide() => Root.style.display = DisplayStyle.None;

        /// <summary>Un borrador de emergencia por si el creador se abre sin servicios.</summary>
        private static IslanderData NewBlankDraft() => new IslanderData
        {
            Identity = new IslanderIdentity { Id = Guid.NewGuid().ToString("N") },
            Appearance = AppearanceData.Default,
            Personality = new PersonalityProfile(0.25f, 0.25f, 0.25f, 0.25f),
            Voice = VoiceConfig.Default,
            Needs = NeedState.Fresh,
            Mood = MoodState.Fresh,
            Progression = ProgressionState.Fresh,
        };

        // --- personalidad -----------------------------------------------------

        private VisualElement BuildPersonality(out Label typeName, out Label tagline)
        {
            var block = new VisualElement();
            block.Add(UiTheme.Body("Cómo es", soft: true));

            _axes[0] = AxisSlider(block, "Calmado", "Enérgico");
            _axes[1] = AxisSlider(block, "Reservado", "Expresivo");
            _axes[2] = AxisSlider(block, "Independiente", "Sociable");
            _axes[3] = AxisSlider(block, "Práctico", "Soñador");

            var result = new VisualElement();
            result.style.backgroundColor = UiTheme.CreamDeep;
            result.style.paddingTop = result.style.paddingBottom = 10;
            result.style.paddingLeft = result.style.paddingRight = 14;
            result.style.marginTop = 8;
            UiTheme.SetRadius(result, UiTheme.RadiusCard);

            typeName = new Label("—");
            typeName.style.fontSize = 17;
            typeName.style.unityFontStyleAndWeight = FontStyle.Bold;
            typeName.style.color = UiTheme.Ink;
            result.Add(typeName);

            tagline = UiTheme.Body("", soft: true);
            result.Add(tagline);

            block.Add(result);
            return block;
        }

        private Slider AxisSlider(VisualElement parent, string low, string high)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 2;

            var lowLabel = UiTheme.Body(low, soft: true);
            lowLabel.style.width = 96;
            row.Add(lowLabel);

            var slider = new Slider(-1f, 1f) { value = 0.25f };
            slider.style.flexGrow = 1;
            slider.RegisterValueChangedCallback(_ => PullPersonality());
            row.Add(slider);

            var highLabel = UiTheme.Body(high, soft: true);
            highLabel.style.width = 82;
            highLabel.style.unityTextAlign = TextAnchor.MiddleRight;
            row.Add(highLabel);

            parent.Add(row);
            return slider;
        }

        /// <summary>Lee los cuatro ejes y enseña el tipo que sale de ellos.</summary>
        private void PullPersonality()
        {
            if (_draft == null) return;

            _draft.Personality = new PersonalityProfile(
                _axes[0].value, _axes[1].value, _axes[2].value, _axes[3].value);

            if (!ServiceRegistry.TryGet<IPersonalityService>(out var personalities))
            {
                _typeName.text = $"Tipo {_draft.Personality.TypeIndex}";
                return;
            }

            var behaviour = personalities.For(_draft.Personality);
            _typeName.text = behaviour.DisplayName;
            _typeTagline.text = behaviour.Tagline;
            _draft.Voice = behaviour.VoiceTemplate;
        }

        // --- aspecto ----------------------------------------------------------

        private void BuildAppearanceSections()
        {
            var head = Section("Cabeza");
            FloatSlider(head, "HeadWidth", "Ancho");
            FloatSlider(head, "HeadHeight", "Alto");
            IntSlider(head, "HeadShape", "Forma", 6);

            var eyes = Section("Ojos");
            IntSlider(eyes, "EyeStyle", "Estilo", 12);
            FloatSlider(eyes, "EyeSize", "Tamaño");
            FloatSlider(eyes, "EyeSpacing", "Separación");
            FloatSlider(eyes, "EyeHeight", "Altura");
            SignedSlider(eyes, "EyeTilt", "Inclinación");

            var brows = Section("Cejas");
            IntSlider(brows, "BrowStyle", "Estilo", 8);
            FloatSlider(brows, "BrowThickness", "Grosor");
            FloatSlider(brows, "BrowHeight", "Altura");
            SignedSlider(brows, "BrowTilt", "Inclinación");

            var face = Section("Nariz y boca");
            IntSlider(face, "NoseStyle", "Nariz", 8);
            FloatSlider(face, "NoseSize", "Tamaño");
            FloatSlider(face, "NoseHeight", "Altura");
            IntSlider(face, "MouthStyle", "Boca", 10);
            FloatSlider(face, "MouthWidth", "Ancho");
            FloatSlider(face, "MouthHeight", "Altura");

            var hair = Section("Pelo");
            IntSlider(hair, "HairStyle", "Peinado",
                      Nimbo.Data.Islanders.HairStyles.BaseCount);

            var body = Section("Cuerpo");
            FloatSlider(body, "BodyHeight", "Estatura");
            FloatSlider(body, "BodyBuild", "Complexión");

            var extras = Section("Detalles");
            IntSlider(extras, "GlassesStyle", "Gafas", 7);
            IntSlider(extras, "FacialHairStyle", "Barba", 6);
            FloatSlider(extras, "Blush", "Rubor");
            FloatSlider(extras, "Freckles", "Pecas");
        }

        private VisualElement Section(string title)
        {
            var card = UiTheme.Card();
            card.Add(UiTheme.Body(title, soft: true));
            _sections.Add(card);
            return card;
        }

        private void FloatSlider(VisualElement parent, string field, string label) =>
            AddFloat(parent, field, label, 0f, 1f);

        private void SignedSlider(VisualElement parent, string field, string label) =>
            AddFloat(parent, field, label, -1f, 1f);

        private void AddFloat(VisualElement parent, string field, string label,
                              float min, float max)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = UiTheme.Body(label, soft: true);
            name.style.width = 92;
            row.Add(name);

            var slider = new Slider(min, max);
            slider.style.flexGrow = 1;
            slider.RegisterValueChangedCallback(_ => PullAppearance());
            row.Add(slider);

            parent.Add(row);
            _floats[field] = slider;
        }

        private void IntSlider(VisualElement parent, string field, string label, int count)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var name = UiTheme.Body(label, soft: true);
            name.style.width = 92;
            row.Add(name);

            var slider = new SliderInt(0, count - 1);
            slider.style.flexGrow = 1;
            slider.RegisterValueChangedCallback(_ => PullAppearance());
            row.Add(slider);

            parent.Add(row);
            _ints[field] = slider;
        }

        /// <summary>
        /// Lleva los deslizadores al borrador. Va campo a campo y no por reflexión:
        /// son veinte líneas aburridas, pero la reflexión sobre un struct falla en
        /// silencio al compilar con IL2CPP y este panel es de las primeras cosas que
        /// el jugador toca.
        /// </summary>
        private void PullAppearance()
        {
            if (_draft == null) return;
            var a = _draft.Appearance;

            a.HeadShape = _ints["HeadShape"].value;
            a.HeadWidth = _floats["HeadWidth"].value;
            a.HeadHeight = _floats["HeadHeight"].value;

            a.EyeStyle = _ints["EyeStyle"].value;
            a.EyeSize = _floats["EyeSize"].value;
            a.EyeSpacing = _floats["EyeSpacing"].value;
            a.EyeHeight = _floats["EyeHeight"].value;
            a.EyeTilt = _floats["EyeTilt"].value;

            a.BrowStyle = _ints["BrowStyle"].value;
            a.BrowThickness = _floats["BrowThickness"].value;
            a.BrowHeight = _floats["BrowHeight"].value;
            a.BrowTilt = _floats["BrowTilt"].value;

            a.NoseStyle = _ints["NoseStyle"].value;
            a.NoseSize = _floats["NoseSize"].value;
            a.NoseHeight = _floats["NoseHeight"].value;

            a.MouthStyle = _ints["MouthStyle"].value;
            a.MouthWidth = _floats["MouthWidth"].value;
            a.MouthHeight = _floats["MouthHeight"].value;

            a.HairStyle = _ints["HairStyle"].value;
            a.BodyHeight = _floats["BodyHeight"].value;
            a.BodyBuild = _floats["BodyBuild"].value;

            a.GlassesStyle = _ints["GlassesStyle"].value;
            a.FacialHairStyle = _ints["FacialHairStyle"].value;
            a.Blush = _floats["Blush"].value;
            a.Freckles = _floats["Freckles"].value;

            _draft.Appearance = a;
        }

        /// <summary>El camino inverso: del borrador a los deslizadores.</summary>
        private void PushToControls()
        {
            var a = _draft.Appearance;

            SetInt("HeadShape", a.HeadShape);
            SetFloat("HeadWidth", a.HeadWidth);
            SetFloat("HeadHeight", a.HeadHeight);

            SetInt("EyeStyle", a.EyeStyle);
            SetFloat("EyeSize", a.EyeSize);
            SetFloat("EyeSpacing", a.EyeSpacing);
            SetFloat("EyeHeight", a.EyeHeight);
            SetFloat("EyeTilt", a.EyeTilt);

            SetInt("BrowStyle", a.BrowStyle);
            SetFloat("BrowThickness", a.BrowThickness);
            SetFloat("BrowHeight", a.BrowHeight);
            SetFloat("BrowTilt", a.BrowTilt);

            SetInt("NoseStyle", a.NoseStyle);
            SetFloat("NoseSize", a.NoseSize);
            SetFloat("NoseHeight", a.NoseHeight);

            SetInt("MouthStyle", a.MouthStyle);
            SetFloat("MouthWidth", a.MouthWidth);
            SetFloat("MouthHeight", a.MouthHeight);

            SetInt("HairStyle", a.HairStyle);
            SetFloat("BodyHeight", a.BodyHeight);
            SetFloat("BodyBuild", a.BodyBuild);

            SetInt("GlassesStyle", a.GlassesStyle);
            SetInt("FacialHairStyle", a.FacialHairStyle);
            SetFloat("Blush", a.Blush);
            SetFloat("Freckles", a.Freckles);

            _axes[0].SetValueWithoutNotify(_draft.Personality.Energy);
            _axes[1].SetValueWithoutNotify(_draft.Personality.Expression);
            _axes[2].SetValueWithoutNotify(_draft.Personality.Attitude);
            _axes[3].SetValueWithoutNotify(_draft.Personality.Outlook);

            _name.SetValueWithoutNotify(_draft.Identity.DisplayName ?? "");
            PullPersonality();
        }

        // SetValueWithoutNotify a propósito: al rellenar los controles no hay que
        // disparar los callbacks, o veinticinco deslizadores harían veinticinco
        // vueltas de PullAppearance sobre el mismo borrador.
        private void SetFloat(string field, float value)
        {
            if (_floats.TryGetValue(field, out var slider)) slider.SetValueWithoutNotify(value);
        }

        private void SetInt(string field, int value)
        {
            if (_ints.TryGetValue(field, out var slider)) slider.SetValueWithoutNotify(value);
        }

        // --- acciones ---------------------------------------------------------

        private void Randomize()
        {
            if (_draft == null) return;

            if (ServiceRegistry.TryGet<IIslanderFactory>(out var factory))
            {
                string keptName = _name.value;
                _draft = factory.CreateRandom();
                if (!string.IsNullOrWhiteSpace(keptName))
                {
                    var identity = _draft.Identity;
                    identity.DisplayName = keptName;
                    _draft.Identity = identity;
                }
            }
            else
            {
                // Sin fábrica, al menos que los deslizadores se muevan.
                foreach (var slider in _floats.Values)
                    slider.SetValueWithoutNotify(_rng.Range(slider.lowValue, slider.highValue));
                foreach (var slider in _ints.Values)
                    slider.SetValueWithoutNotify(_rng.Range(slider.lowValue, slider.highValue + 1));
                PullAppearance();
            }

            PushToControls();
        }

        private void Finish()
        {
            if (_draft == null) return;

            PullAppearance();
            PullPersonality();

            var identity = _draft.Identity;
            identity.DisplayName = string.IsNullOrWhiteSpace(_name.value)
                ? "Sin nombre"
                : _name.value.Trim();
            _draft.Identity = identity;

            var finished = _draft;
            _draft = null;
            Hide();

            if (_forProtagonist)
            {
                // El protagonista no entra en el censo: no es un vecino más, y meterlo
                // ahí lo pondría a trabajar, a tener necesidades y a que la simulación
                // lo mande de un lado a otro.
                EventBus.Publish(new ProtagonistCreated(
                    finished.Identity.DisplayName, finished.Appearance));
                return;
            }

            OnFinished?.Invoke(finished);
        }
    }
}
