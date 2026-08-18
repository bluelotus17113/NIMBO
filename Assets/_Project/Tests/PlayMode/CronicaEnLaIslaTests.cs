using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Social;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la crónica exista **en el juego**, no solo en su clase.
    /// </summary>
    /// <remarks>
    /// Es la prueba que este proyecto lleva necesitando. La lista de sistemas escritos,
    /// probados y apagados va por cuatro: las ampliaciones de casa, las bodas, los tres
    /// minijuegos y el módulo de eventos entero. Todos tenían tests en verde y ninguno
    /// existía para el jugador, porque a lo que le faltaba no era lógica: era que alguien
    /// lo registrara o lo pusiera en pantalla.
    ///
    /// Así que aquí se carga la isla de verdad y se comprueban las dos cosas que hacen
    /// que algo exista: que el servicio esté registrado y que haya por dónde abrirlo.
    /// </remarks>
    public class CronicaEnLaIslaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena: sin
            // barrerlo, esta prueba se encontraría el de la anterior ya montado. Es el
            // mismo cuidado que tiene BootTests.
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

        [UnityTest]
        public IEnumerator LaCronicaEstaRegistradaAlEmpezar()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.IsRegistered<IChronicleService>(),
                "nadie registró la crónica, así que en el juego no existe");
        }

        [UnityTest]
        public IEnumerator HayUnBotonParaAbrirLaCronica()
        {
            yield return CargarYEmpezar();

            var textos = TextosDeBotones();

            Assert.That(textos, Is.Not.Empty, "no encuentro la barra de acciones");
            Assert.That(textos, Does.Contain("Crónica"),
                "el servicio puede estar perfecto: si no hay botón, el jugador no lo ve. " +
                $"Botones encontrados: {string.Join(", ", textos)}");
        }

        [UnityTest]
        public IEnumerator LoQuePasaEnLaAldeaLlegaALaCronica()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IChronicleService>(out var cronica));
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(2),
                "hacen falta dos vecinos para que pase algo entre ellos");

            int antes = cronica.Entries.Count;

            // Justo lo que publica el evaluador de romance cuando dos se prometen.
            var uno = censo.All[0].Id;
            var otro = censo.All[1].Id;
            EventBus.Publish(new RomanceStageChanged(uno, otro, RomanceStage.Engaged));
            yield return null;

            Assert.That(cronica.Entries.Count, Is.EqualTo(antes + 1),
                "el aviso se publicó y la crónica no se enteró: no hay nadie escuchando");
        }

        [UnityTest]
        public IEnumerator UnaRivalidadLlegaALaCronica()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IChronicleService>(out var cronica));
            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThanOrEqualTo(2));

            int antes = cronica.Entries.Count;

            // Justo lo que publica el triángulo al abrirse (§13.2).
            EventBus.Publish(new ConflictStageChanged(
                censo.All[0].Id, censo.All[1].Id, ConflictStage.Rivalry));
            yield return null;

            Assert.That(cronica.Entries.Count, Is.EqualTo(antes + 1),
                "la rivalidad es la historia más jugosa que da la simulación y el " +
                "jugador no tiene por dónde enterarse");
        }

        /// <summary>
        /// El texto de todos los botones que hay en pantalla.
        /// </summary>
        /// <remarks>
        /// Se recorre el árbol de la interfaz en vez de buscar el panel por código: lo
        /// que se quiere saber es si el jugador tiene dónde pulsar, y eso solo lo
        /// contesta lo que está montado de verdad en el documento.
        /// </remarks>
        private static List<string> TextosDeBotones()
        {
            var textos = new List<string>();

            foreach (var document in Object.FindObjectsByType<UIDocument>(
                         FindObjectsSortMode.None))
            {
                if (document.rootVisualElement == null) continue;
                Recorrer(document.rootVisualElement, textos);
            }
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Button button && !string.IsNullOrEmpty(button.text))
                textos.Add(button.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
