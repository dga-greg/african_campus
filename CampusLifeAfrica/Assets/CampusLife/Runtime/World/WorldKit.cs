using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.World
{
    /// <summary>Helpers for building original stylised geometry from shared primitive meshes.</summary>
    public sealed class WorldKit
    {
        public MaterialLibrary Materials { get; }
        static Font _font;

        public WorldKit(MaterialLibrary materials) { Materials = materials; }

        public static Transform Group(string name, Transform parent, Vector3 localPosition = default, float yaw = 0f)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            t.localPosition = localPosition;
            t.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return t;
        }

        /// <summary>Box whose <paramref name="center"/> and <paramref name="size"/> are in the parent's space.</summary>
        public GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, Color color, bool collider = true, float yaw = 0f, float smoothness = 0.15f)
        {
            var go = MeshCache.CreateVisual(name, PrimitiveType.Cube, parent, Materials.Get(color, smoothness));
            go.transform.localPosition = center;
            go.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            go.transform.localScale = size;
            if (collider) go.AddComponent<BoxCollider>();
            return go;
        }

        public GameObject Emissive(Transform parent, string name, PrimitiveType type, Vector3 center, Vector3 size, Color color, float intensity = 1.6f)
        {
            var go = MeshCache.CreateVisual(name, type, parent, Materials.GetEmissive(color, intensity));
            go.transform.localPosition = center;
            go.transform.localScale = size;
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go;
        }

        public GameObject Shape(Transform parent, string name, PrimitiveType type, Vector3 center, Vector3 size, Color color, bool collider = false, Vector3? euler = null)
        {
            var go = MeshCache.CreateVisual(name, type, parent, Materials.Get(color));
            go.transform.localPosition = center;
            go.transform.localScale = size;
            if (euler.HasValue) go.transform.localEulerAngles = euler.Value;
            if (collider)
            {
                switch (type)
                {
                    case PrimitiveType.Sphere: go.AddComponent<SphereCollider>(); break;
                    case PrimitiveType.Capsule:
                    case PrimitiveType.Cylinder:
                        var cap = go.AddComponent<CapsuleCollider>();
                        cap.height = 2f;
                        cap.radius = 0.5f;
                        break;
                    default: go.AddComponent<BoxCollider>(); break;
                }
            }
            return go;
        }

        /// <summary>Invisible collider, e.g. ramps under stairs or boundary walls.</summary>
        public static GameObject InvisibleCollider(Transform parent, string name, Vector3 center, Vector3 size, Vector3 euler)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = center;
            go.transform.localEulerAngles = euler;
            go.AddComponent<BoxCollider>().size = size;
            return go;
        }

        /// <summary>A flight of stairs rising along local +Z, with a smooth invisible ramp for walking.</summary>
        public void Stairs(Transform parent, string name, Vector3 bottomFrontCenter, float width, float rise, float run, int steps, Color color)
        {
            var group = Group(name, parent, bottomFrontCenter);
            float stepH = rise / steps, stepD = run / steps;
            for (int i = 0; i < steps; i++)
            {
                float h = stepH * (i + 1);
                Box(group, "Step", new Vector3(0f, h * 0.5f, stepD * (i + 0.5f)), new Vector3(width, h, stepD), color, collider: false);
            }
            float length = Mathf.Sqrt(rise * rise + run * run);
            float angle = Mathf.Atan2(rise, run) * Mathf.Rad2Deg;
            InvisibleCollider(group, "Ramp", new Vector3(0f, rise * 0.5f - 0.05f, run * 0.5f), new Vector3(width, 0.1f, length), new Vector3(-angle, 0f, 0f));
        }

        public static Font SignFont
        {
            get
            {
                if (_font != null) return _font;
                // Unity 2022.2+ ships LegacyRuntime.ttf; older versions ship Arial.ttf.
                try { _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch (System.ArgumentException) { }
                if (_font == null)
                {
                    try { _font = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch (System.ArgumentException) { }
                }
                return _font;
            }
        }

        /// <summary>World-space text facing local +Z (readable from the front).</summary>
        public static TextMesh Text(Transform parent, string text, Vector3 localPos, float height, Color color, float yaw = 0f)
        {
            var go = new GameObject("Text_" + text);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            // TextMesh reads toward -Z of its transform, so rotate it to face +Z.
            go.transform.localRotation = Quaternion.Euler(0f, 180f + yaw, 0f);
            var tm = go.AddComponent<TextMesh>();
            tm.text = text;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            // Rendered line height is roughly fontSize * characterSize / 10 world units.
            tm.characterSize = height * 10f / tm.fontSize;
            tm.color = color;
            var font = SignFont;
            if (font != null)
            {
                tm.font = font;
                go.GetComponent<MeshRenderer>().sharedMaterial = font.material;
            }
            go.GetComponent<MeshRenderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return tm;
        }

        /// <summary>Culls small props when they cover less than <paramref name="screenFraction"/> of the screen height.</summary>
        public static void CullWhenSmall(GameObject root, float screenFraction = 0.012f)
        {
            var renderers = root.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var lod = root.AddComponent<LODGroup>();
            lod.SetLODs(new[] { new LOD(screenFraction, renderers) });
            lod.RecalculateBounds();
        }
    }
}
