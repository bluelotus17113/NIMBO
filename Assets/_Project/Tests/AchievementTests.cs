using System;
using Nimbo.Core.Events;
using Nimbo.Core.Time;
using Nimbo.Data.Save;
using Nimbo.Simulation.Progression;
using NUnit.Framework;

namespace Nimbo.Tests
{
    public class AchievementTests
    {
        const string CatalogJson = @"{
            ""version"": 1,
            ""items"": [
                {
                    ""achievementId"": ""test_avance"",
                    ""displayName"": ""Test Avance"",
                    ""description"": ""Para probar Advance."",
                    ""kind"": ""Life"",
                    ""goal"": 3,
                    ""reward"": 100,
                    ""hidden"": false
                },
                {
                    ""achievementId"": ""test_record"",
                    ""displayName"": ""Test Record"",
                    ""description"": ""Para probar Record."",
                    ""kind"": ""Life"",
                    ""goal"": 5,
                    ""reward"": 200,
                    ""hidden"": false
                }
            ]
        }";

        AchievementCatalog _catalog;
        SaveGame _save;
        GameClock _clock;

        [SetUp]
        public void SetUp()
        {
            _catalog = new AchievementCatalog(CatalogJson);
            _save = new SaveGame();
            _clock = new GameClock();
            EventBus.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
        }

        // ── helpers ──────────────────────────────────────────────────────────

        AchievementService NewService() => new AchievementService(_catalog, _save, _clock);

        // ── 1. Nuevo empieza a cero y sin registro ──────────────────────────

        [Test]
        public void NewAchievement_StartsAtZero_AndNoRecordInSave()
        {
            var service = NewService();

            var progress = service.ProgressOf("test_avance");

            Assert.AreEqual(0, progress.Current);
            Assert.AreEqual(3, progress.Goal);
            Assert.IsFalse(progress.Unlocked);

            // no debe crear registro solo por preguntar
            Assert.AreEqual(0, _save.Achievements.Count,
                "el guardado no debe tener registro sin haber tocado el logro");
        }

        // ── 2. Advance sube el contador y lo guarda ─────────────────────────

        [Test]
        public void Advance_IncrementsCounter_AndSaves()
        {
            var service = NewService();

            service.Advance("test_avance");
            var progress = service.ProgressOf("test_avance");

            Assert.AreEqual(1, progress.Current);
            Assert.IsFalse(progress.Unlocked);

            // debe haber creado registro
            Assert.AreEqual(1, _save.Achievements.Count);
            Assert.AreEqual("test_avance", _save.Achievements[0].AchievementId);
            Assert.AreEqual(1, _save.Achievements[0].Current);
        }

        // ── 3. Llegar al objetivo desbloquea, publica una vez, devuelve cierto

        [Test]
        public void ReachingGoal_Unlocks_PublishesOnce_ReturnsTrue()
        {
            var service = NewService();
            int unlocks = 0;
            EventBus.Subscribe<AchievementUnlocked>(_ => unlocks++);

            // primer avance: 0→1, no llega
            bool r1 = service.Advance("test_avance");
            Assert.IsFalse(r1, "1/3 no debería desbloquear");

            // segundo avance: 1→2, no llega
            bool r2 = service.Advance("test_avance");
            Assert.IsFalse(r2, "2/3 no debería desbloquear");

            // tercer avance: 2→3, llega justo
            bool r3 = service.Advance("test_avance");
            Assert.IsTrue(r3, "3/3 debería desbloquear justo ahora");

            Assert.AreEqual(1, unlocks,
                "AchievementUnlocked debe haberse publicado exactamente una vez");

            var progress = service.ProgressOf("test_avance");
            Assert.IsTrue(progress.Unlocked);
            Assert.AreEqual(3, progress.Current);
        }

        // ── 4. Después de desbloqueado, Advance devuelve falso y no publica ─

        [Test]
        public void Advance_AfterUnlock_ReturnsFalse_NoDoublePublish()
        {
            var service = NewService();

            // desbloquear primero
            service.Advance("test_avance", 3);
            Assert.IsTrue(service.ProgressOf("test_avance").Unlocked);

            // suscribirse después para contar solo las publicaciones POST-desbloqueo
            int unlocks = 0;
            EventBus.Subscribe<AchievementUnlocked>(_ => unlocks++);

            bool result = service.Advance("test_avance");

            Assert.IsFalse(result,
                "Advance después de desbloqueado debe devolver falso");
            Assert.AreEqual(0, unlocks,
                "no debe publicar AchievementUnlocked una segunda vez — sería dinero infinito");
        }

        // ── 5. Record con valor menor no baja el contador ────────────────────

        [Test]
        public void Record_LowerValue_DoesNotDecrease()
        {
            var service = NewService();

            service.Record("test_record", 4);
            Assert.AreEqual(4, service.ProgressOf("test_record").Current);

            service.Record("test_record", 2); // menor, se ignora
            Assert.AreEqual(4, service.ProgressOf("test_record").Current,
                "Record con valor menor no debe bajar el contador");
        }

        // ── 6. Record con valor mayor sube y desbloquea ─────────────────────

        [Test]
        public void Record_HigherValue_Increases_AndUnlocks()
        {
            var service = NewService();
            int unlocks = 0;
            EventBus.Subscribe<AchievementUnlocked>(_ => unlocks++);

            // primero un valor que no llega
            bool r1 = service.Record("test_record", 3);
            Assert.IsFalse(r1);
            Assert.AreEqual(3, service.ProgressOf("test_record").Current);

            // ahora sí llega
            bool r2 = service.Record("test_record", 6);
            Assert.IsTrue(r2,
                "Record con valor >= goal debe desbloquear");
            Assert.AreEqual(6, service.ProgressOf("test_record").Current);
            Assert.IsTrue(service.ProgressOf("test_record").Unlocked);
            Assert.AreEqual(1, unlocks);
        }

        // ── 7. Id desconocido no rompe nada ─────────────────────────────────

        [Test]
        public void UnknownId_IsSilentNoOp()
        {
            var service = NewService();

            Assert.DoesNotThrow(() => service.Advance("no_existe"));
            Assert.DoesNotThrow(() => service.Record("no_existe", 5));

            Assert.IsFalse(service.Advance("no_existe"),
                "Advance con id desconocido debe devolver falso");
            Assert.IsFalse(service.Record("no_existe", 5),
                "Record con id desconocido debe devolver falso");

            var progress = service.ProgressOf("no_existe");
            Assert.AreEqual(0, progress.Current);
            Assert.IsFalse(progress.Unlocked);

            // no debe crear registro para ids que no están en el catálogo
            Assert.AreEqual(0, _save.Achievements.Count,
                "los ids huérfanos no deben crear registro");
        }

        // ── 8. UnlockedCount cuadra ─────────────────────────────────────────

        [Test]
        public void UnlockedCount_Matches()
        {
            var service = NewService();

            Assert.AreEqual(0, service.UnlockedCount);

            service.Advance("test_avance", 3); // desbloquea
            Assert.AreEqual(1, service.UnlockedCount);

            service.Record("test_record", 5); // desbloquea
            Assert.AreEqual(2, service.UnlockedCount);

            // desbloquear otra vez el mismo no debería sumar
            service.Advance("test_avance");
            Assert.AreEqual(2, service.UnlockedCount,
                "desbloquear dos veces el mismo logro no debe subir UnlockedCount");
        }

        // ── 9. Sigue desbloqueado aunque suban el objetivo ──────────────────

        [Test]
        public void Unlocked_PersistsWhenGoalIncreases()
        {
            // catálogo con goal=3
            var service = new AchievementService(_catalog, _save, _clock);
            service.Advance("test_avance", 3);
            Assert.IsTrue(service.ProgressOf("test_avance").Unlocked);

            // nuevo catálogo con goal=10 para el mismo logro
            const string newJson = @"{
                ""version"": 1,
                ""items"": [
                    {
                        ""achievementId"": ""test_avance"",
                        ""displayName"": ""Test Avance"",
                        ""description"": ""Para probar Advance."",
                        ""kind"": ""Life"",
                        ""goal"": 10,
                        ""reward"": 100,
                        ""hidden"": false
                    }
                ]
            }";
            var newCatalog = new AchievementCatalog(newJson);
            var newService = new AchievementService(newCatalog, _save, _clock);

            var progress = newService.ProgressOf("test_avance");
            Assert.IsTrue(progress.Unlocked,
                "Unlocked se guarda aparte del contador: cambiar el objetivo no quita el logro");
            Assert.AreEqual(3, progress.Current,
                "el contador sigue en lo que estaba");
            Assert.AreEqual(10, progress.Goal,
                "el objetivo lo da el nuevo catálogo");
        }

        // ── 10. Después de Dispose los eventos no cambian contadores ────────

        [Test]
        public void AfterDispose_EventsDontChangeCounters()
        {
            // catálogo con un logro que responde a DayPassed
            const string daysJson = @"{
                ""version"": 1,
                ""items"": [
                    {
                        ""achievementId"": ""logro_primer_amanecer"",
                        ""displayName"": ""Días"",
                        ""description"": ""Contar días."",
                        ""kind"": ""Life"",
                        ""goal"": 5,
                        ""reward"": 50,
                        ""hidden"": false
                    }
                ]
            }";
            var catalog = new AchievementCatalog(daysJson);
            var service = new AchievementService(catalog, _save, _clock);

            // publicar un día — el servicio está suscrito y debe reaccionar
            EventBus.Publish(new DayPassed(1));
            Assert.AreEqual(1, service.ProgressOf("logro_primer_amanecer").Current,
                "antes de Dispose el evento debe subir el contador");

            service.Dispose();

            // publicar otro día — el servicio ya no debe reaccionar
            EventBus.Publish(new DayPassed(2));
            Assert.AreEqual(1, service.ProgressOf("logro_primer_amanecer").Current,
                "después de Dispose el evento no debe cambiar el contador");
        }
    }
}
