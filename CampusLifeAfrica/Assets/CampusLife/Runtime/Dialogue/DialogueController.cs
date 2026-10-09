using System;
using CampusLife.Core.Dialogue;
using CampusLife.Core.Session;
using CampusLife.Runtime.CameraSystem;
using CampusLife.Runtime.Npc;
using CampusLife.Runtime.Player;
using UnityEngine;

namespace CampusLife.Runtime.Dialogue
{
    /// <summary>
    /// Bridges the deterministic <see cref="DialogueRunner"/> with the scene:
    /// pauses the clock, locks movement, frames the camera and routes choice
    /// input. The UI only reads state and calls <see cref="Choose"/>/<see cref="Continue"/>.
    /// </summary>
    public sealed class DialogueController : MonoBehaviour
    {
        public const string PauseReason = "dialogue";

        public static DialogueController Instance { get; private set; }

        public Func<GameSession> Session { get; set; }
        public ThirdPersonCameraRig CameraRig { get; set; }

        public DialogueRunner Runner { get; private set; }
        public NpcCharacter Partner { get; private set; }
        PlayerController _player;
        int _startedFrame;

        public bool IsActive => Runner != null && !Runner.IsFinished;

        /// <summary>Frame a conversation last ended on, so the same key press is not reused (e.g. Esc opening the pause menu).</summary>
        public int LastEndedFrame { get; private set; } = -1;

        public event Action<DialogueController> Started;
        public event Action<DialogueController> Ended;
        /// <summary>Player-facing feedback from effects ("Bought …", "Esi will remember that.").</summary>
        public event Action<string> Feedback;

        void Awake() => Instance = this;

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Begin(NpcCharacter npc, PlayerController player)
        {
            if (IsActive || npc == null) return;
            var session = Session?.Invoke();
            if (session == null) return;

            Partner = npc;
            _player = player;
            _startedFrame = Time.frameCount;
            Runner = new DialogueRunner(npc.Dialogue, session, npc.Definition.DisplayName, npc.Definition.Id);
            npc.ConfigureRunner?.Invoke(Runner);
            session.Clock.Pause(PauseReason);
            player.ControlEnabled = false;
            player.FaceTowards(npc.transform.position);
            npc.BeginConversation(player.transform);
            if (CameraRig != null) CameraRig.FrameConversation(player.transform, npc.transform);

            Runner.Start();
            Started?.Invoke(this);
            PublishFeedback();
            if (Runner.IsFinished) Finish();
        }

        public void Choose(int availableIndex)
        {
            if (!IsActive || availableIndex < 0 || availableIndex >= Runner.AvailableChoices.Count) return;
            Runner.Choose(availableIndex);
            PublishFeedback();
            if (Runner.IsFinished) Finish();
        }

        public void Continue()
        {
            if (!IsActive || !Runner.CanContinue) return;
            Runner.Continue();
            PublishFeedback();
            if (Runner.IsFinished) Finish();
        }

        /// <summary>Leaves the conversation without choosing (Esc / back button).</summary>
        public void Cancel()
        {
            if (!IsActive) return;
            Runner.Abort();
            Finish();
        }

        void Update()
        {
            if (!IsActive || _player == null || Time.frameCount == _startedFrame) return;
            var input = _player.Input;
            if (input.ChoicePressed >= 0) Choose(input.ChoicePressed);
            else if (input.ContinuePressed && Runner.CanContinue) Continue();
            else if (input.PausePressed) Cancel();
        }

        void PublishFeedback()
        {
            foreach (var line in Runner.Feedback) Feedback?.Invoke(line);
        }

        void Finish()
        {
            var session = Session?.Invoke();
            session?.Clock.Resume(PauseReason);
            if (Partner != null) Partner.EndConversation();
            if (CameraRig != null) CameraRig.SetFollow();
            if (_player != null) _player.ControlEnabled = true;
            LastEndedFrame = Time.frameCount;
            Ended?.Invoke(this);
            Partner = null;
        }
    }
}
