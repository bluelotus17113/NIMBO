using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.PlayerView
{
    /// <summary>
    /// El cuerpo del protagonista: el mismo muñeco chibi que un vecino, pero movido
    /// por el jugador en vez de por la simulación.
    /// </summary>
    /// <remarks>
    /// Comparte constructor de mallas con <c>IslanderView</c> a propósito. El
    /// protagonista sale del mismo creador de personajes, así que si se dibujara con
    /// otro código acabaría pareciendo de otro juego en cuanto uno de los dos se
    /// retocara — y encima habría que hacer dos veces cada peinado.
    ///
    /// El movimiento va en el plano de la cámara y no en el del mundo: si el jugador
    /// ha girado la vista, la W tiene que seguir yendo «hacia arriba en la pantalla».
    /// Es lo que espera cualquiera que haya jugado a algo con vista cenital.
    /// </remarks>
    public sealed class PlayerBody : MonoBehaviour
    {
        [SerializeField] private float _walkSpeed = 6.5f;
        [SerializeField] private float _runSpeed = 10.5f;

        [Tooltip("Grados por segundo al girarse hacia donde anda.")]
        [SerializeField] private float _turnSpeed = 720f;

        [Tooltip("Velocidad con el vigor a cero, como fracción de la normal.")]
        [SerializeField, Range(0.2f, 1f)] private float _tiredFactor = 0.55f;

        private CharacterController _controller;
        private ChibiFaceTexture _face;
        private Transform _visual;
        private Transform _cameraTransform;

        private float _bobPhase;
        private bool _frozen;
        private Nimbo.Player.PlayerService _service;

        /// <summary>Le engancha el servicio al que va contando dónde está.</summary>
        public void Bind(Nimbo.Player.PlayerService service)
        {
            _service = service;
            VigorFraction = service?.VigorFraction ?? 1f;
        }

        /// <summary>Hacia dónde mira. Lo usa la interacción para saber qué tienes delante.</summary>
        public Vector3 Facing => transform.forward;

        /// <summary>Cierto mientras el jugador esté moviéndose de verdad.</summary>
        public bool IsMoving { get; private set; }

        /// <summary>Lo pone quien tenga que quitarle el mando: un diálogo, un menú.</summary>
        public void Freeze(bool frozen) => _frozen = frozen;

        /// <summary>
        /// Cierto mientras manda la interfaz: el menú abierto, o el modo decorar.
        /// </summary>
        /// <remarks>
        /// Es una bandera aparte de <c>_frozen</c> y no la misma: amueblar congela por
        /// un lado y la interfaz por otro, y con una sola el que se soltara primero
        /// devolvería el mando estando el otro todavía puesto.
        ///
        /// Los dos avisos escriben aquí y no se pisan porque entrar en decorar cierra
        /// el menú primero —el aviso de cierre llega antes que el del modo—, y salir
        /// del modo no abre nada.
        /// </remarks>
        private bool _uiInControl;

        private void OnEnable()
        {
            EventBus.Subscribe<MenuOpened>(OnMenuOpened);
            EventBus.Subscribe<DecorModeChanged>(OnDecorMode);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<MenuOpened>(OnMenuOpened);
            EventBus.Unsubscribe<DecorModeChanged>(OnDecorMode);
        }

        private void OnMenuOpened(MenuOpened evt) => _uiInControl = evt.Open;

        private void OnDecorMode(DecorModeChanged evt) => _uiInControl = evt.Decorating;

        public static PlayerBody Create(in AppearanceData appearance, Vector3 position,
                                        float yaw, Transform parent = null)
        {
            var go = new GameObject("Protagonista");
            if (parent != null) go.transform.SetParent(parent, worldPositionStays: false);
            go.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));

            var body = go.AddComponent<PlayerBody>();
            body.Build(appearance);
            return body;
        }

        private void Build(in AppearanceData appearance)
        {
            var meshes = ChibiMeshBuilder.Build(appearance);

            // El muñeco cuelga de un hijo para poder inclinarlo y hacerlo botar al
            // andar sin tocar el CharacterController, que si se rota se lía con las
            // colisiones.
            _visual = new GameObject("cuerpo").transform;
            _visual.SetParent(transform, worldPositionStays: false);

            Add("piel", meshes.Skin, ToonPalette.Solid(appearance.SkinTone));
            Add("ropa", meshes.Clothes, ToonPalette.Solid(OutfitColor(appearance)));
            Add("pelo", meshes.Hair, ToonPalette.Solid(appearance.HairColor));

            // La cara se guarda para poder cambiarle el gesto: el protagonista pone
            // caras igual que los vecinos, y sin quedarse con la textura habría que
            // repintarla entera cada vez.
            _face = new ChibiFaceTexture(appearance, "protagonista");
            _face.Draw(Emotion.Happy);
            Add("cara", meshes.Face, ToonPalette.Textured(_face.Texture));

            _controller = gameObject.AddComponent<CharacterController>();
            _controller.height = Mathf.Max(1.2f, meshes.Height);
            _controller.radius = 0.42f;
            _controller.center = new Vector3(0f, _controller.height * 0.5f, 0f);

            // Escalones bajos y pendientes suaves: la isla tiene bordes irregulares y
            // sin esto el protagonista se queda clavado en cualquier arruga del prado.
            _controller.stepOffset = 0.45f;
            _controller.slopeLimit = 48f;

            if (Camera.main != null) _cameraTransform = Camera.main.transform;

            void Add(string name, Mesh mesh, Material material)
            {
                var part = new GameObject(name);
                part.transform.SetParent(_visual, worldPositionStays: false);
                part.AddComponent<MeshFilter>().sharedMesh = mesh;
                part.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        /// <summary>Le cambia el gesto. Se usa al comer, al recoger algo bueno o al fallar.</summary>
        public void SetEmotion(Emotion emotion) => _face?.Draw(emotion);

        /// <summary>
        /// El color de la ropa sale del aspecto, igual que en los vecinos, para que
        /// dos protagonistas creados iguales se vean iguales.
        /// </summary>
        private static Color OutfitColor(in AppearanceData appearance)
        {
            var rng = Core.Util.Rng.FromSeed($"jugador-{appearance.HairStyle}-{appearance.HeadShape}");
            return Color.HSVToRGB(rng.NextFloat(), rng.Range(0.35f, 0.62f), rng.Range(0.78f, 0.96f));
        }

        /// <summary>Cuánto vigor le queda, de 0 a 1. Lo pone el servicio del jugador.</summary>
        public float VigorFraction { get; set; } = 1f;

        private void Update()
        {
            if (_controller == null) return;

            var move = _frozen || _uiInControl ? Vector3.zero : ReadMove();
            IsMoving = move.sqrMagnitude > 0.0001f;

            float speed = Input.GetKey(KeyCode.LeftShift) ? _runSpeed : _walkSpeed;

            // El cansancio no para al jugador, solo lo ralentiza. En este juego el
            // vigor a cero no puede dejarte tirado en mitad del campo.
            if (VigorFraction <= 0f) speed *= _tiredFactor;

            // No se puede salir de la isla. El borde del prado es irregular —va de 86
            // a 100 metros según la dirección—, así que un radio fijo o cortaría suelo
            // bueno o dejaría huecos por los que caerse. Se mira si hay suelo justo
            // donde vas a pisar, que funciona sea cual sea la forma.
            var step = move * speed * Time.deltaTime;
            if (!_indoors && step.sqrMagnitude > 0f && !HasGroundAt(transform.position + step))
            {
                step = Vector3.zero;
                IsMoving = false;
            }

            // La gravedad va aparte y siempre: sin ella, el muñeco no baja las cuestas
            // y va dando saltitos por las arrugas del prado.
            step.y = -9.8f * Time.deltaTime;
            _controller.Move(step);

            KeepOnTheIsland();

            if (IsMoving)
            {
                var look = Quaternion.LookRotation(move, Vector3.up);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, look, _turnSpeed * Time.deltaTime);
            }

            Bob(speed);

            // Se le va contando al servicio dónde ha acabado, para que la partida
            // guarde la posición sin que la lógica tenga que buscar el cuerpo.
            _service?.SyncTransform(transform.position, transform.eulerAngles.y);
        }

        /// <summary>¿Hay algo sólido debajo de ese punto?</summary>
        private static bool HasGroundAt(Vector3 position)
        {
            // Se tira desde tres metros por encima y se buscan diez hacia abajo: desde
            // los pies exactos, una cuesta abajo daría «no hay suelo» y el jugador se
            // quedaría clavado al principio de cualquier bajada.
            return Physics.Raycast(position + Vector3.up * 3f, Vector3.down,
                                   10f, ~0, QueryTriggerInteraction.Ignore);
        }

        private Vector3 _lastSafe;
        private bool _indoors;

        /// <summary>
        /// Le dice que está dentro de una casa, donde no hay que rescatarlo.
        /// </summary>
        /// <remarks>
        /// La red anticaída rescata todo lo que baje de menos seis metros, y los
        /// interiores se montan quinientos por debajo del mundo. Sin esto, entrar en
        /// casa montaba la habitación y la red devolvía al jugador a la calle en el
        /// mismo fotograma: la casa quedaba construida y vacía, y desde fuera parecía
        /// que la puerta no hacía nada.
        /// </remarks>
        public void SetIndoors(bool indoors) => _indoors = indoors;

        /// <summary>
        /// Red de seguridad por si aun así acaba en el aire.
        /// </summary>
        /// <remarks>
        /// La comprobación de arriba debería bastar, pero una malla generada tiene
        /// costuras y basta un hueco de un centímetro para colarse. Sin esto se cae
        /// para siempre: la partida se guardó una vez con el protagonista a cincuenta
        /// metros por debajo de la isla, cayendo, y no había forma de volver.
        /// </remarks>
        private void KeepOnTheIsland()
        {
            if (_indoors) return;

            var position = transform.position;

            if (position.y > -6f)
            {
                // Solo se apunta como sitio seguro si de verdad se está de pie: si no,
                // el sitio seguro acabaría siendo un punto del aire durante la caída.
                if (_controller.isGrounded) _lastSafe = position;
                return;
            }

            var rescue = _lastSafe.sqrMagnitude > 0.01f ? _lastSafe : Nimbo.Data.Player.PlayerHome.Spawn;

            // Hay que apagar el controlador para teletransportarlo: si no, se come el
            // cambio de posición y lo deja donde estaba.
            _controller.enabled = false;
            transform.position = rescue + Vector3.up * 0.5f;
            _controller.enabled = true;
        }

        /// <summary>
        /// La dirección en la que quiere andar, ya en coordenadas del mundo.
        /// </summary>
        /// <remarks>
        /// Se proyecta sobre el plano del suelo el «adelante» de la cámara. Si se
        /// usaran los ejes del mundo, con la cámara girada la W llevaría al muñeco de
        /// lado y el jugador no sabría hacia dónde va a salir.
        ///
        /// Sistema de entrada antiguo (`activeInputHandler: 0`): nada de
        /// `Keyboard.current`, que aquí lanza excepción.
        /// </remarks>
        private Vector3 ReadMove()
        {
            float x = Input.GetAxisRaw("Horizontal");
            float z = Input.GetAxisRaw("Vertical");
            var raw = new Vector3(x, 0f, z);
            if (raw.sqrMagnitude < 0.0001f) return Vector3.zero;
            raw = Vector3.ClampMagnitude(raw, 1f);

            if (_cameraTransform == null)
            {
                if (Camera.main == null) return raw;
                _cameraTransform = Camera.main.transform;
            }

            var forward = Vector3.ProjectOnPlane(_cameraTransform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
            forward.Normalize();

            var right = Vector3.Cross(Vector3.up, forward);
            return (forward * raw.z + right * raw.x).normalized * raw.magnitude;
        }

        /// <summary>
        /// El botecito al andar. No hay esqueleto ni animación: el muñeco es una malla
        /// suelta, así que el paso se finge subiendo y bajando el cuerpo entero. Suena
        /// a truco y lo es, pero a esta distancia de cámara se lee como andar.
        /// </summary>
        private void Bob(float speed)
        {
            if (_visual == null) return;

            if (!IsMoving)
            {
                _bobPhase = 0f;
                _visual.localPosition = Vector3.zero;
                _visual.localRotation = Quaternion.identity;
                return;
            }

            _bobPhase += Time.deltaTime * speed * 1.6f;
            float bob = Mathf.Abs(Mathf.Sin(_bobPhase)) * 0.12f;
            float sway = Mathf.Sin(_bobPhase * 0.5f) * 4f;

            _visual.localPosition = new Vector3(0f, bob, 0f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, sway);
        }
    }
}
