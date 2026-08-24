using System.Collections.Generic;
using Nimbo.Art.World;
using Nimbo.Core.Services.Contracts;
using Nimbo.Events.Scheduling;
using Nimbo.Island.Zones;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El decorado de fiesta, por piezas: que su tabla de zonas no se separe del
    /// calendario, que el anillo que levanta quepa donde tiene que caber y que lo que
    /// construye sea un decorado y no tres palos.
    /// </summary>
    /// <remarks>
    /// La tabla de <see cref="FestivalDecor"/> es conocimiento del calendario escrito
    /// en la vista, porque Nimbo.Art no ve Nimbo.Events. Es el mismo caso que la
    /// escala del árbol: una duplicación a través de ensamblados que solo puede
    /// vigilar una prueba. Si alguien añade un evento con zona de fiesta y no le
    /// pone línea en la tabla, esta suite se pone roja — sin ella sería un festival
    /// sin decorado y nadie sabría por qué.
    /// </remarks>
    public class DecoradoDeFiestaTests
    {
        private readonly List<Mesh> _mallas = new();
        private GameObject _raiz;

        [TearDown]
        public void TearDown()
        {
            if (_raiz != null) Object.DestroyImmediate(_raiz);
            _raiz = null;

            foreach (var mesh in _mallas)
                if (mesh != null) Object.DestroyImmediate(mesh);
            _mallas.Clear();

            // La maceta del decorado usa la malla compartida del catálogo: dejarla
            // limpia es dejar las cosas como estaban.
            DecorMeshBuilder.Clear();
        }

        [Test]
        public void LaTablaDelDecoradoSigueAlCalendario()
        {
            foreach (var def in EventCalendar.All)
            {
                bool decora = false;
                if (!string.IsNullOrEmpty(def.RequiredZone))
                {
                    var purpose = PurposeOfZone(def.RequiredZone);
                    decora = purpose == ZonePurpose.Social || purpose == ZonePurpose.Leisure;
                }

                Assert.That(FestivalDecor.TryZoneOf(def.Id, out var asignado),
                            Is.EqualTo(decora),
                    $"«{def.DisplayName}»: el calendario dice que {(decora ? "sí" : "no")} " +
                    "se decora y la tabla del decorado dice lo contrario");

                if (!decora) continue;

                Assert.That(asignado,
                            Is.EqualTo(PurposeOfZone(def.RequiredZone)),
                    $"«{def.DisplayName}» se decora en una zona que no es la suya");
            }

            // Y al revés: ninguna clave zombi de un evento que ya no existe.
            foreach (var eventId in FestivalDecor.DecoratedEventIds)
                Assert.That(EventCalendar.ById(eventId), Is.Not.Null,
                    $"la tabla del decorado menciona «{eventId}», que ya no está en el " +
                    "calendario: esa línea nunca se ejecutará");
        }

        [Test]
        public void ElAnilloCabeEnLasZonasQueDecora()
        {
            foreach (var zone in IslandLayout.FirstIsland())
            {
                if (zone.Purpose != ZonePurpose.Social &&
                    zone.Purpose != ZonePurpose.Leisure) continue;

                Assert.That(zone.Radius,
                    Is.GreaterThanOrEqualTo(FestivalDecorBuilder.RingRadius + 1f),
                    $"«{zone.ZoneId}» mide {zone.Radius} m de radio y el anillo del " +
                    "decorado necesita más: los mástiles quedarían fuera, en el prado");
            }
        }

        [Test]
        public void ElDecoradoSeLevantaCompletoYDentroDelAnillo()
        {
            _raiz = new GameObject("prueba_decorado");

            FestivalDecorBuilder.Build(_raiz.transform, _mallas);

            var renderers = _raiz.GetComponentsInChildren<MeshRenderer>();
            Assert.That(renderers.Length, Is.GreaterThanOrEqualTo(6),
                "un decorado de una pieza no se lee como fiesta: hay grupos que faltan " +
                "(mástiles, farolillos, banderines por color, macetas)");

            int vertices = 0;
            var total = new Bounds(Vector3.zero, Vector3.zero);
            foreach (var renderer in renderers)
            {
                Assert.That(renderer.sharedMaterial, Is.Not.Null,
                    $"{renderer.name} se levanta sin material: se vería magenta o nada");
                vertices += renderer.GetComponent<MeshFilter>().sharedMesh.vertexCount;
                total.Encapsulate(renderer.bounds);
            }

            Assert.That(vertices, Is.GreaterThan(1000),
                "menos de mil vértices para toda la fiesta: algo no se ha combinado");

            Assert.That(total.size.x, Is.LessThanOrEqualTo(FestivalDecorBuilder.RingRadius * 2f + 1f),
                "el decorado es más ancho que su anillo: sobresale hacia el prado");
            Assert.That(total.size.z, Is.LessThanOrEqualTo(FestivalDecorBuilder.RingRadius * 2f + 1f),
                "el decorado es más profundo que su anillo: sobresale hacia el prado");
            Assert.That(total.max.y, Is.LessThanOrEqualTo(3.7f),
                "el decorado es más alto que sus mástiles: algo ha quedado flotando");
            Assert.That(total.min.y, Is.GreaterThanOrEqualTo(-0.05f),
                "algo del decorado queda bajo el suelo");
        }

        private static ZonePurpose PurposeOfZone(string zoneId)
        {
            foreach (var zone in IslandLayout.FirstIsland())
                if (zone.ZoneId == zoneId)
                    return zone.Purpose;

            Assert.Fail($"la zona «{zoneId}» no está en el plano de la isla");
            return ZonePurpose.Civic;   // inalcanzable: Assert.Fail corta antes
        }
    }
}
