using CampusLife.Core.Economy;
using UnityEngine;

namespace CampusLife.Runtime.UI
{
    /// <summary>
    /// Visual language for the immediate-mode UI: dark translucent panels,
    /// kente-inspired gold/green/red accents and large touch-friendly targets.
    /// Textures are generated at runtime so no image assets are required.
    /// </summary>
    public static class UiTheme
    {
        public static readonly Color Gold = new Color(0.98f, 0.76f, 0.1f);
        public static readonly Color Green = new Color(0.12f, 0.62f, 0.36f);
        public static readonly Color Red = new Color(0.85f, 0.2f, 0.18f);
        public static readonly Color Ink = new Color(0.07f, 0.08f, 0.11f, 0.9f);
        public static readonly Color InkSoft = new Color(0.12f, 0.14f, 0.19f, 0.94f);
        public static readonly Color Text = new Color(0.96f, 0.95f, 0.92f);
        public static readonly Color TextDim = new Color(0.72f, 0.72f, 0.7f);

        public static GUIStyle Panel, PanelSoft, Title, Heading, Body, Small, Dim, Button, ButtonPrimary, ButtonSelected,
            Choice, Toast, Prompt, Key, TextField, Center, Pill, ToggleStyle;
        public static Texture2D White, Arrow, Circle, Ring;

        static bool _ready;
        static bool _hasCediGlyph;

        public static void Ensure()
        {
            if (_ready && Panel != null && Panel.normal.background != null) return;
            White = Solid(Color.white);
            Arrow = MakeArrow(64);
            Circle = MakeCircle(128, false);
            Ring = MakeCircle(128, true);

            var font = GUI.skin.font;
            _hasCediGlyph = font != null && font.HasCharacter('₵');

            Panel = Box(Rounded(Ink, 14), 20);
            PanelSoft = Box(Rounded(InkSoft, 12), 14);
            Pill = Box(Rounded(new Color(0f, 0f, 0f, 0.55f), 16), 10);
            Title = Label(46, FontStyle.Bold, Gold);
            Heading = Label(24, FontStyle.Bold, Text);
            Body = Label(18, FontStyle.Normal, Text);
            Body.wordWrap = true;
            Small = Label(15, FontStyle.Normal, Text);
            Small.wordWrap = true;
            Dim = Label(15, FontStyle.Italic, TextDim);
            Dim.wordWrap = true;
            Center = Label(18, FontStyle.Normal, Text);
            Center.alignment = TextAnchor.MiddleCenter;
            Center.wordWrap = true;

            Button = MakeButton(new Color(1f, 1f, 1f, 0.1f), new Color(1f, 1f, 1f, 0.2f), Text);
            ButtonPrimary = MakeButton(new Color(0.98f, 0.76f, 0.1f, 0.95f), new Color(1f, 0.85f, 0.35f, 1f), new Color(0.1f, 0.08f, 0.02f));
            ButtonSelected = MakeButton(new Color(0.12f, 0.62f, 0.36f, 0.85f), new Color(0.2f, 0.72f, 0.45f, 0.95f), Text);
            Choice = MakeButton(new Color(1f, 1f, 1f, 0.08f), new Color(0.98f, 0.76f, 0.1f, 0.3f), Text);
            Choice.alignment = TextAnchor.MiddleLeft;
            Choice.wordWrap = true;
            Choice.padding = new RectOffset(16, 16, 10, 10);

            Toast = Box(Rounded(new Color(0.05f, 0.06f, 0.08f, 0.88f), 10), 12);
            Toast.fontSize = 16;
            Toast.wordWrap = true;
            Toast.normal.textColor = Text;
            Toast.alignment = TextAnchor.MiddleLeft;

            Prompt = Box(Rounded(new Color(0.05f, 0.06f, 0.08f, 0.82f), 18), 12);
            Prompt.fontSize = 19;
            Prompt.alignment = TextAnchor.MiddleCenter;
            Prompt.normal.textColor = Text;

            Key = Box(Rounded(Gold, 6), 4);
            Key.fontSize = 16;
            Key.fontStyle = FontStyle.Bold;
            Key.alignment = TextAnchor.MiddleCenter;
            Key.normal.textColor = new Color(0.1f, 0.08f, 0.02f);

            ToggleStyle = new GUIStyle(GUI.skin.toggle) { fontSize = 18, richText = true };
            ToggleStyle.normal.textColor = ToggleStyle.onNormal.textColor = ToggleStyle.hover.textColor = ToggleStyle.onHover.textColor = Text;
            ToggleStyle.active.textColor = ToggleStyle.onActive.textColor = Text;

            TextField = new GUIStyle(GUI.skin.textField) { fontSize = 20, padding = new RectOffset(10, 10, 8, 8) };
            TextField.normal.background = Rounded(new Color(1f, 1f, 1f, 0.12f), 8);
            TextField.focused.background = Rounded(new Color(1f, 1f, 1f, 0.2f), 8);
            TextField.normal.textColor = TextField.focused.textColor = Text;
            _ready = true;
        }

        /// <summary>Uses the cedi sign when the font has it, otherwise the common "GH¢" form.</summary>
        public static string Fix(string text)
        {
            if (string.IsNullOrEmpty(text) || _hasCediGlyph) return text;
            return text.Replace("₵", "¢");
        }

        public static string Cedis(long pesewas) => Fix(Money.Format(pesewas));

        static GUIStyle Label(int size, FontStyle style, Color color)
        {
            var s = new GUIStyle(GUI.skin.label) { fontSize = size, fontStyle = style, richText = true };
            s.normal.textColor = color;
            return s;
        }

        static GUIStyle Box(Texture2D bg, int padding)
        {
            var s = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(padding, padding, padding, padding),
                border = new RectOffset(16, 16, 16, 16),
                richText = true,
            };
            s.normal.background = bg;
            return s;
        }

        static GUIStyle MakeButton(Color normal, Color hover, Color text)
        {
            var s = new GUIStyle(GUI.skin.button)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                padding = new RectOffset(14, 14, 10, 10),
                border = new RectOffset(12, 12, 12, 12),
                richText = true,
                wordWrap = true,
            };
            s.normal.background = Rounded(normal, 10);
            s.hover.background = s.focused.background = Rounded(hover, 10);
            s.active.background = Rounded(hover * 0.85f, 10);
            s.normal.textColor = s.hover.textColor = s.active.textColor = s.focused.textColor = text;
            return s;
        }

        public static Texture2D Solid(Color c)
        {
            var t = new Texture2D(1, 1, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            t.SetPixel(0, 0, c);
            t.Apply();
            return t;
        }

        /// <summary>32px rounded rectangle for 9-slice backgrounds.</summary>
        public static Texture2D Rounded(Color fill, int radius)
        {
            const int size = 32;
            radius = Mathf.Min(radius, size / 2 - 1);
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave, wrapMode = TextureWrapMode.Clamp };
            var px = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(0f, Mathf.Max(radius - x - 0.5f, x + 0.5f - (size - radius)));
                float dy = Mathf.Max(0f, Mathf.Max(radius - y - 0.5f, y + 0.5f - (size - radius)));
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float a = Mathf.Clamp01(radius - d + 0.5f);
                px[y * size + x] = new Color(fill.r, fill.g, fill.b, fill.a * a);
            }
            t.SetPixels(px);
            t.Apply();
            return t;
        }

        static Texture2D MakeArrow(int size)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                // Upward-pointing chevron triangle.
                float u = (x + 0.5f) / size - 0.5f, v = (y + 0.5f) / size;
                bool inside = v > 0.1f && v < 0.9f && Mathf.Abs(u) < (0.9f - v) * 0.55f && !(v < 0.35f && Mathf.Abs(u) < (0.35f - v) * 0.9f);
                t.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
            t.Apply();
            return t;
        }

        static Texture2D MakeCircle(int size, bool ring)
        {
            var t = new Texture2D(size, size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontSave };
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                float a = Mathf.Clamp01(r - d);
                if (ring) a *= Mathf.Clamp01(d - (r - 6f));
                t.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
            t.Apply();
            return t;
        }

        public static void Fill(Rect r, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, White);
            GUI.color = prev;
        }

        public static void Tint(Rect r, Texture2D tex, Color c)
        {
            var prev = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, tex);
            GUI.color = prev;
        }
    }
}
