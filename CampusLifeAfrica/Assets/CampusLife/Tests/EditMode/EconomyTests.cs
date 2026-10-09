using System;
using CampusLife.Core.Content;
using CampusLife.Core.Economy;
using NUnit.Framework;

namespace CampusLife.Tests
{
    public class EconomyTests
    {
        [Test]
        public void FormatsCedis()
        {
            Assert.AreEqual("GH₵0.50", Money.Format(50));
            Assert.AreEqual("GH₵1,250.05", Money.Format(125005));
            Assert.AreEqual("-GH₵8.00", Money.Format(-800));
            Assert.AreEqual(1050, Money.FromCedis(10, 50));
            Assert.Throws<ArgumentOutOfRangeException>(() => Money.FromCedis(1, 100));
        }

        [Test]
        public void CreditAndDebitUpdateBalanceAndLedger()
        {
            var w = new Wallet(1000);
            Assert.AreEqual(TransactionResult.Applied, w.Credit("a", 500, "allowance", "", 0));
            Assert.AreEqual(TransactionResult.Applied, w.Debit("b", 300, "food", "", 1));
            Assert.AreEqual(1200, w.Balance);
            Assert.AreEqual(2, w.Ledger.Count);
            Assert.AreEqual(-300, w.Ledger[1].amountPesewas);
            Assert.AreEqual(1200, w.Ledger[1].balanceAfterPesewas);
        }

        [Test]
        public void DuplicateTransactionIsRejected()
        {
            var w = new Wallet(0);
            Assert.AreEqual(TransactionResult.Applied, w.Credit("reward:1", 100, "reward", "", 0));
            Assert.AreEqual(TransactionResult.Duplicate, w.Credit("reward:1", 100, "reward", "", 0));
            Assert.AreEqual(100, w.Balance);
        }

        [Test]
        public void CannotOverdrawOrUseInvalidAmounts()
        {
            var w = new Wallet(100);
            Assert.AreEqual(TransactionResult.InsufficientFunds, w.Debit("x", 101, "food", "", 0));
            Assert.AreEqual(TransactionResult.InvalidAmount, w.Debit("y", 0, "food", "", 0));
            Assert.AreEqual(TransactionResult.InvalidAmount, w.Debit("z", -5, "food", "", 0));
            Assert.AreEqual(TransactionResult.InvalidId, w.Credit(" ", 5, "food", "", 0));
            Assert.AreEqual(100, w.Balance);
            Assert.AreEqual(0, w.Ledger.Count);
            // A rejected id can be retried later because it was never applied.
            Assert.AreEqual(TransactionResult.Applied, w.Debit("x", 100, "food", "", 0));
        }

        [Test]
        public void LedgerRoundTripsAndDetectsTampering()
        {
            var w = new Wallet(500);
            w.Credit("a", 250, "allowance", "Allowance", 10);
            w.Debit("b", 125, "food", "Lunch", 20);
            var restored = Wallet.FromData(w.ToData());
            Assert.AreEqual(w.Balance, restored.Balance);
            Assert.IsTrue(restored.HasTransaction("b"));

            var tampered = w.ToData();
            tampered.transactions[1].balanceAfterPesewas = 999999;
            Assert.Throws<InvalidOperationException>(() => Wallet.FromData(tampered));

            var duplicated = w.ToData();
            duplicated.transactions.Add(duplicated.transactions[0]);
            Assert.Throws<InvalidOperationException>(() => Wallet.FromData(duplicated));
        }

        [Test]
        public void PurchaseIsAtomic()
        {
            var catalog = Phase1Content.CreateItemCatalog();
            var wallet = new Wallet(1000);
            var inv = new Inventory();

            Assert.AreEqual(PurchaseResult.Purchased, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemMalt, 1, "t1", 0));
            Assert.AreEqual(200, wallet.Balance);
            Assert.AreEqual(1, inv.Count(Ids.ItemMalt));

            Assert.AreEqual(PurchaseResult.InsufficientFunds, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemMeatPie, 1, "t2", 0));
            Assert.AreEqual(200, wallet.Balance);
            Assert.AreEqual(0, inv.Count(Ids.ItemMeatPie));

            Assert.AreEqual(PurchaseResult.Duplicate, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemSachetWater, 1, "t1", 0));
            Assert.AreEqual(0, inv.Count(Ids.ItemSachetWater));

            Assert.AreEqual(PurchaseResult.Purchased, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemSachetWater, 4, "t3", 0));
            Assert.AreEqual(0, wallet.Balance);
            Assert.AreEqual(4, inv.Count(Ids.ItemSachetWater));
        }

        [Test]
        public void FreeAndUnknownItemsCannotBeBought()
        {
            var catalog = Phase1Content.CreateItemCatalog();
            var wallet = new Wallet(1000);
            var inv = new Inventory();
            Assert.AreEqual(PurchaseResult.UnknownItem, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemLibraryCard, 1, "t", 0));
            Assert.AreEqual(PurchaseResult.UnknownItem, Commerce.TryPurchase(wallet, inv, catalog, "laptop", 1, "t", 0));
            Assert.AreEqual(PurchaseResult.InvalidQuantity, Commerce.TryPurchase(wallet, inv, catalog, Ids.ItemMalt, 0, "t", 0));
            Assert.AreEqual(1000, wallet.Balance);
        }

        [Test]
        public void InventoryAddRemove()
        {
            var inv = new Inventory();
            inv.Add("a", 2);
            Assert.IsFalse(inv.Remove("a", 3));
            Assert.IsTrue(inv.Remove("a", 2));
            Assert.IsFalse(inv.Has("a"));
            var restored = Inventory.FromData(new[] { new InventoryEntry { itemId = "b", count = 3 }, new InventoryEntry { itemId = "c", count = -1 } });
            Assert.AreEqual(3, restored.Count("b"));
            Assert.AreEqual(0, restored.Count("c"));
        }
    }
}
