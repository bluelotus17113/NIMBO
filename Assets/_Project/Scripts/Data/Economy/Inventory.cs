using System;
using System.Collections.Generic;

namespace Nimbo.Data.Economy
{
    /// <summary>Cuántas monedas tiene el jugador. La isla tiene una sola moneda a propósito.</summary>
    [Serializable]
    public struct Wallet
    {
        public long Coins;
        public long TotalEarned;
        public long TotalSpent;

        public bool CanAfford(long price) => Coins >= price;
    }

    [Serializable]
    public struct ItemStack
    {
        public string CatalogId;
        public int Quantity;

        public ItemStack(string catalogId, int quantity)
        {
            CatalogId = catalogId;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// Lo que el jugador tiene guardado para dar o colocar. Los objetos consumibles
    /// se apilan; los muebles y la ropa también, porque nada distingue dos sillas iguales.
    /// </summary>
    [Serializable]
    public class Inventory
    {
        public List<ItemStack> Stacks = new List<ItemStack>();

        public int CountOf(string catalogId)
        {
            for (int i = 0; i < Stacks.Count; i++)
                if (Stacks[i].CatalogId == catalogId) return Stacks[i].Quantity;
            return 0;
        }

        public void Add(string catalogId, int quantity = 1)
        {
            if (quantity <= 0) return;
            for (int i = 0; i < Stacks.Count; i++)
            {
                if (Stacks[i].CatalogId != catalogId) continue;
                var s = Stacks[i];
                s.Quantity += quantity;
                Stacks[i] = s;
                return;
            }
            Stacks.Add(new ItemStack(catalogId, quantity));
        }

        /// <summary>Saca del inventario. Devuelve false y no toca nada si no había suficiente.</summary>
        public bool Remove(string catalogId, int quantity = 1)
        {
            for (int i = 0; i < Stacks.Count; i++)
            {
                if (Stacks[i].CatalogId != catalogId) continue;
                if (Stacks[i].Quantity < quantity) return false;

                var s = Stacks[i];
                s.Quantity -= quantity;
                if (s.Quantity == 0) Stacks.RemoveAt(i); else Stacks[i] = s;
                return true;
            }
            return false;
        }
    }
}
