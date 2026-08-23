using System.IO;
using Nimbo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que los botones del tema lleven sus estados puestos donde tocan.
    /// </summary>
    /// <remarks>
    /// El fallo que esto vigila ya costó una auditoría entera: UiTheme pintaba el
    /// fondo y el borde **inline**, y en UI Toolkit el inline gana a cualquier
    /// pseudoclase de la hoja de estilos. Resultado: los botones tenían :hover,
    /// :active y :focus escritos en el tema y ninguno se veía jamás. Aquí se
    /// comprueba la estructura —clase aplicada, nada pintado inline—; que el
    /// estado resuelto sale de verdad lo mide TemaEnLaIslaTests, en juego.
    ///
    /// Lo del resolved style no va en esta prueba de editor a propósito: sin bucle
    /// de jugador el panel no resuelve estilos con fiatura, y una prueba que pasa
    /// por coincidencia vale menos que ninguna.
    /// </remarks>
    public class TemaBotonesTests
    {
        private const string ThemePath = "Assets/_Project/Settings/NimboRuntimeTheme.tss";

        [Test]
        public void ElBotonPrincipalLlevaSuClaseYNoPintaInline()
        {
            var button = UiTheme.Action("Probar", null);

            Assert.That(button.ClassListContains(UiTheme.ClassAction), Is.True,
                        "sin la clase, el tema no puede vestir este botón");
            AssertFondoFueraDelInline(button);
        }

        [Test]
        public void ElBotonSecundarioLlevaSuClaseYNoPintaInline()
        {
            var button = UiTheme.Secondary("Probar", null);

            Assert.That(button.ClassListContains(UiTheme.ClassSecondary), Is.True,
                        "sin la clase, el tema no puede vestir este botón");
            AssertFondoFueraDelInline(button);
        }

        [Test]
        public void ElBotonApagadoLlevaSuClasePropiaYNingunaDeVivo()
        {
            var button = UiTheme.Disabled("Probar");

            Assert.That(button.enabledSelf, Is.False,
                        "el botón apagado tiene que seguir sin poder pulsarse");
            Assert.That(button.ClassListContains(UiTheme.ClassDisabled), Is.True,
                        "el apagado lleva clase propia para que ningún estado de vivo le toque");
            Assert.That(button.ClassListContains(UiTheme.ClassAction), Is.False,
                        "un botón apagado no puede llevar clase de vivo: prometería pulsarse");
            Assert.That(button.ClassListContains(UiTheme.ClassSecondary), Is.False,
                        "un botón apagado no puede llevar clase de vivo: prometería pulsarse");
            AssertFondoFueraDelInline(button);
        }

        /// <summary>
        /// El tema importa sin romperse y trae los tres estados por escrito.
        /// </summary>
        /// <remarks>
        /// La mitad fuerte es cargar el <c>ThemeStyleSheet</c>: si el USS no
        /// compila, Unity lo deja a nulo y la interfaz entera sale desnuda. La otra
        /// mitad mira el texto del fichero, que es más humilde pero caza el accidente
        /// de que alguien borre un bloque de estados creyendo que sobraba.
        /// </remarks>
        [Test]
        public void ElTemaImportaYTieneLosTresEstadosPorEscrito()
        {
            var theme = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>(ThemePath);
            Assert.That(theme, Is.Not.Null,
                        $"el tema de {ThemePath} no compila: toda la interfaz sale sin estilo");

            string uss = File.ReadAllText(ThemePath);

            foreach (var clase in new[] { UiTheme.ClassAction, UiTheme.ClassSecondary, UiTheme.ClassDisabled })
                Assert.That(uss, Does.Contain("." + clase),
                            $"falta la clase .{clase} en el tema");

            foreach (var estado in new[] { ":hover", ":active", ":focus" })
                Assert.That(uss, Does.Contain(estado),
                            $"el tema ha perdido el estado {estado}: los botones vuelven a estar muertos");
        }

        /// <summary>Que nadie vuelva a fijar fondo ni borde inline en las fábricas.</summary>
        /// <remarks>
        /// Se compara contra un botón virgen y no contra una palabra clave concreta:
        /// qué devuelve el inline cuando nadie lo ha tocado ha cambiado entre
        /// versiones de Unity, y lo que aquí se vigila es «¿hay alguien que haya
        /// escrito en esta propiedad?», no cómo lo escriba esta versión.
        /// </remarks>
        private static void AssertFondoFueraDelInline(VisualElement dressed)
        {
            var virgin = new Button();

            Assert.That(dressed.style.backgroundColor.keyword,
                        Is.EqualTo(virgin.style.backgroundColor.keyword),
                        "el fondo vuelve a estar inline: pisa el :hover, el :active y el :focus del tema");

            Assert.That(dressed.style.borderTopWidth.keyword,
                        Is.EqualTo(virgin.style.borderTopWidth.keyword),
                        "el borde vuelve a estar inline: pisa el anillo de foco del tema");
            Assert.That(dressed.style.borderBottomWidth.keyword,
                        Is.EqualTo(virgin.style.borderBottomWidth.keyword),
                        "el borde vuelve a estar inline: pisa el anillo de foco del tema");
            Assert.That(dressed.style.borderLeftWidth.keyword,
                        Is.EqualTo(virgin.style.borderLeftWidth.keyword),
                        "el borde vuelve a estar inline: pisa el anillo de foco del tema");
            Assert.That(dressed.style.borderRightWidth.keyword,
                        Is.EqualTo(virgin.style.borderRightWidth.keyword),
                        "el borde vuelve a estar inline: pisa el anillo de foco del tema");
        }
    }
}
