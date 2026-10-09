using CampusLife.Core.Simulation;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.Interaction
{
    /// <summary>Finds the best interactable near the player and triggers it on the interact input.</summary>
    [RequireComponent(typeof(PlayerController))]
    public sealed class InteractionDetector : MonoBehaviour
    {
        const float VerticalTolerance = 2.2f;

        PlayerController _player;

        public Interactable Current { get; private set; }

        void Awake() => _player = GetComponent<PlayerController>();

        void Update()
        {
            Current = _player.ControlEnabled && !_player.IsSeated ? FindBest() : null;
            if (_player.IsSeated) return; // seated players stand up through PlayerController
            if (Current != null && _player.ControlEnabled && Time.frameCount > _player.ControlEnabledFrame && _player.Input.InteractPressed)
                Current.Interact(_player);
        }

        Interactable FindBest()
        {
            Vector3 origin = transform.position;
            Vector3 forward = transform.forward;
            forward.y = 0f;
            Interactable best = null;
            float bestScore = 0f;
            var list = Interactable.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var candidate = list[i];
                if (candidate == null || !candidate.CanInteract(_player)) continue;
                Vector3 to = candidate.InteractionPoint - origin;
                if (Mathf.Abs(to.y) > VerticalTolerance) continue;
                to.y = 0f;
                float distance = to.magnitude;
                float angle = distance > 0.001f ? Vector3.Angle(forward, to) : 0f;
                float score = InteractionScoring.Score(distance, candidate.Range, angle);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = candidate;
                }
            }
            return best;
        }
    }
}
