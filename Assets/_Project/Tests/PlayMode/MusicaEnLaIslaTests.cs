using System.Collections;
using Nimbo.Art.Audio;
using Nimbo.Core.Audio;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Time;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la música reaccione **en la isla de verdad**.
    /// </summary>
    /// <remarks>
    /// La regla de qué suena se prueba aparte y es una función pura. Lo que se prueba
    /// aquí es la costura: que el director de audio esté en la escena, que empiece en
    /// el humor que dice el reloj y que se entere de que ha empezado una fiesta. Es
    /// exactamente el sitio donde este proyecto se rompe siempre.
    /// </remarks>
    public class MusicaEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        private static AudioDirector Director()
        {
            var director = Object.FindFirstObjectByType<AudioDirector>();
            Assert.IsNotNull(director, "no hay director de audio en la isla");
            return director;
        }

        [UnityTest]
        public IEnumerator ArrancaEnElHumorQueDiceElReloj()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<GameClock>(out var reloj));

            // Sin fiesta al arrancar, así que manda la hora. Cargar una partida a las
            // once de la noche y oír el fondo de mediodía es la misma incoherencia que
            // había antes, solo que en el primer minuto.
            Assert.That(Director().CurrentMood,
                Is.EqualTo(MusicMoods.For(reloj.Hour, eventRunning: false)),
                $"son las {reloj.Hour} y no suena lo que toca");
        }

        [UnityTest]
        public IEnumerator AlEmpezarUnaFiestaLaMusicaSeEntera()
        {
            yield return CargarYEmpezar();

            var director = Director();
            EventBus.Publish(new VillageEventStarted("concert", "Concierto", 18));
            yield return null;

            Assert.That(director.CurrentMood, Is.EqualTo(MusicMood.Party),
                "empezó la fiesta y el fondo se quedó como estaba");
        }

        [UnityTest]
        public IEnumerator YCuandoAcabaVuelveALoQueTocaPorLaHora()
        {
            yield return CargarYEmpezar();

            var director = Director();
            Assert.IsTrue(ServiceRegistry.TryGet<GameClock>(out var reloj));

            EventBus.Publish(new VillageEventStarted("concert", "Concierto", 18));
            yield return null;
            EventBus.Publish(new VillageEventEnded("concert"));
            yield return null;

            Assert.That(director.CurrentMood,
                Is.EqualTo(MusicMoods.For(reloj.Hour, eventRunning: false)),
                "la fiesta acabó y el fondo se quedó de fiesta");
        }

        [UnityTest]
        public IEnumerator ElCruceNoDejaLaIslaEnSilencio()
        {
            // Lo que hace falta que no pase. Con una sola fuente de música, cambiar de
            // clip es un silencio de un frame, y un silencio en el fondo se oye como
            // un fallo aunque dure nada.
            yield return CargarYEmpezar();

            var director = Director();
            EventBus.Publish(new VillageEventStarted("concert", "Concierto", 18));

            for (int frame = 0; frame < 30; frame++)
            {
                Assert.IsTrue(SuenaAlgo(director), $"silencio en el frame {frame} del cruce");
                yield return null;
            }
        }

        private static bool SuenaAlgo(AudioDirector director)
        {
            foreach (var source in director.GetComponents<AudioSource>())
                if (source.isPlaying && source.loop && source.clip != null) return true;

            return false;
        }
    }
}
