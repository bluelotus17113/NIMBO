using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Nimbo.Art.CameraWork;
using Nimbo.Art.PlayerView;
using Nimbo.Art.World;
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
    /// Que los bultos de cámara existan **en el juego**: la cámara no se mete en el
    /// árbol junto al que estás, y lo que colocas estando dentro también aparta.
    /// </summary>
    /// <remarks>
    /// Es la misma enfermedad de siempre en su variante óptica: un registro escrito,
    /// unas esferas bien intencionadas y ningún aviso si nadie las enchufa. Aquí se
    /// carga la isla de verdad, se pone al protagonista pegado a un tronco con la
    /// cámara detrás —el gesto exacto de ir a talar— y se comprueba que el
    /// antiobstáculos actuó y que la cámara no acabó dentro de nada.
    /// </remarks>
    public class CamaraEnLaIslaTests
    {
        [SetUp]
        public void BarrerArranquesAjenos()
        {
            // El mismo cuidado que CronicaEnLaIslaTests: el arranque es
            // DontDestroyOnLoad y sobrevive a cargar otra escena. Sin barrerlo, la isla
            // que encuentra esta prueba es la de la prueba anterior —con sus cuerpos
            // vivos y sus bultos ya olvidados— y ningún segundo arranque vuelve a
            // publicar GameLoaded para registrarlos de nuevo.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        // Pose de serie del seguimiento: IslandCamera._followDistance/_followPitch/
        // _followHeight. Escritas aquí porque el rig es privado de la cámara; si un día
        // cambian las de allí, estas cifras dejan de describir el escenario y la
        // primera aserción lo delata.
        private const float DistanciaSerie = 5.5f;
        private const float PitchSerie = 18f;
        private const float AlturaPivote = 1.2f;

        /// <summary>
        /// Mueve al protagonista de verdad.
        /// </summary>
        /// <remarks>
        /// Con el <c>CharacterController</c> encendido, escribir el transform no sirve:
        /// el controlador se come el cambio y lo deja donde estaba — lo dice el
        /// comentario de <c>PlayerBody.KeepOnTheIsland</c>, que teletransporta con el
        /// controlador apagado por exactamente esto. La primera versión de esta prueba
        /// teletransportaba al muñeco cuatro veces seguidas sin moverle un pie.
        /// </remarks>
        private static void Teletransporta(PlayerBody jugador, Vector3 donde)
        {
            var controlador = jugador.GetComponent<CharacterController>();
            bool estaba = controlador.enabled;
            controlador.enabled = false;
            jugador.transform.SetPositionAndRotation(donde, Quaternion.Euler(0f, 180f, 0f));
            controlador.enabled = estaba;
        }

        [UnityTest]
        public IEnumerator LaCamaraNoSeMeteEnElTroncoAlQuedarseDetras()
        {
            CameraObstacles.Clear();
            yield return Aldea.Cargar();

            // ── un árbol vivo, el primero que haya dibujado ──────────────────
            Assert.IsTrue(ServiceRegistry.TryGet<IGatheringService>(out var gathering),
                          "no hay servicio de recolección");
            var raiz = GameObject.Find("Recursos");
            Assert.IsNotNull(raiz, "los nodos no tienen raíz en la escena");

            Transform arbol = null;
            foreach (var nodo in gathering.Nodes)
            {
                if (nodo.IsDepleted) continue;
                if (!gathering.TryGetDefinition(nodo.NodeId, out var definicion) ||
                    definicion.Kind != NodeKind.Tree) continue;

                arbol = raiz.transform.Find(nodo.InstanceId);
                if (arbol != null) break;
            }
            Assert.IsNotNull(arbol, "no hay ni un árbol vivo dibujado en la isla");

            // El enchufe se comprueba antes que la geometría: si la isla cargó sin
            // registrar bultos, lo de abajo fallaría con un mensaje de geometría que
            // no diría nada de lo que pasa.
            Assert.That(CameraObstacles.Count, Is.GreaterThan(0),
                        $"la isla cargó y GatheringView no registró ni un bulto de cámara. " +
                        $"Raíz Recursos: {(raiz != null ? $"{raiz.transform.childCount} cuerpos en escena '{raiz.scene.name}'" : "ausente")}; " +
                        $"escena activa: '{SceneManager.GetActiveScene().name}'; " +
                        $"arranques vivos: {Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(FindObjectsSortMode.None).Length}");

            UnityEngine.Debug.Log($"[colisiones] tras cargar: bultos={CameraObstacles.Count} " +
                                  $"árbol={arbol.position} fotograma={Time.frameCount}");

            var jugador = Object.FindFirstObjectByType<PlayerBody>();
            Assert.IsNotNull(jugador, "no hay protagonista en la isla");

            // Con el yaw de serie (0) la cámara se pone al SUR del pivote. El jugador
            // va al NORTE del tronco: así el fuste queda EN MEDIO del recorrido
            // pivote→cámara, que es justo lo que pasa al girar la vista junto a un
            // árbol para talarlo.
            //
            // La distancia exacta se busca y no se supone: el prado sube y baja, y el
            // jugador se asienta en el suelo LOCAL, que no es el del pie del árbol. Se
            // prueba a pegarse más o menos y vale la primera postura cuyo recorrido
            // pivote→cámara corta de verdad un bulto — sin ese corte, lo de abajo no
            // estaría probando nada.
            var direccion = Quaternion.Euler(PitchSerie, 0f, 0f) * Vector3.back;
            direccion.Normalize();

            bool montado = false;
            float corte = 0f;
            Vector3 pivote = Vector3.zero;

            foreach (float pegado in new[] { 0.9f, 1.4f, 2.0f, 2.8f })
            {
                Teletransporta(jugador, arbol.position + new Vector3(0f, 0f, pegado));

                // Que el controlador asiente al suelo local y el suavizado del rig
                // respire un poco.
                for (int i = 0; i < 12; i++) yield return null;

                pivote = jugador.transform.position + Vector3.up * AlturaPivote;
                if (CameraObstacles.TryHit(pivote, direccion, DistanciaSerie, out corte))
                {
                    montado = true;
                    break;
                }
            }

            Assert.IsTrue(montado,
                $"ninguna postura junto al árbol cruzó un bulto. Árbol en {arbol.position}, " +
                $"jugador en {jugador.transform.position}, bultos registrados: {CameraObstacles.Count}");

            // Converge del todo. En TIEMPO y no en fotogramas: el rig se suaviza por
            // segundo (Sharpness 8) y el runner en batchmode dispara a cientos de
            // FPS — cuarenta fotogramas suyos son ocho centésimas y la cámara seguiría
            // a doscientos metros del muñeco. Un segundo y medio deja el resto de una
            // convergencia exponencial en micras a cualquier velocidad de reloj.
            float limite = Time.unscaledTime + 1.5f;
            while (Time.unscaledTime < limite) yield return null;

            var camara = Camera.main;
            Assert.IsNotNull(camara, "la escena Isla no trae cámara");

            // El jugador puede haberse asentado unos centímetros más durante la
            // convergencia: se mide el corte con la postura final.
            pivote = jugador.transform.position + Vector3.up * AlturaPivote;
            Assert.IsTrue(CameraObstacles.TryHit(pivote, direccion, DistanciaSerie, out corte),
                          "el escenario dejó de cruzar el bulto al asentarse el jugador");

            float conseguida = Vector3.Distance(pivote, camara.transform.position);
            float esperada = Mathf.Max(corte - 0.6f, 1.5f);   // el mismo recorte del antiobstáculos

            Assert.That(conseguida, Is.LessThanOrEqualTo(esperada + 0.15f),
                        $"el antiobstáculos no actuó: la cámara se quedó a {conseguida:0.00} m " +
                        $"cuando el bulto pedía {esperada:0.00}");

            Assert.IsFalse(CameraObstacles.Contains(camara.transform.position),
                           "la cámara acabó DENTRO de un bulto registrado");
        }

        [UnityTest]
        public IEnumerator UnAdornoAltoRecienColocadoApartaLaCamara()
        {
            CameraObstacles.Clear();
            yield return Aldea.Cargar();

            int antes = CameraObstacles.Count;

            Assert.IsTrue(ServiceRegistry.TryGet<IDecorService>(out var decor),
                          "no hay servicio de adornos");

            // Lo alto del catálogo, lo más barato de desbloquear primero. No hay
            // ninguna luz a nivel 0 —la más baja es el farolillo, a nivel 1—, así que
            // se pide cualquiera de los tipos altos y se intenta colocar; si el
            // servicio rechaza, el motivo queda en el mensaje.
            var altos = new List<DecorDefinition>();
            foreach (var ficha in decor.Catalog)
                if (ficha.Kind is DecorKind.Light or DecorKind.Statue
                    or DecorKind.Sign or DecorKind.Water)
                    altos.Add(ficha);
            altos.Sort((a, b) => a.UnlockLevel.CompareTo(b.UnlockLevel));
            Assert.That(altos, Is.Not.Empty,
                        "el catálogo no tiene ningún adorno alto que poder colocar");

            Assert.IsTrue(ServiceRegistry.TryGet<IIslandService>(out var isla));
            var mundo = Object.FindFirstObjectByType<WorldView>();
            Assert.IsNotNull(mundo, "no hay WorldView en la escena");

            // En alguna zona abierta, en un anillo alrededor del edificio central:
            // pegado a la pared es donde antes puede rechazarlo.
            string colocacion = null;
            Vector3 local = Vector3.zero, centroZona = Vector3.zero;
            DecorRejection motivo = DecorRejection.Ok;
            string catalogo = null;

            foreach (var ficha in altos)
            {
                foreach (var zona in isla.ZoneIds)
                {
                    if (!isla.IsUnlocked(zona)) continue;
                    if (!mundo.TryGetZoneCentre(zona, out var centro)) continue;

                    foreach (var candidato in new[]
                             {
                                 new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 5f),
                                 new Vector3(5f, 0f, -5f), new Vector3(-5f, 0f, -5f),
                                 new Vector3(8f, 0f, 0f), new Vector3(-8f, 0f, 0f),
                             })
                    {
                        colocacion = decor.Place(ficha.CatalogId, zona, candidato, 0f);
                        if (string.IsNullOrEmpty(colocacion))
                        {
                            motivo = decor.CanPlace(ficha.CatalogId, zona, candidato);
                            continue;
                        }

                        local = candidato;
                        centroZona = centro;
                        catalogo = ficha.CatalogId;
                        break;
                    }

                    if (!string.IsNullOrEmpty(colocacion)) break;
                }

                if (!string.IsNullOrEmpty(colocacion)) break;
            }
            Assert.That(colocacion, Is.Not.Null.And.Not.Empty,
                        $"no se pudo colocar ningún adorno alto. Último intento: {catalogo}, " +
                        $"motivo: {motivo}");

            // El barrido va al fotograma siguiente del aviso, a propósito.
            yield return null;
            yield return null;

            Assert.That(CameraObstacles.Count, Is.GreaterThan(antes),
                        "se colocó un adorno y el registro de la cámara no se enteró");

            // Un tiro horizontal que pasa por su eje tiene que cortar alguna de sus
            // esferas. Las alturas son las de la tabla de DecorMeshBuilder: mástil de
            // farola (1,65), columna de estatua (1,65), tabla de cartel (1,75), poste
            // (0,75) y fuente (1,10). El corte esperado es 4 m hasta el eje menos el
            // radio de la esfera que toque.
            var posicion = centroZona + local;
            bool corto = false;
            float distancia = 0f;
            foreach (float altura in new[] { 0.75f, 1.10f, 1.65f, 1.75f })
            {
                if (!CameraObstacles.TryHit(posicion + new Vector3(-4f, altura, 0f),
                                            Vector3.right, 8f, out distancia)) continue;
                if (distancia >= 3.0f && distancia <= 4.4f)
                {
                    corto = true;
                    break;
                }
            }

            Assert.IsTrue(corto,
                        $"{catalogo} recién colocado no aparta a la cámara: ningún tiro por " +
                        $"su eje cortó una esfera donde tocaba");
            Assert.That(distancia, Is.InRange(3.0f, 4.4f),
                        "el corte no está donde está el adorno");
        }

        /// <summary>
        /// Entrar y salir del modo construcción no puede apagar los adornos.
        /// </summary>
        /// <remarks>
        /// La enfermedad que esta prueba cierra: construir apaga <c>IslandCamera</c>
        /// (<c>BuildModeView.Enter</c>), su <c>OnDisable</c> suelta los bultos de
        /// adornos, y al volver a encenderla nadie volvía a marcar el barrido — sin
        /// excepción y sin aviso, la mitad de adornos del registro se quedaba muerta
        /// hasta recargar la escena. Es la variante silenciosa de siempre: cargaba
        /// fresca y las pruebas no entraban en construir, que es el bucle central.
        /// </remarks>
        [UnityTest]
        public IEnumerator EntrarYSalirDeConstruirConservaLosBultosDeAdornos()
        {
            CameraObstacles.Clear();
            yield return Aldea.Cargar();

            // Un adorno alto garantizado: la isla recién estrenada puede traer
            // adornos o no según la partida, y esta prueba necesita al menos uno
            // registrado para poder notar su caída.
            Assert.IsTrue(ColocaUnAdornoAlto(out string motivo),
                          $"no se pudo colocar ni un adorno alto: {motivo}");

            // El barrido va al fotograma siguiente del aviso, a propósito.
            yield return null;
            yield return null;

            int antes = CameraObstacles.Count;
            Assert.That(antes, Is.GreaterThan(0),
                        "con adorno colocado el registro siguió vacío");

            // Entrar en construir apaga la cámara y su OnDisable suelta los bultos
            // de adornos. Si NO bajara, este ida-y-vuelta no estaría probando nada.
            EventBus.Publish(new BuildModeChanged(true));
            yield return null;

            int durante = CameraObstacles.Count;
            Assert.That(durante, Is.LessThan(antes),
                        $"entrar en construir no soltó bultos ({durante} de {antes}): " +
                        "o BuildModeView no apagó la cámara o no había adornos registrados");

            // Salir tiene que reponerlos. El barrido remarca en OnEnable y corre al
            // fotograma siguiente, como todos los de esta cámara.
            EventBus.Publish(new BuildModeChanged(false));
            yield return null;
            yield return null;

            Assert.That(CameraObstacles.Count, Is.EqualTo(antes),
                        $"al salir de construir quedaron {CameraObstacles.Count} bultos " +
                        $"de los {antes} que había: los adornos no volvieron al registro");
        }

        /// <summary>
        /// Coloca el primer adorno alto que acepte la isla.
        /// </summary>
        /// <remarks>
        /// Alto = con esferas de cámara según <c>DecorMeshBuilder.TryCameraSpheres</c>
        /// (farola, estatua, cartel, fuente); asiento, planta y valla no registran
        /// porque no llegan al recorrido pivote→cámara. La búsqueda replica la de
        /// <see cref="UnAdornoAltoRecienColocadoApartaLaCamara"/> en corto: catálogo
        /// por nivel de desbloqueo, zonas abiertas, unas cuantas casillas libres.
        /// </remarks>
        private static bool ColocaUnAdornoAlto(out string motivo)
        {
            motivo = "sin servicio de adornos";
            if (!ServiceRegistry.TryGet<IDecorService>(out var decor)) return false;
            if (!ServiceRegistry.TryGet<IIslandService>(out var isla))
            {
                motivo = "sin servicio de isla";
                return false;
            }

            var mundo = Object.FindFirstObjectByType<WorldView>();
            if (mundo == null)
            {
                motivo = "no hay WorldView en la escena";
                return false;
            }

            var altos = new List<DecorDefinition>();
            foreach (var ficha in decor.Catalog)
                if (ficha.Kind is DecorKind.Light or DecorKind.Statue
                    or DecorKind.Sign or DecorKind.Water)
                    altos.Add(ficha);
            altos.Sort((a, b) => a.UnlockLevel.CompareTo(b.UnlockLevel));

            motivo = "catálogo alto vacío";
            foreach (var ficha in altos)
                foreach (var zona in isla.ZoneIds)
                {
                    if (!isla.IsUnlocked(zona)) continue;
                    if (!mundo.TryGetZoneCentre(zona, out _)) continue;

                    foreach (var candidato in new[]
                             {
                                 new Vector3(5f, 0f, 5f), new Vector3(-5f, 0f, 5f),
                                 new Vector3(5f, 0f, -5f), new Vector3(-8f, 0f, 0f),
                             })
                    {
                        if (!string.IsNullOrEmpty(
                                decor.Place(ficha.CatalogId, zona, candidato, 0f)))
                            return true;

                        motivo = $"{ficha.CatalogId} en {zona}{candidato}: " +
                                 $"{decor.CanPlace(ficha.CatalogId, zona, candidato)}";
                    }
                }

            return false;
        }

        /// <summary>
        /// El precio de todo esto, medido contra el registro real de la isla cargada.
        /// </summary>
        /// <remarks>
        /// La alternativa era una capa física con ciento veinte colisionadores nuevos,
        /// que habrían entrado como candidatos en CADA rayo de la escena. Esto es una
        /// pasada por una lista corta una vez por fotograma: el número sale impreso en
        /// el log para el informe, y el techo de la aserción es deliberadamente
        /// generoso para no volver la prueba frágil en máquina lenta.
        /// </remarks>
        [UnityTest]
        public IEnumerator ConsultarElRegistroCuestaMicrosegundos()
        {
            CameraObstacles.Clear();
            yield return Aldea.Cargar();

            int bultos = CameraObstacles.Count;
            UnityEngine.Debug.Log($"[colisiones] esferas registradas tras cargar la isla: {bultos}");

            var reloj = new Stopwatch();
            const int Consultas = 20000;
            var origen = new Vector3(0f, 2f, -40f);

            reloj.Start();
            for (int i = 0; i < Consultas; i++)
                CameraObstacles.TryHit(origen, Vector3.forward, 80f, out _);
            reloj.Stop();

            float micros = (float)reloj.Elapsed.TotalMilliseconds * 1000f / Consultas;
            UnityEngine.Debug.Log($"[colisiones] {micros:0.000} µs por consulta con {bultos} esferas");

            Assert.That(bultos, Is.GreaterThan(0),
                        "la isla cargó entera y no registró ni un bulto de cámara");
            Assert.That(micros, Is.LessThan(50f),
                        $"una consulta cuesta {micros:0.000} µs: más de lo que esto puede permitir");
        }
    }
}
