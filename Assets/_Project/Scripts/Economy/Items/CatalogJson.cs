using System;
using System.Collections.Generic;

namespace Nimbo.Economy.Items
{
    /// <summary>Raíz de un JSON de catálogo, con versión, categoría y lista de items.</summary>
    [Serializable]
    public class CatalogRootJson
    {
        public int version;
        public string category;
        public List<CatalogItemJson> items;
    }

    /// <summary>
    /// Un item tal cual viene en el JSON. Todos los campos son opcionales porque cada
    /// categoría trae los suyos; Newtonsoft deja en cero o null los que no aparecen.
    /// </summary>
    [Serializable]
    public class CatalogItemJson
    {
        // ── comunes a los cuatro catálogos ──
        public string catalogId;
        public string displayName;
        public string description;
        public int price;
        public int unlockLevel;
        public List<string> tags;

        // ── herramientas ──
        public string toolKind;

        /// <summary>1 la de siempre, 2 la crafteada. Sin escribir se lee como 1.</summary>
        public int toolTier;

        // ── comida ──
        public int hungerRestore;
        public string foodKind;

        // ── ropa ──
        public string slot;
        public string style;
        public List<string> palette;

        // ── muebles ──
        public string layer;
        public int footprintX;
        public int footprintY;
        public string function;
        public Dictionary<string, int> needBonus;

        // ── acabados ──
        public string surface;
        public string baseColor;
        public string pattern;
    }
}
