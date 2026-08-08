using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Nimbo.EditorTools
{
    /// <summary>
    /// Crea y asigna el asset de URP. Sin esto, los materiales del juego se ven en
    /// magenta: el shader existe pero no hay pipeline que lo entienda.
    /// </summary>
    /// <remarks>
    /// Va por script y no a mano por lo mismo que la escena: para que clonar el
    /// repositorio y ejecutar un comando deje el proyecto listo, sin una lista de
    /// pasos que alguien tenga que recordar.
    ///
    /// Uso: <c>unity -batchmode -quit -executeMethod Nimbo.EditorTools.RenderPipelineSetup.Setup</c>
    /// </remarks>
    public static class RenderPipelineSetup
    {
        private const string Dir = "Assets/_Project/Settings";
        private const string RendererPath = Dir + "/NimboRenderer.asset";
        private const string PipelinePath = Dir + "/NimboPipeline.asset";

        [MenuItem("Isla Nimbo/Configurar URP")]
        public static void Setup()
        {
            Directory.CreateDirectory(Dir);

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(rendererData);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }

            // Sombras suaves y a media distancia: la cámara mira la isla entera, y
            // sombras de 500 m no se ven pero sí se pagan.
            pipeline.shadowDistance = 160f;
            pipeline.supportsHDR = true;
            pipeline.msaaSampleCount = 4;

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"URP configurado: {PipelinePath}");
        }

        /// <summary>Deja el proyecto listo de una sola vez: pipeline y escena.</summary>
        public static void SetupAll()
        {
            Setup();
            SceneBuilder.Build();
        }
    }
}
