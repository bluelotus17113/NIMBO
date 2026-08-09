using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Island.Decor;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests de colocación para el módulo Decor. No dependen de Unity Resources: usan
    /// un catálogo construido en memoria con JSON mínimo.
    /// </summary>
    public class DecorTests
    {
        // ── JSON mínimo para los tests ──────────────────────────────────────

        const string DECOR_JSON = @"{
            ""items"": [
                { ""catalogId"": ""test_banco"",       ""displayName"": ""Banco"",
                  ""description"": ""Un banco de prueba."", ""kind"": ""Seat"",
                  ""price"": 80, ""unlockLevel"": 1, ""footprint"": 1.0, ""charm"": 0.10 },
                { ""catalogId"": ""test_farola"",      ""displayName"": ""Farola"",
                  ""description"": ""Una farola de prueba."", ""kind"": ""Light"",
                  ""price"": 100, ""unlockLevel"": 2, ""footprint"": 0.8, ""charm"": 0.08 },
                { ""catalogId"": ""test_estatua"",     ""displayName"": ""Estatua"",
                  ""description"": ""Una estatua de prueba."", ""kind"": ""Statue"",
                  ""price"": 400, ""unlockLevel"": 6, ""footprint"": 1.5, ""charm"": 0.15 },
                { ""catalogId"": ""test_arbusto"",     ""displayName"": ""Arbusto"",
                  ""description"": ""Un arbusto de prueba."", ""kind"": ""Plant"",
                  ""price"": 40, ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.05 },
                { ""catalogId"": ""test_cartel"",      ""displayName"": ""Cartel"",
                  ""description"": ""Un cartel de prueba."", ""kind"": ""Sign"",
                  ""price"": 80, ""unlockLevel"": 2, ""footprint"": 0.6, ""charm"": 0.06 },
                { ""catalogId"": ""test_valla"",       ""displayName"": ""Valla"",
                  ""description"": ""Una valla de prueba."", ""kind"": ""Fence"",
                  ""price"": 60, ""unlockLevel"": 1, ""footprint"": 0.4, ""charm"": 0.04 },
                { ""catalogId"": ""test_fuente"",      ""displayName"": ""Fuente"",
                  ""description"": ""Una fuente de prueba."", ""kind"": ""Water"",
                  ""price"": 500, ""unlockLevel"": 7, ""footprint"": 3.0, ""charm"": 0.20 },
                { ""catalogId"": ""test_a"",           ""displayName"": ""Adorno A"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_b"",           ""displayName"": ""Adorno B"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_c"",           ""displayName"": ""Adorno C"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_d"",           ""displayName"": ""Adorno D"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_e"",           ""displayName"": ""Adorno E"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_f"",           ""displayName"": ""Adorno F"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_g"",           ""displayName"": ""Adorno G"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_h"",           ""displayName"": ""Adorno H"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_i"",           ""displayName"": ""Adorno I"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_j"",           ""displayName"": ""Adorno J"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_k"",           ""displayName"": ""Adorno K"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_l"",           ""displayName"": ""Adorno L"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_m"",           ""displayName"": ""Adorno M"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_n"",           ""displayName"": ""Adorno N"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_o"",           ""displayName"": ""Adorno O"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_p"",           ""displayName"": ""Adorno P"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_q"",           ""displayName"": ""Adorno Q"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_r"",           ""displayName"": ""Adorno R"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_s"",           ""displayName"": ""Adorno S"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 },
                { ""catalogId"": ""test_t"",           ""displayName"": ""Adorno T"",
                  ""description"": """", ""kind"": ""Plant"", ""price"": 40,
                  ""unlockLevel"": 1, ""footprint"": 0.5, ""charm"": 0.10 }
            ]
        }";

        // ── setup ───────────────────────────────────────────────────────────

        DecorService _service;
        DecorCatalog _catalog;
        SaveGame _save;
        FakeIslandService _island;

        [SetUp]
        public void SetUp()
        {
            _catalog = new DecorCatalog(DECOR_JSON);
            _save = new SaveGame();
            _island = new FakeIslandService();
            _service = new DecorService(_catalog, _save, _island);
        }

        Vector3 Pos(float x, float z) => new Vector3(x, 0f, z);

        // ── 1. colocar un adorno válido ─────────────────────────────────────

        [Test]
        public void Place_Valid_ReturnsIdAndAddsToSave()
        {
            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);

            Assert.IsNotEmpty(id, "debería devolver un identificador");
            Assert.AreEqual(1, _save.Island.Decor.Count);
            Assert.AreEqual("test_banco", _save.Island.Decor[0].CatalogId);
            Assert.AreEqual("plaza", _save.Island.Decor[0].ZoneId);
            Assert.AreEqual(id, _save.Island.Decor[0].PlacementId);
        }

        // ── 2. zona cerrada → ZoneLocked y no toca el guardado ──────────────

        [Test]
        public void Place_LockedZone_ReturnsZoneLocked()
        {
            _island.Lock("plaza");

            var rejection = _service.CanPlace("test_banco", "plaza", Pos(0, 0));
            Assert.AreEqual(DecorRejection.ZoneLocked, rejection);

            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            Assert.IsEmpty(id, "Place debería devolver vacío en zona cerrada");
            Assert.AreEqual(0, _save.Island.Decor.Count,
                "no debió tocar el guardado");
        }

        // ── 3. dos adornos que se pisan → Overlaps ──────────────────────────

        [Test]
        public void Place_Overlapping_ReturnsOverlaps()
        {
            string id1 = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            Assert.IsNotEmpty(id1);

            // el banco tiene footprint 1.0, así que a 0.5 m se solapan
            var rejection = _service.CanPlace("test_farola", "plaza", Pos(0.5f, 0));
            Assert.AreEqual(DecorRejection.Overlaps, rejection);

            string id2 = _service.Place("test_farola", "plaza", Pos(0.5f, 0), 0f);
            Assert.IsEmpty(id2, "Place debería devolver vacío si solapa");
            Assert.AreEqual(1, _save.Island.Decor.Count,
                "no debió añadir la segunda pieza");
        }

        // ── 4. dos adornos que se rozan sin tocarse → ambos entran ──────────

        [Test]
        public void Place_NearButNotOverlapping_BothEnter()
        {
            // banco footprint 1.0 + farola footprint 0.8 = 1.8 m necesarios
            // a 2.0 m de distancia no se tocan
            string id1 = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            Assert.IsNotEmpty(id1);

            var rejection = _service.CanPlace("test_farola", "plaza", Pos(2.0f, 0));
            Assert.AreEqual(DecorRejection.Ok, rejection);

            string id2 = _service.Place("test_farola", "plaza", Pos(2.0f, 0), 0f);
            Assert.IsNotEmpty(id2);
            Assert.AreEqual(2, _save.Island.Decor.Count);
        }

        // ── 5. mover a sitio libre y mover un centímetro ────────────────────

        [Test]
        public void Move_ToFreeSpot_Succeeds()
        {
            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            bool moved = _service.Move(id, Pos(5, 3), 90f);
            Assert.IsTrue(moved);

            var p = _save.Island.Decor[0];
            Assert.AreEqual(5f, p.X);
            Assert.AreEqual(3f, p.Z);
            Assert.AreEqual(90f, p.Yaw);
        }

        [Test]
        public void Move_OneCentimeter_DoesNotSelfOverlap()
        {
            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);

            // moverlo un centímetro no debe fallar por chocar consigo mismo
            bool moved = _service.Move(id, Pos(0.01f, 0), 0f);
            Assert.IsTrue(moved,
                "mover un centímetro no debe fallar por solaparse consigo mismo");
        }

        // ── 6. pasar de CapacityPerZone → TooMany ───────────────────────────

        [Test]
        public void Place_TooMany_ReturnsTooMany()
        {
            // llenar la zona con 20 arbustos en una cuadrícula 5×4 dentro del
            // radio de 12 m (footprint 0.5, separación 2 m entre ellos)
            int placed = 0;
            for (int col = 0; col < 5 && placed < 20; col++)
            {
                for (int row = 0; row < 4 && placed < 20; row++)
                {
                    string id = _service.Place("test_arbusto", "plaza",
                        Pos(col * 2f, row * 2f), 0f);
                    Assert.IsNotEmpty(id,
                        $"el adorno {placed} debería haberse colocado en ({col*2},{row*2})");
                    placed++;
                }
            }

            // la posición (0, 0) ya está ocupada, pero el orden de las reglas pone
            // TooMany antes que Overlaps: con 20 piezas, cualquier intento dentro del
            // radio de 12 m y en zona desbloqueada debe devolver TooMany
            var rejection = _service.CanPlace("test_arbusto", "plaza", Pos(11, 0));
            Assert.AreEqual(DecorRejection.TooMany, rejection);
        }

        // ── 7. quitar una pieza la saca del guardado ────────────────────────

        [Test]
        public void Remove_Existing_FreesSlot()
        {
            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            Assert.AreEqual(1, _save.Island.Decor.Count);

            bool removed = _service.Remove(id);
            Assert.IsTrue(removed);
            Assert.AreEqual(0, _save.Island.Decor.Count);
        }

        [Test]
        public void Remove_Nonexistent_ReturnsFalse()
        {
            bool removed = _service.Remove("no_existe");
            Assert.IsFalse(removed);
        }

        // ── 8. CharmOf vacío es 0 y nunca pasa de 1 ─────────────────────────

        [Test]
        public void CharmOf_EmptyZone_ReturnsZero()
        {
            Assert.AreEqual(0f, _service.CharmOf("plaza"));
        }

        [Test]
        public void CharmOf_CappedAtOne()
        {
            // 12 adornos distintos con footprint ≤ 1.0 y charm ≥ 0.08 repartidos
            // en una rejilla de 3 m para que no solapen. Suma sin tope ≈ 1.40,
            // con tope debe dar exactamente 1.0.
            //   test_banco       charm 0.10  footprint 1.0
            //   test_farola      charm 0.08  footprint 0.8
            //   test_cartel      charm 0.06  footprint 0.6
            //   test_arbusto     charm 0.05  footprint 0.5
            //   test_valla       charm 0.04  footprint 0.4
            //   test_a..test_h   charm 0.10  footprint 0.5  (8 items)
            //   total = 0.10+0.08+0.06+0.05+0.04+0.80 = 1.13 → recortado a 1.0
            (string id, float x, float z)[] placements =
            {
                ("test_banco",   0f,  0f),
                ("test_farola",  3f,  0f),
                ("test_cartel",  6f,  0f),
                ("test_arbusto", 9f,  0f),
                ("test_valla",   0f,  3f),
                ("test_a",       3f,  3f),
                ("test_b",       6f,  3f),
                ("test_c",       9f,  3f),
                ("test_d",       0f,  6f),
                ("test_e",       3f,  6f),
                ("test_f",       6f,  6f),
                ("test_g",       9f,  6f),
            };

            for (int i = 0; i < placements.Length; i++)
            {
                var (id, x, z) = placements[i];
                string placed = _service.Place(id, "plaza", Pos(x, z), 0f);
                Assert.IsNotEmpty(placed,
                    $"no se pudo colocar {id} en ({x},{z})");
            }

            float charm = _service.CharmOf("plaza");
            Assert.AreEqual(1f, charm, "el encanto no debe pasar de 1");
        }

        // ── 9. 20 iguales < 20 distintos ────────────────────────────────────

        [Test]
        public void CharmOf_IdenticalVsDiverse()
        {
            // 20 arbustos iguales (charm 0.05 cada uno)
            var saveIdentical = new SaveGame();
            var svcIdentical = new DecorService(_catalog, saveIdentical, _island);
            for (int i = 0; i < 20; i++)
                svcIdentical.Place("test_arbusto", "plaza", Pos(i * 1.5f, 0), 0f);

            float charmIdentical = svcIdentical.CharmOf("plaza");

            // 20 adornos distintos (test_a a test_t, charm 0.10 cada uno)
            var saveDiverse = new SaveGame();
            var svcDiverse = new DecorService(_catalog, saveDiverse, _island);
            char[] diverseIds = "abcdefghijklmnopqrst".ToCharArray();
            for (int i = 0; i < diverseIds.Length; i++)
                svcDiverse.Place($"test_{diverseIds[i]}", "parque", Pos(i * 1.5f, 0), 0f);

            float charmDiverse = svcDiverse.CharmOf("parque");

            Assert.Greater(charmDiverse, charmIdentical,
                "20 adornos distintos deberían dar más encanto que 20 iguales");
        }

        // ── 10. ids estables al borrar del medio ────────────────────────────

        [Test]
        public void Place_AfterRemovingMiddle_DoesNotReuseId()
        {
            // poner tres piezas
            string id0 = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            string id1 = _service.Place("test_banco", "plaza", Pos(3, 0), 0f);
            string id2 = _service.Place("test_banco", "plaza", Pos(6, 0), 0f);

            Assert.AreEqual("plaza_test_banco_0", id0);
            Assert.AreEqual("plaza_test_banco_1", id1);
            Assert.AreEqual("plaza_test_banco_2", id2);

            // quitar la del medio
            _service.Remove(id1);

            // volver a poner: no debe reutilizar _1
            string id3 = _service.Place("test_banco", "plaza", Pos(9, 0), 0f);
            Assert.AreEqual("plaza_test_banco_3", id3,
                "no debe reutilizar un identificador que ya estuvo en uso");
        }

        // ── auxiliares: más pruebas de cobertura ────────────────────────────

        [Test]
        public void CanPlace_UnknownItem_ReturnsUnknownItem()
        {
            var rejection = _service.CanPlace("no_existe", "plaza", Pos(0, 0));
            Assert.AreEqual(DecorRejection.UnknownItem, rejection);
        }

        [Test]
        public void CanPlace_OutsideZone_ReturnsOutsideZone()
        {
            // a 20 metros del centro, fuera del radio de 12
            var rejection = _service.CanPlace("test_banco", "plaza", Pos(20, 0));
            Assert.AreEqual(DecorRejection.OutsideZone, rejection);
        }

        [Test]
        public void InZone_ReturnsOnlyZoneItems()
        {
            _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            _service.Place("test_farola", "plaza", Pos(4, 0), 0f);
            _service.Place("test_arbusto", "parque", Pos(0, 0), 0f);

            var plazaItems = new List<DecorPlacement>(_service.InZone("plaza"));
            Assert.AreEqual(2, plazaItems.Count);
            Assert.IsTrue(plazaItems.Exists(p => p.CatalogId == "test_banco"));
            Assert.IsTrue(plazaItems.Exists(p => p.CatalogId == "test_farola"));
        }

        [Test]
        public void TryGetDefinition_Existing_ReturnsTrue()
        {
            bool found = _service.TryGetDefinition("test_banco", out var def);
            Assert.IsTrue(found);
            Assert.AreEqual("test_banco", def.CatalogId);
        }

        [Test]
        public void TryGetDefinition_Missing_ReturnsFalse()
        {
            bool found = _service.TryGetDefinition("no_existe", out _);
            Assert.IsFalse(found);
        }

        [Test]
        public void CapacityPerZone_IsTwenty()
        {
            Assert.AreEqual(20, _service.CapacityPerZone);
        }

        [Test]
        public void Move_Nonexistent_ReturnsFalse()
        {
            bool moved = _service.Move("no_existe", Pos(0, 0), 0f);
            Assert.IsFalse(moved);
        }

        [Test]
        public void Move_ToOutsideZone_ReturnsFalse()
        {
            string id = _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            bool moved = _service.Move(id, Pos(20, 0), 0f);
            Assert.IsFalse(moved);

            // la posición no debió cambiar
            Assert.AreEqual(0f, _save.Island.Decor[0].X);
        }

        [Test]
        public void CharmOf_RepeatedItemsHaveDiminishingReturns()
        {
            // test_banco tiene charm 0.10
            // 1 banco → 0.10 * 1    = 0.10
            // 2 bancos → 0.10 * 1.5 = 0.15
            // 3 bancos → 0.10 * 1.75 = 0.175
            // 4 bancos → 0.10 * 1.75 = 0.175 (el cuarto no suma)

            _service.Place("test_banco", "plaza", Pos(0, 0), 0f);
            float c1 = _service.CharmOf("plaza");
            Assert.AreEqual(0.10f, c1, 0.001f);

            _service.Place("test_banco", "plaza", Pos(3, 0), 0f);
            float c2 = _service.CharmOf("plaza");
            Assert.AreEqual(0.15f, c2, 0.001f);

            _service.Place("test_banco", "plaza", Pos(6, 0), 0f);
            float c3 = _service.CharmOf("plaza");
            Assert.AreEqual(0.175f, c3, 0.001f);

            _service.Place("test_banco", "plaza", Pos(9, 0), 0f);
            float c4 = _service.CharmOf("plaza");
            Assert.AreEqual(0.175f, c4, 0.001f,
                "el cuarto banco no debería sumar encanto");
        }

        [Test]
        public void Place_UnknownItem_ReturnsEmpty()
        {
            string id = _service.Place("no_existe", "plaza", Pos(0, 0), 0f);
            Assert.IsEmpty(id);
        }

        // ── mock de IIslandService ───────────────────────────────────────────

        class FakeIslandService : IIslandService
        {
            readonly HashSet<string> _locked = new HashSet<string>();

            public void Lock(string zoneId) => _locked.Add(zoneId);

            public IslandState State => null;
            public IReadOnlyList<string> ZoneIds => null;
            public ZonePurpose PurposeOf(string zoneId) => ZonePurpose.Leisure;
            public bool TryGetSpawnPoint(string zoneId, out Vector3 position)
            {
                position = Vector3.zero;
                return false;
            }
            public IEnumerable<string> ReachableFrom(string zoneId)
            {
                yield break;
            }
            public bool IsUnlocked(string buildingId) => !_locked.Contains(buildingId);
            public bool Unlock(string buildingId) => true;
            public void SendTo(string islanderId, string zoneId) { }
        }
    }
}
