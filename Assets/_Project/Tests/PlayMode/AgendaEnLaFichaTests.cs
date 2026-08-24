using System.Collections;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.UI.Islander;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Nimbo.PlayTests
{
    /// <summary>
    /// Que la agenda del vecino exista **en el juego**: abierta la ficha, el día de ese
    /// vecino está, dice algo, y es SUYO — cambia cuando cambia su personalidad.
    /// </summary>
    /// <remarks>
    /// El GDD §2.2 promete «ver la agenda del día» y hasta esta prueba no había forma
    /// de mirarla. La prueba clave no es que la tarjeta exista: es que el texto que
    /// enseña sale de los datos del vecino y no de una frase fija. Para eso se siembra
    /// la personalidad a mano —mismo precedente que <see cref="FichaEnLaIslaTests"/>
    /// usa para sembrar relaciones— porque la isla nueva solo trae tres vecinos
    /// (GameBootstrap.cs:55) y confiar en que dos traigan caracteres distintos era una
    /// prueba que fallaría una de cada siete veces.
    /// </remarks>
    public class AgendaEnLaFichaTests
    {
        [SetUp]
        public void SetUp()
        {
            // El arranque es DontDestroyOnLoad y sobrevive a cargar otra escena.
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
        public IEnumerator LaFichaAbiertaEnseniaElDiaDelVecino()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));
            Assert.That(censo.Count, Is.GreaterThan(0));

            var vecino = censo.All[0];

            var ficha = new IslanderPanel();
            ficha.Show(vecino.Id);

            var textos = Textos(ficha.Root);

            Assert.That(textos, Does.Contain("Su día"),
                "el GDD promete ver la agenda del día: sin tarjeta, la promesa no existe");

            // Lo que dice la tarjeta tiene que ser LO DE ESTE VECINO: la frase de
            // paseo proyectada desde su personalidad de verdad, no un texto fijo.
            // Has.Some.Contains y no Does.Contain: ahí se busca dentro de cada Label,
            // no que una etiqueta entera sea exactamente esa frase.
            string esperado = AgendaProjection.Haunt(vecino.Personality);
            Assert.That(textos, Has.Some.Contains(esperado),
                "la agenda enseña un día que no es el del vecino según su carácter");

            Assert.That(textos, Has.Some.Contains("Duerme"),
                "sin la franja de sueño no hay ritmo, y sin ritmo no hay agenda");

            // En la isla hay reloj registrado y los dos tramos cubren las 24 horas,
            // así que siempre hay exactamente uno marcado como el que está pasando.
            Assert.That(textos, Has.Some.Contains("ahora"),
                "una agenda sin «ahora» no dice dónde está el vecino dentro del día");
        }

        [UnityTest]
        public IEnumerator CambiarElCaracterCambiaLaAgendaEnElMismoRefresco()
        {
            yield return CargarYEmpezar();

            Assert.IsTrue(ServiceRegistry.TryGet<IIslanderRegistry>(out var censo));

            var vecino = censo.All[0];
            var ficha = new IslanderPanel();

            vecino.Personality = new PersonalityProfile(-0.75f, -0.75f, 0.75f, -0.75f);
            ficha.Show(vecino.Id);
            var textosSociable = Textos(ficha.Root);

            vecino.Personality = new PersonalityProfile(-0.75f, -0.75f, -0.75f, -0.75f);
            ficha.Refresh();
            var textosCasero = Textos(ficha.Root);

            string plaza = AgendaProjection.Haunt(
                new PersonalityProfile(-0.75f, -0.75f, 0.75f, -0.75f));
            string casa = AgendaProjection.Haunt(
                new PersonalityProfile(-0.75f, -0.75f, -0.75f, -0.75f));

            Assert.That(textosSociable, Has.Some.Contains(plaza),
                "el sociable tiene que acabar en la plaza, que es donde se junta la gente");
            Assert.That(textosCasero, Has.Some.Contains(casa),
                "cambiado el carácter, el mismo vecino tiene que enseñar otro día");
            Assert.That(textosCasero, Is.Not.EqualTo(textosSociable),
                "dos personalidades con la misma agenda no son dos personajes: es un " +
                "texto fijo disfrazado");
        }

        private static List<string> Textos(VisualElement root)
        {
            var textos = new List<string>();
            Recorrer(root, textos);
            return textos;
        }

        private static void Recorrer(VisualElement element, List<string> textos)
        {
            if (element is Label label && !string.IsNullOrEmpty(label.text))
                textos.Add(label.text);

            for (int i = 0; i < element.childCount; i++)
                Recorrer(element[i], textos);
        }
    }
}
