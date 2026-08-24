using System.Collections.Generic;
using Nimbo.CharacterCreator;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Core.Time;
using Nimbo.Data.Islanders;
using Nimbo.Data.Social;
using Nimbo.Data.World;
using Nimbo.Personality.Runtime;
using Nimbo.Simulation;
using Nimbo.Simulation.Needs;
using Nimbo.Social;
using Nimbo.Social.Memory;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que un vecino saque en conversación algo de lo que pasó: la riña de ayer, la
    /// pareja nueva, lo propio antes que el cotilleo — y que calle cuando toca.
    /// </summary>
    /// <remarks>
    /// La vida social quedaba registrada en la crónica y al hablar nadie te mencionaba
    /// nada. Estas pruebas no montan partida: el recuerdo lee la crónica que le cuelan
    /// por <see cref="IChronicleService"/> y las banderas son una lista cualquiera, que
    /// es el mismo pacto de siempre («sin partida se lleva en memoria»).
    /// </remarks>
    public class MemoriaConversacionalTests
    {
        private GameClock _clock;
        private IslanderRegistry _registry;
        private CronicaFalsa _cronica;
        private List<string> _flags;
        private ConversationRecall _recall;

        private IslanderData _bea;   // expresiva
        private IslanderData _mia;   // reservada
        private IslanderData _leo;

        private const string RinaDeAyer = "¡Bea y Mia han discutido!";

        [SetUp]
        public void SetUp()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();

            _clock = new GameClock(8 * GameClock.MinutesPerHour); // día 1
            _registry = new IslanderRegistry();
            _cronica = new CronicaFalsa();
            _flags = new List<string>();

            ServiceRegistry.Register<IChronicleService>(_cronica);

            _bea = NuevoVecino("Bea", 0.75f);
            _mia = NuevoVecino("Mia", -0.75f);
            _leo = NuevoVecino("Leo", 0.75f);

            // Dado a 1: las pruebas de contenido deciden qué hay, no si salió cara o cruz.
            _recall = new ConversationRecall(_registry, _clock, _flags)
            {
                ChanceFloor = 1f,
                ChanceCeiling = 1f,
            };
        }

        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── puede mencionar ─────────────────────────────────────────────────

        [Test]
        public void UnVecinoPuedeMencionarUnaRinaDeAyer()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

            string linea = _recall.Peek(_bea.Id);
            Assert.That(linea, Is.Not.Null,
                        "una riña de ayer es justo lo que un vecino puede sacar al hablar");
            Assert.That(linea, Does.Contain("discutid"),
                        "la frase trae el suceso tal cual lo escribió la crónica");

            Assert.That(_recall.TryGetRecall(_bea.Id, out string dicha), Is.True,
                        "con el dado a 1, la conversación lo suelta");
            Assert.That(dicha, Does.Contain("discutid"));
        }

        [Test]
        public void UnaParejaNuevaTambienSeCuenta()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, "¡Mia y Leo ya son pareja!"));

            Assert.That(_recall.Peek(_bea.Id), Does.Contain("ya son pareja"),
                        "las parejas nuevas son material de conversación");
        }

        // ── envejece ────────────────────────────────────────────────────────

        [Test]
        public void DejaDeMencionarloCuandoEnvejece()
        {
            _cronica.Lineas.Add(new ChronicleEntry(1, RinaDeAyer));

            AvanzarADia(4); // edad 3: justo dentro de la ventana
            Assert.That(_recall.Peek(_bea.Id), Is.Not.Null,
                        "a tres días el suceso todavía está caliente");

            AvanzarADia(5); // edad 4: fuera
            Assert.That(_recall.Peek(_bea.Id), Is.Null,
                        "lo de hace cuatro días ya no se cuenta");
        }

        // ── no repite ───────────────────────────────────────────────────────

        [Test]
        public void NoTeRepiteLoQueYaTeConto()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.True);
            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.False,
                        "contado una vez, no se repite el mismo día");

            AvanzarADia(2); // el suceso sigue fresco (edad 2)
            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.False,
                        "ni al día siguiente, mientras siga dentro de la ventana");

            // «Cerrar y abrir el juego»: otro recuerdo con las mismas banderas de partida.
            var reencarnado = new ConversationRecall(_registry, _clock, _flags)
            {
                ChanceFloor = 1f,
                ChanceCeiling = 1f,
            };
            Assert.That(reencarnado.TryGetRecall(_bea.Id, out _), Is.False,
                        "el ya-contado vive en las banderas de la partida, no en memoria de sesión");
        }

        [Test]
        public void ContarUnaCosaLeCierraElDia()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, "¡Ha nacido Cielo! Ana y Leo son padres."));

            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.True);
            Assert.That(_recall.Peek(_bea.Id), Is.Null,
                        "una historia al día por vecino: quien cuenta tres seguidas es un boletín");

            AvanzarADia(2);
            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.True,
                        "al día siguiente vuelve a haber sitio para una");
        }

        // ── la voz depende de quién sea ─────────────────────────────────────

        [Test]
        public void UnaReservadaNoCuentaCotilleosAjenos()
        {
            // De Bea y Leo, que no van con Mia: es cotilleo ajeno para ella.
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, "¡Bea y Leo ya son pareja!"));

            Assert.That(_recall.Peek(_mia.Id), Is.Null,
                        "lo de los demás no va con una reservada");

            _mia.Personality = new PersonalityProfile(0f, 0.75f, 0f, 0f);
            Assert.That(_recall.Peek(_mia.Id), Is.Not.Null,
                        "la misma historia, en boca de alguien expresivo, sale");
        }

        [Test]
        public void LoPropioLoCuentaHastaUnaReservada()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, "¡Mia ha llegado al nivel 10!"));

            Assert.That(_recall.Peek(_mia.Id), Is.Not.Null,
                        "lo propio se cuenta aunque uno sea de pocas palabras");
        }

        [Test]
        public void ElMismoSucesoNoSeCuentaIgualEnBocasDistintas()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, "¡Mia y Leo ya son pareja!"));

            string deCotilla = _recall.Peek(_bea.Id);     // Bea, expresiva: cotilleo ajeno
            string deInteresada = _recall.Peek(_mia.Id);  // Mia, reservada: es lo suyo

            Assert.That(deCotilla, Does.Contain("ya son pareja"));
            Assert.That(deInteresada, Does.Contain("ya son pareja"));
            Assert.That(deCotilla, Is.Not.EqualTo(deInteresada),
                        "un cotilleo ajeno y la propia historia no salen con la misma voz");
        }

        // ── verdad presente ─────────────────────────────────────────────────

        [Test]
        public void UnaRinaCompuestaYaNoSeCuenta()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

            var deBea = _bea.Relationships.GetOrCreate(_mia.Id);
            deBea.Conflict = ConflictStage.Quarrel;
            _bea.Relationships.Set(deBea);
            var deMia = _mia.Relationships.GetOrCreate(_bea.Id);
            deMia.Conflict = ConflictStage.Quarrel;
            _mia.Relationships.Set(deMia);

            Assert.That(_recall.Peek(_bea.Id), Is.Not.Null, "estando la riña viva, es contable");

            // Se componen — como hace el servicio al mediar, en los dos lados.
            deBea.Conflict = ConflictStage.None;
            _bea.Relationships.Set(deBea);
            deMia.Conflict = ConflictStage.None;
            _mia.Relationships.Set(deMia);

            Assert.That(_recall.Peek(_bea.Id), Is.Null,
                        "compuesta la riña, contarla ya no dice nada");
        }

        // ── el dado ─────────────────────────────────────────────────────────

        [Test]
        public void ConElDadoACeroHayMaterialPeroNoSale()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

            _recall.ChanceFloor = 0f;
            _recall.ChanceCeiling = 0f;

            Assert.That(_recall.Peek(_bea.Id), Is.Not.Null,
                        "haber material no depende del dado");
            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.False,
                        "el dado decide si hoy le apetece contarlo");
            Assert.That(_recall.Peek(_bea.Id), Is.Not.Null,
                        "y como no lo contó, el material sigue ahí para mañana");
        }

        [Test]
        public void ElDadoDelDiaEsPropiedadDelDia()
        {
            _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

            _recall.ChanceFloor = 0.2f;
            _recall.ChanceCeiling = 0.4f;
            bool primera = _recall.TryGetRecall(_bea.Id, out _);

            // Otra instancia, banderas aparte: mismo día, misma semilla, misma suerte.
            var gemela = new ConversationRecall(_registry, _clock, new List<string>())
            {
                ChanceFloor = 0.2f,
                ChanceCeiling = 0.4f,
            };
            Assert.That(gemela.TryGetRecall(_bea.Id, out _), Is.EqualTo(primera),
                        "que hoy le apetezca contar es un hecho del día, no de cada charla");
        }

        // ── bordes ──────────────────────────────────────────────────────────

        [Test]
        public void SinCronicaNoSeCuelgaYCalla()
        {
            ServiceRegistry.Clear(); // se va la crónica

            Assert.DoesNotThrow(() => _recall.TryGetRecall(_bea.Id, out _),
                                "sin crónica no hay nada que leer, pero tampoco nada que reviente");
            Assert.That(_recall.TryGetRecall(_bea.Id, out _), Is.False);
        }

        [Test]
        public void ElServicioSocialExponeLoQueElVecinoContaria()
        {
            var personalities = PersonalityRoster.CreateService();
            var needsConfig = ScriptableObject.CreateInstance<NeedsConfig>();
            var socialConfig = ScriptableObject.CreateInstance<SocialConfig>();
            var simulation = new SimulationService(_registry, personalities, needsConfig);
            var factory = new IslanderFactory(_registry, personalities, _clock, new List<string>());

            // Con las banderas de la partida: el «ya te lo contó» debe sobrevivirle al guardado.
            var social = new SocialService(_registry, personalities, simulation, factory,
                                           _clock, socialConfig, null, null, _flags);
            try
            {
                social.Recall.ChanceFloor = 1f;
                social.Recall.ChanceCeiling = 1f;

                _cronica.Lineas.Add(new ChronicleEntry(_clock.Day - 1, RinaDeAyer));

                Assert.That(social.RecallLine(_bea.Id), Does.Contain("discutid"),
                            "el servicio social es la puerta del recuerdo conversacional");
                Assert.That(social.RecallLine(_bea.Id), Is.Null,
                            "y una vez contado, calla");
            }
            finally
            {
                social.Dispose();
            }
        }

        // ── utilería ────────────────────────────────────────────────────────

        private IslanderData NuevoVecino(string nombre, float expresion)
        {
            var vecino = new IslanderData
            {
                Identity = new IslanderIdentity
                {
                    Id = nombre.ToLowerInvariant(),
                    DisplayName = nombre,
                },
                Personality = new PersonalityProfile(0f, expresion, 0f, 0f),
            };
            _registry.Add(vecino);
            return vecino;
        }

        private void AvanzarADia(int dia) =>
            _clock.SetElapsed((dia - 1) * (long)GameClock.MinutesPerDay
                              + 8L * GameClock.MinutesPerHour);

        private class CronicaFalsa : IChronicleService
        {
            public readonly List<ChronicleEntry> Lineas = new List<ChronicleEntry>();

            public IReadOnlyList<ChronicleEntry> Entries => Lineas;
        }
    }
}
