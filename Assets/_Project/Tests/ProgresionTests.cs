using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Farming;
using Nimbo.Data.Player;
using Nimbo.Player;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Las cinco vías del protagonista: que suban con lo que hace y que abran cosas.
    /// </summary>
    /// <remarks>
    /// Lo que más se prueba aquí es lo que **no** debe subir. Una progresión que se
    /// llena sola mientras el jugador mira es peor que no tenerla: quita la única cosa
    /// que la hace valer, que es que cada nivel diga algo de lo que has estado
    /// haciendo.
    /// </remarks>
    public class ProgresionTests
    {
        private PlayerState _jugador;
        private PlayerProgressionService _vias;

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _jugador = new PlayerState();
            _vias = new PlayerProgressionService(_jugador);
        }

        [TearDown]
        public void TearDown()
        {
            _vias?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── la curva ─────────────────────────────────────────────────────────

        [Test]
        public void SeEmpiezaANivelUnoEnLasCinco()
        {
            foreach (SkillKind via in System.Enum.GetValues(typeof(SkillKind)))
                Assert.That(_vias.LevelOf(via), Is.EqualTo(1), $"{via} no empieza a uno");
        }

        [Test]
        public void UnaPartidaViejaSinViasNoSeQuedaANivelCero()
        {
            // Las partidas guardadas antes de que existiera esto no traen la lista, y
            // cargarlas no puede dejar al protagonista sin saber hacer nada.
            _jugador.Skills.Lines.Clear();

            Assert.That(_vias.LevelOf(SkillKind.Farming), Is.EqualTo(1));
            Assert.That(_vias.VillagerLevel, Is.EqualTo(1));
        }

        [Test]
        public void LlegarAlTopeCuestaLasCuatroMilDeLaTabla()
        {
            float total = 0f;
            for (int nivel = 1; nivel < SkillSet.MaxLevel; nivel++)
                total += SkillSet.XpForLevel(nivel);

            Assert.That(total, Is.EqualTo(SkillSet.TotalXpForLevel(SkillSet.MaxLevel)).Within(0.01f));
            Assert.That(total, Is.EqualTo(3960f).Within(0.01f), "la curva se ha movido del diseño");
        }

        [Test]
        public void UnPremioGordoPuedeSubirDosNivelesDeGolpe()
        {
            // De 1 a 2 cuestan 120 y de 2 a 3 doscientas: 400 pasan de largo las dos.
            // Quedarse en el primero se comería el sobrante sin decirlo.
            _vias.Grant(SkillKind.Crafting, 400f);

            Assert.That(_vias.LevelOf(SkillKind.Crafting), Is.EqualTo(3));
        }

        [Test]
        public void EnElTopeNoSeAcumulaMas()
        {
            _vias.Grant(SkillKind.Crafting, 99999f);

            Assert.That(_vias.LevelOf(SkillKind.Crafting), Is.EqualTo(SkillSet.MaxLevel));
            Assert.That(_vias.XpOf(SkillKind.Crafting), Is.Zero,
                "una barra que sigue llenándose en el tope promete un nivel que no llega");
        }

        [Test]
        public void ElNivelDeAldeanoEsLaMediaYNoSeGana()
        {
            _vias.Grant(SkillKind.Crafting, 99999f);   // esta al tope

            Assert.That(_vias.LevelOf(SkillKind.Crafting), Is.EqualTo(10));
            Assert.That(_vias.VillagerLevel, Is.EqualTo(2),
                "(10+1+1+1+1)/5 = 2: una vía al tope no convierte al protagonista en veterano");
        }

        // ── lo que la sube ───────────────────────────────────────────────────

        [Test]
        public void TalarSubeRecoleccionYNoCultivo()
        {
            EventBus.Publish(new NodeGathered("nodo", "mat_madera", 4));

            Assert.That(_vias.XpOf(SkillKind.Gathering), Is.GreaterThan(0f));
            Assert.That(_vias.XpOf(SkillKind.Farming), Is.Zero,
                "una tarde de hachazos no puede pagar los desbloqueos de otra vía");
        }

        [Test]
        public void AtenderUnaPeticionPagaEnConvivenciaYEnAldea()
        {
            EventBus.Publish(new RequestResolved("pet", "vecino", satisfied: true));

            Assert.That(_vias.XpOf(SkillKind.Social), Is.GreaterThan(0f));
            Assert.That(_vias.XpOf(SkillKind.Village), Is.GreaterThan(0f),
                "atender es a la vez un favor a alguien y una cosa de quien lleva la aldea");
        }

        [Test]
        public void NegarseNoPagaNiCuesta()
        {
            EventBus.Publish(new RequestResolved("pet", "vecino", satisfied: false));

            Assert.That(_vias.XpOf(SkillKind.Social), Is.Zero);
            Assert.That(_vias.XpOf(SkillKind.Village), Is.Zero,
                "decir que no ya cuesta el ánimo del vecino; cobrarlo dos veces es castigo");
        }

        [Test]
        public void DosVecinosQueSeCaenBienNoSubenTuConvivencia()
        {
            // `AffinityChanged` lo publican también los vecinos entre ellos, todo el
            // día. Sin filtro, la vía social se subiría sola mientras el jugador mira.
            EventBus.Publish(new AffinityChanged("bea", "leo", 5f, 30f));

            Assert.That(_vias.XpOf(SkillKind.Social), Is.Zero);
        }

        [Test]
        public void CaerleBienATiSiSube()
        {
            EventBus.Publish(new AffinityChanged(SocialIds.Player, "leo", 5f, 30f));

            Assert.That(_vias.XpOf(SkillKind.Social), Is.GreaterThan(0f));
        }

        [Test]
        public void CaerMalNoSuma()
        {
            EventBus.Publish(new AffinityChanged(SocialIds.Player, "leo", -5f, 10f));

            Assert.That(_vias.XpOf(SkillKind.Social), Is.Zero,
                "discutir con alguien no es practicar la convivencia");
        }

        [Test]
        public void DejarDeEscucharAlSoltarlo()
        {
            _vias.Dispose();
            EventBus.Publish(new ItemCrafted("receta", "cosa", 1));

            Assert.That(_vias.XpOf(SkillKind.Crafting), Is.Zero);
        }

        // ── lo que abre ──────────────────────────────────────────────────────

        [Test]
        public void RepartirTrabajosPideAldeaDos()
        {
            Assert.That(_vias.IsUnlocked(Unlock.AssignJobs), Is.False,
                "ser el que manda tiene que costar algo, o no significa nada");

            _vias.Grant(SkillKind.Village, SkillSet.XpForLevel(1));

            Assert.That(_vias.LevelOf(SkillKind.Village), Is.EqualTo(2));
            Assert.That(_vias.IsUnlocked(Unlock.AssignJobs), Is.True);
        }

        [Test]
        public void CadaDesbloqueoSabeDecirQueLeFalta()
        {
            _vias.RequirementFor(Unlock.Courtship, out var via, out int nivel);

            Assert.That(via, Is.EqualTo(SkillKind.Social));
            Assert.That(nivel, Is.EqualTo(5));
        }

        [Test]
        public void SubirDeNivelSeCuentaYLoQueAbreTambien()
        {
            SkillKind? subio = null;
            Unlock? abrio = null;

            void OnNivel(SkillLeveledUp e) => subio = e.Skill;
            void OnAbre(UnlockGained e) => abrio = e.Unlock;

            EventBus.Subscribe<SkillLeveledUp>(OnNivel);
            EventBus.Subscribe<UnlockGained>(OnAbre);

            _vias.Grant(SkillKind.Village, SkillSet.XpForLevel(1));

            EventBus.Unsubscribe<SkillLeveledUp>(OnNivel);
            EventBus.Unsubscribe<UnlockGained>(OnAbre);

            Assert.That(subio, Is.EqualTo(SkillKind.Village));
            Assert.That(abrio, Is.EqualTo(Unlock.AssignJobs),
                "el número sube y no se dice qué has ganado con él");
        }

        // ── la parcela que crece ─────────────────────────────────────────────

        [Test]
        public void ElHuertoEmpiezaSiendoCuatroPorTres()
        {
            FarmPlot.UsableSize(1, out int ancho, out int alto);

            Assert.That(ancho * alto, Is.EqualTo(12));
            Assert.That(FarmPlot.IsUsable(0, 0, 1), Is.False, "la esquina viene después");
            Assert.That(FarmPlot.IsUsable(3, 2, 1), Is.True, "el medio se trabaja desde el día uno");
        }

        [Test]
        public void LaParcelaSoloCreceYNuncaSeEncoge()
        {
            // Si al subir de nivel una casilla ya trabajada quedara fuera, subir sería
            // perder algo. La comprobación es tonta y es justo la que hay que hacer.
            for (int nivel = 1; nivel < SkillSet.MaxLevel; nivel++)
                for (int x = 0; x < 8; x++)
                    for (int y = 0; y < 6; y++)
                    {
                        if (!FarmPlot.IsUsable(x, y, nivel)) continue;

                        Assert.That(FarmPlot.IsUsable(x, y, nivel + 1), Is.True,
                            $"la casilla ({x},{y}) se trabajaba al {nivel} y no al {nivel + 1}");
                    }
        }

        [Test]
        public void ConCultivoOchoSeTrabajaElHuertoEntero()
        {
            for (int x = 0; x < 8; x++)
                for (int y = 0; y < 6; y++)
                    Assert.That(FarmPlot.IsUsable(x, y, 8), Is.True);
        }
    }
}
