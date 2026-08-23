using Nimbo.Art.CameraWork;
using Nimbo.Art.Chibi;
using Nimbo.Art.World;
using Nimbo.Core.Services.Contracts;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La matemática del registro de bultos de cámara, sin escena.
    /// </summary>
    /// <remarks>
    /// <see cref="CameraObstacles"/> es lo que evita meter colisionadores en los
    /// árboles solo para que la cámara no se meta en ellos. Que su intersección
    /// rayo-esfera falle a la primera es justo el tipo de fallo que no se ve en
    /// pantalla —la cámara roza el tronco un fotograma y nadie sabe por qué—, así que
    /// va medida aquí con geometría escrita.
    /// </remarks>
    [TestFixture]
    public class ColisionesDeCamaraTests
    {
        private static readonly Vector3 Origen = new(0f, 1f, -5f);
        private static readonly Vector3 Norte = Vector3.forward;

        [SetUp]
        public void VaciarRegistro() => CameraObstacles.Clear();

        [TearDown]
        public void SoltarTodo()
        {
            CameraObstacles.Clear();
        }

        // ── TryHit ──────────────────────────────────────────────────────────

        [Test]
        public void UnBultoEnMedioCortaALaDistanciaJusta()
        {
            var dueno = new GameObject("bulto");
            // Esfera de radio 1 en el origen del plano: el rayo sale 5 m al sur.
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, 0f), 1f);

            bool corto = CameraObstacles.TryHit(Origen, Norte, 50f, out float distancia);

            Assert.IsTrue(corto, "el rayo atraviesa la esfera de lado a lado: tiene que cortar");
            Assert.That(distancia, Is.EqualTo(4f).Within(0.0001f),
                        "de −5 a la cara sur de una esfera de radio 1 hay exactamente 4");
        }

        [Test]
        public void UnBultoDetrasDelOrigenNoCorta()
        {
            var dueno = new GameObject("bulto");
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, -10f), 1f);

            Assert.IsFalse(CameraObstacles.TryHit(Origen, Norte, 50f, out _),
                           "lo que queda a la espalda del recorrido no aparta la cámara");
        }

        [Test]
        public void UnBultoMasAllaDelTopeNoCorta()
        {
            var dueno = new GameObject("bulto");
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, 0f), 1f);

            // El corte estaría a 4 m; el segmento acaba a 3.
            Assert.IsFalse(CameraObstacles.TryHit(Origen, Norte, 3f, out _),
                           "un bulto más allá del final del segmento no puede tirar de la cámara");
        }

        [Test]
        public void UnBultoQueElRayoRozaDeLadoNoCorta()
        {
            var dueno = new GameObject("bulto");
            // A 5 m de lado: pasa lejos de una esfera de radio 1.
            CameraObstacles.Add(dueno.transform, new Vector3(5f, 1f, 0f), 1f);

            Assert.IsFalse(CameraObstacles.TryHit(Origen, Norte, 50f, out _),
                           "el rayo pasa a 5 metros del centro: no hay corte que valga");
        }

        [Test]
        public void EmpezarDentroDevuelveCero()
        {
            var dueno = new GameObject("bulto");
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, 0f), 1f);

            bool corto = CameraObstacles.TryHit(new Vector3(0f, 1f, 0.5f), Norte, 50f,
                                                out float distancia);

            Assert.IsTrue(corto);
            Assert.That(distancia, Is.EqualTo(0f),
                        "si la cámara ya está dentro del bulto, hay que apartarla ya");
        }

        [Test]
        public void DeVariosBultosGanaElMasCercano()
        {
            var lejano = new GameObject("lejano");
            var cercano = new GameObject("cercano");
            CameraObstacles.Add(lejano.transform, new Vector3(0f, 1f, 20f), 2f);
            CameraObstacles.Add(cercano.transform, new Vector3(0f, 1f, 8f), 1f);

            CameraObstacles.TryHit(Origen, Norte, 50f, out float distancia);

            Assert.That(distancia, Is.EqualTo(12f).Within(0.0001f),
                        "la cámara se aparta por lo primero que encuentre, no por lo más gordo");
        }

        // ── dueños ──────────────────────────────────────────────────────────

        [Test]
        public void QuitarElDuenoQuitaTodasSusEsferas()
        {
            var tronco = new GameObject("tronco");
            var adorno = new GameObject("adorno");

            // Un árbol apila tres esferas bajo el mismo dueño.
            CameraObstacles.Add(tronco.transform, Vector3.zero, 0.35f);
            CameraObstacles.Add(tronco.transform, Vector3.up, 0.35f);
            CameraObstacles.Add(tronco.transform, Vector3.up * 2f, 0.35f);
            CameraObstacles.Add(adorno.transform, new Vector3(30f, 0f, 0f), 1f);

            CameraObstacles.Remove(tronco.transform);

            Assert.That(CameraObstacles.Count, Is.EqualTo(1),
                        "quitar un dueño se lleva todo lo suyo, no solo una esfera");
        }

        [Test]
        public void UnDuenoDestruidoSeIgnoraYSaleDeLaLista()
        {
            var dueno = new GameObject("cadáver");
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, 0f), 1f);
            Assert.That(CameraObstacles.Count, Is.EqualTo(1));

            Object.DestroyImmediate(dueno);

            Assert.IsFalse(CameraObstacles.TryHit(Origen, Norte, 50f, out _),
                           "un cuerpo que ya no está no debe seguir apartando cámaras");
            Assert.That(CameraObstacles.Count, Is.EqualTo(0),
                        "y de paso se limpia de la lista, que si no acumula cadáveres");
        }

        // ── Contains ────────────────────────────────────────────────────────

        [Test]
        public void ContieneSoloLoQueEstaDentro()
        {
            var dueno = new GameObject("bulto");
            CameraObstacles.Add(dueno.transform, new Vector3(0f, 1f, 0f), 1f);

            Assert.IsTrue(CameraObstacles.Contains(new Vector3(0.5f, 1f, 0.5f)),
                          "a 0,7 del centro, dentro");
            Assert.IsFalse(CameraObstacles.Contains(new Vector3(0f, 1f, 1.2f)),
                           "a 1,2 del centro, fuera");
        }

        // ── adornos ─────────────────────────────────────────────────────────

        [Test]
        public void LaMirillaInversaReconoceLasMallasDelCatalogo()
        {
            foreach (DecorKind kind in System.Enum.GetValues(typeof(DecorKind)))
            {
                var mesh = DecorMeshBuilder.For(kind);
                Assert.IsTrue(DecorMeshBuilder.TryKindOf(mesh, out var reconocido),
                              $"la malla de {kind} no se reconoce como suya");
                Assert.That(reconocido, Is.EqualTo(kind));
            }

            Assert.IsFalse(DecorMeshBuilder.TryKindOf(null, out _),
                           "una malla nula no es de nadie");

            // Y una malla que no sale del catálogo —el prado, un muro— tampoco.
            var ajena = MeshShapes.Box(Vector3.one);
            Assert.IsFalse(DecorMeshBuilder.TryKindOf(ajena, out _),
                           "una malla ajena al catálogo no puede colar por adorno");
        }

        [Test]
        public void LosAdornosBajosNoPidenBulto()
        {
            // El recorrido pivote→cámara nunca baja de ~1,5 m: asiento (1,30), planta
            // (1,42) y valla (1,00) no lo cruzan ni de casualidad.
            Assert.IsFalse(DecorMeshBuilder.TryCameraSpheres(DecorKind.Seat, out _));
            Assert.IsFalse(DecorMeshBuilder.TryCameraSpheres(DecorKind.Plant, out _));
            Assert.IsFalse(DecorMeshBuilder.TryCameraSpheres(DecorKind.Fence, out _));
        }

        [Test]
        public void LosAdornosAltosPidenBultoALaAlturaQueToca()
        {
            Assert.IsTrue(DecorMeshBuilder.TryCameraSpheres(DecorKind.Light, out var farola));
            Assert.That(farola.Length, Is.EqualTo(2),
                        "farola: mástil y bola de la lámpara, dos cosas que rodear");
            Assert.That(farola[1].centre.y, Is.EqualTo(3.35f).Within(0.01f),
                        "la segunda esfera tiene que estar en la lámpara, no a media asta: " +
                        "el bounds de la farola la deja sin cubrir");

            Assert.IsTrue(DecorMeshBuilder.TryCameraSpheres(DecorKind.Statue, out _));
            Assert.IsTrue(DecorMeshBuilder.TryCameraSpheres(DecorKind.Sign, out _));
            Assert.IsTrue(DecorMeshBuilder.TryCameraSpheres(DecorKind.Water, out _));
        }
    }
}
