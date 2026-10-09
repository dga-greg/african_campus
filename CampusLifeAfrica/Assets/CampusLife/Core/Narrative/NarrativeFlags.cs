using System;
using System.Collections.Generic;

namespace CampusLife.Core.Narrative
{
    /// <summary>Persistent story flags set by choices and events.</summary>
    public sealed class NarrativeFlags
    {
        readonly HashSet<string> _flags = new HashSet<string>(StringComparer.Ordinal);

        public event Action<string, bool> Changed;

        public bool Has(string flag) => flag != null && _flags.Contains(flag);

        public void Set(string flag)
        {
            if (string.IsNullOrEmpty(flag)) throw new ArgumentException("Flag id required.", nameof(flag));
            if (_flags.Add(flag)) Changed?.Invoke(flag, true);
        }

        public void Clear(string flag)
        {
            if (flag != null && _flags.Remove(flag)) Changed?.Invoke(flag, false);
        }

        public List<string> ToData()
        {
            var list = new List<string>(_flags);
            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public static NarrativeFlags FromData(IEnumerable<string> flags)
        {
            var result = new NarrativeFlags();
            if (flags != null)
                foreach (var f in flags)
                    if (!string.IsNullOrEmpty(f)) result._flags.Add(f);
            return result;
        }
    }
}
