using CampusLife.Core.Content;
using CampusLife.Core.Npcs;
using CampusLife.Runtime.Npc;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>
    /// Nkabom Library interior: lobby with the front desk (Mr. Darko),
    /// reading tables with usable chairs, stacks under a mezzanine, a
    /// staircase up to the quiet-study mezzanine, and the exit door.
    /// Built on first entry, away from the outdoor campus.
    /// </summary>
    public static class LibraryInteriorBuilder
    {
        public const string AreaId = "library_interior";
        public static readonly Vector3 Origin = new Vector3(2000f, 0f, 0f);

        const float W = 24f, D = 18f, H = 7f, MezzanineTop = 3.225f;
        static readonly Vector3 DeskLocal = new Vector3(4.5f, 0f, -3.5f);
        static readonly Vector3 DarkoLocal = DeskLocal + new Vector3(0f, 0f, 1.1f);

        /// <summary>World position of the library assistant's post.</summary>
        public static Vector3 DeskNpcPosition => Origin + DarkoLocal;

        public static GameObject Build(CampusArea area, WorldBuildContext ctx)
        {
            var kit = ctx.Kit;
            var root = WorldKit.Group("Area_LibraryInterior", ctx.WorldRoot, Origin);
            var s = WorldKit.Group("Static", root);
            var props = WorldKit.Group("Props", root);

            // Shell.
            kit.Box(s, "Floor", new Vector3(0f, -0.1f, 0f), new Vector3(W, 0.2f, D), Palette.WoodLight, smoothness: 0.45f);
            kit.Box(s, "Ceiling", new Vector3(0f, H + 0.1f, 0f), new Vector3(W, 0.2f, D), Palette.Cream);
            kit.Box(s, "Wall_N", new Vector3(0f, H * 0.5f, D * 0.5f), new Vector3(W, H, 0.3f), Palette.Cream);
            kit.Box(s, "Wall_E", new Vector3(W * 0.5f, H * 0.5f, 0f), new Vector3(0.3f, H, D), Palette.Cream);
            kit.Box(s, "Wall_W", new Vector3(-W * 0.5f, H * 0.5f, 0f), new Vector3(0.3f, H, D), Palette.Cream);
            float sideW = (W - 2.8f) * 0.5f;
            kit.Box(s, "Wall_S_L", new Vector3(-(1.4f + sideW * 0.5f), H * 0.5f, -D * 0.5f), new Vector3(sideW, H, 0.3f), Palette.Cream);
            kit.Box(s, "Wall_S_R", new Vector3(1.4f + sideW * 0.5f, H * 0.5f, -D * 0.5f), new Vector3(sideW, H, 0.3f), Palette.Cream);
            kit.Box(s, "Wall_S_Top", new Vector3(0f, (H + 3.4f) * 0.5f, -D * 0.5f), new Vector3(2.8f, H - 3.4f, 0.3f), Palette.Cream);
            kit.Box(s, "ExitDoor", new Vector3(0f, 1.7f, -D * 0.5f), new Vector3(2.8f, 3.4f, 0.2f), Palette.DarkWood);
            kit.Box(s, "Skirting", new Vector3(0f, 0.12f, D * 0.5f - 0.17f), new Vector3(W, 0.24f, 0.05f), Palette.Terracotta, collider: false);

            // Windows on east and west walls (soft daylight panels).
            for (float z = -6f; z <= 6.1f; z += 4f)
            {
                kit.Emissive(s, "Window_E", PrimitiveType.Cube, new Vector3(W * 0.5f - 0.16f, 4.9f, z), new Vector3(0.05f, 2.2f, 2.2f), new Color(0.75f, 0.85f, 0.95f), 0.9f);
                kit.Emissive(s, "Window_W", PrimitiveType.Cube, new Vector3(-W * 0.5f + 0.16f, 4.9f, z), new Vector3(0.05f, 2.2f, 2.2f), new Color(0.75f, 0.85f, 0.95f), 0.9f);
            }

            // Kente-inspired runner rug from the door to the desk.
            Color[] rug = { Palette.Gold, Palette.Green, Palette.Red, Palette.Green, Palette.Gold };
            for (int i = 0; i < rug.Length; i++)
                kit.Box(s, "Rug", new Vector3(-0.8f + i * 0.4f, 0.006f, -5f), new Vector3(0.4f, 0.012f, 7.5f), rug[i], collider: false);

            // Front desk and Mr. Darko.
            var desk = DeskLocal;
            kit.Box(s, "Desk", desk + new Vector3(0f, 0.55f, 0f), new Vector3(5f, 1.1f, 1f), Palette.Wood);
            kit.Box(s, "DeskTop", desk + new Vector3(0f, 1.13f, 0f), new Vector3(5.2f, 0.06f, 1.2f), Palette.DarkWood, collider: false);
            kit.Box(s, "DeskBand", desk + new Vector3(0f, 0.7f, -0.51f), new Vector3(5f, 0.12f, 0.02f), Palette.Gold, collider: false);
            kit.Box(s, "Monitor", desk + new Vector3(1.4f, 1.4f, 0.2f), new Vector3(0.6f, 0.4f, 0.05f), new Color(0.08f, 0.08f, 0.1f), collider: false);
            kit.Box(props, "BookPile", desk + new Vector3(-1.5f, 1.25f, 0f), new Vector3(0.4f, 0.18f, 0.3f), Palette.Red, collider: false);
            kit.Box(s, "SignBoard", new Vector3(4.5f, 4.6f, -1f), new Vector3(4.2f, 0.7f, 0.05f), Palette.Cream, collider: false);
            WorldKit.Text(s, "FRONT DESK · NKABOM LIBRARY", new Vector3(4.5f, 4.6f, -1.04f), 0.3f, Palette.SignText, 180f);
            foreach (float x in new[] { 2.6f, 6.4f })
                kit.Box(s, "SignWire", new Vector3(x, 5.9f, -1f), new Vector3(0.02f, 2.6f, 0.02f), Palette.Metal, collider: false);
            var darko = ctx.Npcs.Spawn(Ids.NpcDarko, root, DarkoLocal, 180f);
            ctx.Locations.Register(Ids.LocationLibraryDesk, AreaId, darko.transform.position, "Library front desk");

            // Reading tables with chairs.
            foreach (float z in new[] { -6f, -1.5f })
                ReadingTable(kit, s, props, new Vector3(-5f, 0f, z), ctx.Slots);

            // Ground-floor stacks under the mezzanine.
            for (int row = 0; row < 2; row++)
                for (int i = 0; i < 4; i++)
                    Shelf(kit, s, new Vector3(-4.5f + i * 3.6f, 0f, 4.6f + row * 2.4f), 2.6f, row * 4 + i);

            // Mezzanine floor, railing and staircase along the west wall.
            kit.Box(s, "Mezzanine", new Vector3(0f, MezzanineTop - 0.125f, 6f), new Vector3(W, 0.25f, 6f), Palette.WoodLight);
            kit.Box(s, "MezzanineFascia", new Vector3(0f, MezzanineTop - 0.4f, 2.98f), new Vector3(W, 0.3f, 0.05f), Palette.Terracotta, collider: false);
            for (float x = -8.5f; x <= 11.6f; x += 1.2f)
                kit.Box(s, "Baluster", new Vector3(x, MezzanineTop + 0.5f, 3.1f), new Vector3(0.05f, 1f, 0.05f), Palette.Metal, collider: false);
            kit.Box(s, "Handrail", new Vector3(1.6f, MezzanineTop + 1f, 3.1f), new Vector3(20.6f, 0.08f, 0.1f), Palette.DarkWood);
            WorldKit.InvisibleCollider(s, "RailBlock", new Vector3(1.6f, MezzanineTop + 0.6f, 3.1f), new Vector3(20.6f, 1.2f, 0.2f), Vector3.zero);
            kit.Stairs(s, "Stairs", new Vector3(-10.4f, 0f, -3f), 2.2f, MezzanineTop, 6f, 16, Palette.WoodLight);
            WorldKit.InvisibleCollider(s, "StairRailBlock", new Vector3(-9.25f, 1.6f, 0f), new Vector3(0.1f, 3.4f, 6f), Vector3.zero);
            kit.Box(s, "StairRail", new Vector3(-9.25f, 1.6f + 0.9f, 0f), new Vector3(0.06f, 0.08f, 6.6f), Palette.DarkWood, collider: false);
            foreach (float x in new[] { -6f, 0f, 6f })
                kit.Shape(s, "Pillar", PrimitiveType.Cylinder, new Vector3(x, MezzanineTop * 0.5f, 3.2f), new Vector3(0.3f, MezzanineTop * 0.5f, 0.3f), Palette.Cream, collider: true);

            // Quiet study upstairs.
            for (int i = 0; i < 5; i++)
                Shelf(kit, s, new Vector3(-7f + i * 3.4f, MezzanineTop, 8.5f), 2.4f, 10 + i);
            foreach (float x in new[] { -3f, 3.5f })
                StudyDesk(kit, s, props, new Vector3(x, MezzanineTop, 5.4f), ctx.Slots);
            WorldKit.Text(s, "QUIET STUDY", new Vector3(0f, MezzanineTop + 2.6f, 8.8f), 0.4f, Palette.SignText, 180f);

            // Warm interior lighting.
            AddLight(root, new Vector3(-3f, 6.2f, -4f), 11f, 1.4f);
            AddLight(root, new Vector3(5f, 6.2f, -4f), 11f, 1.4f);
            AddLight(root, new Vector3(0f, 2.7f, 5.5f), 9f, 1.1f);
            AddLight(root, new Vector3(0f, 6.3f, 6.5f), 9f, 1.1f);

            // Spawns, exit door and portals.
            area.AddSpawn("entrance", root, new Vector3(0f, 0.05f, -7f), 0f);
            var exit = new GameObject("Door_Exit").AddComponent<DoorInteractable>();
            exit.transform.SetParent(root, false);
            exit.transform.localPosition = new Vector3(0f, 1f, -D * 0.5f + 0.4f);
            exit.TargetAreaId = CampusExteriorBuilder.AreaId;
            exit.TargetSpawnId = "library_front";
            exit.Label = "Exit to campus";
            exit.Range = 2.4f;
            ctx.Locations.RegisterPortal(AreaId, CampusExteriorBuilder.AreaId, exit.transform.position);
            // Residents leave through the door; a couple can browse the stacks.
            ctx.Slots.SetAreaExit(AreaId, root.TransformPoint(new Vector3(0f, 0f, -8f)));
            ctx.Slots.AddStand(PlaceIds.Library, AreaId, root.TransformPoint(new Vector3(-2f, 0.05f, 3.9f)), 0f);
            ctx.Slots.AddStand(PlaceIds.Library, AreaId, root.TransformPoint(new Vector3(5f, 0.05f, 3.9f)), 0f);

            StaticBatchingUtility.Combine(s.gameObject);
            ctx.Population.BakeArea(AreaId, root, new Bounds(Origin + new Vector3(0f, 3f, 0f), new Vector3(W + 2f, 10f, D + 2f)));
            return root.gameObject;
        }

        static void AddLight(Transform root, Vector3 local, float range, float intensity)
        {
            var go = new GameObject("InteriorLight");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = Palette.WarmLight;
            l.shadows = LightShadows.None;
        }

        static void Shelf(WorldKit kit, Transform s, Vector3 basePos, float height, int seed)
        {
            kit.Box(s, "Shelf", basePos + new Vector3(0f, height * 0.5f, 0f), new Vector3(3f, height, 0.55f), Palette.Wood);
            Color[] spines =
            {
                new Color(0.6f, 0.15f, 0.12f), new Color(0.12f, 0.3f, 0.55f), new Color(0.1f, 0.42f, 0.25f),
                new Color(0.85f, 0.65f, 0.15f), new Color(0.35f, 0.2f, 0.45f), new Color(0.9f, 0.88f, 0.8f),
            };
            int shelves = Mathf.FloorToInt(height / 0.55f);
            for (int level = 0; level < shelves; level++)
            {
                float y = 0.3f + level * 0.55f;
                for (int side = -1; side <= 1; side += 2)
                {
                    // A few colour bands per shelf read as rows of books at a fraction of the draw cost.
                    for (int b = 0; b < 3; b++)
                    {
                        var c = spines[((seed * 7 + level * 3 + b + side) & int.MaxValue) % spines.Length];
                        kit.Box(s, "Books", basePos + new Vector3(-0.95f + b * 0.95f, y + 0.17f, side * 0.24f), new Vector3(0.88f, 0.34f, 0.1f), c, collider: false);
                    }
                }
            }
        }

        static void ReadingTable(WorldKit kit, Transform s, Transform props, Vector3 c, PlaceSlots slots)
        {
            kit.Box(s, "Table", c + new Vector3(0f, 0.74f, 0f), new Vector3(3.2f, 0.06f, 1.4f), Palette.DarkWood);
            foreach (float x in new[] { -1.45f, 1.45f })
                kit.Box(s, "TableLeg", c + new Vector3(x, 0.36f, 0f), new Vector3(0.1f, 0.72f, 1.2f), Palette.DarkWood);
            kit.Box(props, "Lamp", c + new Vector3(0f, 0.95f, 0f), new Vector3(0.15f, 0.35f, 0.15f), Palette.Green, collider: false);
            foreach (float x in new[] { -0.8f, 0.8f })
            {
                slots.AddSeat(PlaceIds.Library, AreaId, Chair(kit, props, c + new Vector3(x, 0f, -1.15f), 0f));
                slots.AddSeat(PlaceIds.Library, AreaId, Chair(kit, props, c + new Vector3(x, 0f, 1.15f), 180f));
            }
        }

        static void StudyDesk(WorldKit kit, Transform s, Transform props, Vector3 c, PlaceSlots slots)
        {
            kit.Box(s, "StudyDesk", c + new Vector3(0f, 0.74f, 0f), new Vector3(2.6f, 0.06f, 0.9f), Palette.DarkWood);
            kit.Box(s, "StudyDeskPanel", c + new Vector3(0f, 0.37f, 0.4f), new Vector3(2.6f, 0.74f, 0.06f), Palette.DarkWood);
            kit.Box(props, "OpenBook", c + new Vector3(-0.5f, 0.79f, 0f), new Vector3(0.45f, 0.03f, 0.32f), Palette.White, collider: false);
            foreach (float x in new[] { -0.7f, 0.7f })
                slots.AddSeat(PlaceIds.Library, AreaId, Chair(kit, props, c + new Vector3(x, 0f, -0.85f), 0f));
        }

        /// <summary>Chair whose occupant faces local +Z.</summary>
        static SeatInteractable Chair(WorldKit kit, Transform parent, Vector3 pos, float yaw)
        {
            var g = WorldKit.Group("Chair", parent, pos, yaw);
            kit.Box(g, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.48f, 0.06f, 0.46f), Palette.Wood, collider: false);
            kit.Box(g, "Back", new Vector3(0f, 0.75f, -0.21f), new Vector3(0.48f, 0.55f, 0.05f), Palette.Wood, collider: false);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, -0.02f);
            col.size = new Vector3(0.48f, 0.9f, 0.46f);
            // Chairs face a table, so the player stands up behind the chair.
            var seat = SeatInteractable.Create(g, new Vector3(0f, 0f, 0.02f), 0f, 0.48f, standOffset: -0.82f);
            seat.Range = 1.4f;
            seat.RoomId = "library";
            return seat;
        }
    }
}
