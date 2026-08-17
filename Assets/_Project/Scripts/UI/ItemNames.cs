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
    /// Si el catálogo no lo conoce se recorta el prefijo del identificador en vez de
    /// escribirlo entero. Un material recién añadido y todavía sin ficha sale como
    /// «savia» y no como <c>mat_savia</c>, que es lo que se vio en la ficha de casa
    /// antes de que existiera esto.
    /// </remarks>
    public static class ItemNames
    {
        public static string Of(string catalogId)
        {
            if (string.IsNullOrEmpty(catalogId)) return "";

            if (ServiceRegistry.TryGet<IEconomyService>(out var economy))
            {
                var item = economy.GetItem(catalogId);
                if (item != null && !string.IsNullOrEmpty(item.DisplayName))
                    return item.DisplayName.ToLowerInvariant();
            }

            return Trimmed(catalogId);
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
