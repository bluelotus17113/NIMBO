namespace Nimbo.Data.Islanders
{
    public enum HairCap { Full = 0, Short = 1, Shaved = 2 }
    public enum HairFringe { None = 0, Straight = 1, Side = 2, Long = 3 }
    public enum HairBack { None = 0, Short = 1, Long = 2 }
    public enum HairSides { None = 0, Tails = 1, Braids = 2, Curls = 3 }
    public enum HairCrown { None = 0, Bun = 1, DoubleBun = 2, Spike = 3, Antenna = 4 }

    /// <summary>Un peinado: de qué piezas se compone y cuándo se puede llevar.</summary>
    public readonly struct HairStyle
    {
        public readonly string Name;
        public readonly HairCap Cap;
        public readonly HairFringe Fringe;
        public readonly HairBack Back;
        public readonly HairSides Sides;
        public readonly HairCrown Crown;

        /// <summary>Nivel de isla que hace falta. 1 es «desde el primer día».</summary>
        public readonly int UnlockLevel;

        public HairStyle(string name, HairCap cap, HairFringe fringe, HairBack back,
                         HairSides sides, HairCrown crown, int unlockLevel = 1)
        {
            Name = name; Cap = cap; Fringe = fringe; Back = back;
            Sides = sides; Crown = crown; UnlockLevel = unlockLevel;
        }
    }

    /// <summary>
    /// Los cuarenta peinados: treinta y dos de salida y ocho que se abren jugando.
    /// </summary>
    /// <remarks>
    /// Están escritos uno a uno en vez de sacarlos de una fórmula sobre el número de
    /// estilo. Antes se hacía así —«si el estilo es par, flequillo; si pasa de ocho,
    /// moño»— y tenía dos problemas: varios números daban exactamente el mismo pelo,
    /// y no se podía nombrar ninguno, así que el creador de personajes solo enseñaba
    /// «Peinado 13». Con la tabla, cada entrada es una decisión y se le puede poner
    /// nombre en la ficha.
    ///
    /// El orden importa y es de guardado: el número de estilo va dentro de la partida,
    /// así que **no se reordena ni se inserta en medio**. Lo nuevo va al final.
    ///
    /// Vive en Data y no en Art porque lo necesitan los dos lados: el que dibuja el
    /// pelo y el creador de personajes, que están en ensamblados que no se ven entre sí.
    /// </remarks>
    public static class HairStyles
    {
        private static readonly HairStyle[] All =
        {
            // ── Cortos ──────────────────────────────────────────────────────
            new("Rapado",        HairCap.Shaved, HairFringe.None,     HairBack.None,  HairSides.None,   HairCrown.None),
            new("Al uno",        HairCap.Shaved, HairFringe.Straight, HairBack.None,  HairSides.None,   HairCrown.None),
            new("Corte clásico", HairCap.Short,  HairFringe.Side,     HairBack.None,  HairSides.None,   HairCrown.None),
            new("De lado",       HairCap.Short,  HairFringe.Side,     HairBack.Short, HairSides.None,   HairCrown.None),
            // Un tazón es flequillo largo y nada por detrás. Estaba escrito igual que
            // «Media melena» y salían dos peinados idénticos con dos nombres.
            new("Tazón",         HairCap.Full,   HairFringe.Long,     HairBack.None,  HairSides.None,   HairCrown.None),
            new("Revuelto",      HairCap.Short,  HairFringe.Straight, HairBack.None,  HairSides.None,   HairCrown.Antenna),
            new("Con cresta",    HairCap.Short,  HairFringe.None,     HairBack.None,  HairSides.None,   HairCrown.Spike),
            new("Punk",          HairCap.Shaved, HairFringe.None,     HairBack.None,  HairSides.None,   HairCrown.Spike),

            // ── Medios ──────────────────────────────────────────────────────
            new("Media melena",  HairCap.Full,   HairFringe.Straight, HairBack.Short, HairSides.None,   HairCrown.None),
            new("Bob",           HairCap.Full,   HairFringe.Straight, HairBack.Short, HairSides.Curls,  HairCrown.None),
            new("Despeinado",    HairCap.Full,   HairFringe.Side,     HairBack.Short, HairSides.None,   HairCrown.Antenna),
            new("Con raya",      HairCap.Full,   HairFringe.Side,     HairBack.Short, HairSides.None,   HairCrown.None),
            new("Cortina",       HairCap.Full,   HairFringe.Long,     HairBack.Short, HairSides.None,   HairCrown.None),
            new("Ondas",         HairCap.Full,   HairFringe.Side,     HairBack.Short, HairSides.Curls,  HairCrown.None),
            new("Coletas",       HairCap.Full,   HairFringe.Straight, HairBack.None,  HairSides.Tails,  HairCrown.None),
            new("Trenzas",       HairCap.Full,   HairFringe.Straight, HairBack.None,  HairSides.Braids, HairCrown.None),

            // ── Largos ──────────────────────────────────────────────────────
            new("Melena",        HairCap.Full,   HairFringe.None,     HairBack.Long,  HairSides.None,   HairCrown.None),
            new("Melena lisa",   HairCap.Full,   HairFringe.Straight, HairBack.Long,  HairSides.None,   HairCrown.None),
            new("Melena de lado",HairCap.Full,   HairFringe.Side,     HairBack.Long,  HairSides.None,   HairCrown.None),
            new("Melenón",       HairCap.Full,   HairFringe.Long,     HairBack.Long,  HairSides.None,   HairCrown.None),
            new("Rizos largos",  HairCap.Full,   HairFringe.Side,     HairBack.Long,  HairSides.Curls,  HairCrown.None),
            new("Coletas largas",HairCap.Full,   HairFringe.Straight, HairBack.Long,  HairSides.Tails,  HairCrown.None),
            new("Trenzas largas",HairCap.Full,   HairFringe.Side,     HairBack.Long,  HairSides.Braids, HairCrown.None),
            new("Suelto",        HairCap.Full,   HairFringe.None,     HairBack.Long,  HairSides.Curls,  HairCrown.None),

            // ── Recogidos ───────────────────────────────────────────────────
            new("Moño",          HairCap.Full,   HairFringe.None,     HairBack.None,  HairSides.None,   HairCrown.Bun),
            new("Moño con raya", HairCap.Full,   HairFringe.Side,     HairBack.None,  HairSides.None,   HairCrown.Bun),
            new("Moño bajo",     HairCap.Full,   HairFringe.Straight, HairBack.Short, HairSides.None,   HairCrown.Bun),
            new("Dos moños",     HairCap.Full,   HairFringe.Straight, HairBack.None,  HairSides.None,   HairCrown.DoubleBun),
            new("Moño y coletas",HairCap.Full,   HairFringe.None,     HairBack.None,  HairSides.Tails,  HairCrown.Bun),
            new("Recogido",      HairCap.Short,  HairFringe.Side,     HairBack.None,  HairSides.None,   HairCrown.Bun),
            new("Trenza y moño", HairCap.Full,   HairFringe.Straight, HairBack.Short, HairSides.Braids, HairCrown.Bun),
            new("Alborotado",    HairCap.Full,   HairFringe.Long,     HairBack.Short, HairSides.Curls,  HairCrown.Antenna),

            // ── Los ocho que se ganan ───────────────────────────────────────
            //
            // Van al final y con nivel: son los llamativos, los que se enseñan. Un
            // peinado que no puede llevar todo el mundo es un motivo pequeño para
            // seguir haciendo crecer la isla, y no cuesta nada de contenido nuevo.
            new("Cresta doble",   HairCap.Shaved, HairFringe.None,     HairBack.None,  HairSides.Tails,  HairCrown.Spike,     3),
            new("Nube",           HairCap.Full,   HairFringe.None,     HairBack.Short, HairSides.Curls,  HairCrown.DoubleBun, 4),
            new("Antenas",        HairCap.Short,  HairFringe.Straight, HairBack.None,  HairSides.Tails,  HairCrown.Antenna,   5),
            new("Cascada",        HairCap.Full,   HairFringe.Long,     HairBack.Long,  HairSides.Braids, HairCrown.None,      6),
            new("Corona",         HairCap.Full,   HairFringe.Side,     HairBack.Long,  HairSides.Braids, HairCrown.Bun,       7),
            new("Tormenta",       HairCap.Shaved, HairFringe.Long,     HairBack.Long,  HairSides.None,   HairCrown.Spike,     8),
            new("Constelación",   HairCap.Full,   HairFringe.Straight, HairBack.Long,  HairSides.Curls,  HairCrown.DoubleBun, 9),
            new("Nimbo",          HairCap.Full,   HairFringe.Long,     HairBack.Long,  HairSides.Curls,  HairCrown.Antenna,  10),
        };

        public static int Count => All.Length;

        /// <summary>Los que están disponibles desde el primer día.</summary>
        public const int BaseCount = 32;

        public static HairStyle Get(int index)
        {
            if (index < 0 || index >= All.Length) return All[0];
            return All[index];
        }

        public static string NameOf(int index) => Get(index).Name;

        /// <summary>Cuántos peinados hay abiertos con la isla en ese nivel.</summary>
        public static int AvailableAt(int islandLevel)
        {
            int available = 0;
            for (int i = 0; i < All.Length; i++)
                if (All[i].UnlockLevel <= islandLevel) available++;
            return available;
        }

        /// <summary>
        /// El peinado abierto número <paramref name="ordinal"/> con ese nivel de isla.
        /// </summary>
        /// <remarks>
        /// Hace falta porque los desbloqueables no van todos al final por nivel: el
        /// deslizador del creador recorre lo que hay abierto sin huecos, y sin esto
        /// habría que enseñar peinados en gris que no se pueden elegir.
        /// </remarks>
        public static int StyleAt(int ordinal, int islandLevel)
        {
            int seen = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (All[i].UnlockLevel > islandLevel) continue;
                if (seen == ordinal) return i;
                seen++;
            }
            return 0;
        }
    }
}
