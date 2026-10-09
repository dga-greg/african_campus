using CampusLife.Core.Content;
using CampusLife.Core.Dialogue;
using CampusLife.Runtime.Characters;
using CampusLife.Runtime.Dialogue;
using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.Npc
{
    /// <summary>
    /// Makes a character talkable. Story NPCs stand at a post and this
    /// component turns them toward the player; autonomous residents set
    /// <see cref="ControlsBody"/> to false and let <see cref="ResidentAgent"/>
    /// handle movement, reacting to the conversation events instead.
    /// </summary>
    public sealed class NpcCharacter : Interactable
    {
        const float NoticeDistance = 6f;

        public NpcDefinition Definition { get; private set; }
        public DialogueGraph Dialogue { get; private set; }
        public CharacterAnimator Animator { get; private set; }

        /// <summary>False when another component (a resident's brain) moves and poses this character.</summary>
        public bool ControlsBody { get; set; } = true;

        /// <summary>Lets the owner fill dialogue variables (what the NPC is doing now, etc.).</summary>
        public System.Action<DialogueRunner> ConfigureRunner;
        public event System.Action<NpcCharacter, Transform> ConversationStarted;
        public event System.Action<NpcCharacter> ConversationEnded;

        public bool IsTalking => _talking;

        float _postYaw;
        float _targetYaw;
        Transform _player;
        bool _talking;

        public override string Prompt => $"Talk to {Definition.DisplayName}";

        public override Vector3 InteractionPoint => transform.position + Vector3.up * 0.9f;

        public void Initialise(NpcDefinition definition, DialogueGraph dialogue, CharacterAnimator animator, Transform player)
        {
            Definition = definition;
            Dialogue = dialogue;
            Animator = animator;
            _player = player;
            _postYaw = _targetYaw = transform.eulerAngles.y;
            Range = 2.6f;
        }

        public override bool CanInteract(PlayerController player)
            => base.CanInteract(player) && !_talking && DialogueController.Instance != null && !DialogueController.Instance.IsActive;

        public override void Interact(PlayerController player) => DialogueController.Instance.Begin(this, player);

        public void BeginConversation(Transform with)
        {
            _talking = true;
            if (ControlsBody)
            {
                Vector3 d = with.position - transform.position;
                d.y = 0f;
                if (d.sqrMagnitude > 0.001f) _targetYaw = Quaternion.LookRotation(d).eulerAngles.y;
                Animator.SetPose(CharacterPose.Talk);
            }
            ConversationStarted?.Invoke(this, with);
        }

        public void EndConversation()
        {
            _talking = false;
            if (ControlsBody)
            {
                _targetYaw = _postYaw;
                Animator.SetPose(CharacterPose.Stand);
            }
            ConversationEnded?.Invoke(this);
        }

        void Update()
        {
            if (!ControlsBody)
            {
                if (_talking && Animator != null && _player != null) Animator.LookAt(_player.position + Vector3.up * 1.6f);
                return;
            }
            float yaw = Mathf.MoveTowardsAngle(transform.eulerAngles.y, _targetYaw, 240f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            if (Animator == null) return;
            Animator.SetSpeed(0f);
            if (_player != null && (_talking || (_player.position - transform.position).sqrMagnitude < NoticeDistance * NoticeDistance))
                Animator.LookAt(_player.position + Vector3.up * 1.6f);
            else
                Animator.LookAt(null);
        }
    }
}
