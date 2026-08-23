using System.Collections;
using System.Collections.Generic;
using System.IO;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Retrata el huerto con los doce cultivos del catálogo, maduros, en la isla de
    /// verdad. Doce siluetas se discuten mirando: sin foto esto no se puede juzgar.
    /// </summary>
    /// <remarks>
    /// No monta un huerto falso: labra, siembra, riega y pasa los días por el
    /// servicio de farming dentro de la escena Isla, así que lo que sale en la foto
    /// es exactamente lo que vería el jugador —vista, eventos y reconstrucción de
    /// casillas incluidos—.
    ///
    /// El guardado ya lo desvía <c>AislarGuardadoEnPruebasDeJuego</c> a una carpeta
    /// temporal antes de que corra cualquier prueba de este ensamblado.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaHuerto</c>
    /// con <c>NIMBO_HUERTO=antes</c> o <c>ahora</c> en el entorno.
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaHuerto
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator RetrataLosDoce()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            // El huerto se levanta cuando el arranque publica GameLoaded; hasta
            // entonces no hay dónde dibujar. Si no aparece, FarmView está
            // desenchufado y esta foto no tendría nada que retratar.
            yield return EsperarHuerto();

            var farm = ServiceRegistry.Get<IFarmingService>();
            var inventory = ServiceRegistry.Get<IInventoryService>();

            // Las doce casillas del centro: son justo las que se pueden trabajar
            // desde Cultivo 1 (FarmPlot.UsableSize da 4×3 centrado en la parcela
            // de 8×6). Fila a fila y en orden de catálogo, para que la foto se
            // lea igual que el JSON.
            var sembradas = new List<(string nombre, int x, int y)>(12);
            int i = 0;
            foreach (var crop in farm.Crops)
            {
                inventory.TryStore(crop.SeedId, 1, out _);

                int x = 2 + i % 4;
                int y = 1 + i / 4;

                Assert.AreEqual(FarmError.Ok, farm.Till(x, y), $"labrar ({x},{y}) falló");
                Assert.AreEqual(FarmError.Ok, farm.Plant(x, y, crop.SeedId),
                                $"sembrar {crop.DisplayName} en ({x},{y}) falló");

                sembradas.Add((crop.DisplayName, x, y));
                i++;
            }
            Assert.AreEqual(12, sembradas.Count, "el catálogo no tiene doce cultivos");

            // Regar y pasar días hasta que todo esté maduro. El cultivo más lento
            // del catálogo tarda ocho días; con doce intentos sobra.
            for (int dia = 0; dia < 12 && Pendiente(farm, sembradas); dia++)
            {
                foreach (var (_, x, y) in sembradas) farm.Water(x, y);
                farm.AdvanceDay();
            }

            foreach (var (nombre, x, y) in sembradas)
                Assert.AreEqual(TileState.Ready, farm.TileAt(x, y).State,
                                $"{nombre} no maduró en doce días");

            // La reconstrucción de casillas va por evento y es síncrona, pero el
            // primer render necesita sus fotogramas.
            for (int frame = 0; frame < 4; frame++) yield return null;

            var camera = Camera.main;
            Assert.IsNotNull(camera, "la escena Isla no trae cámara");
            var seguimiento = camera.GetComponent<Art.CameraWork.IslandCamera>();
            if (seguimiento != null) seguimiento.enabled = false;

            // El bloque plantado ocupa las casillas (2..5, 1..3): centro en
            // (-10, -168.8), unas seis casillas y media de ancho.
            var centro = new Vector3(Data.Farming.FarmPlot.CentreX, 0.2f,
                                     Data.Farming.FarmPlot.CentreZ - 0.8f);

            // Plano general: las doce, cada una reconocible de un vistazo.
            Encuadre(camera, centro + new Vector3(7.5f, 5.2f, 8.6f), centro);
            yield return Foto(camera, Nombre("huerto_doce"));

            // De cerca, a la altura a la que se juega: aquí se ve si una seta
            // parece una seta o una bola sobre un palo.
            Encuadre(camera, centro + new Vector3(3.6f, 1.5f, 4.4f),
                     centro + Vector3.up * 0.35f);
            yield return Foto(camera, Nombre("huerto_cerca"));

            for (int fila = 0; fila < 3; fila++)
                Debug.Log($"[huerto] fila {fila}: {sembradas[fila * 4].nombre}, " +
                          $"{sembradas[fila * 4 + 1].nombre}, " +
                          $"{sembradas[fila * 4 + 2].nombre}, " +
                          $"{sembradas[fila * 4 + 3].nombre}");

            Assert.Pass();
        }

        private static IEnumerator EsperarHuerto()
        {
            for (int frame = 0; frame < 600; frame++)
            {
                if (GameObject.Find("Huerto") != null) yield break;
                yield return null;
            }
            Assert.Fail("FarmView no levantó el huerto en la escena Isla");
        }

        private static bool Pendiente(IFarmingService farm,
                                      List<(string nombre, int x, int y)> sembradas)
        {
            foreach (var (_, x, y) in sembradas)
                if (farm.TileAt(x, y).State == TileState.Planted) return true;
            return false;
        }

        private static string Nombre(string baseName)
        {
            string sufijo = System.Environment.GetEnvironmentVariable("NIMBO_HUERTO");
            if (string.IsNullOrEmpty(sufijo)) sufijo = "ahora";
            return $"{baseName}_{sufijo}.png";
        }

        private static void Encuadre(Camera camera, Vector3 desde, Vector3 mira)
        {
            camera.transform.SetPositionAndRotation(
                desde, Quaternion.LookRotation((mira - desde).normalized, Vector3.up));
        }

        private static IEnumerator Foto(Camera camera, string nombre)
        {
            const int Ancho = 1200, Alto = 700;

            var rt = new RenderTexture(Ancho, Alto, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = rt;

            // Nada de WaitForEndOfFrame: en batchmode ese punto del bucle no llega.
            yield return null;
            camera.Render();

            var texture = new Texture2D(Ancho, Alto, TextureFormat.RGB24, false);
            var activa = RenderTexture.active;
            RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, Ancho, Alto), 0, 0);
            texture.Apply();
            RenderTexture.active = activa;

            camera.targetTexture = null;

            Directory.CreateDirectory(Salida);
            File.WriteAllBytes(Path.Combine(Salida, nombre), texture.EncodeToPNG());
            Debug.Log($"[huerto] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
