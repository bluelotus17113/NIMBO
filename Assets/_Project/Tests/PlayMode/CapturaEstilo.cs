using System.Collections;
using System.IO;
using Nimbo.Art.Chibi;
using Nimbo.Core.Events;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Los mismos encuadres, siempre. Es la vara de medir del estilo.
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

            // A la altura de los ojos mirando un macizo de flores. El macizo no se
            // elige a ojo: es un rodal denso del campo de ruido que siembra las
            // flores (ValueNoise.Variation ≥ 0,70 en el 87 % de su radio de 5 m,
            // media 0,849, centro 54.5, -11.5), así que sigue siendo un macizo
            // aunque cambie la semilla del prado. No es el pico del campo —ese cae
            // en la pradera oeste, (-65.3, 0.0), con 0,991— pero su vecindad es
            // más espesa que la del pico: el círculo de 5 m alrededor de aquel
            // solo tiene un 76 % por encima del umbral, y aquí el macizo manda.
            // Cámara casi horizontal a 12,5 m: es el encuadre
            // que delata una flor tumbada —de horizonte una roseta proyecta
            // sen(inclinación)— y que juzga que el macizo enseñe la cara llena.
            // Detrás quedan ~45 m de prado descendiendo hacia la orilla este, para
            // leer la bruma a esta escala.
            ("flores_a_ras", new Vector3(44f, 1.34f, -9.5f), new Vector3(56f, 0.23f, -13f)),

            // El Árbol Nimbo entero, desde el sur y a media altura.
            ("arbol", new Vector3(0f, 13f, -38f), new Vector3(0f, 17f, 0f)),

            // El linde del noroeste, donde caen los nodos de recoger: árboles pequeños,
            // rocas y matas juntos en un mismo plano.
            ("linde", new Vector3(-44f, 7f, 40f), new Vector3(-62f, 2.5f, 58f)),

            // El borde desde fuera y por debajo del labio: es el encuadre que delata
            // una rendija entre prado y roca. Desde arriba la hierba doblada tapa la
            // costura; a esta altura, si los dos bordes no coinciden, se ve el cielo
            // a través de la isla. La falda se juzga aquí o no se juzga.
            ("bajo_el_borde", new Vector3(150f, -18f, 118f), new Vector3(72f, -6f, 56f)),

            // A ras de prado, con un árbol del linde entre el objetivo y la cámara:
            // es el encuadre que delata si la cámara se mete en tronco o copa cuando
            // va a ras de ojos. Desde arriba ningún árbol estorba; a esta altura,
            // si el antiobstáculos no rodea el fuste, la foto sale llena de hoja
            // por dentro.
            ("tras_el_arbol", new Vector3(-47f, 2.2f, 43f), new Vector3(-56f, 3.0f, 52f)),
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

            // El quinto encuadre no va escrito porque no retrata un escenario: retrata
            // la propia cámara de seguimiento. Se le enciende de verdad y se le deja
            // ponerse sola detrás del protagonista, con su pose y su antiobstáculos;
            // si mañana cambia la pose, la foto cambia con ella sin tocar aquí nada.
            // Es la excepción deliberada a la regla de posiciones escritas de arriba.
            if (seguimiento != null)
            {
                seguimiento.enabled = true;

                // Al publicar ProtagonistCreated el mundo llamó a Follow y la cámara
                // se pegó a su pose de golpe; estos fotogramas son por si el suavizado
                // tuviera que terminar y para que el fondo asiente.
                for (int i = 0; i < 20; i++) yield return null;

                yield return Foto(camera, $"estilo_tercera_persona_{sufijo}.png");
                seguimiento.enabled = false;
            }

            // El vecino de espaldas y desde abajo: el ángulo que delata un casquete
            // de pelo sin cerrar —el borde en canto solo se ve desde abajo— o una
            // cara despegada del cráneo. Desde arriba jamás se nota, y por eso este
            // encuadre no retrata un escenario sino a un habitante: lo saca del
            // censo, lo planta en un sitio escrito del prado y lo fotografía desde
            // donde la cámara de tercera persona acaba mirando a los vecinos.
            yield return RetrataVecinoDeEspaldas(camera, sufijo);

            Assert.Pass();
        }

        private static IEnumerator RetrataVecinoDeEspaldas(Camera camera, string sufijo)
        {
            IslanderView vista = null;
            foreach (var candidata in Object.FindObjectsByType<IslanderView>(
                         FindObjectsSortMode.None))
            {
                vista = candidata;
                break;
            }
            if (vista == null) yield break;   // sin vecinos no hay retrato que tomar

            // Sitio escrito en el prado, el mismo rodal que retrata «flores_a_ras».
            // La altura no va escrita: la isla curva y una Y fija flotaría o se
            // enterraría, así que la da el prado de verdad con un rayo hacia abajo.
            const float x = 50f, z = -11f;
            if (!Physics.Raycast(new Vector3(x, 40f, z), Vector3.down, out var suelo, 80f))
                yield break;

            // PlaceAt también fija su destino: el vecino se queda quieto. Es un
            // retrato, no una caza.
            vista.PlaceAt(suelo.point);
            vista.transform.rotation = Quaternion.Euler(0f, 37f, 0f);

            // Detrás y abajo: a 35 cm del prado —media pantorrilla— y a 1,7 m de los
            // talones, mirando hacia la cabeza. Es el ángulo bajo el que un cacillo
            // abierto enseña su borde como papel y una cara flotante se recorta
            // contra el cráneo; con el pelo cerrado y la cara pegada, lo que se ve
            // es la nuca y el interior del pelo, que es lo que debe verse.
            Vector3 detras = -vista.transform.forward;
            Vector3 camaraEn = suelo.point + detras * 1.7f + Vector3.up * 0.35f;
            Vector3 cabeza = suelo.point + Vector3.up * 1.05f;
            camera.transform.SetPositionAndRotation(
                camaraEn, Quaternion.LookRotation((cabeza - camaraEn).normalized, Vector3.up));

            yield return null;
            yield return null;
            yield return Foto(camera, $"estilo_vecino_de_espaldas_{sufijo}.png");
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
