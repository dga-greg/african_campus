using System;
using CampusLife.Runtime.Interaction;
using CampusLife.Runtime.Player;

namespace CampusLife.Runtime.World
{
    /// <summary>A notice board or sign the player can read; the text appears as an on-screen message.</summary>
    public sealed class InfoInteractable : Interactable
    {
        public static event Action<string> Shown;

        public string Label = "Read notice";
        public string Message;
        /// <summary>Optional live text (e.g. the player's own timetable); overrides <see cref="Message"/>.</summary>
        public Func<string> MessageProvider;

        public override string Prompt => Label;

        public override void Interact(PlayerController player) => Shown?.Invoke(MessageProvider != null ? MessageProvider() : Message);
    }
}
