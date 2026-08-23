using System.Collections.Generic;
using Nimbo.Art.Chibi;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Islanders;
using Nimbo.Economy.Items;
using NUnit.Framework;
using UnityEngine;

namespace Nimbo.Tests
{
    /// <summary>
    /// Que una prenda del catálogo se convierta en algo que se ve: color en la ropa,
    /// piernas que dejan de ser piel y la pieza que distingue cada familia.
    /// </summary>
    /// <remarks>
    /// <c>EquippedOutfit</c> se llevaba escribiendo años sin que ninguna vista lo
    /// leyera. Estas pruebas miran el otro lado del enchufe: no que el dato se
    /// guarde, sino qué malla sale cuando llega.
    /// </remarks>
    public class RopaLookTests
    {
        private static ItemDefinition Prenda(string catalogId, string slot,
                                             params string[] palette)
        {
            return new ItemDefinition(new CatalogItemJson
            {
                catalogId = catalogId,
                displayName = catalogId,
                slot = slot,
                style = "casual",
                palette = new List<string>(palette),
            }, ItemCategory.Clothing);
        }

        // ── el mapeador: catálogo → look ─────────────────────────────────────

        [Test]
        public void ElVestidoSePintaConSuPaletaYLlevaFalda()
        {
            var look = OutfitLook.From(Prenda("cloth_vestido_de_algodon_celeste", "outfit",
                                              "#B8D4E8", "#FFFFFF", "#7BA3C9"));

            Assert.IsTrue(look.HasGarment, "un vestido tiene que vestir");
            ColorUtility.TryParseHtmlString("#B8D4E8", out var prenda);
            Assert.That(look.Garment, Is.EqualTo(prenda),
                        "la ropa tiene que salir del color autorizado del catálogo");
            Assert.That(look.Piece, Is.EqualTo(GarmentPiece.Skirt));
            ColorUtility.TryParseHtmlString("#FFFFFF", out var secundario);
            Assert.That(look.PieceColor, Is.EqualTo(secundario),
                "la pieza lleva el segundo color: si llevara el mismo, la falda " +
                "no se distinguiría del torso");
        }

        [Test]
        public void CadaFamiliaDePrendaTieneSuPieza()
        {
            Assert.That(OutfitLook.From(Prenda("cloth_pantalones_cargo_marrones", "outfit",
                                               "#8B7355")).Piece, Is.EqualTo(GarmentPiece.None),
                        "un pantalón es solo color, sin pieza");
            Assert.That(OutfitLook.From(Prenda("cloth_sudadera_con_capucha_gris", "outfit",
                                               "#888888")).Piece, Is.EqualTo(GarmentPiece.Hood));
            Assert.That(OutfitLook.From(Prenda("cloth_capa_de_explorador_nimbo", "outfit",
                                               "#445533")).Piece, Is.EqualTo(GarmentPiece.Cape));
            Assert.That(OutfitLook.From(Prenda("cloth_gorra_nimbo_clasica", "hat",
                                               "#3366AA")).Piece, Is.EqualTo(GarmentPiece.HatBrimmed));
            Assert.That(OutfitLook.From(Prenda("cloth_gorro_de_lana_con_pompom", "hat",
                                               "#CC4444")).Piece, Is.EqualTo(GarmentPiece.HatBeanie));
        }

        [Test]
        public void UnAccesorioNoVisteElCuerpo()
        {
            // A la altura de los ojos, una pulsera son cuatro píxeles. Pintarle el
            // torso color silbato sería peor que no pintar nada.
            var look = OutfitLook.From(Prenda("cloth_pulsera_de_la_amistad", "accessory",
                                              "#FF88AA"));

            Assert.IsFalse(look.HasGarment, "un accesorio no puede quedarse con el cuerpo");
            Assert.That(look.Piece, Is.EqualTo(GarmentPiece.None));
        }

        [Test]
        public void UnSombreroNoTineLaRopaDelCuerpo()
        {
            // El defecto que esto fija: con HasGarment=true, IslanderView pintaba la
            // malla de ropa entera del color de la gorra y quien se ponía una gorra
            // azul aparecía con el torso y la cadera azules. El sombrero lleva su
            // color en la pieza; la ropa del cuerpo se queda como estaba.
            var gorra = OutfitLook.From(Prenda("cloth_gorra_nimbo_clasica", "hat",
                                               "#3366AA"));

            Assert.IsFalse(gorra.HasGarment,
                           "el sombrero se quedó con el cuerpo y lo tiñó entero");
            Assert.That(gorra.Piece, Is.EqualTo(GarmentPiece.HatBrimmed),
                        "al quitarle el cuerpo al sombrero no le sobra la pieza");
            ColorUtility.TryParseHtmlString("#3366AA", out var azul);
            Assert.That(gorra.PieceColor, Is.EqualTo(azul),
                        "el color del sombrero tiene que viajar en la pieza");

            var gorro = OutfitLook.From(Prenda("cloth_gorro_de_lana_con_pompom", "hat",
                                               "#CC4444"));
            Assert.IsFalse(gorro.HasGarment,
                           "el gorro tiñe el cuerpo igual que la gorra");
        }

        [Test]
        public void UnaPaletaRotaDaColorDeducidoYEstable()
        {
            var primera = OutfitLook.From(Prenda("cloth_rara", "outfit", "no-es-un-color"));
            var segunda = OutfitLook.From(Prenda("cloth_rara", "outfit", "no-es-un-color"));

            Assert.IsTrue(primera.HasGarment,
                          "una paleta mal escrita no puede dejar a nadie en cueros");
            Assert.That(primera.Garment, Is.EqualTo(segunda.Garment),
                        "el mismo id tiene que dar el mismo color siempre");
        }

        // ── el constructor: look → malla ─────────────────────────────────────

        private static ChibiMeshes Construye(in OutfitLook look)
            => ChibiMeshBuilder.Build(AppearanceData.Default, look);

        [Test]
        public void SinPrendaLaMallaEsLaMismaQueSiempre()
        {
            var legado = ChibiMeshBuilder.Build(AppearanceData.Default);
            var desnudo = Construye(OutfitLook.Bare());

            Assert.That(desnudo.Skin.vertices.Length,
                        Is.EqualTo(legado.Skin.vertices.Length),
                        "quitar la ropa no puede cambiar al muñeco de siempre");
            Assert.That(desnudo.Clothes.vertices.Length,
                        Is.EqualTo(legado.Clothes.vertices.Length));
            Assert.IsNull(desnudo.Extra, "sin prenda no hay pieza que crear");
        }

        [Test]
        public void UnPantalonMueveLasPiernasDeLaPielALaRopa()
        {
            var desnudo = Construye(OutfitLook.Bare());
            var pantalon = Construye(OutfitLook.From(
                Prenda("cloth_pantalones_cargo_marrones", "outfit", "#8B7355")));

            Assert.That(pantalon.Skin.vertices.Length,
                        Is.LessThan(desnudo.Skin.vertices.Length),
                        "con pantalón las piernas ya no son piel");
            Assert.That(pantalon.Clothes.vertices.Length,
                        Is.GreaterThan(desnudo.Clothes.vertices.Length),
                        "y ahora son ropa");
        }

        [Test]
        public void UnVestidoEnsanchaLaSiluetaDeLaRopa()
        {
            var desnudo = Construye(OutfitLook.Bare());
            var vestido = Construye(OutfitLook.From(
                Prenda("cloth_vestido_de_algodon_celeste", "outfit",
                       "#B8D4E8", "#FFFFFF")));

            Assert.That(vestido.Clothes.bounds.size.x,
                        Is.GreaterThan(desnudo.Clothes.bounds.size.x * 1.3f),
                        "la campana de la falda tiene que abrirse más que la cadera");
        }

        [Test]
        public void UnSombreroAnadeUnaPiezaPropia()
        {
            var gorra = Construye(OutfitLook.From(
                Prenda("cloth_gorra_nimbo_clasica", "hat", "#3366AA")));

            Assert.IsNotNull(gorra.Extra, "el sombrero tiene que existir como pieza");
            Assert.That(gorra.Extra.vertexCount, Is.GreaterThan(0));

            var pantalon = Construye(OutfitLook.From(
                Prenda("cloth_pantalones_cargo_marrones", "outfit", "#8B7355")));
            Assert.IsNull(pantalon.Extra, "un pantalón no cuelga nada del cuerpo");
        }
    }
}
