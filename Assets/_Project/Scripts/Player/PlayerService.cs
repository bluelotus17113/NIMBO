using Nimbo.Core.Events;
using Nimbo.Core.Time;
using Nimbo.Data.Player;
using UnityEngine;

namespace Nimbo.Player
{
    /// <summary>
    /// El protagonista visto desde la lógica: dónde está, cuánto vigor le queda y
    /// cuándo hay que dibujarlo.
    /// </summary>
    /// <remarks>
    /// Guarda la posición al vuelo en el <c>PlayerState</c>, que va dentro de la
    /// partida: al volver, apareces donde lo dejaste y no en la puerta de casa.
    ///
    /// El vigor **no mata** (ver `Docs/04_ALDEA.md`, §2). A cero no puedes usar
    /// herramientas y andas más lento; ni te desmayas ni pierdes nada. Se recupera
    /// durmiendo y comiendo, no con el paso del tiempo, porque si se recuperase solo
    /// bastaría con esperar y dejaría de ser una decisión.
    /// </remarks>
    public sealed class PlayerService
    {
        private readonly PlayerState _state;
        private readonly GameClock _clock;

        private int _lastDay;

        public PlayerService(PlayerState state, GameClock clock)
        {
            _state = state;
            _clock = clock;
            _lastDay = clock.Day;
        }

        public PlayerState State => _state;
        public bool Exists => _state.Created;

        public float Vigor => _state.Vigor;
        public float VigorFraction => Mathf.Clamp01(_state.Vigor / PlayerState.MaxVigor);

        /// <summary>Puede usar herramientas mientras le quede algo de vigor.</summary>
        public bool CanUseTool => _state.Vigor > 0f;

        /// <summary>Dónde está. Lo mantiene al día el cuerpo con <see cref="SyncTransform"/>.</summary>
        public Vector3 Position => new Vector3(_state.X, _state.Y, _state.Z);

        /// <summary>
        /// Lo llama el creador de personajes al terminar. A partir de aquí el
        /// protagonista existe y la partida puede empezar de verdad.
        /// </summary>
        public void Create(string displayName, in Data.Islanders.AppearanceData appearance,
                           Vector3 spawn)
        {
            _state.DisplayName = displayName;
            _state.Appearance = appearance;
            _state.X = spawn.x;
            _state.Y = spawn.y;
            _state.Z = spawn.z;
            _state.Created = true;
        }

        /// <summary>
        /// El cuerpo le dice dónde ha acabado. Lo llama la vista cada fotograma.
        /// </summary>
        /// <remarks>
        /// Va en este sentido —del cuerpo al servicio— y no al revés porque el cuerpo
        /// vive en <c>Nimbo.Art</c> y este módulo está por debajo: si el servicio
        /// tuviera que ir a buscar el cuerpo, se cerraría un ciclo entre ensamblados
        /// y ni compilaría.
        /// </remarks>
        public void SyncTransform(Vector3 position, float yaw)
        {
            _state.X = position.x;
            _state.Y = position.y;
            _state.Z = position.z;
            _state.Yaw = yaw;
        }

        /// <summary>Gasta vigor. Nunca baja de cero y nunca hace nada más que eso.</summary>
        public void SpendVigor(float amount)
        {
            if (amount <= 0f) return;
            SetVigor(_state.Vigor - amount);
        }

        public void RestoreVigor(float amount)
        {
            if (amount <= 0f) return;
            SetVigor(_state.Vigor + amount);
        }

        private void SetVigor(float value)
        {
            float clamped = Mathf.Clamp(value, 0f, PlayerState.MaxVigor);
            if (Mathf.Approximately(clamped, _state.Vigor)) return;

            _state.Vigor = clamped;
            EventBus.Publish(new VigorChanged(clamped));
        }

        /// <summary>
        /// Lo llama el arranque cada fotograma.
        /// </summary>
        /// <remarks>
        /// Ya no repone nada. Reponía al pasar de día —y el comentario que había aquí
        /// decía justo lo contrario, que el vigor no se recupera con el tiempo «porque
        /// si se recuperase solo bastaría con esperar y dejaría de ser una decisión».
        /// Bastaba con esperar: a medianoche volvía a estar lleno, así que la hamaca no
        /// hacía nada que no hiciera el reloj y el vigor no limitaba absolutamente
        /// nada. Se puso así cuando no había cama; la hay desde hace tiempo.
        ///
        /// Sigue sin castigar, que es el contrato (`Docs/04_ALDEA.md`, §2): a cero no
        /// usas herramientas y andas más lento. Para volver a tenerlo, dormir en la
        /// hamaca —que además te lleva a la mañana siguiente— o comer algo.
        /// </remarks>
        public void Tick()
        {
            if (_clock.Day == _lastDay) return;
            _lastDay = _clock.Day;
        }
    }
}
