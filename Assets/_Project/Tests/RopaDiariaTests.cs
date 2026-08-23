using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Data.Islanders;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Simulation.Wardrobe;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que el cambio de ropa diario dispare cuando pasa el día, y no solo en el papel.
    /// </summary>
    /// <remarks>
    /// <c>OnDayPassed</c> nació sin ni una prueba que publicara <c>DayPassed</c>:
    /// verde por ausencia, que es la enfermedad histórica del proyecto — manejador
    /// escrito, nadie lo dispara, nadie lo delata. Aquí el evento se publica de
    /// verdad contra el sorteo real, que es determinista: misma semilla
    /// <c>{id}|cambio-ropa|{día}</c>, mismo resultado, en cualquier máquina.
    /// </remarks>
    public class RopaDiariaTests
    {
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private WardrobeService _armario;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            // Sin economía registrada a propósito: así el gusto es solo la semilla
            // {id}|{prenda} y el sorteo no depende del catálogo.
            _registry = new IslanderRegistry();
            var personalities = PersonalityRoster.CreateService();
            _simulation = new SimulationService(
                _registry, personalities,
                ScriptableObject.CreateInstance<NeedsConfig>());
            _armario = new WardrobeService(_registry, _simulation, personalities);
        }

        [TearDown]
        public void TearDown()
        {
            _armario.Dispose();
            _simulation.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        private static IslanderData Vecino(string id, params string[] prendas)
        {
            var vecino = new IslanderData
            {
                Identity = new IslanderIdentity { Id = id, DisplayName = id },
            };
            for (int i = 0; i < prendas.Length; i++) vecino.Wardrobe.Add(prendas[i]);
            if (prendas.Length > 0) vecino.EquippedOutfit = prendas[0];
            return vecino;
        }

        [Test]
        public void AlPasarLosDiasQuienTieneVariasPrendasSeCambia()
        {
            var vecino = Vecino("cambiador", "cloth_a", "cloth_b", "cloth_c");
            _registry.Add(vecino);

            int cambios = 0;
            for (int dia = 1; dia <= 60; dia++)
            {
                string antes = vecino.EquippedOutfit;
                EventBus.Publish(new DayPassed(dia));
                if (vecino.EquippedOutfit != antes) cambios++;
            }

            Assert.That(cambios, Is.GreaterThan(0),
                "sesenta días con tres prendas y no se puso otra nunca: el calendario " +
                "no llega al armario");
            Assert.That(vecino.Wardrobe.Contains(vecino.EquippedOutfit), Is.True,
                "se puso una prenda que no está en su armario");
        }

        [Test]
        public void QuienSoloTieneUnaPrendaNoSeCambiaNunca()
        {
            var vecino = Vecino("monovestimenta", "cloth_a");
            _registry.Add(vecino);

            for (int dia = 1; dia <= 60; dia++)
                EventBus.Publish(new DayPassed(dia));

            Assert.That(vecino.EquippedOutfit, Is.EqualTo("cloth_a"),
                "con una sola prenda el armario no ofrece alternativa");
        }

        [Test]
        public void TrasElDisposeElDiaPasaYElArmarioYaNoDecide()
        {
            // Mismo vecino y mismos días que la prueba anterior, que demuestra que
            // con la suscripción viva hay cambios: si aquí no hay ninguno, es el
            // Dispose quien lo separa del calendario y no suerte distinta.
            var vecino = Vecino("cambiador", "cloth_a", "cloth_b", "cloth_c");
            _registry.Add(vecino);

            _armario.Dispose();
            for (int dia = 1; dia <= 60; dia++)
                EventBus.Publish(new DayPassed(dia));

            Assert.That(vecino.EquippedOutfit, Is.EqualTo("cloth_a"),
                "tras el Dispose el servicio sigue escuchando el calendario");
        }
    }
}
