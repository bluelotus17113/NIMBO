using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Nimbo.EditorTools
{
    /// <summary>
    /// Compila el juego a un ejecutable, por línea de órdenes.
    /// </summary>
    /// <remarks>
    /// Uso: <c>unity -batchmode -quit -executeMethod Nimbo.EditorTools.GameBuilder.BuildLinux</c>
    /// </remarks>
    public static class GameBuilder
    {
        private const string OutDir = "Build/Linux";
        private const string Executable = "IslaNimbo";

        public static void BuildLinux()
        {
            Directory.CreateDirectory(OutDir);

            var scenes = EditorBuildSettings.scenes;
            if (scenes.Length == 0)
            {
                Debug.LogError("No hay escenas en el build. Ejecuta SceneBuilder.Build primero.");
                EditorApplication.Exit(1);
                return;
            }

            var paths = new string[scenes.Length];
            for (int i = 0; i < scenes.Length; i++) paths[i] = scenes[i].path;

            PlayerSettings.productName = "Isla Nimbo";
            PlayerSettings.companyName = "Nimbo";

            // Ventana, no pantalla completa: es un juego de mirar de reojo mientras
            // haces otra cosa, no uno que se adueñe del monitor.
            PlayerSettings.defaultScreenWidth = 1280;
            PlayerSettings.defaultScreenHeight = 720;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.runInBackground = true;

            var options = new BuildPlayerOptions
            {
                scenes = paths,
                locationPathName = Path.Combine(OutDir, Executable),
                target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"BUILD OK: {summary.outputPath} " +
                          $"({summary.totalSize / 1024 / 1024} MB, {summary.totalTime.TotalSeconds:0} s)");
            }
            else
            {
                Debug.LogError($"BUILD FALLIDO: {summary.result}, {summary.totalErrors} errores");
                EditorApplication.Exit(1);
            }
        }
    }
}
