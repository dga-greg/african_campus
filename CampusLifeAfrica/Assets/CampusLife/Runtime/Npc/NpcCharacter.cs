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
    /// A conversation partner placed at a post (gate, desk, kiosk). Phase 2
    /// adds schedule-driven NavMesh movement on top of this component.
    /// </summary>
    public sealed class NpcCharacter : Interactable
    {
        const float NoticeDistance = 6f;

        public NpcDefinition Definition { get; private set; }
        public DialogueGraph Dialogue { get; private set; }
        public CharacterAnimator Animator { get; private set; }

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
            Vector3 d = with.position - transform.position;
            d.y = 0f;
            if (d.sqrMagnitude > 0.001f) _targetYaw = Quaternion.LookRotation(d).eulerAngles.y;
            Animator.SetPose(CharacterPose.Talk);
        }

        public void EndConversation()
        {
            _talking = false;
            _targetYaw = _postYaw;
            Animator.SetPose(CharacterPose.Stand);
        }

        void Update()
        {
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
