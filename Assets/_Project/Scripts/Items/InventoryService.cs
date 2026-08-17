using System;
using System.Collections.Generic;
using Nimbo.Core.Events;
using Nimbo.Core.Services.Contracts;
using Nimbo.Data.Economy;
using Nimbo.Data.Player;

namespace Nimbo.Items
{
    /// <summary>
    /// La mochila del protagonista: 24 huecos posicionales, pilas hasta 99
    /// (1 para herramientas), y lo que lleva en la mano.
    /// </summary>
    /// <remarks>
    /// Los huecos vacíos son <see cref="ItemStack"/> con <c>Quantity=0</c>, no
    /// elementos que se quitan de la lista. Si la lista se compacta, todo lo que
    /// hay a la derecha se desplaza y la mochila se reordena sola delante del
    /// jugador — la prueba 7 del encargo existe para pillarlo.
    /// </remarks>
    public class InventoryService : IInventoryService
    {
        const int MaxStack = 99;

        readonly PlayerState _player;
        readonly IEconomyService _economy;

        public int SlotCount => 24;
        public int HotbarSize => 10;

        public int SelectedSlot => _player.SelectedSlot;

        /// <summary>Todo lo que lleva, hueco a hueco. La misma instancia que guarda el estado.</summary>
        public IReadOnlyList<ItemStack> Slots => _player.Bag.Stacks;

        public ItemStack InHand => At(_player.SelectedSlot);

        public InventoryService(PlayerState player, IEconomyService economy)
        {
            _player = player;
            _economy = economy;
            NormalizeBag();
        }

        // ── normalización ────────────────────────────────────────────────────

        /// <summary>
        /// La mochila tiene que tener exactamente <see cref="SlotCount"/> huecos
        /// siempre. Si una partida vieja guardó menos (o más), lo arreglamos al cargar.
        /// </summary>
        void NormalizeBag()
        {
            var stacks = _player.Bag.Stacks;
            // Rellenar huecos que falten
            while (stacks.Count < SlotCount)
                stacks.Add(new ItemStack("", 0));
            // Recortar si sobra (no debería, pero por si acaso)
            if (stacks.Count > SlotCount)
                stacks.RemoveRange(SlotCount, stacks.Count - SlotCount);
            // El hueco seleccionado también puede venir mal de una partida antigua
            if (_player.SelectedSlot < 0 || _player.SelectedSlot >= HotbarSize)
                _player.SelectedSlot = 0;
        }

        // ── consulta ──────────────────────────────────────────────────────────

        /// <summary>Lo que hay en ese hueco. Fuera de rango devuelve vacío.</summary>
        public ItemStack At(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return new ItemStack("", 0);
            return _player.Bag.Stacks[slot];
        }

        /// <summary>Cuántos lleva encima de ese objeto, sumando todas las pilas.</summary>
        public int CountOf(string catalogId)
        {
            var total = 0;
            var stacks = _player.Bag.Stacks;
            for (int i = 0; i < stacks.Count; i++)
                if (stacks[i].CatalogId == catalogId)
                    total += stacks[i].Quantity;
            return total;
        }

        // ── límite de pila ────────────────────────────────────────────────────

        /// <summary>
        /// 1 para herramientas (dos azadas = dos huecos), 99 para todo lo demás.
        /// Si el objeto no está en el catálogo, devuelve 99 por seguridad.
        /// </summary>
        public int StackLimitOf(string catalogId)
        {
            var def = _economy.GetItem(catalogId);
            if (def == null) return MaxStack;
            return def.Category == ItemCategory.Tool ? 1 : MaxStack;
        }

        // ── selección ─────────────────────────────────────────────────────────

        /// <summary>
        /// Selecciona un hueco de la barra de abajo. Recorta al rango de la barra,
        /// no de la mochila entera: el hueco 20 no se ve y no tiene sentido tenerlo
        /// seleccionado.
        /// </summary>
        public void Select(int slot)
        {
            slot = Math.Clamp(slot, 0, HotbarSize - 1);
            if (slot == _player.SelectedSlot) return;
            _player.SelectedSlot = slot;
            EventBus.Publish(new SlotSelected(slot));
        }

        // ── lo que llevas en la mano ──────────────────────────────────────────

        /// <summary>
        /// Qué herramienta lleva en la mano, o <c>None</c> si el hueco está vacío
        /// o lo que lleva no es una herramienta.
        /// </summary>
        public ToolKind ToolInHand
        {
            get
            {
                // SelectedSlot puede venir mal de una partida antigua
                var slot = _player.SelectedSlot;
                if (slot < 0 || slot >= SlotCount) return ToolKind.None;
                var stack = _player.Bag.Stacks[slot];
                if (stack.Quantity <= 0) return ToolKind.None;
                var def = _economy.GetItem(stack.CatalogId);
                if (def == null || def.Category != ItemCategory.Tool) return ToolKind.None;

                // Del catálogo, no del nombre del identificador. Deducirlo del nombre
                // daba «ninguna» para todas: los ids están en castellano y se buscaban
                // en inglés, así que no se podía ni labrar ni regar y nada fallaba.
                return def.Tool;
            }
        }

        /// <summary>De qué escalón es lo que lleva en la mano. 1 si no es herramienta.</summary>
        public int ToolTierInHand
        {
            get
            {
                var slot = _player.SelectedSlot;
                if (slot < 0 || slot >= SlotCount) return 1;

                var stack = _player.Bag.Stacks[slot];
                if (stack.Quantity <= 0) return 1;

                var def = _economy.GetItem(stack.CatalogId);
                return def == null || def.Category != ItemCategory.Tool ? 1 : def.ToolTier;
            }
        }

        // ── guardar ───────────────────────────────────────────────────────────

        /// <summary>
        /// Mete lo que pueda en la mochila. Llena primero las pilas que ya haya de
        /// ese objeto y luego los huecos vacíos, en orden.
        /// </summary>
        public StoreResult TryStore(string catalogId, int quantity, out int leftover)
        {
            leftover = 0;
            if (quantity <= 0) return StoreResult.Ok;

            // Un objeto que no existe en el catálogo no entra
            var def = _economy.GetItem(catalogId);
            if (def == null) return StoreResult.UnknownItem;

            var limit = def.Category == ItemCategory.Tool ? 1 : MaxStack;
            var remaining = quantity;
            var stacks = _player.Bag.Stacks;

            // Primera pasada: rellenar pilas que ya haya de este objeto
            for (int i = 0; i < stacks.Count && remaining > 0; i++)
            {
                if (stacks[i].CatalogId != catalogId) continue;
                var room = limit - stacks[i].Quantity;
                if (room <= 0) continue;
                var add = Math.Min(room, remaining);
                stacks[i] = new ItemStack(catalogId, stacks[i].Quantity + add);
                remaining -= add;
            }

            // Segunda pasada: ocupar huecos vacíos
            for (int i = 0; i < stacks.Count && remaining > 0; i++)
            {
                if (stacks[i].Quantity > 0) continue;
                var add = Math.Min(limit, remaining);
                stacks[i] = new ItemStack(catalogId, add);
                remaining -= add;
            }

            leftover = remaining;

            // Publicar después de cambiar el estado, con el hueco concreto si fue uno solo
            if (remaining == quantity)
            {
                // No entró nada
                return StoreResult.Full;
            }

            PublishInventoryChanged(remaining > 0 ? -1 : FindLastChangedSlot(catalogId));
            return remaining > 0 ? StoreResult.Partial : StoreResult.Ok;
        }

        /// <summary>
        /// Encuentra el hueco del último cambio (para TryStore y TryTake).
        /// Devuelve -1 si no encuentra ninguno o si cambió más de uno.
        /// </summary>
        int FindLastChangedSlot(string catalogId)
        {
            var found = -1;
            var stacks = _player.Bag.Stacks;
            for (int i = 0; i < stacks.Count; i++)
            {
                if (stacks[i].CatalogId != catalogId) continue;
                if (found >= 0) return -1; // más de uno: ambiguo
                found = i;
            }
            return found;
        }

        void PublishInventoryChanged(int slot)
        {
            EventBus.Publish(new InventoryChanged(slot));
        }

        // ── sacar ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Saca esa cantidad del objeto, de cualquier pila. Falso y sin tocar nada
        /// si no llevaba suficiente.
        /// </summary>
        public bool TryTake(string catalogId, int quantity = 1)
        {
            if (quantity <= 0) return false;

            // Verificar que tiene suficiente antes de tocar nada
            if (CountOf(catalogId) < quantity) return false;

            var remaining = quantity;
            var stacks = _player.Bag.Stacks;

            // Sacar desde el final: así se vacían primero los huecos de más a la derecha
            for (int i = stacks.Count - 1; i >= 0 && remaining > 0; i--)
            {
                if (stacks[i].CatalogId != catalogId) continue;
                var take = Math.Min(stacks[i].Quantity, remaining);
                stacks[i] = new ItemStack(catalogId, stacks[i].Quantity - take);
                remaining -= take;
            }

            PublishInventoryChanged(FindLastChangedSlot(catalogId));
            return true;
        }

        /// <summary>
        /// Saca del hueco seleccionado. Para gastar lo que llevas en la mano.
        /// </summary>
        public bool TryConsumeSelected(int quantity = 1)
        {
            if (quantity <= 0) return false;

            var slot = _player.SelectedSlot;
            // SelectedSlot puede venir mal de una partida antigua o de código externo
            if (slot < 0 || slot >= SlotCount) return false;
            var stack = _player.Bag.Stacks[slot];
            if (stack.Quantity < quantity) return false;

            _player.Bag.Stacks[slot] = new ItemStack(stack.CatalogId, stack.Quantity - quantity);
            EventBus.Publish(new InventoryChanged(slot));
            return true;
        }

        // ── soltar ────────────────────────────────────────────────────────────

        /// <summary>
        /// Tira lo que haya en ese hueco. No lo devuelve a ningún sitio.
        /// Un hueco ya vacío también devuelve true: el resultado es el mismo.
        /// </summary>
        public bool Drop(int slot)
        {
            if (slot < 0 || slot >= SlotCount) return false;
            _player.Bag.Stacks[slot] = new ItemStack("", 0);
            EventBus.Publish(new InventoryChanged(slot));
            return true;
        }

        // ── ordenar ───────────────────────────────────────────────────────────

        /// <summary>
        /// Intercambia dos huecos. Es como el jugador ordena la mochila.
        /// Con el mismo índice no hace nada.
        /// </summary>
        public void Swap(int a, int b)
        {
            if (a == b) return;
            if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount) return;

            var stacks = _player.Bag.Stacks;
            var temp = stacks[a];
            stacks[a] = stacks[b];
            stacks[b] = temp;

            // Publicar -1: cambiaron dos huecos
            EventBus.Publish(new InventoryChanged(-1));
        }
    }
}
