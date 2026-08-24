using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Nimbo.UI.Menu;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que la lista de controles salga de donde dice salir.
    /// </summary>
    /// <remarks>
    /// El defecto de auditoría era que Tab, M y B no estuvieran escritas en ninguna
    /// parte del juego. La cura —una lista en ajustes— tiene su propia enfermedad
    /// conocida: la lista escrita a mano que miente el día que alguien mueve una
    /// tecla. Estas pruebas son la vacuna: cada constante declarada en
    /// <see cref="GameKeys"/> tiene su fila, no hay filas inventadas, y el panel
    /// pinta exactamente lo que la fuente dice, ni una palabra más.
    /// </remarks>
    public class TeclasTests
    {
        [Test]
        public void TodaTeclaDeclaradaEstaEnLaListaYNingunaMas()
        {
            var declaradas = typeof(GameKeys)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
                .Where(campo => campo.FieldType == typeof(KeyCode))
                .Select(campo => (KeyCode)campo.GetValue(null))
                .ToList();

            var enLista = GameKeys.Listed.Select(fila => fila.Key).ToList();

            Assert.That(enLista, Is.EquivalentTo(declaradas),
                "hay una tecla declarada fuera de la lista o una fila sin tecla: " +
                "o sobra una constante o falta una fila, y las dos cosas acaban mintiendo");
        }

        [Test]
        public void NingunaTeclaSeRepiteEnLaLista()
        {
            var teclas = GameKeys.Listed.Select(fila => fila.Key).ToList();

            Assert.That(teclas.Count, Is.EqualTo(teclas.Distinct().Count()),
                "dos acciones comparten tecla: al pulsarla pasarían las dos cosas");
        }

        [Test]
        public void CadaFilaTieneUnaAccionQueSeLea()
        {
            foreach (var fila in GameKeys.Listed)
                Assert.That(string.IsNullOrWhiteSpace(fila.Action), Is.False,
                    $"la tecla {fila.Key} sale en la lista sin decir para qué sirve");
        }

        [Test]
        public void ElPanelDeOpcionesPintaExactamenteLaLista()
        {
            var opciones = new OptionsPanel(() => { });

            var filas = opciones.Root.Query(className: "fila-tecla").ToList();

            Assert.That(filas.Count, Is.EqualTo(GameKeys.Listed.Count),
                "el panel pinta filas de más o de menos: no está leyendo GameKeys");

            for (int i = 0; i < GameKeys.Listed.Count; i++)
            {
                var textos = TextosDe(filas[i]);

                Assert.That(textos, Does.Contain(GameKeys.Listed[i].Action),
                    $"la fila {i} no dice «{GameKeys.Listed[i].Action}»: la lista del panel " +
                    "se ha escrito a mano y ya se ha despegado de la fuente");
                Assert.That(textos, Does.Contain(GameKeys.Name(GameKeys.Listed[i].Key)),
                    $"la fila {i} no enseña la tecla que declara GameKeys");
            }
        }

        private static System.Collections.Generic.List<string> TextosDe(VisualElement fila)
        {
            var textos = new System.Collections.Generic.List<string>();
            Recorrer(fila, textos);
            return textos;
        }

        private static void Recorrer(VisualElement elemento, System.Collections.Generic.List<string> textos)
        {
            // «text» no está en VisualElement sino en TextElement: es la misma
            // razón por la que CronicaEnLaIslaTests pregunta primero por Button.
            if (elemento is TextElement legible && !string.IsNullOrEmpty(legible.text))
                textos.Add(legible.text);

            for (int i = 0; i < elemento.childCount; i++)
                Recorrer(elemento[i], textos);
        }
    }
}
