using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>A bench or chair the player can sit on. Facing direction is the seat's +Z.</summary>
    public sealed class SeatInteractable : Interactable
    {
        public Transform SitPose;
        public Transform StandPose;
        public float SeatHeight = 0.48f;
        /// <summary>Room the seat belongs to ("LT1", "LT2", "library"), used for lectures, exams and study.</summary>
        public string RoomId;
        public bool Occupied { get; set; }

        public override string Prompt => "Sit down";

        public override Vector3 InteractionPoint => StandPose != null ? StandPose.position : transform.position;

        public override bool CanInteract(PlayerController player) => base.CanInteract(player) && !Occupied;

        public override void Interact(PlayerController player) => player.SitOn(this);

        /// <summary>Creates a seat at local position/yaw under <paramref name="parent"/>.</summary>
        public static SeatInteractable Create(Transform parent, Vector3 localPos, float yaw, float seatHeight, float standOffset = 0.75f)
        {
            var go = new GameObject("Seat");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            var seat = go.AddComponent<SeatInteractable>();
            seat.SeatHeight = seatHeight;
            seat.Range = 1.8f;
            seat.SitPose = new GameObject("SitPose").transform;
            seat.SitPose.SetParent(go.transform, false);
            seat.SitPose.localPosition = new Vector3(0f, 0f, -0.05f);
            seat.StandPose = new GameObject("StandPose").transform;
            seat.StandPose.SetParent(go.transform, false);
            // Positive offsets stand up in front of the seat, negative ones behind it.
            seat.StandPose.localPosition = new Vector3(0f, 0.05f, standOffset);
            if (standOffset < 0f) seat.StandPose.localRotation = Quaternion.Euler(0f, 180f, 0f);
            return seat;
        }
    }
}
