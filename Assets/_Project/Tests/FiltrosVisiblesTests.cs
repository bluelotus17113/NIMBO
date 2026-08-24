using Nimbo.Core.Services.Contracts;
using Nimbo.UI;
using Nimbo.UI.Achievements;
using Nimbo.UI.Decor;
using NUnit.Framework;
using UnityEngine.UIElements;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que un filtro activo se distinga de uno inactivo sin pulsar nada.
    /// </summary>
    /// <remarks>
    /// El defecto que esto vigila: dos paneles con filtros que no decían cuál estaba
    /// puesto, cuando el mismo repo ya lo contaba dos veces bien hecho —las pestañas
    /// del crafteo y las zonas del propio panel de adornos—. Aquí se mira el estilo
    /// **inline** y no el resuelto a propósito: sin bucle de jugador el panel no
    /// resuelve estilos con fiatura, y el marcado del activo se escribe inline igual
    /// que en CraftPanel.BuildStations.
    /// </remarks>
    public class FiltrosVisiblesTests
    {
        // ── logros ───────────────────────────────────────────────────────────

        [Test]
        public void LosLogrosMarcanTodoComoFiltroInicial()
        {
            var panel = new AchievementsPanel();
            var todo = Chip(panel.Root, "filtros-logros", "Todo");

            Assert.That(todo, Is.Not.Null, "no encuentro el filtro «Todo»");
            Assert.That(EstaActivo(todo), Is.True,
                "al abrir la lista se ve todo el catálogo: ese estado tiene que leerse");
        }

        [Test]
        public void ElegirUnFiltroDeLogrosLoMarcaYDejaElRestoApagados()
        {
            var panel = new AchievementsPanel();

            panel.SetFilter(AchievementKind.Social);

            var vecinos = Chip(panel.Root, "filtros-logros", "Vecinos");
            var todo = Chip(panel.Root, "filtros-logros", "Todo");

            Assert.That(vecinos, Is.Not.Null);
            Assert.That(EstaActivo(vecinos), Is.True,
                "el filtro recién elegido es el que hay que ver encendido");
            Assert.That(EstaActivo(todo), Is.False,
                "«Todo» sigue con su aspecto de reposo: si también parece activo, ninguno lo está");
        }

        [Test]
        public void VolverATodoEnLogrosDevuelveLaMarcaASuSitio()
        {
            var panel = new AchievementsPanel();
            panel.SetFilter(AchievementKind.Money);

            panel.SetFilter(null);

            Assert.That(EstaActivo(Chip(panel.Root, "filtros-logros", "Todo")), Is.True);
            Assert.That(EstaActivo(Chip(panel.Root, "filtros-logros", "Nimbos")), Is.False);
        }

        // ── adornos ──────────────────────────────────────────────────────────

        [Test]
        public void LosAdornosMarcanTodoComoFiltroInicial()
        {
            var panel = new DecorPanel();
            var todo = Chip(panel.Root, "filtros-adornos", "Todo");

            Assert.That(todo, Is.Not.Null, "no encuentro el filtro «Todo»");
            Assert.That(EstaActivo(todo), Is.True,
                "el catálogo arranca completo: la fila tiene que decirlo");
        }

        [Test]
        public void ElegirUnFiltroDeAdornosLoMarcaYDejaElRestoApagados()
        {
            var panel = new DecorPanel();

            panel.SetFilter(DecorKind.Plant);

            Assert.That(EstaActivo(Chip(panel.Root, "filtros-adornos", "Plantas")), Is.True,
                "el filtro recién elegido es el que hay que ver encendido");
            Assert.That(EstaActivo(Chip(panel.Root, "filtros-adornos", "Todo")), Is.False);
        }

        // ── ayudas ───────────────────────────────────────────────────────────

        /// <summary>El chip de una fila de filtros por su texto.</summary>
        private static Button Chip(VisualElement root, string rowName, string text)
        {
            var row = root.Q<VisualElement>(rowName);
            if (row == null) return null;

            for (int i = 0; i < row.childCount; i++)
                if (row[i] is Button button && button.text == text)
                    return button;

            return null;
        }

        /// <summary>
        /// Activo significa: fondo melocotón escrito en el inline.
        /// </summary>
        /// <remarks>
        /// Se compara contra un botón virgen y no contra una palabra clave concreta:
        /// qué devuelve el inline cuando nadie ha tocado la propiedad cambia entre
        /// versiones de Unity, y lo que aquí importa es «¿lo escribió alguien?».
        /// Es el mismo cuidado de TemaBotonesTests.AssertFondoFueraDelInline, al revés.
        /// </remarks>
        private static bool EstaActivo(Button chip)
        {
            var virgin = new Button();
            bool pintado = chip.style.backgroundColor.keyword !=
                           virgin.style.backgroundColor.keyword;

            return pintado && chip.style.backgroundColor.value == UiTheme.Peach;
        }
    }
}
