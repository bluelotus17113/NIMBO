using System.Collections.Generic;
using Nimbo.Core.Services.Contracts;
using Nimbo.Economy.Items;
using Nimbo.Economy.Shops;
using Nimbo.Farming;
using NUnit.Framework;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que el huerto dé dinero sin ser la única respuesta, y que las semillas se
    /// puedan comprar.
    /// </summary>
    /// <remarks>
    /// Estos números son de los que se mueven solos: se retoca un cultivo, se añade
    /// otro, y seis meses después resulta que hay uno que renta cinco veces más que el
    /// resto y el huerto tiene una sola respuesta correcta. Por eso se comprueban
    /// contra la banda del diseño (§7.4) y no contra valores concretos: lo que se está
    /// fijando es la forma de la economía, no las cifras.
    /// </remarks>
    public class EconomiaHuertoTests
    {
        private ItemCatalog _catalogo;
        private CropCatalog _cultivos;

        /// <summary>Lo que el diseño dice que debe rendir una casilla al día (§7.4).</summary>
        private const float MinPorDia = 18f;
        private const float MaxPorDia = 34f;

        /// <summary>La parcela de salida: 4×3 (§12.3).</summary>
        private const int CasillasDeSalida = 12;

        [SetUp]
        public void SetUp()
        {
            _catalogo = new ItemCatalog();
            _cultivos = new CropCatalog();
        }

        [Test]
        public void NingunCultivoRentaMuchoMasQueLosDemas()
        {
            foreach (var crop in _cultivos.Crops)
            {
                float porDia = PorDia(crop);

                Assert.That(porDia, Is.InRange(MinPorDia, MaxPorDia),
                    $"{crop.DisplayName} renta {porDia:F1} al día. Con un cultivo que " +
                    "rinde el doble que el resto, el huerto deja de ser una decisión y " +
                    "pasa a tener una sola respuesta correcta");
            }
        }

        [Test]
        public void LaParcelaDeSalidaCaeEnLaBandaDelDiseño()
        {
            float media = 0f;
            foreach (var crop in _cultivos.Crops) media += PorDia(crop);
            media /= _cultivos.Crops.Count;

            float alDia = media * CasillasDeSalida;

            Assert.That(alDia, Is.InRange(200f, 500f),
                $"la parcela de salida da {alDia:F0} al día, y el diseño pide entre 200 " +
                "y 500 netos por sesión (§7.4)");
        }

        [Test]
        public void ElPrimerCorteNuncaSaleAPerder()
        {
            foreach (var crop in _cultivos.Crops)
            {
                int semilla = Precio(crop.SeedId);
                int primero = Precio(crop.CropId) * crop.Yield;

                Assert.That(primero, Is.GreaterThan(semilla),
                    $"{crop.DisplayName} cuesta más de lo que da la primera cosecha. Aunque " +
                    "rebrote y a la larga compense, ese es justo el momento en que el " +
                    "jugador decide si merece la pena");
            }
        }

        [Test]
        public void LasSemillasSeVendenEnAlgunaTienda()
        {
            // No se vendían en ninguna: se empezaba con ocho de un solo cultivo y, al
            // acabarse, el huerto se quedaba en tierra labrada para siempre. Media capa
            // de granja dependía de un regalo de bienvenida.
            bool alguna = false;
            foreach (var tienda in ShopDefinition.All)
                for (int i = 0; i < tienda.Categories.Count; i++)
                    if (tienda.Categories[i] == ItemCategory.Seed) alguna = true;

            Assert.That(alguna, Is.True);
        }

        [Test]
        public void LaTiendaDeComidaTraeSemillasTodosLosDias()
        {
            // Con un sorteo plano, doce semillas contra cuarenta y cinco comidas salían
            // dos días de cada tres sin una sola semilla, y el huerto se quedaba parado
            // esperando surtido.
            var tienda = ShopDefinition.Get("NimboMart");
            Assert.That(tienda, Is.Not.Null);

            for (int dia = 1; dia <= 30; dia++)
            {
                var stock = ShopStock.Generate(tienda, dia, _catalogo);

                bool haySemilla = false;
                foreach (var id in stock)
                    if (_catalogo.GetItem(id)?.Category == ItemCategory.Seed) haySemilla = true;

                Assert.That(haySemilla, Is.True, $"el día {dia} no había ni una semilla");
            }
        }

        /// <remarks>
        /// Solo se exigen las familias que **tienen algo en el catálogo**. `Consumable`
        /// está declarada en dos tiendas y no hay ni un objeto de esa familia en ningún
        /// fichero: es una intención para más adelante, no un fallo. El generador de
        /// surtido se la salta sin gastar hueco, que es lo que había que comprobar.
        ///
        /// Y solo tantas como huecos tenga: el mercado flotante declara nueve familias
        /// con seis puestos, así que no puede traer una de cada.
        /// </remarks>
        [Test]
        public void CadaTiendaTraeAlgoDeCadaFamiliaQueDiceVender()
        {
            foreach (var tienda in ShopDefinition.All)
            {
                var stock = ShopStock.Generate(tienda, day: 7, catalog: _catalogo);
                var familias = new HashSet<ItemCategory>();

                foreach (var id in stock)
                {
                    var item = _catalogo.GetItem(id);
                    if (item != null) familias.Add(item.Category);
                }

                int exigidas = 0;
                for (int i = 0; i < tienda.Categories.Count && exigidas < tienda.StockSlots; i++)
                {
                    var familia = tienda.Categories[i];
                    if (!TieneAlgo(familia)) continue;

                    exigidas++;
                    Assert.That(familias, Does.Contain(familia),
                        $"{tienda.DisplayName} dice vender {familia} y hoy no tiene ni uno");
                }
            }
        }

        private bool TieneAlgo(ItemCategory category)
        {
            foreach (var _ in _catalogo.ItemsOfCategory(category)) return true;
            return false;
        }

        [Test]
        public void LasSemillasNoSeVendenEnElCajon()
        {
            // El cajón compra materiales y cosechas. Si comprara semillas, vender de más
            // un día dejaría el huerto sin poder sembrar y no habría forma de saber por
            // qué.
            foreach (var crop in _cultivos.Crops)
                Assert.That(_catalogo.GetItem(crop.SeedId).Category,
                    Is.EqualTo(ItemCategory.Seed),
                    $"{crop.SeedId} no está catalogada como semilla y el cajón se la comería");
        }

        // ── ayudas ──────────────────────────────────────────────────────────

        /// <summary>
        /// Lo que renta una casilla al día con ese cultivo, a la larga.
        /// </summary>
        /// <remarks>
        /// Los que rebrotan se miden por su ritmo **sostenido**: tras el primer corte no
        /// hay semilla que pagar y vuelven en la mitad de días. Midiéndolos por el
        /// primer ciclo salían tres veces por debajo de lo que rinden de verdad, que es
        /// justo el error que deja un cultivo dominando sin que nadie lo vea.
        /// </remarks>
        private float PorDia(in CropDefinition crop)
        {
            int semilla = Precio(crop.SeedId);
            int cosecha = Precio(crop.CropId) * crop.Yield;

            if (crop.Regrows)
            {
                int ciclo = System.Math.Max(1, crop.DaysToGrow / 2);
                return cosecha / (float)ciclo;
            }

            return (cosecha - semilla) / (float)crop.DaysToGrow;
        }

        private int Precio(string catalogId)
        {
            var item = _catalogo.GetItem(catalogId);
            Assert.That(item, Is.Not.Null, $"{catalogId} no está en el catálogo");
            return item.Price;
        }
    }
}
