using System.Collections.Generic;
using Nimbo.Data.Islanders;
using Nimbo.UI.Islander;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// La agenda que la ficha enseña: que cubra el día, que sea de cada uno y que no
    /// se separe de las reglas que dice copiar.
    /// </summary>
    /// <remarks>
    /// <see cref="AgendaProjection"/> proyecta el día con las dos reglas fijas del
    /// cerebro —la franja de sueño y el paseo por personalidad— porque la agenda real
    /// no existe: <c>IslanderBrain</c> decide hora a hora y no guarda nada. Estas
    /// pruebas son lo que mantiene la copia pegada al original: si alguien cambia el
    /// umbral o los bordes del sueño en el cerebro, aquí se nota, porque el
    /// comportamiento de frontera está clavado a los literales de
    /// IslanderBrain.cs:126-129 y GameClock.cs:59.
    /// </remarks>
    public class AgendaDeLosVecinosTests
    {
        private static readonly PersonalityProfile Sociable =
            new PersonalityProfile(-0.75f, -0.75f, 0.75f, -0.75f);
        private static readonly PersonalityProfile Sonador =
            new PersonalityProfile(-0.75f, -0.75f, -0.75f, 0.75f);
        private static readonly PersonalityProfile Energetico =
            new PersonalityProfile(0.75f, -0.75f, -0.75f, -0.75f);
        private static readonly PersonalityProfile Casero =
            new PersonalityProfile(-0.75f, -0.75f, -0.75f, -0.75f);

        [Test]
        public void LaAgendaCubreLasVeinticuatroHorasSinHuecos()
        {
            var blocks = AgendaProjection.Day(Sociable, hasHome: true);

            Assert.That(blocks, Is.Not.Empty);
            int cubiertas = 0;
            foreach (var block in blocks)
            {
                int largo = block.EndHour - block.StartHour;
                if (largo <= 0) largo += 24; // el sueño envuelve: 23 → 7
                cubiertas += largo;
            }

            Assert.That(cubiertas, Is.EqualTo(24),
                "una agenda con huecos dice horas que nadie va a mirar");
        }

        [Test]
        public void CuatroCaracteresDistintosDanCuatroDiasDistintos()
        {
            string plaza = AgendaProjection.Haunt(Sociable);
            string parque = AgendaProjection.Haunt(Sonador);
            string ocio = AgendaProjection.Haunt(Energetico);
            string casa = AgendaProjection.Haunt(Casero);

            Assert.That(plaza, Is.Not.EqualTo(parque));
            Assert.That(parque, Is.Not.EqualTo(ocio));
            Assert.That(ocio, Is.Not.EqualTo(casa));
            Assert.That(casa, Is.Not.EqualTo(plaza),
                "si el independiente y el sociable tienen el mismo día, la agenda " +
                "enseña datos y no un personaje");
        }

        /// <summary>
        /// Los dieciséis tipos canónicos caen en las cuatro ramas del paseo, y ninguna
        /// queda huérfana: si una rama dejara de producirse, su frase sería texto muerto.
        /// </summary>
        [Test]
        public void LosDieciseisTiposRepartenLosCuatroPlanes()
        {
            var dias = new HashSet<string>();
            for (int tipo = 0; tipo < PersonalityProfile.TypeCount; tipo++)
                dias.Add(AgendaProjection.Haunt(PersonalityProfile.FromTypeIndex(tipo)));

            Assert.That(dias, Has.Count.EqualTo(4),
                "cada rama de WanderByPersonality tiene que tener su día reconocible");
        }

        [Test]
        public void ElUmbralDelPaseoEsElMismoQueElDelCerebro()
        {
            // En el cerebro la comparación es estricta (> 0,3): exactamente 0,3 NO es
            // todavía «sociable». La proyección copia el literal, así que la frontera
            // tiene que caer igual de lado.
            var justoEnElUmbral = new PersonalityProfile(-0.75f, -0.75f, 0.3f, -0.75f);
            var unPelilloPorEncima = new PersonalityProfile(-0.75f, -0.75f, 0.31f, -0.75f);

            Assert.That(AgendaProjection.Haunt(justoEnElUmbral),
                        Is.Not.EqualTo(AgendaProjection.Haunt(Sociable)),
                        "con 0,3 exacto el cerebro aún no va a la plaza");
            Assert.That(AgendaProjection.Haunt(unPelilloPorEncima),
                        Is.EqualTo(AgendaProjection.Haunt(Sociable)));
        }

        [Test]
        public void LaMismaPersonalidadDaLaMismaAgenda()
        {
            var primera = AgendaProjection.Day(Sonador, hasHome: true);
            var segunda = AgendaProjection.Day(Sonador, hasHome: true);

            Assert.That(primera.Count, Is.EqualTo(segunda.Count));
            for (int i = 0; i < primera.Count; i++)
                Assert.That(primera[i].Text, Is.EqualTo(segunda[i].Text),
                    "la agenda sale de la personalidad, no del azar: si cambiara entre " +
                    "llamadas, el refresco de 0,4 s repintaría la tarjeta sin parar");
        }

        [Test]
        public void ElQueNoTieneCasaDuermeDistinto()
        {
            var conCasa = AgendaProjection.Day(Casero, hasHome: true);
            var sinCasa = AgendaProjection.Day(Casero, hasHome: false);

            Assert.That(sinCasa[1].Text, Is.Not.EqualTo(conCasa[1].Text),
                "el cerebro solo lo manda a dormir a casa si tiene (IslanderBrain.cs:112); " +
                "prometerle cama al que no tiene es mentirle al jugador");
            Assert.That(sinCasa[1].Text, Does.Contain("casa").Or.Contain("pille"),
                "el texto tiene que explicar dónde duerme entonces");
        }

        [Test]
        public void ElSuenoEnvolventeMarcaBienLaHora()
        {
            var blocks = AgendaProjection.Day(Sociable, hasHome: true);

            AgendaBlock dia = blocks[0];
            AgendaBlock sueno = blocks[1];

            Assert.That(dia.Includes(7), Is.True, "a las 7 ya se levantó");
            Assert.That(dia.Includes(22), Is.True);
            Assert.That(dia.Includes(23), Is.False, "a las 23 ya está acostado");

            Assert.That(sueno.Includes(23), Is.True);
            Assert.That(sueno.Includes(3), Is.True, "las 3 de la madrugada es sueño, aunque venga después de medianoche");
            Assert.That(sueno.Includes(6), Is.True);
            Assert.That(sueno.Includes(7), Is.False);
        }
    }
}
