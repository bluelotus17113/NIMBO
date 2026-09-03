using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Player;
using Nimbo.UI.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que las cinco vías existan **en el juego**: registradas, escuchando y con
    /// pantalla donde mirarlas.
    /// </summary>
    /// <remarks>
    /// La progresión es el sistema con más papeletas de este proyecto para quedarse
    /// apagada, porque no la llama nadie: se suscribe a lo que ya pasa y cuenta. Un
    /// servicio así se puede escribir entero, probar entero y no estar construido, y
    /// nada falla — simplemente el jugador no sube nunca de nivel.
    /// </remarks>
    public class ViasEnLaIslaTests
    {
        private IPlayerProgression _previa;
        private bool _habiaPrevia;

        [SetUp]
        public void SetUp()
        {
            _habiaPrevia = ServiceRegistry.TryGet(out _previa);

            foreach (var stale in Object.FindObjectsByType<Game.Bootstrap.GameBootstrap>(
                         FindObjectsSortMode.None))
                Object.DestroyImmediate(stale.gameObject);
        }

        [TearDown]
        public void TearDown()
        {
            if (!_habiaPrevia) return;

            ServiceRegistry.Unregister<IPlayerProgression>();
            ServiceRegistry.Register(_previa);
        }

        private static IEnumerator CargarYEmpezar()
        {
            yield return SceneManager.LoadSceneAsync("Isla", LoadSceneMode.Single);
            yield return null;

            EventBus.Publish(new ProtagonistCreated(
                "Nimbo", Data.Islanders.AppearanceData.Default));

            for (int i = 0; i < 4; i++) yield return null;
        }

        [UnityTest]
        public IEnumerator LasViasEstanRegistradas()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IPlayerProgression>(),
                "nadie construyó la progresión: el protagonista no sube de nivel nunca");
        }

        [UnityTest]
        public IEnumerator HayUnBotonParaVerlas()
        {
            yield return CargarYEmpezar();

            var textos = TextosDeBotones();

            Assert.That(textos, Is.Not.Empty, "no encuentro la barra de acciones");
            Assert.That(textos, Does.Contain("Vías"),
                $"suben y no hay dónde verlas. Botones: {string.Join(", ", textos)}");
        }

        [UnityTest]
        public IEnumerator LoQueHaceElJugadorEnLaIslaLeSube()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IPlayerProgression>(out var vias));
            float antes = vias.XpOf(SkillKind.Gathering);

            // Justo lo que publica la recolección al agotar un nodo.
            EventBus.Publish(new NodeGathered("nodo", "mat_madera", 3));
            yield return null;

            Assert.That(vias.XpOf(SkillKind.Gathering), Is.GreaterThan(antes),
                "el aviso se publicó y la progresión no estaba escuchando");
        }

        [UnityTest]
        public IEnumerator LaProgresionSeGuardaConLaPartida()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IPlayerProgression>(out var vias));
            vias.Grant(SkillKind.Village, Data.Player.SkillSet.XpForLevel(1));

            Assert.That(vias.LevelOf(SkillKind.Village), Is.EqualTo(2));

            // Lo que importa es que el nivel viva en el estado del jugador, que es lo
            // que se serializa. Si el servicio lo guardara aparte, subir de nivel se
            // perdería al cerrar y nadie lo notaría hasta la segunda sesión.
            Assert.IsTrue(ServiceRegistry.TryGet<Nimbo.Player.PlayerService>(out var jugador));
            Assert.That(jugador.State.Skills.LevelOf(SkillKind.Village), Is.EqualTo(2));
        }

        // ── la pantalla ──────────────────────────────────────────────────────

        [Test]
        public void LaPantallaEnseñaLasCincoViasYLoQueVieneDespues()
        {
            var estado = new PlayerState();
            using var vias = new Nimbo.Player.PlayerProgressionService(estado);

            ServiceRegistry.Unregister<IPlayerProgression>();
            ServiceRegistry.Register<IPlayerProgression>(vias);

            var panel = new SkillsPanel();
            panel.Show();

            var textos = new List<string>();
            Recorrer(panel.Root, textos);

            Assert.That(textos, Does.Contain("Cultivo"));
            Assert.That(textos, Does.Contain("Recolección"));
            Assert.That(textos, Does.Contain("Oficio"));
            Assert.That(textos, Does.Contain("Convivencia"));
            Assert.That(textos, Does.Contain("Aldea"));

            bool diceQueViene = false;
            foreach (var texto in textos)
                if (texto.StartsWith("En el ")) diceQueViene = true;

            Assert.That(diceQueViene, Is.True,
                "una barra sola dice cuánto falta pero no para qué, y entonces subir de " +
                "nivel es leer un número más grande");
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }

        private static List<string> TextosDeBotones() => Pulsables.Textos();

        private static void RecorrerBotones(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);

            for (int i = 0; i < element.childCount; i++)
                RecorrerBotones(element[i], textos);
        }
    }
}
