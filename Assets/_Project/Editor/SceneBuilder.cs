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
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
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
        private const string VolumePath = SettingsDir + "/NimboVolume.asset";
        private const string ScenePath = ScenesDir + "/Isla.unity";

        [MenuItem("Isla Nimbo/Reconstruir escena")]
        public static void Build()
        {
            Directory.CreateDirectory(SettingsDir);
            Directory.CreateDirectory(ScenesDir);

            var panelSettings = BuildPanelSettings();
            var volumeProfile = BuildVolumeProfile();
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            BuildCamera();
            BuildLighting();
            BuildGrade(volumeProfile);
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

            // Sin esto la cámara se salta el revelado entero y da igual lo que diga
            // el volumen: el mapeo de tonos, el color y el velo no llegan a aplicarse.
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            go.AddComponent<AudioListener>();

            // La posición de arriba es solo la del primer fotograma, antes de que
            // cargue nada. En cuanto hay partida manda esto: orbita, acerca y se va
            // a mirar de cerca al habitante cuya ficha abras.
            go.AddComponent<IslandCamera>();
        }

        /// <summary>
        /// El revelado: cómo se convierte la luz calculada en los colores que salen
        /// por pantalla.
        /// </summary>
        /// <remarks>
        /// Hasta ahora no había ninguno, y por eso una pared crema al sol saturaba a
        /// blanco puro: sin curva de revelado, todo lo que pasa de uno se recorta.
        /// El mapeo neutro dobla esa parte alta en vez de cortarla, que es lo que
        /// deja ver una ventana en una fachada iluminada.
        ///
        /// El resto son las tres cosas que separan una tarde amable de una foto:
        /// blancos algo cálidos, color un punto más vivo, y un velo de luz en lo que
        /// más brilla. Todo flojo — en cuanto se nota, deja de parecer un juguete y
        /// empieza a parecer un filtro.
        /// </remarks>
        private static VolumeProfile BuildVolumeProfile()
        {
            AssetDatabase.DeleteAsset(VolumePath);

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, VolumePath);

            var tonemapping = profile.Add<Tonemapping>();
            tonemapping.mode.overrideState = true;
            tonemapping.mode.value = TonemappingMode.Neutral;

            var white = profile.Add<WhiteBalance>();
            white.temperature.overrideState = true;
            white.temperature.value = 8f;

            var color = profile.Add<ColorAdjustments>();
            color.saturation.overrideState = true;
            color.saturation.value = 14f;
            color.contrast.overrideState = true;
            color.contrast.value = 2f;
            color.postExposure.overrideState = true;
            color.postExposure.value = 0.2f;

            // Aquí NO se tiñe la sombra. Lo hace el shader, y hacerlo también en el
            // revelado lo aplicaba dos veces: la mitad de la aldea que no daba al sol
            // se veía de cemento violáceo. El color de la sombra se decide en un solo
            // sitio, y es el que sabe qué está iluminado y qué no.
            var bloom = profile.Add<Bloom>();
            bloom.threshold.overrideState = true;
            bloom.threshold.value = 1.05f;
            bloom.intensity.overrideState = true;
            bloom.intensity.value = 0.55f;
            bloom.scatter.overrideState = true;
            bloom.scatter.value = 0.72f;

            EditorUtility.SetDirty(profile);
            return profile;
        }

        private static void BuildGrade(VolumeProfile profile)
        {
            var go = new GameObject("Revelado");
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 0f;
            volume.sharedProfile = profile;
        }

        private static void BuildLighting()
        {
            // Los números viven en IslandLighting, que es de donde los sacan también
            // las herramientas de captura: el plató tiene que alumbrar como la isla.
            var go = new GameObject("Sol");
            IslandLighting.Apply(go.AddComponent<Light>());
        }

        private static void BuildGame(PanelSettings panelSettings)
        {
            var go = new GameObject("Isla Nimbo");
            go.AddComponent<GameBootstrap>();

            var worldGo = new GameObject("Mundo");
            worldGo.transform.SetParent(go.transform);
            worldGo.AddComponent<WorldView>();
            worldGo.AddComponent<FarmView>();
            worldGo.AddComponent<GatheringView>();
            worldGo.AddComponent<PlayerHomeView>();
            worldGo.AddComponent<BuildModeView>();
            worldGo.AddComponent<InteriorView>();
            worldGo.AddComponent<FurnishModeView>();

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
