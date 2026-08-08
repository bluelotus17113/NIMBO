using Nimbo.Data.Islanders;
using Nimbo.Simulation.Jobs;
using Nimbo.Simulation.Progression;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// El catálogo de oficios y el ritmo semanal. Los dos son tablas puras, así que
    /// se prueban sin montar la isla entera.
    /// </summary>
    public class JobCatalogTests
    {
        [Test]
        public void LosOchoOficiosTienenZonaYSueldo()
        {
            Assert.AreEqual(8, JobCatalog.All.Length);

            foreach (var entry in JobCatalog.All)
            {
                Assert.IsNotEmpty(entry.DisplayName, $"{entry.Kind} sin nombre");
                Assert.IsNotEmpty(entry.ZoneId, $"{entry.Kind} sin zona");
                Assert.Greater(entry.BaseWage, 0, $"{entry.Kind} no paga nada");
                Assert.That(entry.StartHour, Is.InRange(0, 23));
                Assert.That(entry.Hours, Is.InRange(1, 12));
            }
        }

        [Test]
        public void CadaOficioApuntaAUnaZonaQueExiste()
        {
            var zones = Nimbo.Island.Zones.IslandLayout.FirstIsland();

            foreach (var entry in JobCatalog.All)
            {
                bool found = false;
                for (int i = 0; i < zones.Length; i++)
                    if (zones[i].ZoneId == entry.ZoneId) { found = true; break; }

                Assert.IsTrue(found, $"{entry.Kind} trabaja en {entry.ZoneId}, que no existe");
            }
        }

        [Test]
        public void ElPerfilIdealDeUnOficioEsElQueMejorLeVa()
        {
            // Quien es exactamente el perfil ideal del puesto tiene que sacar la
            // afinidad más alta posible: si no, la fórmula de distancia está mal.
            foreach (var entry in JobCatalog.All)
            {
                float perfect = JobCatalog.Affinity(entry.Ideal, entry.Kind);
                Assert.That(perfect, Is.GreaterThan(0.95f),
                            $"el perfil ideal de {entry.Kind} solo saca {perfect}");
            }
        }

        [Test]
        public void LaAfinidadNuncaSeSaleDeCeroAUno()
        {
            for (int type = 0; type < PersonalityProfile.TypeCount; type++)
            {
                var profile = PersonalityProfile.FromTypeIndex(type);
                foreach (var entry in JobCatalog.All)
                {
                    float affinity = JobCatalog.Affinity(profile, entry.Kind);
                    Assert.That(affinity, Is.InRange(0f, 1f),
                                $"tipo {type} en {entry.Kind} da {affinity}");
                }
            }
        }

        [Test]
        public void SubirDeRangoSubeElSueldo()
        {
            foreach (var entry in JobCatalog.All)
            {
                int low = JobCatalog.Wage(entry.Kind, 1, 0.5f);
                int high = JobCatalog.Wage(entry.Kind, JobState.MaxRank, 0.5f);
                Assert.Greater(high, low, $"{entry.Kind} paga igual en rango 1 que en 5");
            }
        }

        [Test]
        public void SinTrabajoNoHaySueldo()
        {
            Assert.AreEqual(0, JobCatalog.Wage(JobKind.None, 3, 1f));
            Assert.AreEqual(0f, JobCatalog.Affinity(default, JobKind.None));
        }

        [Test]
        public void ElAscensoLlegaTrasVariosTurnos()
        {
            var job = new JobState { Kind = JobKind.Cook, Rank = 1, ShiftsWorked = 0 };
            Assert.IsFalse(job.CanPromote, "asciende sin haber trabajado");

            job.ShiftsWorked = job.ShiftsForNextRank;
            Assert.IsTrue(job.CanPromote);

            job.Rank = JobState.MaxRank;
            Assert.IsFalse(job.CanPromote, "asciende por encima del rango máximo");
        }
    }

    public class WeeklyRhythmTests
    {
        [Test]
        public void LaSemanaDaLaVueltaCadaSieteDias()
        {
            Assert.AreEqual(Weekday.Monday, WeeklyRhythm.DayOf(1));
            Assert.AreEqual(Weekday.Sunday, WeeklyRhythm.DayOf(7));
            Assert.AreEqual(Weekday.Monday, WeeklyRhythm.DayOf(8));
            Assert.AreEqual(Weekday.Wednesday, WeeklyRhythm.DayOf(31));
        }

        [Test]
        public void ElDomingoNoSeTrabajaYNoSePaga()
        {
            Assert.IsFalse(WeeklyRhythm.IsWorkday(Weekday.Sunday));
            Assert.AreEqual(0f, WeeklyRhythm.WageMultiplier(Weekday.Sunday));

            for (int i = 0; i < 6; i++)
            {
                var day = (Weekday)i;
                Assert.IsTrue(WeeklyRhythm.IsWorkday(day), $"el {WeeklyRhythm.NameOf(day)} no se trabaja");
            }
        }

        [Test]
        public void ElLunesPagaMasQueUnDiaNormal()
        {
            Assert.Greater(WeeklyRhythm.WageMultiplier(Weekday.Monday),
                           WeeklyRhythm.WageMultiplier(Weekday.Tuesday));
        }

        [Test]
        public void LosSieteDiasTienenTitularPropio()
        {
            var seen = new System.Collections.Generic.HashSet<string>();
            for (int i = 0; i < 7; i++)
            {
                var day = (Weekday)i;
                string headline = WeeklyRhythm.Headline(day);
                Assert.IsNotEmpty(headline, $"{WeeklyRhythm.NameOf(day)} sin titular");
                Assert.IsTrue(seen.Add(headline), $"el titular de {WeeklyRhythm.NameOf(day)} está repetido");
            }
        }

        [Test]
        public void LosMultiplicadoresSonRazonables()
        {
            // Ninguno puede ser tan alto que convierta un día en el único que importa.
            for (int i = 0; i < 7; i++)
            {
                var day = (Weekday)i;
                Assert.That(WeeklyRhythm.WageMultiplier(day), Is.InRange(0f, 2f));
                Assert.That(WeeklyRhythm.SocialMultiplier(day), Is.InRange(0.5f, 2f));
                Assert.That(WeeklyRhythm.RestMultiplier(day), Is.InRange(0.5f, 2f));
                Assert.That(WeeklyRhythm.MoodBonus(day), Is.InRange(0f, 15f));
                Assert.That(WeeklyRhythm.ExtraStock(day), Is.InRange(0, 10));
            }
        }
    }
}
