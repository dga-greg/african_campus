using System.Collections.Generic;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.Interaction
{
    /// <summary>
    /// Base for anything the player can use with the interact key. Active
    /// interactables register themselves, so detection needs no physics layers
    /// and inactive areas (disabled interiors) are excluded automatically.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        static readonly List<Interactable> ActiveList = new List<Interactable>();
        public static IReadOnlyList<Interactable> Active => ActiveList;

        // Supports "Enter Play Mode" without domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ActiveList.Clear();

        [Tooltip("Maximum planar distance from the player to the interaction point.")]
        public float Range = 2.2f;

        /// <summary>Verb phrase shown in the prompt, e.g. "Talk to Esi".</summary>
        public abstract string Prompt { get; }

        public virtual Vector3 InteractionPoint => transform.position;

        public virtual bool CanInteract(PlayerController player) => isActiveAndEnabled;

        public abstract void Interact(PlayerController player);

        protected virtual void OnEnable() => ActiveList.Add(this);
        protected virtual void OnDisable() => ActiveList.Remove(this);
    }
}
