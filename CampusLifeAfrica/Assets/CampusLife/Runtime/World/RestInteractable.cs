using System;
using CampusLife.Core.Time;
using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Player;

namespace CampusLife.Runtime.World
{
    /// <summary>Unity Hall entrance: go up to your room to sleep (at night) or rest for two hours.</summary>
    public sealed class RestInteractable : Interactable
    {
        public static event Action RestRequested;

        /// <summary>Supplies the current game time so the prompt can say "Sleep" or "Rest".</summary>
        public static Func<double> Now;

        public override string Prompt
            => Now != null && RestRules.IsNightSleep(Now()) ? "Go to your room and sleep until morning" : "Go to your room and rest for two hours";

        public override void Interact(PlayerController player) => RestRequested?.Invoke();
    }
}
