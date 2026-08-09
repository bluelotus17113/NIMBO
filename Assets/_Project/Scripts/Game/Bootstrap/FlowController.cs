using Nimbo.Core.Events;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Nimbo.Game.Bootstrap
{
    /// <summary>
    /// Traduce lo que el jugador pide en el menú a lo que hay que hacer con la partida.
    /// </summary>
    /// <remarks>
    /// Existe para que el menú no tenga que conocer a <c>GameBootstrap</c>. La
    /// interfaz vive en <c>Nimbo.UI</c>, que está por debajo de <c>Nimbo.Game</c> en
    /// el grafo de ensamblados; si el botón de «Continuar» llamara al arranque
    /// directamente, se cerraría el ciclo y ni compilaría. Publica el menú, escucha
    /// esto, y cada uno se queda en su lado.
    ///
    /// No es persistente entre escenas a propósito: al volver al menú la escena se
    /// recarga entera y este objeto vuelve a nacer limpio con ella.
    /// </remarks>
    public sealed class FlowController : MonoBehaviour
    {
        [SerializeField] private GameBootstrap _bootstrap;

        private void Awake()
        {
            if (_bootstrap == null) _bootstrap = FindFirstObjectByType<GameBootstrap>();
        }

        private void OnEnable()
        {
            EventBus.Subscribe<NewGameRequested>(OnNewGame);
            EventBus.Subscribe<ContinueRequested>(OnContinue);
            EventBus.Subscribe<ReturnToMenuRequested>(OnReturnToMenu);
            EventBus.Subscribe<QuitRequested>(OnQuit);
            EventBus.Subscribe<GamePaused>(OnPaused);
        }

        private void OnDisable()
        {
            EventBus.Unsubscribe<NewGameRequested>(OnNewGame);
            EventBus.Unsubscribe<ContinueRequested>(OnContinue);
            EventBus.Unsubscribe<ReturnToMenuRequested>(OnReturnToMenu);
            EventBus.Unsubscribe<QuitRequested>(OnQuit);
            EventBus.Unsubscribe<GamePaused>(OnPaused);

            // Si se descarga la escena con el juego en pausa, el tiempo se quedaría
            // a cero en la siguiente. Se deja como estaba pase lo que pase.
            Time.timeScale = 1f;
        }

        private void OnNewGame(NewGameRequested _) => _bootstrap?.StartGame(newGame: true);

        private void OnContinue(ContinueRequested _) => _bootstrap?.StartGame(newGame: false);

        /// <summary>
        /// Pausar es parar el reloj, y el reloj corre sobre <c>Time.deltaTime</c>, así
        /// que basta con la escala del tiempo. La interfaz sigue viva porque se
        /// refresca con <c>unscaledDeltaTime</c>, que era el motivo de usarlo allí.
        /// </summary>
        private void OnPaused(GamePaused evt) => Time.timeScale = evt.Paused ? 0f : 1f;

        private void OnReturnToMenu(ReturnToMenuRequested _)
        {
            if (_bootstrap == null) return;

            _bootstrap.WriteSave();
            Time.timeScale = 1f;

            // El arranque es DontDestroyOnLoad, así que recargar la escena lo dejaría
            // vivo y tendríamos dos islas montadas y dos relojes corriendo. Se devuelve
            // a la escena activa para que la recarga se lo lleve por delante como a
            // todo lo demás — y su OnDestroy, que es quien vacía el registro de
            // servicios, se ejecuta en el momento correcto.
            SceneManager.MoveGameObjectToScene(_bootstrap.gameObject,
                                               SceneManager.GetActiveScene());

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuit(QuitRequested _)
        {
            // Guardar antes de salir, siempre. OnApplicationQuit del arranque también
            // lo hace, pero desde el menú de inicio no hay partida montada y esta es
            // la única salida que existe.
            _bootstrap?.WriteSave();

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
