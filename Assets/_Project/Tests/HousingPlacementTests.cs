using System.Collections.Generic;
using System.Linq;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Housing;
using Nimbo.Housing;
using Nimbo.Housing.Catalog;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Tests de colocación para el módulo Housing. No dependen de Unity: usan un
    /// catálogo construido en memoria con JSON mínimo.
    /// </summary>
    public class HousingPlacementTests
    {
        HousingService _service;
        FurnitureCatalog _catalog;

        // --- JSON mínimo para los tests -------------------------------------------

        const string FURNITURE_JSON = @"{
            ""items"": [
                { ""catalogId"": ""test_silla"",       ""displayName"": ""Silla"",
                  ""description"": """", ""price"": 10, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""seat"", ""needBonus"": { ""energy"": 2 } },
                { ""catalogId"": ""test_mesa"",        ""displayName"": ""Mesa"",
                  ""description"": """", ""price"": 30, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 2, ""footprintY"": 2,
                  ""function"": ""table"", ""needBonus"": { ""social"": 4 } },
                { ""catalogId"": ""test_sofa_2x1"",    ""displayName"": ""Sofá 2x1"",
                  ""description"": """", ""price"": 50, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 2, ""footprintY"": 1,
                  ""function"": ""seat"", ""needBonus"": { ""energy"": 5 } },
                { ""catalogId"": ""test_alfombra"",    ""displayName"": ""Alfombra"",
                  ""description"": """", ""price"": 15, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Rug"", ""footprintX"": 2, ""footprintY"": 2,
                  ""function"": ""decor"", ""needBonus"": {} },
                { ""catalogId"": ""test_cuadro"",      ""displayName"": ""Cuadro"",
                  ""description"": """", ""price"": 20, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""WallMounted"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""decor"", ""needBonus"": {} },
                { ""catalogId"": ""test_lampara_mesa"", ""displayName"": ""Lámpara mesa"",
                  ""description"": """", ""price"": 25, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Counter"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""light"", ""needBonus"": {} },
                { ""catalogId"": ""test_mesa_1x1"",    ""displayName"": ""Mesita"",
                  ""description"": """", ""price"": 20, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""table"", ""needBonus"": {} },
                { ""catalogId"": ""test_planta"",      ""displayName"": ""Planta"",
                  ""description"": """", ""price"": 10, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 1, ""footprintY"": 1,
                  ""function"": ""decor"", ""needBonus"": {} },
                { ""catalogId"": ""test_estanteria"",  ""displayName"": ""Estantería"",
                  ""description"": """", ""price"": 40, ""unlockLevel"": 1,
                  ""tags"": [], ""layer"": ""Furniture"", ""footprintX"": 1, ""footprintY"": 2,
                  ""function"": ""storage"", ""needBonus"": {} }
            ]
        }";

        const string FINISHES_JSON = @"{ ""items"": [] }";

        // --- setup -----------------------------------------------------------------

        [SetUp]
        public void SetUp()
        {
            _catalog = new FurnitureCatalog(FURNITURE_JSON, FINISHES_JSON);
            _service = new HousingService(_catalog);
        }

        RoomLayout NewRoom(int width = 8, int height = 8)
        {
            var room = new RoomLayout { Width = width, Height = height };
            room.FillFloor("floor_basic");
            return room;
        }

        // --- 1. colocar 1×1 en vacío -----------------------------------------------

        [Test]
        public void Place_1x1_InEmptyRoom_Succeeds()
        {
            var room = NewRoom();
            var result = _service.Place(room, "test_silla", new GridCoord(0, 0), Facing.North);
            Assert.AreEqual(PlacementError.None, result);
            Assert.AreEqual(1, room.Objects.Count);
            Assert.AreEqual("test_silla", room.Objects[0].CatalogId);
            Assert.AreNotEqual("", room.Objects[0].InstanceId);
        }

        // --- 2. mismo sitio dos veces → Occupied -----------------------------------

        [Test]
        public void Place_SameCell_SameLayer_ReturnsOccupied()
        {
            var room = NewRoom();
            _service.Place(room, "test_silla", new GridCoord(2, 2), Facing.North);
            var result = _service.Place(room, "test_silla", new GridCoord(2, 2), Facing.North);
            Assert.AreEqual(PlacementError.Occupied, result);
            Assert.AreEqual(1, room.Objects.Count, "la segunda silla no debió colocarse");
        }

        // --- 3. alfombra + mesa misma casilla → OK (capas distintas) ---------------

        [Test]
        public void Place_RugAndTable_SameCell_Succeeds()
        {
            var room = NewRoom();
            _service.Place(room, "test_alfombra", new GridCoord(0, 0), Facing.North);
            var result = _service.Place(room, "test_mesa", new GridCoord(0, 0), Facing.North);
            Assert.AreEqual(PlacementError.None, result);
            Assert.AreEqual(2, room.Objects.Count);
        }

        // --- 4. 2×1 girado al este -------------------------------------------------

        [Test]
        public void Footprint_2x1_FacingEast_OccupiesY()
        {
            var room = NewRoom();

            // sofa 2×1 facing North ocupa (0,0) y (1,0)
            _service.Place(room, "test_sofa_2x1", new GridCoord(0, 0), Facing.North);
            Assert.AreEqual(PlacementError.None,
                _service.CanPlace(room, "test_silla", new GridCoord(0, 1), Facing.North),
                "la silla en (0,1) debería estar libre tras colocar sofá norte");

            // sofa 2×1 facing East ocupa (2,0) y (2,1)
            _service.Place(room, "test_sofa_2x1", new GridCoord(2, 0), Facing.East);
            Assert.AreEqual(PlacementError.Occupied,
                _service.CanPlace(room, "test_silla", new GridCoord(2, 1), Facing.North),
                "el sofá este ocupa (2,1): la silla no cabe");
        }

        // --- 5. cuadro en mitad → NeedsWall; en pared → OK -------------------------

        [Test]
        public void Place_WallMounted_MidRoom_ReturnsNeedsWall()
        {
            var room = NewRoom();
            var result = _service.Place(room, "test_cuadro", new GridCoord(4, 3), Facing.North);
            Assert.AreEqual(PlacementError.NeedsWall, result);
        }

        [Test]
        public void Place_WallMounted_NorthWall_Succeeds()
        {
            var room = NewRoom();
            // y == Height-1 = 7 es la pared norte
            var result = _service.Place(room, "test_cuadro", new GridCoord(3, 7), Facing.North);
            Assert.AreEqual(PlacementError.None, result);
        }

        [Test]
        public void Place_WallMounted_WestWall_Succeeds()
        {
            var room = NewRoom();
            // x == 0 es la pared oeste
            var result = _service.Place(room, "test_cuadro", new GridCoord(0, 2), Facing.North);
            Assert.AreEqual(PlacementError.None, result);
        }

        // --- 6. Surface sobre suelo → NeedsSurface; sobre mesa → OK ----------------

        [Test]
        public void Place_Surface_OnFloor_ReturnsNeedsSurface()
        {
            var room = NewRoom();
            var result = _service.Place(room, "test_lampara_mesa", new GridCoord(2, 2), Facing.North);
            Assert.AreEqual(PlacementError.NeedsSurface, result);
        }

        [Test]
        public void Place_Surface_OnTable_Succeeds()
        {
            var room = NewRoom();
            _service.Place(room, "test_mesa_1x1", new GridCoord(3, 4), Facing.North);
            var result = _service.Place(room, "test_lampara_mesa", new GridCoord(3, 4), Facing.North);
            Assert.AreEqual(PlacementError.None, result);
        }

        // --- 7. Place que falla no toca la habitación ------------------------------

        [Test]
        public void Place_Fails_DoesNotMutateRoom()
        {
            var room = NewRoom();
            _service.Place(room, "test_silla", new GridCoord(0, 0), Facing.North);
            int countBefore = room.Objects.Count;

            var result = _service.Place(room, "test_cuadro", new GridCoord(4, 3), Facing.North);
            Assert.AreEqual(PlacementError.NeedsWall, result);
            Assert.AreEqual(countBefore, room.Objects.Count, "la habitación no debió cambiar");
        }

        // --- 8. OutOfBounds --------------------------------------------------------

        [Test]
        public void Place_FootprintOutOfBounds_ReturnsOutOfBounds()
        {
            var room = NewRoom(8, 8);
            // mesa 2×2 en (7,7) se sale: ocuparía (7,7) y (8,7) → fuera
            var result = _service.Place(room, "test_mesa", new GridCoord(7, 7), Facing.North);
            Assert.AreEqual(PlacementError.OutOfBounds, result);
        }

        [Test]
        public void Place_NegativeOrigin_ReturnsOutOfBounds()
        {
            var room = NewRoom();
            var result = _service.Place(room, "test_silla", new GridCoord(-1, 0), Facing.North);
            Assert.AreEqual(PlacementError.OutOfBounds, result);
        }

        // --- auxiliares ------------------------------------------------------------

        [Test]
        public void CanPlace_UnknownCatalogId_ReturnsUnknownCatalogId()
        {
            var room = NewRoom();
            var result = _service.CanPlace(room, "no_existe", new GridCoord(0, 0), Facing.North);
            Assert.AreEqual(PlacementError.UnknownCatalogId, result);
        }

        [Test]
        public void Remove_ExistingObject_ReturnsTrue()
        {
            var room = NewRoom();
            _service.Place(room, "test_silla", new GridCoord(1, 1), Facing.North);
            string id = room.Objects[0].InstanceId;
            Assert.IsTrue(_service.Remove(room, id));
            Assert.AreEqual(0, room.Objects.Count);
        }

        [Test]
        public void Remove_AfterRemove_CanPlaceAgain()
        {
            var room = NewRoom();
            _service.Place(room, "test_silla", new GridCoord(1, 1), Facing.North);
            string id = room.Objects[0].InstanceId;
            _service.Remove(room, id);

            var result = _service.Place(room, "test_silla", new GridCoord(1, 1), Facing.North);
            Assert.AreEqual(PlacementError.None, result, "tras quitar la silla, la celda debe estar libre");
        }

        [Test]
        public void ComfortScore_EmptyRoom_ReturnsZero()
        {
            var room = NewRoom();
            Assert.AreEqual(0f, _service.ComfortScore(room));
        }

        [Test]
        public void ComfortScore_WithFurniture_ReturnsPositive()
        {
            var room = NewRoom();
            _service.Place(room, "test_silla", new GridCoord(0, 0), Facing.North);  // energy +2
            _service.Place(room, "test_planta", new GridCoord(2, 0), Facing.North); // decor → +2
            float score = _service.ComfortScore(room);
            Assert.Greater(score, 0f);
            Assert.LessOrEqual(score, 100f);
        }

        [Test]
        public void FootprintOf_ReturnsCorrectCoords()
        {
            var coords = _service.FootprintOf("test_mesa", new GridCoord(2, 3), Facing.North).ToList();
            Assert.AreEqual(4, coords.Count);
            CollectionAssert.Contains(coords, new GridCoord(2, 3));
            CollectionAssert.Contains(coords, new GridCoord(3, 3));
            CollectionAssert.Contains(coords, new GridCoord(2, 4));
            CollectionAssert.Contains(coords, new GridCoord(3, 4));
        }

        [Test]
        public void FootprintOf_2x1_FacingEast_SwapsDimensions()
        {
            var coords = _service.FootprintOf("test_sofa_2x1", new GridCoord(0, 0), Facing.East).ToList();
            // 2×1 facing East → footprint 1×2 en grid: (0,0) y (0,1)
            Assert.AreEqual(2, coords.Count);
            CollectionAssert.Contains(coords, new GridCoord(0, 0));
            CollectionAssert.Contains(coords, new GridCoord(0, 1));
        }
    }
}
