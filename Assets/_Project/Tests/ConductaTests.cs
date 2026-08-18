using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Data.Islanders;
using Nimbo.Data.Player;
using Nimbo.Player;
using Nimbo.Social.Relationships;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// La personalidad del protagonista, deducida de lo que hace (§14.3).
    /// </summary>
    /// <remarks>
    /// Son las cinco comprobaciones que el propio diseño pide, y la primera es la que
    /// importa: **dos partidas que hacen lo mismo dan el mismo perfil aunque una dure
    /// veinte minutos y la otra tres horas.** Si eso falla, lo que se está midiendo es
    /// cuánto rato juega la gente y no cómo es, y el sistema entero no vale para nada.
    /// </remarks>
    public class ConductaTests
    {
        [TearDown]
        public void TearDown()
        {
            EventBus.Clear();
            ServiceRegistry.Clear();
        }

        // ── 1. la que invalida la idea entera si falla ───────────────────────

        [Test]
        public void LaSesionLargaYLaCortaDanElMismoPerfil()
        {
            var corta = new ConductRecord();
            var larga = new ConductRecord();

            // La misma conducta —tres cuartas partes del rato corriendo— en dos
            // sesiones de duración muy distinta.
            Correr(corta, segundos: 300f, proporcionCorriendo: 0.75f);
            Correr(larga, segundos: 9000f, proporcionCorriendo: 0.75f);

            Assert.That(corta.RawAxisOf(PersonalityAxis.Energy),
                Is.EqualTo(larga.RawAxisOf(PersonalityAxis.Energy)).Within(0.001f),
                "el eje mide la duración de la sesión y no el carácter: quien juega tres " +
                "horas saldría enérgico y quien juega veinte minutos, calmado");

            // Lo que sí puede —y debe— diferir es cuánto hay que fiarse. Cinco minutos
            // de juego son cinco minutos de pruebas: la conducta es la misma, la
            // evidencia no. Por eso lo de arriba se mide en crudo y esto aparte.
            Assert.That(corta.ConfidenceOf(PersonalityAxis.Energy),
                Is.LessThan(larga.ConfidenceOf(PersonalityAxis.Energy)));
        }

        [Test]
        public void LosCuatroEjesTardanParecidoEnCreerse()
        {
            // Las medidas continuas llegan en minutos y las de golpe de una en una. Si
            // llegaran en segundos, cuarenta se juntarían antes de cruzar el prado y la
            // Energía quedaría decidida en el primer paseo mientras la Expresión seguía
            // pidiendo cuarenta conversaciones — y entonces la confianza de un eje no
            // significaría lo mismo que la del otro.
            var andando = new ConductRecord();
            Correr(andando, segundos: 40f * 60f, proporcionCorriendo: 1f);

            var hablando = new ConductRecord();
            for (int i = 0; i < 40; i++) hablando.Note(PersonalityAxis.Expression, true);

            Assert.That(andando.ConfidenceOf(PersonalityAxis.Energy),
                Is.EqualTo(hablando.ConfidenceOf(PersonalityAxis.Expression)).Within(0.02f),
                "cuarenta minutos andando y cuarenta conversaciones tienen que valer lo mismo");
        }

        // ── 2. el arranque no miente ─────────────────────────────────────────

        [Test]
        public void UnEjeSinMuestrasDaCeroYConfianzaCero()
        {
            var conducta = new ConductRecord();

            Assert.That(conducta.AxisOf(PersonalityAxis.Outlook), Is.Zero);
            Assert.That(conducta.ConfidenceOf(PersonalityAxis.Outlook), Is.Zero);
        }

        [Test]
        public void ConPocasMuestrasElEjeTiraACero()
        {
            var pocas = new ConductRecord();
            var muchas = new ConductRecord();

            // Las dos con la misma conducta: siempre acompañado.
            for (int i = 0; i < 5; i++) pocas.Note(PersonalityAxis.Attitude, true);
            for (int i = 0; i < 500; i++) muchas.Note(PersonalityAxis.Attitude, true);

            float conPocas = pocas.AxisOf(PersonalityAxis.Attitude);
            float conMuchas = muchas.AxisOf(PersonalityAxis.Attitude);

            Assert.That(conPocas, Is.LessThan(conMuchas * 0.5f),
                "cinco momentos no pueden decidir cómo eres");
            Assert.That(conMuchas, Is.GreaterThan(0.85f),
                "quinientos sí, o el encogimiento no suelta nunca");
        }

        // ── 3. cambiar de verdad cuesta ──────────────────────────────────────

        [Test]
        public void CambiarDeConductaTardaDiasEnNotarse()
        {
            var conducta = new ConductRecord();

            // Dos semanas siendo de los que corren.
            for (int dia = 0; dia < 14; dia++)
            {
                Correr(conducta, segundos: 1800f, proporcionCorriendo: 1f);
                conducta.Decay();
            }

            float antes = conducta.AxisOf(PersonalityAxis.Energy);
            Assert.That(antes, Is.GreaterThan(0.8f));

            // Y ahora tres días andando. No basta ni de lejos.
            for (int dia = 0; dia < 3; dia++)
            {
                Correr(conducta, segundos: 1800f, proporcionCorriendo: 0f);
                conducta.Decay();
            }

            Assert.That(conducta.AxisOf(PersonalityAxis.Energy), Is.GreaterThan(0.3f),
                "si tres días bastaran para reescribirte, mover un eje a propósito " +
                "saldría más barato que buscar a alguien compatible");
        }

        [Test]
        public void PeroCambiarDeVerdadAcabaNotandose()
        {
            var conducta = new ConductRecord();

            for (int dia = 0; dia < 14; dia++)
            {
                Correr(conducta, segundos: 1800f, proporcionCorriendo: 1f);
                conducta.Decay();
            }

            for (int dia = 0; dia < 28; dia++)
            {
                Correr(conducta, segundos: 1800f, proporcionCorriendo: 0f);
                conducta.Decay();
            }

            Assert.That(conducta.AxisOf(PersonalityAxis.Energy), Is.LessThan(-0.4f),
                "puedes cambiar, y cambiar de verdad lleva semanas: eso es lo que hace " +
                "que signifique algo");
        }

        // ── 4. y 5. las reglas duras ─────────────────────────────────────────

        [Test]
        public void LaCompatibilidadConElJugadorNuncaSeSaleDeRango()
        {
            var conducta = new ConductRecord();
            for (int i = 0; i < 500; i++)
            {
                conducta.Note(PersonalityAxis.Energy, true);
                conducta.Note(PersonalityAxis.Attitude, false);
                conducta.Note(PersonalityAxis.Expression, true);
                conducta.Note(PersonalityAxis.Outlook, false);
            }

            var jugador = conducta.AsProfile();

            foreach (var otro in new[]
            {
                PersonalityProfile.FromTypeIndex(0),
                PersonalityProfile.FromTypeIndex(7),
                PersonalityProfile.FromTypeIndex(15),
            })
            {
                float compat = Compatibility.Between(jugador, otro);
                Assert.That(compat, Is.InRange(-1f, 1f));
            }
        }

        [Test]
        public void ElPerfilDelJugadorSeQuedaDentroDeMenosUnoYUno()
        {
            var conducta = new ConductRecord();
            for (int i = 0; i < 2000; i++) conducta.Note(PersonalityAxis.Energy, true, 10f);

            var perfil = conducta.AsProfile();

            Assert.That(perfil.Energy, Is.InRange(-1f, 1f));
            Assert.That(perfil.Energy, Is.LessThan(1f),
                "el encogimiento nunca llega a uno del todo, y está bien: siempre queda " +
                "sitio para ser un poco más de lo que eres");
        }

        // ── que lo escuche quien tiene que escucharlo ────────────────────────

        [Test]
        public void ElServicioDecaeAlPasarElDia()
        {
            var estado = new PlayerState();
            using var servicio = new ConductService(estado);

            for (int i = 0; i < 100; i++) servicio.Note(PersonalityAxis.Energy, true);
            float antes = estado.Conduct.SamplesOf(PersonalityAxis.Energy);

            EventBus.Publish(new DayPassed(2));

            Assert.That(estado.Conduct.SamplesOf(PersonalityAxis.Energy), Is.LessThan(antes),
                "sin decaimiento, la identidad se congela en lo que hiciste la primera " +
                "semana y ya no cambia nunca");
        }

        [Test]
        public void AlSoltarloDejaDeEscuchar()
        {
            var estado = new PlayerState();
            var servicio = new ConductService(estado);

            servicio.Note(PersonalityAxis.Energy, true, 100f);
            float antes = estado.Conduct.SamplesOf(PersonalityAxis.Energy);

            servicio.Dispose();
            EventBus.Publish(new DayPassed(2));

            Assert.That(estado.Conduct.SamplesOf(PersonalityAxis.Energy),
                Is.EqualTo(antes).Within(0.001f));
        }

        [Test]
        public void UnaPartidaViejaSinConductaNoRevienta()
        {
            var conducta = new ConductRecord { Positive = null, Total = null };

            Assert.DoesNotThrow(() => conducta.Note(PersonalityAxis.Energy, true));
            Assert.That(conducta.AsProfile().Energy, Is.Not.NaN);
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>Un rato moviéndose, con esa parte del tiempo corriendo.</summary>
        private static void Correr(ConductRecord conducta, float segundos,
                                   float proporcionCorriendo)
        {
            // En minutos, como lo hace el cuerpo del jugador de verdad: una muestra es
            // un momento, y con segundos la Energía se creería sola en el primer paseo.
            float minutos = segundos / 60f;
            conducta.Note(PersonalityAxis.Energy, true, minutos * proporcionCorriendo);
            conducta.Note(PersonalityAxis.Energy, false, minutos * (1f - proporcionCorriendo));
        }
    }
}
