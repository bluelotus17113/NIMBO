using System.Collections.Generic;
using System.IO;
using Nimbo.Core.Events;
using Nimbo.Core.Save;
using Nimbo.Core.Services;
using Nimbo.Core.Time;
using Nimbo.Core.Util;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using NUnit.Framework;

namespace Nimbo.Tests
{
    public class EventBusTests
    {
        private readonly struct Ping
        {
            public readonly int Value;
            public Ping(int value) => Value = value;
        }

        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void ElSuscritoRecibeLoPublicado()
        {
            int received = 0;
            EventBus.Subscribe<Ping>(p => received = p.Value);
            EventBus.Publish(new Ping(42));
            Assert.AreEqual(42, received);
        }

        [Test]
        public void DesuscribirseDentroDelManejadorNoRompeLaIteracion()
        {
            // El caso "avísame una vez": es lo que rompía las implementaciones ingenuas.
            int calls = 0;
            System.Action<Ping> handler = null;
            handler = _ =>
            {
                calls++;
                EventBus.Unsubscribe(handler);
            };

            EventBus.Subscribe(handler);
            EventBus.Publish(new Ping(1));
            EventBus.Publish(new Ping(2));

            Assert.AreEqual(1, calls, "el manejador siguió recibiendo tras desuscribirse");
        }

        [Test]
        public void UnManejadorQuePetaNoImpideLosDemas()
        {
            bool second = false;
            EventBus.Subscribe<Ping>(_ => throw new System.Exception("a propósito"));
            EventBus.Subscribe<Ping>(_ => second = true);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            EventBus.Publish(new Ping(1));
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.IsTrue(second, "un módulo roto se llevó por delante al siguiente");
        }
    }

    public class GameClockTests
    {
        [TearDown]
        public void TearDown() => EventBus.Clear();

        [Test]
        public void ArrancaALasOchoDelDiaUno()
        {
            var clock = new GameClock();
            Assert.AreEqual(1, clock.Day);
            Assert.AreEqual(8, clock.Hour);
        }

        [Test]
        public void UnSaltoLargoAvisaDeCadaHora()
        {
            // Las necesidades acumulan hora a hora: si un salto de dos días publicara
            // un solo aviso, una partida cerrada saldría con el hambre intacta.
            var clock = new GameClock(0);
            int hours = 0, days = 0;
            EventBus.Subscribe<HourPassed>(_ => hours++);
            EventBus.Subscribe<DayPassed>(_ => days++);

            clock.Advance(GameClock.MinutesPerDay * 2);

            Assert.AreEqual(48, hours);
            Assert.AreEqual(2, days);
        }

        [Test]
        public void ElTickAcumulaFraccionesHastaCompletarUnMinuto()
        {
            var clock = new GameClock(0) { MinutesPerRealSecond = 1f };
            for (int i = 0; i < 60; i++) clock.Tick(1f / 60f);
            Assert.AreEqual(1, clock.ElapsedMinutes);
        }

        [Test]
        public void EnPausaNoAvanza()
        {
            var clock = new GameClock(0);
            clock.Pause();
            clock.Tick(10f);
            Assert.AreEqual(0, clock.ElapsedMinutes);
        }
    }

    public class RngTests
    {
        [Test]
        public void LaMismaSemillaDaLaMismaSecuencia()
        {
            // De esto depende que la comida favorita de un habitante no cambie entre
            // partidas: se deriva de su id.
            var a = Rng.FromSeed("habitante-123");
            var b = Rng.FromSeed("habitante-123");
            for (int i = 0; i < 50; i++) Assert.AreEqual(a.NextFloat(), b.NextFloat());
        }

        [Test]
        public void SemillasDistintasDivergen()
        {
            var a = Rng.FromSeed("uno");
            var b = Rng.FromSeed("dos");
            Assert.AreNotEqual(a.NextFloat(), b.NextFloat());
        }

        [Test]
        public void PickWeightedDevuelveMenosUnoSiTodoEsCero()
        {
            var rng = new Rng(7);
            Assert.AreEqual(-1, rng.PickWeighted(new List<float> { 0f, 0f, 0f }));
        }

        [Test]
        public void PickWeightedRespetaLosPesos()
        {
            var rng = new Rng(99);
            var weights = new List<float> { 0f, 1f, 0f };
            for (int i = 0; i < 100; i++)
                Assert.AreEqual(1, rng.PickWeighted(weights), "eligió un peso de cero");
        }

        [Test]
        public void RangeNuncaDevuelveElLimiteSuperior()
        {
            var rng = new Rng(3);
            for (int i = 0; i < 500; i++) Assert.Less(rng.Range(0, 4), 4);
        }
    }

    public class SaveSystemTests
    {
        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            foreach (var path in new[] { SaveSystem.SavePath, SaveSystem.BackupPath })
                if (File.Exists(path)) File.Delete(path);
        }

        private static SaveGame SampleSave()
        {
            var save = new SaveGame { ElapsedMinutes = 1234 };
            save.Wallet.Coins = 250;
            save.Islanders.Add(new IslanderData
            {
                Identity = new IslanderIdentity { Id = "abc", DisplayName = "Marta" },
                Personality = new PersonalityProfile(0.5f, -0.5f, 0.5f, -0.5f),
                Needs = NeedState.Fresh,
                Mood = MoodState.Fresh,
                Progression = ProgressionState.Fresh,
            });
            return save;
        }

        [Test]
        public void LoGuardadoSeVuelveALeerIgual()
        {
            var original = SampleSave();
            Assert.IsTrue(SaveSystem.Write(original));

            var loaded = SaveSystem.Read();
            Assert.IsNotNull(loaded);
            Assert.AreEqual(1234, loaded.ElapsedMinutes);
            Assert.AreEqual(250, loaded.Wallet.Coins);
            Assert.AreEqual(1, loaded.Islanders.Count);
            Assert.AreEqual("Marta", loaded.Islanders[0].Identity.DisplayName);
            Assert.AreEqual(5, loaded.Islanders[0].Personality.TypeIndex);
        }

        [Test]
        public void GuardarDosVecesDejaUnRespaldoDeLaAnterior()
        {
            var first = SampleSave();
            SaveSystem.Write(first);

            var second = SampleSave();
            second.ElapsedMinutes = 9999;
            SaveSystem.Write(second);

            Assert.IsTrue(File.Exists(SaveSystem.BackupPath),
                          "no se dejó respaldo: una escritura a medias se llevaría la partida");
            Assert.AreEqual(9999, SaveSystem.Read().ElapsedMinutes);
        }

        [Test]
        public void UnaPartidaCorruptaCaeEnElRespaldo()
        {
            SaveSystem.Write(SampleSave());
            var second = SampleSave();
            second.ElapsedMinutes = 5555;
            SaveSystem.Write(second);

            File.WriteAllText(SaveSystem.SavePath, "{ esto no es json");

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            var loaded = SaveSystem.Read();
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.IsNotNull(loaded, "se perdió la partida en vez de tirar del respaldo");
            Assert.AreEqual(1234, loaded.ElapsedMinutes);
        }
    }

    public class IslanderRegistryTests
    {
        [Test]
        public void NoAdmiteDosVecesElMismoHabitante()
        {
            var registry = new IslanderRegistry();
            var islander = new IslanderData
            {
                Identity = new IslanderIdentity { Id = "x", DisplayName = "Teo" },
            };

            registry.Add(islander);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            registry.Add(islander);
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(1, registry.Count);
        }

        [Test]
        public void EncuentraPorIdYPorZona()
        {
            var registry = new IslanderRegistry();
            registry.Add(new IslanderData
            {
                Identity = new IslanderIdentity { Id = "a" }, CurrentZoneId = "plaza",
            });
            registry.Add(new IslanderData
            {
                Identity = new IslanderIdentity { Id = "b" }, CurrentZoneId = "playa",
            });

            Assert.IsTrue(registry.TryGet("a", out _));
            Assert.IsFalse(registry.TryGet("z", out _));
            CollectionAssert.AreEqual(new[] { "a" },
                new List<string>(System.Linq.Enumerable.Select(registry.InZone("plaza"), i => i.Id)));
        }
    }
}
