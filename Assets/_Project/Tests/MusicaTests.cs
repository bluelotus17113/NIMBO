using Nimbo.Art.Audio;
using Nimbo.Core.Audio;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que la música cambie con lo que pasa.
    /// </summary>
    /// <remarks>
    /// La regla —qué debería sonar— es una función pura y se prueba entera. Lo otro que
    /// se puede comprobar sin oídos es que los tres fondos **no sean el mismo sonido**:
    /// si los tres humores dieran la misma onda, todo lo demás daría igual.
    /// </remarks>
    public class HumorDeLaMusicaTests
    {
        [Test]
        public void DeDiaYSinNadaMontadoSuenaLaCalma()
        {
            Assert.That(MusicMoods.For(9, eventRunning: false), Is.EqualTo(MusicMood.Calm));
            Assert.That(MusicMoods.For(15, eventRunning: false), Is.EqualTo(MusicMood.Calm));
        }

        [Test]
        public void DeNocheSuenaLaNoche()
        {
            Assert.That(MusicMoods.For(23, eventRunning: false), Is.EqualTo(MusicMood.Night));
            Assert.That(MusicMoods.For(3, eventRunning: false), Is.EqualTo(MusicMood.Night));
        }

        [Test]
        public void ElAmanecerYElAnochecerCaenDelLadoQueDicen()
        {
            // Las fronteras, que es donde estos fallos se esconden: a las 6 ya es de
            // día y a las 21 ya es de noche.
            Assert.That(MusicMoods.For(MusicMoods.DawnHour - 1, false), Is.EqualTo(MusicMood.Night));
            Assert.That(MusicMoods.For(MusicMoods.DawnHour, false), Is.EqualTo(MusicMood.Calm));
            Assert.That(MusicMoods.For(MusicMoods.DuskHour - 1, false), Is.EqualTo(MusicMood.Calm));
            Assert.That(MusicMoods.For(MusicMoods.DuskHour, false), Is.EqualTo(MusicMood.Night));
        }

        [Test]
        public void UnaFiestaDeNocheSigueSonandoAFiesta()
        {
            // El caso que importa, porque es el normal: los conciertos son a las cinco
            // de la tarde y duran hasta bien entrada la noche. Si al dar las nueve el
            // fondo se pusiera a sonar a noche en mitad del concierto, parecería que
            // algo se ha roto.
            Assert.That(MusicMoods.For(23, eventRunning: true), Is.EqualTo(MusicMood.Party));
            Assert.That(MusicMoods.For(4, eventRunning: true), Is.EqualTo(MusicMood.Party));
        }

        [Test]
        public void LosTresHumoresSuenanDistinto()
        {
            var calma = SoundBank.BuildAmbience(7, MusicMood.Calm, 6f);
            var noche = SoundBank.BuildAmbience(7, MusicMood.Night, 6f);
            var fiesta = SoundBank.BuildAmbience(7, MusicMood.Party, 6f);

            Assert.IsTrue(Difieren(calma, noche), "la noche suena igual que la calma");
            Assert.IsTrue(Difieren(calma, fiesta), "la fiesta suena igual que la calma");
            Assert.IsTrue(Difieren(noche, fiesta), "la fiesta suena igual que la noche");

            Object.DestroyImmediate(calma);
            Object.DestroyImmediate(noche);
            Object.DestroyImmediate(fiesta);
        }

        [Test]
        public void LaNocheEsMasGraveYLaFiestaMasMovida()
        {
            // Los cruces por cero miden el agudo de una onda a lo bruto, que es todo lo
            // que hace falta aquí: la noche va una octava abajo, así que tiene que
            // cruzar menos veces que la calma, y la fiesta lleva el pulso y el agudo
            // casi igualado, así que tiene que cruzar más.
            int calma = Cruces(MusicMood.Calm);
            int noche = Cruces(MusicMood.Night);
            int fiesta = Cruces(MusicMood.Party);

            Assert.Less(noche, calma, $"noche={noche}, calma={calma}");
            Assert.Greater(fiesta, calma, $"fiesta={fiesta}, calma={calma}");
        }

        [Test]
        public void NingunHumorSatura()
        {
            foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
            {
                var clip = SoundBank.BuildAmbience(7, mood, 6f);

                foreach (float s in Muestras(clip))
                    Assert.That(Mathf.Abs(s), Is.LessThanOrEqualTo(1f), $"{mood} satura");

                Object.DestroyImmediate(clip);
            }
        }

        [Test]
        public void LosTresEmpiezanYAcabanEnSilencio()
        {
            // Los tres se reproducen en bucle, así que los tres necesitan la rampa en
            // los extremos. Solo la tenía el de calma cuando era el único que había.
            foreach (MusicMood mood in System.Enum.GetValues(typeof(MusicMood)))
            {
                var clip = SoundBank.BuildAmbience(7, mood, 6f);
                var muestras = Muestras(clip);

                Assert.That(Mathf.Abs(muestras[0]), Is.LessThan(0.01f), $"{mood} empieza de golpe");
                Assert.That(Mathf.Abs(muestras[^1]), Is.LessThan(0.01f), $"{mood} acaba de golpe");

                Object.DestroyImmediate(clip);
            }
        }

        private static float[] Muestras(AudioClip clip)
        {
            var muestras = new float[clip.samples * clip.channels];
            clip.GetData(muestras, 0);
            return muestras;
        }

        private static bool Difieren(AudioClip a, AudioClip b)
        {
            var sa = Muestras(a);
            var sb = Muestras(b);

            for (int i = 0; i < Mathf.Min(sa.Length, sb.Length); i += 11)
                if (Mathf.Abs(sa[i] - sb[i]) > 0.001f) return true;

            return false;
        }

        private static int Cruces(MusicMood mood)
        {
            var clip = SoundBank.BuildAmbience(7, mood, 6f);
            var muestras = Muestras(clip);

            int cruces = 0;
            for (int i = 1; i < muestras.Length; i++)
                if ((muestras[i - 1] < 0f) != (muestras[i] < 0f)) cruces++;

            Object.DestroyImmediate(clip);
            return cruces;
        }
    }
}
