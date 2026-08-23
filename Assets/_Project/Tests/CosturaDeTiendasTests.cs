using System.Text.RegularExpressions;
using Nimbo.Economy.Items;
using Nimbo.Economy.Shops;
using Nimbo.Island.Zones;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Nimbo.Tests
{
    /// <summary>
    /// La costura entre el mundo y las tiendas: todo identificador de tienda que una
    /// capa declara tiene que existir y tener surtido en la otra.
    /// </summary>
    /// <remarks>
    /// Esta prueba no existía y por eso nadie pudo comprar nada durante semanas: la
    /// interfaz abría «tienda_comida», el catálogo solo conocía «NimboMart», y
    /// <c>StockOf</c> contestaba vacío sin error ni aviso — el modo de fallo exacto
    /// que <c>EconomyTests.StockOf_UnknownShop_ReturnsEmpty</c> tenía escrito como
    /// comportamiento correcto.
    ///
    /// La regla que lo caza: los identificadores se toman **de donde están
    /// declarados** —el plano de la isla— y jamás copiados a mano en la prueba.
    /// Copiarlos sería repetir el fallo con otros medios: dos listas paralelas que
    /// pueden volver a divergir sin que nadie se entere.
    /// </remarks>
    public class CosturaDeTiendasTests
    {
        private ItemCatalog _catalogo;

        [SetUp]
        public void SetUp() => _catalogo = new ItemCatalog();   // el real, desde Resources

        [Test]
        public void TodaTiendaQueAnunciaUnaZonaExisteEnElCatalogo()
        {
            int revisadas = 0;

            foreach (var zona in IslandLayout.FirstIsland())
            {
                if (string.IsNullOrEmpty(zona.ShopId)) continue;
                revisadas++;

                Assert.That(ShopDefinition.Get(zona.ShopId), Is.Not.Null,
                    $"la zona {zona.ZoneId} abre la tienda «{zona.ShopId}» y el catálogo " +
                    "no conoce ese id: quien entre ahí encontrará la tienda vacía todos " +
                    "los días de todas las partidas");
            }

            Assert.That(revisadas, Is.GreaterThanOrEqualTo(3),
                "el plano ha perdido zonas con tienda: esta prueba se queda sin material");
        }

        [Test]
        public void CadaZonaConTiendaTieneSurtidoTodosLosDias()
        {
            // Que el id resuelva no basta: la rotación diaria podría amanecer vacía.
            // Treinta días porque un mes de partidas no puede contener ni uno sin nada
            // que comprar.
            foreach (var zona in IslandLayout.FirstIsland())
            {
                if (string.IsNullOrEmpty(zona.ShopId)) continue;
                var tienda = ShopDefinition.Get(zona.ShopId);

                for (int dia = 1; dia <= 30; dia++)
                {
                    var stock = ShopStock.Generate(tienda, dia, _catalogo);
                    Assert.That(stock, Is.Not.Empty,
                        $"{zona.ShopId} amanece sin nada que vender el día {dia}");
                }
            }
        }

        [Test]
        public void TodaTiendaDelCatalogoSacaSurtidoConElCatalogoReal()
        {
            // Las tiendas sin zona todavía —Antigüedades, el mercado flotante— no
            // llegan a esta costura hoy, pero si mañana alguien las enchufa, que no
            // sea a un surtido imposible.
            foreach (var tienda in ShopDefinition.All)
                Assert.That(ShopStock.Generate(tienda, day: 3, catalog: _catalogo),
                    Is.Not.Empty,
                    $"{tienda.ShopId} no puede sacar ni un objeto con este catálogo");
        }

        [Test]
        public void UnIdDeTiendaDesconocidoGritaYDevuelveVacio()
        {
            // El contrato cambió: antes un id desconocido devolvía vacío sin decir
            // nada, y ese silencio escondió la tienda más rota del juego. Ahora grita
            // igual que ItemCatalog.GetItem con un objeto desconocido, y devuelve
            // vacío solo para que el juego siga en pie mientras el fallo queda visto.
            LogAssert.Expect(LogType.Error, new Regex("tienda_inventada"));
            Assert.That(ShopDefinition.Get("tienda_inventada"), Is.Null);
        }
    }
}
