using System.Collections.Generic;
using Nimbo.Art.World;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El rodeo de los vecinos. No es un NavMesh y no pretende serlo: lo que se prueba
    /// es que nadie cruce por dentro de un edificio y que aun así lleguen.
    /// </summary>
    public class WalkAroundTests
    {
        private static IReadOnlyList<Obstacle> One(Vector3 centre, float radius) =>
            new List<Obstacle> { new Obstacle(centre, radius) };

        [Test]
        public void SinBultosVaEnLineaRecta()
        {
            var step = WalkAround.Steer(Vector3.zero, new Vector3(10f, 0f, 0f), null);
            Assert.AreEqual(1f, step.x, 0.001f);
            Assert.AreEqual(0f, step.z, 0.001f);
        }

        [Test]
        public void UnBultoALadoNoDesvia()
        {
            // El edificio está lejos de la recta: no tiene por qué estorbar, y
            // desviarse por él haría que la gente anduviera en zigzag por la isla.
            var obstacles = One(new Vector3(5f, 0f, 20f), 4f);
            var step = WalkAround.Steer(Vector3.zero, new Vector3(10f, 0f, 0f), obstacles);

            Assert.AreEqual(1f, step.x, 0.001f);
        }

        [Test]
        public void UnBultoEnMedioDesvia()
        {
            var obstacles = One(new Vector3(5f, 0f, 0f), 3f);
            var step = WalkAround.Steer(Vector3.zero, new Vector3(12f, 0f, 0f), obstacles);

            Assert.Greater(Mathf.Abs(step.z), 0.3f,
                           "va derecho contra el edificio en vez de rodearlo");
        }

        [Test]
        public void DesdeDentroSaleHaciaFuera()
        {
            // Pasa cuando se abre una zona encima de alguien que estaba ahí de pie.
            var obstacles = One(new Vector3(0f, 0f, 0f), 5f);
            var step = WalkAround.Steer(new Vector3(1f, 0f, 0f), new Vector3(20f, 0f, 0f), obstacles);

            Assert.Greater(step.x, 0.9f, "se quedó dentro del edificio");
        }

        [Test]
        public void RodeandoAcabaLlegando()
        {
            // La prueba que de verdad importa: dando pasitos, ¿llega o se queda dando
            // vueltas al edificio para siempre? Es el fallo típico de esquivar solo por
            // la tangente, y no lo ve ninguna prueba de una sola llamada.
            var obstacles = One(new Vector3(10f, 0f, 0f), 4f);
            var destination = new Vector3(20f, 0f, 0f);

            var position = Vector3.zero;
            for (int i = 0; i < 400; i++)
            {
                var step = WalkAround.Steer(position, destination, obstacles);
                if (step.sqrMagnitude < 0.0001f) break;

                position += step * 0.2f;
                if ((destination - position).sqrMagnitude < 0.25f) break;
            }

            Assert.Less(Vector3.Distance(position, destination), 1f,
                        $"se quedó dando vueltas: acabó en {position}");
        }

        [Test]
        public void RodeandoNoPisaElEdificio()
        {
            var centre = new Vector3(10f, 0f, 0f);
            var obstacles = One(centre, 4f);
            var destination = new Vector3(20f, 0f, 0f);

            var position = Vector3.zero;
            float closest = float.MaxValue;

            for (int i = 0; i < 400; i++)
            {
                var step = WalkAround.Steer(position, destination, obstacles);
                if (step.sqrMagnitude < 0.0001f) break;

                position += step * 0.2f;
                closest = Mathf.Min(closest, Vector3.Distance(position, centre));
                if ((destination - position).sqrMagnitude < 0.25f) break;
            }

            Assert.Greater(closest, 3.5f,
                           $"se metió a {closest:0.0} m del centro de un edificio de radio 4");
        }
    }
}
