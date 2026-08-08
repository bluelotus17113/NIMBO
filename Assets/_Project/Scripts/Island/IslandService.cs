using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Util;
using Nimbo.Data.Housing;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Island.Zones;
using UnityEngine;

namespace Nimbo.Island
{
    /// <summary>
    /// La isla: qué zonas hay, cuáles están abiertas y dónde está cada habitante.
    /// </summary>
    /// <remarks>
    /// El desbloqueo se reevalúa cuando cambia algo que pueda haberlo provocado (llega
    /// alguien, alguien sube de nivel) y no cada fotograma. Con diez zonas daría igual,
    /// pero así el evento <c>BuildingUnlocked</c> sale en el instante en que el jugador
    /// hizo la cosa que lo abrió, que es cuando espera verlo.
    /// </remarks>
    public sealed class IslandService : IIslandService, IDisposable
    {
        private readonly IIslanderRegistry _registry;
        private readonly SaveGame _save;
        private readonly ZoneDefinition[] _zones;
        private readonly Dictionary<string, int> _zoneIndex;
        private readonly List<string> _zoneIds;

        private Rng _rng = Rng.FromTime();

        public IslandService(IIslanderRegistry registry, SaveGame save)
        {
            _registry = registry;
            _save = save;
            _zones = IslandLayout.FirstIsland();

            _zoneIndex = new Dictionary<string, int>(_zones.Length);
            _zoneIds = new List<string>(_zones.Length);
            for (int i = 0; i < _zones.Length; i++)
            {
                _zoneIndex[_zones[i].ZoneId] = i;
                _zoneIds.Add(_zones[i].ZoneId);
            }

            State.PopulationCap = IslandLayout.MaxResidents;
            SeedBuildingStates();

            EventBus.Subscribe<IslanderCreated>(OnRosterChanged);
            EventBus.Subscribe<IslanderLeft>(OnRosterChanged);
            EventBus.Subscribe<IslanderLeveledUp>(OnLeveledUp);
        }

        public void Dispose()
        {
            EventBus.Unsubscribe<IslanderCreated>(OnRosterChanged);
            EventBus.Unsubscribe<IslanderLeft>(OnRosterChanged);
            EventBus.Unsubscribe<IslanderLeveledUp>(OnLeveledUp);
        }

        public IslandState State => _save.Island;

        public IReadOnlyList<string> ZoneIds => _zoneIds;

        /// <summary>Solo las zonas abiertas: es lo que la IA puede elegir como destino.</summary>
        public IEnumerable<string> OpenZoneIds
        {
            get
            {
                for (int i = 0; i < _zones.Length; i++)
                    if (State.IsUnlocked(_zones[i].ZoneId)) yield return _zones[i].ZoneId;
            }
        }

        private void SeedBuildingStates()
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                if (State.IsUnlocked(_zones[i].ZoneId)) continue;
                if (Find(_zones[i].ZoneId) >= 0) continue;

                State.Buildings.Add(new BuildingState
                {
                    BuildingId = _zones[i].ZoneId,
                    Unlocked = _zones[i].Unlock.IsAlways,
                });
            }
            EvaluateUnlocks();
        }

        private int Find(string buildingId)
        {
            for (int i = 0; i < State.Buildings.Count; i++)
                if (State.Buildings[i].BuildingId == buildingId) return i;
            return -1;
        }

        private void OnRosterChanged<T>(T _) => EvaluateUnlocks();
        private void OnLeveledUp(IslanderLeveledUp _) => EvaluateUnlocks();

        /// <summary>Abre lo que se haya ganado desde la última vez. Es idempotente.</summary>
        public void EvaluateUnlocks()
        {
            RecalculateIslandLevel();

            int residents = _registry.Count;
            int topLevel = HighestLevel();

            for (int i = 0; i < _zones.Length; i++)
            {
                var zone = _zones[i];
                if (State.IsUnlocked(zone.ZoneId)) continue;

                var condition = zone.Unlock;
                if (residents < condition.MinResidents) continue;
                if (topLevel < condition.MinAnyLevel) continue;
                if (!string.IsNullOrEmpty(condition.RequiredFlag) &&
                    !_save.HasFlag(condition.RequiredFlag)) continue;

                Unlock(zone.ZoneId);
            }
        }

        /// <summary>
        /// El nivel de la isla sale de la suma de niveles de quienes viven en ella:
        /// pocos habitantes muy cuidados y muchos recién llegados son dos caminos
        /// válidos al mismo sitio, que es lo que pedía el diseño.
        /// </summary>
        private void RecalculateIslandLevel()
        {
            int total = 0;
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++) total += all[i].Progression.Level;

            State.Level = IslandState.LevelForTotal(total);
        }

        private int HighestLevel()
        {
            int highest = 0;
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Progression.Level > highest) highest = all[i].Progression.Level;
            return highest;
        }

        // --- IIslandService --------------------------------------------------

        public ZonePurpose PurposeOf(string zoneId) =>
            _zoneIndex.TryGetValue(zoneId, out int i) ? _zones[i].Purpose : ZonePurpose.Civic;

        public bool TryGetSpawnPoint(string zoneId, out Vector3 position)
        {
            if (!_zoneIndex.TryGetValue(zoneId, out int i))
            {
                position = Vector3.zero;
                return false;
            }

            var zone = _zones[i];

            // Punto al azar dentro del círculo, con raíz cuadrada para que no se
            // amontonen todos en el centro: sin ella el reparto no es uniforme por área.
            float angle = _rng.Range(0f, Mathf.PI * 2f);
            float radius = zone.Radius * Mathf.Sqrt(_rng.NextFloat());
            position = zone.Center + new Vector3(Mathf.Cos(angle) * radius, 0f,
                                                 Mathf.Sin(angle) * radius);
            return true;
        }

        /// <summary>La isla es pequeña y no hay obstáculos: desde cualquier zona abierta se llega a todas.</summary>
        public IEnumerable<string> ReachableFrom(string zoneId)
        {
            foreach (var id in OpenZoneIds)
                if (id != zoneId) yield return id;
        }

        public bool IsUnlocked(string buildingId) => State.IsUnlocked(buildingId);

        public bool Unlock(string buildingId)
        {
            int i = Find(buildingId);
            if (i < 0)
            {
                Debug.LogError($"IslandService: {buildingId} no es una zona de esta isla");
                return false;
            }
            if (State.Buildings[i].Unlocked) return false;

            var building = State.Buildings[i];
            building.Unlocked = true;
            State.Buildings[i] = building;

            EventBus.Publish(new BuildingUnlocked(buildingId));
            return true;
        }

        public void SendTo(string islanderId, string zoneId)
        {
            if (!_registry.TryGet(islanderId, out var islander)) return;
            if (!State.IsUnlocked(zoneId))
            {
                Debug.LogWarning($"IslandService: {zoneId} está cerrada, {islanderId} no va");
                return;
            }
            islander.CurrentZoneId = zoneId;
        }

        // --- vivienda ---------------------------------------------------------

        /// <summary>
        /// Le da la primera casa libre. Devuelve false si no queda ninguna, que es la
        /// forma de saber que la isla está llena.
        /// </summary>
        public bool TryAssignHome(IslanderData islander)
        {
            for (int i = 0; i < _zones.Length; i++)
            {
                var zone = _zones[i];
                if (zone.HousingUnits == 0 || !State.IsUnlocked(zone.ZoneId)) continue;

                for (int unit = 0; unit < zone.HousingUnits; unit++)
                {
                    if (IsUnitTaken(zone.ZoneId, unit)) continue;

                    islander.Home = new HomeAssignment { BuildingId = zone.ZoneId, UnitIndex = unit };
                    islander.CurrentZoneId = zone.ZoneId;

                    if (_save.FindHome(zone.ZoneId, unit) == null)
                        _save.Homes.Add(new HomeRecord
                        {
                            BuildingId = zone.ZoneId,
                            UnitIndex = unit,
                            Layout = RoomLayout.Starter(),
                        });

                    EventBus.Publish(new IslanderMovedIn(islander.Id, zone.ZoneId, unit));
                    return true;
                }
            }
            return false;
        }

        private bool IsUnitTaken(string buildingId, int unitIndex)
        {
            var all = _registry.All;
            for (int i = 0; i < all.Count; i++)
                if (all[i].Home.BuildingId == buildingId && all[i].Home.UnitIndex == unitIndex)
                    return true;
            return false;
        }

        /// <summary>Casas libres que quedan. La interfaz lo enseña antes de crear a nadie.</summary>
        public int FreeHomes()
        {
            int free = 0;
            for (int i = 0; i < _zones.Length; i++)
            {
                var zone = _zones[i];
                if (zone.HousingUnits == 0 || !State.IsUnlocked(zone.ZoneId)) continue;

                for (int unit = 0; unit < zone.HousingUnits; unit++)
                    if (!IsUnitTaken(zone.ZoneId, unit)) free++;
            }
            return free;
        }

        public ZoneDefinition ZoneAt(int index) => _zones[index];
        public int ZoneCount => _zones.Length;
    }
}
