using System.Collections;
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
    /// Que el decorado de fiesta exista **en el mundo**, no solo en su clase.
    /// </summary>
    /// <remarks>
    /// Es la prueba de la costura de este encargo: el sistema de fiestas estaba entero
    /// —calendario, pago, flechazos, música— y sin embargo durante un festival la isla
    /// se veía idéntica a un martes cualquiera. Aquí se carga la Isla de verdad, se
    /// publica por el bus justo lo que publica el planificador
    /// (EventScheduler.StartEvent) y se exige que algo haya aparecido… y que al acabar
    /// se haya ido, que es la mitad del contrato: un decorado que se queda puesto para
    /// siempre es peor que no tenerlo.
    /// </remarks>
    public class FiestaDecoradaEnLaIslaTests
    {
        private const string RootName = "DecoradoFiesta";

        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado. Es el
            // mismo cuidado que tiene CronicaEnLaIslaTests.
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            // Es lo que hace el menú cuando el creador termina: FlowController escucha
            // este aviso y llama a StartGame (FlowController.cs:60). La escena trae
            // _startFromMenu activo, así que cargarla sola deja los servicios sin
            // montar — publicarlo es la forma de «pulsar Continuar» de las pruebas.
            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        /// <summary>Abre la primera zona del plano con ese propósito.</summary>
        /// <remarks>
        /// El concierto pide el escenario, y una isla recién empezada lo tiene cerrado
        /// (pide nivel 10): el decorado se niega a montarse en un claro cerrado, así
        /// que la prueba lo abre por la puerta pública — la misma Unlock que la
        /// partida usa cuando la isla se lo gana.
        /// </remarks>
        private static bool AbrirZonaDe(ZonePurpose purpose)
        {
            if (!ServiceRegistry.TryGet<IIslandService>(out var island)) return false;

            foreach (var zoneId in island.ZoneIds)
            {
                if (island.PurposeOf(zoneId) != purpose) continue;
                return island.IsUnlocked(zoneId) || island.Unlock(zoneId);
            }

            return false;
        }

        /// <summary>El centro de la zona de ese propósito, por quien manda la
        /// colocación. Es la misma fuente que usa el propio decorado.</summary>
        private static bool TryCentroDeZona(ZonePurpose purpose, out Vector3 centre)
        {
            centre = Vector3.zero;
            if (!ServiceRegistry.TryGet<IIslandService>(out var island)) return false;
            if (!ServiceRegistry.TryGet<IBuildService>(out var build)) return false;

            foreach (var zoneId in island.ZoneIds)
            {
                if (island.PurposeOf(zoneId) != purpose) continue;
                return build.TryGetWorldCentre(zoneId, out centre);
            }

            return false;
        }

        private static void VerDecoradoEn(Vector3 centro, string porque)
        {
            var root = GameObject.Find(RootName);
            Assert.That(root, Is.Not.Null, porque);

            Assert.That((root.transform.position - centro).magnitude, Is.LessThan(0.01f),
                "el decorado apareció, pero no donde pasa el evento");

            Assert.That(root.GetComponentsInChildren<MeshRenderer>().Length,
                Is.GreaterThanOrEqualTo(6),
                "el decorado salió incompleto: faltan grupos enteros (mástiles, " +
                "farolillos, banderines, macetas)");
        }

        [UnityTest]
        public IEnumerator AlEmpezarElFestivalApareceElDecoradoEnLaPlaza()
        {
            yield return CargarYEmpezar();

            Assert.That(TryCentroDeZona(ZonePurpose.Social, out var plaza), Is.True,
                "la prueba necesita saber dónde está la plaza para exigir ahí el decorado");

            // Justo lo que publica el planificador al arrancar un evento.
            EventBus.Publish(new VillageEventStarted(
                "island_festival", "Festival de la isla", 14));
            yield return null;

            VerDecoradoEn(plaza,
                "el festival empezó —música y avisos ya se enteraron— y la isla se ve " +
                "igual: nadie está escuchando el aviso para pintar el mundo");
        }

        [UnityTest]
        public IEnumerator AlAcabarElFestivalElDecoradoSeRecoge()
        {
            yield return CargarYEmpezar();
            EventBus.Publish(new VillageEventStarted(
                "island_festival", "Festival de la isla", 14));
            yield return null;

            EventBus.Publish(new VillageEventEnded("island_festival"));

            // Destroy se hace efectivo al cerrar el fotograma: sin esperar, Find aún
            // vería el decorado y la prueba mentiría en las dos direcciones.
            yield return null;

            Assert.That(GameObject.Find(RootName), Is.Null,
                "el decorado se queda puesto después del evento: un adorno que nunca " +
                "se va es peor que no tenerlo");

            // Y por si acaso, un segundo aviso de cierre no rompe ni deja nada.
            EventBus.Publish(new VillageEventEnded("island_festival"));
            yield return null;
            Assert.That(GameObject.Find(RootName), Is.Null,
                "recoger dos veces dejó algo colgando");
        }

        [UnityTest]
        public IEnumerator ElConciertoDecoraElEscenario()
        {
            yield return CargarYEmpezar();

            // Sin escenario abierto no hay concierto que valga: el planificador no
            // arrancaría el evento, y el decorado se niega a pintar un claro cerrado.
            Assert.That(AbrirZonaDe(ZonePurpose.Leisure), Is.True,
                "la prueba no pudo abrir el escenario, así que no hay nada que decorar");

            Assert.That(TryCentroDeZona(ZonePurpose.Leisure, out var escenario), Is.True,
                "la prueba necesita saber dónde está el escenario");

            EventBus.Publish(new VillageEventStarted("concert", "Concierto en el escenario",
                                                     17));
            yield return null;

            VerDecoradoEn(escenario,
                "el concierto empezó y el escenario sigue igual que un martes");
        }

        [UnityTest]
        public IEnumerator LosEventosSinZonaNoDejanDecoradoYLaTablaSeRecupera()
        {
            yield return CargarYEmpezar();

            // El día de mercado no tiene zona: no hay sitio donde montarlo y montarlo
            // en la plaza sería inventar.
            EventBus.Publish(new VillageEventStarted("market_day", "Día de mercado", 8));
            yield return null;
            Assert.That(GameObject.Find(RootName), Is.Null,
                "un evento sin zona ha decorado… algo, en algún sitio");

            // Un identificador que no existe tampoco debe colgar nada ni petar.
            EventBus.Publish(new VillageEventStarted("evento_inventado", "?", 12));
            yield return null;
            Assert.That(GameObject.Find(RootName), Is.Null,
                "un evento desconocido ha dejado decorado");

            // Y después de los dos avisos raros, la siguiente fiesta sí se pinta:
            // que lo raro no haya dejado el sistema sordo.
            Assert.That(TryCentroDeZona(ZonePurpose.Social, out var plaza), Is.True);
            EventBus.Publish(new VillageEventStarted(
                "island_festival", "Festival de la isla", 14));
            yield return null;
            VerDecoradoEn(plaza, "tras dos avisos raros, el festival bueno no decora");

            EventBus.Publish(new VillageEventEnded("island_festival"));
            yield return null;
            Assert.That(GameObject.Find(RootName), Is.Null,
                "el decorado de la última fiesta no se recogió");
        }
    }
}
