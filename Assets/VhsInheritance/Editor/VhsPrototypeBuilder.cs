using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace VhsInheritance.Editor
{
    public static class VhsPrototypeBuilder
    {
        public const string ConfigPath = "Assets/VhsInheritance/Resources/VhsGameConfig.asset";
        public const string ScenePath = "Assets/VhsInheritance/Scenes/VhsSalon.unity";
        public const string WindowsBuildPath = "Builds/VhsInheritance/VhsInheritance.exe";

        [MenuItem("Tools/VHS Prototype/Build Prototype Scene")]
        public static void BuildPrototypeScene()
        {
            EnsureTmpResources();
            VhsGameConfig gameConfig = AssetDatabase.LoadAssetAtPath<VhsGameConfig>(ConfigPath);
            if (gameConfig == null)
            {
                gameConfig = ScriptableObject.CreateInstance<VhsGameConfig>();
                gameConfig.ResetToPrototypeDefaults();
                AssetDatabase.CreateAsset(gameConfig, ConfigPath);
            }
            else if (gameConfig.upgrades == null || gameConfig.upgrades.Count < 10)
            {
                gameConfig.ResetToPrototypeDefaults();
                EditorUtility.SetDirty(gameConfig);
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "VhsSalon";
            GameObject root = new GameObject("VHS Inheritance Prototype");
            VhsGameController controller = root.AddComponent<VhsGameController>();
            controller.Config = gameConfig;
            EditorUtility.SetDirty(controller);
            EditorSceneManager.SaveScene(scene, ScenePath);

            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(ScenePath, true)
            };

            PlayerSettings.companyName = "Prototype Studio";
            PlayerSettings.productName = "VHS Inheritance";
            PlayerSettings.defaultScreenWidth = 1920;
            PlayerSettings.defaultScreenHeight = 1080;
            PlayerSettings.resizableWindow = true;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, "com.prototype.vhsinheritance");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeGameObject = root;
            Debug.Log($"[VHS] Прототипная сцена собрана: {ScenePath}");
        }

        [MenuItem("Tools/VHS Prototype/Build Windows Player")]
        public static void BuildWindowsPlayer()
        {
            if (!File.Exists(ScenePath))
            {
                BuildPrototypeScene();
            }

            string directory = Path.GetDirectoryName(WindowsBuildPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Unity 6.6's D3D12 player can display presentation errors over the UI.
            // This 2D prototype has no D3D12-specific requirements.
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneWindows64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneWindows64,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.Direct3D11 });

            BuildPlayerOptions options = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = WindowsBuildPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[VHS] Windows build готов: {WindowsBuildPath} ({report.summary.totalSize / (1024 * 1024f):0.0} MB)");
            }
            else
            {
                throw new BuildFailedException($"VHS build failed: {report.summary.result}");
            }
        }

        [MenuItem("Tools/VHS Prototype/Validate Prototype")]
        public static void ValidatePrototype()
        {
            VhsGameConfig gameConfig = AssetDatabase.LoadAssetAtPath<VhsGameConfig>(ConfigPath);
            if (gameConfig == null)
            {
                throw new FileNotFoundException("Balance config is missing", ConfigPath);
            }
            if (gameConfig.upgrades == null || gameConfig.upgrades.Count != 10)
            {
                throw new InvalidDataException("Balance config must contain exactly ten upgrade tracks.");
            }
            if (!File.Exists(ScenePath))
            {
                throw new FileNotFoundException("Prototype scene is missing", ScenePath);
            }
            Debug.Log("[VHS] Validation passed: scene, config and all 10 upgrade tracks are present.");
        }

        private static void EnsureTmpResources()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
            if (font == null)
            {
                TMP_PackageResourceImporter.ImportResources(true, false, false);
            }
        }
    }
}
