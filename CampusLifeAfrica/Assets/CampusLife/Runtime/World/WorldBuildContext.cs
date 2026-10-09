using CampusLife.Runtime.Lighting;
using CampusLife.Runtime.Npc;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>Shared services handed to area builders.</summary>
    public sealed class WorldBuildContext
    {
        public WorldKit Kit;
        public WorldLocations Locations;
        public NpcFactory Npcs;
        public DayNightCycle Lighting;
        public Transform WorldRoot;
    }

    /// <summary>Original colour palette for AMU: warm laterite, cream plaster, greenery and kente-inspired accents.</summary>
    public static class Palette
    {
        public static readonly Color Grass = new Color(0.42f, 0.62f, 0.27f);
        public static readonly Color GrassDark = new Color(0.33f, 0.52f, 0.22f);
        public static readonly Color Pitch = new Color(0.31f, 0.6f, 0.23f);
        public static readonly Color Paving = new Color(0.85f, 0.79f, 0.68f);
        public static readonly Color PavingWarm = new Color(0.89f, 0.83f, 0.71f);
        public static readonly Color Laterite = new Color(0.71f, 0.36f, 0.24f);
        public static readonly Color Asphalt = new Color(0.23f, 0.24f, 0.26f);
        public static readonly Color RoadLine = new Color(0.95f, 0.93f, 0.85f);
        public static readonly Color RoadYellow = new Color(0.95f, 0.78f, 0.2f);
        public static readonly Color Cream = new Color(0.94f, 0.89f, 0.79f);
        public static readonly Color Plaster = new Color(0.86f, 0.82f, 0.75f);
        public static readonly Color Terracotta = new Color(0.74f, 0.37f, 0.25f);
        public static readonly Color RoofTile = new Color(0.62f, 0.27f, 0.18f);
        public static readonly Color HostelPink = new Color(0.85f, 0.64f, 0.56f);
        public static readonly Color Glass = new Color(0.16f, 0.25f, 0.33f);
        public static readonly Color Wood = new Color(0.45f, 0.29f, 0.17f);
        public static readonly Color WoodLight = new Color(0.66f, 0.47f, 0.3f);
        public static readonly Color DarkWood = new Color(0.27f, 0.17f, 0.1f);
        public static readonly Color Metal = new Color(0.36f, 0.38f, 0.4f);
        public static readonly Color TreeTrunk = new Color(0.4f, 0.3f, 0.22f);
        public static readonly Color Leaf = new Color(0.2f, 0.45f, 0.18f);
        public static readonly Color LeafLight = new Color(0.33f, 0.56f, 0.2f);
        public static readonly Color PalmLeaf = new Color(0.27f, 0.52f, 0.2f);
        public static readonly Color Gold = new Color(0.98f, 0.76f, 0.1f);
        public static readonly Color Green = new Color(0.06f, 0.5f, 0.28f);
        public static readonly Color Red = new Color(0.78f, 0.12f, 0.12f);
        public static readonly Color SignText = new Color(0.1f, 0.2f, 0.12f);
        public static readonly Color White = new Color(0.96f, 0.96f, 0.94f);
        public static readonly Color WarmLight = new Color(1f, 0.82f, 0.58f);
    }
}
