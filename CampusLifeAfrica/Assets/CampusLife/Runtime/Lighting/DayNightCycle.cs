using System.Collections.Generic;
using CampusLife.Core.Time;
using UnityEngine;

namespace CampusLife.Runtime.Lighting
{
    /// <summary>
    /// Drives sun direction/colour, ambient light, fog and street lights from
    /// the game clock. Sunrise is around 06:00 and sunset around 18:00, close
    /// to Ghana's near-equatorial day length all year round.
    /// </summary>
    public sealed class DayNightCycle : MonoBehaviour
    {
        public Light Sun;
        public Camera Camera;
        public System.Func<GameClock> Clock;

        readonly List<Light> _streetLights = new List<Light>();
        readonly List<Renderer> _lampBulbs = new List<Renderer>();
        Material _bulbOn, _bulbOff;
        bool _lightsOn;
        int _maxStreetLights = 8;
        bool _forceInterior;

        static readonly Color NightSky = new Color(0.05f, 0.07f, 0.15f);
        static readonly Color DawnSky = new Color(0.95f, 0.62f, 0.45f);
        static readonly Color DaySky = new Color(0.55f, 0.75f, 0.95f);
        static readonly Color NightAmbient = new Color(0.12f, 0.14f, 0.22f);
        static readonly Color DayAmbient = new Color(0.62f, 0.62f, 0.6f);
        static readonly Color DawnSun = new Color(1f, 0.62f, 0.38f);
        static readonly Color NoonSun = new Color(1f, 0.96f, 0.88f);

        public bool StreetLightsOn => _lightsOn;

        public void RegisterStreetLight(Light light, Renderer bulb)
        {
            _streetLights.Add(light);
            if (bulb != null) _lampBulbs.Add(bulb);
        }

        public void SetBulbMaterials(Material on, Material off)
        {
            _bulbOn = on;
            _bulbOff = off;
        }

        public void SetMaxStreetLights(int max)
        {
            _maxStreetLights = max;
            ApplyStreetLights(_lightsOn, force: true);
        }

        /// <summary>Interiors keep a fixed, warm indoor ambience regardless of the hour.</summary>
        public void SetInterior(bool interior) => _forceInterior = interior;

        void Update()
        {
            var clock = Clock?.Invoke();
            float hours = clock != null ? clock.MinuteOfDay / 60f : 10f;
            UpdateLighting(hours);
        }

        public void UpdateLighting(float hours)
        {
            // Sun elevation: 0° at 06:00, 90° at 12:00, 180° at 18:00.
            float elevation = (hours - 6f) / 12f * 180f;
            float daylight = Mathf.Clamp01(Mathf.Sin(elevation * Mathf.Deg2Rad) * 1.4f + 0.1f);
            if (elevation < 0f || elevation > 180f) daylight = 0f;
            float lowSun = 1f - Mathf.Clamp01(Mathf.Sin(Mathf.Clamp(elevation, 0f, 180f) * Mathf.Deg2Rad) * 2.2f);

            if (Sun != null)
            {
                // Interiors are lit by their own lamps; the sun would leak through roofs without shadows.
                Sun.enabled = !_forceInterior;
                Sun.transform.rotation = Quaternion.Euler(Mathf.Clamp(elevation, -10f, 190f), -35f, 0f);
                Sun.intensity = Mathf.Lerp(0.04f, 1.25f, daylight);
                Sun.color = daylight > 0f ? Color.Lerp(NoonSun, DawnSun, lowSun) : new Color(0.55f, 0.62f, 0.9f);
                if (daylight <= 0f) Sun.transform.rotation = Quaternion.Euler(50f, 150f, 0f); // moonlight
            }

            Color sky = daylight <= 0f ? NightSky : Color.Lerp(DaySky, DawnSky, lowSun * 0.8f);
            sky = Color.Lerp(NightSky, sky, Mathf.Clamp01(daylight * 2f));
            Color ambient = Color.Lerp(NightAmbient, DayAmbient, daylight);
            if (_forceInterior) ambient = new Color(0.5f, 0.46f, 0.42f);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = _forceInterior ? ambient : Color.Lerp(ambient, sky, 0.35f);
            RenderSettings.ambientEquatorColor = ambient;
            RenderSettings.ambientGroundColor = ambient * 0.55f;
            RenderSettings.fog = !_forceInterior;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 260f;
            RenderSettings.fogColor = Color.Lerp(sky, ambient, 0.4f);

            if (Camera != null)
            {
                Camera.clearFlags = CameraClearFlags.SolidColor;
                Camera.backgroundColor = _forceInterior ? new Color(0.1f, 0.09f, 0.08f) : sky;
            }

            bool on = hours >= 18.25f || hours < 6f;
            if (on != _lightsOn) ApplyStreetLights(on, force: false);
        }

        void ApplyStreetLights(bool on, bool force)
        {
            if (!force && on == _lightsOn) return;
            _lightsOn = on;
            for (int i = 0; i < _streetLights.Count; i++)
                if (_streetLights[i] != null) _streetLights[i].enabled = on && i < _maxStreetLights;
            if (_bulbOn != null && _bulbOff != null)
                foreach (var r in _lampBulbs)
                    if (r != null) r.sharedMaterial = on ? _bulbOn : _bulbOff;
        }
    }
}
