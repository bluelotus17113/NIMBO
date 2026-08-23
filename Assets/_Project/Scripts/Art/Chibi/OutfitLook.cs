using Nimbo.Core.Services.Contracts;
using UnityEngine;

namespace Nimbo.Art.Chibi
{
    /// <summary>La pieza extra que una prenda añade a la silueta del muñeco.</summary>
    public enum GarmentPiece
    {
        None = 0,
        Skirt,       // falda o vestido: campana sobre las piernas
        Hood,        // capucha: casquete detrás de la cabeza
        Cape,        // capa o poncho: tabla a la espalda
        HatBrimmed,  // sombrero con ala
        HatBeanie,   // gorro ceñido, con o sin pompón
    }

    /// <summary>
    /// Cómo se ve una prenda puesta: color de la ropa, si tapa las piernas y qué
    /// pieza extra añade a la silueta.
    /// </summary>
    /// <remarks>
    /// Es el traductor entre el catálogo (que habla de paletas y ranuras) y la malla
    /// (que solo entiende de colores y piezas). Vive en Arte y no en Economía porque
    /// es una decisión de representación —qué ranura se dibuja y cuál no—, no de datos.
    ///
    /// A tres metros de cámara lo que distingue una prenda de otra es el color del
    /// torso y de las piernas, y lo que distingue una familia de prendas es una pieza:
    /// falda, capucha, capa o sombrero. Nada más hace falta para que «me puso un
    /// vestido» se lea en pantalla.
    /// </remarks>
    public readonly struct OutfitLook
    {
        public readonly bool HasGarment;
        public readonly Color Garment;
        public readonly GarmentPiece Piece;
        public readonly Color PieceColor;

        private OutfitLook(bool has, Color garment, GarmentPiece piece, Color pieceColor)
        {
            HasGarment = has; Garment = garment; Piece = piece; PieceColor = pieceColor;
        }

        /// <summary>Sin prenda: el muñeco lleva su ropa de siempre.</summary>
        public static OutfitLook Bare() => new(false, Color.white, GarmentPiece.None, Color.white);

        /// <summary>
        /// El look de una entrada del catálogo, o <see cref="Bare"/> si no se puede
        /// vestir con ella.
        /// </summary>
        /// <remarks>
        /// Los accesorios (pulseras, gafas, collares) no se dibujan todavía: a la
        /// altura de los ojos una pulsera son cuatro píxeles y una gafa menos. Se
        /// queda como «sin prenda» y el muñeco conserva su ropa base, que es menos
        /// mentira que pintarle el torso color silbato.
        /// </remarks>
        public static OutfitLook From(IItemDefinition item)
        {
            if (item == null || string.IsNullOrEmpty(item.Slot)) return Bare();

            var main = ColourOf(item, 0);
            var second = ColourOf(item, 1);

            switch (item.Slot)
            {
                case "outfit":
                    return new OutfitLook(true, main, PieceOf(item.CatalogId),
                                          second == main ? main : second);

                case "hat":
                    // HasGarment=false a propósito: un sombrero no viste el cuerpo.
                    // Con true, IslanderView pintaba torso y cadera del color de la
                    // gorra y quien se ponía una gorra azul aparecía en azul entero.
                    // El color del sombrero viaja en PieceColor, que es lo que pinta
                    // la pieza Extra; la ropa del cuerpo conserva la suya de siempre.
                    var hat = item.CatalogId != null && item.CatalogId.Contains("gorro")
                        ? GarmentPiece.HatBeanie : GarmentPiece.HatBrimmed;
                    return new OutfitLook(false, Color.white, hat, main);

                default:
                    return Bare();
            }
        }

        /// <summary>
        /// La pieza por el identificador, que es dato estable y no un nombre que
        /// alguien pueda traducir. Capucha antes que capa: «capucha» no contiene la
        /// palabra «capa», pero el orden documentado evita sorpresas al ampliarlo.
        /// </summary>
        private static GarmentPiece PieceOf(string id)
        {
            if (id == null) return GarmentPiece.None;

            if (id.Contains("capucha") || id.Contains("sudadera")) return GarmentPiece.Hood;
            if (id.Contains("vestido") || id.Contains("falda") ||
                id.Contains("kimono") || id.Contains("tunica")) return GarmentPiece.Skirt;
            if (id.Contains("capa") || id.Contains("poncho") || id.Contains("abrigo"))
                return GarmentPiece.Cape;
            return GarmentPiece.None;
        }

        /// <summary>
        /// El color número <paramref name="index"/> de la paleta del catálogo. Si no
        /// hay, se deduce del identificador: mismo id, mismo color, en cualquier isla.
        /// </summary>
        private static Color ColourOf(IItemDefinition item, int index)
        {
            var palette = item.Palette;
            if (palette != null && palette.Length > index &&
                ColorUtility.TryParseHtmlString(palette[index], out var parsed))
                return parsed;

            uint hash = 2166136261u;
            string id = item.CatalogId ?? "";
            for (int i = 0; i < id.Length; i++)
            {
                hash ^= id[i];
                hash *= 16777619u;
            }
            return Color.HSVToRGB((hash & 0xFF) / 255f, 0.45f, 0.85f);
        }
    }
}
