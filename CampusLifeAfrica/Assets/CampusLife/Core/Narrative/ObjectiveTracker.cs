using System.Collections.Generic;

namespace CampusLife.Core.Narrative
{
    public sealed class ObjectiveDefinition
    {
        public string Id { get; }
        public string Text { get; }
        /// <summary>Flags that must be set before this objective becomes active.</summary>
        public string[] RequiresFlags { get; }
        /// <summary>When this flag is set, the objective is complete.</summary>
        public string CompletedByFlag { get; }
        /// <summary>Optional location id used for map markers and the guide arrow.</summary>
        public string TargetLocationId { get; }

        public ObjectiveDefinition(string id, string text, string completedByFlag, string targetLocationId = null, params string[] requiresFlags)
        {
            Id = id; Text = text; CompletedByFlag = completedByFlag;
            TargetLocationId = targetLocationId; RequiresFlags = requiresFlags ?? new string[0];
        }
    }

    /// <summary>
    /// Derives the current objective purely from story flags, so the objective
    /// is always consistent with saved state and never needs its own save data.
    /// </summary>
    public sealed class ObjectiveTracker
    {
        readonly List<ObjectiveDefinition> _ordered = new List<ObjectiveDefinition>();

        public ObjectiveTracker(IEnumerable<ObjectiveDefinition> objectivesInOrder)
        {
            _ordered.AddRange(objectivesInOrder);
        }

        public IReadOnlyList<ObjectiveDefinition> All => _ordered;

        public ObjectiveDefinition Current(NarrativeFlags flags)
        {
            foreach (var o in _ordered)
            {
                if (flags.Has(o.CompletedByFlag)) continue;
                bool unlocked = true;
                foreach (var req in o.RequiresFlags)
                    if (!flags.Has(req)) { unlocked = false; break; }
                if (unlocked) return o;
            }
            return null;
        }

        public int CompletedCount(NarrativeFlags flags)
        {
            int n = 0;
            foreach (var o in _ordered) if (flags.Has(o.CompletedByFlag)) n++;
            return n;
        }
    }
}
