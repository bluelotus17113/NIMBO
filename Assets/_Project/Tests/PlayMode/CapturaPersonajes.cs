using System.Collections;
using System.IO;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Retrata a los muñecos en un estudio vacío, para poder mirarlos de cerca.
    /// </summary>
    /// <remarks>
    /// No prueba nada: enseña, igual que <see cref="CapturaInterior"/>. Monta una
    /// escena limpia con luz y fondo liso y alinea varios habitantes, porque en la
    /// isla se ven a quince metros y desde arriba, que es justo la distancia a la que
    /// no se distingue si una mano está donde debe.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaPersonajes</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaPersonajes
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator RetrataALosMunecos()
        {
            // Escena nueva y en blanco: nada de cargar «Isla», que traería el prado,
            // los vecinos y su cámara. Aquí lo que se quiere es el muñeco a solas.
            SceneManager.SetActiveScene(SceneManager.CreateScene("estudio"));
            yield return null;

            Luz();
            Suelo();

            var camera = Camara(new Vector3(0f, 0.62f, -4.4f), Quaternion.Euler(2f, 0f, 0f));

            // Seis muñecos con aspectos bien distintos: si algo está mal en una malla
            // generada, casi siempre se ve solo en un extremo del rango.
            //
            // Girados 180°: el muñeco mira hacia +z, y la cámara está en -z mirando
            // hacia +z. Sin darles la vuelta se retrata a seis nucas.
            for (int i = 0; i < 6; i++)
            {
                var look = Aspecto(i);
                var body = Muneco(look, new Vector3(-2.25f + i * 0.9f, 0f, 0f), 180f);
                body.name = $"muneco_{i}";
            }

            yield return null;
            yield return Foto(camera, "personajes_frente.png", 1400, 640);

            // Y de lado, que es donde se ve si el brazo cuelga del hombro o está
            // pegado al costado, y si la cabeza se apoya en algo o flota.
            camera.transform.SetPositionAndRotation(new Vector3(3.4f, 0.62f, -2.0f),
                                                    Quaternion.Euler(2f, -60f, 0f));
            yield return null;
            yield return Foto(camera, "personajes_perfil.png", 1400, 640);

            Assert.Pass();
        }

        private static AppearanceData Aspecto(int i)
        {
            var look = AppearanceData.Default;
            look.HairStyle = i * 6 % HairStyles.Count;
            look.BodyHeight = i / 5f;
            look.BodyBuild = (5 - i) / 5f;
            look.HeadWidth = i % 2 == 0 ? 0.2f : 0.85f;
            look.HeadHeight = i % 3 == 0 ? 0.25f : 0.8f;
            look.SkinTone = Color.HSVToRGB(0.07f, 0.18f + i * 0.09f, 0.95f - i * 0.09f);
            look.HairColor = Color.HSVToRGB(i / 6f, 0.45f, 0.6f);
            return look;
        }

        private static Transform Muneco(in AppearanceData look, Vector3 at, float yaw)
        {
            var meshes = ChibiMeshBuilder.Build(look);

            var root = new GameObject("muneco").transform;
            root.SetPositionAndRotation(at, Quaternion.Euler(0f, yaw, 0f));

            Parte("piel", meshes.Skin, ToonPalette.Solid(look.SkinTone));
            Parte("ropa", meshes.Clothes, ToonPalette.Solid(Color.HSVToRGB(0.55f, 0.3f, 0.92f)));
            Parte("pelo", meshes.Hair, ToonPalette.Solid(look.HairColor));

            var face = new ChibiFaceTexture(look, $"retrato_{at.x}");
            face.Draw(Emotion.Happy);
            Parte("cara", meshes.Face, ToonPalette.Textured(face.Texture));

            return root;

            void Parte(string name, Mesh mesh, Material material)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, worldPositionStays: false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        /// <summary>Un suelo liso: sin él no hay sombra, y sin sombra no se ve si el pie apoya.</summary>
        private static void Suelo()
        {
            var go = new GameObject("suelo");
            go.transform.position = new Vector3(0f, -0.06f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = MeshShapes.Box(new Vector3(20f, 0.12f, 20f));
            go.AddComponent<MeshRenderer>().sharedMaterial =
                ToonPalette.Solid(new Color(0.90f, 0.92f, 0.94f));
        }

        private static void Luz()
        {
            var go = new GameObject("sol");
            var light = go.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.96f, 0.88f);
            light.intensity = 1.35f;
            light.shadows = LightShadows.Soft;
            go.transform.rotation = Quaternion.Euler(48f, -35f, 0f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.81f, 0.93f);
            RenderSettings.ambientEquatorColor = new Color(0.74f, 0.72f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.52f, 0.50f, 0.42f);
        }

        private static Camera Camara(Vector3 at, Quaternion looking)
        {
            var go = new GameObject("camara");
            go.transform.SetPositionAndRotation(at, looking);

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.80f, 0.86f, 0.90f);
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.05f;
            go.tag = "MainCamera";
            return camera;
        }

        private static IEnumerator Foto(Camera camera, string nombre, int ancho, int alto)
        {
            var rt = new RenderTexture(ancho, alto, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;

            // Nada de WaitForEndOfFrame: en batchmode ese punto del bucle no llega.
            yield return null;
            camera.Render();

            var texture = new Texture2D(ancho, alto, TextureFormat.RGB24, false);
            var activa = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, ancho, alto), 0, 0);
            texture.Apply();
            RenderTexture.active = activa;

            camera.targetTexture = null;

            Directory.CreateDirectory(Salida);
            File.WriteAllBytes(Path.Combine(Salida, nombre), texture.EncodeToPNG());
            Debug.Log($"[captura] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
