using System.Collections.Generic;
using CampusLife.Core.Content;
using CampusLife.Core.Npcs;
using CampusLife.Runtime.Npc;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>
    /// Builds the outdoor campus of Akwaaba Metropolitan University: gate,
    /// roads, walkways, Akwaaba Plaza, gardens, Nkabom Library (enterable),
    /// lecture complex and hostel exteriors, sports field, parking, the kiosk
    /// and shuttle stop. North is +Z; the main gate is at the south edge.
    /// </summary>
    public static class CampusExteriorBuilder
    {
        public const string AreaId = "campus";
        public const float Half = 88f;
        public static readonly Vector3 LibraryDoor = new Vector3(0f, 1f, 42.6f);

        public static GameObject Build(CampusArea area, WorldBuildContext ctx)
        {
            PlazaSeats.Clear();
            FieldSeats.Clear();
            ShuttleSeats.Clear();
            var root = WorldKit.Group("Area_Campus", ctx.WorldRoot);
            var statics = WorldKit.Group("Static", root);
            // Trees, benches and small props never move, so they are static-batched too.
            var props = WorldKit.Group("Props", statics);
            var lamps = WorldKit.Group("StreetLights", root);
            var kit = ctx.Kit;
            var loc = ctx.Locations;
            var occupied = new List<Rect>();

            BuildGround(kit, statics, loc, occupied);
            BuildBoundaryAndGate(kit, statics, loc);
            BuildLibraryExterior(kit, statics, area, loc, occupied);
            BuildLectureComplex(kit, statics, loc, occupied);
            BuildHostel(kit, statics, loc, occupied);
            BuildSportsField(kit, statics, props, loc, occupied);
            BuildParking(kit, statics, loc, occupied);
            BuildPlaza(kit, statics, props, loc, occupied);
            BuildKioskAndShuttle(kit, statics, props, ctx, loc, occupied);
            BuildStreetLights(kit, lamps, ctx);
            BuildResidentPlaces(kit, statics, ctx, occupied);
            BuildTrees(kit, props, occupied);

            // Spawn points and NPC posts.
            area.AddSpawn("start", root, new Vector3(0f, 0.05f, -83f), 0f);
            area.AddSpawn("library_front", root, new Vector3(0f, 0.95f, 40.4f), 180f);
            area.AddSpawn("creator", root, new Vector3(-3f, 0.05f, -20f), 160f);

            var esi = ctx.Npcs.Spawn(Ids.NpcEsi, root, new Vector3(3.5f, 0f, -77.5f), 205f);
            kit.Box(esi.Animator.Visual.Props, "Clipboard", new Vector3(0f, 0f, 0.05f), new Vector3(0.22f, 0.3f, 0.02f), Palette.WoodLight, collider: false);
            loc.Register(Ids.LocationGate, AreaId, esi.transform.position, "Esi (main gate)");

            BuildOrientationBanner(kit, statics);

            StaticBatchingUtility.Combine(statics.gameObject);
            ctx.Population.BakeArea(AreaId, root, new Bounds(new Vector3(0f, 2f, 0f), new Vector3(190f, 14f, 190f)));
            return root.gameObject;
        }

        static Rect R(float cx, float cz, float w, float d) => new Rect(cx - w * 0.5f, cz - d * 0.5f, w, d);

        static void BuildGround(WorldKit kit, Transform s, WorldLocations loc, List<Rect> occupied)
        {
            kit.Box(s, "Ground", new Vector3(0f, -0.25f, 0f), new Vector3(220f, 0.5f, 220f), Palette.Grass);
            // Gentle lawn variation.
            kit.Box(s, "LawnWest", new Vector3(-20f, 0.005f, 15f), new Vector3(22f, 0.01f, 40f), Palette.GrassDark, collider: false);
            kit.Box(s, "LawnEast", new Vector3(18f, 0.005f, -30f), new Vector3(20f, 0.01f, 22f), Palette.GrassDark, collider: false);

            // Main walkway (gate → library), split around the ring road crossing.
            kit.Box(s, "Walkway_South", new Vector3(0f, 0.02f, -70.5f), new Vector3(6f, 0.04f, 35f), Palette.Paving);
            kit.Box(s, "Walkway_North", new Vector3(0f, 0.02f, -4.5f), new Vector3(6f, 0.04f, 83f), Palette.Paving);
            occupied.Add(R(0f, -25f, 9f, 130f));
            loc.AddMapFeature("Main walkway", R(0f, -25f, 6f, 126f), MapFeatureKind.Path);

            // Ring road with zebra crossing and centre dashes.
            kit.Box(s, "Road_South", new Vector3(0f, 0.015f, -50f), new Vector3(132f, 0.03f, 8f), Palette.Asphalt);
            kit.Box(s, "Road_West", new Vector3(-62f, 0.016f, 10f), new Vector3(8f, 0.03f, 128f), Palette.Asphalt);
            kit.Box(s, "Road_East", new Vector3(62f, 0.016f, 10f), new Vector3(8f, 0.03f, 128f), Palette.Asphalt);
            kit.Box(s, "Road_North", new Vector3(0f, 0.017f, 70f), new Vector3(132f, 0.03f, 8f), Palette.Asphalt);
            occupied.Add(R(0f, -50f, 140f, 12f)); occupied.Add(R(0f, 70f, 140f, 12f));
            occupied.Add(R(-62f, 10f, 12f, 136f)); occupied.Add(R(62f, 10f, 12f, 136f));
            loc.AddMapFeature("Ring road", R(0f, -50f, 132f, 8f), MapFeatureKind.Path);
            loc.AddMapFeature(null, R(-62f, 10f, 8f, 128f), MapFeatureKind.Path);
            loc.AddMapFeature(null, R(62f, 10f, 8f, 128f), MapFeatureKind.Path);
            loc.AddMapFeature(null, R(0f, 70f, 132f, 8f), MapFeatureKind.Path);
            for (float x = -2.4f; x <= 2.41f; x += 1.2f)
                kit.Box(s, "Zebra", new Vector3(x, 0.035f, -50f), new Vector3(0.6f, 0.01f, 7f), Palette.RoadLine, collider: false);
            for (float x = -60f; x <= 60f; x += 6f)
                if (Mathf.Abs(x) > 5f) kit.Box(s, "Dash", new Vector3(x, 0.035f, -50f), new Vector3(2.4f, 0.01f, 0.18f), Palette.RoadYellow, collider: false);
            // Path from walkway to the kiosk and shuttle stop.
            kit.Box(s, "KioskPath", new Vector3(13f, 0.021f, -58f), new Vector3(20f, 0.04f, 3f), Palette.PavingWarm);
        }

        static void BuildBoundaryAndGate(WorldKit kit, Transform s, WorldLocations loc)
        {
            float h = 2.4f;
            Color wall = Palette.Cream, cap = Palette.Terracotta;
            void Wall(string n, Vector3 c, Vector3 size)
            {
                kit.Box(s, n, c, size, wall);
                kit.Box(s, n + "_Cap", c + new Vector3(0f, size.y * 0.5f + 0.08f, 0f), new Vector3(size.x + 0.1f, 0.16f, size.z + 0.1f), cap, collider: false);
            }
            Wall("Wall_North", new Vector3(0f, h * 0.5f, Half), new Vector3(Half * 2f, h, 0.5f));
            Wall("Wall_West", new Vector3(-Half, h * 0.5f, 0f), new Vector3(0.5f, h, Half * 2f));
            Wall("Wall_East", new Vector3(Half, h * 0.5f, 0f), new Vector3(0.5f, h, Half * 2f));
            float sideLen = Half - 7.6f;
            Wall("Wall_SouthW", new Vector3(-(7.6f + sideLen * 0.5f), h * 0.5f, -Half), new Vector3(sideLen, h, 0.5f));
            Wall("Wall_SouthE", new Vector3(7.6f + sideLen * 0.5f, h * 0.5f, -Half), new Vector3(sideLen, h, 0.5f));
            // Keep the player on campus through the open gate.
            WorldKit.InvisibleCollider(s, "GateBarrier", new Vector3(0f, 2f, -Half - 3f), new Vector3(16f, 4f, 0.5f), Vector3.zero);
            kit.Box(s, "OuterRoad", new Vector3(0f, 0.015f, -Half - 7f), new Vector3(220f, 0.03f, 8f), Palette.Asphalt, collider: false);

            // Gate: laterite pillars, cream lintel, kente-inspired bands.
            foreach (float side in new[] { -1f, 1f })
            {
                kit.Box(s, "GatePillar", new Vector3(side * 7f, 3.2f, -Half), new Vector3(1.3f, 6.4f, 1.3f), Palette.Terracotta);
                kit.Box(s, "GatePillarCap", new Vector3(side * 7f, 6.55f, -Half), new Vector3(1.6f, 0.3f, 1.6f), Palette.Cream, collider: false);
            }
            kit.Box(s, "GateLintel", new Vector3(0f, 5.6f, -Half), new Vector3(13f, 1.3f, 1f), Palette.Cream);
            Color[] bands = { Palette.Gold, Palette.Green, Palette.Red, Palette.Green, Palette.Gold };
            for (int i = 0; i < bands.Length; i++)
                kit.Box(s, "GateBand", new Vector3(-6f + i * 3f, 5.0f, -Half - 0.52f), new Vector3(2.6f, 0.18f, 0.05f), bands[i], collider: false);
            WorldKit.Text(s, "AKWAABA METROPOLITAN UNIVERSITY", new Vector3(0f, 5.75f, -Half - 0.53f), 0.55f, Palette.SignText, 180f);
            WorldKit.Text(s, "AKWAABA!  WELCOME", new Vector3(0f, 5.75f, -Half + 0.53f), 0.55f, Palette.SignText, 0f);

            // Security booth (exterior only in Phase 1; security staff arrive with NPC schedules).
            kit.Box(s, "SecurityBooth", new Vector3(10.5f, 1.3f, -85f), new Vector3(2.6f, 2.6f, 2.6f), Palette.Plaster);
            kit.Box(s, "SecurityRoof", new Vector3(10.5f, 2.7f, -85f), new Vector3(3.2f, 0.2f, 3.2f), Palette.RoofTile, collider: false);
            kit.Box(s, "SecurityWindow", new Vector3(9.18f, 1.6f, -85f), new Vector3(0.05f, 0.9f, 1.6f), Palette.Glass, collider: false);
            loc.AddMapFeature("Main gate", R(0f, -Half, 16f, 3f), MapFeatureKind.Building);
        }

        static void BuildOrientationBanner(WorldKit kit, Transform s)
        {
            foreach (float x in new[] { -7.5f, -3.5f })
                kit.Shape(s, "BannerPole", PrimitiveType.Cylinder, new Vector3(x, 1.5f, -74f), new Vector3(0.12f, 1.5f, 0.12f), Palette.Metal, collider: true);
            kit.Box(s, "Banner", new Vector3(-5.5f, 2.4f, -74f), new Vector3(3.9f, 0.9f, 0.05f), Palette.Green, collider: false);
            WorldKit.Text(s, "WELCOME, FRESHERS!", new Vector3(-5.5f, 2.4f, -74.03f), 0.32f, Palette.White, 180f);
            WorldKit.Text(s, "Orientation week", new Vector3(-5.5f, 2.4f, -73.97f), 0.3f, Palette.White, 0f);
        }

        static void BuildLibraryExterior(WorldKit kit, Transform s, CampusArea area, WorldLocations loc, List<Rect> occupied)
        {
            const float plinth = 0.9f;
            float front = 43f;
            kit.Box(s, "Library_Plinth", new Vector3(0f, plinth * 0.5f, 52f), new Vector3(34f, plinth, 19f), Palette.Laterite);
            kit.Box(s, "Library_Body", new Vector3(0f, plinth + 4.75f, 52.5f), new Vector3(32f, 9.5f, 19f), Palette.Cream);
            kit.Box(s, "Library_Roof", new Vector3(0f, plinth + 9.8f, 52.5f), new Vector3(33.5f, 0.6f, 20.5f), Palette.RoofTile, collider: false);
            kit.Box(s, "Library_Band", new Vector3(0f, plinth + 8.6f, front - 0.02f), new Vector3(32f, 0.5f, 0.06f), Palette.Terracotta, collider: false);

            // Entrance platform, stairs and portico.
            kit.Box(s, "Library_Platform", new Vector3(0f, plinth * 0.5f, 41f), new Vector3(14f, plinth, 4f), Palette.PavingWarm);
            kit.Stairs(s, "Library_Stairs", new Vector3(0f, 0f, 36f), 10f, plinth, 3f, 5, Palette.PavingWarm);
            foreach (float x in new[] { -6f, -3.6f, 3.6f, 6f })
                kit.Shape(s, "Column", PrimitiveType.Cylinder, new Vector3(x, plinth + 2.75f, 39.6f), new Vector3(0.6f, 2.75f, 0.6f), Palette.Cream, collider: true);
            kit.Box(s, "Portico", new Vector3(0f, plinth + 5.75f, 41.1f), new Vector3(14f, 0.5f, 4.6f), Palette.Cream, collider: false);
            kit.Box(s, "PorticoTrim", new Vector3(0f, plinth + 5.45f, 38.85f), new Vector3(14f, 0.12f, 0.1f), Palette.Gold, collider: false);

            // Door and windows.
            kit.Box(s, "Library_DoorFrame", new Vector3(0f, plinth + 1.75f, front - 0.06f), new Vector3(3.2f, 3.5f, 0.12f), Palette.Gold, collider: false);
            kit.Box(s, "Library_Door", new Vector3(0f, plinth + 1.65f, front - 0.1f), new Vector3(2.6f, 3.3f, 0.12f), Palette.DarkWood, collider: false);
            kit.Box(s, "Library_DoorSplit", new Vector3(0f, plinth + 1.65f, front - 0.17f), new Vector3(0.05f, 3.3f, 0.02f), Palette.Gold, collider: false);
            for (float x = -14f; x <= 14.1f; x += 3.5f)
            {
                kit.Box(s, "Window", new Vector3(x, plinth + 6.6f, front - 0.04f), new Vector3(2.2f, 2f, 0.1f), Palette.Glass, collider: false, smoothness: 0.8f);
                if (Mathf.Abs(x) > 4f)
                    kit.Box(s, "Window", new Vector3(x, plinth + 2.4f, front - 0.04f), new Vector3(2.2f, 2.4f, 0.1f), Palette.Glass, collider: false, smoothness: 0.8f);
            }
            WorldKit.Text(s, "NKABOM LIBRARY", new Vector3(0f, plinth + 4.3f, front - 0.08f), 0.7f, Palette.SignText, 180f);

            var door = new GameObject("Door_Library").AddComponent<DoorInteractable>();
            door.transform.SetParent(s.parent, false);
            door.transform.localPosition = LibraryDoor;
            door.TargetAreaId = LibraryInteriorBuilder.AreaId;
            door.TargetSpawnId = "entrance";
            door.Label = "Enter Nkabom Library";
            door.Range = 2.6f;

            loc.RegisterPortal(AreaId, LibraryInteriorBuilder.AreaId, LibraryDoor);
            loc.Register(Ids.LocationLibraryEntrance, AreaId, LibraryDoor, "Nkabom Library");
            occupied.Add(R(0f, 50f, 40f, 32f));
            loc.AddMapFeature("Nkabom Library", R(0f, 52.5f, 32f, 19f), MapFeatureKind.EnterableBuilding);
        }

        static void BuildLectureComplex(WorldKit kit, Transform s, WorldLocations loc, List<Rect> occupied)
        {
            var c = new Vector3(42f, 0f, 5f);
            kit.Box(s, "Lecture_Body", c + new Vector3(0f, 6f, 0f), new Vector3(26f, 12f, 34f), Palette.Plaster);
            kit.Box(s, "Lecture_Roof", c + new Vector3(0f, 12.3f, 0f), new Vector3(27f, 0.6f, 35f), Palette.Metal, collider: false);
            for (int floor = 0; floor < 3; floor++)
            {
                float y = 2.2f + floor * 3.6f;
                kit.Box(s, "Lecture_Windows", c + new Vector3(-13.03f, y, 0f), new Vector3(0.08f, 1.6f, 30f), Palette.Glass, collider: false, smoothness: 0.8f);
                kit.Box(s, "Lecture_Accent", c + new Vector3(-13.05f, y + 1.15f, 0f), new Vector3(0.06f, 0.25f, 31f), Palette.Terracotta, collider: false);
            }
            WorldKit.Text(s, "LECTURE THEATRE COMPLEX", c + new Vector3(-13.1f, 10.6f, 0f), 0.75f, Palette.SignText, -90f);
            occupied.Add(R(c.x, c.z, 32f, 40f));
            loc.AddMapFeature("Lecture Theatre Complex", R(c.x, c.z, 26f, 34f), MapFeatureKind.Building);
        }

        static void BuildHostel(WorldKit kit, Transform s, WorldLocations loc, List<Rect> occupied)
        {
            var c = new Vector3(-42f, 0f, 20f);
            kit.Box(s, "Hostel_Body", c + new Vector3(0f, 7.5f, 0f), new Vector3(22f, 15f, 36f), Palette.HostelPink);
            kit.Box(s, "Hostel_Roof", c + new Vector3(0f, 15.3f, 0f), new Vector3(23f, 0.6f, 37f), Palette.RoofTile, collider: false);
            for (int floor = 0; floor < 4; floor++)
            {
                float y = 2.6f + floor * 3.4f;
                if (floor > 0)
                {
                    kit.Box(s, "Hostel_Balcony", c + new Vector3(11.4f, y - 1f, 0f), new Vector3(0.8f, 0.15f, 34f), Palette.Cream, collider: false);
                    kit.Box(s, "Hostel_Rail", c + new Vector3(11.75f, y - 0.5f, 0f), new Vector3(0.08f, 0.9f, 34f), Palette.Cream, collider: false);
                }
                for (float z = -15f; z <= 15.1f; z += 3f)
                    if (floor > 0 || Mathf.Abs(z) > 3f)
                        kit.Box(s, "Hostel_Window", c + new Vector3(11.03f, y, z), new Vector3(0.06f, 1.5f, 1.2f), Palette.Glass, collider: false, smoothness: 0.8f);
            }
            WorldKit.Text(s, "UNITY HALL", c + new Vector3(11.1f, 14.1f, 0f), 0.9f, Palette.SignText, 90f);
            occupied.Add(R(c.x, c.z, 28f, 42f));
            loc.AddMapFeature("Unity Hall (hostel)", R(c.x, c.z, 22f, 36f), MapFeatureKind.Building);
        }

        static void BuildSportsField(WorldKit kit, Transform s, Transform props, WorldLocations loc, List<Rect> occupied)
        {
            var c = new Vector3(-38f, 0f, -26f);
            kit.Box(s, "Pitch", c + new Vector3(0f, 0.03f, 0f), new Vector3(40f, 0.06f, 24f), Palette.Pitch);
            Color line = Palette.White;
            kit.Box(s, "Line", c + new Vector3(0f, 0.065f, 11.5f), new Vector3(38f, 0.01f, 0.15f), line, collider: false);
            kit.Box(s, "Line", c + new Vector3(0f, 0.065f, -11.5f), new Vector3(38f, 0.01f, 0.15f), line, collider: false);
            kit.Box(s, "Line", c + new Vector3(-19f, 0.065f, 0f), new Vector3(0.15f, 0.01f, 23f), line, collider: false);
            kit.Box(s, "Line", c + new Vector3(19f, 0.065f, 0f), new Vector3(0.15f, 0.01f, 23f), line, collider: false);
            kit.Box(s, "Line", c + new Vector3(0f, 0.065f, 0f), new Vector3(0.15f, 0.01f, 23f), line, collider: false);
            foreach (float side in new[] { -1f, 1f })
            {
                float x = c.x + side * 19.5f;
                kit.Shape(s, "GoalPost", PrimitiveType.Cylinder, new Vector3(x, 1.2f, c.z - 3.6f), new Vector3(0.12f, 1.2f, 0.12f), line, collider: true);
                kit.Shape(s, "GoalPost", PrimitiveType.Cylinder, new Vector3(x, 1.2f, c.z + 3.6f), new Vector3(0.12f, 1.2f, 0.12f), line, collider: true);
                kit.Box(s, "Crossbar", new Vector3(x, 2.4f, c.z), new Vector3(0.12f, 0.12f, 7.3f), line, collider: false);
            }
            // Spectator benches along the north touchline (sittable).
            for (int i = -2; i <= 2; i++)
                foreach (var seat in Bench(kit, props, new Vector3(c.x + i * 6f, 0f, c.z + 14f), 180f)) FieldSeats.Add(seat);
            occupied.Add(R(c.x, c.z, 44f, 32f));
            loc.AddMapFeature("Sports field", R(c.x, c.z, 40f, 24f), MapFeatureKind.Sports);
        }

        static void BuildParking(WorldKit kit, Transform s, WorldLocations loc, List<Rect> occupied)
        {
            var c = new Vector3(42f, 0f, -30f);
            kit.Box(s, "Parking", c + new Vector3(0f, 0.02f, 0f), new Vector3(24f, 0.04f, 14f), Palette.Asphalt);
            for (int i = 0; i <= 8; i++)
                kit.Box(s, "Stall", c + new Vector3(-12f + i * 3f, 0.045f, 4f), new Vector3(0.12f, 0.01f, 5f), Palette.RoadLine, collider: false);
            Color[] cars = { new Color(0.75f, 0.1f, 0.1f), new Color(0.9f, 0.9f, 0.9f), new Color(0.15f, 0.3f, 0.6f), new Color(0.95f, 0.75f, 0.15f) };
            for (int i = 0; i < cars.Length; i++)
            {
                var p = c + new Vector3(-10.5f + i * 6f, 0f, 4f);
                kit.Box(s, "CarBody", p + new Vector3(0f, 0.6f, 0f), new Vector3(1.8f, 0.7f, 4.2f), cars[i], smoothness: 0.6f);
                kit.Box(s, "CarCabin", p + new Vector3(0f, 1.2f, -0.2f), new Vector3(1.6f, 0.55f, 2.2f), Palette.Glass, collider: false, smoothness: 0.8f);
                foreach (float wx in new[] { -0.85f, 0.85f })
                    foreach (float wz in new[] { -1.3f, 1.3f })
                        kit.Shape(s, "Wheel", PrimitiveType.Cylinder, p + new Vector3(wx, 0.33f, wz), new Vector3(0.62f, 0.12f, 0.62f), new Color(0.08f, 0.08f, 0.08f), euler: new Vector3(0f, 0f, 90f));
            }
            occupied.Add(R(c.x, c.z, 28f, 18f));
            loc.AddMapFeature("Parking", R(c.x, c.z, 24f, 14f), MapFeatureKind.Parking);
        }

        static void BuildPlaza(WorldKit kit, Transform s, Transform props, WorldLocations loc, List<Rect> occupied)
        {
            var c = new Vector3(0f, 0f, -15f);
            kit.Shape(s, "Plaza", PrimitiveType.Cylinder, c + new Vector3(0f, 0.025f, 0f), new Vector3(26f, 0.025f, 26f), Palette.PavingWarm, collider: false);
            // "Nkabom Tower": an original abstract sculpture of stacked, twisting blocks in kente-inspired colours.
            kit.Shape(s, "SculptureBase", PrimitiveType.Cylinder, c + new Vector3(0f, 0.3f, 0f), new Vector3(4.4f, 0.3f, 4.4f), Palette.Laterite, collider: true);
            Color[] tiers = { Palette.Gold, Palette.Green, Palette.Red, Palette.Gold, Palette.Green };
            for (int i = 0; i < tiers.Length; i++)
            {
                float size = 1.6f - i * 0.22f;
                var block = kit.Box(s, "SculptureTier", c + new Vector3(0f, 0.95f + i * 0.75f, 0f), new Vector3(size, 0.7f, size), tiers[i], collider: i == 0, smoothness: 0.5f);
                block.transform.localRotation = Quaternion.Euler(0f, i * 18f, 0f);
            }
            WorldKit.Text(s, "AKWAABA PLAZA", c + new Vector3(0f, 0.62f, -2.21f), 0.22f, Palette.Cream, 180f);

            // Flower beds in four quadrants.
            Color[] flowers = { new Color(0.95f, 0.35f, 0.55f), Palette.Gold, new Color(0.9f, 0.2f, 0.15f), new Color(0.95f, 0.95f, 0.9f) };
            for (int q = 0; q < 4; q++)
            {
                float ang = 45f + q * 90f;
                var dir = Quaternion.Euler(0f, ang, 0f) * Vector3.forward;
                var bedCenter = c + dir * 6.5f;
                kit.Box(s, "FlowerBed", bedCenter + new Vector3(0f, 0.2f, 0f), new Vector3(4.2f, 0.4f, 1.6f), Palette.Laterite, yaw: ang + 90f);
                kit.Box(s, "Soil", bedCenter + new Vector3(0f, 0.41f, 0f), new Vector3(3.9f, 0.04f, 1.3f), new Color(0.3f, 0.2f, 0.12f), collider: false, yaw: ang + 90f);
                for (int f = 0; f < 7; f++)
                {
                    var offset = Quaternion.Euler(0f, ang + 90f, 0f) * new Vector3(-1.6f + f * 0.53f, 0f, (f % 2 == 0) ? -0.3f : 0.3f);
                    kit.Shape(props, "Flower", PrimitiveType.Sphere, bedCenter + offset + new Vector3(0f, 0.6f, 0f), Vector3.one * 0.38f, flowers[(f + q) % flowers.Length]);
                }
            }

            // Benches facing the sculpture.
            for (int i = 0; i < 6; i++)
            {
                float ang = 15f + i * 60f;
                if (Mathf.Abs(Mathf.DeltaAngle(ang, 180f)) < 20f || Mathf.Abs(Mathf.DeltaAngle(ang, 0f)) < 20f) ang += 30f;
                var pos = c + Quaternion.Euler(0f, ang, 0f) * Vector3.forward * 10.5f;
                foreach (var seat in Bench(kit, props, pos, ang + 180f)) PlazaSeats.Add(seat);
            }
            occupied.Add(R(c.x, c.z, 28f, 28f));
            loc.AddMapFeature("Akwaaba Plaza", R(c.x, c.z, 26f, 26f), MapFeatureKind.Green);
        }

        static void BuildKioskAndShuttle(WorldKit kit, Transform s, Transform props, WorldBuildContext ctx, WorldLocations loc, List<Rect> occupied)
        {
            var k = new Vector3(12f, 0f, -61f);
            kit.Box(s, "Kiosk_Floor", k + new Vector3(0f, 0.05f, 0.8f), new Vector3(3.6f, 0.1f, 2.6f), Palette.PavingWarm);
            kit.Box(s, "Kiosk_Counter", k + new Vector3(0f, 0.55f, 0f), new Vector3(3.2f, 1.1f, 0.7f), Palette.Green);
            kit.Box(s, "Kiosk_CounterTop", k + new Vector3(0f, 1.12f, 0f), new Vector3(3.4f, 0.06f, 0.85f), Palette.WoodLight, collider: false);
            kit.Box(s, "Kiosk_Back", k + new Vector3(0f, 1.3f, 2f), new Vector3(3.4f, 2.6f, 0.15f), Palette.Plaster);
            foreach (float x in new[] { -1.65f, 1.65f })
            {
                kit.Box(s, "Kiosk_Post", k + new Vector3(x, 1.3f, -0.3f), new Vector3(0.12f, 2.6f, 0.12f), Palette.Wood);
                kit.Box(s, "Kiosk_Side", k + new Vector3(x, 1.3f, 0.85f), new Vector3(0.1f, 2.6f, 2.2f), Palette.Plaster);
            }
            // Striped canopy.
            for (int i = 0; i < 6; i++)
                kit.Box(s, "Canopy", k + new Vector3(-1.75f + i * 0.7f, 2.7f, 0.8f), new Vector3(0.7f, 0.12f, 3f), i % 2 == 0 ? Palette.Gold : Palette.Green, collider: false);
            // Shelf of drinks.
            for (int i = 0; i < 10; i++)
                kit.Shape(props, "Bottle", PrimitiveType.Cylinder, k + new Vector3(-1.4f + i * 0.31f, 1.75f, 1.85f), new Vector3(0.12f, 0.16f, 0.12f), i % 3 == 0 ? new Color(0.25f, 0.12f, 0.05f) : new Color(0.8f, 0.85f, 0.95f));
            kit.Box(s, "Kiosk_Shelf", k + new Vector3(0f, 1.55f, 1.85f), new Vector3(3.2f, 0.06f, 0.3f), Palette.Wood, collider: false);
            WorldKit.Text(s, "AUNTIE AKOSUA'S KIOSK", k + new Vector3(0f, 3.0f, -0.72f), 0.28f, Palette.SignText, 180f);
            kit.Box(s, "Kiosk_SignBoard", k + new Vector3(0f, 3.0f, -0.68f), new Vector3(3.6f, 0.45f, 0.05f), Palette.Cream, collider: false);

            var akosua = ctx.Npcs.Spawn(Ids.NpcAkosua, s.parent, k + new Vector3(0f, 0.1f, 0.9f), 180f);
            akosua.Range = 3.4f; // reachable from the front of the queue
            loc.Register(Ids.LocationKiosk, AreaId, akosua.transform.position, "Auntie Akosua's kiosk");
            occupied.Add(R(k.x, k.z, 6f, 6f));
            loc.AddMapFeature("Kiosk", R(k.x, k.z + 0.5f, 3.6f, 2.6f), MapFeatureKind.Building);

            // Shuttle stop facing the ring road.
            var st = new Vector3(24f, 0f, -55.5f);
            kit.Box(s, "Shuttle_Roof", st + new Vector3(0f, 2.6f, 0f), new Vector3(4.6f, 0.12f, 2f), Palette.Green, collider: false);
            kit.Box(s, "Shuttle_Back", st + new Vector3(0f, 1.3f, -0.9f), new Vector3(4.4f, 2.6f, 0.1f), Palette.Glass, smoothness: 0.8f);
            foreach (float x in new[] { -2.2f, 2.2f })
                kit.Box(s, "Shuttle_Post", st + new Vector3(x, 1.3f, 0.85f), new Vector3(0.1f, 2.6f, 0.1f), Palette.Metal);
            kit.Box(s, "Shuttle_Sign", st + new Vector3(2.6f, 2.1f, 0.9f), new Vector3(0.9f, 0.6f, 0.06f), Palette.Gold, collider: false);
            WorldKit.Text(s, "AMU SHUTTLE", st + new Vector3(2.6f, 2.1f, 0.94f), 0.14f, Palette.SignText, 0f);
            foreach (var seat in Bench(kit, props, st + new Vector3(0f, 0f, -0.4f), 0f)) ShuttleSeats.Add(seat);
            occupied.Add(R(st.x, st.z, 6f, 4f));
            loc.AddMapFeature("Shuttle stop", R(st.x, st.z, 4.6f, 2f), MapFeatureKind.Building);
        }

        static void BuildStreetLights(WorldKit kit, Transform props, WorldBuildContext ctx)
        {
            var mats = kit.Materials;
            var on = mats.GetEmissive(Palette.WarmLight, 2.2f);
            var off = mats.Get(new Color(0.85f, 0.85f, 0.8f), 0.6f);
            ctx.Lighting.SetBulbMaterials(on, off);
            float[] zs = { -76f, -60f, -38f, -26f, -4f, 8f, 22f, 34f };
            for (int i = 0; i < zs.Length; i++)
            {
                float x = i % 2 == 0 ? -4.2f : 4.2f;
                var pole = WorldKit.Group("StreetLight", props, new Vector3(x, 0f, zs[i]));
                kit.Shape(pole, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), new Vector3(0.14f, 2.1f, 0.14f), Palette.Metal, collider: true);
                kit.Box(pole, "Arm", new Vector3(-Mathf.Sign(x) * 0.5f, 4.15f, 0f), new Vector3(1f, 0.08f, 0.08f), Palette.Metal, collider: false);
                var bulb = kit.Shape(pole, "Bulb", PrimitiveType.Sphere, new Vector3(-Mathf.Sign(x) * 0.95f, 4.0f, 0f), Vector3.one * 0.3f, Palette.White);
                bulb.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var lightGo = new GameObject("Light");
                lightGo.transform.SetParent(pole, false);
                lightGo.transform.localPosition = new Vector3(-Mathf.Sign(x) * 0.95f, 3.8f, 0f);
                var light = lightGo.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 13f;
                light.intensity = 1.8f;
                light.color = Palette.WarmLight;
                light.shadows = LightShadows.None;
                light.enabled = false;
                ctx.Lighting.RegisterStreetLight(light, bulb.GetComponent<MeshRenderer>());
            }
        }

        static void BuildTrees(WorldKit kit, Transform props, List<Rect> occupied)
        {
            // Avenue of shade trees along the main walkway.
            for (float z = -42f; z <= 32f; z += 9f)
            {
                if (z > -30f && z < 0f) continue; // plaza
                ShadeTree(kit, props, new Vector3(-7.5f, 0f, z), 1f + Mathf.PingPong(z * 0.13f, 0.25f));
                ShadeTree(kit, props, new Vector3(7.5f, 0f, z + 4f), 1f + Mathf.PingPong(z * 0.17f, 0.25f));
            }
            // Palms along the ring road.
            for (float x = -54f; x <= 54f; x += 12f)
            {
                if (Mathf.Abs(x) < 8f) continue;
                Palm(kit, props, new Vector3(x, 0f, -44.5f), 1f);
                if (x < 6f || x > 30f) Palm(kit, props, new Vector3(x + 6f, 0f, -55.5f), 0.9f);
            }
            // Scattered garden trees, placed deterministically and away from paths and buildings.
            var rng = new System.Random(1957);
            int placed = 0, attempts = 0;
            while (placed < 26 && attempts < 600)
            {
                attempts++;
                var p = new Vector2((float)(rng.NextDouble() * 160 - 80), (float)(rng.NextDouble() * 160 - 80));
                bool blocked = false;
                foreach (var r in occupied)
                {
                    var grown = new Rect(r.x - 3f, r.y - 3f, r.width + 6f, r.height + 6f);
                    if (grown.Contains(p)) { blocked = true; break; }
                }
                if (blocked || Mathf.Abs(p.x) > 84f || Mathf.Abs(p.y) > 84f) continue;
                occupied.Add(new Rect(p.x - 2.5f, p.y - 2.5f, 5f, 5f));
                if (rng.NextDouble() < 0.35) Palm(kit, props, new Vector3(p.x, 0f, p.y), 0.85f + (float)rng.NextDouble() * 0.3f);
                else ShadeTree(kit, props, new Vector3(p.x, 0f, p.y), 0.9f + (float)rng.NextDouble() * 0.5f);
                placed++;
            }
        }

        static void ShadeTree(WorldKit kit, Transform parent, Vector3 pos, float scale)
        {
            var t = WorldKit.Group("ShadeTree", parent, pos);
            t.localScale = Vector3.one * scale;
            kit.Shape(t, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 1.6f, 0f), new Vector3(0.45f, 1.6f, 0.45f), Palette.TreeTrunk, collider: true);
            kit.Shape(t, "Canopy", PrimitiveType.Sphere, new Vector3(0f, 4.2f, 0f), new Vector3(4.6f, 2.8f, 4.6f), Palette.Leaf);
            kit.Shape(t, "Canopy2", PrimitiveType.Sphere, new Vector3(0.9f, 4.9f, 0.4f), new Vector3(3f, 2.2f, 3f), Palette.LeafLight);
            WorldKit.CullWhenSmall(t.gameObject, 0.01f);
        }

        static void Palm(WorldKit kit, Transform parent, Vector3 pos, float scale)
        {
            var t = WorldKit.Group("Palm", parent, pos, (pos.x * 37f + pos.z * 11f) % 360f);
            t.localScale = Vector3.one * scale;
            kit.Shape(t, "Trunk", PrimitiveType.Cylinder, new Vector3(0f, 3.2f, 0f), new Vector3(0.32f, 3.2f, 0.32f), Palette.TreeTrunk, collider: true);
            kit.Shape(t, "Crown", PrimitiveType.Sphere, new Vector3(0f, 6.4f, 0f), Vector3.one * 0.6f, Palette.TreeTrunk);
            for (int i = 0; i < 7; i++)
            {
                var frond = WorldKit.Group("Frond", t, new Vector3(0f, 6.4f, 0f), i * (360f / 7f));
                kit.Shape(frond, "Leaf", PrimitiveType.Cube, new Vector3(0f, -0.35f, 1.4f), new Vector3(0.7f, 0.06f, 2.9f), Palette.PalmLeaf, euler: new Vector3(18f, 0f, 0f));
            }
            WorldKit.CullWhenSmall(t.gameObject, 0.01f);
        }

        /// <summary>Wooden bench whose seat faces local +Z, with two sittable places.</summary>
        public static SeatInteractable[] Bench(WorldKit kit, Transform parent, Vector3 pos, float yaw)
        {
            var b = WorldKit.Group("Bench", parent, pos, yaw);
            kit.Box(b, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(1.8f, 0.07f, 0.48f), Palette.WoodLight, collider: false);
            kit.Box(b, "Back", new Vector3(0f, 0.78f, -0.24f), new Vector3(1.8f, 0.42f, 0.06f), Palette.WoodLight, collider: false);
            foreach (float x in new[] { -0.8f, 0.8f })
                kit.Box(b, "Leg", new Vector3(x, 0.22f, 0f), new Vector3(0.08f, 0.44f, 0.44f), Palette.Metal, collider: false);
            var col = b.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.5f, -0.04f);
            col.size = new Vector3(1.8f, 1f, 0.5f);
            var seats = new[]
            {
                SeatInteractable.Create(b, new Vector3(-0.45f, 0f, 0.05f), 0f, 0.48f),
                SeatInteractable.Create(b, new Vector3(0.45f, 0f, 0.05f), 0f, 0.48f),
            };
            WorldKit.CullWhenSmall(b.gameObject, 0.008f);
            return seats;
        }

        static readonly List<SeatInteractable> PlazaSeats = new List<SeatInteractable>();
        static readonly List<SeatInteractable> FieldSeats = new List<SeatInteractable>();
        static readonly List<SeatInteractable> ShuttleSeats = new List<SeatInteractable>();

        /// <summary>
        /// Registers where residents go and what they do there: seats, chat
        /// groups, the kiosk queue, routes, and the doors of buildings without
        /// interiors yet (residents walk in and out of them).
        /// </summary>
        static void BuildResidentPlaces(WorldKit kit, Transform s, WorldBuildContext ctx, List<Rect> occupied)
        {
            var slots = ctx.Slots;
            const string A = AreaId;

            // Plaza: bench seats and two chat circles.
            var plaza = new Vector3(0f, 0f, -15f);
            foreach (var seat in PlazaSeats) slots.AddSeat(PlaceIds.Plaza, A, seat);
            slots.AddGroup(PlaceIds.Plaza, A, plaza + new Vector3(7.5f, 0.05f, 0f), 4, 1.05f, 0);
            slots.AddGroup(PlaceIds.Plaza, A, plaza + new Vector3(-7.5f, 0.05f, 0f), 4, 1.05f, 1);
            slots.AddGroup(PlaceIds.Plaza, A, plaza + new Vector3(0f, 0.05f, 7.2f), 3, 1.0f, 2);
            slots.SetAccess(PlaceIds.Plaza, plaza + new Vector3(3.5f, 0f, 0f));

            // Kiosk: a queue facing the counter and spots to eat nearby.
            var queue = new List<Vector3>();
            for (int i = 0; i < 6; i++) queue.Add(new Vector3(12f, 0.05f, -62.3f - i * 0.9f));
            slots.SetQueue(PlaceIds.Kiosk, queue, 0f);
            slots.AddStand(PlaceIds.Kiosk, A, new Vector3(15.6f, 0.05f, -60.8f), 250f);
            slots.AddStand(PlaceIds.Kiosk, A, new Vector3(16.6f, 0.05f, -62.4f), 290f);
            slots.AddStand(PlaceIds.Kiosk, A, new Vector3(15.9f, 0.05f, -64.2f), 320f);
            slots.AddStand(PlaceIds.Kiosk, A, new Vector3(8.2f, 0.05f, -62.6f), 70f);
            slots.AddStand(PlaceIds.Kiosk, A, new Vector3(8.6f, 0.05f, -64.4f), 40f);
            slots.SetAccess(PlaceIds.Kiosk, new Vector3(12f, 0f, -64.5f));

            // Sports field: laps around the pitch and the spectator benches.
            var f = new Vector3(-38f, 0.06f, -26f);
            slots.AddRoute(PlaceIds.SportsField, A, new[]
            {
                f + new Vector3(-18f, 0f, -10.5f), f + new Vector3(0f, 0f, -10.5f), f + new Vector3(18f, 0f, -10.5f), f + new Vector3(18f, 0f, 0f),
                f + new Vector3(18f, 0f, 10.5f), f + new Vector3(0f, 0f, 10.5f), f + new Vector3(-18f, 0f, 10.5f), f + new Vector3(-18f, 0f, 0f),
            }, 8);
            foreach (var seat in FieldSeats) slots.AddSeat(PlaceIds.SportsField, A, seat);
            slots.SetAccess(PlaceIds.SportsField, f + new Vector3(0f, 0f, 12.5f));

            // Shuttle stop.
            foreach (var seat in ShuttleSeats) slots.AddSeat(PlaceIds.ShuttleStop, A, seat);
            slots.AddStand(PlaceIds.ShuttleStop, A, new Vector3(21.4f, 0.05f, -54.4f), 10f);
            slots.AddStand(PlaceIds.ShuttleStop, A, new Vector3(26.6f, 0.05f, -54.4f), 350f);
            slots.SetAccess(PlaceIds.ShuttleStop, new Vector3(24f, 0f, -54f));

            // West lawn: friends sit on the grass.
            occupied.Add(R(-19f, 18f, 16f, 26f));
            slots.AddGroup(PlaceIds.Lawn, A, new Vector3(-18f, 0.02f, 9f), 3, 0.95f, 0, SlotKind.GroundSeat);
            slots.AddGroup(PlaceIds.Lawn, A, new Vector3(-22f, 0.02f, 20f), 3, 0.95f, 1, SlotKind.GroundSeat);
            slots.AddGroup(PlaceIds.Lawn, A, new Vector3(-16f, 0.02f, 27f), 2, 0.8f, 2, SlotKind.GroundSeat);
            slots.AddStand(PlaceIds.Lawn, A, new Vector3(-13.5f, 0.02f, 16f), 260f, -1, SlotKind.GroundSeat);
            slots.AddStand(PlaceIds.Lawn, A, new Vector3(-24f, 0.02f, 12f), 120f, -1, SlotKind.GroundSeat);
            slots.SetAccess(PlaceIds.Lawn, new Vector3(-14f, 0f, 15f));

            // Security booth and patrol.
            slots.AddStand(PlaceIds.SecurityPost, A, new Vector3(8.4f, 0.05f, -83.6f), 270f);
            slots.SetAccess(PlaceIds.SecurityPost, new Vector3(8.4f, 0f, -82f));
            var patrol = new[]
            {
                new Vector3(4.6f, 0f, -80f), new Vector3(4.6f, 0f, -57f), new Vector3(9f, 0f, -42f), new Vector3(10.5f, 0f, -15f),
                new Vector3(6.5f, 0f, 33f), new Vector3(-6.5f, 0f, 33f), new Vector3(-10.5f, 0f, -15f), new Vector3(-9f, 0f, -42f),
                new Vector3(-4.6f, 0f, -57f), new Vector3(-4.6f, 0f, -80f),
            };
            slots.AddRoute(PlaceIds.PatrolRoute, A, patrol, 2);
            slots.SetAccess(PlaceIds.PatrolRoute, patrol[0]);

            // Cleaner's sweeping route around the plaza and along the walkway.
            var sweep = new List<Vector3>();
            for (int i = 0; i < 8; i++) sweep.Add(plaza + Quaternion.Euler(0f, i * 45f + 22f, 0f) * Vector3.forward * 12.3f);
            sweep.Add(new Vector3(1.5f, 0f, -34f));
            sweep.Add(new Vector3(-1.5f, 0f, -42f));
            sweep.Add(new Vector3(1.5f, 0f, 2f));
            sweep.Add(new Vector3(-1.5f, 0f, 20f));
            slots.AddRoute(PlaceIds.SweepRoute, A, sweep.ToArray(), 1);
            slots.SetAccess(PlaceIds.SweepRoute, new Vector3(0f, 0f, -28f));

            // Buildings residents walk in and out of (their interiors arrive in later phases).
            BuildDoorway(kit, s, new Vector3(-30.93f, 0f, 20f), 90f, Palette.DarkWood, "UNITY HALL",
                "Read the hall notice", "Unity Hall — residents only. Freshers' room allocation is handled by the hall office during Week 1.");
            slots.SetAccess(PlaceIds.Hostel, new Vector3(-29.4f, 0f, 20f));
            BuildDoorway(kit, s, new Vector3(28.94f, 0f, 5f), -90f, Palette.Glass, "LECTURE HALLS",
                "Read the notice board", "Lecture Theatre Complex — first-year lectures begin after course registration. Timetables will be posted here.");
            slots.SetAccess(PlaceIds.LectureComplex, new Vector3(27.4f, 0f, 5f));

            slots.SetAccess(PlaceIds.OffCampus, new Vector3(0f, 0f, -86.6f));
            slots.SetAccess(PlaceIds.Library, LibraryDoor);
        }

        /// <summary>
        /// A closed double door on a facade (local +Z = outward) with a readable
        /// notice beside it, so the doorway explains itself instead of looking like a broken door.
        /// </summary>
        static void BuildDoorway(WorldKit kit, Transform s, Vector3 wallPoint, float yaw, Color door, string sign, string label, string notice)
        {
            var g = WorldKit.Group("Doorway_" + sign, s, wallPoint, yaw);
            kit.Box(g, "Frame", new Vector3(0f, 1.45f, 0.03f), new Vector3(2.8f, 2.9f, 0.08f), Palette.Gold, collider: false);
            kit.Box(g, "Doors", new Vector3(0f, 1.4f, 0.08f), new Vector3(2.4f, 2.8f, 0.06f), door, collider: false, smoothness: 0.7f);
            kit.Box(g, "Split", new Vector3(0f, 1.4f, 0.12f), new Vector3(0.05f, 2.8f, 0.02f), Palette.Metal, collider: false);
            kit.Box(g, "Canopy", new Vector3(0f, 3.1f, 0.7f), new Vector3(3.6f, 0.15f, 1.4f), Palette.Terracotta, collider: false);
            kit.Box(g, "Step", new Vector3(0f, 0.06f, 0.6f), new Vector3(3.2f, 0.12f, 1.2f), Palette.PavingWarm, collider: false);
            WorldKit.Text(g, sign, new Vector3(0f, 3.45f, 0.12f), 0.32f, Palette.SignText);
            kit.Box(g, "NoticeBoard", new Vector3(2.2f, 1.5f, 0.1f), new Vector3(1.1f, 0.8f, 0.06f), Palette.Cream, collider: false);
            kit.Box(g, "NoticeFrame", new Vector3(2.2f, 1.5f, 0.07f), new Vector3(1.2f, 0.9f, 0.04f), Palette.Wood, collider: false);
            var info = new GameObject("Notice").AddComponent<InfoInteractable>();
            info.transform.SetParent(g, false);
            info.transform.localPosition = new Vector3(2.2f, 1f, 0.6f);
            info.Label = label;
            info.Message = notice;
            info.Range = 2.2f;
        }
    }
}
