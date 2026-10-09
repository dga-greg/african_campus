using System.Collections;
using System.Collections.Generic;
using CampusLife.Core.Save;
using CampusLife.Runtime.Infrastructure;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    /// <summary>
    /// Immediate-mode game UI: menus, character creator, HUD, dialogue,
    /// dashboard, map, settings and touch controls. Laid out on a virtual
    /// 1280×720 canvas scaled to the screen. Every button maps to working
    /// behaviour; features not built yet are simply not shown.
    /// </summary>
    public sealed partial class GameUI : MonoBehaviour, IScreenFader
    {
        const float VirtualWidth = 1280f, VirtualHeight = 720f;

        public GameBootstrap Game;

        float _scale = 1f;
        float _w = VirtualWidth, _h = VirtualHeight;
        readonly List<KeyValuePair<string, float>> _toasts = new List<KeyValuePair<string, float>>();
        string _banner;
        float _bannerUntil;
        float _fade;
        string _fadeCaption;
        float _saveIndicatorUntil;
        readonly List<Rect> _touchRects = new List<Rect>();
        readonly Dictionary<string, SaveSummary> _slotCache = new Dictionary<string, SaveSummary>();
        string _mostRecentSlot;
        float _fpsSmoothed = 60f;

        void Start()
        {
            Game.Input.IsOverTouchControl = IsOverTouchControl;
            RefreshSlots();
        }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            if (dt > 0f) _fpsSmoothed = Mathf.Lerp(_fpsSmoothed, 1f / dt, 0.05f);
            for (int i = _toasts.Count - 1; i >= 0; i--)
                if (_toasts[i].Value < Time.unscaledTime) _toasts.RemoveAt(i);
        }

        public void Toast(string message)
        {
            if (string.IsNullOrEmpty(message)) return;
            _toasts.Add(new KeyValuePair<string, float>(message, Time.unscaledTime + 4.5f));
            if (_toasts.Count > 4) _toasts.RemoveAt(0);
        }

        public void ShowAreaBanner(string name)
        {
            _banner = name;
            _bannerUntil = Time.unscaledTime + 2.5f;
        }

        public void ShowSaveIndicator()
        {
            _saveIndicatorUntil = Time.unscaledTime + 1.5f;
            RefreshSlots();
        }

        public void RefreshSlots()
        {
            _slotCache.Clear();
            foreach (var slot in Game.Saves.AllSlots())
            {
                var summary = Game.Saves.Summary(slot);
                if (summary != null) _slotCache[slot] = summary;
            }
            _mostRecentSlot = null;
            string best = null;
            foreach (var kv in _slotCache)
                if (best == null || string.CompareOrdinal(kv.Value.SavedUtc, best) > 0) { best = kv.Value.SavedUtc; _mostRecentSlot = kv.Key; }
        }

        // ------------------------------------------------------- IScreenFader

        public IEnumerator FadeOut(float seconds, string caption)
        {
            _fadeCaption = caption;
            float speed = Game.Settings.reducedMotion ? 1000f : 1f / Mathf.Max(0.01f, seconds);
            while (_fade < 1f)
            {
                _fade = Mathf.Min(1f, _fade + Time.unscaledDeltaTime * speed);
                yield return null;
            }
        }

        public IEnumerator FadeIn(float seconds)
        {
            float speed = Game.Settings.reducedMotion ? 1000f : 1f / Mathf.Max(0.01f, seconds);
            while (_fade > 0f)
            {
                _fade = Mathf.Max(0f, _fade - Time.unscaledDeltaTime * speed);
                yield return null;
            }
            _fadeCaption = null;
        }

        // ------------------------------------------------------------- OnGUI

        void OnGUI()
        {
            UiTheme.Ensure();
            _scale = Mathf.Min(Screen.width / VirtualWidth, Screen.height / VirtualHeight) * Game.Settings.uiScale;
            _scale = Mathf.Max(0.5f, _scale);
            _w = Screen.width / _scale;
            _h = Screen.height / _scale;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(_scale, _scale, 1f));
            if (Event.current.type == EventType.Repaint) _touchRects.Clear();

            if (Game.State == GameFlowState.MainMenu && Game.Overlay != Overlay.None
                && Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                Game.CloseSubMenu();
                Event.current.Use();
            }

            switch (Game.State)
            {
                case GameFlowState.MainMenu:
                    if (Game.Overlay == Overlay.Settings) DrawSettings();
                    else if (Game.Overlay == Overlay.Load) DrawLoadPanel();
                    else DrawMainMenu();
                    break;
                case GameFlowState.CharacterCreation:
                    DrawCreator();
                    break;
                case GameFlowState.Playing:
                    DrawPlaying();
                    break;
            }

            DrawToasts();
            DrawFade();
            if (Game.Settings.showFps)
                GUI.Label(new Rect(_w - 90f, _h - 28f, 84f, 24f), $"{_fpsSmoothed:0} fps", UiTheme.Small);
        }

        void DrawPlaying()
        {
            bool dialogue = Game.Dialogue.IsActive;
            switch (Game.Overlay)
            {
                case Overlay.None:
                    DrawHud(dialogue);
                    if (dialogue) DrawDialogue();
                    else if (Game.Input.TouchControlsEnabled && !Game.Areas.IsTransitioning) DrawTouchControls();
                    break;
                case Overlay.Pause: DrawPauseMenu(); break;
                case Overlay.Settings: DrawSettings(); break;
                case Overlay.Save: DrawSavePanel(); break;
                case Overlay.Load: DrawLoadPanel(); break;
                case Overlay.Dashboard: DrawDashboard(); break;
                case Overlay.Map: DrawMap(); break;
            }
        }

        // ----------------------------------------------------------- helpers

        void RegisterTouchRect(Rect r)
        {
            if (Event.current.type == EventType.Repaint) _touchRects.Add(r);
        }

        /// <summary>Screen point (bottom-left origin, pixels) → is it over an on-screen control?</summary>
        bool IsOverTouchControl(Vector2 screenPoint)
        {
            var gui = new Vector2(screenPoint.x / _scale, (Screen.height - screenPoint.y) / _scale);
            foreach (var r in _touchRects) if (r.Contains(gui)) return true;
            return false;
        }

        Vector2 ScreenToGui(Vector2 screenPoint) => new Vector2(screenPoint.x / _scale, (Screen.height - screenPoint.y) / _scale);

        static bool Button(Rect r, string text, GUIStyle style = null) => GUI.Button(r, UiTheme.Fix(text), style ?? UiTheme.Button);

        void DrawToasts()
        {
            float y = Game.State == GameFlowState.Playing ? 120f : 20f;
            for (int i = 0; i < _toasts.Count; i++)
            {
                var content = new GUIContent(UiTheme.Fix(_toasts[i].Key));
                float h = Mathf.Max(44f, UiTheme.Toast.CalcHeight(content, 380f));
                GUI.Box(new Rect(_w - 400f, y, 380f, h), content, UiTheme.Toast);
                y += h + 8f;
            }
        }

        void DrawFade()
        {
            if (_fade <= 0f) return;
            UiTheme.Fill(new Rect(0, 0, _w, _h), new Color(0.02f, 0.02f, 0.03f, _fade));
            if (!string.IsNullOrEmpty(_fadeCaption) && _fade > 0.6f)
            {
                var prev = GUI.color;
                GUI.color = new Color(1f, 1f, 1f, (_fade - 0.6f) / 0.4f);
                GUI.Label(new Rect(0, _h * 0.5f - 30f, _w, 60f), _fadeCaption, UiTheme.Center);
                GUI.color = prev;
            }
        }

        static string FormatClock(double totalMinutes)
        {
            var clock = new Core.Time.GameClock(totalMinutes);
            return clock.Format();
        }
    }
}
