using System.Collections;
using System.Collections.Generic;
using System.IO;
using Nimbo.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Retrata cada edificio de la aldea desde su puerta, en la isla de verdad.
    /// </summary>
    /// <remarks>
    /// Las otras herramientas montan un plató: un prado liso y dos construcciones
    /// puestas a mano mirando a la cámara. Eso sirve para ver una puerta de cerca y
    /// engaña para todo lo demás, porque en la isla cada zona se gira para mirar al
    /// centro y sus fachadas acaban apuntando a los cuatro vientos. Del plató salió
    /// la conclusión de que las fachadas estaban siempre a contraluz, y era falso:
    /// lo estaban las del plató.
    ///
    /// Aquí la cámara se planta delante de cada puerta —en la dirección a la que
    /// mira la fachada— para ver cómo le da el sol a cada una dando la vuelta.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaAldea</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaAldea
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator RetrataLaAldea()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));
            for (int i = 0; i < 4; i++) yield return null;

            var camera = Camera.main;
            var island = camera.GetComponent<Art.CameraWork.IslandCamera>();
            island.enabled = false;

            var zonas = Zonas();
            Assert.That(zonas, Is.Not.Empty, "la isla no ha levantado ningún edificio");

            for (int i = 0; i < zonas.Count && i < 6; i++)
            {
                var zona = zonas[i];

                // Delante de la puerta: la fachada es el +z local de la zona, así que
                // plantarse ahí es plantarse donde se planta quien va a llamar.
                var mira = zona.position + Vector3.up * 2.2f;
                var desde = zona.position + zona.forward * 11f + Vector3.up * 5f;

                camera.transform.SetPositionAndRotation(
                    desde, Quaternion.LookRotation((mira - desde).normalized, Vector3.up));

                yield return null;
                yield return Foto(camera, $"aldea_{i}_{zona.name}.png");
            }

            Assert.Pass();
        }

        /// <summary>Las zonas construidas, reconocidas por tener muros.</summary>
        private static List<Transform> Zonas()
        {
            var zonas = new List<Transform>();
            foreach (var filtro in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
            {
                if (filtro.gameObject.name != "muros") continue;

                var zona = filtro.transform.parent;
                if (zona != null && !zonas.Contains(zona)) zonas.Add(zona);
            }

            zonas.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            return zonas;
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
            Debug.Log($"[captura] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
