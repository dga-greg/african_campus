using System;
using System.Collections.Generic;

namespace CampusLife.Core.Economy
{
    public enum TransactionResult { Applied, Duplicate, InsufficientFunds, InvalidAmount, InvalidId }

    [Serializable]
    public class TransactionRecord
    {
        public string id;
        public string category;
        public string description;
        /// <summary>Signed amount: positive for credits, negative for debits.</summary>
        public long amountPesewas;
        public long balanceAfterPesewas;
        public double gameMinute;
    }

    [Serializable]
    public class WalletData
    {
        public long openingBalancePesewas;
        public List<TransactionRecord> transactions = new List<TransactionRecord>();
    }

    /// <summary>
    /// Ledger-backed wallet. Every change is a transaction with a unique id, so
    /// replaying the same reward or payment (double click, reloaded event) is
    /// rejected instead of being applied twice. The balance can always be
    /// recomputed from the opening balance plus the ledger.
    /// </summary>
    public sealed class Wallet
    {
        readonly List<TransactionRecord> _ledger = new List<TransactionRecord>();
        readonly HashSet<string> _appliedIds = new HashSet<string>(StringComparer.Ordinal);

        public long OpeningBalance { get; private set; }
        public long Balance { get; private set; }
        public IReadOnlyList<TransactionRecord> Ledger => _ledger;

        public event Action<Wallet, TransactionRecord> TransactionApplied;

        public Wallet(long openingBalancePesewas = 0)
        {
            if (openingBalancePesewas < 0) throw new ArgumentOutOfRangeException(nameof(openingBalancePesewas));
            OpeningBalance = openingBalancePesewas;
            Balance = openingBalancePesewas;
        }

        public bool HasTransaction(string id) => id != null && _appliedIds.Contains(id);

        public bool CanAfford(long pesewas) => pesewas >= 0 && Balance >= pesewas;

        public TransactionResult Credit(string id, long pesewas, string category, string description, double gameMinute)
            => Apply(id, pesewas, category, description, gameMinute);

        public TransactionResult Debit(string id, long pesewas, string category, string description, double gameMinute)
        {
            if (pesewas <= 0) return TransactionResult.InvalidAmount;
            return Apply(id, -pesewas, category, description, gameMinute);
        }

        TransactionResult Apply(string id, long signedAmount, string category, string description, double gameMinute)
        {
            if (string.IsNullOrWhiteSpace(id)) return TransactionResult.InvalidId;
            if (signedAmount == 0) return TransactionResult.InvalidAmount;
            if (_appliedIds.Contains(id)) return TransactionResult.Duplicate;
            long newBalance;
            try { newBalance = checked(Balance + signedAmount); }
            catch (OverflowException) { return TransactionResult.InvalidAmount; }
            if (newBalance < 0) return TransactionResult.InsufficientFunds;

            var record = new TransactionRecord
            {
                id = id,
                category = category ?? "general",
                description = description ?? string.Empty,
                amountPesewas = signedAmount,
                balanceAfterPesewas = newBalance,
                gameMinute = gameMinute,
            };
            _ledger.Add(record);
            _appliedIds.Add(id);
            Balance = newBalance;
            TransactionApplied?.Invoke(this, record);
            return TransactionResult.Applied;
        }

        public WalletData ToData()
        {
            var data = new WalletData { openingBalancePesewas = OpeningBalance };
            foreach (var t in _ledger)
            {
                data.transactions.Add(new TransactionRecord
                {
                    id = t.id, category = t.category, description = t.description,
                    amountPesewas = t.amountPesewas, balanceAfterPesewas = t.balanceAfterPesewas, gameMinute = t.gameMinute,
                });
            }
            return data;
        }

        /// <summary>
        /// Rebuilds a wallet from saved data, replaying and verifying every
        /// transaction. Throws <see cref="InvalidOperationException"/> if the
        /// ledger is inconsistent (tampered or corrupted save).
        /// </summary>
        public static Wallet FromData(WalletData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            var wallet = new Wallet(data.openingBalancePesewas);
            if (data.transactions == null) return wallet;
            foreach (var t in data.transactions)
            {
                var result = wallet.Apply(t.id, t.amountPesewas, t.category, t.description, t.gameMinute);
                if (result != TransactionResult.Applied)
                    throw new InvalidOperationException($"Wallet ledger rejected transaction '{t.id}': {result}.");
                if (wallet.Balance != t.balanceAfterPesewas)
                    throw new InvalidOperationException($"Wallet ledger balance mismatch at transaction '{t.id}'.");
            }
            return wallet;
        }
    }
}
