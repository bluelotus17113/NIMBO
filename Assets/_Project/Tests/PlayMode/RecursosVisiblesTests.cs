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
