using System.Collections.Generic;
using CampusLife.Core.Npcs;
using CampusLife.Runtime.Npc;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>
    /// Lecture Theatre Complex interior: a lobby with the timetable board and
    /// two tiered lecture halls (LT1, LT2). You enter each hall at the back on
    /// a landing; rows of desks step down toward the stage, board and podium.
    /// </summary>
    public static class LectureInteriorBuilder
    {
        public const string AreaId = "lecture_interior";
        public static readonly Vector3 Origin = new Vector3(2000f, 0f, 200f);
        public static readonly string[] Rooms = { "LT1", "LT2" };

        const float HallFloor = -0.9f;
        const float TierStep = 0.3f;
        const float TierDepth = 2.2f;
        const int Tiers = 4;
        const float CeilingY = 5f;

        /// <summary>World centre of a hall's entrance landing (objective target).</summary>
        public static Vector3 RoomEntrance(string room) => Origin + new Vector3(room == "LT1" ? -5f : 5f, 0f, 1f);

        /// <summary>Per-room references the academic system animates (board text, lecturer post).</summary>
        public sealed class Room
        {
            public string Id;
            public TextMesh Board;
            public Transform LecturerPost;
        }

        public static readonly Dictionary<string, Room> BuiltRooms = new Dictionary<string, Room>();

        public static GameObject Build(CampusArea area, WorldBuildContext ctx)
        {
            BuiltRooms.Clear();
            var kit = ctx.Kit;
            var root = WorldKit.Group("Area_LectureInterior", ctx.WorldRoot, Origin);
            var s = WorldKit.Group("Static", root);
            var props = WorldKit.Group("Props", s);
            Color wall = Palette.Plaster, trim = Palette.Terracotta;

            // Floors: lobby + hall landings at 0; hall bowls at -0.9.
            kit.Box(s, "Floor_Lobby", new Vector3(0f, -0.1f, -3f), new Vector3(32f, 0.2f, 10f), Palette.PavingWarm, smoothness: 0.4f);
            kit.Box(s, "Floor_Halls", new Vector3(0f, HallFloor - 0.1f, 9f), new Vector3(32f, 0.2f, 14f), Palette.WoodLight, smoothness: 0.35f);

            // Lobby walls and exit.
            kit.Box(s, "Lobby_S_L", new Vector3(-4.7f, 2f, -8f), new Vector3(6.6f, 4f, 0.3f), wall);
            kit.Box(s, "Lobby_S_R", new Vector3(4.7f, 2f, -8f), new Vector3(6.6f, 4f, 0.3f), wall);
            kit.Box(s, "Lobby_S_Top", new Vector3(0f, 3.5f, -8f), new Vector3(2.8f, 1f, 0.3f), wall);
            kit.Box(s, "ExitDoor", new Vector3(0f, 1.5f, -8f), new Vector3(2.8f, 3f, 0.2f), Palette.Glass, smoothness: 0.8f);
            kit.Box(s, "Lobby_W", new Vector3(-8f, 2f, -4f), new Vector3(0.3f, 4f, 8f), wall);
            kit.Box(s, "Lobby_E", new Vector3(8f, 2f, -4f), new Vector3(0.3f, 4f, 8f), wall);
            kit.Box(s, "Lobby_Ceiling", new Vector3(0f, 4.1f, -4f), new Vector3(16f, 0.2f, 8f), Palette.Cream);
            kit.Box(s, "Lobby_Band", new Vector3(0f, 0.15f, -7.83f), new Vector3(16f, 0.3f, 0.04f), trim, collider: false);

            // Hall shell: outer walls, dividing wall, front wall, ceiling.
            kit.Box(s, "Hall_W", new Vector3(-16f, 2f, 8f), new Vector3(0.3f, 6f, 16f), wall);
            kit.Box(s, "Hall_E", new Vector3(16f, 2f, 8f), new Vector3(0.3f, 6f, 16f), wall);
            kit.Box(s, "Hall_N", new Vector3(0f, 2f, 16f), new Vector3(32f, 6f, 0.3f), wall);
            kit.Box(s, "Hall_Divider", new Vector3(0f, 2f, 8f), new Vector3(0.3f, 6f, 16f), wall);
            kit.Box(s, "Hall_Ceiling", new Vector3(0f, CeilingY + 0.1f, 8f), new Vector3(32f, 0.2f, 16f), Palette.Cream);
            // South wall with a doorway into each hall from the lobby.
            kit.Box(s, "Hall_S_W", new Vector3(-11f, 2f, 0f), new Vector3(10f, 4f, 0.3f), wall);
            kit.Box(s, "Hall_S_Mid", new Vector3(0f, 2f, 0f), new Vector3(7.6f, 4f, 0.3f), wall);
            kit.Box(s, "Hall_S_E", new Vector3(11f, 2f, 0f), new Vector3(10f, 4f, 0.3f), wall);
            kit.Box(s, "Hall_S_Top", new Vector3(0f, 4.5f, 0f), new Vector3(32f, 1f, 0.3f), wall);

            foreach (var room in Rooms) BuildHall(kit, s, props, root, room, ctx);

            // Lobby furnishing: timetable board and benches.
            kit.Box(s, "TimetableBoard", new Vector3(-7.8f, 1.7f, -4f), new Vector3(0.08f, 1.4f, 2.6f), Palette.Cream, collider: false);
            kit.Box(s, "TimetableFrame", new Vector3(-7.83f, 1.7f, -4f), new Vector3(0.06f, 1.55f, 2.75f), Palette.Wood, collider: false);
            WorldKit.Text(s, "TIMETABLE", new Vector3(-7.74f, 2.6f, -4f), 0.25f, Palette.SignText, 90f);
            var board = new GameObject("Timetable").AddComponent<InfoInteractable>();
            board.transform.SetParent(root, false);
            board.transform.localPosition = new Vector3(-7f, 1f, -4f);
            board.Label = "Read the timetable";
            board.Range = 2.2f;
            TimetableBoard = board;

            // Warm lighting.
            AddLight(root, new Vector3(0f, 3.6f, -4f), 10f, 1.2f);
            foreach (float x in new[] { -8f, 8f })
            {
                AddLight(root, new Vector3(x, 4.4f, 4f), 11f, 1.3f);
                AddLight(root, new Vector3(x, 4.4f, 12f), 11f, 1.3f);
            }

            // Spawn, exit and navigation hooks.
            area.AddSpawn("entrance", root, new Vector3(0f, 0.05f, -6.6f), 0f);
            var exit = new GameObject("Door_Exit").AddComponent<DoorInteractable>();
            exit.transform.SetParent(root, false);
            exit.transform.localPosition = new Vector3(0f, 1f, -7.6f);
            exit.TargetAreaId = CampusExteriorBuilder.AreaId;
            exit.TargetSpawnId = "lecture_front";
            exit.Label = "Exit to campus";
            exit.Range = 2.4f;
            ctx.Locations.RegisterPortal(AreaId, CampusExteriorBuilder.AreaId, exit.transform.position);
            ctx.Slots.SetAreaExit(AreaId, root.TransformPoint(new Vector3(0f, 0f, -7f)));

            StaticBatchingUtility.Combine(s.gameObject);
            ctx.Population.BakeArea(AreaId, root, new Bounds(Origin + new Vector3(0f, 2f, 4f), new Vector3(34f, 9f, 26f)));
            return root.gameObject;
        }

        /// <summary>The lobby timetable board; its text is supplied by the academic system.</summary>
        public static InfoInteractable TimetableBoard { get; private set; }

        static void BuildHall(WorldKit kit, Transform s, Transform props, Transform root, string room, WorldBuildContext ctx)
        {
            float cx = room == "LT1" ? -8f : 8f;
            float sign = room == "LT1" ? -1f : 1f;
            // Landing at the back (entrance level), then tiers stepping down toward the stage.
            kit.Box(s, room + "_Landing", new Vector3(cx, -0.45f, 1f), new Vector3(15.7f, 0.9f, 2f), Palette.WoodLight);
            for (int k = 0; k < Tiers - 1; k++)
            {
                float top = -TierStep * k;
                float z0 = 2f + TierDepth * k;
                kit.Box(s, room + "_Tier" + k, new Vector3(cx, (top + HallFloor) * 0.5f, z0 + TierDepth * 0.5f), new Vector3(15.7f, top - HallFloor, TierDepth), Palette.WoodLight);
                kit.Box(s, room + "_Nosing" + k, new Vector3(cx, top - 0.02f, z0 + TierDepth), new Vector3(15.7f, 0.04f, 0.06f), Palette.Terracotta, collider: false);
            }

            // Rows: desks with five chairs each, leaving walkways at both ends of every row.
            for (int k = 0; k < Tiers; k++)
            {
                float y = -TierStep * k;
                float z0 = 2f + TierDepth * k;
                kit.Box(props, room + "_Desk" + k, new Vector3(cx, y + 0.74f, z0 + 1.7f), new Vector3(11.2f, 0.06f, 0.45f), Palette.DarkWood);
                kit.Box(props, room + "_DeskFront" + k, new Vector3(cx, y + 0.37f, z0 + 1.91f), new Vector3(11.2f, 0.74f, 0.04f), Palette.DarkWood);
                for (int i = 0; i < 5; i++)
                {
                    float x = cx - 4.4f + i * 2.2f;
                    var seat = Chair(kit, props, new Vector3(x, y, z0 + 0.8f), room);
                    ctx.Slots.AddSeat(PlaceIds.LectureComplex, AreaId, seat);
                }
            }

            // Stage, board and podium.
            kit.Box(s, room + "_Board", new Vector3(cx, HallFloor + 2.4f, 15.8f), new Vector3(7f, 2.2f, 0.06f), new Color(0.1f, 0.22f, 0.16f), collider: false);
            kit.Box(s, room + "_BoardFrame", new Vector3(cx, HallFloor + 2.4f, 15.83f), new Vector3(7.3f, 2.5f, 0.04f), Palette.Wood, collider: false);
            var boardText = WorldKit.Text(s, room, new Vector3(cx, HallFloor + 2.4f, 15.74f), 0.28f, new Color(0.92f, 0.95f, 0.9f), 180f);
            boardText.transform.SetParent(root, true); // not static-batched: text changes
            kit.Box(s, room + "_Podium", new Vector3(cx + sign * 2.5f, HallFloor + 0.55f, 13.2f), new Vector3(0.9f, 1.1f, 0.6f), Palette.Wood);
            var post = WorldKit.Group(room + "_LecturerPost", root, new Vector3(cx + sign * 2.5f, HallFloor, 13.9f), 180f);

            WorldKit.Text(s, room, new Vector3(4.9f * sign, 3.4f, -0.17f), 0.45f, Palette.SignText, 180f);
            BuiltRooms[room] = new Room { Id = room, Board = boardText, LecturerPost = post };
        }

        /// <summary>A chair facing the board (+Z); the player stands up into the row behind it.</summary>
        static SeatInteractable Chair(WorldKit kit, Transform parent, Vector3 pos, string room)
        {
            var g = WorldKit.Group("Chair", parent, pos);
            kit.Box(g, "Seat", new Vector3(0f, 0.45f, 0f), new Vector3(0.48f, 0.06f, 0.46f), Palette.Wood, collider: false);
            kit.Box(g, "Back", new Vector3(0f, 0.75f, -0.21f), new Vector3(0.48f, 0.55f, 0.05f), Palette.Wood, collider: false);
            var col = g.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.45f, -0.02f);
            col.size = new Vector3(0.48f, 0.9f, 0.46f);
            var seat = SeatInteractable.Create(g, new Vector3(0f, 0f, 0.02f), 0f, 0.48f, standOffset: -0.5f);
            seat.Range = 1.3f;
            seat.RoomId = room;
            return seat;
        }

        static void AddLight(Transform root, Vector3 local, float range, float intensity)
        {
            var go = new GameObject("HallLight");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.range = range;
            l.intensity = intensity;
            l.color = Palette.WarmLight;
            l.shadows = LightShadows.None;
        }
    }
}
