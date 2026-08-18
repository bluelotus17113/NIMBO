using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Save;
using Nimbo.Data.Social;
using Nimbo.Economy;
using Nimbo.Economy.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Dos vecinos colados por la misma persona, y enterándose (§13.2).
    /// </summary>
    /// <remarks>
    /// Podía pasar desde siempre y no pasaba nada: cada uno suspiraba por su lado sin
    /// saber del otro. Lo que se prueba aquí es que ahora sí pasa algo, que dura, que
    /// se resuelve por lo que uno esperaría, y —lo más importante— que **no se cura
    /// solo**: una rivalidad que se evapora a la mañana siguiente no es una historia,
    /// es un parpadeo.
    ///
    /// Va con el <see cref="SocialService"/> de verdad. Los flechazos se escriben a
    /// mano porque llegar hasta uno por convivencia son decenas de días de juego y lo
    /// que se prueba aquí es lo que viene después.
    /// </remarks>
    public class TriangulosTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private SocialService _social;
        private SocialConfig _config;

        private string _bea;    // por quien compiten
        private string _leo;    // el que se lleva mejor con ella
        private string _nil;    // el otro

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _save = new SaveGame { ElapsedMinutes = 8 * GameClock.MinutesPerHour };
            _clock = new GameClock(_save.ElapsedMinutes);
            _registry = new IslanderRegistry();

            var personalities = PersonalityRoster.CreateService();
            var catalog = new ItemCatalog();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            _config = ScriptableObject.CreateInstance<SocialConfig>();

            _simulation = new SimulationService(_registry, personalities, needsConfig);
            var economy = new EconomyService(catalog, _save);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, _simulation, factory,
                                        _clock, _config, _save.Triangles);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_simulation);
            ServiceRegistry.Register<IEconomyService>(economy);
            ServiceRegistry.Register<ISocialService>(_social);

            _bea = Nacer(factory, "bea");
            _leo = Nacer(factory, "leo");
            _nil = Nacer(factory, "nil");
        }

        [TearDown]
        public void TearDown()
        {
            _social?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── que nazca ────────────────────────────────────────────────────────

        [Test]
        public void DosColadosPorLaMismaPersonaSeVuelvenRivales()
        {
            Triangulo();

            Assert.That(ConflictoEntre(_leo, _nil), Is.EqualTo(ConflictStage.Rivalry));
            Assert.That(ConflictoEntre(_nil, _leo), Is.EqualTo(ConflictStage.Rivalry),
                "la rivalidad es de dos: puesta en un solo lado, uno ve un rival donde " +
                "el otro ve a un vecino cualquiera");
            Assert.That(_save.Triangles, Has.Count.EqualTo(1));
        }

        [Test]
        public void UnFlechazoASolasNoAbreNada()
        {
            Colar(_leo, _bea);
            _social.Triangles.OnCrushBorn(_leo, _bea, day: 1);

            Assert.That(_save.Triangles, Is.Empty);
            Assert.That(ConflictoEntre(_leo, _nil), Is.EqualTo(ConflictStage.None));
        }

        [Test]
        public void ElMismoTrianguloNoSeAbreDosVeces()
        {
            Triangulo();
            _social.Triangles.OnCrushBorn(_nil, _bea, day: 1);

            Assert.That(_save.Triangles, Has.Count.EqualTo(1),
                "los dos miran hacia atrás al enamorarse, y el triángulo es uno solo");
        }

        [Test]
        public void QuienYaSaleConEllaNoEsUnRival()
        {
            Poner(_leo, _bea, RomanceStage.Dating, 80f);
            Colar(_nil, _bea);
            _social.Triangles.OnCrushBorn(_nil, _bea, day: 1);

            Assert.That(_save.Triangles, Is.Empty,
                "quien ya está con ella no es un pretendiente: es la pareja, y eso es " +
                "otra historia distinta");
        }

        [Test]
        public void UnaRiñaDeVerdadNoSeRebajaARivalidad()
        {
            Poner(_leo, _nil, RomanceStage.None, -80f);
            var libro = _registry.Get(_leo).Relationships;
            libro.TryGet(_nil, out var ficha);
            ficha.Conflict = ConflictStage.Feud;
            libro.Set(ficha);

            Triangulo();

            Assert.That(ConflictoEntre(_leo, _nil), Is.EqualTo(ConflictStage.Feud),
                "ya se llevaban peor que esto; lo que hay entre ellos sigue siendo lo peor");
        }

        // ── que dure ─────────────────────────────────────────────────────────

        [Test]
        public void CadaDiaQueDuraSeLlevanUnPocoPeor()
        {
            Triangulo();
            float antes = AfinidadEntre(_leo, _nil);

            _social.Triangles.AdvanceDay(2);

            Assert.That(AfinidadEntre(_leo, _nil), Is.LessThan(antes));
            Assert.That(AfinidadEntre(_leo, _bea), Is.EqualTo(70f).Within(0.01f),
                "con ella no se enfadan: no es culpa suya");
        }

        [Test]
        public void LaRivalidadNoSeCuraSola()
        {
            Triangulo();

            // Un día entero de la simulación de verdad: enfriamiento, reevaluación de
            // etapas y flechazos nuevos. La rivalidad tiene que seguir ahí.
            EventBus.Publish(new DayPassed(2));

            Assert.That(ConflictoEntre(_leo, _nil), Is.EqualTo(ConflictStage.Rivalry),
                "la reevaluación diaria la borraba y el triángulo no llegaba a verse");
        }

        [Test]
        public void DisculparseLaLevanta()
        {
            Triangulo();
            _social.Interact(_leo, _nil, SocialInteraction.Apologize);

            Assert.That(ConflictoEntre(_leo, _nil), Is.EqualTo(ConflictStage.None));
            Assert.That(ConflictoEntre(_nil, _leo), Is.EqualTo(ConflictStage.None),
                "se disculpa uno y se levanta en los dos: si no, el otro seguiría viendo " +
                "un rival en quien vino a pedirle perdón");
        }

        [Test]
        public void SiUnoSeDesenamoraElTrianguloSeCierraSinDrama()
        {
            Triangulo();
            Poner(_nil, _bea, RomanceStage.None, 20f);

            _social.Triangles.AdvanceDay(2);

            Assert.That(_save.Triangles, Is.Empty);
            Assert.That(EtapaDe(_leo, _bea), Is.EqualTo(RomanceStage.Crush),
                "al que sigue colado no le pasa nada: simplemente ya no hay competencia");
        }

        // ── que se resuelva ──────────────────────────────────────────────────

        [Test]
        public void ALosSeisDiasGanaElQueMejorSeLlevaConElla()
        {
            Triangulo(afinidadLeo: 80f, afinidadNil: 55f);

            Resolver();

            Assert.That(EtapaDe(_leo, _bea), Is.EqualTo(RomanceStage.Dating));
            Assert.That(EtapaDe(_bea, _leo), Is.EqualTo(RomanceStage.Dating),
                "salir es cosa de dos: escrito en una sola agenda, el evaluador lo " +
                "deshace a la mañana siguiente");
        }

        [Test]
        public void ElQuePierdeSeQuedaSinNadaYConLaEsperaPuesta()
        {
            Triangulo(afinidadLeo: 80f, afinidadNil: 55f);

            Resolver();

            Assert.That(EtapaDe(_nil, _bea), Is.EqualTo(RomanceStage.None));
            Assert.That(_registry.Get(_nil).CrushBlockedUntilDay,
                Is.GreaterThan(1 + _config.TriangleDays),
                "sin la espera se encapricha de otra al día siguiente y el desamor no " +
                "significa nada");
        }

        [Test]
        public void AntesDePlazoNoSeResuelveNada()
        {
            Triangulo(afinidadLeo: 80f, afinidadNil: 55f);

            for (int dia = 2; dia < 1 + _config.TriangleDays; dia++)
                _social.Triangles.AdvanceDay(dia);

            Assert.That(_save.Triangles, Has.Count.EqualTo(1));
            Assert.That(EtapaDe(_leo, _bea), Is.EqualTo(RomanceStage.Crush));
        }

        [Test]
        public void ElTrianguloSeCierraAlResolverse()
        {
            Triangulo();
            Resolver();

            Assert.That(_save.Triangles, Is.Empty);
        }

        [Test]
        public void AlQuePierdeNoLeNaceOtroFlechazoDeInmediato()
        {
            Triangulo(afinidadLeo: 80f, afinidadNil: 55f);
            Resolver();

            // Una tercera con la que se llevaría de maravilla. Aun así, hoy no.
            var otra = _registry.Get(_bea);
            var nil = _registry.Get(_nil);
            Poner(_nil, otra.Id, RomanceStage.None, 0f);

            EventBus.Publish(new DayPassed(1 + _config.TriangleDays + 1));

            Assert.That(nil.CrushBlockedUntilDay,
                Is.GreaterThanOrEqualTo(1 + _config.TriangleDays + 1));
        }

        // ── utilidades ───────────────────────────────────────────────────────

        /// <summary>
        /// Deja a los dos colados por ella y abre el triángulo, como haría el día que
        /// le nace el flechazo al segundo.
        /// </summary>
        private void Triangulo(float afinidadLeo = 70f, float afinidadNil = 70f)
        {
            Colar(_leo, _bea, afinidadLeo);
            Colar(_nil, _bea, afinidadNil);

            // Los dos empiezan llevándose bien: así se ve que la rivalidad los separa.
            Poner(_leo, _nil, RomanceStage.None, 40f);
            Poner(_nil, _leo, RomanceStage.None, 40f);

            _social.Triangles.OnCrushBorn(_nil, _bea, day: 1);
        }

        /// <summary>Pasa los días que hagan falta hasta que el plazo se cumple.</summary>
        private void Resolver()
        {
            for (int dia = 2; dia <= 1 + _config.TriangleDays; dia++)
                _social.Triangles.AdvanceDay(dia);
        }

        private void Colar(string quien, string porQuien, float afinidad = 70f) =>
            Poner(quien, porQuien, RomanceStage.Crush, afinidad);

        private void Poner(string deQuien, string aQuien, RomanceStage etapa, float afinidad)
        {
            var islander = _registry.Get(deQuien);
            var record = islander.Relationships.GetOrCreate(aQuien);
            record.Romance = etapa;
            record.Affinity = afinidad;
            record.Friendship = FriendshipStage.Friend;
            record.Interactions = 5;
            islander.Relationships.Set(record);
        }

        private string Nacer(IslanderFactory factory, string semilla)
        {
            var islander = factory.CreateRandom(semilla);
            _registry.Add(islander);
            _save.Islanders.Add(islander);
            return islander.Id;
        }

        private ConflictStage ConflictoEntre(string a, string b) =>
            _social.GetRelationship(a, b).Conflict;

        private float AfinidadEntre(string a, string b) =>
            _social.GetRelationship(a, b).Affinity;

        private RomanceStage EtapaDe(string deQuien, string aQuien) =>
            _social.GetRelationship(deQuien, aQuien).Romance;
    }
}
