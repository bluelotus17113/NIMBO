using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Islander
{
    /// <summary>
    /// Qué le puedes decir a un vecino, y qué te falta para poder decirle más.
    /// </summary>
    /// <remarks>
    /// Hasta ahora la única forma de tratar con alguien era ponerse delante y pulsar,
    /// y eso siempre era «charlar»: las otras ocho interacciones estaban escritas, con
    /// sus efectos y sus límites diarios, y no había manera de usarlas. La vía de
    /// Convivencia subía sin abrir nada porque no había puerta donde poner la llave.
    ///
    /// Va en la ficha y no en el mundo a propósito: en este juego la vida social se lee
    /// abriendo el menú, y lo del mundo —las burbujas, las caras— es refuerzo.
    ///
    /// Los botones que aún no tienes salen igual, apagados y con lo que falta escrito.
    /// Esconderlos dejaría una fila que crece sola sin que nadie sepa por qué.
    ///
    /// Sobre el refresco: <c>UiRoot</c> llama a <see cref="Refresh"/> cada 0,4 s pase
    /// lo que pase. Derribar los botones en cada pasada se comía los clics —el puntero
    /// quedaba sobre un elemento que acababa de dejar de existir— y resetear el aviso
    /// mataba «Por hoy ya está bien» antes de medio segundo. Ahora los botones solo se
    /// levantan cuando algo de lo que manda ha cambiado, y el aviso aguanta hasta que
    /// cambies de persona o amanezca.
    /// </remarks>
    public sealed class SocialSection
    {
        private readonly VisualElement _actions;
        private readonly Label _hint;

        private string _islanderId;

        /// <summary>El habitante para quien están levantados los botones de ahora.</summary>
        private string _builtFor;

        /// <summary>
        /// Lo que mandaba la última vez que se construyeron los botones: etapas del
        /// romance, veredictos del cortejo y puertas de la vía. Si no cambia, no se toca.
        /// </summary>
        private string _signature;

        /// <summary>El día en que se escribió el aviso que ahora mismo se lee.</summary>
        private int _hintDay = -1;

        public VisualElement Root { get; }

        /// <summary>Qué hace falta para cada gesto. Los dos primeros no piden nada.</summary>
        private static readonly (SocialInteraction Interaction, string Label, Unlock? Needs)[] Gestures =
        {
            (SocialInteraction.Chat, "Charlar", null),
            (SocialInteraction.Joke, "Contar un chiste", null),
            (SocialInteraction.Compliment, "Halagar", Unlock.Compliment),
            (SocialInteraction.Hug, "Abrazar", Unlock.WarmGestures),
            (SocialInteraction.PlayTogether, "Jugar un rato", Unlock.WarmGestures),
        };

        public SocialSection()
        {
            Root = UiTheme.Card();
            Root.Add(UiTheme.Title("Qué le dices"));

            _actions = new VisualElement();
            _actions.style.flexDirection = FlexDirection.Row;
            _actions.style.flexWrap = Wrap.Wrap;
            Root.Add(_actions);

            _hint = UiTheme.Body("", soft: true);
            _hint.style.marginTop = 6;
            _hint.style.whiteSpace = WhiteSpace.Normal;
            Root.Add(_hint);
        }

        public void Refresh(string islanderId)
        {
            _islanderId = islanderId;

            if (!ServiceRegistry.TryGet<ISocialService>(out var social))
            {
                Root.style.display = DisplayStyle.None;
                return;
            }
            Root.style.display = DisplayStyle.Flex;

            int day = Today();
            string signature = Signature(social, islanderId);

            if (islanderId == _builtFor && signature == _signature && day == _hintDay) return;

            // El aviso habla de hoy y de esta persona: si cambia cualquiera de las dos
            // cosas, se va. Que cambien los botones —te has declarado, ha subido la
            // vía— no lo toca: «ya está dicho» es justo lo que hay que seguir leyendo
            // después de declararse.
            if (islanderId != _builtFor || day != _hintDay) _hint.text = "";

            _actions.Clear();
            BuildButtons(social, islanderId);

            _builtFor = islanderId;
            _signature = signature;
            _hintDay = day;
        }

        /// <summary>Lo que la sección le dice al jugador tras un gesto suyo.</summary>
        /// <remarks>
        /// Va en un método y no a pelo en cada botón para que el contrato quede en un
        /// sitio: lo escrito aquí aguanta los refrescos mientras no cambies de persona
        /// ni de día. Es internal para que la prueba pueda clavar ese contrato sin
        /// simular clics.
        /// </remarks>
        internal void Say(string message) => _hint.text = message ?? "";

        /// <summary>
        /// Todo lo que decide qué botones existen y cómo están, en una cadena. Si la
        /// cadena no cambia, la fila de botones tampoco tiene por qué.
        /// </summary>
        private static string Signature(ISocialService social, string islanderId)
        {
            var stage = social.PlayerRelationship(islanderId).Romance;
            return $"{stage}|{social.CanConfess(islanderId)}|{social.CanPropose(islanderId)}"
                 + $"|{Gates.Allows(Unlock.Compliment)}|{Gates.Allows(Unlock.WarmGestures)}"
                 + $"|{Gates.Allows(Unlock.AskFavour)}|{Gates.Allows(Unlock.Mediate)}";
        }

        /// <summary>Sin reloj registrado no hay día que mirar; vale cero, que es hoy.</summary>
        private static int Today() =>
            ServiceRegistry.TryGet<GameClock>(out var clock) ? clock.Day : 0;

        private void BuildButtons(ISocialService social, string islanderId)
        {
            for (int i = 0; i < Gestures.Length; i++) _actions.Add(Gesture(social, Gestures[i]));

            _actions.Add(FavourButton(social));
            _actions.Add(MediateButton(social));

            // Declararse y pedir la mano nunca están los dos: son dos escalones de la
            // misma escalera, y enseñar el segundo antes de subir el primero solo sirve
            // para que el jugador pulse y le digan que no.
            var etapa = social.PlayerRelationship(islanderId).Romance;
            if (etapa is Data.Social.RomanceStage.Dating or Data.Social.RomanceStage.Engaged
                or Data.Social.RomanceStage.Married)
                _actions.Add(ProposeButton(social));
            else
                _actions.Add(ConfessButton(social));
        }

        private VisualElement Gesture(ISocialService social,
                                      (SocialInteraction Interaction, string Label, Unlock? Needs) gesture)
        {
            if (gesture.Needs.HasValue && !Gates.Allows(gesture.Needs.Value, out string falta))
            {
                var locked = UiTheme.Disabled(gesture.Label);
                locked.style.marginRight = 6;
                locked.style.marginBottom = 4;
                locked.style.fontSize = 12;
                locked.tooltip = falta;
                return locked;
            }

            var button = UiTheme.Secondary(gesture.Label, () =>
            {
                // Falso quiere decir que ya se ha hecho todas las veces que se puede
                // hoy. Los límites diarios existen para que no se pueda subir una
                // amistad a tope repitiendo «charlar» cuarenta veces seguidas.
                if (!social.PlayerInteract(_islanderId, gesture.Interaction))
                    Say("Por hoy ya está bien. Mañana más.");
                else
                    Say("");
            });

            button.style.marginRight = 6;
            button.style.marginBottom = 4;
            return button;
        }

        /// <summary>
        /// Declararse: el botón que puede salir mal.
        /// </summary>
        /// <remarks>
        /// Cuando no se puede, el botón dice **por qué** en vez de desaparecer. «Te
        /// falta un ramo» es algo que hacer esta tarde; un hueco vacío no es nada.
        /// </remarks>
        private VisualElement ConfessButton(ISocialService social)
        {
            var refusal = social.CanConfess(_islanderId);

            if (refusal == CourtshipRefusal.Ok)
            {
                var button = UiTheme.Action("Declararte", () =>
                {
                    if (social.PlayerConfess(_islanderId))
                        Say("Ya está dicho. Te contestará mañana.");
                });
                button.style.backgroundColor = UiTheme.Rose;
                button.style.marginRight = 6;
                button.style.marginBottom = 4;
                return button;
            }

            var locked = UiTheme.Disabled("Declararte");
            locked.style.marginRight = 6;
            locked.style.marginBottom = 4;
            locked.style.fontSize = 12;
            locked.tooltip = Excuse(refusal);
            return locked;
        }

        /// <summary>Pedirle que te traiga algo (Convivencia 7).</summary>
        private VisualElement FavourButton(ISocialService social)
        {
            if (!Gates.Allows(Unlock.AskFavour, out string falta)) return Locked("Pedir un favor", falta);

            return Small("Pedir un favor", () =>
            {
                string traido = social.PlayerAskFavour(_islanderId);
                Say(traido == null
                    ? "Hoy ya te ha hecho uno. Mañana más."
                    : $"Te ha traído {ItemNames.Of(traido)}.");
            });
        }

        /// <summary>Mediar en la peor riña que tenga (Convivencia 9).</summary>
        private VisualElement MediateButton(ISocialService social)
        {
            if (!Gates.Allows(Unlock.Mediate, out string falta)) return Locked("Mediar", falta);

            return Small("Mediar", () =>
            {
                string conQuien = social.PlayerMediate(_islanderId);
                Say(conQuien == null
                    ? "No está reñido con nadie."
                    : "Has hablado con los dos. Se les ha bajado un poco el enfado.");
            });
        }

        private VisualElement Locked(string label, string why)
        {
            var locked = UiTheme.Disabled(label);
            locked.style.marginRight = 6;
            locked.style.marginBottom = 4;
            locked.style.fontSize = 12;
            locked.tooltip = why;
            return locked;
        }

        private static VisualElement Small(string label, System.Action onClick)
        {
            var button = UiTheme.Secondary(label, onClick);
            button.style.marginRight = 6;
            button.style.marginBottom = 4;
            return button;
        }

        /// <summary>
        /// Pedir la mano: el último escalón, y el que toca las tres mitades del juego.
        /// </summary>
        /// <remarks>
        /// El motivo por el que no se puede es media explicación del juego entero — que
        /// hace falta tiempo, cariño, un anillo y una casa donde quepáis— así que sale
        /// escrito aunque el botón esté apagado.
        /// </remarks>
        private VisualElement ProposeButton(ISocialService social)
        {
            var refusal = social.CanPropose(_islanderId);

            if (refusal == ProposalRefusal.Ok)
            {
                var button = UiTheme.Action("Pedirle la mano", () =>
                {
                    if (social.PlayerPropose(_islanderId))
                        Say("Ha dicho que sí. La aldea ya está poniendo fecha.");
                });
                button.style.backgroundColor = UiTheme.Rose;
                button.style.marginRight = 6;
                button.style.marginBottom = 4;
                return button;
            }

            var locked = UiTheme.Disabled("Pedirle la mano");
            locked.style.marginRight = 6;
            locked.style.marginBottom = 4;
            locked.style.fontSize = 12;
            locked.tooltip = Excuse(refusal);
            return locked;
        }

        private static string Excuse(ProposalRefusal refusal) => refusal switch
        {
            ProposalRefusal.NotDating => "Primero habría que salir.",
            ProposalRefusal.TooEarly => "Lleváis muy poco. Dale unos días más.",
            ProposalRefusal.NotFondEnough => "Todavía no os queréis tanto.",
            ProposalRefusal.NoRing => "Hace falta un anillo, y eso se fabrica.",
            ProposalRefusal.HomeTooSmall => "Tu cabaña se os queda pequeña. Amplíala antes.",
            ProposalRefusal.AlreadyEngaged => "Ya está dicho.",
            _ => "Ahora mismo no.",
        };

        private static string Excuse(CourtshipRefusal refusal) => refusal switch
        {
            CourtshipRefusal.NotUnlocked =>
                Gates.Short(Unlock.Courtship) + " para atreverte a tanto.",
            CourtshipRefusal.NotFriendEnough => "Todavía no sois ni amigos.",
            CourtshipRefusal.NoBouquet => "Con las manos vacías no. Hace falta un ramo.",
            CourtshipRefusal.TooSoon => "Te dijo que no hace poco. Deja pasar unos días.",
            CourtshipRefusal.AlreadyCourting => "Ya está dicho. Te contestará mañana.",
            CourtshipRefusal.Taken => "Ya está con otra persona.",
            _ => "Ahora mismo no.",
        };
    }
}
