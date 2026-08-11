using Nimbo.Art.World;
using Nimbo.Core.Events;
using UnityEngine;

namespace Nimbo.Art.CameraWork
{
    /// <summary>
    /// El MonoBehaviour que conecta CameraRig con la escena de Unity. Va en el mismo
    /// GameObject que la cámara y reacciona a los eventos del juego.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class IslandCamera : MonoBehaviour
    {
        private CameraRig _rig;
        private Camera _cam;
        private WorldView _world;

        // Seguimiento de un habitante enfocado.
        private Transform _focusedBody;
        private bool _followingFocus;

        // A quién sigue cuando no está enfocando a nadie. Desde que el jugador tiene
        // cuerpo, el estado normal de la cámara es ir detrás de él; el plano general
        // pasó a ser lo que se ve mientras no hay protagonista.
        private Transform _player;

        [Header("Seguimiento del protagonista")]
        [SerializeField] private float _followDistance = 26f;
        [SerializeField] private float _followPitch = 42f;
        [SerializeField] private float _followHeight = 1.2f;

        /// <summary>
        /// Le dice a quién seguir. Lo llama el arranque en cuanto el cuerpo existe;
        /// con null vuelve al plano general de la isla.
        /// </summary>
        public void Follow(Transform player)
        {
            _player = player;
            if (_rig == null) return;

            if (player != null)
            {
                _rig.Target = FollowPose(_rig.Target.Yaw);
                _rig.SnapToTarget();
            }
        }

        private CameraPose FollowPose(float yaw) => new CameraPose
        {
            Pivot = _player.position + Vector3.up * _followHeight,
            Distance = _followDistance,
            Pitch = _followPitch,
            Yaw = yaw,
        };

        private bool _paused;
        private Vector3 _lastMousePosition;

        // ── ciclo de vida de Unity ─────────────────────────────────────────

        private void Awake()
        {
            _cam = GetComponent<Camera>();
        }

        private void Start()
        {
            _world = FindFirstObjectByType<WorldView>();
            if (_world == null)
            {
                Debug.LogError("IslandCamera: no se encontró WorldView en la escena.");
                return;
            }

            _rig = new CameraRig(_world.IslandRadius);
            ApplyPose();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<IslanderFocused>(OnIslanderFocused);
            EventBus.Subscribe<GamePaused>(OnGamePaused);
        }

        private void OnDisable()
        {
            // Darse de baja es obligatorio: el EventBus guarda delegados y un
            // suscriptor muerto que no se dio de baja peta al recargar la escena.
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<IslanderFocused>(OnIslanderFocused);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
        }

        // ── eventos del juego ──────────────────────────────────────────────

        private void OnGameLoaded(GameLoaded _)
        {
            if (_rig == null) return;

            _rig.Target = new CameraPose
            {
                Pivot = Vector3.zero,
                Distance = 150f,
                Pitch = 45f,
                Yaw = 0f,
            };
            _rig.SnapToTarget();
            _focusedBody = null;
            _followingFocus = false;
            _followingFocus = false;
            ApplyPose();
        }

        private void OnIslanderFocused(IslanderFocused evt)
        {
            if (_rig == null || _world == null) return;

            if (string.IsNullOrEmpty(evt.IslanderId))
            {
                // Dejó de enfocar: se vuelve al protagonista si lo hay, y al plano
                // general solo si todavía no existe. El yaw no se toca en ninguno de
                // los dos casos, para que la transición no dé un latigazo.
                _focusedBody = null;
                _followingFocus = false;
                _rig.Target = _player != null
                    ? FollowPose(_rig.Target.Yaw)
                    : new CameraPose
                    {
                        Pivot = Vector3.zero,
                        Distance = 150f,
                        Pitch = 45f,
                        Yaw = _rig.Target.Yaw,
                    };
            }
            else
            {
                if (_world.TryGetIslander(evt.IslanderId, out var body))
                {
                    _focusedBody = body;
                    _followingFocus = true;

                    // Pivote en la cabeza: un metro y medio por encima del suelo.
                    Vector3 headPos = body.position + Vector3.up * 1.1f;
                    _rig.Frame(headPos, 14f);

                    // Pitch bajo para verle la cara, yaw intacto para no marear.
                    var t = _rig.Target;
                    t.Pitch = 22f;
                    _rig.Target = t;
                }
            }
        }

        private void OnGamePaused(GamePaused evt)
        {
            _paused = evt.Paused;
        }

        // ── bucle principal ────────────────────────────────────────────────

        private void Update()
        {
            if (_rig == null || _world == null) return;

            if (!_paused)
                HandleInput();
        }

        /// <summary>
        /// En LateUpdate para pillar la posición real tras el movimiento
        /// de los habitantes en el Update de WorldView. Si se hiciera en Update
        /// la cámara iría un fotograma por detrás y el muñeco temblaría.
        /// </summary>
        private void LateUpdate()
        {
            if (_rig == null) return;

            UpdateFocusTracking();
            UpdatePlayerTracking();
            _rig.Advance(Time.unscaledDeltaTime);
            ApplyPose();
        }

        // ── entrada del jugador (solo UnityEngine.Input) ────────────────────

        private void HandleInput()
        {
            // WASD o flechas → desplazar el pivote.
            Vector3 panInput = Vector3.zero;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    panInput.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  panInput.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  panInput.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) panInput.x += 1f;

            bool orbiting = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            float scroll = Input.mouseScrollDelta.y;

            bool manualInput = panInput.sqrMagnitude > 0.001f
                            || orbiting
                            || Mathf.Abs(scroll) > 0.001f;

            if (!manualInput)
            {
                _lastMousePosition = Input.mousePosition;
                return;
            }

            // Desplazar en el plano de la cámara proyectado sobre el suelo.
            if (panInput.sqrMagnitude > 0.001f)
            {
                Vector3 camForward = _cam.transform.forward;
                Vector3 camRight = _cam.transform.right;

                // Proyectar sobre Y=0: el movimiento siempre es horizontal aunque la
                // cámara esté inclinada. Sin esto, mirar hacia abajo hace que W meta
                // la cámara bajo tierra.
                camForward.y = 0f;
                camRight.y = 0f;

                if (camForward.sqrMagnitude > 0.001f) camForward.Normalize();
                else camForward = Vector3.forward;

                if (camRight.sqrMagnitude > 0.001f) camRight.Normalize();
                else camRight = Vector3.right;

                // Escalar por la distancia actual: de lejos el paso es más grande
                // para que el desplazamiento se sienta constante en pantalla.
                float panSpeed = _rig.Current.Distance * 12f * Time.unscaledDeltaTime;
                Vector3 worldDelta = (camForward * panInput.z + camRight * panInput.x) * panSpeed;
                _rig.Pan(worldDelta);
            }

            if (orbiting)
            {
                Vector3 mouseDelta = Input.mousePosition - _lastMousePosition;
                float sensitivity = 0.3f;
                _rig.Orbit(-mouseDelta.x * sensitivity, -mouseDelta.y * sensitivity);
            }

            if (Mathf.Abs(scroll) > 0.001f)
            {
                // Escalar por distancia: sin esto, de lejos el zoom va a paso de
                // tortuga y de cerca se pega un salto al mínimo toque de rueda.
                float zoomStep = scroll * _rig.Current.Distance * 0.05f;
                _rig.Zoom(zoomStep);
            }

            // El jugador ha movido la cámara a mano: manda él.
            _followingFocus = false;

            _lastMousePosition = Input.mousePosition;
        }

        // ── seguimiento ────────────────────────────────────────────────────

        /// <summary>
        /// Mientras un habitante está enfocado y el jugador no ha tomado el mando,
        /// el pivote persigue su cabeza fotograma a fotograma porque los habitantes
        /// andan. Si solo se apuntara una vez, el muñeco se saldría del plano a los
        /// tres segundos.
        /// </summary>
        /// <summary>
        /// Sigue al protagonista mientras no se esté mirando a un vecino.
        /// </summary>
        /// <remarks>
        /// El giro se respeta: si el jugador ha orbitado, la cámara le sigue desde
        /// donde él la puso. Recolocarla sola a un ángulo fijo cada fotograma es lo
        /// que hace que una cámara de seguimiento se sienta como un forcejeo.
        /// </remarks>
        private void UpdatePlayerTracking()
        {
            if (_followingFocus || _player == null) return;
            _rig.Target = FollowPose(_rig.Target.Yaw);
        }

        private void UpdateFocusTracking()
        {
            if (!_followingFocus)
                return;

            // Si el Transform fue destruido (el habitante se fue de la isla),
            // volvemos al plano general en vez de apuntar a la nada.
            if (_focusedBody == null)
            {
                _focusedBody = null;
                _followingFocus = false;

                // Si hay protagonista se vuelve a él; el plano general solo queda
                // para cuando todavía no lo hay.
                _rig.Target = _player != null
                    ? FollowPose(_rig.Target.Yaw)
                    : new CameraPose
                    {
                        Pivot = Vector3.zero,
                        Distance = 150f,
                        Pitch = 45f,
                        Yaw = _rig.Target.Yaw,
                    };
                return;
            }

            Vector3 headPos = _focusedBody.position + Vector3.up * 1.1f;
            _rig.Target = new CameraPose
            {
                Pivot = headPos,
                Distance = 14f,
                Pitch = 22f,
                Yaw = _rig.Target.Yaw,
            };
        }

        // ── helpers ────────────────────────────────────────────────────────

        private void ApplyPose()
        {
            _cam.transform.position = _rig.Position;
            _cam.transform.rotation = _rig.Rotation;
        }
    }
}
