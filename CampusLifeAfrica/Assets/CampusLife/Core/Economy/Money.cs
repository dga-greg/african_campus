using System;
using System.Globalization;

namespace CampusLife.Core.Economy
{
    /// <summary>
    /// All money is stored as integer pesewas (100 pesewas = GH₵1) to avoid
    /// floating point rounding in balances.
    /// </summary>
    public static class Money
    {
        public const long PesewasPerCedi = 100;

        public static long FromCedis(long cedis, long pesewas = 0)
        {
            if (pesewas < 0 || pesewas >= PesewasPerCedi) throw new ArgumentOutOfRangeException(nameof(pesewas));
            checked { return cedis * PesewasPerCedi + (cedis < 0 ? -pesewas : pesewas); }
        }

        public static string Format(long pesewas)
        {
            string sign = pesewas < 0 ? "-" : string.Empty;
            long abs = Math.Abs(pesewas);
            long cedis = abs / PesewasPerCedi;
            long rest = abs % PesewasPerCedi;
            return sign + "GH₵" + cedis.ToString("N0", CultureInfo.InvariantCulture) + "." + rest.ToString("00", CultureInfo.InvariantCulture);
        }
    }
}
