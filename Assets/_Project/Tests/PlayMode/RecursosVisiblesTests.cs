using System.Collections;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Lo que hay para recoger tiene que verse.
    /// </summary>
    /// <remarks>
    /// Esta es la prueba que faltaba. Había una docena comprobando que el servicio
    /// siembra ciento veinte nodos, que se agotan a los golpes que toca y que vuelven
    /// a los días que dice el catálogo — todas en verde, todo el rato, mientras la
    /// isla era un prado vacío en el que salía un cartel diciendo «Talar roble
    /// anciano» encima de la nada.
    ///
    /// Ninguna prueba de lógica podía verlo, porque no había nada mal en la lógica.
    /// Lo que faltaba era la pregunta de esta clase: ¿y esto se ve?
    /// </remarks>
    public class RecursosVisiblesTests
    {
        [UnityTest]
        public IEnumerator CadaNodoDeLaIslaTieneCuerpo()
        {
            yield return Aldea.Cargar();

            Assert.That(ServiceRegistry.TryGet<IGatheringService>(out var gathering),
                        "no hay servicio de recolección");

            var raiz = GameObject.Find("Recursos");
            Assert.That(raiz, Is.Not.Null,
                        "nadie ha montado los recursos: la isla vuelve a estar pelada");

            int vivos = 0, dibujados = 0;
            foreach (var nodo in gathering.Nodes)
            {
                if (nodo.IsDepleted) continue;
                vivos++;

                var cuerpo = raiz.transform.Find(nodo.InstanceId);
                if (cuerpo != null && cuerpo.GetComponentInChildren<MeshRenderer>() != null)
                    dibujados++;
            }

            Assert.That(vivos, Is.GreaterThan(0), "la isla no ha sembrado nada");
            Assert.That(dibujados, Is.EqualTo(vivos),
                        $"{vivos - dibujados} nodos sin cuerpo: se pueden recoger y no se ven");
        }

        /// <summary>
        /// Y ninguno dentro de un edificio.
        /// </summary>
        /// <remarks>
        /// Los nodos se siembran entre los treinta y los noventa y cinco metros, y las
        /// diez zonas de la aldea están repartidas justo en esa corona. Sin esquivarlas
        /// era cuestión de tiempo que un roble creciera atravesando el tejado de la
        /// panadería — y encima no se podría talar, porque para darle hay que ponerse
        /// delante y delante hay una pared.
        /// </remarks>
        [UnityTest]
        public IEnumerator NingunRecursoCaeDentroDeUnEdificio()
        {
            yield return Aldea.Cargar();

            Assert.That(ServiceRegistry.TryGet<IGatheringService>(out var gathering));
            // Se le pregunta a quien coloca, no a la isla: TryGetSpawnPoint devuelve
            // un punto al azar dentro del círculo de la zona, no dónde está el
            // edificio. Midiendo contra ese punto, esta prueba comprobaba otra cosa.
            Assert.That(ServiceRegistry.TryGet<IBuildService>(out var build));

            // La misma holgura que usa la siembra: la parcela de dos casillas de cuatro
            // metros, más el vuelo del alero.
            const float Parcela = 6f;

            foreach (var zoneId in build.Movable)
            {
                if (!build.TryGetWorldCentre(zoneId, out var centro)) continue;

                foreach (var nodo in gathering.Nodes)
                {
                    float dx = nodo.X - centro.x;
                    float dz = nodo.Z - centro.z;
                    Assert.That(dx * dx + dz * dz, Is.GreaterThanOrEqualTo(Parcela * Parcela),
                                $"{nodo.InstanceId} ha salido dentro de {zoneId}");
                }
            }
        }

        /// <summary>
        /// Y tienen que estar sobre el prado, no flotando ni enterrados.
        /// </summary>
        /// <remarks>
        /// Se siembran a altura cero porque quien siembra no sabe cómo ondula el
        /// terreno, y el prado sube y baja metro y medio. Sin apoyarlos, media
        /// arboleda saldría con las raíces al aire y la otra media enterrada hasta la
        /// copa — que es peor que no dibujarlos, porque parece un fallo de física.
        /// </remarks>
        [UnityTest]
        public IEnumerator LosNodosSeApoyanEnElSuelo()
        {
            yield return Aldea.Cargar();

            var raiz = GameObject.Find("Recursos");
            Assert.That(raiz, Is.Not.Null);

            int mirados = 0;
            foreach (Transform cuerpo in raiz.transform)
            {
                var desde = cuerpo.position + Vector3.up * 40f;
                if (!Physics.Raycast(desde, Vector3.down, out var suelo, 80f,
                                     ~0, QueryTriggerInteraction.Ignore))
                    continue;

                mirados++;
                Assert.That(Mathf.Abs(cuerpo.position.y - suelo.point.y), Is.LessThan(0.5f),
                            $"{cuerpo.name} está a {cuerpo.position.y:0.00} y el suelo a " +
                            $"{suelo.point.y:0.00}");
            }

            Assert.That(mirados, Is.GreaterThan(0), "no se ha podido medir ni un nodo");
        }
    }
}
