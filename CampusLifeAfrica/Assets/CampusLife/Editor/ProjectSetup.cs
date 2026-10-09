using System.IO;
using CampusLife.Runtime;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
#if CAMPUS_URP
using UnityEngine.Rendering.Universal;
#endif

namespace CampusLife.EditorTools
{
    /// <summary>
    /// One-click project configuration: URP pipeline asset, shader-carrying
    /// materials (so procedural materials survive build stripping), the boot
    /// scene, build settings and platform player settings. Safe to run repeatedly.
    /// </summary>
    public static class ProjectSetup
    {
        public const string ScenePath = "Assets/Scenes/Campus.unity";
        const string SettingsFolder = "Assets/Settings";
        const string ResourcesFolder = "Assets/CampusLife/Resources";

        [MenuItem("Campus Life Africa/Set Up Project")]
        public static void SetUpAll()
        {
            EnsureUrp();
            EnsureShaderMaterials();
            EnsureScene();
            EnsurePlayerSettings();
            AssetDatabase.SaveAssets();
            Debug.Log("[Campus Life Africa] Project setup complete. Open Assets/Scenes/Campus.unity and press Play.");
        }

        public static void EnsureUrp()
        {
#if CAMPUS_URP
            if (GraphicsSettings.defaultRenderPipeline != null) return;
            Directory.CreateDirectory(SettingsFolder);
            var rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
            AssetDatabase.CreateAsset(rendererData, SettingsFolder + "/CampusURP_Renderer.asset");
            var asset = UniversalRenderPipelineAsset.Create(rendererData);
            asset.shadowDistance = 60f;
            asset.supportsHDR = true;
            AssetDatabase.CreateAsset(asset, SettingsFolder + "/CampusURP.asset");
            GraphicsSettings.defaultRenderPipeline = asset;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = asset;
            }
            QualitySettings.SetQualityLevel(current, false);
            Debug.Log("[Campus Life Africa] Created and assigned a URP pipeline asset.");
#else
            Debug.LogWarning("[Campus Life Africa] URP package not installed; the game will render with the built-in pipeline.");
#endif
        }

        public static void EnsureShaderMaterials()
        {
            Directory.CreateDirectory(ResourcesFolder);
            CreateMaterial("CampusLitURP", "Universal Render Pipeline/Lit", false);
            CreateMaterial("CampusLitBuiltin", "Standard", false);
            // Emissive twins keep the _EMISSION shader variant in player builds (lamps, windows, guide marker).
            CreateMaterial("CampusLitURP_Emissive", "Universal Render Pipeline/Lit", true);
            CreateMaterial("CampusLitBuiltin_Emissive", "Standard", true);
        }

        static void CreateMaterial(string name, string shaderName, bool emissive)
        {
            string path = $"{ResourcesFolder}/{name}.mat";
            if (File.Exists(path)) return;
            var shader = Shader.Find(shaderName);
            if (shader == null) return;
            var mat = new Material(shader) { enableInstancing = true };
            if (emissive)
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", Color.white);
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            AssetDatabase.CreateAsset(mat, path);
        }

        public static void EnsureScene()
        {
            if (!File.Exists(ScenePath))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
                var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                new GameObject("CampusLifeAfrica").AddComponent<GameBootstrap>();
                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        public static void EnsurePlayerSettings()
        {
            PlayerSettings.companyName = "Campus Life Africa";
            PlayerSettings.productName = "Campus Life Africa";
            PlayerSettings.bundleVersion = GameBootstrap.GameVersion;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.campuslifeafrica.game");
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.campuslifeafrica.game");

            // Landscape-only on phones and tablets.
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
            PlayerSettings.allowedAutorotateToPortrait = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = true;
            PlayerSettings.allowedAutorotateToLandscapeRight = true;

            // WebGL: compressed build that still works on servers without Brotli headers.
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.runInBackground = false;
        }
    }
}
