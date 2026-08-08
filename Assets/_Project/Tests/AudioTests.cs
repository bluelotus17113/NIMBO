using Nimbo.Art.Audio;
using Nimbo.Data.Islanders;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// El audio generado. No se puede comprobar que suene bien desde un test, pero sí
    /// que suene: que tenga muestras, que no sature y que dos voces distintas no den
    /// la misma onda. Lo de «suena bien» hay que escucharlo, como el arte hay que mirarlo.
    /// </summary>
    public class VoiceSynthTests
    {
        private static float[] SamplesOf(AudioClip clip)
        {
            var samples = new float[clip.samples * clip.channels];
            clip.GetData(samples, 0);
            return samples;
        }

        [Test]
        public void UnaFraseProduceSonido()
        {
            var clip = VoiceSynth.Speak(VoiceConfig.Default, "Hola, qué tal el día", "a");
            Assert.IsNotNull(clip);
            Assert.Greater(clip.samples, 0, "el clip salió vacío");

            float peak = 0f;
            foreach (float s in SamplesOf(clip)) peak = Mathf.Max(peak, Mathf.Abs(s));
            Assert.Greater(peak, 0.01f, "el clip es silencio");

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void NuncaSatura()
        {
            // Pasarse de 1.0 recorta la onda y suena a distorsión. Con la voz más
            // extrema del juego tampoco puede pasar.
            var loud = new VoiceConfig
            {
                Pitch = VoicePitch.VeryHigh, Speed = 1.6f, Warble = 1f, Nasal = 1f,
            };
            var clip = VoiceSynth.Speak(loud, "AAAA EEEE IIII OOOO UUUU", "b");

            foreach (float s in SamplesOf(clip))
                Assert.That(Mathf.Abs(s), Is.LessThanOrEqualTo(1f), "la onda satura");

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void UnaFraseLargaDuraMasQueUnaCorta()
        {
            var shortClip = VoiceSynth.Speak(VoiceConfig.Default, "Sí", "c");
            var longClip = VoiceSynth.Speak(VoiceConfig.Default,
                "Pues mira, hoy me apetecía contarte una cosa larga", "c");

            Assert.Greater(longClip.samples, shortClip.samples);

            Object.DestroyImmediate(shortClip);
            Object.DestroyImmediate(longClip);
        }

        [Test]
        public void ElMismoHabitanteDiciendoLoMismoSuenaIgual()
        {
            var a = VoiceSynth.Speak(VoiceConfig.Default, "Buenos días", "marta");
            var b = VoiceSynth.Speak(VoiceConfig.Default, "Buenos días", "marta");

            var sa = SamplesOf(a);
            var sb = SamplesOf(b);
            Assert.AreEqual(sa.Length, sb.Length);
            for (int i = 0; i < sa.Length; i += 97)
                Assert.AreEqual(sa[i], sb[i], 0.0001f, $"difieren en la muestra {i}");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void DosHabitantesDistintosNoSuenanIgual()
        {
            var a = VoiceSynth.Speak(VoiceConfig.Default, "Buenos días", "marta");
            var b = VoiceSynth.Speak(VoiceConfig.Default, "Buenos días", "bruno");

            var sa = SamplesOf(a);
            var sb = SamplesOf(b);

            bool different = false;
            for (int i = 0; i < Mathf.Min(sa.Length, sb.Length); i++)
                if (Mathf.Abs(sa[i] - sb[i]) > 0.001f) { different = true; break; }

            Assert.IsTrue(different, "dos habitantes distintos suenan exactamente igual");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }

        [Test]
        public void UnTonoGraveTieneMenosCrucesPorCeroQueUnoAgudo()
        {
            // Los cruces por cero son una medida barata de la frecuencia: una voz
            // grave los tiene que dar menos veces que una aguda, o el tono de voz
            // no está haciendo nada.
            int low = ZeroCrossings(VoicePitch.VeryLow);
            int high = ZeroCrossings(VoicePitch.VeryHigh);
            Assert.Less(low, high, $"grave={low}, agudo={high}");
        }

        private static int ZeroCrossings(VoicePitch pitch)
        {
            var voice = VoiceConfig.Default;
            voice.Pitch = pitch;
            voice.Warble = 0f;

            var clip = VoiceSynth.Speak(voice, "aaaaaa", "tono");
            var samples = SamplesOf(clip);

            int crossings = 0;
            for (int i = 1; i < samples.Length; i++)
                if ((samples[i - 1] < 0f) != (samples[i] < 0f)) crossings++;

            Object.DestroyImmediate(clip);
            return crossings;
        }

        [Test]
        public void UnTextoVacioNoRompe()
        {
            var clip = VoiceSynth.Speak(VoiceConfig.Default, "", "x");
            Assert.IsNotNull(clip);
            Assert.Greater(clip.samples, 0);
            Object.DestroyImmediate(clip);
        }

        [Test]
        public void LasSilabasSeCuentanPorGruposDeVocales()
        {
            Assert.AreEqual(1, VoiceSynth.CountSyllables("sol"));
            Assert.AreEqual(2, VoiceSynth.CountSyllables("hola"));
            Assert.AreEqual(1, VoiceSynth.CountSyllables(""));
            Assert.Greater(VoiceSynth.CountSyllables("murciélago"), 2);
        }
    }

    public class SoundBankTests
    {
        [TearDown]
        public void TearDown() => SoundBank.Clear();

        [Test]
        public void TodosLosEfectosSeGeneran()
        {
            foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
            {
                var clip = SoundBank.Get(sfx);
                Assert.IsNotNull(clip, $"{sfx} no se generó");
                Assert.Greater(clip.samples, 0, $"{sfx} salió vacío");
            }
        }

        [Test]
        public void ElMismoEfectoSeReutiliza()
        {
            // Generar un clip nuevo por cada clic llenaría la memoria de clips iguales.
            var first = SoundBank.Get(Sfx.Click);
            var second = SoundBank.Get(Sfx.Click);
            Assert.AreSame(first, second);
        }

        [Test]
        public void NingunEfectoSatura()
        {
            foreach (Sfx sfx in System.Enum.GetValues(typeof(Sfx)))
            {
                var clip = SoundBank.Get(sfx);
                var samples = new float[clip.samples];
                clip.GetData(samples, 0);

                foreach (float s in samples)
                    Assert.That(Mathf.Abs(s), Is.LessThanOrEqualTo(1f), $"{sfx} satura");
            }
        }

        [Test]
        public void ElAmbienteDuraLoPedidoYEmpiezaEnSilencio()
        {
            var clip = SoundBank.BuildAmbience(seed: 42, seconds: 8f);
            Assert.AreEqual(SoundBank.SampleRate * 8, clip.samples, 2);

            var samples = new float[clip.samples];
            clip.GetData(samples, 0);

            // Rampa en los extremos: sin ella el bucle chasca al volver al principio.
            Assert.That(Mathf.Abs(samples[0]), Is.LessThan(0.01f), "empieza de golpe");
            Assert.That(Mathf.Abs(samples[^1]), Is.LessThan(0.01f), "acaba de golpe");

            Object.DestroyImmediate(clip);
        }

        [Test]
        public void DosSemillasDanAmbientesDistintos()
        {
            var a = SoundBank.BuildAmbience(1, 4f);
            var b = SoundBank.BuildAmbience(2, 4f);

            var sa = new float[a.samples];
            var sb = new float[b.samples];
            a.GetData(sa, 0);
            b.GetData(sb, 0);

            bool different = false;
            for (int i = 0; i < sa.Length; i += 13)
                if (Mathf.Abs(sa[i] - sb[i]) > 0.001f) { different = true; break; }

            Assert.IsTrue(different, "dos semillas dan el mismo ambiente");

            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
        }
    }
}
