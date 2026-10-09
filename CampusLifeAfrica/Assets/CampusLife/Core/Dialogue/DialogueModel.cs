using System;
using System.Collections.Generic;
using CampusLife.Core.Social;

namespace CampusLife.Core.Dialogue
{
    public enum ConditionType
    {
        FlagSet, FlagNotSet,
        AffinityAtLeast, AffinityBelow,
        Remembers, DoesNotRemember,
        CanAfford, HasItem, LacksItem,
    }

    public enum EffectType
    {
        SetFlag, ClearFlag,
        ChangeAffinity, ChangeTrust,
        Remember,
        GiveItem,
        Purchase,
    }

    /// <summary>Data-only condition evaluated against the current game session and NPC.</summary>
    public sealed class DialogueCondition
    {
        public ConditionType Type { get; }
        public string Key { get; }
        public long Value { get; }

        public DialogueCondition(ConditionType type, string key = null, long value = 0) { Type = type; Key = key; Value = value; }

        public static DialogueCondition Flag(string flag) => new DialogueCondition(ConditionType.FlagSet, flag);
        public static DialogueCondition NoFlag(string flag) => new DialogueCondition(ConditionType.FlagNotSet, flag);
        public static DialogueCondition Remembers(string key) => new DialogueCondition(ConditionType.Remembers, key);
        public static DialogueCondition Forgot(string key) => new DialogueCondition(ConditionType.DoesNotRemember, key);
        public static DialogueCondition Afford(long pesewas) => new DialogueCondition(ConditionType.CanAfford, null, pesewas);
        public static DialogueCondition Item(string itemId) => new DialogueCondition(ConditionType.HasItem, itemId);
        public static DialogueCondition NoItem(string itemId) => new DialogueCondition(ConditionType.LacksItem, itemId);
        public static DialogueCondition AffinityAtLeast(int value) => new DialogueCondition(ConditionType.AffinityAtLeast, null, value);
    }

    /// <summary>
    /// Data-only effect. Effects are the only way dialogue changes game state,
    /// which keeps consequences deterministic and auditable (and means any
    /// future generated dialogue can be shown without being able to change state).
    /// </summary>
    public sealed class DialogueEffect
    {
        public EffectType Type { get; }
        public string Key { get; }
        public long Value { get; }
        public MemoryKind MemoryKind { get; }

        public DialogueEffect(EffectType type, string key = null, long value = 0, MemoryKind memoryKind = MemoryKind.Conversation)
        { Type = type; Key = key; Value = value; MemoryKind = memoryKind; }

        public static DialogueEffect SetFlag(string flag) => new DialogueEffect(EffectType.SetFlag, flag);
        public static DialogueEffect ClearFlag(string flag) => new DialogueEffect(EffectType.ClearFlag, flag);
        public static DialogueEffect Affinity(int delta) => new DialogueEffect(EffectType.ChangeAffinity, null, delta);
        public static DialogueEffect Trust(int delta) => new DialogueEffect(EffectType.ChangeTrust, null, delta);
        public static DialogueEffect Remember(string key, MemoryKind kind = MemoryKind.Conversation) => new DialogueEffect(EffectType.Remember, key, 0, kind);
        public static DialogueEffect Give(string itemId, int count = 1) => new DialogueEffect(EffectType.GiveItem, itemId, count);
        public static DialogueEffect Buy(string itemId, int count = 1) => new DialogueEffect(EffectType.Purchase, itemId, count);
    }

    public sealed class DialogueChoice
    {
        public string Text { get; }
        /// <summary>Next node id, or null to end the conversation.</summary>
        public string NextNodeId { get; }
        public IReadOnlyList<DialogueCondition> Conditions { get; }
        public IReadOnlyList<DialogueEffect> Effects { get; }

        public DialogueChoice(string text, string nextNodeId, DialogueCondition[] conditions = null, DialogueEffect[] effects = null)
        {
            Text = text; NextNodeId = nextNodeId;
            Conditions = conditions ?? Array.Empty<DialogueCondition>();
            Effects = effects ?? Array.Empty<DialogueEffect>();
        }
    }

    public sealed class DialogueNode
    {
        public string Id { get; }
        /// <summary>"npc" for the conversation partner or "player".</summary>
        public string Speaker { get; }
        public string Text { get; }
        public IReadOnlyList<DialogueChoice> Choices { get; }
        public IReadOnlyList<DialogueEffect> OnEnter { get; }
        /// <summary>Used only when the node has no choices; null ends the conversation.</summary>
        public string NextNodeId { get; }

        public DialogueNode(string id, string text, DialogueChoice[] choices = null, DialogueEffect[] onEnter = null,
            string nextNodeId = null, string speaker = "npc")
        {
            Id = id; Text = text; Speaker = speaker; NextNodeId = nextNodeId;
            Choices = choices ?? Array.Empty<DialogueChoice>();
            OnEnter = onEnter ?? Array.Empty<DialogueEffect>();
        }
    }

    /// <summary>An entry rule picks the opening node from game state (e.g. a returning visitor).</summary>
    public sealed class DialogueEntry
    {
        public string NodeId { get; }
        public IReadOnlyList<DialogueCondition> Conditions { get; }

        public DialogueEntry(string nodeId, params DialogueCondition[] conditions)
        {
            NodeId = nodeId;
            Conditions = conditions ?? Array.Empty<DialogueCondition>();
        }
    }

    public sealed class DialogueGraph
    {
        readonly Dictionary<string, DialogueNode> _nodes = new Dictionary<string, DialogueNode>(StringComparer.Ordinal);
        readonly List<DialogueEntry> _entries = new List<DialogueEntry>();

        public string Id { get; }
        public string NpcId { get; }
        public string DefaultNodeId { get; }
        public IReadOnlyList<DialogueEntry> Entries => _entries;
        public IEnumerable<DialogueNode> Nodes => _nodes.Values;

        public DialogueGraph(string id, string npcId, string defaultNodeId)
        {
            Id = id; NpcId = npcId; DefaultNodeId = defaultNodeId;
        }

        public DialogueGraph Node(DialogueNode node)
        {
            if (_nodes.ContainsKey(node.Id)) throw new ArgumentException($"Dialogue '{Id}' already has node '{node.Id}'.");
            _nodes.Add(node.Id, node);
            return this;
        }

        public DialogueGraph Entry(DialogueEntry entry)
        {
            _entries.Add(entry);
            return this;
        }

        public bool TryGetNode(string id, out DialogueNode node) => _nodes.TryGetValue(id ?? string.Empty, out node);

        public DialogueNode GetNode(string id)
        {
            if (!TryGetNode(id, out var node)) throw new KeyNotFoundException($"Dialogue '{Id}' has no node '{id}'.");
            return node;
        }

        /// <summary>Returns human-readable structural problems; empty when the graph is valid.</summary>
        public List<string> Validate()
        {
            var errors = new List<string>();
            if (!_nodes.ContainsKey(DefaultNodeId ?? string.Empty)) errors.Add($"{Id}: default node '{DefaultNodeId}' is missing.");
            foreach (var e in _entries)
                if (!_nodes.ContainsKey(e.NodeId ?? string.Empty)) errors.Add($"{Id}: entry references missing node '{e.NodeId}'.");
            foreach (var n in _nodes.Values)
            {
                if (string.IsNullOrWhiteSpace(n.Text)) errors.Add($"{Id}/{n.Id}: empty text.");
                if (n.Choices.Count > 0 && n.NextNodeId != null) errors.Add($"{Id}/{n.Id}: has both choices and an auto-advance target.");
                if (n.NextNodeId != null && !_nodes.ContainsKey(n.NextNodeId)) errors.Add($"{Id}/{n.Id}: next node '{n.NextNodeId}' is missing.");
                for (int i = 0; i < n.Choices.Count; i++)
                {
                    var c = n.Choices[i];
                    if (string.IsNullOrWhiteSpace(c.Text)) errors.Add($"{Id}/{n.Id}: choice {i} has no text.");
                    if (c.NextNodeId != null && !_nodes.ContainsKey(c.NextNodeId)) errors.Add($"{Id}/{n.Id}: choice {i} targets missing node '{c.NextNodeId}'.");
                }
            }
            // Unreachable nodes usually indicate a typo in an id.
            var reachable = new HashSet<string>(StringComparer.Ordinal);
            var stack = new Stack<string>();
            if (DefaultNodeId != null) stack.Push(DefaultNodeId);
            foreach (var e in _entries) if (e.NodeId != null) stack.Push(e.NodeId);
            while (stack.Count > 0)
            {
                var id = stack.Pop();
                if (!reachable.Add(id) || !_nodes.TryGetValue(id, out var node)) continue;
                if (node.NextNodeId != null) stack.Push(node.NextNodeId);
                foreach (var c in node.Choices) if (c.NextNodeId != null) stack.Push(c.NextNodeId);
            }
            foreach (var id in _nodes.Keys)
                if (!reachable.Contains(id)) errors.Add($"{Id}/{id}: node is unreachable.");
            return errors;
        }
    }
}
