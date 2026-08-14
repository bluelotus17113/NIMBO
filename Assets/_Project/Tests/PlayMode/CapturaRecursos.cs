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
    /// Retrata lo que hay por la isla para recoger, en la isla de verdad.
    /// </summary>
    /// <remarks>
    /// Los ciento veinte nodos existieron durante meses sin que los dibujara nadie, y
    /// ninguna prueba lo vio: todas comprobaban que el servicio los sembraba, los
    /// agotaba y los reponía, y todas pasaban. Lo que faltaba no era lógica, era
    /// mirar.
    ///
    /// <c>unity -runTests -testPlatform PlayMode -testFilter CapturaRecursos</c>
    /// </remarks>
    [Explicit("Herramienta de capturas, no una prueba. Se lanza con -testFilter.")]
    public class CapturaRecursos
    {
        private const string Salida = "Capturas";

        [UnityTest]
        public IEnumerator RetrataLosRecursos()
        {
            yield return Aldea.Cargar();

            var camera = Camera.main;
            camera.GetComponent<Art.CameraWork.IslandCamera>().enabled = false;

            // De lejos: la isla entera, para ver si el prado sigue pelado o tiene
            // arboleda. Es la foto que responde a la pregunta de un vistazo.
            Encuadre(camera, new Vector3(0f, 70f, -120f), new Vector3(0f, 0f, -10f));
            yield return Foto(camera, "recursos_isla.png");

            // De cerca, a la altura a la que se juega: aquí es donde se ve si un roble
            // parece un roble o una bola verde sobre un palo.
            var nodo = PrimerNodo();
            Assert.That(nodo, Is.Not.Null, "no hay ni un cuerpo de recurso en la isla");

            Encuadre(camera, nodo.position + new Vector3(6f, 4.5f, 6f),
                     nodo.position + Vector3.up * 1.6f);
            yield return Foto(camera, "recursos_cerca.png");

            Assert.Pass();
        }

        private static Transform PrimerNodo()
        {
            if (!ServiceRegistry.TryGet<IGatheringService>(out var gathering)) return null;

            var raiz = GameObject.Find("Recursos");
            if (raiz == null) return null;

            // El primero que tenga malla: los agotados dejan un tocón o nada, y un
            // tocón no dice si el árbol está bien hecho.
            foreach (var nodo in gathering.Nodes)
            {
                if (nodo.IsDepleted) continue;
                var cuerpo = raiz.transform.Find(nodo.InstanceId);
                if (cuerpo != null && cuerpo.GetComponentInChildren<MeshRenderer>() != null)
                    return cuerpo;
            }
            return null;
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
            Debug.Log($"[captura] escrita {nombre}");

            Object.DestroyImmediate(texture);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }

    /// <summary>Cargar la isla de verdad con protagonista, que es lo que todas repiten.</summary>
    public static class Aldea
    {
        public static IEnumerator Cargar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            // Unos cuantos fotogramas: la vista de recursos se construye al siguiente
            // de cargar, a propósito, para que el prado ya tenga colisionador.
            for (int i = 0; i < 6; i++) yield return null;
        }
    }
}
