using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Economy;
using Nimbo.Data.World;
using Nimbo.Gathering;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests de recolección para el módulo Gathering. No dependen de Unity Resources:
    /// usan un catálogo construido en memoria con JSON mínimo.
    /// </summary>
    public class GatheringTests
    {
        // ── JSON mínimo para los tests ──────────────────────────────────────
        //     Dos nodos de mano (zarza y flor), uno de hacha (roble) y uno de
        //     pico (roca). Suficiente para cubrir todos los caminos de Gather
        //     sin cargarse los tests de legibilidad.

        const string CATALOG_JSON = @"{
            ""version"": 1,
            ""items"": [
                { ""nodeId"": ""node_zarza"",   ""displayName"": ""Zarza"",
                  ""kind"": ""Bush"", ""requiredTool"": ""None"", ""dropId"": ""mat_fibra"",
                  ""minDrop"": 1, ""maxDrop"": 2, ""hits"": 1, ""respawnDays"": 1 },
                { ""nodeId"": ""node_flor"",    ""displayName"": ""Flor"",
                  ""kind"": ""Flower"", ""requiredTool"": ""None"", ""dropId"": ""mat_petalo"",
                  ""minDrop"": 1, ""maxDrop"": 2, ""hits"": 1, ""respawnDays"": 2 },
                { ""nodeId"": ""node_roble"",   ""displayName"": ""Roble"",
                  ""kind"": ""Tree"", ""requiredTool"": ""Axe"", ""dropId"": ""mat_madera"",
                  ""minDrop"": 3, ""maxDrop"": 3, ""hits"": 3, ""respawnDays"": 5 },
                { ""nodeId"": ""node_roca"",    ""displayName"": ""Roca"",
                  ""kind"": ""Rock"", ""requiredTool"": ""Pickaxe"", ""dropId"": ""mat_piedra"",
                  ""minDrop"": 2, ""maxDrop"": 2, ""hits"": 4, ""respawnDays"": 7 },
                { ""nodeId"": ""node_efimero"", ""displayName"": ""Efímero"",
                  ""kind"": ""Herb"", ""requiredTool"": ""None"", ""dropId"": ""mat_fibra"",
                  ""minDrop"": 1, ""maxDrop"": 1, ""hits"": 1, ""respawnDays"": 0 }
            ]
        }";

        // ── setup ───────────────────────────────────────────────────────────

        NodeCatalog _catalog;
        GatheringState _state;
        GatheringTestInventory _inventory;
        GameClock _clock;
        GatheringService _service;

        [SetUp]
        public void SetUp()
        {
            _catalog = new NodeCatalog(CATALOG_JSON);
            _state = new GatheringState();
            _inventory = new GatheringTestInventory();
            _clock = new GameClock();
            _service = new GatheringService(_catalog, _state, _inventory, _clock);
        }

        /// <summary>Fuerza la siembra accediendo a Nodes.</summary>
        void EnsureSeeded()
        {
            // acceder a Nodes dispara EnsureSeeded
            _ = _service.Nodes;
        }

        /// <summary>Busca un nodo por NodeId en la lista sembrada.</summary>
        ResourceNode FindByNodeId(string nodeId)
        {
            foreach (var n in _service.Nodes)
                if (n.NodeId == nodeId) return n;
            return null;
        }

        // ── apartar por un edificio ──────────────────────────────────────

        /// <summary>
        /// Colocar un edificio encima de una arboleda la aparta, en vez de dejarla
        /// atravesando el tejado.
        /// </summary>
        /// <remarks>
        /// La siembra esquiva los edificios que ya están, pero el jugador los mueve
        /// después. Sin esta segunda respuesta, arrastrar la panadería sobre tres
        /// robles los dejaba dentro de la pared — y ahí no se pueden talar, porque
        /// para darle a un nodo hay que ponerse delante y delante hay un muro.
        /// </remarks>
        [Test]
        public void ClearAround_ApartaLoQueQuedaDebajoYNoPierdeNiUnNodo()
        {
            EnsureSeeded();
            int total = _service.Nodes.Count;
            Assert.Greater(total, 0);

            // Un sitio donde seguro que hay algo: encima de un nodo cualquiera.
            var victima = _state.Nodes[0];
            var centro = new UnityEngine.Vector3(victima.X, 0f, victima.Z);
            const float Radio = 6f;

            int movidos = _service.ClearAround(centro, Radio);

            Assert.Greater(movidos, 0, "no ha apartado ni el que estaba justo debajo");
            Assert.AreEqual(total, _service.Nodes.Count,
                            "apartar ha perdido nodos por el camino");

            foreach (var nodo in _service.Nodes)
            {
                float dx = nodo.X - centro.x;
                float dz = nodo.Z - centro.z;
                Assert.GreaterOrEqual(dx * dx + dz * dz, Radio * Radio - 0.01f,
                                      $"{nodo.InstanceId} sigue debajo del edificio");
            }
        }

        [Test]
        public void ClearAround_NoTocaLoQueEstaLejos()
        {
            EnsureSeeded();

            var lejos = new UnityEngine.Vector3(0f, 0f, 0f); // la plaza: ahí no se siembra
            var antes = new List<(float x, float z)>();
            foreach (var nodo in _service.Nodes) antes.Add((nodo.X, nodo.Z));

            _service.ClearAround(lejos, 5f);

            int i = 0;
            foreach (var nodo in _service.Nodes)
            {
                Assert.AreEqual(antes[i].x, nodo.X, 0.0001f);
                Assert.AreEqual(antes[i].z, nodo.Z, 0.0001f);
                i++;
            }
        }

        // ── 1. sembrar deja unos 120 nodos y pone Seeded ──────────────────

        [Test]
        public void Seed_CreatesAround120Nodes()
        {
            EnsureSeeded();
            Assert.IsTrue(_state.Seeded, "Seeded debería ser true tras sembrar");
            Assert.GreaterOrEqual(_service.Nodes.Count, 110,
                "debería haber al menos 110 nodos");
            Assert.LessOrEqual(_service.Nodes.Count, 130,
                "debería haber como mucho 130 nodos");
            // con 16 definiciones y 120 intentos deberían ser 120 justos,
            // ya que el catálogo de test tiene solo 5
            Assert.AreEqual(_catalog.Count > 0 ? 120 : 0, _service.Nodes.Count,
                "con espacio de sobra deberían entrar los 120");
        }

        // ── 2. partida ya sembrada no vuelve a sembrar ────────────────────

        [Test]
        public void Seed_AlreadySeeded_DoesNotReseed()
        {
            EnsureSeeded();
            int count = _service.Nodes.Count;
            Assert.IsTrue(_state.Seeded);

            // acceder otra vez no debería cambiar nada
            _ = _service.Nodes;
            Assert.AreEqual(count, _service.Nodes.Count,
                "no debería añadir nodos si ya está sembrado");
        }

        // ── 3. ningún nodo en el círculo central ni fuera del disco ──────

        [Test]
        public void Seed_NodesWithinBounds()
        {
            EnsureSeeded();
            foreach (var node in _service.Nodes)
            {
                float distSqr = node.X * node.X + node.Z * node.Z;
                Assert.GreaterOrEqual(distSqr, 30f * 30f,
                    $"{node.InstanceId} está dentro de la plaza (radio 30)");
                Assert.LessOrEqual(distSqr, 95f * 95f,
                    $"{node.InstanceId} está fuera del disco de spawn (radio 95)");
            }
        }

        // ── 4. no hay dos nodos a menos de 2,5 m ─────────────────────────

        [Test]
        public void Seed_NoOverlaps()
        {
            EnsureSeeded();
            var nodes = _service.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                for (int j = i + 1; j < nodes.Count; j++)
                {
                    float dx = nodes[i].X - nodes[j].X;
                    float dz = nodes[i].Z - nodes[j].Z;
                    float distSqr = dx * dx + dz * dz;
                    Assert.GreaterOrEqual(distSqr, 2.5f * 2.5f,
                        $"{nodes[i].InstanceId} y {nodes[j].InstanceId} están a menos de 2.5 m");
                }
            }
        }

        // ── 5. misma semilla = misma isla; distinta semilla = distinta ───

        [Test]
        public void Seed_SameSeed_SameIsland()
        {
            EnsureSeeded();

            var state2 = new GatheringState();
            var service2 = new GatheringService(_catalog, state2,
                _inventory, new GameClock());
            _ = service2.Nodes;

            Assert.AreEqual(_service.Nodes.Count, service2.Nodes.Count,
                "misma semilla debería dar el mismo número de nodos");
            for (int i = 0; i < _service.Nodes.Count; i++)
            {
                Assert.AreEqual(_service.Nodes[i].NodeId, service2.Nodes[i].NodeId,
                    $"nodo {i}: mismo NodeId");
                Assert.AreEqual(_service.Nodes[i].X, service2.Nodes[i].X, 0.001f,
                    $"nodo {i}: misma X");
                Assert.AreEqual(_service.Nodes[i].Z, service2.Nodes[i].Z, 0.001f,
                    $"nodo {i}: misma Z");
            }
        }

        [Test]
        public void Seed_DifferentSeed_DifferentIsland()
        {
            EnsureSeeded();

            var state2 = new GatheringState();
            var service2 = new GatheringService(_catalog, state2,
                _inventory, new GameClock(), "semilla_distinta_v1");
            _ = service2.Nodes;

            // Con semillas distintas, al menos una posición debería diferir
            bool anyDifferent = false;
            for (int i = 0; i < _service.Nodes.Count; i++)
            {
                if (System.Math.Abs(_service.Nodes[i].X - service2.Nodes[i].X) > 0.01f
                    || System.Math.Abs(_service.Nodes[i].Z - service2.Nodes[i].Z) > 0.01f)
                {
                    anyDifferent = true;
                    break;
                }
            }
            Assert.IsTrue(anyDifferent,
                "dos semillas distintas deberían producir islas distintas");
        }

        // ── 6. WrongTool devuelve WrongTool y no gasta golpes ────────────

        [Test]
        public void Gather_WrongTool_ReturnsWrongTool()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_roble"); // pide Axe
            Assert.IsNotNull(node, "debería existir al menos un roble");
            int hitsBefore = node.HitsLeft;

            int dropped;
            var result = _service.Gather(node.InstanceId, ToolKind.Pickaxe, out dropped);

            Assert.AreEqual(GatherResult.WrongTool, result);
            Assert.AreEqual(0, dropped);
            Assert.AreEqual(hitsBefore, node.HitsLeft,
                "los golpes no deberían cambiar con herramienta incorrecta");
        }

        // ── 7. nodo de mano: ToolKind.None Y también con hacha ───────────

        [Test]
        public void Gather_HandNode_WithNone_Succeeds()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_zarza"); // pide None
            Assert.IsNotNull(node, "debería existir al menos una zarza");
            _inventory.Capacity = 99;

            int dropped;
            var result = _service.Gather(node.InstanceId, ToolKind.None, out dropped);

            Assert.AreEqual(GatherResult.Ok, result,
                "un nodo de mano debería recogerse con ToolKind.None");
            Assert.Greater(dropped, 0, "debería soltar al menos 1");
        }

        [Test]
        public void Gather_HandNode_WithAxe_Succeeds()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_zarza"); // pide None
            Assert.IsNotNull(node, "debería existir al menos una zarza");
            _inventory.Capacity = 99;

            int dropped;
            var result = _service.Gather(node.InstanceId, ToolKind.Axe, out dropped);

            Assert.AreEqual(GatherResult.Ok, result,
                "un nodo de mano debería recogerse también llevando hacha");
            Assert.Greater(dropped, 0, "debería soltar al menos 1");
        }

        // ── 8. nodo de 3 golpes: Hit dos veces, Ok a la tercera ─────────

        [Test]
        public void Gather_MultiHit_ReturnsHitThenOk()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_roble"); // 3 hits, drop 3-3
            Assert.IsNotNull(node, "debería existir al menos un roble");
            Assert.AreEqual(3, node.HitsLeft);
            _inventory.Capacity = 99;

            // Golpe 1 → Hit
            int dropped;
            var r1 = _service.Gather(node.InstanceId, ToolKind.Axe, out dropped);
            Assert.AreEqual(GatherResult.Hit, r1);
            Assert.AreEqual(0, dropped);
            Assert.AreEqual(2, node.HitsLeft);

            // Golpe 2 → Hit
            var r2 = _service.Gather(node.InstanceId, ToolKind.Axe, out dropped);
            Assert.AreEqual(GatherResult.Hit, r2);
            Assert.AreEqual(0, dropped);
            Assert.AreEqual(1, node.HitsLeft);

            // Golpe 3 → Ok, suelta material
            var r3 = _service.Gather(node.InstanceId, ToolKind.Axe, out dropped);
            Assert.AreEqual(GatherResult.Ok, r3);
            Assert.AreEqual(3, dropped, "el roble de test suelta 3 maderas fijas");
            Assert.AreEqual(0, node.HitsLeft);
            Assert.IsTrue(node.IsDepleted);
        }

        // ── 9. nodo agotado devuelve Depleted ────────────────────────────

        [Test]
        public void Gather_Depleted_ReturnsDepleted()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_zarza"); // 1 hit
            Assert.IsNotNull(node);
            _inventory.Capacity = 99;

            // Agotarlo
            int dropped;
            _service.Gather(node.InstanceId, ToolKind.None, out dropped);
            Assert.IsTrue(node.IsDepleted);

            // Intentar de nuevo
            var result = _service.Gather(node.InstanceId, ToolKind.None, out dropped);
            Assert.AreEqual(GatherResult.Depleted, result);
            Assert.AreEqual(0, dropped);
        }

        // ── 10. mochila llena: InventoryFull, nodo a 1 golpe, sin perder ─

        [Test]
        public void Gather_InventoryFull_ReturnsInventoryFull()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_zarza"); // 1 hit
            Assert.IsNotNull(node);
            _inventory.Capacity = 0; // mochila llena

            int dropped;
            var result = _service.Gather(node.InstanceId, ToolKind.None, out dropped);

            Assert.AreEqual(GatherResult.InventoryFull, result);
            Assert.AreEqual(0, dropped);
            Assert.AreEqual(1, node.HitsLeft,
                "el nodo debería quedar con 1 golpe, no agotado");
            Assert.IsFalse(node.IsDepleted);
            Assert.AreEqual(0, node.RespawnOnDay,
                "no debería haberse puesto día de respawn");
            Assert.AreEqual(0, _inventory.TotalStored,
                "no debería haberse guardado nada en la mochila");

            // El jugador vuelve con hueco y lo recoge
            _inventory.Capacity = 99;
            var r2 = _service.Gather(node.InstanceId, ToolKind.None, out dropped);
            Assert.AreEqual(GatherResult.Ok, r2);
            Assert.Greater(dropped, 0);
            Assert.IsTrue(node.IsDepleted);
        }

        // ── 11. AdvanceDay repone justo el día que toca ──────────────────

        [Test]
        public void AdvanceDay_RespawnsOnExactDay()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_zarza"); // respawnDays: 1
            Assert.IsNotNull(node);
            _inventory.Capacity = 99;

            // Agotarlo hoy
            int dropped;
            _service.Gather(node.InstanceId, ToolKind.None, out dropped);
            Assert.IsTrue(node.IsDepleted);

            int respawnDay = node.RespawnOnDay;
            Assert.Greater(respawnDay, _clock.Day);

            // Mismo día: no repone
            _service.AdvanceDay();
            Assert.IsTrue(node.IsDepleted, "mismo día no debería reponer");

            // Avanzar justo al día de respawn
            _clock.Advance((respawnDay - _clock.Day) * GameClock.MinutesPerDay);
            _service.AdvanceDay();
            Assert.IsFalse(node.IsDepleted, "el día de respawn debería revivir el nodo");
            Assert.AreEqual(1, node.HitsLeft,
                "debería recuperar sus golpes originales");
        }

        // ── 12. adelantar diez días repone todo ──────────────────────────

        [Test]
        public void AdvanceDay_MultipleDays_RespawnsAll()
        {
            EnsureSeeded();
            _inventory.Capacity = 99;

            // Agotar varios nodos
            var zarza = FindByNodeId("node_zarza"); // respawnDays: 1
            var flor = FindByNodeId("node_flor");   // respawnDays: 2
            Assert.IsNotNull(zarza);
            Assert.IsNotNull(flor);

            int dropped;
            _service.Gather(zarza.InstanceId, ToolKind.None, out dropped);
            _service.Gather(flor.InstanceId, ToolKind.None, out dropped);
            Assert.IsTrue(zarza.IsDepleted);
            Assert.IsTrue(flor.IsDepleted);

            // Adelantar 10 días de golpe
            _clock.Advance(10 * GameClock.MinutesPerDay);
            _service.AdvanceDay();

            Assert.IsFalse(zarza.IsDepleted,
                "zarza (1 día) debería haberse repuesto tras 10 días");
            Assert.IsFalse(flor.IsDepleted,
                "flor (2 días) debería haberse repuesto tras 10 días");
        }

        // ── 13. respawnDays 0 no vuelve nunca ────────────────────────────

        [Test]
        public void AdvanceDay_RespawnZero_NeverRespawns()
        {
            EnsureSeeded();
            var node = FindByNodeId("node_efimero"); // respawnDays: 0
            Assert.IsNotNull(node, "debería existir al menos un nodo efímero");
            _inventory.Capacity = 99;

            int dropped;
            _service.Gather(node.InstanceId, ToolKind.None, out dropped);
            Assert.IsTrue(node.IsDepleted);
            Assert.AreEqual(0, node.RespawnOnDay,
                "con respawnDays 0, RespawnOnDay debería ser 0");

            // Avanzar muchos días
            _clock.Advance(30 * GameClock.MinutesPerDay);
            _service.AdvanceDay();

            Assert.IsTrue(node.IsDepleted,
                "un nodo con respawnDays 0 no debería reponerse nunca");
        }

        // ── 14. InstanceId no se repiten ─────────────────────────────────

        [Test]
        public void Seed_NoDuplicateInstanceIds()
        {
            EnsureSeeded();
            var ids = new HashSet<string>();
            foreach (var node in _service.Nodes)
            {
                Assert.IsFalse(ids.Contains(node.InstanceId),
                    $"InstanceId duplicado: {node.InstanceId}");
                ids.Add(node.InstanceId);
            }
        }

        // ── 15. UnknownNode ──────────────────────────────────────────────

        [Test]
        public void Gather_UnknownNode_ReturnsUnknownNode()
        {
            EnsureSeeded();
            int dropped;
            var result = _service.Gather("no_existe", ToolKind.None, out dropped);
            Assert.AreEqual(GatherResult.UnknownNode, result);
            Assert.AreEqual(0, dropped);
        }

        // ── 16. TryGetNode y TryGetDefinition ────────────────────────────

        [Test]
        public void TryGetNode_ExistingAndMissing()
        {
            EnsureSeeded();
            var first = _service.Nodes[0];

            Assert.IsTrue(_service.TryGetNode(first.InstanceId, out var found));
            Assert.AreEqual(first.InstanceId, found.InstanceId);

            Assert.IsFalse(_service.TryGetNode("no_existe", out _));
        }

        [Test]
        public void TryGetDefinition_ExistingAndMissing()
        {
            Assert.IsTrue(_service.TryGetDefinition("node_zarza", out var def));
            Assert.AreEqual("node_zarza", def.NodeId);
            Assert.AreEqual(NodeKind.Bush, def.Kind);

            Assert.IsFalse(_service.TryGetDefinition("no_existe", out _));
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Dobles de prueba
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Doble de IInventoryService para tests de recolección.
    /// Solo implementa de verdad TryStore; el resto son stubs mínimos.
    /// </summary>
    sealed class GatheringTestInventory : IInventoryService
    {
        public int Capacity = 99; // cuántas unidades caben en total
        public int TotalStored { get; private set; }
        public string LastStoredId { get; private set; }

        public int SlotCount => 24;
        public int SelectedSlot => 0;
        public int HotbarSize => 8;

        public ItemStack At(int slot) => default;
        public ItemStack InHand => default;
        public ToolKind ToolInHand => ToolKind.None;
        public IReadOnlyList<ItemStack> Slots => System.Array.Empty<ItemStack>();

        public void Select(int slot) { }
        public void Swap(int a, int b) { }
        public bool Drop(int slot) => false;
        public bool TryConsumeSelected(int quantity = 1) => false;

        public int CountOf(string catalogId) => 0;
        public int StackLimitOf(string catalogId) => 999;

        public bool TryTake(string catalogId, int quantity = 1) => false;

        public StoreResult TryStore(string catalogId, int quantity, out int leftover)
        {
            if (Capacity <= 0)
            {
                leftover = quantity;
                return StoreResult.Full;
            }

            int canFit = Capacity - TotalStored;
            if (canFit <= 0)
            {
                leftover = quantity;
                return StoreResult.Full;
            }

            int stored = quantity <= canFit ? quantity : canFit;
            TotalStored += stored;
            LastStoredId = catalogId;
            leftover = quantity - stored;
            return leftover > 0 ? StoreResult.Partial : StoreResult.Ok;
        }
    }
}
