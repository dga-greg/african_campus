using System;
using CampusLife.Core.Player;
using CampusLife.Runtime.Infrastructure;
using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    public sealed partial class GameUI
    {
        string _creatorError;
        Vector2 _creatorScroll;
        string _pendingDeleteSlot;
        string _pendingOverwriteSlot;
        bool _confirmMainMenu;

        // --------------------------------------------------------- main menu

        void DrawMainMenu()
        {
            var panel = new Rect(48f, 0f, 460f, _h);
            UiTheme.Fill(panel, new Color(0.04f, 0.05f, 0.07f, 0.78f));
            UiTheme.Fill(new Rect(panel.xMax, 0f, 6f, _h * 0.33f), UiTheme.Gold);
            UiTheme.Fill(new Rect(panel.xMax, _h * 0.33f, 6f, _h * 0.34f), UiTheme.Green);
            UiTheme.Fill(new Rect(panel.xMax, _h * 0.67f, 6f, _h * 0.33f), UiTheme.Red);

            float x = panel.x + 40f, w = panel.width - 80f, y = Mathf.Max(60f, _h * 0.16f);
            GUI.Label(new Rect(x, y, w, 60f), "Campus Life Africa", UiTheme.Title);
            y += 62f;
            GUI.Label(new Rect(x, y, w, 50f), "Akwaaba Metropolitan University · Ghana", UiTheme.Dim);
            y += 64f;

            if (_mostRecentSlot != null && _slotCache.TryGetValue(_mostRecentSlot, out var recent))
            {
                if (Button(new Rect(x, y, w, 56f), $"Continue  <size=14>({recent.PlayerName} · {FormatClock(recent.ClockMinutes)})</size>", UiTheme.ButtonPrimary))
                {
                    if (!Game.TryLoad(_mostRecentSlot, out var message)) RefreshSlots();
                    Toast(message);
                }
                y += 66f;
            }
            if (Button(new Rect(x, y, w, 52f), "New Game", _mostRecentSlot == null ? UiTheme.ButtonPrimary : UiTheme.Button))
            {
                _creatorError = null;
                Game.BeginCharacterCreation();
            }
            y += 62f;
            if (_slotCache.Count > 0)
            {
                if (Button(new Rect(x, y, w, 52f), "Load Game")) { RefreshSlots(); Game.OpenOverlay(Overlay.Load); }
                y += 62f;
            }
            if (Button(new Rect(x, y, w, 52f), "Settings")) Game.OpenOverlay(Overlay.Settings);
            y += 62f;
            if (GameBootstrap.CanQuitApplication && Button(new Rect(x, y, w, 52f), "Quit")) Application.Quit();

            GUI.Label(new Rect(x, _h - 60f, w, 44f), $"v{GameBootstrap.GameVersion} · Phase 3 university build · Original fictional university", UiTheme.Dim);
        }

        // ------------------------------------------------- character creator

        void DrawCreator()
        {
            var p = Game.DraftProfile;
            var a = p.appearance;
            var panel = new Rect(24f, 24f, 520f, _h - 48f);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            GUI.Label(new Rect(panel.x + 24f, panel.y + 16f, 470f, 40f), "Create your student", UiTheme.Heading);

            var view = new Rect(panel.x + 16f, panel.y + 64f, panel.width - 32f, panel.height - 150f);
            float contentH = 26 * 44f;
            _creatorScroll = GUI.BeginScrollView(view, _creatorScroll, new Rect(0f, 0f, view.width - 20f, contentH));
            float y = 0f, lw = 150f, fw = view.width - lw - 30f;
            bool appearanceChanged = false;

            GUI.Label(new Rect(8f, y + 8f, lw, 30f), "Name", UiTheme.Body);
            GUI.SetNextControlName("name");
            p.displayName = GUI.TextField(new Rect(lw, y, fw, 40f), p.displayName ?? "", 24, UiTheme.TextField);
            y += 50f;

            GUI.Label(new Rect(8f, y + 8f, lw, 30f), "Pronouns", UiTheme.Body);
            p.pronouns = GUI.TextField(new Rect(lw, y, fw, 40f), p.pronouns ?? "", 24, UiTheme.TextField);
            y += 46f;
            float pw = (fw - 16f) / 3f;
            for (int i = 0; i < CharacterOptions.Pronouns.Length; i++)
            {
                var opt = CharacterOptions.Pronouns[i];
                if (Button(new Rect(lw + i * (pw + 8f), y, pw, 36f), opt, p.pronouns == opt ? UiTheme.ButtonSelected : UiTheme.Button)) p.pronouns = opt;
            }
            y += 50f;

            p.hometown = CycleRow(ref y, lw, fw, "Hometown", CharacterOptions.Hometowns, p.hometown);
            int programIndex = 0;
            for (int i = 0; i < AcademicPrograms.All.Count; i++) if (AcademicPrograms.All[i].Id == p.programId) programIndex = i;
            var programNames = new string[AcademicPrograms.All.Count];
            for (int i = 0; i < programNames.Length; i++) programNames[i] = AcademicPrograms.All[i].Name;
            programIndex = CycleIndexRow(ref y, lw, fw, "Programme", programNames, programIndex);
            p.programId = AcademicPrograms.All[programIndex].Id;
            p.ambition = CycleRow(ref y, lw, fw, "Ambition", CharacterOptions.Ambitions, p.ambition);
            int bg = Math.Max(0, Array.IndexOf(CharacterOptions.Backgrounds, p.background));
            bg = CycleIndexRow(ref y, lw, fw, "Background", CharacterOptions.BackgroundNames, bg);
            p.background = CharacterOptions.Backgrounds[bg];

            y += 8f;
            GUI.Label(new Rect(8f, y, 400f, 30f), "<b>Appearance</b>  <size=14><i>(cosmetic only — never affects stats)</i></size>", UiTheme.Body);
            y += 36f;
            appearanceChanged |= SwatchRow(ref y, lw, fw, "Skin tone", CharacterOptions.SkinTones, ref a.skinTone);
            appearanceChanged |= IndexRow(ref y, lw, fw, "Hairstyle", CharacterOptions.HairStyles, ref a.hairStyle);
            appearanceChanged |= SwatchRow(ref y, lw, fw, "Hair colour", CharacterOptions.HairColors, ref a.hairColor);
            appearanceChanged |= IndexRow(ref y, lw, fw, "Top", CharacterOptions.TopStyles, ref a.topStyle);
            appearanceChanged |= SwatchRow(ref y, lw, fw, "Top colour", CharacterOptions.ClothingColors, ref a.topColor);
            appearanceChanged |= SwatchRow(ref y, lw, fw, "Trousers", CharacterOptions.ClothingColors, ref a.bottomColor);
            appearanceChanged |= SwatchRow(ref y, lw, fw, "Shoes", CharacterOptions.ClothingColors, ref a.shoeColor);
            appearanceChanged |= IndexRow(ref y, lw, fw, "Accessory", CharacterOptions.Accessories, ref a.accessory);
            GUI.Label(new Rect(8f, y + 8f, lw, 30f), "Height", UiTheme.Body);
            float h = GUI.HorizontalSlider(new Rect(lw, y + 16f, fw, 24f), a.height <= 0f ? 1f : a.height, 0.92f, 1.08f);
            if (Mathf.Abs(h - a.height) > 0.0005f) { a.height = h; appearanceChanged = true; }
            y += 46f;
            if (Button(new Rect(lw, y, fw, 40f), "Randomise look"))
            {
                a.skinTone = UnityEngine.Random.Range(0, CharacterOptions.SkinTones.Length);
                a.hairStyle = UnityEngine.Random.Range(0, CharacterOptions.HairStyles.Length);
                a.hairColor = UnityEngine.Random.Range(0, CharacterOptions.HairColors.Length);
                a.topStyle = UnityEngine.Random.Range(0, CharacterOptions.TopStyles.Length);
                a.topColor = UnityEngine.Random.Range(0, CharacterOptions.ClothingColors.Length);
                a.bottomColor = UnityEngine.Random.Range(0, CharacterOptions.ClothingColors.Length);
                a.shoeColor = UnityEngine.Random.Range(0, CharacterOptions.ClothingColors.Length);
                a.accessory = UnityEngine.Random.Range(0, CharacterOptions.Accessories.Length);
                a.height = UnityEngine.Random.Range(0.94f, 1.06f);
                appearanceChanged = true;
            }
            GUI.EndScrollView();

            if (appearanceChanged) Game.PreviewDraftAppearance();

            // Preview rotation and confirmation.
            float by = panel.yMax - 76f;
            if (!string.IsNullOrEmpty(_creatorError))
                GUI.Label(new Rect(panel.x + 24f, by - 36f, panel.width - 48f, 32f), $"<color=#FF8A80>{_creatorError}</color>", UiTheme.Small);
            if (Button(new Rect(panel.x + 24f, by, 120f, 52f), "Back")) Game.EnterMainMenu();
            if (Button(new Rect(panel.x + 156f, by, panel.width - 180f, 52f), "Begin at AMU", UiTheme.ButtonPrimary))
            {
                if (Game.TryStartNewGame(out var error)) _creatorError = null;
                else _creatorError = error;
            }

            var rot = new Rect(panel.xMax + ((_w - panel.xMax) * 0.5f) - 130f, _h - 84f, 260f, 56f);
            if (GUI.RepeatButton(new Rect(rot.x, rot.y, 120f, 56f), "< Rotate", UiTheme.Button)) Game.RotatePreview(-120f * Time.unscaledDeltaTime);
            if (GUI.RepeatButton(new Rect(rot.x + 140f, rot.y, 120f, 56f), "Rotate >", UiTheme.Button)) Game.RotatePreview(120f * Time.unscaledDeltaTime);
        }

        static string CycleRow(ref float y, float lw, float fw, string label, string[] options, string current)
        {
            int index = Math.Max(0, Array.IndexOf(options, current));
            index = CycleIndexRow(ref y, lw, fw, label, options, index);
            return options[index];
        }

        static int CycleIndexRow(ref float y, float lw, float fw, string label, string[] options, int index)
        {
            GUI.Label(new Rect(8f, y + 8f, lw, 30f), label, UiTheme.Body);
            if (GUI.Button(new Rect(lw, y, 44f, 40f), "<", UiTheme.Button)) index = CharacterOptions.Wrap(index - 1, options.Length);
            GUI.Label(new Rect(lw + 52f, y + 8f, fw - 104f, 30f), options[index], UiTheme.Center);
            if (GUI.Button(new Rect(lw + fw - 44f, y, 44f, 40f), ">", UiTheme.Button)) index = CharacterOptions.Wrap(index + 1, options.Length);
            y += 46f;
            return index;
        }

        static bool IndexRow(ref float y, float lw, float fw, string label, string[] options, ref int index)
        {
            int before = index;
            index = CycleIndexRow(ref y, lw, fw, label, options, index);
            return before != index;
        }

        static bool SwatchRow(ref float y, float lw, float fw, string label, float[][] colors, ref int index)
        {
            GUI.Label(new Rect(8f, y + 8f, lw, 30f), label, UiTheme.Body);
            int before = index;
            float size = Mathf.Min(36f, (fw - (colors.Length - 1) * 6f) / colors.Length);
            for (int i = 0; i < colors.Length; i++)
            {
                var r = new Rect(lw + i * (size + 6f), y + 2f, size, size);
                if (i == index) UiTheme.Fill(new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f), UiTheme.Gold);
                UiTheme.Fill(r, MaterialLibrary.FromRgb(colors[i]));
                if (GUI.Button(r, GUIContent.none, GUIStyle.none)) index = i;
            }
            y += 46f;
            return before != index;
        }

        // -------------------------------------------------------- pause menu

        void DrawPauseMenu()
        {
            UiTheme.Fill(new Rect(0, 0, _w, _h), new Color(0f, 0f, 0f, 0.45f));
            var panel = new Rect((_w - 420f) * 0.5f, Mathf.Max(20f, (_h - 520f) * 0.5f), 420f, 520f);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            float x = panel.x + 30f, w = panel.width - 60f, y = panel.y + 24f;
            GUI.Label(new Rect(x, y, w, 40f), "Paused", UiTheme.Heading);
            y += 56f;

            if (_confirmMainMenu)
            {
                GUI.Label(new Rect(x, y, w, 80f), "Return to the main menu? Your progress will be autosaved first.", UiTheme.Body);
                y += 96f;
                if (Button(new Rect(x, y, w, 52f), "Yes, return to main menu", UiTheme.ButtonPrimary))
                {
                    _confirmMainMenu = false;
                    Game.QuitToMainMenu();
                    RefreshSlots();
                }
                y += 62f;
                if (Button(new Rect(x, y, w, 52f), "Cancel")) _confirmMainMenu = false;
                return;
            }

            if (Button(new Rect(x, y, w, 52f), "Resume", UiTheme.ButtonPrimary)) Game.CloseOverlay();
            y += 62f;
            if (Button(new Rect(x, y, w, 52f), "Save Game")) { RefreshSlots(); Game.OpenOverlay(Overlay.Save); }
            y += 62f;
            if (Button(new Rect(x, y, w, 52f), "Load Game")) { RefreshSlots(); Game.OpenOverlay(Overlay.Load); }
            y += 62f;
            if (Button(new Rect(x, y, w, 52f), "Settings")) Game.OpenOverlay(Overlay.Settings);
            y += 62f;
            if (Button(new Rect(x, y, w, 52f), "Main Menu")) _confirmMainMenu = true;
            y += 62f;
            if (GameBootstrap.CanQuitApplication && Button(new Rect(x, y, w, 52f), "Save & Quit")) Game.QuitApplication();
        }

        // ------------------------------------------------------- save / load

        void DrawSavePanel()
        {
            var panel = CenterPanel(620f, 470f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;
            GUI.Label(new Rect(x, y, w, 40f), "Save game", UiTheme.Heading);
            y += 52f;
            foreach (var slot in SaveSystem.ManualSlots)
            {
                var row = new Rect(x, y, w, 84f);
                GUI.Box(row, GUIContent.none, UiTheme.PanelSoft);
                GUI.Label(new Rect(row.x + 16f, row.y + 10f, w - 200f, 66f), SlotDescription(slot), UiTheme.Small);
                bool exists = _slotCache.ContainsKey(slot);
                if (_pendingOverwriteSlot == slot)
                {
                    if (Button(new Rect(row.xMax - 176f, row.y + 16f, 160f, 52f), "Overwrite?", UiTheme.ButtonPrimary)) DoSave(slot);
                }
                else if (Button(new Rect(row.xMax - 176f, row.y + 16f, 160f, 52f), exists ? "Overwrite" : "Save here", exists ? UiTheme.Button : UiTheme.ButtonPrimary))
                {
                    if (exists) _pendingOverwriteSlot = slot;
                    else DoSave(slot);
                }
                y += 94f;
            }
            GUI.Label(new Rect(x, y, w, 40f), $"Stored in: {Game.Saves.StorageDescription}", UiTheme.Dim);
            if (Button(new Rect(x, panel.yMax - 70f, 160f, 50f), "Back")) { _pendingOverwriteSlot = null; Game.CloseSubMenu(); }
        }

        void DoSave(string slot)
        {
            _pendingOverwriteSlot = null;
            Game.SaveTo(slot, out var message);
            Toast(message);
            RefreshSlots();
        }

        void DrawLoadPanel()
        {
            var panel = CenterPanel(620f, 560f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f;
            GUI.Label(new Rect(x, y, w, 40f), "Load game", UiTheme.Heading);
            y += 52f;
            foreach (var slot in Game.Saves.AllSlots())
            {
                var row = new Rect(x, y, w, 84f);
                GUI.Box(row, GUIContent.none, UiTheme.PanelSoft);
                GUI.Label(new Rect(row.x + 16f, row.y + 10f, w - 290f, 66f), SlotDescription(slot), UiTheme.Small);
                if (_slotCache.ContainsKey(slot))
                {
                    if (Button(new Rect(row.xMax - 276f, row.y + 16f, 120f, 52f), "Load", UiTheme.ButtonPrimary))
                    {
                        _pendingDeleteSlot = null;
                        bool ok = Game.TryLoad(slot, out var message);
                        Toast(message);
                        if (!ok) RefreshSlots();
                    }
                    bool confirming = _pendingDeleteSlot == slot;
                    if (Button(new Rect(row.xMax - 146f, row.y + 16f, 130f, 52f), confirming ? "Confirm?" : "Delete", confirming ? UiTheme.ButtonSelected : UiTheme.Button))
                    {
                        if (confirming)
                        {
                            Game.Saves.Delete(slot);
                            _pendingDeleteSlot = null;
                            RefreshSlots();
                            Toast($"Deleted {SaveSystem.SlotLabel(slot)}");
                        }
                        else _pendingDeleteSlot = slot;
                    }
                }
                y += 94f;
            }
            if (Button(new Rect(x, panel.yMax - 70f, 160f, 50f), "Back")) { _pendingDeleteSlot = null; Game.CloseSubMenu(); }
        }

        string SlotDescription(string slot)
        {
            string label = $"<b>{SaveSystem.SlotLabel(slot)}</b>";
            if (!_slotCache.TryGetValue(slot, out var s)) return label + "\n<i>Empty</i>";
            string saved = DateTime.TryParse(s.SavedUtc, null, System.Globalization.DateTimeStyles.RoundtripKind, out var dt)
                ? dt.ToLocalTime().ToString("d MMM yyyy, HH:mm") : s.SavedUtc;
            var program = AcademicPrograms.Find(s.ProgramId);
            return $"{label} — {s.PlayerName}, {(program != null ? program.Name : s.ProgramId)}\n{FormatClock(s.ClockMinutes)} · {UiTheme.Cedis(s.BalancePesewas)} · saved {saved}";
        }

        Rect CenterPanel(float width, float height)
        {
            UiTheme.Fill(new Rect(0, 0, _w, _h), new Color(0f, 0f, 0f, 0.5f));
            width = Mathf.Min(width, _w - 32f);
            height = Mathf.Min(height, _h - 32f);
            var panel = new Rect((_w - width) * 0.5f, (_h - height) * 0.5f, width, height);
            GUI.Box(panel, GUIContent.none, UiTheme.Panel);
            return panel;
        }

        // ---------------------------------------------------------- settings

        void DrawSettings()
        {
            var s = Game.Settings;
            var panel = CenterPanel(640f, 680f);
            float x = panel.x + 28f, w = panel.width - 56f, y = panel.y + 22f, lw = 250f, fw = w - lw;
            GUI.Label(new Rect(x, y, w, 40f), "Settings", UiTheme.Heading);
            y += 54f;
            bool changed = false;

            GUI.Label(new Rect(x, y, lw, 30f), $"Look sensitivity  <b>{s.lookSensitivity:0.0}</b>", UiTheme.Body);
            float sens = GUI.HorizontalSlider(new Rect(x + lw, y + 8f, fw, 24f), s.lookSensitivity, 0.2f, 3f);
            if (Mathf.Abs(sens - s.lookSensitivity) > 0.001f) { s.lookSensitivity = Mathf.Round(sens * 10f) / 10f; changed = true; }
            y += 46f;
            changed |= Toggle(ref y, x, w, "Invert camera Y axis", ref s.invertY);
            changed |= Toggle(ref y, x, w, "Reduced motion (no camera sway, bob or fades)", ref s.reducedMotion);
            changed |= Toggle(ref y, x, w, "Show objective guide marker", ref s.showGuideMarker);
            changed |= Toggle(ref y, x, w, "Show FPS counter", ref s.showFps);
            changed |= Toggle(ref y, x, w, "Show living-campus stats", ref s.showSimStats);

            GUI.Label(new Rect(x, y + 8f, lw, 30f), "Touch controls", UiTheme.Body);
            changed |= EnumButtons(ref s.touchControls, new Rect(x + lw, y, fw, 42f));
            y += 54f;
            GUI.Label(new Rect(x, y + 8f, lw, 30f), "Graphics quality", UiTheme.Body);
            var before = s.graphics;
            changed |= EnumButtons(ref s.graphics, new Rect(x + lw, y, fw, 42f));
            if (before != s.graphics) s.hasChosenGraphics = true;
            y += 54f;
            GUI.Label(new Rect(x, y, lw, 30f), $"Interface size  <b>{s.uiScale:0.00}×</b>", UiTheme.Body);
            float ui = GUI.HorizontalSlider(new Rect(x + lw, y + 8f, fw, 24f), s.uiScale, 0.8f, 1.5f);
            if (Mathf.Abs(ui - s.uiScale) > 0.001f) { s.uiScale = Mathf.Round(ui * 20f) / 20f; changed = true; }
            y += 46f;

            if (changed) Game.ApplySettings();
            GUI.Label(new Rect(x, panel.yMax - 120f, w, 40f), "Controls: WASD move · Mouse look · Shift run · E interact · Tab dashboard · M map · Esc menu · 1–5 dialogue choices", UiTheme.Dim);
            if (Button(new Rect(x, panel.yMax - 70f, 160f, 50f), "Back")) Game.CloseSubMenu();
        }

        static bool Toggle(ref float y, float x, float w, string label, ref bool value)
        {
            bool next = GUI.Toggle(new Rect(x, y, w, 36f), value, "  " + label, UiTheme.ToggleStyle);
            y += 42f;
            if (next == value) return false;
            value = next;
            return true;
        }

        static bool EnumButtons<T>(ref T value, Rect area) where T : struct, Enum
        {
            var values = (T[])Enum.GetValues(typeof(T));
            float bw = (area.width - (values.Length - 1) * 8f) / values.Length;
            bool changed = false;
            for (int i = 0; i < values.Length; i++)
            {
                bool selected = values[i].Equals(value);
                if (GUI.Button(new Rect(area.x + i * (bw + 8f), area.y, bw, area.height), values[i].ToString(), selected ? UiTheme.ButtonSelected : UiTheme.Button) && !selected)
                {
                    value = values[i];
                    changed = true;
                }
            }
            return changed;
        }
    }
}
