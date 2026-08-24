using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Cuando la aldea celebra algo, el mundo se entera: levanta el decorado de
    /// fiesta en la zona del evento y lo recoge al acabar.
    /// </summary>
    /// <remarks>
    /// Hasta ahora el único suscriptor a estos avisos en todo el proyecto era el
    /// sonido (<c>rg "Subscribe&lt;VillageEvent"</c> daba un fichero:
    /// AudioDirector.cs) — durante un festival la isla se veía idéntica a un martes
    /// cualquiera, aunque el jugador hubiera pagado 1.200 monedas por montarlo
    /// (VillageEvents.FestivalCost). Este es el trozo de vista que faltaba.
    ///
    /// Es estático y se arranca solo (mismo camino que GateNoticeHost): ni la escena
    /// ni el bootstrap saben que existe, así que no hace falta tocar fichero ajeno
    /// para tenerlo vivo. No necesita fotogramas —colocar y recoger son operaciones
    /// secas—, así que no hay Update: solo el bus.
    /// </remarks>
    public static class FestivalDecor
    {
        private const string RootName = "DecoradoFiesta";

        /// <summary>
        /// Qué tipo de zona celebra cada evento que hoy se decora.
        /// </summary>
        /// <remarks>
        /// El aviso del bus (<c>VillageEventStarted</c>) lleva el identificador del
        /// evento pero no su zona, y la zona vive en <c>Nimbo.Events</c>, que Nimbo.Art
        /// no ve. La tabla puentea eso por el lado que puede: en vez de escribir ids de
        /// zona —el fallo histórico de «stage» y «plaza_central» pasó por escribirlos a
        /// mano—, escribe el propósito y deja que <see cref="ZoneWithPurpose"/> lo
        /// resuelva contra el plano que trae el servicio de isla, que sale de
        /// IslandLayout.
        ///
        /// Que la tabla y el calendario no se separen lo vigila
        /// DecoradoDeFiestaTests.LaTablaDelDecoradoSigueAlCalendario, que compara una
        /// contra EventCalendar: un evento nuevo con zona de fiesta y sin línea aquí es
        /// una prueba roja, no un festival sordo.
        /// </remarks>
        private static readonly Dictionary<string, ZonePurpose> EventZone = new()
        {
            ["island_festival"] = ZonePurpose.Social,    // la plaza
            ["talent_show"] = ZonePurpose.Leisure,       // el escenario
            ["concert"] = ZonePurpose.Leisure,           // el escenario
        };

        private static Transform _root;

        /// <summary>Las mallas estrenadas por el decorado en pie. Al morir la escena
        /// quedan referencias muertas: se barren antes de reutilizar la lista.</summary>
        private static readonly List<Mesh> CreatedMeshes = new();

        /// <summary>Qué zona celebraría ese evento, si es de los que se decoran.</summary>
        public static bool TryZoneOf(string eventId, out ZonePurpose purpose) =>
            EventZone.TryGetValue(eventId ?? "", out purpose);

        /// <summary>Los eventos que hoy se decoran. Es lo que recorre la prueba que
        /// vigila que la tabla y el calendario no se separen.</summary>
        public static IReadOnlyCollection<string> DecoratedEventIds => EventZone.Keys;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            EventBus.Subscribe<GameLoaded>(OnGameLoaded);
            EventBus.Subscribe<VillageEventStarted>(OnEventStarted);
            EventBus.Subscribe<VillageEventEnded>(OnEventEnded);
        }

        private static void OnGameLoaded(GameLoaded _)
        {
            TearDown();

            // Una partida guardada con la fiesta a media sesión carga con el evento ya
            // activo: su aviso de inicio se publicó en otra sesión y nadie lo está
            // esperando. Lo mismo que hace el sonido (AudioDirector.OnGameLoaded lee
            // ActiveEventId para elegir el humor), pero para los ojos.
            if (ServiceRegistry.TryGet<IVillageEvents>(out var events) &&
                !string.IsNullOrEmpty(events.ActiveEventId))
                SetUp(events.ActiveEventId);
        }

        private static void OnEventStarted(VillageEventStarted evt)
        {
            // Defensivo: el planificador no monta dos a la vez, pero recoger antes de
            // poner cuesta una línea y un decorado huérfano cuesta más.
            TearDown();
            SetUp(evt.EventId);
        }

        private static void OnEventEnded(VillageEventEnded evt) => TearDown();

        private static void SetUp(string eventId)
        {
            if (!EventZone.TryGetValue(eventId ?? "", out var purpose)) return;
            if (!ServiceRegistry.TryGet<IIslandService>(out var island)) return;

            string zoneId = ZoneWithPurpose(island, purpose);
            if (zoneId == null) return;

            // Una zona cerrada es un claro de hierba: montar la fiesta en el vacío
            // delataría el decorado como pegatina. Los eventos piden su zona abierta
            // para poder empezar (EventScheduler.EligibleEvents), así que esto solo
            // filtra avisos que no deberían llegar.
            if (!island.IsUnlocked(zoneId)) return;

            // El centro manda igual que para los vecinos: donde esté puesto el
            // edificio, o donde lo dejó el diseño si nadie lo movió. Es la misma
            // fuente que usan las agendas (IslandService.TryGetSpawnPoint), así que el
            // anillo nace centrado donde la gente va a estar.
            if (!ServiceRegistry.TryGet<IBuildService>(out var build) ||
                !build.TryGetWorldCentre(zoneId, out var centre))
                return;

            // Sin vista de mundo no hay isla donde colgarlo: en el menú no decora.
            var world = Object.FindFirstObjectByType<WorldView>();
            if (world == null) return;

            _root = new GameObject(RootName).transform;
            _root.SetParent(world.transform, worldPositionStays: true);
            _root.position = centre;

            FestivalDecorBuilder.Build(_root, CreatedMeshes);
        }

        private static void TearDown()
        {
            if (_root != null) Object.Destroy(_root.gameObject);
            _root = null;

            foreach (var mesh in CreatedMeshes)
                if (mesh != null) Object.Destroy(mesh);
            CreatedMeshes.Clear();
        }

        /// <summary>La primera zona del plano con ese propósito.</summary>
        /// <remarks>
        /// Hoy el plano tiene exactamente una zona Social y una Leisure, así que no hay
        /// ambigüedad. Si algún día hay dos, esto elegirá la primera del plano sin
        /// avisar — decisión de diseño pendiente, y mejor ahí que escondida en un
        /// desempate por azar.
        /// </remarks>
        private static string ZoneWithPurpose(IIslandService island, ZonePurpose purpose)
        {
            foreach (var zoneId in island.ZoneIds)
                if (island.PurposeOf(zoneId) == purpose)
                    return zoneId;

            return null;
        }
    }
}
