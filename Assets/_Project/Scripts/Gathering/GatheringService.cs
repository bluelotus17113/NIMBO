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

        /// <summary>
        /// El sitio de un edificio, donde no se siembra nada.
        /// </summary>
        /// <remarks>
        /// La parcela de un edificio es de dos casillas de cuatro metros —ocho por
        /// ocho— y el alero del tejado asoma metro y medio por cada lado. Seis metros
        /// desde el centro cubren la parcela entera con su vuelo. Con menos, salían
        /// robles atravesando el tejado de la panadería; sin nada, que es como estaba,
        /// pasaba de vez en cuando por pura suerte.
        /// </remarks>
        const float BuildingClearance = 6f;

        // ── estado interno ──────────────────────────────────────────────────

        readonly NodeCatalog _catalog;
        readonly GatheringState _state;
        readonly IInventoryService _inventory;
        readonly GameClock _clock;
        readonly string _seed;

        /// <summary>
        /// Quién sabe dónde está cada edificio, para no sembrar encima.
        /// </summary>
        /// <remarks>
        /// Es <c>IBuildService</c> y no <c>IIslandService</c>, y la diferencia costó una
        /// prueba en rojo: <c>IIslandService.TryGetSpawnPoint</c> devuelve un punto **al
        /// azar** dentro del círculo de la zona —es donde plantar a un vecino que va a
        /// esa zona, no dónde está el edificio— y las zonas miden de diez a veinte
        /// metros de radio. Preguntándole a ese, la comprobación daba un sitio distinto
        /// cada vez y no esquivaba nada.
        ///
        /// Opcional: los tests siembran sin aldea, y una isla sin edificios es una isla
        /// donde se puede sembrar en cualquier parte.
        /// </remarks>
        readonly IBuildService _build;

        List<ResourceNode> _nodes => _state.Nodes;

        // ── constructores ───────────────────────────────────────────────────

        /// <summary>
        /// Constructor de producción: usa la semilla fija.
        /// </summary>
        public GatheringService(NodeCatalog catalog, GatheringState state,
                                IInventoryService inventory, GameClock clock,
                                IBuildService build = null)
            : this(catalog, state, inventory, clock, GATHERING_SEED, build) { }

        /// <summary>
        /// Constructor con semilla explícita. Los tests lo usan para verificar
        /// que dos semillas distintas producen islas distintas.
        /// </summary>
        public GatheringService(NodeCatalog catalog, GatheringState state,
                                IInventoryService inventory, GameClock clock,
                                string seed, IBuildService build = null)
        {
            _catalog = catalog;
            _state = state;
            _inventory = inventory;
            _clock = clock;
            _seed = seed;
            _build = build;
        }

        // ── IGatheringService ───────────────────────────────────────────────

        public IReadOnlyList<NodeDefinition> Catalog => _catalog.All;

        public bool TryGetDefinition(string nodeId, out NodeDefinition definition)
        {
            return _catalog.TryGetDefinition(nodeId, out definition);
        }

        /// <summary>
        /// Todo lo sembrado. La envoltura de solo lectura se guarda en vez de crearse
        /// en cada llamada.
        /// </summary>
        /// <remarks>
        /// <c>AsReadOnly()</c> asigna un objeto nuevo cada vez, y quien pregunta es la
        /// interacción del jugador seis veces por segundo mientras andas: era basura
        /// nueva en el montón siete veces por segundo durante toda la partida. La
        /// envoltura mira a la lista viva, así que sigue viéndose lo que cambia.
        /// </remarks>
        public IReadOnlyList<ResourceNode> Nodes
        {
            get
            {
                EnsureSeeded();
                return _readOnlyNodes ??= _nodes.AsReadOnly();
            }
        }

        IReadOnlyList<ResourceNode> _readOnlyNodes;

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

        /// <summary>
        /// ¿Se ha ganado ya eso el protagonista?
        /// </summary>
        /// <remarks>
        /// Se pregunta al registro y no se pide por el constructor: la recolección se
        /// monta igual que siempre, las pruebas que la construyen a mano no cambian, y
        /// una partida sin progresión juega con los números de salida.
        /// </remarks>
        bool IsUnlocked(Unlock unlock) =>
            Core.Services.ServiceRegistry.TryGet<IPlayerProgression>(out var progression)
            && progression.IsUnlocked(unlock);

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

            // 4. Restar el golpe, y los que se hayan ganado por otro lado: uno por
            // Recolección 5 y otro por llevar la herramienta buena (§12.4). Solo en lo
            // que aguanta más de un golpe —quitarle uno a una flor que se coge a mano
            // no significa nada— y nunca por debajo de uno, o habría nodos que se
            // agotarían sin llegar a tocarlos.
            int blow = 1;
            if (def.Hits > 1)
            {
                if (IsUnlocked(Unlock.StrongArms)) blow++;
                if (_inventory.ToolTierInHand >= 2 && tool != ToolKind.None) blow++;
                if (blow >= def.Hits) blow = def.Hits - 1;
            }

            node.HitsLeft -= blow;
            if (node.HitsLeft < 0) node.HitsLeft = 0;

            // 5. Si aún le quedan golpes, solo fue un Hit
            if (node.HitsLeft > 0)
            {
                EventBus.Publish(new NodeHit(node.InstanceId, node.NodeId, node.HitsLeft));
                return GatherResult.Hit;
            }

            // 6. Sortear la cantidad y meterla en la mochila.
            //    Usar un Rng con semilla del instanceId para que el resultado
            //    sea estable entre cargas si el jugador recarga sin guardar.
            var dropRng = Rng.FromSeed($"{instanceId}_drop_{node.RespawnOnDay}");
            int quantity = dropRng.Range(def.MinDrop, def.MaxDrop + 1);

            // Manos finas: el doble en lo que se coge agachándose. No en árboles ni en
            // rocas, que ya tienen su premio en el golpe que se ahorran.
            if (IsUnlocked(Unlock.DeftHands) &&
                def.Kind is NodeKind.Flower or NodeKind.Herb or NodeKind.Bush)
                quantity *= 2;

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

            // 8. Anotar respawn y publicar. Con Recolección 6 vuelve un día antes, pero
            // nunca el mismo día: un nodo que se repone al instante deja de ser un
            // sitio al que volver y pasa a ser un botón.
            int wait = def.RespawnDays;
            if (wait > 1 && IsUnlocked(Unlock.FastRegrowth)) wait--;

            node.RespawnOnDay = def.RespawnDays > 0 ? _clock.Day + wait : 0;

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

                // Y que no caiga en el sitio de un edificio. La plaza ya está fuera por
                // el radio central, pero las diez zonas están repartidas entre los
                // treinta y los ochenta metros: justo en la corona donde se siembra.
                if (OnABuilding(x, z)) continue;

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

        /// <summary>¿Ese punto cae en la parcela de algún edificio de la aldea?</summary>
        bool OnABuilding(float x, float z)
        {
            if (_build == null) return false;

            // Movable son todas menos la plaza, y la plaza ya queda fuera por el radio
            // central: entre las dos listas no se escapa ningún edificio.
            var zones = _build.Movable;
            for (int i = 0; i < zones.Count; i++)
            {
                if (!_build.TryGetWorldCentre(zones[i], out var centre)) continue;

                float dx = x - centre.x;
                float dz = z - centre.z;
                if (dx * dx + dz * dz < BuildingClearance * BuildingClearance) return true;
            }
            return false;
        }

        // ── apartar ─────────────────────────────────────────────────────────

        public int ClearAround(Vector3 centre, float radius)
        {
            EnsureSeeded();

            float radiusSqr = radius * radius;
            int moved = 0;

            for (int i = 0; i < _nodes.Count; i++)
            {
                var node = _nodes[i];

                float dx = node.X - centre.x;
                float dz = node.Z - centre.z;
                if (dx * dx + dz * dz >= radiusSqr) continue;

                if (!TryFindSpotOutside(centre, radius, node, out float x, out float z)) continue;

                node.X = x;
                node.Z = z;
                moved++;

                EventBus.Publish(new NodeMoved(node.InstanceId));
            }

            return moved;
        }

        /// <summary>
        /// Un hueco justo fuera del círculo, dando la vuelta hasta encontrarlo.
        /// </summary>
        /// <remarks>
        /// Se sale por donde ya estaba: se empuja en la dirección en la que el nodo se
        /// encontraba respecto al centro y desde ahí se prueban vueltas de treinta
        /// grados. Empujar siempre hacia el mismo lado amontonaría toda la arboleda
        /// desplazada en la misma esquina del edificio, y se vería.
        ///
        /// Si no hay hueco —está rodeado de otros nodos, o se sale de la isla— se
        /// queda donde está. Un árbol bajo un alero se ve raro; un árbol en el vacío,
        /// más.
        /// </remarks>
        bool TryFindSpotOutside(Vector3 centre, float radius, ResourceNode node,
                                out float x, out float z)
        {
            var away = new Vector2(node.X - centre.x, node.Z - centre.z);
            if (away.sqrMagnitude < 0.01f) away = Vector2.right;
            away.Normalize();

            float distance = radius + MinSeparation;

            for (int turn = 0; turn < 12; turn++)
            {
                var direction = Quaternion.Euler(0f, turn * 30f, 0f) *
                                new Vector3(away.x, 0f, away.y);

                x = centre.x + direction.x * distance;
                z = centre.z + direction.z * distance;

                if (x * x + z * z > SpawnRadius * SpawnRadius) continue;
                if (OnABuilding(x, z)) continue;
                if (TooCloseToAnother(x, z, node)) continue;

                return true;
            }

            x = node.X;
            z = node.Z;
            return false;
        }

        bool TooCloseToAnother(float x, float z, ResourceNode self)
        {
            for (int i = 0; i < _nodes.Count; i++)
            {
                var other = _nodes[i];
                if (ReferenceEquals(other, self)) continue;

                float dx = x - other.X;
                float dz = z - other.Z;
                if (dx * dx + dz * dz < MinSeparationSqr) return true;
            }
            return false;
        }
    }
}
