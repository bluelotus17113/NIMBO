using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Art.Materials;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using UnityEngine;

namespace Nimbo.Art.World
{
    /// <summary>
    /// Levanta la isla y sus habitantes, y los mantiene en su sitio.
    /// </summary>
    /// <remarks>
    /// Es la única pieza de arte que habla con la simulación, y solo por servicios.
    /// Todo lo que dibuja se genera aquí en el arranque: el proyecto no tiene ni un
    /// modelo importado, y esa fue la condición para poder tener el juego entero
    /// funcionando antes de ponerle cara.
    /// </remarks>
    public sealed class WorldView : MonoBehaviour
    {
        [SerializeField] private float _islandRadius = 100f;
        [SerializeField] private float _islandDepth = 62f;

        private readonly Dictionary<string, IslanderView> _views = new();
        private readonly Dictionary<string, Vector3> _zoneCentres = new();

        private Transform _islanders;
        private IIslanderRegistry _registry;
        private IIslandService _island;
        private IPersonalityService _personalities;

        private Rng _rng = Rng.FromTime();
        private float _sinceCheck;

        private void OnEnable() => EventBus.Subscribe<GameLoaded>(OnGameLoaded);

        private void OnDisable()
        {
            EventBus.Unsubscribe<GameLoaded>(OnGameLoaded);
            EventBus.Unsubscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Unsubscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Unsubscribe<EmotionShown>(OnEmotionShown);
        }

        private void OnGameLoaded(GameLoaded _)
        {
            if (!ServiceRegistry.TryGet(out _registry)) return;
            if (!ServiceRegistry.TryGet(out _island)) return;
            ServiceRegistry.TryGet(out _personalities);

            BuildIsland();
            BuildZones();

            _islanders = new GameObject("Habitantes").transform;
            _islanders.SetParent(transform, worldPositionStays: false);

            foreach (var islander in _registry.All) Spawn(islander);

            EventBus.Subscribe<IslanderCreated>(OnIslanderCreated);
            EventBus.Subscribe<IslanderLeft>(OnIslanderLeft);
            EventBus.Subscribe<EmotionShown>(OnEmotionShown);
        }

        private void BuildIsland()
        {
            var island = new GameObject("Isla").transform;
            island.SetParent(transform, worldPositionStays: false);

            AddMesh(island, "prado", IslandMeshBuilder.BuildSurface(_islandRadius),
                    ToonPalette.Solid(ToonPalette.Grass));
            AddMesh(island, "roca", IslandMeshBuilder.BuildUnderside(_islandRadius, _islandDepth),
                    ToonPalette.Solid(ToonPalette.Rock));

            // El Árbol Nimbo: el corazón de la isla y su referencia visual. Va en el
            // centro porque es donde cae la plaza y donde mira la cámara al empezar.
            var (trunk, crown) = IslandMeshBuilder.BuildTree(26f, 9f);
            var tree = new GameObject("Árbol Nimbo").transform;
            tree.SetParent(island, worldPositionStays: false);
            AddMesh(tree, "tronco", trunk, ToonPalette.Solid(ToonPalette.TrunkBrown));
            AddMesh(tree, "copa", crown, ToonPalette.Solid(ToonPalette.LeafGreen));

            BuildClouds(island);
        }

        /// <summary>Nubes alrededor y por debajo: son las que venden que la isla flota.</summary>
        private void BuildClouds(Transform parent)
        {
            var clouds = new GameObject("Nubes").transform;
            clouds.SetParent(parent, worldPositionStays: false);

            var material = ToonPalette.Solid(ToonPalette.CloudWhite, smoothness: 0.02f);
            var blob = MeshShapes.Sphere(12, 9, new Vector3(1f, 0.55f, 1f));

            for (int i = 0; i < 26; i++)
            {
                float angle = _rng.Range(0f, Mathf.PI * 2f);
                float distance = _rng.Range(_islandRadius * 0.95f, _islandRadius * 1.9f);
                float y = _rng.Range(-_islandDepth * 0.85f, -4f);

                var cloud = new GameObject($"nube_{i}").transform;
                cloud.SetParent(clouds, worldPositionStays: false);
                cloud.localPosition = new Vector3(Mathf.Cos(angle) * distance, y,
                                                  Mathf.Sin(angle) * distance);
                cloud.localScale = Vector3.one * _rng.Range(14f, 38f);

                AddMesh(cloud, "masa", blob, material);
            }
        }

        private void BuildZones()
        {
            foreach (var zoneId in _island.ZoneIds)
                if (_island.TryGetSpawnPoint(zoneId, out var centre))
                    _zoneCentres[zoneId] = centre;
        }

        private static void AddMesh(Transform parent, string name, Mesh mesh, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, worldPositionStays: false);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = material;
        }

        private void OnIslanderCreated(IslanderCreated evt)
        {
            if (_registry.TryGet(evt.IslanderId, out var islander)) Spawn(islander);
        }

        private void OnIslanderLeft(IslanderLeft evt)
        {
            if (!_views.TryGetValue(evt.IslanderId, out var view)) return;
            Destroy(view.gameObject);
            _views.Remove(evt.IslanderId);
        }

        private void OnEmotionShown(EmotionShown evt)
        {
            if (_views.TryGetValue(evt.IslanderId, out var view)) view.SetEmotion(evt.Emotion);
        }

        private void Spawn(IslanderData islander)
        {
            if (_views.ContainsKey(islander.Id)) return;

            float speed = 3.2f;
            if (_personalities != null)
                speed *= _personalities.For(islander.Personality).WalkSpeedMultiplier;

            var view = IslanderView.Create(islander, speed, _islanders);
            view.PlaceAt(PointIn(islander.CurrentZoneId));
            view.SetEmotion(islander.Mood.Emotion);
            _views[islander.Id] = view;
        }

        private Vector3 PointIn(string zoneId)
        {
            if (!string.IsNullOrEmpty(zoneId) && _island.TryGetSpawnPoint(zoneId, out var point))
                return point;

            return _zoneCentres.TryGetValue("zona_plaza", out var plaza) ? plaza : Vector3.zero;
        }

        private void Update()
        {
            if (_registry == null) return;

            // Los destinos se comprueban dos veces por segundo: la simulación decide
            // por horas de juego, así que mirar cada fotograma no aportaría nada.
            _sinceCheck += Time.deltaTime;
            if (_sinceCheck < 0.5f) return;
            _sinceCheck = 0f;

            foreach (var islander in _registry.All)
            {
                if (!_views.TryGetValue(islander.Id, out var view)) continue;
                if (view.IsWalking) continue;

                var destination = PointIn(islander.CurrentZoneId);
                if ((destination - view.transform.position).sqrMagnitude > 9f)
                    view.WalkTo(destination);
            }
        }
    }
}
