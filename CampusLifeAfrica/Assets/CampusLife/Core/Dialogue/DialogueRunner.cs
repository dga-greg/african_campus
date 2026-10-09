using System;
using System.Collections.Generic;
using CampusLife.Core.Economy;
using CampusLife.Core.Player;
using CampusLife.Core.Session;

namespace CampusLife.Core.Dialogue
{
    /// <summary>
    /// Plays a <see cref="DialogueGraph"/> against a <see cref="GameSession"/>.
    /// Deterministic: the same state and choices always produce the same result.
    /// </summary>
    public sealed class DialogueRunner
    {
        readonly GameSession _session;
        readonly List<DialogueChoice> _available = new List<DialogueChoice>();
        readonly List<string> _feedback = new List<string>();

        public DialogueGraph Graph { get; }
        public string NpcId => Graph.NpcId;
        public string NpcDisplayName { get; }
        public DialogueNode Current { get; private set; }
        public bool IsFinished => Current == null;

        /// <summary>Choices whose conditions pass at the current node.</summary>
        public IReadOnlyList<DialogueChoice> AvailableChoices => _available;

        /// <summary>Player-facing notes produced by the last step ("Bought …", "Esi will remember that.").</summary>
        public IReadOnlyList<string> Feedback => _feedback;

        public event Action<DialogueRunner> Ended;

        public DialogueRunner(DialogueGraph graph, GameSession session, string npcDisplayName)
        {
            Graph = graph ?? throw new ArgumentNullException(nameof(graph));
            _session = session ?? throw new ArgumentNullException(nameof(session));
            NpcDisplayName = npcDisplayName ?? graph.NpcId;
        }

        public void Start()
        {
            if (Current != null) throw new InvalidOperationException("Dialogue already started.");
            _session.Relationships.Get(NpcId).RecordConversation(_session.Clock.TotalMinutes);
            string start = Graph.DefaultNodeId;
            foreach (var entry in Graph.Entries)
            {
                if (AllPass(entry.Conditions)) { start = entry.NodeId; break; }
            }
            _feedback.Clear();
            Enter(start);
        }

        /// <summary>Text for the current node with {player}, {npc} and {program} filled in.</summary>
        public string CurrentText => Current == null ? string.Empty : FormatText(Current.Text);

        public string FormatText(string text)
        {
            var program = AcademicPrograms.Find(_session.Profile.programId);
            return text
                .Replace("{player}", _session.Profile.displayName)
                .Replace("{npc}", NpcDisplayName)
                .Replace("{program}", program != null ? program.Name : _session.Profile.programId);
        }

        /// <summary>True when the current node offers no selectable choice and can only be continued.</summary>
        public bool CanContinue => Current != null && _available.Count == 0;

        /// <summary>
        /// Advances a node without selectable choices: follows its auto-advance
        /// target, or ends the conversation if every choice is unavailable.
        /// </summary>
        public void Continue()
        {
            if (Current == null) return;
            if (_available.Count > 0) throw new InvalidOperationException("Current node requires a choice.");
            _feedback.Clear();
            Enter(Current.Choices.Count == 0 ? Current.NextNodeId : null);
        }

        public void Choose(int availableIndex)
        {
            if (Current == null) throw new InvalidOperationException("Dialogue has ended.");
            if (availableIndex < 0 || availableIndex >= _available.Count) throw new ArgumentOutOfRangeException(nameof(availableIndex));
            var choice = _available[availableIndex];
            _feedback.Clear();
            foreach (var effect in choice.Effects) Apply(effect);
            Enter(choice.NextNodeId);
        }

        /// <summary>Ends the conversation early (player walked away / pressed Esc).</summary>
        public void Abort()
        {
            if (Current == null) return;
            Current = null;
            _available.Clear();
            Ended?.Invoke(this);
        }

        void Enter(string nodeId)
        {
            _available.Clear();
            if (nodeId == null)
            {
                Current = null;
                Ended?.Invoke(this);
                return;
            }
            Current = Graph.GetNode(nodeId);
            foreach (var effect in Current.OnEnter) Apply(effect);
            foreach (var c in Current.Choices)
                if (AllPass(c.Conditions)) _available.Add(c);
        }

        bool AllPass(IReadOnlyList<DialogueCondition> conditions)
        {
            foreach (var c in conditions) if (!Evaluate(c)) return false;
            return true;
        }

        public bool Evaluate(DialogueCondition c)
        {
            var rel = _session.Relationships.Get(NpcId);
            switch (c.Type)
            {
                case ConditionType.FlagSet: return _session.Flags.Has(c.Key);
                case ConditionType.FlagNotSet: return !_session.Flags.Has(c.Key);
                case ConditionType.AffinityAtLeast: return rel.Affinity >= c.Value;
                case ConditionType.AffinityBelow: return rel.Affinity < c.Value;
                case ConditionType.Remembers: return rel.Remembers(c.Key);
                case ConditionType.DoesNotRemember: return !rel.Remembers(c.Key);
                case ConditionType.CanAfford: return _session.Wallet.CanAfford(c.Value);
                case ConditionType.HasItem: return _session.Inventory.Has(c.Key);
                case ConditionType.LacksItem: return !_session.Inventory.Has(c.Key);
                default: throw new ArgumentOutOfRangeException(nameof(c), c.Type, "Unknown condition type.");
            }
        }

        void Apply(DialogueEffect e)
        {
            var rel = _session.Relationships.Get(NpcId);
            double now = _session.Clock.TotalMinutes;
            switch (e.Type)
            {
                case EffectType.SetFlag: _session.Flags.Set(e.Key); break;
                case EffectType.ClearFlag: _session.Flags.Clear(e.Key); break;
                case EffectType.ChangeAffinity: rel.ChangeAffinity((int)e.Value); break;
                case EffectType.ChangeTrust: rel.ChangeTrust((int)e.Value); break;
                case EffectType.Remember:
                    if (rel.Remember(e.Key, e.MemoryKind, now)) _feedback.Add($"{NpcDisplayName} will remember that.");
                    break;
                case EffectType.GiveItem:
                {
                    int count = (int)Math.Max(1, e.Value);
                    _session.Inventory.Add(e.Key, count);
                    _feedback.Add($"Received: {ItemName(e.Key)}" + (count > 1 ? $" x{count}" : string.Empty));
                    break;
                }
                case EffectType.Purchase:
                {
                    int count = (int)Math.Max(1, e.Value);
                    string txId = _session.NextTransactionId("buy:" + e.Key);
                    var result = Commerce.TryPurchase(_session.Wallet, _session.Inventory, _session.Items, e.Key, count, txId, now);
                    if (result == PurchaseResult.Purchased)
                    {
                        long price = _session.Items.Get(e.Key).PricePesewas * count;
                        _feedback.Add($"Bought {ItemName(e.Key)} (-{Money.Format(price)})");
                    }
                    else
                    {
                        _feedback.Add(result == PurchaseResult.InsufficientFunds ? "Not enough money." : $"Purchase failed: {result}.");
                    }
                    break;
                }
                default: throw new ArgumentOutOfRangeException(nameof(e), e.Type, "Unknown effect type.");
            }
        }

        string ItemName(string itemId) => _session.Items.TryGet(itemId, out var item) ? item.DisplayName : itemId;
    }
}
