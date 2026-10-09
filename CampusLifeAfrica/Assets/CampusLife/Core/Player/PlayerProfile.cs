using System;
using System.Collections.Generic;

namespace CampusLife.Core.Player
{
    [Serializable]
    public class AppearanceData
    {
        public int skinTone;
        public int hairStyle;
        public int hairColor;
        public int topStyle;
        public int topColor;
        public int bottomColor;
        public int shoeColor;
        public int accessory;
        /// <summary>Height multiplier, 0.92..1.08.</summary>
        public float height = 1f;
    }

    [Serializable]
    public class PlayerProfileData
    {
        public string displayName = "Student";
        public string pronouns = "they/them";
        public string hometown = "Kumasi";
        public string programId = "computer_science";
        public string ambition = "Graduate with a story worth telling";
        public string background = "first_generation";
        public AppearanceData appearance = new AppearanceData();
    }

    /// <summary>
    /// Options offered by the character creator. Appearance options are purely
    /// cosmetic; none of them affect stats or story outcomes.
    /// </summary>
    public static class CharacterOptions
    {
        public static readonly string[] Pronouns = { "she/her", "he/him", "they/them" };

        public static readonly string[] Hometowns =
        {
            "Accra", "Kumasi", "Tamale", "Cape Coast", "Takoradi", "Ho", "Koforidua", "Sunyani", "Bolgatanga", "Wa",
        };

        public static readonly string[] Ambitions =
        {
            "Graduate with first-class honours",
            "Build a business before graduation",
            "Lead the student body",
            "Land a role at a top company",
            "Give back to my community",
            "Discover what I truly love",
        };

        public static readonly string[] Backgrounds = { "first_generation", "scholarship", "family_business", "returning_student" };

        public static readonly string[] BackgroundNames =
        {
            "First in my family at university",
            "Scholarship student",
            "Raised around the family business",
            "Returning to study after working",
        };

        // Linear RGB-ish 0..1 colours, consumed by the renderer.
        public static readonly float[][] SkinTones =
        {
            new[] { 0.98f, 0.82f, 0.68f }, new[] { 0.87f, 0.66f, 0.49f }, new[] { 0.74f, 0.52f, 0.36f },
            new[] { 0.60f, 0.40f, 0.26f }, new[] { 0.48f, 0.31f, 0.20f }, new[] { 0.38f, 0.24f, 0.16f },
            new[] { 0.29f, 0.18f, 0.12f }, new[] { 0.22f, 0.14f, 0.10f },
        };

        public static readonly string[] HairStyles = { "Low cut", "Short afro", "Big afro", "Braids", "Locs", "Bantu knots", "Headwrap", "Bald" };

        public static readonly float[][] HairColors =
        {
            new[] { 0.06f, 0.05f, 0.05f }, new[] { 0.20f, 0.12f, 0.07f }, new[] { 0.40f, 0.24f, 0.12f },
            new[] { 0.72f, 0.52f, 0.25f }, new[] { 0.55f, 0.10f, 0.12f }, new[] { 0.80f, 0.80f, 0.82f },
        };

        public static readonly string[] TopStyles = { "T-shirt", "Kente-trim shirt", "Hoodie", "Smock (fugu)" };

        public static readonly float[][] ClothingColors =
        {
            new[] { 0.93f, 0.93f, 0.90f }, new[] { 0.10f, 0.12f, 0.16f }, new[] { 0.80f, 0.16f, 0.14f },
            new[] { 0.98f, 0.76f, 0.10f }, new[] { 0.06f, 0.52f, 0.30f }, new[] { 0.12f, 0.36f, 0.72f },
            new[] { 0.55f, 0.24f, 0.62f }, new[] { 0.95f, 0.45f, 0.12f }, new[] { 0.40f, 0.30f, 0.22f },
        };

        public static readonly string[] Accessories = { "None", "Glasses", "Beaded bracelet", "Backpack", "Cap" };

        public static int Wrap(int value, int count) => count <= 0 ? 0 : ((value % count) + count) % count;

        /// <summary>Clamps every index into range so damaged saves still render.</summary>
        public static AppearanceData Sanitize(AppearanceData a)
        {
            if (a == null) return new AppearanceData();
            return new AppearanceData
            {
                skinTone = Wrap(a.skinTone, SkinTones.Length),
                hairStyle = Wrap(a.hairStyle, HairStyles.Length),
                hairColor = Wrap(a.hairColor, HairColors.Length),
                topStyle = Wrap(a.topStyle, TopStyles.Length),
                topColor = Wrap(a.topColor, ClothingColors.Length),
                bottomColor = Wrap(a.bottomColor, ClothingColors.Length),
                shoeColor = Wrap(a.shoeColor, ClothingColors.Length),
                accessory = Wrap(a.accessory, Accessories.Length),
                height = float.IsNaN(a.height) ? 1f : Math.Max(0.92f, Math.Min(1.08f, a.height)),
            };
        }

        public static IList<string> Validate(PlayerProfileData p)
        {
            var errors = new List<string>();
            if (p == null) { errors.Add("Profile missing."); return errors; }
            string name = p.displayName?.Trim() ?? string.Empty;
            if (name.Length < 2) errors.Add("Name must be at least 2 characters.");
            if (name.Length > 24) errors.Add("Name must be at most 24 characters.");
            foreach (char c in name)
                if (char.IsControl(c)) { errors.Add("Name contains invalid characters."); break; }
            if (string.IsNullOrWhiteSpace(p.pronouns) || p.pronouns.Length > 24) errors.Add("Pronouns are required (max 24 characters).");
            if (string.IsNullOrWhiteSpace(p.programId)) errors.Add("Academic program is required.");
            return errors;
        }
    }
}
