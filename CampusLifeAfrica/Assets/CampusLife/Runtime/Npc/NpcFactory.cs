using System.Collections.Generic;
using CampusLife.Core.Content;
using CampusLife.Core.Dialogue;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.Npc
{
    /// <summary>Spawns content-defined NPCs with their visual rig, animator and dialogue.</summary>
    public sealed class NpcFactory
    {
        const int IgnoreRaycastLayer = 2;

        readonly MaterialLibrary _materials;
        readonly Dictionary<string, NpcDefinition> _npcs = new Dictionary<string, NpcDefinition>();
        readonly Dictionary<string, DialogueGraph> _dialogues = new Dictionary<string, DialogueGraph>();
        readonly List<NpcCharacter> _spawned = new List<NpcCharacter>();

        public Transform Player { get; set; }
        public IReadOnlyList<NpcCharacter> Spawned => _spawned;

        public NpcFactory(MaterialLibrary materials, IEnumerable<NpcDefinition> npcs, IEnumerable<DialogueGraph> dialogues)
        {
            _materials = materials;
            foreach (var n in npcs) _npcs.Add(n.Id, n);
            foreach (var d in dialogues) _dialogues.Add(d.Id, d);
        }

        public bool TryGetDefinition(string npcId, out NpcDefinition def) => _npcs.TryGetValue(npcId, out def);

        public NpcCharacter Spawn(string npcId, Transform parent, Vector3 localPosition, float yaw)
        {
            var def = _npcs[npcId];
            var go = new GameObject("NPC_" + def.DisplayName);
            go.layer = IgnoreRaycastLayer;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);

            var body = go.AddComponent<CapsuleCollider>();
            body.center = new Vector3(0f, 0.9f, 0f);
            body.height = 1.8f;
            body.radius = 0.3f;

            var visual = CharacterRigBuilder.Build(go.transform, def.Appearance, _materials);
            var animator = go.AddComponent<CharacterAnimator>();
            animator.Visual = visual;

            var npc = go.AddComponent<NpcCharacter>();
            npc.Initialise(def, _dialogues[def.DialogueId], animator, Player);
            _spawned.Add(npc);
            return npc;
        }
    }
}
