using Nimbo.Core.Services;
using Nimbo.Core.Services.Contracts;

namespace Nimbo.UI
{
    /// <summary>
    /// Cómo se llama un objeto cuando hay que escribirlo en una frase.
    /// </summary>
    /// <remarks>
    /// En minúsculas porque casi siempre va dentro de una oración —«te faltan 2 de
    /// madera»— y una mayúscula en mitad de la frase canta.
    ///
    /// **Este es el único sitio donde se decide el nombre de un objeto.** Hubo tres:
    /// el hotbar recortaba el identificador («regadera»), la mochila escribía el
    /// nombre del catálogo tal cual y aquí se bajaba a minúsculas — el mismo objeto,
    /// tres voces según la pantalla que miraras. Desde entonces <see cref="Display"/>
    /// manda y las otras dos son vistas de él: <see cref="Of"/> la misma frase en
    /// minúsculas y <see cref="Short"/> su primera palabra para los huecos pequeños.
    ///
    /// Si el catálogo no lo conoce se recorta el prefijo del identificador en vez de
    /// escribirlo entero. Un material recién añadido y todavía sin ficha sale como
    /// «savia» y no como <c>mat_savia</c>, que es lo que se vio en la ficha de casa
    /// antes de que existiera esto.
    /// </remarks>
    public static class ItemNames
    {
        /// <summary>El nombre del catálogo tal cual lo escribe, o el identificador aseado.</summary>
        public static string Display(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return "";

            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
            {
                var item = economy.GetItem(catalogId);
                if (item != null && !string.IsNullOrEmpty(item.DisplayName))
                    return item.DisplayName;
            }

            return Trimmed(catalogId);
        }

        /// <summary>El nombre para dentro de una frase: en minúsculas.</summary>
        public static string Of(string catalogId) => Display(catalogId).ToLowerInvariant();

        /// <summary>
        /// El nombre corto para huecos pequeños: la primera palabra del nombre bueno.
        /// </summary>
        /// <remarks>
        /// Recortar el identificador daba una palabra sin relación garantizada con el
        /// catálogo; partir <see cref="Display"/> sale de la misma fuente, así que lo
        /// que lee el jugador en la barra es lo mismo que lee al abrir la mochila.
        /// </remarks>
        public static string Short(string catalogId)
        {
            string full = Display(catalogId);
            int space = full.IndexOf(' ');
            return space > 0 ? full[..space] : full;
        }

        private static string Trimmed(string catalogId)
        {
            int underscore = catalogId.IndexOf('_');
            return underscore > 0 && underscore < catalogId.Length - 1
                ? catalogId.Substring(underscore + 1).Replace('_', ' ')
                : catalogId;
        }
    }
}
