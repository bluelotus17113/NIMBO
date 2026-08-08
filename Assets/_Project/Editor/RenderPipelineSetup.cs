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

            EnsureAlwaysIncludedShaders();

            EditorUtility.SetDirty(pipeline);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"URP configurado: {PipelinePath}");
        }

        /// <summary>
        /// Mete los shaders de URP en la lista de «siempre incluidos».
        /// </summary>
        /// <remarks>
        /// Sin esto el juego compila pero revienta al arrancar con un
        /// <c>ArgumentNullException</c> en el primer material. Unity solo empaqueta
        /// los shaders que algún material del proyecto referencia, y aquí **todos los
        /// materiales se crean en tiempo de ejecución**: para el empaquetador, nadie
        /// usa URP/Lit y lo deja fuera. En el editor funciona igual porque el shader
        /// está cargado de todas formas, así que es de los fallos que solo aparecen
        /// en el ejecutable.
        /// </remarks>
        private static void EnsureAlwaysIncludedShaders()
        {
            string[] needed =
            {
                "Universal Render Pipeline/Lit",
                "Universal Render Pipeline/Unlit",
            };

            var graphics = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/GraphicsSettings.asset");
            if (graphics == null || graphics.Length == 0)
            {
                Debug.LogError("No se pudo abrir GraphicsSettings para incluir los shaders");
                return;
            }

            var settings = new SerializedObject(graphics[0]);
            var included = settings.FindProperty("m_AlwaysIncludedShaders");

            foreach (string name in needed)
            {
                var shader = Shader.Find(name);
                if (shader == null)
                {
                    Debug.LogWarning($"No se encontro el shader {name}");
                    continue;
                }

                bool already = false;
                for (int i = 0; i < included.arraySize; i++)
                {
                    if (included.GetArrayElementAtIndex(i).objectReferenceValue == shader)
                    {
                        already = true;
                        break;
                    }
                }
                if (already) continue;

                included.InsertArrayElementAtIndex(included.arraySize);
                included.GetArrayElementAtIndex(included.arraySize - 1).objectReferenceValue = shader;
                Debug.Log($"Shader incluido siempre: {name}");
            }

            settings.ApplyModifiedProperties();
            AssetDatabase.SaveAssets();
        }

        /// <summary>Deja el proyecto listo de una sola vez: pipeline y escena.</summary>
        public static void SetupAll()
        {
            Setup();
            SceneBuilder.Build();
        }
    }
}
