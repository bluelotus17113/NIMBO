using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.World;
using UnityEngine;

namespace Nimbo.Gathering
{
    /// <summary>
    /// Implementación de <see cref="IGatheringService"/>: siembra la isla con nodos de
    /// recurso, los recoge y los repone con el paso de los días.
    /// </summary>
    /// <remarks>
    /// El servicio decide y guarda; no dibuja nada. Publica <c>NodeGathered</c> y
    /// <c>NodeRespawned</c>, y quien pinta la isla se entera por ahí.
    ///
    /// La siembra usa una semilla fija: la misma partida cargada dos veces produce
    /// exactamente la misma isla. Dos partidas nuevas de jugadores distintos comparten
    /// la misma disposición, y así el diseñador puede iterar sin sorpresas.
    /// </remarks>
    public class GatheringService : IGatheringService
    {
        // ── constantes ────────────────────────────────────────────────────────

        /// <summary>
        /// Semilla fija para el generador. Una constante y no <c>Rng.FromTime()</c>
        /// para que la misma partida cargada dos veces siembre la misma isla.
        /// </summary>
        const string GATHERING_SEED = "isla_nimbo_recoleccion_v1";

        const float IslandRadius = 100f;
        const float SpawnRadius = 95f;
        const float PlazaRadius = 30f;
        const float MinSeparation = 2.5f;
        const float MinSeparationSqr = MinSeparation * MinSeparation;
        const int TargetNodeCount = 120;
        const int MaxAttempts = TargetNodeCount * 20;

        // ── estado interno ──────────────────────────────────────────────────

        readonly NodeCatalog _catalog;
        readonly GatheringState _state;
        readonly IInventoryService _inventory;
        readonly GameClock _clock;
        readonly string _seed;

        List<ResourceNode> _nodes => _state.Nodes;

        // ── constructores ───────────────────────────────────────────────────

        /// <summary>
        /// Constructor de producción: usa la semilla fija.
        /// </summary>
        public GatheringService(NodeCatalog catalog, GatheringState state,
                                IInventoryService inventory, GameClock clock)
            : this(catalog, state, inventory, clock, GATHERING_SEED) { }

        /// <summary>
        /// Constructor con semilla explícita. Los tests lo usan para verificar
        /// que dos semillas distintas producen islas distintas.
        /// </summary>
        public GatheringService(NodeCatalog catalog, GatheringState state,
                                IInventoryService inventory, GameClock clock,
                                string seed)
        {
            _catalog = catalog;
            _state = state;
            _inventory = inventory;
            _clock = clock;
            _seed = seed;
        }

        // ── IGatheringService ───────────────────────────────────────────────

        public IReadOnlyList<NodeDefinition> Catalog => _catalog.All;

        public bool TryGetDefinition(string nodeId, out NodeDefinition definition)
        {
            return _catalog.TryGetDefinition(nodeId, out definition);
        }

        public IReadOnlyList<ResourceNode> Nodes
        {
            get
            {
                EnsureSeeded();
                return _nodes.AsReadOnly();
            }
        }

        public bool TryGetNode(string instanceId, out ResourceNode node)
        {
            EnsureSeeded();
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].InstanceId == instanceId)
                {
                    node = _nodes[i];
                    return true;
                }
            }
            node = null;
            return false;
        }

        // ── Gather ──────────────────────────────────────────────────────────

        public GatherResult Gather(string instanceId, ToolKind tool, out int dropped)
        {
            dropped = 0;

            EnsureSeeded();

            // 1. Buscar el nodo
            ResourceNode node = null;
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (_nodes[i].InstanceId == instanceId)
                {
                    node = _nodes[i];
                    break;
                }
            }
            if (node == null) return GatherResult.UnknownNode;

            // 2. ¿Ya estaba agotado?
            if (node.IsDepleted) return GatherResult.Depleted;

            // 3. ¿Herramienta correcta?
            if (!_catalog.TryGetDefinition(node.NodeId, out var def))
                return GatherResult.UnknownNode;

            // Si el nodo se coge a mano, cualquier herramienta vale
            if (def.RequiredTool != ToolKind.None && tool != def.RequiredTool)
                return GatherResult.WrongTool;

            // 4. Restar un golpe
            node.HitsLeft--;

            // 5. Si aún le quedan golpes, solo fue un Hit
            if (node.HitsLeft > 0) return GatherResult.Hit;

            // 6. Sortear la cantidad y meterla en la mochila.
            //    Usar un Rng con semilla del instanceId para que el resultado
            //    sea estable entre cargas si el jugador recarga sin guardar.
            var dropRng = Rng.FromSeed($"{instanceId}_drop_{node.RespawnOnDay}");
            int quantity = dropRng.Range(def.MinDrop, def.MaxDrop + 1);

            // 7. Intentar guardar en la mochila
            StoreResult storeResult = _inventory.TryStore(def.DropId, quantity, out int leftover);

            if (storeResult == StoreResult.Full)
            {
                // No cupo nada: devolver InventoryFull, dejar el nodo con un golpe
                node.HitsLeft = 1;
                // No se toca RespawnOnDay, no se publica NodeGathered
                return GatherResult.InventoryFull;
            }

            // Cupo total o parcial. Si fue parcial, leftover se queda sin entrar
            // pero el nodo ya está agotado. Lo que entró es quantity - leftover.
            int stored = quantity - leftover;

            // 8. Anotar respawn y publicar
            node.RespawnOnDay = def.RespawnDays > 0
                ? _clock.Day + def.RespawnDays
                : 0;

            if (stored > 0)
            {
                dropped = stored;
                EventBus.Publish(new NodeGathered(node.InstanceId, def.DropId, stored));
            }

            return GatherResult.Ok;
        }

        // ── AdvanceDay ──────────────────────────────────────────────────────

        public void AdvanceDay()
        {
            if (_nodes == null || _nodes.Count == 0) return;

            int today = _clock.Day;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];

                // Solo los agotados con día de respawn cumplido
                if (!node.IsDepleted) continue;
                if (node.RespawnOnDay <= 0) continue;
                if (node.RespawnOnDay > today) continue;

                // Devolverlo a la vida con los golpes originales
                if (_catalog.TryGetDefinition(node.NodeId, out var def))
                {
                    node.HitsLeft = def.Hits;
                }
                else
                {
                    // Si por algún motivo el catálogo ya no tiene esta definición,
                    // lo dejamos con 1 golpe para que al menos se pueda recoger.
                    node.HitsLeft = 1;
                }

                node.RespawnOnDay = 0;
                EventBus.Publish(new NodeRespawned(node.InstanceId));
            }
        }

        // ── siembra ─────────────────────────────────────────────────────────

        void EnsureSeeded()
        {
            if (_state.Seeded) return;

            // Semilla fija: Rng.FromSeed con constante, nunca Rng.FromTime()
            var rng = Rng.FromSeed(_seed);
            Seed(rng);

            _state.Seeded = true;
        }

        void Seed(Rng rng)
        {
            if (_nodes == null) return;

            // Construir la lista de tipos que se van a sembrar, repartiendo
            // equitativamente entre las definiciones del catálogo
            var allDefs = _catalog.All;
            if (allDefs.Count == 0) return;

            // Repartir los 120 nodos entre las definiciones en round-robin
            var nodeQueue = new List<NodeDefinition>(TargetNodeCount);
            for (int i = 0; i < TargetNodeCount; i++)
            {
                nodeQueue.Add(allDefs[i % allDefs.Count]);
            }

            // Barajar para que el orden de colocación no coincida con el de catálogo
            rng.Shuffle(nodeQueue);

            int placed = 0;
            int attempts = 0;
            var counters = new Dictionary<string, int>(); // para generar InstanceId estables

            while (placed < nodeQueue.Count && attempts < MaxAttempts)
            {
                attempts++;

                var def = nodeQueue[placed];

                // Posición aleatoria dentro del disco de radio SpawnRadius,
                // pero fuera del círculo central de radio PlazaRadius.
                // Generar en coordenadas polares para garantizar distribución uniforme.
                float angle = rng.NextFloat() * Mathf.PI * 2f;
                // Para distribución uniforme en el disco, usar sqrt del radio
                float radius = Mathf.Sqrt(rng.NextFloat()) * (SpawnRadius - PlazaRadius) + PlazaRadius;

                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                float y = 0f; // el mundo es plano; la altura la pone quien dibuja

                // Comprobar que no se solapa con ningún nodo ya puesto
                bool tooClose = false;
                for (int i = 0; i < _nodes.Count; i++)
                {
                    var existing = _nodes[i];
                    float dx = x - existing.X;
                    float dz = z - existing.Z;
                    if (dx * dx + dz * dz < MinSeparationSqr)
                    {
                        tooClose = true;
                        break;
                    }
                }

                if (tooClose) continue;

                // Generar InstanceId estable
                if (!counters.TryGetValue(def.NodeId, out int counter))
                    counter = 0;

                string instanceId;
                do
                {
                    instanceId = $"{def.NodeId}_{counter}";
                    counter++;
                } while (HasInstanceId(instanceId));

                counters[def.NodeId] = counter;

                var node = new ResourceNode
                {
                    InstanceId = instanceId,
                    NodeId = def.NodeId,
                    X = x,
                    Y = y,
                    Z = z,
                    Yaw = rng.NextFloat() * 360f,
                    HitsLeft = def.Hits,
                    RespawnOnDay = 0,
                };

                _nodes.Add(node);
                placed++;
            }
        }

        bool HasInstanceId(string instanceId)
        {
            for (int i = 0; i < _nodes.Count; i++)
                if (_nodes[i].InstanceId == instanceId) return true;
            return false;
        }
    }
}
