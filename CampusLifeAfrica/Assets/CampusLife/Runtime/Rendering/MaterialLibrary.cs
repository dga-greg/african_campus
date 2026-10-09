using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CampusLife.Runtime.Rendering
{
    /// <summary>
    /// Creates and caches shared materials for procedurally built geometry.
    /// Works with URP when a URP asset is active and falls back to the
    /// built-in pipeline otherwise, so the project renders either way.
    /// Materials are shared per colour, which keeps the SRP Batcher and GPU
    /// instancing effective.
    /// </summary>
    public sealed class MaterialLibrary
    {
        readonly Dictionary<long, Material> _cache = new Dictionary<long, Material>();
        readonly Shader _lit;
        readonly Material _template;

        public bool UsingUrp { get; }

        public MaterialLibrary()
        {
            UsingUrp = GraphicsSettings.currentRenderPipeline != null;
            // A material in Resources guarantees the shader is included in player builds.
            _template = Resources.Load<Material>(UsingUrp ? "CampusLitURP" : "CampusLitBuiltin");
            _lit = _template != null ? _template.shader : FindLitShader(UsingUrp);
        }

        static Shader FindLitShader(bool urp)
        {
            string[] candidates = urp
                ? new[] { "Universal Render Pipeline/Lit", "Universal Render Pipeline/Simple Lit" }
                : new[] { "Standard", "Legacy Shaders/Diffuse" };
            foreach (var name in candidates)
            {
                var s = Shader.Find(name);
                if (s != null) return s;
            }
            Debug.LogWarning("[MaterialLibrary] No lit shader found; falling back to Unlit/Color.");
            return Shader.Find("Unlit/Color");
        }

        public Material Get(Color color, float smoothness = 0.15f) => GetInternal(color, smoothness, false);

        /// <summary>Self-lit material for lamps, windows at night and markers.</summary>
        public Material GetEmissive(Color color, float intensity = 1.5f) => GetInternal(color * intensity, 0.3f, true);

        Material GetInternal(Color color, float smoothness, bool emissive)
        {
            Color32 c32 = color;
            long key = ((long)c32.r << 32) | ((long)c32.g << 24) | ((long)c32.b << 16) | ((long)Mathf.RoundToInt(smoothness * 100) << 1) | (emissive ? 1L : 0L);
            if (_cache.TryGetValue(key, out var mat)) return mat;

            mat = _template != null ? new Material(_template) : new Material(_lit);
            mat.name = $"Campus_{ColorUtility.ToHtmlStringRGB(color)}{(emissive ? "_E" : "")}";
            Color baseColor = emissive ? Color.Lerp(color, Color.white, 0.2f) : color;
            baseColor.a = 1f;
            mat.color = baseColor;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (emissive && mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                mat.SetColor("_EmissionColor", color);
            }
            mat.enableInstancing = true;
            _cache.Add(key, mat);
            return mat;
        }

        public static Color Hex(string html)
        {
            return ColorUtility.TryParseHtmlString(html, out var c) ? c : Color.magenta;
        }

        public static Color FromRgb(float[] rgb) => new Color(rgb[0], rgb[1], rgb[2]);
    }
}
