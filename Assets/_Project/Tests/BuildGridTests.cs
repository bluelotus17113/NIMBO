using System.Linq;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Save;
using Nimbo.Data.World;
using Nimbo.Island;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// La rejilla y la colocación de edificios.
    /// </summary>
    /// <remarks>
    /// Lo que se prueba aquí no es que se pueda colocar: es que **nadie más se entere**
    /// de que la posición dejó de estar en la tabla. Ese era el riesgo del cambio.
    /// </remarks>
    public class BuildGridTests
    {
        [TearDown]
        public void TearDown() => EventBus.Clear();

        private static SaveGame NewSave() => new SaveGame();

        [Test]
        public void LaCasillaYElMundoSonLaMismaCosa()
        {
            // Ida y vuelta: del mundo a la casilla y de la casilla al mundo. Si estos
            // dos se separan, el fantasma se pinta en una casilla y el edificio cae en
            // otra, que es de los fallos más desconcertantes que hay.
            for (int x = -12; x <= 12; x += 3)
            for (int y = -12; y <= 12; y += 3)
            {
                var centre = BuildGrid.CentreOf(x, y);
                BuildGrid.CellAt(centre, out int backX, out int backY);

                Assert.AreEqual(x, backX, $"({x},{y}) volvió como ({backX},{backY})");
                Assert.AreEqual(y, backY, $"({x},{y}) volvió como ({backX},{backY})");
            }
        }

        [Test]
        public void FueraDeLaIslaNoSeConstruye()
        {
            int far = BuildGrid.MaxCell;
            Assert.IsFalse(BuildGrid.InsideIsland(far, far), "deja construir en el vacío");
            Assert.IsTrue(BuildGrid.InsideIsland(6, 6), "no deja construir en medio de la isla");
        }

        [Test]
        public void ElCentroEstaReservado()
        {
            Assert.IsTrue(BuildGrid.OnReservedCentre(0, 0),
                          "deja construir encima del Árbol Nimbo");
            Assert.IsFalse(BuildGrid.OnReservedCentre(12, 12));
        }

        [Test]
        public void DosEdificiosNoSePisan()
        {
            Assert.IsTrue(BuildGrid.Overlaps(5, 5, 6, 5), "dos casas pegadas no se detectan");
            Assert.IsTrue(BuildGrid.Overlaps(5, 5, 5, 5));
            Assert.IsFalse(BuildGrid.Overlaps(5, 5, 7, 5), "dos casas separadas se creen encima");
        }

        [Test]
        public void LaPlazaNoSeMueve()
        {
            var build = new BuildService(NewSave());

            Assert.IsTrue(build.IsFixed(BuildService.PlazaId));
            Assert.AreEqual(BuildRejection.Fixed, build.CanPlace(BuildService.PlazaId, 10, 10));
            Assert.IsFalse(build.Place(BuildService.PlazaId, 10, 10));
            Assert.IsFalse(build.Movable.Contains(BuildService.PlazaId),
                           "la plaza sale en la lista de lo que se puede colocar");
        }

        [Test]
        public void SinColocarNadaLosEdificiosSiguenDondeEstaban()
        {
            // Lo que protege las partidas viejas: una aldea sin nada colocado tiene que
            // leerse igual que antes del cambio, con las posiciones de la tabla.
            var build = new BuildService(NewSave());

            Assert.IsTrue(build.TryGetWorldCentre("zona_parque", out var centre));
            Assert.AreNotEqual(Vector3.zero, centre, "el parque salió sin sitio");
            Assert.IsFalse(build.TryGetPlacement("zona_parque", out _),
                           "una aldea nueva no debería tener nada colocado a mano");
        }

        [Test]
        public void ColocarMueveElCentroQueLeenLosDemas()
        {
            // La prueba del cambio entero: quien pregunte dónde está el parque tiene
            // que recibir el sitio nuevo, sin que nadie le diga que se ha movido.
            var build = new BuildService(NewSave());
            build.TryGetWorldCentre("zona_parque", out var before);

            Assert.IsTrue(build.Place("zona_parque", 14, 6));
            Assert.IsTrue(build.TryGetWorldCentre("zona_parque", out var after));

            Assert.AreNotEqual(before, after, "el parque se movió y sigue diciendo el sitio viejo");
            Assert.AreEqual(BuildGrid.CentreOf(14, 6), after);
        }

        [Test]
        public void ColocarAvisaUnaVez()
        {
            var build = new BuildService(NewSave());

            int avisos = 0;
            void OnMoved(BuildingMoved _) => avisos++;
            EventBus.Subscribe<BuildingMoved>(OnMoved);

            build.Place("zona_parque", 14, 6);
            Assert.AreEqual(1, avisos);

            // Rechazado: ni se mueve ni avisa.
            build.Place("zona_parque", 900, 900);
            Assert.AreEqual(1, avisos, "avisó de un movimiento que no ocurrió");

            EventBus.Unsubscribe<BuildingMoved>(OnMoved);
        }

        [Test]
        public void NoSeColocaEncimaDeOtroAunqueNadieLoHayaMovido()
        {
            // El caso que se escapa: comprobar solo contra lo colocado a mano dejaría
            // poner el primer edificio movido encima de otro que sigue en su sitio de
            // fábrica y que nadie ha tocado.
            var build = new BuildService(NewSave());

            Assert.IsTrue(build.TryGetWorldCentre("zona_tienda_comida", out var shop));
            BuildGrid.CellAt(shop, out int cx, out int cy);

            Assert.AreEqual(BuildRejection.Occupied, build.CanPlace("zona_parque", cx, cy));
        }

        [Test]
        public void UnEdificioColocadoSobreviveAlGuardado()
        {
            var save = NewSave();
            new BuildService(save).Place("zona_parque", 14, 6);

            // Otro servicio sobre el mismo guardado: es lo que pasa al cargar partida.
            var reloaded = new BuildService(save);
            Assert.IsTrue(reloaded.TryGetPlacement("zona_parque", out var placement));
            Assert.AreEqual(14, placement.CellX);
            Assert.AreEqual(6, placement.CellY);
        }
    }
}
