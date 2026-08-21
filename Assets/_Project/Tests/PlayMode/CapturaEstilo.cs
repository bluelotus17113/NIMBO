using System.Collections;
using System.IO;
using Nimbo.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Los mismos cuatro encuadres, siempre. Es la vara de medir del estilo.
    /// </summary>
    /// <remarks>
    /// Un lavado de cara solo se juzga comparando, y comparar exige que la cámara esté
    /// en el mismo sitio las dos veces: dos fotos parecidas desde ángulos distintos no
    /// dicen nada. Por eso las posiciones van escritas y no salen de buscar objetos por
    /// la escena, que cambiarían al cambiar el arte.
    ///
    /// El sufijo lo pone quien la lanza, para no pisar la foto anterior:
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaEstilo</c>
    /// con <c>NIMBO_ESTILO=antes</c> o <c>ahora</c> en el entorno.
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaEstilo
    {
        private const string Salida = "Capturas";

        /// <summary>Dónde se pone la cámara y a dónde mira, por nombre de foto.</summary>
        private static readonly (string nombre, Vector3 desde, Vector3 mira)[] Encuadres =
        {
            // La isla entera desde el sureste alto: el bulto, el borde y la copa grande.
            ("panorama", new Vector3(92f, 58f, -108f), new Vector3(0f, 6f, -18f)),

            // A ras de prado mirando al borde. Es el encuadre que enseña la hierba:
            // desde arriba el césped es una alfombra verde y no se ve una brizna.
            ("prado", new Vector3(34f, 1.9f, 30f), new Vector3(76f, 3.2f, 66f)),

            // El Árbol Nimbo entero, desde el sur y a media altura.
            ("arbol", new Vector3(0f, 13f, -38f), new Vector3(0f, 17f, 0f)),

            // El linde del noroeste, donde caen los nodos de recoger: árboles pequeños,
            // rocas y matas juntos en un mismo plano.
            ("linde", new Vector3(-44f, 7f, 40f), new Vector3(-62f, 2.5f, 58f)),
        };

        [UnityTest]
        public IEnumerator RetrataElEstilo()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            // La hierba y los nodos se siembran un fotograma después de cargar, y el
            // viento necesita unos cuantos más para no salir todo clavado en el frame
            // cero. Con dos fotogramas el prado salía a medio sembrar.
            for (int i = 0; i < 12; i++) yield return null;

            var camera = Camera.main;
            Assert.IsNotNull(camera, "la escena Isla no trae cámara");
            var seguimiento = camera.GetComponent<Art.CameraWork.IslandCamera>();
            if (seguimiento != null) seguimiento.enabled = false;

            string sufijo = System.Environment.GetEnvironmentVariable("NIMBO_ESTILO");
            if (string.IsNullOrEmpty(sufijo)) sufijo = "estilo";

            foreach (var (nombre, desde, mira) in Encuadres)
            {
                camera.transform.SetPositionAndRotation(
                    desde, Quaternion.LookRotation((mira - desde).normalized, Vector3.up));

                yield return null;
                yield return Foto(camera, $"estilo_{nombre}_{sufijo}.png");
            }

            Assert.Pass();
        }

        private static IEnumerator Foto(Camera camera, string nombre)
        {
            const int Ancho = 1280, Alto = 720;

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
            Debug.Log($"[estilo] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
