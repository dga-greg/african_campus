using System;
using System.Collections.Generic;

namespace CampusLife.Core.Economy
{
    public sealed class ItemDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Description { get; }
        /// <summary>Shop price; 0 for items that can only be received (e.g. a library card).</summary>
        public long PricePesewas { get; }

        public ItemDefinition(string id, string displayName, string description, long pricePesewas)
        {
            if (pricePesewas < 0) throw new ArgumentOutOfRangeException(nameof(pricePesewas));
            Id = id; DisplayName = displayName; Description = description; PricePesewas = pricePesewas;
        }
    }

    public sealed class ItemCatalog
    {
        readonly Dictionary<string, ItemDefinition> _items = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);

        public ItemCatalog Add(ItemDefinition item)
        {
            if (_items.ContainsKey(item.Id)) throw new ArgumentException($"Duplicate item id '{item.Id}'.");
            _items.Add(item.Id, item);
            return this;
        }

        public bool TryGet(string id, out ItemDefinition item) => _items.TryGetValue(id ?? string.Empty, out item);

        public ItemDefinition Get(string id)
        {
            if (!TryGet(id, out var item)) throw new KeyNotFoundException($"Unknown item '{id}'.");
            return item;
        }

        public IEnumerable<ItemDefinition> All => _items.Values;
    }

    public enum PurchaseResult { Purchased, UnknownItem, InsufficientFunds, Duplicate, InvalidQuantity }

    public static class Commerce
    {
        /// <summary>
        /// Atomically debits the wallet and adds items. If the debit is not
        /// applied, inventory is left untouched.
        /// </summary>
        public static PurchaseResult TryPurchase(Wallet wallet, Inventory inventory, ItemCatalog catalog,
            string itemId, int quantity, string transactionId, double gameMinute)
        {
            if (quantity <= 0) return PurchaseResult.InvalidQuantity;
            if (!catalog.TryGet(itemId, out var item) || item.PricePesewas <= 0) return PurchaseResult.UnknownItem;
            long total;
            try { total = checked(item.PricePesewas * quantity); }
            catch (OverflowException) { return PurchaseResult.InvalidQuantity; }

            var result = wallet.Debit(transactionId, total, "purchase", $"{quantity} x {item.DisplayName}", gameMinute);
            switch (result)
            {
                case TransactionResult.Applied:
                    inventory.Add(item.Id, quantity);
                    return PurchaseResult.Purchased;
                case TransactionResult.Duplicate: return PurchaseResult.Duplicate;
                case TransactionResult.InsufficientFunds: return PurchaseResult.InsufficientFunds;
                default: return PurchaseResult.InvalidQuantity;
            }
        }
    }
}
