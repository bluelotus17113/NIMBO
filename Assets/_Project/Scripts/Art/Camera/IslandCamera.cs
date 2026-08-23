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

        // La casa en la que esté metido, si lo está. Dentro manda otra cámara.
        private InteriorView _interior;

        [Header("Seguimiento del protagonista")]
        // Tercera persona a la altura de los ojos: pivote en la cabeza del muñeco y
        // cámara detrás, un poco por encima. La terna anterior (15 m / 48°) era una
        // cámara de seguimiento ALTA: se veía la cara, pero la isla se leía como una
        // maqueta, no había forma de mirar lo que había al fondo ni de pararse junto a
        // algo y verlo de frente. A 5,5 m y 18° el muñeco ocupa pantalla, el mundo
        // recupera escala de persona y el antiobstáculos puede esquivar lo que se cruce
        // entre él y la cámara. Son punto de partida por comparación con lo que había,
        // no una medida: retócalos mirando el juego.
        [SerializeField] private float _followDistance = 5.5f;
        [SerializeField] private float _followPitch = 18f;
        [SerializeField] private float _followHeight = 1.2f;

        [Header("Mirar con el ratón")]
        [Tooltip("Grados de giro por unidad de movimiento del ratón.")]
        [SerializeField] private float _lookSensitivity = 2.2f;

        [Tooltip("Cierto para que el ratón gire la cámara sin apretar ningún botón.")]
        [SerializeField] private bool _freeLook = true;

        /// <summary>
        /// Le dice a quién seguir. Lo llama el arranque en cuanto el cuerpo existe;
        /// con null vuelve al plano general de la isla.
        /// </summary>
        public void Follow(Transform player)
        {
            _player = player;

            // Sin protagonista no se captura el ratón: la cámara del plano general se
            // maneja arrastrando, y ahí el cursor hace falta.
            ApplyPointerState();

            if (_rig == null) return;

            if (player != null)
            {
                // Aparecer es empezar de nuevo: pitch de serie, no el que dejara la
                // partida anterior.
                _playerPitch = null;
                _rig.Target = FollowPose(_rig.Target.Yaw);
                _rig.SnapToTarget();
            }
        }

        // El pitch que el jugador eligió mirando arriba/abajo con el ratón. El
        // seguimiento reapunta la pose cada fotograma; si esa pose llevara el pitch
        // clavado de serie, cualquier mirada elegida se desharía sola antes de mover un
        // metro —y pasaba de verdad: el arrastre con botón derecho cambiaba el pitch y
        // el seguimiento lo pisaba en el mismo LateUpdate, así que la «única forma de
        // cambiar la altura» que anunciaba el comentario no hacía nada—. Va a null en
        // los cambios de mundo (cargar, aparecer, cruzar una puerta), donde lo que
        // valía fuera no tiene por qué valer dentro.
        private float? _playerPitch;

        private CameraPose FollowPose(float yaw) => new CameraPose
        {
            Pivot = _player.position + Vector3.up * _followHeight,
            Distance = _followDistance,
            Pitch = _playerPitch ?? _followPitch,
            Yaw = yaw,
        };

        private bool _paused;

        // ── ciclo de vida de Unity ─────────────────────────────────────────

        /// <summary>
        /// El fondo de dentro de una casa.
        /// </summary>
        /// <remarks>
        /// Fuera, lo que hay detrás de la isla es cielo. Dentro no hay nada detrás
        /// —la habitación flota sola medio kilómetro bajo el mundo—, y con el azul del
        /// cielo el cuarto parecía una maqueta a la intemperie en vez de un sitio
        /// cerrado. Un tono cálido y oscuro lo cierra sin necesidad de techo.
        /// </remarks>
        private static readonly Color IndoorBackdrop = new(0.24f, 0.20f, 0.25f);

        private Color _outdoorBackdrop;

        private void Awake()
        {
            _cam = GetComponent<Camera>();
            _outdoorBackdrop = _cam.backgroundColor;
        }

        private void Start()
        {
            _interior = FindFirstObjectByType<InteriorView>();
            _world = FindFirstObjectByType<WorldView>();
            if (_world == null)
            {
                Debug.LogError("IslandCamera: no se encontró WorldView en la escena.");
                return;
            }

            _rig = new CameraRig(_world.IslandRadius);
            _rig.AllowSecondIsland(Data.World.Archipelago.HomeCentre,
                                   Data.World.Archipelago.HomeRadius);
            ApplyPose();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<IslanderFocused>(OnIslanderFocused);
            EventBus.Subscribe<GamePaused>(OnGamePaused);
            EventBus.Subscribe<PointerNeeded>(OnPointerNeeded);
        }

        private void OnDisable()
        {
            // Darse de baja es obligatorio: el EventBus guarda delegados y un
            // suscriptor muerto que no se dio de baja peta al recargar la escena.
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<IslanderFocused>(OnIslanderFocused);
            EventBus.Unsubscribe<GamePaused>(OnGamePaused);
            EventBus.Unsubscribe<PointerNeeded>(OnPointerNeeded);

            // Devolver el ratón al salir. Si no, apagar este componente —volver al
            // menú, recargar la escena— deja el cursor capturado y sin nadie que lo
            // suelte: no se puede clicar nada ni cerrar la ventana.
            _pointerCaptured = false;
            ReleasePointer();
        }

        // ── eventos del juego ──────────────────────────────────────────────

        private void OnGameLoaded(GameLoaded _)
        {
            if (_rig == null) return;

            _focusedBody = null;
            _followingFocus = false;
            _playerPitch = null;

            // Si ya hay protagonista, se le encuadra a él y no al plano general.
            //
            // Esto no es una comodidad: el mundo y la cámara escuchan los dos el mismo
            // aviso, y el orden entre ellos no está garantizado. Cuando la cámara
            // atendía la segunda, ponía el plano general encima del encuadre que el
            // mundo acababa de pedirle y la partida arrancaba mirando la isla desde
            // lejos con el muñeco perdido en el medio.
            _rig.Target = _player != null
                ? FollowPose(0f)
                : new CameraPose
                {
                    Pivot = Vector3.zero,
                    Distance = 150f,
                    Pitch = 45f,
                    Yaw = 0f,
                };

            _rig.SnapToTarget();
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
            ApplyPointerState();
        }

        private bool _pointerNeeded;

        private void OnPointerNeeded(PointerNeeded evt)
        {
            _pointerNeeded = evt.Needed;
            ApplyPointerState();
        }

        /// <summary>
        /// Girar con el ratón solo cuando no haga falta el ratón para otra cosa.
        /// </summary>
        /// <remarks>
        /// Mientras se gira, el cursor va **capturado**: si no, se llega al borde de la
        /// ventana y la cámara deja de girar a mitad de vuelta. Y capturado no se puede
        /// clicar, así que en cuanto se abre la mochila, una ficha, la tienda o la
        /// pausa hay que soltarlo. Es el trato normal de un juego en tercera persona, y
        /// la parte que se rompe sola si alguien añade un panel: por eso quien decide
        /// es la interfaz entera de una vez y no cada panel por su cuenta.
        /// </remarks>
        private bool ShouldLook => _freeLook && _player != null && !_paused
                                && !_pointerNeeded && !PlayerView.DebugPanel.AnyOpen;

        private bool _pointerCaptured;

        private void ApplyPointerState()
        {
            bool capture = ShouldLook;
            if (capture == _pointerCaptured) return;

            _pointerCaptured = capture;
            if (capture) CapturePointer(); else ReleasePointer();
        }

        private static void CapturePointer()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void ReleasePointer()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ── bucle principal ────────────────────────────────────────────────

        private void Update()
        {
            if (_rig == null || _world == null) return;

            // Se repasa cada fotograma porque el panel de pruebas no avisa por evento
            // —es un estático del mismo ensamblado— y sin repasar, abrirlo con F1
            // dejaría el ratón capturado y sus botones sin poder pulsarse.
            ApplyPointerState();

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

            // Cruzar una puerta son quinientos metros de golpe. Sin pegar la cámara a
            // su sitio, entrar en casa sería un picado de dos segundos desde el prado
            // hasta el sótano del mundo, y salir, el mismo viaje al revés.
            bool indoors = Indoors();
            bool crossedADoor = indoors != _wasIndoors;
            _wasIndoors = indoors;

            // Cambiar de mundo devuelve el pitch a la pose de serie: el ángulo con el
            // que se mira un prado no es el que quiere un cuarto cerrado, ni al revés.
            // Lo que eligió el jugador vale hasta la puerta, y detrás de la puerta
            // vuelve a empezar.
            if (crossedADoor) _playerPitch = null;

            if (crossedADoor) _cam.backgroundColor = indoors ? IndoorBackdrop : _outdoorBackdrop;

            if (indoors)
            {
                // El suelo de la cámara se baja al de la habitación: si no, se queda
                // clavada tres metros sobre el prado mirando a medio kilómetro de
                // profundidad, que es cómo se entraba en casa y no se veía nada.
                _rig.GroundLevel = InteriorView.Anchor.y;
                _rig.Target = RoomPose(_rig.Target.Yaw);

                if (crossedADoor) _rig.SnapToTarget();
                _rig.Advance(Time.unscaledDeltaTime);

                // Sin apartar la cámara de lo que tenga detrás: dentro, lo que hay
                // detrás es la pared de la propia habitación, y esquivarla sería
                // meter la cámara en el cuarto.
                _cam.transform.SetPositionAndRotation(_rig.Position, _rig.Rotation);
                return;
            }

            _rig.GroundLevel = 0f;

            UpdateFocusTracking();
            UpdatePlayerTracking();

            if (crossedADoor) _rig.SnapToTarget();
            _rig.Advance(Time.unscaledDeltaTime);
            ApplyPose();
        }

        private bool _wasIndoors;

        private bool Indoors() => _interior != null && _interior.Inside && _interior.CurrentRoom != null;

        /// <summary>
        /// El plano de dentro de una casa: la habitación entera desde arriba.
        /// </summary>
        /// <remarks>
        /// Aquí no se sigue al protagonista. Un cuarto mide doce metros y la cámara de
        /// fuera va a quince, así que seguirle dejaría la cámara al otro lado de la
        /// pared todo el rato, pegando tirones cada vez que esquivara una. Se encuadra
        /// la habitación y ya está, como en Animal Crossing: dentro no hay nada que
        /// buscar, se ve todo de un vistazo.
        /// </remarks>
        private CameraPose RoomPose(float yaw)
        {
            var room = _interior.CurrentRoom;
            float width = room.Width * InteriorView.Tile;
            float depth = room.Height * InteriorView.Tile;

            return new CameraPose
            {
                Pivot = _interior.RoomOrigin + new Vector3(width * 0.5f, 1.2f, depth * 0.5f),
                Distance = Mathf.Max(width, depth) * 1.6f + 4f,
                Pitch = 62f,
                Yaw = yaw,
            };
        }

        // ── entrada del jugador (solo UnityEngine.Input) ────────────────────

        private void HandleInput()
        {
            // WASD o flechas → desplazar el pivote, pero SOLO sin protagonista.
            //
            // Desde que el jugador tiene cuerpo, esas teclas son suyas: si las leyeran
            // los dos, cada paso movería al muñeco y arrastraría la cámara al doble de
            // velocidad, y el muñeco se saldría del plano hacia atrás. Con cuerpo, a la
            // cámara le quedan el ratón y la rueda.
            Vector3 panInput = Vector3.zero;
            if (_player == null)
            {
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    panInput.z += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  panInput.z -= 1f;
                if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  panInput.x -= 1f;
                if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) panInput.x += 1f;
            }

            // Mirar libre: mover el ratón gira la cámara alrededor del protagonista,
            // sin apretar nada. Los dos ejes: el horizontal orbita, el vertical sube o
            // baja la mirada entre los topes del rig (5°–78°).
            //
            // El eje vertical estuvo clavado a propósito cuando la cámara iba a 48°:
            // ese picado veía a la vez al muñeco y lo que tenía delante, y un pitch
            // movido por accidente estropeaba el único encuadre que funcionaba. En
            // tercera persona la decisión se revierte sola: sin mirada vertical no se
            // puede ver el Árbol Nimbo, ni lo que hay en una repisa, ni la cara de
            // quien tienes delante. Quedó descartado el auto-alineado (volver solo a
            // 18° tras unos segundos quieto): pelea con el jugador justo cuando está
            // mirando algo, añade un temporizador más que afinar, y este código ya
            // documenta la filosofía contraria —recolocar la cámara sin que nadie la
            // pida es lo que hace que un seguimiento se sienta como un forcejeo—. El
            // pitch elegido se recuerda en `_playerPitch` y dura hasta que el mundo
            // cambia; volver al ángulo de serie siempre cuesta una puerta.
            //
            // `Mouse X`/`Mouse Y` ya vienen como diferencia por fotograma: multiplicarlos
            // por deltaTime los dejaría el doble de lentos a sesenta fotogramas que a
            // ciento veinte, que es justo lo contrario de lo que se busca.
            if (ShouldLook)
            {
                float lookX = Input.GetAxis("Mouse X");
                float lookY = Input.GetAxis("Mouse Y");
                if (Mathf.Abs(lookX) > 0.0001f || Mathf.Abs(lookY) > 0.0001f)
                {
                    // Ratón hacia arriba = mirar arriba = menos pitch (el pitch
                    // positivo mira hacia abajo); la misma convención del arrastre
                    // de más abajo.
                    _rig.Orbit(lookX * _lookSensitivity, -lookY * _lookSensitivity);
                    _playerPitch = _rig.Target.Pitch;
                    _followingFocus = false;
                }
            }

            bool orbiting = Input.GetMouseButton(1) || Input.GetMouseButton(2);
            float scroll = Input.mouseScrollDelta.y;

            bool manualInput = panInput.sqrMagnitude > 0.001f
                            || orbiting
                            || Mathf.Abs(scroll) > 0.001f;

            if (!manualInput) return;

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

            // Arrastrar con el derecho sigue existiendo para quien no use la mirada
            // libre, aunque para cambiar la altura ya no hace falta: el eje Y del
            // ratón suelto la mueve desde arriba.
            //
            // Va por ejes del ratón y no por diferencias de `mousePosition`, que es
            // como estaba: con el cursor capturado para mirar libre, `mousePosition` se
            // queda clavada en el centro de la ventana y el arrastre dejaría de girar
            // sin que nada avisara. Los ejes funcionan capturado y suelto.
            if (orbiting)
            {
                _rig.Orbit(Input.GetAxis("Mouse X") * _lookSensitivity,
                           -Input.GetAxis("Mouse Y") * _lookSensitivity);
                _playerPitch = _rig.Target.Pitch;
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
            _cam.transform.position = Unobstructed(_rig.Current.Pivot, _rig.Position);
            _cam.transform.rotation = _rig.Rotation;
        }

        [Tooltip("Hueco que se deja entre la cámara y lo que tenga detrás, en metros.")]
        [SerializeField] private float _cameraPadding = 0.6f;

        /// <summary>
        /// Acerca la cámara si hay algo entre ella y lo que mira.
        /// </summary>
        /// <remarks>
        /// Sin esto, seguir al protagonista mete la cámara dentro del primer edificio o
        /// del Árbol Nimbo que le pille por detrás, y el jugador se queda mirando el
        /// interior de una malla sin entender qué ha pasado. Se vio a la primera
        /// partida y no lo pilla ningún test: la posición era correcta, lo que estaba
        /// mal es que había una pared en medio.
        ///
        /// El suelo mínimo de <c>CameraRig</c> no basta: aquel evita hundirse en el
        /// prado, y esto evita meterse en lo que hay de pie sobre él.
        /// </remarks>
        private Vector3 Unobstructed(Vector3 pivot, Vector3 desired)
        {
            var offset = desired - pivot;
            float distance = offset.magnitude;
            if (distance < 0.01f) return desired;

            // QueryTriggerInteraction.Ignore para que un disparador —una puerta, una
            // zona de aviso— no tire de la cámara hacia delante como si fuera un muro.
            if (!Physics.Raycast(pivot, offset / distance, out var hit, distance,
                                 ~0, QueryTriggerInteraction.Ignore))
                return desired;

            float pulled = Mathf.Max(hit.distance - _cameraPadding, 1.5f);
            return pivot + offset / distance * pulled;
        }
    }
}
