using System.Collections.Generic;
using UnityEngine;

namespace CampusLife.Runtime.Rendering
{
    /// <summary>
    /// Shared primitive meshes. Building renderers from cached meshes avoids
    /// the collider that GameObject.CreatePrimitive always adds.
    /// </summary>
    public static class MeshCache
    {
        static readonly Dictionary<PrimitiveType, Mesh> Meshes = new Dictionary<PrimitiveType, Mesh>();

        public static Mesh Get(PrimitiveType type)
        {
            if (Meshes.TryGetValue(type, out var mesh) && mesh != null) return mesh;
            var temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (Application.isPlaying) Object.Destroy(temp); else Object.DestroyImmediate(temp);
            Meshes[type] = mesh;
            return mesh;
        }

        /// <summary>Creates a renderer-only primitive (no collider).</summary>
        public static GameObject CreateVisual(string name, PrimitiveType type, Transform parent, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>().sharedMesh = Get(type);
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterial = material;
            return go;
        }
    }
}
