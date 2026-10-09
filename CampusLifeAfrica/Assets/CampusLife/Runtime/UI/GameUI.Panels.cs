using System.Collections.Generic;
using CampusLife.Core.Content;
using CampusLife.Core.Player;
using CampusLife.Core.Time;
using CampusLife.Runtime.World;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    public sealed partial class GameUI
    {
        enum DashboardTab { Profile, Wallet, Inventory, People, Journal }

        DashboardTab _tab;
        Vector2 _dashScroll;

        // --------------------------------------------------------- dashboard

        void DrawDashboard()
        {
            var session = Game.Session;
            var panel = CenterPanel(900f, 600f);
            float x = panel.x + 24f, w = panel.width - 48f, y = panel.y + 18f;
            GUI.Label(new Rect(x, y, w - 140f, 40f), $"Student dashboard · {session.Profile.displayName}", UiTheme.Heading);
            if (Button(new Rect(panel.xMax - 140f, y, 116f, 42f), "Close")) Game.CloseOverlay();
            y += 54f;

            var tabs = (DashboardTab[])System.Enum.GetValues(typeof(DashboardTab));
            float tw = (w - (tabs.Length - 1) * 8f) / tabs.Length;
            for (int i = 0; i < tabs.Length; i++)
                if (Button(new Rect(x + i * (tw + 8f), y, tw, 44f), tabs[i].ToString(), tabs[i] == _tab ? UiTheme.ButtonSelected : UiTheme.Button))
                {
                    _tab = tabs[i];
                    _dashScroll = Vector2.zero;
                }
            y += 58f;

            var view = new Rect(x, y, w, panel.yMax - y - 20f);
            var lines = BuildDashboardLines();
            float contentH = 0f;
            foreach (var l in lines) contentH += LineHeight(l, w - 24f);
            _dashScroll = GUI.BeginScrollView(view, _dashScroll, new Rect(0f, 0f, w - 24f, Mathf.Max(contentH, view.height - 4f)));
            float ly = 0f;
            foreach (var l in lines)
            {
                float h = LineHeight(l, w - 24f);
                GUI.Label(new Rect(0f, ly, w - 24f, h), l.Text, l.Style);
                if (l.Bar >= 0f)
                {
                    var bar = new Rect(w - 260f, ly + 8f, 220f, 12f);
                    UiTheme.Fill(bar, new Color(1f, 1f, 1f, 0.12f));
                    float mid = bar.x + bar.width * 0.5f;
                    float v = (l.Bar - 0.5f) * bar.width;
                    UiTheme.Fill(v >= 0 ? new Rect(mid, bar.y, v, bar.height) : new Rect(mid + v, bar.y, -v, bar.height), v >= 0 ? UiTheme.Green : UiTheme.Red);
                }
                ly += h;
            }
            GUI.EndScrollView();
        }

        struct Line
        {
            public string Text;
            public GUIStyle Style;
            public float Bar; // 0..1 when a relationship bar should be drawn, else -1
        }

        static float LineHeight(Line l, float width) => Mathf.Max(28f, l.Style.CalcHeight(new GUIContent(l.Text), width)) + 6f;

        static Line L(string text, GUIStyle style = null, float bar = -1f) => new Line { Text = UiTheme.Fix(text), Style = style ?? UiTheme.Body, Bar = bar };

        List<Line> BuildDashboardLines()
        {
            var s = Game.Session;
            var lines = new List<Line>();
            switch (_tab)
            {
                case DashboardTab.Profile:
                {
                    var p = s.Profile;
                    var program = AcademicPrograms.Find(p.programId);
                    int bgIndex = System.Array.IndexOf(CharacterOptions.Backgrounds, p.background);
                    lines.Add(L($"<b>{p.displayName}</b>  ({p.pronouns})", UiTheme.Heading));
                    lines.Add(L($"Programme: <b>{(program != null ? program.Name : p.programId)}</b> — {(program != null ? program.Faculty : "")}"));
                    lines.Add(L($"Year 1 · Orientation week at Akwaaba Metropolitan University"));
                    lines.Add(L($"Hometown: {p.hometown}"));
                    lines.Add(L($"Ambition: {p.ambition}"));
                    lines.Add(L($"Background: {(bgIndex >= 0 ? CharacterOptions.BackgroundNames[bgIndex] : p.background)}"));
                    lines.Add(L($"In-game time: {s.Clock.Format()}  ·  Play time: {System.TimeSpan.FromSeconds(s.PlayTimeSeconds):hh\\:mm\\:ss}", UiTheme.Dim));
                    break;
                }
                case DashboardTab.Wallet:
                {
                    lines.Add(L($"Balance: <b>{UiTheme.Cedis(s.Wallet.Balance)}</b>", UiTheme.Heading));
                    lines.Add(L("Every cedi is recorded in your ledger (most recent first):", UiTheme.Dim));
                    var ledger = s.Wallet.Ledger;
                    for (int i = ledger.Count - 1; i >= 0; i--)
                    {
                        var t = ledger[i];
                        string colour = t.amountPesewas >= 0 ? "#7BE0A0" : "#FF8A80";
                        string sign = t.amountPesewas >= 0 ? "+" : "";
                        lines.Add(L($"<color={colour}><b>{sign}{UiTheme.Cedis(t.amountPesewas)}</b></color>  {t.description}   <size=14><i>{new GameClock(t.gameMinute).Format()} · balance {UiTheme.Cedis(t.balanceAfterPesewas)}</i></size>"));
                    }
                    break;
                }
                case DashboardTab.Inventory:
                {
                    int count = 0;
                    foreach (var kv in s.Inventory.Items)
                    {
                        count++;
                        string name = kv.Key, desc = "";
                        if (s.Items.TryGet(kv.Key, out var def)) { name = def.DisplayName; desc = def.Description; }
                        lines.Add(L($"<b>{name}</b> ×{kv.Value}   <size=15><i>{desc}</i></size>"));
                    }
                    if (count == 0) lines.Add(L("Your bag is empty. Auntie Akosua's kiosk near the gate sells drinks and snacks.", UiTheme.Dim));
                    break;
                }
                case DashboardTab.People:
                {
                    int known = 0;
                    foreach (var rel in s.Relationships.All)
                    {
                        known++;
                        var npc = Phase2Content.FindAnyNpc(rel.NpcId);
                        string name = npc != null ? npc.DisplayName : rel.NpcId;
                        string role = npc != null ? npc.Role : "";
                        lines.Add(L($"<b>{name}</b> — {role}   <size=14>affinity {rel.Affinity:+0;-0;0} · trust {rel.Trust:+0;-0;0} · {rel.Conversations} conversation(s)</size>", UiTheme.Body, (rel.Affinity + 100f) / 200f));
                        foreach (var m in rel.Memories)
                            lines.Add(L($"     • {Phase2Content.DescribeMemory(m.key)}  <i>({new GameClock(m.gameMinute).Format()})</i>", UiTheme.Small));
                    }
                    if (known == 0) lines.Add(L("You haven't met anyone yet. Try talking to the people around campus.", UiTheme.Dim));
                    break;
                }
                case DashboardTab.Journal:
                {
                    var current = Game.CurrentObjective;
                    foreach (var o in Game.Objectives.All)
                    {
                        bool done = s.Flags.Has(o.CompletedByFlag);
                        bool active = current == o;
                        string mark = done ? "<color=#7BE0A0><b>Done</b></color>" : active ? "<color=#F9C21A><b>Current</b></color>" : "<color=#9A9A9A>Upcoming</color>";
                        lines.Add(L($"{mark}   {(done || active ? o.Text : "???")}"));
                    }
                    if (current == null) lines.Add(L("Orientation complete! Explore the campus freely. More of AMU opens up in upcoming updates.", UiTheme.Dim));
                    break;
                }
            }
            return lines;
        }

        // --------------------------------------------------------------- map

        void DrawMap()
        {
            var panel = CenterPanel(920f, 660f);
            float x = panel.x + 24f, y = panel.y + 18f;
            GUI.Label(new Rect(x, y, 500f, 40f), "Campus map · Akwaaba Metropolitan University", UiTheme.Heading);
            if (Button(new Rect(panel.xMax - 140f, y, 116f, 42f), "Close")) Game.CloseOverlay();

            var loc = Game.Locations;
            var bounds = loc.CampusBounds;
            var area = new Rect(x, y + 54f, panel.height - 100f, panel.height - 100f);
            area.width = Mathf.Min(area.width, panel.width - 300f);
            area.height = area.width;
            UiTheme.Fill(area, new Color(0.36f, 0.55f, 0.27f, 1f));

            Rect ToMap(Rect world)
            {
                float sx = area.width / bounds.width, sz = area.height / bounds.height;
                return new Rect(area.x + (world.xMin - bounds.xMin) * sx, area.y + (bounds.yMax - world.yMax) * sz, world.width * sx, world.height * sz);
            }
            Vector2 PointToMap(Vector3 world)
            {
                float sx = area.width / bounds.width, sz = area.height / bounds.height;
                return new Vector2(area.x + (world.x - bounds.xMin) * sx, area.y + (bounds.yMax - world.z) * sz);
            }

            foreach (var f in loc.MapFeatures)
            {
                Color c;
                switch (f.Kind)
                {
                    case MapFeatureKind.EnterableBuilding: c = new Color(0.98f, 0.76f, 0.1f); break;
                    case MapFeatureKind.Building: c = new Color(0.86f, 0.8f, 0.7f); break;
                    case MapFeatureKind.Path: c = new Color(0.32f, 0.33f, 0.36f); break;
                    case MapFeatureKind.Sports: c = new Color(0.25f, 0.65f, 0.25f); break;
                    case MapFeatureKind.Parking: c = new Color(0.4f, 0.4f, 0.44f); break;
                    default: c = new Color(0.85f, 0.78f, 0.62f); break;
                }
                var r = ToMap(f.Bounds);
                UiTheme.Fill(r, c);
            }
            foreach (var f in loc.MapFeatures)
            {
                if (string.IsNullOrEmpty(f.Label) || f.Kind == MapFeatureKind.Path) continue;
                var r = ToMap(f.Bounds);
                GUI.Label(new Rect(r.center.x - 80f, r.center.y - 12f, 160f, 40f), $"<size=12><b>{f.Label}</b></size>", MapLabel);
            }

            // Residents out on campus right now (students and staff).
            int outdoors = 0;
            var residents = Game.Residents;
            foreach (var state in residents.Sim.States)
            {
                if (!residents.TryGetCampusPosition(state, out var rp)) continue;
                outdoors++;
                var dot = PointToMap(rp);
                bool student = state.Profile.Role == CampusLife.Core.Npcs.NpcRole.Student;
                UiTheme.Tint(new Rect(dot.x - 4f, dot.y - 4f, 8f, 8f), UiTheme.Circle, student ? StudentDot : StaffDot);
            }

            // Objective marker.
            var objective = Game.CurrentObjective;
            if (objective != null && loc.TryGetCampusPosition(objective.TargetLocationId, out var objPos))
            {
                var p = PointToMap(objPos);
                UiTheme.Tint(new Rect(p.x - 11f, p.y - 11f, 22f, 22f), UiTheme.Circle, UiTheme.Gold);
                UiTheme.Tint(new Rect(p.x - 15f, p.y - 15f, 30f, 30f), UiTheme.Ring, Color.white);
            }

            // Player marker (interiors show at their entrance).
            Vector3 playerPos = Game.Player.transform.position;
            bool inside = Game.Areas.Current != null && Game.Areas.Current.IsInterior;
            if (inside && loc.TryResolveGuide(Ids.LocationLibraryEntrance, "campus", out var entrance)) playerPos = entrance;
            var pp = PointToMap(playerPos);
            var m = GUI.matrix;
            GUIUtility.RotateAroundPivot(Game.Player.transform.eulerAngles.y, pp * _scale);
            UiTheme.Tint(new Rect(pp.x - 12f, pp.y - 12f, 24f, 24f), UiTheme.Arrow, new Color(0.2f, 0.6f, 1f));
            GUI.matrix = m;

            // Legend.
            float lx = area.xMax + 24f, ly = area.y, lw = panel.xMax - lx - 24f;
            GUI.Label(new Rect(lx, ly, lw, 30f), "<b>Legend</b>", UiTheme.Body); ly += 36f;
            LegendRow(ref ly, lx, lw, new Color(0.2f, 0.6f, 1f), inside ? $"You (inside {Game.Areas.Current.DisplayName})" : "You");
            LegendRow(ref ly, lx, lw, UiTheme.Gold, objective != null ? "Current objective" : "No active objective");
            LegendRow(ref ly, lx, lw, new Color(0.98f, 0.76f, 0.1f), "Building you can enter");
            LegendRow(ref ly, lx, lw, new Color(0.86f, 0.8f, 0.7f), "Other buildings");
            LegendRow(ref ly, lx, lw, StudentDot, "Students");
            LegendRow(ref ly, lx, lw, StaffDot, "Staff");
            GUI.Label(new Rect(lx, ly, lw, 28f), $"{outdoors} people out on campus", UiTheme.Dim);
            ly += 30f;
            ly += 12f;
            if (objective != null) GUI.Label(new Rect(lx, ly, lw, 90f), UiTheme.Fix(objective.Text), UiTheme.Small);
            GUI.Label(new Rect(lx, area.yMax - 60f, lw, 60f), "North is up. The main gate is at the south edge.", UiTheme.Dim);
        }

        static readonly Color StudentDot = new Color(0.3f, 0.85f, 0.95f);
        static readonly Color StaffDot = new Color(1f, 0.55f, 0.2f);

        static GUIStyle _mapLabel;
        static GUIStyle MapLabel
        {
            get
            {
                if (_mapLabel == null)
                {
                    _mapLabel = new GUIStyle(UiTheme.Center) { fontSize = 12, richText = true };
                    _mapLabel.normal.textColor = new Color(0.08f, 0.08f, 0.08f);
                }
                return _mapLabel;
            }
        }

        static void LegendRow(ref float y, float x, float w, Color c, string text)
        {
            UiTheme.Tint(new Rect(x, y + 6f, 16f, 16f), UiTheme.Circle, c);
            GUI.Label(new Rect(x + 26f, y, w - 26f, 28f), text, UiTheme.Small);
            y += 30f;
        }
    }
}
