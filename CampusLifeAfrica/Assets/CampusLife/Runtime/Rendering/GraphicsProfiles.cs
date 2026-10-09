using CampusLife.Runtime.Infrastructure;
using UnityEngine;

namespace CampusLife.Runtime.Rendering
{
    /// <summary>
    /// Platform graphics profiles. Values are starting budgets to validate
    /// with the profiler on target devices (see docs/PERFORMANCE.md).
    /// </summary>
    public static class GraphicsProfiles
    {
        public struct Profile
        {
            public int TargetFrameRate;
            public float ShadowDistance;
            public LightShadows SunShadows;
            public float LodBias;
            public float CameraFarClip;
            public int MaxStreetLights;
            public bool StreetLightShadows;
            /// <summary>Most residents with a 3D body at once, and how far away bodies appear.</summary>
            public int MaxResidents;
            public float ResidentRadius;
        }

        public static GraphicsProfile RecommendedForPlatform()
        {
            if (Application.isMobilePlatform) return GraphicsProfile.Low;
            if (Application.platform == RuntimePlatform.WebGLPlayer) return GraphicsProfile.Medium;
            return GraphicsProfile.High;
        }

        public static Profile Get(GraphicsProfile p)
        {
            switch (p)
            {
                case GraphicsProfile.Low:
                    return new Profile { TargetFrameRate = 30, ShadowDistance = 25f, SunShadows = LightShadows.None, LodBias = 0.6f, CameraFarClip = 180f, MaxStreetLights = 0, StreetLightShadows = false, MaxResidents = 20, ResidentRadius = 45f };
                case GraphicsProfile.Medium:
                    return new Profile { TargetFrameRate = 60, ShadowDistance = 45f, SunShadows = LightShadows.Hard, LodBias = 1f, CameraFarClip = 260f, MaxStreetLights = 4, StreetLightShadows = false, MaxResidents = 30, ResidentRadius = 60f };
                default:
                    return new Profile { TargetFrameRate = 60, ShadowDistance = 80f, SunShadows = LightShadows.Soft, LodBias = 1.5f, CameraFarClip = 350f, MaxStreetLights = 8, StreetLightShadows = false, MaxResidents = 40, ResidentRadius = 70f };
            }
        }

        public static Profile Apply(GraphicsProfile p, Camera camera, Light sun)
        {
            var profile = Get(p);
            // WebGL frame pacing is driven by the browser; targetFrameRate is ignored there.
            Application.targetFrameRate = profile.TargetFrameRate;
            QualitySettings.vSyncCount = 0;
            QualitySettings.shadowDistance = profile.ShadowDistance;
            QualitySettings.lodBias = profile.LodBias;
            if (camera != null) camera.farClipPlane = profile.CameraFarClip;
            if (sun != null) sun.shadows = profile.SunShadows;
            return profile;
        }
    }
}
