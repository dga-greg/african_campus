using System.Collections.Generic;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    public enum MapFeatureKind { Building, EnterableBuilding, Path, Green, Water, Parking, Sports }

    public struct MapFeature
    {
        public string Label;
        public Rect Bounds; // x/z in world metres
        public MapFeatureKind Kind;
    }

    /// <summary>
    /// Named places (NPC posts, doors) used by objectives, the guide marker
    /// and the campus map. Doors between areas are recorded as portals so a
    /// target inside a building can be routed via its entrance.
    /// </summary>
    public sealed class WorldLocations
    {
        struct Location { public string AreaId; public Vector3 Position; public string Label; }
        struct Portal { public string FromArea; public string ToArea; public Vector3 Position; }

        readonly Dictionary<string, Location> _locations = new Dictionary<string, Location>();
        readonly List<Portal> _portals = new List<Portal>();
        public readonly List<MapFeature> MapFeatures = new List<MapFeature>();
        public Rect CampusBounds = new Rect(-90f, -95f, 180f, 185f);

        public void Register(string id, string areaId, Vector3 position, string label = null)
            => _locations[id] = new Location { AreaId = areaId, Position = position, Label = label ?? id };

        public void RegisterPortal(string fromArea, string toArea, Vector3 position)
            => _portals.Add(new Portal { FromArea = fromArea, ToArea = toArea, Position = position });

        public void AddMapFeature(string label, Rect bounds, MapFeatureKind kind)
            => MapFeatures.Add(new MapFeature { Label = label, Bounds = bounds, Kind = kind });

        public bool TryGetArea(string id, out string areaId)
        {
            areaId = null;
            if (id == null || !_locations.TryGetValue(id, out var l)) return false;
            areaId = l.AreaId;
            return true;
        }

        /// <summary>
        /// Where to guide the player inside <paramref name="currentArea"/> to reach
        /// a location: the location itself, or the door that leads toward it.
        /// </summary>
        public bool TryResolveGuide(string locationId, string currentArea, out Vector3 position)
        {
            position = default;
            if (locationId == null || !_locations.TryGetValue(locationId, out var loc)) return false;
            if (loc.AreaId == currentArea) { position = loc.Position; return true; }
            foreach (var p in _portals)
                if (p.FromArea == currentArea && p.ToArea == loc.AreaId) { position = p.Position; return true; }
            // Not directly connected: head for any exit from the current area (interiors lead outside).
            foreach (var p in _portals)
                if (p.FromArea == currentArea) { position = p.Position; return true; }
            return false;
        }

        /// <summary>Position of a location on the outdoor campus (interiors map to their entrance).</summary>
        public bool TryGetCampusPosition(string locationId, out Vector3 position)
            => TryResolveGuide(locationId, "campus", out position);
    }
}
