using System.Collections;
using System.IO;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Art.World;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Retrata los edificios en un estudio vacío, con un vecino al lado por escala.
    /// </summary>
    /// <remarks>
    /// No prueba nada: enseña, igual que <see cref="CapturaInterior"/> y
    /// <see cref="CapturaPersonajes"/>. El muñeco no es decoración — un edificio de
    /// código se dibuja con números y sin nada al lado no hay forma de saber si la
    /// puerta le llega a la barbilla o al ombligo.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaEdificios</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaEdificios
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator RetrataLosEdificios()
        {
            Estudio.Nuevo("estudio_edificios");
            yield return null;

            Luz();
            Suelo();

            // Casa y tienda, que son las dos que se miran de cerca al andar por la
            // aldea. El escenario y el embarcadero se ven de paso.
            Edificio(ZonePurpose.Home, new Vector3(-6.5f, 0f, 0f));
            Edificio(ZonePurpose.Food, new Vector3(6.5f, 0f, 0f));

            // Un vecino delante de cada uno, a tamaño real.
            Vecino(new Vector3(-6.5f, 0f, 4.2f));
            Vecino(new Vector3(6.5f, 0f, 4.0f));

            // La cámara va en +z porque es donde los edificios tienen la fachada: las
            // puertas, las ventanas y el toldo están todos en esa cara. La primera
            // tanda de fotos salió desde -z y retrató dos culos de caja.
            var camera = Camara(new Vector3(0f, 4.2f, 17f), new Vector3(0f, 3.2f, 0f));
            yield return null;
            yield return Foto(camera, "edificios_frente.png", 1400, 700);

            // Y de tres cuartos, que es como se ven de verdad desde la cámara del
            // juego: nunca de frente perfecto.
            Mira(camera, new Vector3(13f, 6.5f, 14f), new Vector3(0f, 2.6f, 0f));
            yield return null;
            yield return Foto(camera, "edificios_tres_cuartos.png", 1400, 700);

            Assert.Pass();
        }

        private static void Edificio(ZonePurpose purpose, Vector3 at)
        {
            var meshes = BuildingMeshBuilder.Build(purpose);

            var root = new GameObject($"edificio_{purpose}").transform;
            root.position = at;

            Parte("muros", meshes.Walls, ToonPalette.Solid(new Color32(0xF2, 0xE7, 0xD6, 255)));
            Parte("tejado", meshes.Roof, ToonPalette.Solid(BuildingMeshBuilder.RoofColor(purpose)));
            Parte("detalle", meshes.Trim, ToonPalette.Solid(ToonPalette.TrunkBrown));
            Parte("cristal", meshes.Glass, ToonPalette.Solid(ToonPalette.Glass, 0.35f));

            void Parte(string name, Mesh mesh, Material material)
            {
                if (mesh == null) return;
                var go = new GameObject(name);
                go.transform.SetParent(root, worldPositionStays: false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private static void Vecino(Vector3 at)
        {
            var look = AppearanceData.Default;
            var meshes = ChibiMeshBuilder.Build(look);

            var root = new GameObject("vecino").transform;
            root.SetPositionAndRotation(at, Quaternion.identity);

            Parte("piel", meshes.Skin, ToonPalette.Solid(look.SkinTone));
            Parte("ropa", meshes.Clothes, ToonPalette.Solid(Color.HSVToRGB(0.55f, 0.3f, 0.92f)));
            Parte("pelo", meshes.Hair, ToonPalette.Solid(look.HairColor));

            var face = new ChibiFaceTexture(look, $"vecino_{at.x}");
            face.Draw(Emotion.Happy);
            Parte("cara", meshes.Face, ToonPalette.Textured(face.Texture));

            void Parte(string name, Mesh mesh, Material material)
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, worldPositionStays: false);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                go.AddComponent<MeshRenderer>().sharedMaterial = material;
            }
        }

        private static void Suelo()
        {
            var go = new GameObject("prado");
            go.transform.position = new Vector3(0f, -0.3f, 0f);
            go.AddComponent<MeshFilter>().sharedMesh = MeshShapes.Box(new Vector3(200f, 0.6f, 200f));
            go.AddComponent<MeshRenderer>().sharedMaterial =
                ToonPalette.Solid(new Color(0.55f, 0.68f, 0.38f));
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

        private static void Mira(Camera camera, Vector3 from, Vector3 to)
        {
            camera.transform.position = from;
            camera.transform.rotation = Quaternion.LookRotation((to - from).normalized, Vector3.up);
        }

        private static Camera Camara(Vector3 at, Vector3 mirandoA)
        {
            var go = new GameObject("camara");
            go.transform.SetPositionAndRotation(
                at, Quaternion.LookRotation((mirandoA - at).normalized, Vector3.up));

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.74f, 0.89f, 0.95f);
            camera.fieldOfView = 42f;
            camera.farClipPlane = 400f;
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
