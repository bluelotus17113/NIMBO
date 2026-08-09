using System.IO;
using Nimbo.Art.Audio;
using Nimbo.Art.CameraWork;
using Nimbo.Art.World;
using Nimbo.Game.Bootstrap;
using Nimbo.UI;
using Nimbo.UI.Menu;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Nimbo.EditorTools
{
    /// <summary>
    /// Construye la escena jugable desde cero, por línea de órdenes.
    /// </summary>
    /// <remarks>
    /// La escena es un fichero binario-ish que dos personas no pueden fusionar. Al
    /// generarla con un script, la fuente de la verdad vuelve a ser código: se borra,
    /// se regenera y siempre sale igual. Es lo mismo que se hace con los catálogos,
    /// y por el mismo motivo.
    ///
    /// Uso: <c>unity -batchmode -quit -executeMethod Nimbo.EditorTools.SceneBuilder.Build</c>
    /// </remarks>
    public static class SceneBuilder
    {
        private const string SettingsDir = "Assets/_Project/Settings";
        private const string ScenesDir = "Assets/_Project/Scenes";
        private const string ThemePath = SettingsDir + "/NimboRuntimeTheme.tss";
        private const string PanelPath = SettingsDir + "/NimboPanelSettings.asset";
        private const string ScenePath = ScenesDir + "/Isla.unity";

        [MenuItem("Isla Nimbo/Reconstruir escena")]
        public static void Build()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(ScenesDir);

            var panelSettings = BuildPanelSettings();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildLighting();
            BuildGame(panelSettings);

            EditorSceneManager.SaveScene(scene, ScenePath);
            RegisterInBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"Escena reconstruida en {ScenePath}");
        }

        private static PanelSettings BuildPanelSettings()
        {
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(PanelPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings, PanelPath);
            }

            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            if (theme == null)
                Debug.LogError($"No se encontró el tema en {ThemePath}: la interfaz saldrá sin estilo");
            else
                settings.themeStyleSheet = theme;

            // Escala con la altura: la interfaz se diseñó a 1080 y en 1440 tiene que
            // verse igual de grande, no más pequeña.
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920, 1080);
            settings.match = 1f;

            EditorUtility.SetDirty(settings);
            return settings;
        }

        private static void BuildCamera()
        {
            var go = new GameObject("Cámara de la isla");
            var camera = go.AddComponent<Camera>();

            // Vista cenital inclinada, como quien mira una maqueta: es la cámara que
            // pide un juego de observar, no de manejar.
            go.transform.position = new Vector3(0f, 62f, -78f);
            go.transform.rotation = Quaternion.Euler(38f, 0f, 0f);

            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = UiTheme.Sky;
            camera.fieldOfView = 45f;
            camera.farClipPlane = 600f;
            go.tag = "MainCamera";

            go.AddComponent<AudioListener>();

            // La posición de arriba es solo la del primer fotograma, antes de que
            // cargue nada. En cuanto hay partida manda esto: orbita, acerca y se va
            // a mirar de cerca al habitante cuya ficha abras.
            go.AddComponent<IslandCamera>();
        }

        private static void BuildLighting()
        {
            var go = new GameObject("Sol");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.80f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.58f, 0.66f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.40f, 0.42f);
        }

        private static void BuildGame(PanelSettings panelSettings)
        {
            var go = new GameObject("Isla Nimbo");
            go.AddComponent<GameBootstrap>();

            var worldGo = new GameObject("Mundo");
            worldGo.transform.SetParent(go.transform);
            worldGo.AddComponent<WorldView>();

            var audioGo = new GameObject("Sonido");
            audioGo.transform.SetParent(go.transform);
            audioGo.AddComponent<AudioSource>();
            audioGo.AddComponent<AudioDirector>();

            var uiGo = new GameObject("Interfaz");
            uiGo.transform.SetParent(go.transform);

            var document = uiGo.AddComponent<UIDocument>();
            document.panelSettings = panelSettings;
            uiGo.AddComponent<UiRoot>();

            // El menú y el control de flujo cuelgan de la raíz de la escena, NO del
            // objeto persistente: volver al menú recarga la escena, y lo que tiene
            // que morir en esa recarga es justo esto, para renacer limpio.
            var menuGo = new GameObject("Menú");
            var menuDocument = menuGo.AddComponent<UIDocument>();
            menuDocument.panelSettings = panelSettings;

            // Por encima de la interfaz de la partida. Con el mismo orden, cuál se
            // dibuja delante depende del orden de creación, que es justo la clase de
            // detalle que se rompe solo el día que alguien reordene el método.
            menuDocument.sortingOrder = 10;
            menuGo.AddComponent<MainMenuView>();

            var flowGo = new GameObject("Flujo");
            flowGo.AddComponent<FlowController>();
        }

        private static void RegisterInBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes;
            foreach (var entry in scenes)
                if (entry.path == ScenePath) return;

            var updated = new EditorBuildSettingsScene[scenes.Length + 1];
            scenes.CopyTo(updated, 0);
            updated[scenes.Length] = new EditorBuildSettingsScene(ScenePath, true);
            EditorBuildSettings.scenes = updated;
        }
    }
}
