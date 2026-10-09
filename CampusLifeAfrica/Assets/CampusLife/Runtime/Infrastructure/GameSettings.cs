using System;
using CampusLife.Core.Serialization;
using CampusLife.Runtime.Rendering;
using UnityEngine;

namespace CampusLife.Runtime.Infrastructure
{
    public enum GraphicsProfile { Low, Medium, High }
    public enum TouchControlsMode { Auto, On, Off }

    [Serializable]
    public class GameSettings
    {
        public float lookSensitivity = 1f;
        public bool invertY;
        public bool reducedMotion;
        public bool showGuideMarker = true;
        public bool showFps;
        public GraphicsProfile graphics = GraphicsProfile.Medium;
        public TouchControlsMode touchControls = TouchControlsMode.Auto;
        public float uiScale = 1f;
        public bool hasChosenGraphics;
    }

    /// <summary>Loads and stores player settings in PlayerPrefs (works on every platform, including WebGL).</summary>
    public static class SettingsStore
    {
        const string Key = "cla.settings.v1";

        public static GameSettings Load()
        {
            GameSettings settings = null;
            if (PlayerPrefs.HasKey(Key))
            {
                try { settings = FieldJson.Deserialize<GameSettings>(PlayerPrefs.GetString(Key)); }
                catch (FormatException e) { Debug.LogWarning($"[Settings] Ignoring damaged settings: {e.Message}"); }
            }
            settings = settings ?? new GameSettings();
            if (!settings.hasChosenGraphics) settings.graphics = GraphicsProfiles.RecommendedForPlatform();
            settings.lookSensitivity = Mathf.Clamp(settings.lookSensitivity, 0.2f, 3f);
            settings.uiScale = Mathf.Clamp(settings.uiScale, 0.8f, 1.5f);
            return settings;
        }

        public static void Save(GameSettings settings)
        {
            PlayerPrefs.SetString(Key, FieldJson.Serialize(settings));
            PlayerPrefs.Save();
        }

        public static bool TouchControlsActive(GameSettings settings)
        {
            switch (settings.touchControls)
            {
                case TouchControlsMode.On: return true;
                case TouchControlsMode.Off: return false;
                default: return Application.isMobilePlatform || Player.PlayerInputReader.TouchPreferred();
            }
        }
    }
}
