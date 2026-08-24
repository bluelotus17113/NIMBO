using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.UI.Menu
{
    /// <summary>
    /// Los ajustes de pantalla que el jugador elige —modo de ventana, resolución
    /// y escala de la interfaz— y que sobreviven al cierre.
    /// </summary>
    /// <remarks>
    /// Mismo camino que <see cref="Nimbo.Core.Settings.AudioPrefs"/>: claves
    /// propias en PlayerPrefs, escritura en cada cambio y disco al salir del
    /// panel. Vive en la interfaz y no en Core porque su único lector es el
    /// propio panel de ajustes: los volúmenes los leen dos sistemas distintos
    /// (menú y sonido) y por eso viven arriba; esto lo aplica quien lo pinta.
    ///
    /// La escala no existe como propiedad cuando el panel va con
    /// <c>ScaleWithScreenSize</c> — <c>PanelSettings.scale</c> solo manda en
    /// <c>ConstantPixelSize</c>. El camino estándar es dividir la resolución de
    /// referencia: con 1920×1080 y escala 1,25, la interfaz se dibuja como si la
    /// pantalla midiera 1536×864, y todo —layout incluido— sale un 25 % más
    /// grande. Es justo lo que necesita quien no lee una fuente de 9 px sin
    /// tener que rediseñar el hotbar.
    /// </remarks>
    public static class DisplayPrefs
    {
        private const string ModeKey   = "nimbo.pantalla.modo";
        private const string WidthKey  = "nimbo.pantalla.ancho";
        private const string HeightKey = "nimbo.pantalla.alto";
        private const string ScaleKey  = "nimbo.pantalla.escala";

        /// <summary>Por debajo de 0,75 la interfaz deja de caber en sí misma.</summary>
        public const float MinScale = 0.75f;

        /// <summary>Dos veces es el techo: más allá se pierde el contexto de pantalla.</summary>
        public const float MaxScale = 2f;

        public const float DefaultScale = 1f;

        /// <summary>
        /// Sin borde: parece pantalla completa pero respeta Alt-Tab y las
        /// superposiciones del sistema sin pelearse con ellas.
        /// </summary>
        public const FullScreenMode DefaultMode = FullScreenMode.FullScreenWindow;

        // La referencia original de cada PanelSettings, capturada ANTES de tocarla.
        // Si se dividiera la referencia ya dividida, dos aplicaciones seguidas
        // compondrían escalas (1,25 y luego 1,5 darían 1,875): la base tiene que
        // seguir siendo siempre la que escribió SceneBuilder en el activo.
        private static readonly Dictionary<PanelSettings, Vector2Int> ReferenceBase = new();

        private static bool _sessionApplied;

        public static FullScreenMode Mode =>
            (FullScreenMode)PlayerPrefs.GetInt(ModeKey, (int)DefaultMode);

        /// <summary>0 significa «nunca se guardó»: entonces no se toca la del sistema.</summary>
        public static int Width => PlayerPrefs.GetInt(WidthKey, 0);

        public static int Height => PlayerPrefs.GetInt(HeightKey, 0);

        public static float Scale =>
            Mathf.Clamp(PlayerPrefs.GetFloat(ScaleKey, DefaultScale), MinScale, MaxScale);

        /// <summary>Guarda y aplica. La resolución cambia al momento, como un volumen.</summary>
        public static void Set(int width, int height, FullScreenMode mode)
        {
            if (width <= 0 || height <= 0) return;

            PlayerPrefs.SetInt(WidthKey, width);
            PlayerPrefs.SetInt(HeightKey, height);
            PlayerPrefs.SetInt(ModeKey, (int)mode);

            // En editor sin play no hay ventana de juego que mover, y un test que
            // redimensiona la ventana del editor es un test que molesta a todos.
            if (Application.isPlaying)
                Screen.SetResolution(width, height, mode);
        }

        public static void SetScale(float scale)
        {
            scale = Mathf.Clamp(scale, MinScale, MaxScale);
            PlayerPrefs.SetFloat(ScaleKey, scale);
            ApplyUiScale(ResolvePanelSettings());
        }

        /// <summary>Escribe a disco. Se llama al salir del panel, no en cada arrastre.</summary>
        public static void Flush() => PlayerPrefs.Save();

        /// <summary>
        /// Aplica lo guardado una vez por sesión. Lo llama el panel de opciones al
        /// construirse —y el menú lo construye en cuanto arranca—, así que los
        /// ajustes de la sesión anterior mandan desde el principio sin tocar ni el
        /// arranque ni la escena: el enchufe vive aquí dentro y nadie más tiene
        /// que acordarse de llamarlo.
        /// </summary>
        public static void ApplySavedOnce()
        {
            if (_sessionApplied || !Application.isPlaying) return;
            _sessionApplied = true;

            int w = Width, h = Height;
            if (w > 0 && h > 0) Screen.SetResolution(w, h, Mode);

            ApplyUiScale(ResolvePanelSettings());
        }

        /// <summary>
        /// Deja la referencia del panel dividida por la escala guardada. Público
        /// porque es el punto que se puede comprobar con un PanelSettings de
        /// laboratorio sin montar toda la interfaz.
        /// </summary>
        public static void ApplyUiScale(PanelSettings settings)
        {
            if (settings == null) return;

            if (!ReferenceBase.TryGetValue(settings, out var basis))
            {
                basis = settings.referenceResolution;
                ReferenceBase[settings] = basis;
            }

            float scale = Scale;
            settings.referenceResolution = new Vector2Int(
                Mathf.Max(1, Mathf.RoundToInt(basis.x / scale)),
                Mathf.Max(1, Mathf.RoundToInt(basis.y / scale)));
        }

        /// <summary>
        /// El PanelSettings compartido por menú y juego: SceneBuilder asigna la
        /// misma instancia a los dos UIDocument, así que basta con encontrar
        /// cualquier documento vivo para escalar las dos capas de golpe. Nulo si
        /// no hay ninguno todavía —p. ej. en pruebas de editor— y entonces no se
        /// toca nada.
        /// </summary>
        public static PanelSettings ResolvePanelSettings() =>
            Object.FindFirstObjectByType<UIDocument>(FindObjectsInactive.Include)?.panelSettings;

        /// <summary>
        /// Las resoluciones que ofrece el sistema, cada tamaño una vez y de mayor
        /// a menor. En headless puede no haber lista: ahí solo se ofrece la
        /// actual, que es la única promesa que se puede cumplir.
        /// </summary>
        public static List<Vector2Int> AvailableResolutions()
        {
            var sizes = new List<Vector2Int>();
            foreach (var r in Screen.resolutions)
            {
                var size = new Vector2Int(r.width, r.height);
                if (!sizes.Contains(size)) sizes.Add(size);
            }

            if (sizes.Count == 0)
            {
                var current = Screen.currentResolution;
                sizes.Add(current.width > 0
                    ? new Vector2Int(current.width, current.height)
                    : new Vector2Int(1280, 720));
            }

            sizes.Sort((a, b) => (b.x * b.y).CompareTo(a.x * a.y));
            return sizes;
        }

        public static void ResetToDefaults()
        {
            // «Por defecto» para resolución solo puede ser lo que el sistema
            // eligió; en headless no hay tal cosa y se cae a un valor honesto.
            var native = Screen.currentResolution;
            Set(native.width > 0 ? native.width : 1280,
                native.height > 0 ? native.height : 720,
                DefaultMode);
            SetScale(DefaultScale);
            Flush();
        }
    }
}
