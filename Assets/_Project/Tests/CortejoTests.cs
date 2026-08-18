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
using Nimbo.Items;
using Nimbo.Personality.Runtime;
using Nimbo.Player;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using Nimbo.Social.Romance;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El protagonista se declara, y le pueden decir que no (§14).
    /// </summary>
    /// <remarks>
    /// Lo importante de esta clase es lo que **no** se puede hacer: declararse sin
    /// ramo, sin ser amigo, dos veces seguidas o al día siguiente de un «no». El
    /// romance del protagonista no es una barra que se llena; si lo fuera, en un juego
    /// donde puedes hablar con la misma persona todos los días sería inevitable y
    /// aburrido.
    /// </remarks>
    public class CortejoTests
    {
        private GameClock _clock;
        private SaveGame _save;
        private IslanderRegistry _registry;
        private SimulationService _simulation;
        private SocialService _social;
        private EconomyService _economia;
        private InventoryService _mochila;
        private PlayerProgressionService _vias;
        private ConductService _conducta;

        private string _bea;

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
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();

            _simulation = new SimulationService(_registry, personalities, needsConfig);
            _economia = new EconomyService(catalog, _save);
            _mochila = new InventoryService(_save.Player, _economia);
            _vias = new PlayerProgressionService(_save.Player);
            _conducta = new ConductService(_save.Player);

            var foodIds = new List<string>();
            for (int i = 0; i < catalog.All.Count; i++)
                if (catalog.All[i].Category == ItemCategory.Food)
                    foodIds.Add(catalog.All[i].CatalogId);

            var factory = new IslanderFactory(_registry, personalities, _clock, foodIds);
            _social = new SocialService(_registry, personalities, _simulation, factory,
                                        _clock, socialConfig, _save.Triangles);

            ServiceRegistry.Register<IIslanderRegistry>(_registry);
            ServiceRegistry.Register<ISimulationService>(_simulation);
            ServiceRegistry.Register<IEconomyService>(_economia);
            ServiceRegistry.Register<IInventoryService>(_mochila);
            ServiceRegistry.Register<ISocialService>(_social);
            ServiceRegistry.Register<IPlayerProgression>(_vias);
            ServiceRegistry.Register<IConductService>(_conducta);

            var bea = factory.CreateRandom("bea");
            _registry.Add(bea);
            _save.Islanders.Add(bea);
            _bea = bea.Id;

            // Convivencia 5, que es lo que pide atreverse (§14.2).
            for (int nivel = 1; nivel < 5; nivel++)
                _vias.Grant(Data.Player.SkillKind.Social, Data.Player.SkillSet.XpForLevel(nivel));
        }

        [TearDown]
        public void TearDown()
        {
            _conducta?.Dispose();
            _vias?.Dispose();
            _social?.Dispose();
            _economia?.Dispose();
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── lo que hace falta ────────────────────────────────────────────────

        [Test]
        public void ElRamoEstaEnElCatalogoYSeFabrica()
        {
            Assert.That(_economia.GetItem(Courtship.BouquetId), Is.Not.Null,
                "sin ramo en el catálogo no hay forma de declararse");
        }

        [Test]
        public void SinSerAmigosNiSeIntenta()
        {
            DarRamo();

            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.NotFriendEnough));
        }

        [Test]
        public void SinRamoTampoco()
        {
            SerAmigos(70f);

            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.NoBouquet),
                "con las manos vacías no: el ramo es lo que ata el cortejo a la isla");
        }

        [Test]
        public void SinConvivenciaCincoTampoco()
        {
            // Una partida recién empezada, con la vía social a uno.
            _save.Player.Skills.Lines.Clear();
            SerAmigos(70f);
            DarRamo();

            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.NotUnlocked));
        }

        [Test]
        public void ConTodoEnRegla_SePuede()
        {
            SerAmigos(70f);
            DarRamo();

            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.Ok));
        }

        // ── declararse ───────────────────────────────────────────────────────

        [Test]
        public void DeclararseGastaElRamoYDejaLaRespuestaParaManana()
        {
            SerAmigos(70f);
            DarRamo();

            Assert.That(_social.PlayerConfess(_bea), Is.True);

            Assert.That(_mochila.CountOf(Courtship.BouquetId), Is.Zero, "el ramo no se gastó");
            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Confessed),
                "un sí o un no en el mismo clic convierte la declaración en una tirada " +
                "de dados que se mira una vez");
        }

        [Test]
        public void NoSePuedeInsistirElMismoDia()
        {
            SerAmigos(70f);
            DarRamo();
            _social.PlayerConfess(_bea);
            DarRamo();

            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.AlreadyCourting));
        }

        // ── la respuesta ─────────────────────────────────────────────────────

        [Test]
        public void ConAfinidadDeSobraDiceQueSi()
        {
            SerAmigos(95f);
            Declararse();

            PasarElDia();

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Dating));
        }

        [Test]
        public void ConPocaAfinidadDiceQueNo()
        {
            SerAmigos(30f);
            Declararse();

            PasarElDia();

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.None));
        }

        [Test]
        public void ElNoCuestaAnimoYUnosDiasDeEspera()
        {
            SerAmigos(30f);
            float antes = AfinidadConmigo();
            Declararse();

            PasarElDia();

            Assert.That(AfinidadConmigo(), Is.LessThan(antes));
            Assert.That(_social.CanConfess(_bea), Is.EqualTo(CourtshipRefusal.TooSoon),
                "sin la espera, declararse otra vez mañana convierte el cortejo en " +
                "insistir hasta que salga");
        }

        [Test]
        public void ElNoLlegaConUnaFraseQueExplicaAlgo()
        {
            string frase = null;
            void Escuchar(CourtshipAnswered e) => frase = e.Line;
            EventBus.Subscribe<CourtshipAnswered>(Escuchar);

            SerAmigos(30f);
            Declararse();
            PasarElDia();

            EventBus.Unsubscribe<CourtshipAnswered>(Escuchar);

            Assert.That(frase, Is.Not.Null.And.Not.Empty,
                "un «no» sin explicación se lee como arbitrario, y el jugador no ve el " +
                "número que lo decidió");
        }

        [Test]
        public void ParecerseBajaElListonYNoParecerseLoSube()
        {
            // La compatibilidad **pone el precio, no el veredicto**: nadie es imposible,
            // a los opuestos les cuesta más convivencia. Con la misma afinidad, quien se
            // parece dice que sí y quien no, que no.
            var bea = _registry.Get(_bea);
            bea.Personality = PersonalityProfile.FromTypeIndex(0);

            Parecerse(bea.Personality);
            SerAmigos(70f);
            Declararse();
            PasarElDia();

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Dating),
                "con setenta de afinidad y pareciéndoos, tenía que salir");
        }

        [Test]
        public void NadieEsImposible()
        {
            var bea = _registry.Get(_bea);
            bea.Personality = PersonalityProfile.FromTypeIndex(0);

            // Lo contrario de ella, y aun así con afinidad de sobra sale.
            Parecerse(new PersonalityProfile
            {
                Energy = -bea.Personality.Energy,
                Expression = -bea.Personality.Expression,
                Attitude = -bea.Personality.Attitude,
                Outlook = -bea.Personality.Outlook,
            });

            SerAmigos(95f);
            Declararse();
            PasarElDia();

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.Dating),
                "los opuestos pueden quererse; solo cuesta el doble de convivencia");
        }

        [Test]
        public void NadieSeEnamoraDeTiPorAcumularCharlas()
        {
            SerAmigos(99f);

            // Un mes entero de convivencia, sin declararse nunca.
            for (int dia = 2; dia < 32; dia++) EventBus.Publish(new DayPassed(dia));

            Assert.That(EtapaConmigo(), Is.EqualTo(RomanceStage.None),
                "el romance por acumulación es inevitable y aburrido en un juego donde " +
                "puedes hablar con la misma persona todos los días");
        }

        // ── utilidades ───────────────────────────────────────────────────────

        private void SerAmigos(float afinidad)
        {
            var bea = _registry.Get(_bea);
            var record = bea.Relationships.GetOrCreate(SocialIds.Player);
            record.Affinity = afinidad;
            record.Friendship = FriendshipStage.Friend;
            record.Interactions = 10;
            bea.Relationships.Set(record);
        }

        private void DarRamo() => _mochila.TryStore(Courtship.BouquetId, 1, out _);

        private void Declararse()
        {
            DarRamo();
            Assert.That(_social.PlayerConfess(_bea), Is.True, "no se ha podido declarar");
        }

        /// <summary>Amanece: es cuando se contesta.</summary>
        private void PasarElDia() => EventBus.Publish(new DayPassed(_clock.Day + 1));

        /// <summary>Le da al protagonista una conducta parecida a ese perfil.</summary>
        private void Parecerse(in PersonalityProfile profile)
        {
            var conduct = _save.Player.Conduct;

            Fill(Data.Islanders.PersonalityAxis.Energy, profile.Energy);
            Fill(Data.Islanders.PersonalityAxis.Expression, profile.Expression);
            Fill(Data.Islanders.PersonalityAxis.Attitude, profile.Attitude);
            Fill(Data.Islanders.PersonalityAxis.Outlook, profile.Outlook);

            void Fill(Data.Islanders.PersonalityAxis axis, float value)
            {
                // Muchas muestras para que el encogimiento no se coma el perfil.
                const float samples = 4000f;
                float positive = (value + 1f) * 0.5f * samples;

                conduct.Note(axis, true, positive);
                conduct.Note(axis, false, samples - positive);
            }
        }

        private RomanceStage EtapaConmigo() =>
            _social.PlayerRelationship(_bea).Romance;

        private float AfinidadConmigo() =>
            _social.PlayerRelationship(_bea).Affinity;
    }
}
