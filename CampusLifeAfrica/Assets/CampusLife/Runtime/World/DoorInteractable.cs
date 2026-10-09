using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>Door that moves the player to another area.</summary>
    public sealed class DoorInteractable : Interactable
    {
        public string TargetAreaId;
        public string TargetSpawnId;
        public string Label = "Enter";

        public override string Prompt => Label;

        public override bool CanInteract(PlayerController player)
            => base.CanInteract(player) && AreaManager.Instance != null && !AreaManager.Instance.IsTransitioning;

        public override void Interact(PlayerController player)
        {
            AreaManager.Instance.RequestTransition(TargetAreaId, TargetSpawnId);
        }
    }
}
