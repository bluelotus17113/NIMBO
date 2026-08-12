using System.Collections;
using System.IO;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Mira la casa por dentro y guarda las fotos en <c>Capturas/</c>.
    /// </summary>
    /// <remarks>
    /// No prueba nada: enseña. Está aquí porque treinta pruebas en verde decían que
    /// entrar en casa funcionaba y la primera foto salió con la pantalla llena de
    /// césped — la habitación se montaba y la cámara se quedaba fuera. Lo que no se
    /// mira no se ve, y una prueba solo mira lo que le has dicho que mire.
    ///
    /// <c>[Explicit]</c> para que no corra con las demás: escribe ficheros en el
    /// repositorio y tarda. Se lanza a mano:
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaInterior</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaInterior
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator SacaFotosDeLaCasa()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));
            yield return null;
            yield return null;

            var camera = Camera.main;

            EventBus.Publish(new InteriorEntered("", "Tu casa"));
            for (int i = 0; i < 6; i++) yield return null;

            var interior = Object.FindFirstObjectByType<Art.World.InteriorView>();
            var room = interior.CurrentRoom;

            // ── 1. Cómo se veía antes ────────────────────────────────────────
            // La cámara con el suelo clavado en el prado: tres metros sobre el
            // césped, mirando a un cuarto que está medio kilómetro más abajo.
            var pivot = interior.RoomOrigin + new Vector3(
                room.Width * Art.World.InteriorView.Tile * 0.5f, 1.2f,
                room.Height * Art.World.InteriorView.Tile * 0.5f);

            var wasPosition = camera.transform.position;
            var wasRotation = camera.transform.rotation;
            var island = camera.GetComponent<Art.CameraWork.IslandCamera>();
            island.enabled = false;

            var vieja = new Vector3(pivot.x, 3f, pivot.z - 10f);
            camera.transform.SetPositionAndRotation(vieja,
                Quaternion.LookRotation((pivot - vieja).normalized, Vector3.up));
            yield return Foto(camera, "interior_antes.png");

            camera.transform.SetPositionAndRotation(wasPosition, wasRotation);
            island.enabled = true;
            yield return null;

            // ── 2. Amueblada ─────────────────────────────────────────────────
            EventBus.Publish(new FurnishModeChanged(true));
            yield return null;

            var furnish = Object.FindFirstObjectByType<Art.World.FurnishModeView>();
            Coloca(furnish, "furn_mesita_de_noche", 3, 4);
            Coloca(furnish, "furn_silla_de_madera_sencilla", 2, 4);
            Coloca(furnish, "furn_maceta_de_girasol_radiante", 6, 6);
            Coloca(furnish, "furn_vela_infinita_nimba", 3, 4);
            yield return null;

            // La rejilla y la vista cenital del modo amueblar.
            yield return Foto(camera, "amueblar_rejilla.png");

            // Y cómo queda al salir del modo, con la cámara de estar dentro.
            EventBus.Publish(new FurnishModeChanged(false));
            for (int i = 0; i < 6; i++) yield return null;
            yield return Foto(camera, "interior_ahora.png");

            Assert.Pass();
        }

        private static void Coloca(Art.World.FurnishModeView furnish, string id, int x, int y)
        {
            EventBus.Publish(new FurnishSelectionChanged(id));
            var error = furnish.TryPlaceAt(x, y);
            Debug.Log($"[captura] {id} en ({x},{y}) → {error}");
        }

        private static IEnumerator Foto(Camera camera, string nombre)
        {
            const int Ancho = 1280;
            const int Alto = 720;

            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            var anterior = camera.targetTexture;
            camera.targetTexture = rt;

            // Nada de WaitForEndOfFrame: en batchmode ese punto del bucle no llega
            // nunca y la corrutina se queda esperando para siempre.
            yield return null;
            camera.Render();

            var texture = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            var activa = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            texture.Apply();
            RenderTexture.active = activa;

            camera.targetTexture = anterior;

            Directory.CreateDirectory(Salida);
            File.WriteAllBytes(Path.Combine(Salida, nombre), texture.EncodeToPNG());
            Debug.Log($"[captura] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
