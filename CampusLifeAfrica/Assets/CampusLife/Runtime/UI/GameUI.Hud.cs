using CampusLife.Core.Time;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    public sealed partial class GameUI
    {
        float _dialogueTextStart;
        object _dialogueNode;

        void DrawHud(bool inDialogue)
        {
            var session = Game.Session;
            if (session == null) return;
            var clock = session.Clock;

            // Status: clock, period and wallet.
            var status = new Rect(16f, 16f, 250f, 92f);
            GUI.Box(status, GUIContent.none, UiTheme.PanelSoft);
            GUI.Label(new Rect(30f, 22f, 230f, 28f), clock.Format(), UiTheme.Heading);
            string period = clock.Period == DayPeriod.Night ? "Night" : clock.Period.ToString();
            string where = Game.Areas.Current != null ? Game.Areas.Current.DisplayName : "";
            GUI.Label(new Rect(30f, 52f, 230f, 22f), $"{period} · {where}", UiTheme.Dim);
            GUI.Label(new Rect(30f, 74f, 230f, 26f), $"<b>{UiTheme.Cedis(session.Wallet.Balance)}</b>", UiTheme.Body);

            if (inDialogue) return;

            // Current objective with direction and distance.
            var objective = Game.CurrentObjective;
            string objText = objective != null ? objective.Text : "Free roam — explore Akwaaba Metropolitan University";
            float objW = Mathf.Min(640f, _w - 560f);
            objW = Mathf.Max(objW, 360f);
            var objRect = new Rect((_w - objW) * 0.5f, 16f, objW, 56f);
            GUI.Box(objRect, GUIContent.none, UiTheme.Pill);
            if (objective != null && Game.TryGetGuideTarget(out var target))
            {
                Vector3 to = target - Game.Player.transform.position;
                float distance = new Vector2(to.x, to.z).magnitude;
                float bearing = Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg - Game.CameraRig.Yaw;
                var arrowRect = new Rect(objRect.x + 14f, objRect.y + 12f, 32f, 32f);
                var m = GUI.matrix;
                GUIUtility.RotateAroundPivot(bearing, arrowRect.center * _scale);
                UiTheme.Tint(arrowRect, UiTheme.Arrow, UiTheme.Gold);
                GUI.matrix = m;
                GUI.Label(new Rect(objRect.x + 56f, objRect.y + 6f, objW - 140f, 46f), objText, UiTheme.Small);
                GUI.Label(new Rect(objRect.xMax - 80f, objRect.y + 16f, 70f, 24f), $"<b>{distance:0} m</b>", UiTheme.Small);
            }
            else
            {
                GUI.Label(new Rect(objRect.x + 18f, objRect.y + 6f, objW - 36f, 46f), objText, UiTheme.Small);
            }

            if (Time.unscaledTime < _bannerUntil && !string.IsNullOrEmpty(_banner))
                GUI.Label(new Rect(0f, 84f, _w, 40f), $"<size=28><b>{_banner}</b></size>", UiTheme.Center);

            // Interaction prompt.
            bool touch = Game.Input.TouchControlsEnabled;
            string prompt = null;
            if (Game.Player.IsSeated) prompt = "Stand up";
            else if (Game.Interactions.Current != null) prompt = Game.Interactions.Current.Prompt;
            if (prompt != null && !touch)
            {
                var content = new GUIContent("      " + prompt);
                float pw = UiTheme.Prompt.CalcSize(content).x + 20f;
                var pr = new Rect((_w - pw) * 0.5f, _h - 118f, pw, 48f);
                GUI.Box(pr, content, UiTheme.Prompt);
                GUI.Box(new Rect(pr.x + 14f, pr.y + 11f, 28f, 26f), "E", UiTheme.Key);
            }

            if (!touch)
                GUI.Label(new Rect(_w - 420f, _h - 34f, 410f, 26f), "<b>Tab</b> Dashboard   <b>M</b> Map   <b>Shift</b> Run   <b>Esc</b> Menu", UiTheme.Dim);

            if (Game.Settings.showSimStats) DrawSimStats();

            if (Time.unscaledTime < _saveIndicatorUntil)
                GUI.Label(new Rect(16f, _h - 34f, 200f, 26f), "Saving…", UiTheme.Dim);
        }

        void DrawSimStats()
        {
            var r = Game.Residents;
            string nav = r.IsNavigationReady(Game.Areas.Current.Id) ? "ready" : $"baking {r.NavigationProgress(Game.Areas.Current.Id) * 100f:0}%";
            string text = $"<b>Living campus</b>\nResidents: {r.Sim.States.Count} · outdoors {r.Sim.CountOutdoors()}\n" +
                          $"3D bodies: {r.ActiveCount} (high {r.HighCount}, medium {r.MediumCount}) · limit {r.MaxActive}\nNavMesh: {nav}";
            GUI.Box(new Rect(16f, 120f, 330f, 92f), text, UiTheme.Toast);
        }

        void DrawDialogue()
        {
            var dc = Game.Dialogue;
            var runner = dc.Runner;
            if (runner == null || runner.Current == null) return;
            if (!ReferenceEquals(_dialogueNode, runner.Current))
            {
                _dialogueNode = runner.Current;
                _dialogueTextStart = Time.unscaledTime;
            }

            float width = Mathf.Min(980f, _w - 32f);
            var choices = runner.AvailableChoices;
            float choiceH = 0f;
            var choiceContents = new GUIContent[choices.Count];
            for (int i = 0; i < choices.Count; i++)
            {
                choiceContents[i] = new GUIContent(UiTheme.Fix($"{i + 1}.  {runner.FormatText(choices[i].Text)}"));
                choiceH += Mathf.Max(46f, UiTheme.Choice.CalcHeight(choiceContents[i], width - 48f)) + 8f;
            }
            if (runner.CanContinue) choiceH += 54f;

            string full = UiTheme.Fix(runner.CurrentText);
            var textContent = new GUIContent(full);
            float textH = Mathf.Max(52f, UiTheme.Body.CalcHeight(textContent, width - 48f));
            float panelH = 70f + textH + 16f + choiceH + 12f;
            var panel = new Rect((_w - width) * 0.5f, _h - panelH - 16f, width, panelH);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);

            var npc = dc.Partner;
            string role = npc != null ? npc.Definition.Role : "";
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, width - 200f, 30f), $"<color=#F9C21A><b>{runner.NpcDisplayName}</b></color>  <size=15><i>{role}</i></size>", UiTheme.Heading);
            if (Button(new Rect(panel.xMax - 132f, panel.y + 14f, 112f, 36f), "Leave"))
                dc.Cancel();

            // Typewriter reveal unless reduced motion is on; clicking the text completes it.
            int visible = full.Length;
            if (!Game.Settings.reducedMotion)
                visible = Mathf.Min(full.Length, Mathf.FloorToInt((Time.unscaledTime - _dialogueTextStart) * 55f));
            var textRect = new Rect(panel.x + 24f, panel.y + 58f, width - 48f, textH);
            GUI.Label(textRect, visible < full.Length ? full.Substring(0, visible) : full, UiTheme.Body);
            if (visible < full.Length && Event.current.type == EventType.MouseDown && textRect.Contains(Event.current.mousePosition))
            {
                _dialogueTextStart = -999f;
                Event.current.Use();
            }

            float y = textRect.yMax + 16f;
            for (int i = 0; i < choices.Count; i++)
            {
                float h = Mathf.Max(46f, UiTheme.Choice.CalcHeight(choiceContents[i], width - 48f));
                if (GUI.Button(new Rect(panel.x + 24f, y, width - 48f, h), choiceContents[i], UiTheme.Choice))
                {
                    dc.Choose(i);
                    return;
                }
                y += h + 8f;
            }
            if (runner.CanContinue && Button(new Rect(panel.x + 24f, y, 220f, 46f), "Continue  (Space)", UiTheme.ButtonPrimary))
                dc.Continue();
        }

        void DrawTouchControls()
        {
            var input = Game.Input;
            // Floating joystick.
            if (input.JoystickActive)
            {
                float r = input.JoystickRadius / _scale;
                var c = ScreenToGui(input.JoystickCenter);
                var k = ScreenToGui(input.JoystickKnob);
                UiTheme.Tint(new Rect(c.x - r, c.y - r, r * 2f, r * 2f), UiTheme.Ring, new Color(1f, 1f, 1f, 0.45f));
                UiTheme.Tint(new Rect(k.x - r * 0.45f, k.y - r * 0.45f, r * 0.9f, r * 0.9f), UiTheme.Circle, new Color(1f, 1f, 1f, 0.55f));
            }
            else
            {
                GUI.Label(new Rect(40f, _h - 60f, 300f, 30f), "Drag here to move", UiTheme.Dim);
            }

            // Contextual interact button.
            string prompt = Game.Player.IsSeated ? "Stand up" : Game.Interactions.Current != null ? Game.Interactions.Current.Prompt : null;
            if (prompt != null)
            {
                var r = new Rect(_w - 300f, _h - 200f, 270f, 76f);
                RegisterTouchRect(r);
                if (Button(r, prompt, UiTheme.ButtonPrimary)) input.QueueInteract();
            }

            var run = new Rect(_w - 150f, _h - 108f, 120f, 76f);
            RegisterTouchRect(run);
            if (Button(run, input.TouchRunToggled ? "Running" : "Walk", input.TouchRunToggled ? UiTheme.ButtonSelected : UiTheme.Button))
                input.TouchRunToggled = !input.TouchRunToggled;

            float bx = _w - 372f;
            var menu = new Rect(bx + 252f, 16f, 104f, 56f);
            var map = new Rect(bx + 136f, 16f, 104f, 56f);
            var dash = new Rect(bx, 16f, 124f, 56f);
            RegisterTouchRect(menu);
            RegisterTouchRect(map);
            RegisterTouchRect(dash);
            if (Button(menu, "Menu")) input.QueuePause();
            if (Button(map, "Map")) input.QueueMap();
            if (Button(dash, "Student")) input.QueueDashboard();
        }
    }
}
