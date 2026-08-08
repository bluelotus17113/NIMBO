using System.IO;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Art.World;
using Nimbo.CharacterCreator;
using Nimbo.CharacterCreator.Appearance;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEditor;
using UnityEngine;

namespace Nimbo.EditorTools
{
    /// <summary>
    /// Renderiza el arte a PNG sin abrir el editor, para poder mirarlo.
    /// </summary>
    /// <remarks>
    /// Un test dice que la malla tiene 2.400 vértices; no dice si el muñeco parece
    /// una persona. Esto último solo se sabe mirándolo, y en un flujo por línea de
    /// órdenes la única forma de mirarlo es sacar la imagen a un fichero.
    ///
    /// Uso: <c>unity -batchmode -quit -executeMethod Nimbo.EditorTools.Screenshotter.Shoot</c>
    /// </remarks>
    public static class Screenshotter
    {
        private const string OutDir = "Capturas";
        private const int Width = 1280;
        private const int Height = 720;

        public static void Shoot()
        {
            Directory.CreateDirectory(OutDir);
            ShootIsland();
            ShootIslanderLineup();
            ShootFaceSheet();
            Debug.Log($"Capturas escritas en {Path.GetFullPath(OutDir)}");
        }

        /// <summary>La isla entera, desde la cámara de juego.</summary>
        private static void ShootIsland()
        {
            var root = new GameObject("captura_isla");
            try
            {
                BuildIslandGeometry(root.transform);

                // Casi a la altura del prado y lejos: es el único encuadre en el que
                // se ve la roca colgando, que es lo que dice «esto flota». Desde
                // arriba la isla parece un campo con un árbol.
                var camera = MakeCamera(new Vector3(0f, 46f, -268f), Quaternion.Euler(11f, 0f, 0f));
                camera.transform.SetParent(root.transform);
                Capture(camera, "isla.png");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void BuildIslandGeometry(Transform parent)
        {
            const float radius = 100f, depth = 62f;

            Add(parent, "prado", IslandMeshBuilder.BuildSurface(radius),
                ToonPalette.Solid(ToonPalette.Grass));
            Add(parent, "roca", IslandMeshBuilder.BuildUnderside(radius, depth),
                ToonPalette.Solid(ToonPalette.Rock));

            var (trunk, crown) = IslandMeshBuilder.BuildTree(26f, 9f);
            Add(parent, "tronco", trunk, ToonPalette.Solid(ToonPalette.TrunkBrown));
            Add(parent, "copa", crown, ToonPalette.Solid(ToonPalette.LeafGreen));

            var cloudMat = ToonPalette.Solid(ToonPalette.CloudWhite, 0.02f);
            var blob = MeshShapes.Sphere(12, 9, new Vector3(1f, 0.55f, 1f));
            var rng = new Rng(11);

            for (int i = 0; i < 26; i++)
            {
                float angle = rng.Range(0f, Mathf.PI * 2f);
                float distance = rng.Range(radius * 0.95f, radius * 1.9f);
                var cloud = Add(parent, $"nube_{i}", blob, cloudMat);
                cloud.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * distance, rng.Range(-depth * 0.85f, -4f),
                    Mathf.Sin(angle) * distance);
                cloud.transform.localScale = Vector3.one * rng.Range(14f, 38f);
            }

            // Unos cuantos habitantes al pie del árbol, para dar escala.
            for (int i = 0; i < 6; i++)
            {
                var appearance = RandomAppearance((uint)(i * 37 + 5));
                var meshes = ChibiMeshBuilder.Build(appearance);
                var person = new GameObject($"habitante_{i}");
                person.transform.SetParent(parent, false);

                float angle = i / 6f * Mathf.PI * 2f;
                person.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 16f, 0f, Mathf.Sin(angle) * 16f);
                person.transform.localScale = Vector3.one * 5.5f;  // escala de maqueta
                person.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);

                AddChibi(person.transform, appearance, meshes, Emotion.Happy);
            }
        }

        /// <summary>Seis habitantes en fila, de cerca: el retrato de familia.</summary>
        private static void ShootIslanderLineup()
        {
            var root = new GameObject("captura_habitantes");
            try
            {
                for (int i = 0; i < 6; i++)
                {
                    var appearance = RandomAppearance((uint)(i * 91 + 13));
                    var meshes = ChibiMeshBuilder.Build(appearance);

                    var person = new GameObject($"habitante_{i}");
                    person.transform.SetParent(root.transform, false);
                    person.transform.localPosition = new Vector3((i - 2.5f) * 0.62f, 0f, 0f);
                    person.transform.localRotation = Quaternion.Euler(0f, (i - 2.5f) * -7f, 0f);

                    AddChibi(person.transform, appearance, meshes,
                             (Emotion)(i % 4 == 0 ? Emotion.Happy :
                                       i % 4 == 1 ? Emotion.Ecstatic :
                                       i % 4 == 2 ? Emotion.Surprised : Emotion.Neutral));
                }

                var camera = MakeCamera(new Vector3(0f, 0.62f, 3.6f),
                                        Quaternion.Euler(4f, 180f, 0f), fov: 42f);
                camera.transform.SetParent(root.transform);
                Capture(camera, "habitantes.png");
            }
            finally { Object.DestroyImmediate(root); }
        }

        /// <summary>Una cara, doce veces, una por emoción. Es la hoja que hay que mirar.</summary>
        private static void ShootFaceSheet()
        {
            const int columns = 4, rows = 3, cell = 128;
            var sheet = new Texture2D(columns * cell, rows * cell, TextureFormat.RGBA32, false);
            var appearance = RandomAppearance(4242);
            var face = new ChibiFaceTexture(appearance, "hoja");

            for (int i = 0; i < 12; i++)
            {
                face.Draw((Emotion)i);
                var pixels = face.Texture.GetPixels32();

                int cx = (i % columns) * cell;
                int cy = (rows - 1 - i / columns) * cell;
                sheet.SetPixels32(cx, cy, cell, cell, pixels);
            }

            sheet.Apply();
            File.WriteAllBytes(Path.Combine(OutDir, "caras.png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(face.Texture);
        }

        private static AppearanceData RandomAppearance(uint seed)
        {
            var rng = new Rng(seed);
            return AppearanceRandomizer.Random(ref rng);
        }

        private static void AddChibi(Transform parent, in AppearanceData appearance,
                                     in ChibiMeshes meshes, Emotion emotion)
        {
            Add(parent, "piel", meshes.Skin, ToonPalette.Solid(appearance.SkinTone));
            Add(parent, "ropa", meshes.Clothes, ToonPalette.Solid(
                Color.HSVToRGB(Random.value, 0.5f, 0.9f)));
            Add(parent, "pelo", meshes.Hair, ToonPalette.Solid(appearance.HairColor));

            var face = new ChibiFaceTexture(appearance, parent.name);
            face.Draw(emotion);
            Add(parent, "cara", meshes.Face, ToonPalette.Textured(face.Texture));
        }

        private static GameObject Add(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
            return go;
        }

        private static Camera MakeCamera(Vector3 position, Quaternion rotation, float fov = 45f)
        {
            var go = new GameObject("camara_captura");
            go.transform.SetPositionAndRotation(position, rotation);

            var camera = go.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color32(0x7E, 0xC8, 0xE3, 255);
            camera.fieldOfView = fov;
            camera.farClipPlane = 900f;

            // Luz propia: en batch no hay iluminación horneada y sin esto sale todo negro.
            var sun = new GameObject("sol_captura");
            sun.transform.SetParent(go.transform);
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.75f;
            light.color = new Color(1f, 0.96f, 0.9f);
            sun.transform.rotation = Quaternion.Euler(42f, -30f, 0f);

            // El ambiente rellena las sombras para que nada quede negro, pero se
            // queda bajo: subido a 0.78 aplanaba a los habitantes y los brazos se
            // leían como paneles pegados al costado en vez de como tubos.
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.58f, 0.66f);
            RenderSettings.ambientIntensity = 1f;

            return camera;
        }

        private static void Capture(Camera camera, string fileName)
        {
            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32)
            {
                antiAliasing = 4,
            };

            camera.targetTexture = target;
            camera.Render();
            camera.Render();

            var previous = RenderTexture.active;
            RenderTexture.active = target;

            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();

            RenderTexture.active = previous;
            camera.targetTexture = null;

            File.WriteAllBytes(Path.Combine(OutDir, fileName), image.EncodeToPNG());

            Object.DestroyImmediate(image);
            target.Release();
            Object.DestroyImmediate(target);
        }
    }
}
