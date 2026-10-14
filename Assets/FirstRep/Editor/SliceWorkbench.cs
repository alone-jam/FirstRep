using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace FirstRep.Fps.Editor
{
    public static class SliceWorkbench
    {
        public const string ContentPath = "Assets/FirstRep/SliceContent.asset";
        public const string BootstrapPath = "Assets/FirstRep/Scenes/Bootstrap.unity";

        [MenuItem("FirstRep/1. Create content binding checklist")]
        public static void CreateChecklist()
        {
            if (AssetDatabase.LoadAssetAtPath<SliceContent>(ContentPath)) return;
            var content = ScriptableObject.CreateInstance<SliceContent>();
            content.levelScenes = new[] {
                "Assets/Scenes/Level_01/Level_01_Main.unity",
                "Assets/Scenes/Level_01/Level_01_Background.unity",
                "Assets/Scenes/Level_01/Level_01_Gameplay.unity" };
            AssetDatabase.CreateAsset(content, ContentPath); AssetDatabase.SaveAssets();
            Selection.activeObject = content;
        }

        [MenuItem("FirstRep/2. Validate reviewed scene and prefab references")]
        public static void Validate()
        {
            var errors = new List<string>();
            var content = AssetDatabase.LoadAssetAtPath<SliceContent>(ContentPath);
            if (!content || !content.IsReady) errors.Add("Original art review or content binding is incomplete.");
            if (!GraphicsSettings.defaultRenderPipeline ||
                GraphicsSettings.defaultRenderPipeline.GetType().FullName != "UnityEngine.Rendering.HighDefinition.HDRenderPipelineAsset")
                errors.Add("Configure HDRP 17.6; changing to URP/Built-in is not an accepted art migration.");
            if (content)
            {
                ValidatePrefab(content.firstPersonPrefab, errors);
                ValidatePrefab(content.targetPrefab, errors);
                if (content.targetPrefab && !content.targetPrefab.GetComponentInChildren<Collider>()) errors.Add("Target has no hit collider.");
                var p = content.firstPersonPrefab ? content.firstPersonPrefab.GetComponentInChildren<FirstPersonPresentation>() : null;
                if (!p || !p.muzzle || !p.muzzleFlash || !p.arms || !p.weapon) errors.Add("First-person weapon/arms/muzzle/FX references incomplete.");
                if (p) { ValidateAnimator(p.arms, errors); ValidateAnimator(p.weapon, errors); }
                if (content.impactPrefab) ValidatePrefab(content.impactPrefab.gameObject, errors);
                foreach (string path in content.levelScenes ?? Array.Empty<string>())
                {
                    if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(path)) { errors.Add("Missing scene " + path); continue; }
                    // Do not discard the user's open scenes or unsaved modifications.
                    var scene = SceneManager.GetSceneByPath(path);
                    bool openedForCheck = !scene.IsValid() || !scene.isLoaded;
                    var activeScene = SceneManager.GetActiveScene();
                    if (openedForCheck) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                    try { foreach (var root in scene.GetRootGameObjects()) ValidateHierarchy(root, errors); }
                    finally
                    {
                        if (openedForCheck) EditorSceneManager.CloseScene(scene, true);
                        if (activeScene.IsValid() && activeScene.isLoaded) SceneManager.SetActiveScene(activeScene);
                    }
                }
                OriginalArtValidator.Validate(content, errors);
            }
            Directory.CreateDirectory("Reports");
            File.WriteAllLines("Reports/unity-reference-check.txt", new[] {
                "Unity " + Application.unityVersion,
                "Serialized references checked by the real Editor. Visual fidelity/GPU shaders/animation playback require separate PlayMode review.",
                "Errors: " + errors.Count }.Concat(errors));
            if (errors.Count != 0) throw new InvalidOperationException(string.Join("\n", errors));
        }

        private static void ValidatePrefab(GameObject prefab, List<string> errors)
        {
            if (!prefab) { errors.Add("A required authentic prefab is not bound."); return; }
            ValidateHierarchy(prefab, errors);
        }
        private static void ValidateHierarchy(GameObject root, List<string> errors)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) != 0)
                    errors.Add(root.name + "/" + t.name + " has missing scripts; do not blanket-remove render/rig/effect components.");
                foreach (var component in t.GetComponents<Component>())
                {
                    if (!component) continue;
                    // Explicitly disabled unsupported source components remain in the difference ledger.
                    if (component is Behaviour disabled && !disabled.enabled) continue;
                    if (component is Collider collider && !collider.enabled) continue;
                    if (component is Renderer) continue; // Validate visible geometry/materials below; particle trail slot is optional.
                    var serialized = new SerializedObject(component); var iterator = serialized.GetIterator();
                    while (iterator.NextVisible(true))
                        if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                            iterator.objectReferenceValue == null && iterator.objectReferenceEntityIdValue != default)
                            errors.Add(root.name + "/" + t.name + ": unresolved " + iterator.propertyPath);
                }
            }
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy) continue;
                foreach (var material in renderer is ParticleSystemRenderer ? new[] { renderer.sharedMaterial } : renderer.sharedMaterials)
                    if (!material || !material.shader || material.shader.name == "Hidden/InternalErrorShader")
                        errors.Add(root.name + "/" + renderer.name + " has missing/error shader or material.");
                if (renderer is SkinnedMeshRenderer skinned && (!skinned.sharedMesh || skinned.bones.Any(b => !b)))
                    errors.Add(root.name + "/" + renderer.name + " has missing skinned mesh/bone.");
            }
            foreach (var mesh in root.GetComponentsInChildren<MeshFilter>(true))
                if (mesh.TryGetComponent<Renderer>(out var meshRenderer) && meshRenderer.enabled && meshRenderer.gameObject.activeInHierarchy && !mesh.sharedMesh) errors.Add(root.name + "/" + mesh.name + " has no mesh.");
        }
        private static void ValidateAnimator(Animator animator, List<string> errors)
        {
            if (!animator || !animator.runtimeAnimatorController) { errors.Add("Missing animator/controller."); return; }
            foreach (var clip in animator.runtimeAnimatorController.animationClips)
            {
                if (!clip) { errors.Add("Missing animation clip."); continue; }
                foreach (var binding in AnimationUtility.GetCurveBindings(clip))
                    if (binding.type == typeof(Transform) && !string.IsNullOrEmpty(binding.path) && !animator.transform.Find(binding.path))
                        errors.Add(clip.name + ": unmatched animation transform " + binding.path);
            }
        }

        [MenuItem("FirstRep/3. Generate bootstrap after validation")]
        public static void GenerateBootstrap()
        {
            Validate();
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var root = new GameObject("FirstRep composition root");
            root.AddComponent<SliceBootstrap>().content = AssetDatabase.LoadAssetAtPath<SliceContent>(ContentPath);
            Directory.CreateDirectory("Assets/FirstRep/Scenes");
            EditorSceneManager.SaveScene(scene, BootstrapPath);
            var paths = new[] { BootstrapPath }.Concat(root.GetComponent<SliceBootstrap>().content.levelScenes);
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
            PlayerSettings.colorSpace = ColorSpace.Linear;
            var settings = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset")[0]);
            var inputHandler = settings.FindProperty("activeInputHandler");
            if (inputHandler == null) throw new InvalidOperationException("Cannot locate Active Input Handling in this Editor.");
            inputHandler.intValue = 2; // Both: upstream LegacyInputHelper workaround.
            settings.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }

        [MenuItem("FirstRep/4. Build Linux vertical slice")]
        public static void BuildLinux()
        {
            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("FIRSTREP_BUILD_TEMP")))
            {
                var info = new System.Diagnostics.ProcessStartInfo("python3", "Tools/prepare_build_temp.py") { UseShellExecute = false };
                using (var process = System.Diagnostics.Process.Start(info))
                {
                    process.WaitForExit();
                    if (process.ExitCode != 0) throw new InvalidOperationException("Build Temp redirection failed; no build started.");
                }
            }
            Validate();
            FrameworkAssetSetup.BuildResources();
            if (!File.Exists(BootstrapPath)) throw new InvalidOperationException("Generate the bootstrap first.");
            var content = AssetDatabase.LoadAssetAtPath<SliceContent>(ContentPath);
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.StandaloneLinux64, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.StandaloneLinux64, new[] { GraphicsDeviceType.Vulkan });
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { BootstrapPath }.Concat(content.levelScenes).ToArray(),
                locationPathName = "Builds/Linux/FirstRep.x86_64", target = BuildTarget.StandaloneLinux64,
                options = BuildOptions.Development | BuildOptions.CompressWithLz4HC });
            File.WriteAllText("Reports/build-summary.txt", $"Result={result.summary.result}\nSize={result.summary.totalSize}\nErrors={result.summary.totalErrors}\nWarnings={result.summary.totalWarnings}\nTime={result.summary.totalTime}\n");
            if (result.summary.result != BuildResult.Succeeded) throw new InvalidOperationException("Build failed: " + result.summary.result);
        }
    }
}
