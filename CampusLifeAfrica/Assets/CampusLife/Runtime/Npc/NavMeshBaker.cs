using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace CampusLife.Runtime.Npc
{
    /// <summary>
    /// Bakes a NavMesh at runtime from the colliders of a procedurally built
    /// area (no NavMesh package or pre-baked asset needed). Characters (layer
    /// "Ignore Raycast") are excluded so they never become obstacles in the bake.
    /// The bake runs asynchronously so it does not stall a frame.
    /// </summary>
    public sealed class NavMeshBaker
    {
        const int CharacterLayer = 2;

        NavMeshDataInstance _instance;
        AsyncOperation _operation;

        public bool IsReady => _operation != null && _operation.isDone;
        public float Progress => _operation != null ? _operation.progress : 0f;

        public static NavMeshBuildSettings AgentSettings()
        {
            var s = NavMesh.GetSettingsByID(0);
            s.agentRadius = 0.3f;
            s.agentHeight = 1.8f;
            s.agentClimb = 0.38f;
            s.agentSlope = 45f;
            s.overrideVoxelSize = true;
            s.voxelSize = 0.12f;
            s.minRegionArea = 2f;
            return s;
        }

        /// <summary>Starts baking everything under <paramref name="root"/> within <paramref name="worldBounds"/>.</summary>
        public void BakeAsync(Transform root, Bounds worldBounds)
        {
            var settings = AgentSettings();
            var sources = new List<NavMeshBuildSource>();
            var markups = new List<NavMeshBuildMarkup>();
            NavMeshBuilder.CollectSources(root, ~(1 << CharacterLayer), NavMeshCollectGeometry.PhysicsColliders, 0, markups, sources);
            var data = new NavMeshData(settings.agentTypeID);
            _instance = NavMesh.AddNavMeshData(data);
            _operation = NavMeshBuilder.UpdateNavMeshDataAsync(data, settings, sources, worldBounds);
        }

        public void Remove()
        {
            if (_instance.valid) _instance.Remove();
        }
    }
}
