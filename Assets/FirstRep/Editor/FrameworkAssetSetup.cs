using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using EjoyFramework.Core.Unity.Editor.Resource;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using EjoyFramework.Core.Unity;
using EjoyFramework.Core.Unity.Editor.UI.Mvvm;
using EjoyFramework.Core.Unity.Editor.CodeGen;
using Object=UnityEngine.Object;
namespace FirstRep.Fps.Editor
{
    public static class FrameworkAssetSetup
    {
        [MenuItem("FirstRep/Framework/Prepare original actor and UI prefabs")]
        public static void PrepareAssets()
        {
            Directory.CreateDirectory("Assets/FirstRep/Framework");AssetDatabase.Refresh();
            var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
            if(!content || !content.IsReady)throw new InvalidOperationException("Original reviewed content is incomplete");
            PrepareFrameworkVariant(content);
            var player=new GameObject("FirstRep Player");
            try
            {
                player.layer=10;var motor=player.AddComponent<CharacterController>();motor.height=1.8f;motor.center=Vector3.up*.9f;motor.radius=.3f;motor.stepOffset=.3f;
                var cameraObject=new GameObject("Player Camera");cameraObject.transform.SetParent(player.transform,false);cameraObject.transform.localPosition=Vector3.up*1.65f;cameraObject.tag="MainCamera";
                var camera=cameraObject.AddComponent<Camera>();camera.nearClipPlane=.03f;camera.fieldOfView=75;camera.cullingMask=~((1<<8)|(1<<12)|(1<<20)|(1<<21)|(1<<26));cameraObject.AddComponent<AudioListener>();
                var art=(GameObject)PrefabUtility.InstantiatePrefab(content.firstPersonPrefab,cameraObject.transform);
                foreach(var t in art.GetComponentsInChildren<Transform>(true))t.gameObject.layer=11;
                var logic=player.AddComponent<FpsPlayer>();logic.view=camera;logic.presentation=art.GetComponentInChildren<FirstPersonPresentation>();
                PrefabUtility.SaveAsPrefabAsset(player,SliceContent.PlayerEntityPath);
            }
            finally{Object.DestroyImmediate(player);}
            var impact=new GameObject("Original impact entity");
            try
            {
                var art=(GameObject)PrefabUtility.InstantiatePrefab(content.impactPrefab.gameObject,impact.transform);
                impact.AddComponent<ImpactEffect>().particles=art.GetComponent<ParticleSystem>();PrefabUtility.SaveAsPrefabAsset(impact,SliceContent.ImpactEntityPath);
            }
            finally{Object.DestroyImmediate(impact);}
            SaveUI<FpsHudForm>(SliceContent.HudPath);SaveUI<FpsPauseForm>(SliceContent.PausePath);SaveUI<FpsMainMenuForm>(content.MainMenuAssetPath);
            var target=PrefabUtility.LoadPrefabContents(AssetDatabase.GetAssetPath(content.targetPrefab));
            try
            {
                if(!target.GetComponent<TargetPractice>())target.AddComponent<TargetPractice>();
                content.TargetAssetPath="Assets/FirstRep/Framework/Target.prefab";
                PrefabUtility.SaveAsPrefabAsset(target,content.TargetAssetPath);
            }
            finally{PrefabUtility.UnloadPrefabContents(target);}
            content.ShotAudioPath=AssetDatabase.GetAssetPath(content.shotAudio);
            EditorUtility.SetDirty(content);AssetDatabase.SaveAssets();
            Debug.Log("FIRSTREP FRAMEWORK PREFABS PREPARED; original art remains unchanged");
        }
        static void PrepareFrameworkVariant(SliceContent content)
        {
            var source=AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.ejoy.framework/Runtime/Prefabs/EjoyFramework.prefab");
            if(!source)throw new InvalidOperationException("Official Framework prefab is missing");
            var root=(GameObject)PrefabUtility.InstantiatePrefab(source);
            try
            {
                var procedure=root.GetComponentInChildren<ProcedureComponent>(true);var serialized=new SerializedObject(procedure);
                var types=new[]{typeof(LaunchFirstRep),typeof(MainMenuOriginalLevel),typeof(LoadOriginalLevel),typeof(PlayOriginalLevel),typeof(PauseOriginalLevel),typeof(FailedOriginalLevel),typeof(ExitOriginalLevel)};
                serialized.FindProperty("m_EntranceProcedureTypeName").stringValue=typeof(LaunchFirstRep).FullName;
                var list=serialized.FindProperty("m_AvailableProcedureTypeNames");list.arraySize=types.Length;
                for(int i=0;i<types.Length;i++)list.GetArrayElementAtIndex(i).stringValue=types[i].FullName;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                root.AddComponent<SliceSessionHost>();
                content.frameworkPrefab=PrefabUtility.SaveAsPrefabAsset(root,SliceContent.FrameworkPrefabPath);
            }
            finally{Object.DestroyImmediate(root);}
        }
        static void SaveUI<T>(string path) where T:Component
        {
            var form=new GameObject(typeof(T).Name,typeof(RectTransform));
            try
            {
                form.AddComponent<T>();
                if(typeof(T)==typeof(FpsHudForm))
                {
                    TextField(form.transform,"Header","Header",new Vector2(0,1),new Vector2(20,-20),new Vector2(1000,36),TextAnchor.UpperLeft);
                    TextField(form.transform,"Instructions","Instructions",new Vector2(0,0),new Vector2(20,40),new Vector2(1200,36),TextAnchor.LowerLeft);
                    TextField(form.transform,"Crosshair","Crosshair",new Vector2(.5f,.5f),Vector2.zero,new Vector2(32,32),TextAnchor.MiddleCenter);
                }
                else if(typeof(T)==typeof(FpsPauseForm))
                {
                    TextField(form.transform,"Status","Status",new Vector2(.5f,.5f),new Vector2(0,110),new Vector2(950,130),TextAnchor.MiddleCenter);
                    MenuButton(form.transform,"Resume",new Vector2(0,25)).AddComponent<VisibilityBinder>().PropertyPath=nameof(FpsPauseViewModel.ShowResume);
                    MenuButton(form.transform,"Retry",new Vector2(0,25)).AddComponent<VisibilityBinder>().PropertyPath=nameof(FpsPauseViewModel.CanRetry);
                    MenuButton(form.transform,"MainMenu",new Vector2(0,-35)).AddComponent<VisibilityBinder>().PropertyPath=nameof(FpsPauseViewModel.ShowMainMenu);
                    MenuButton(form.transform,"Exit",new Vector2(0,-95));
                }
                else if(typeof(T)==typeof(FpsMainMenuForm))
                {
                    TextField(form.transform,"Title","Title",new Vector2(.5f,.5f),new Vector2(0,150),new Vector2(950,60),TextAnchor.MiddleCenter);
                    TextField(form.transform,"Description","Description",new Vector2(.5f,.5f),new Vector2(0,90),new Vector2(950,40),TextAnchor.MiddleCenter);
                    MenuButton(form.transform,"Start",new Vector2(0,15));MenuButton(form.transform,"Exit",new Vector2(0,-45));
                }
                PrefabUtility.SaveAsPrefabAsset(form,path);
                MvvmBindingsGenerator.WireBindersInternal(path);
            }
            finally{Object.DestroyImmediate(form);}
        }
        static Text TextField(Transform parent,string name,string property,Vector2 anchor,Vector2 position,Vector2 size,TextAnchor alignment)
        {
            var go=new GameObject(name,typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=anchor;rt.pivot=anchor;rt.anchoredPosition=position;rt.sizeDelta=size;
            var text=go.AddComponent<Text>();text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");text.fontSize=18;text.alignment=alignment;text.color=Color.white;text.raycastTarget=false;
            if(!string.IsNullOrEmpty(property))go.name="B_"+property+"_Text";
            return text;
        }
        static GameObject MenuButton(Transform parent,string command,Vector2 position)
        {
            var go=new GameObject("B_"+command+"_Button",typeof(RectTransform));go.transform.SetParent(parent,false);
            var rt=(RectTransform)go.transform;rt.anchorMin=rt.anchorMax=rt.pivot=new Vector2(.5f,.5f);rt.anchoredPosition=position;rt.sizeDelta=new Vector2(180,40);
            var image=go.AddComponent<Image>();image.color=new Color(.15f,.15f,.15f,.95f);
            var button=go.AddComponent<Button>();button.targetGraphic=image;
            var label=TextField(go.transform,"Label",null,new Vector2(.5f,.5f),Vector2.zero,new Vector2(180,40),TextAnchor.MiddleCenter);label.text=command=="MainMenu"?"Main menu":command;
            return go;
        }
        [MenuItem("FirstRep/Framework/Build framework AssetBundles for Linux")]
        public static void BuildResources()
        {
            PrepareAssets();var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
            var config=ScriptableObject.CreateInstance<AssetBundleBuildConfig>();
            try
            {
                config.OutputDirectory="Assets/StreamingAssets/AssetBundles";config.CopyToStreamingAssets=false;config.CleanBeforeBuild=false;
                config.IncludeDirectories=new List<string>{"Assets/FirstRep/Framework",Path.GetDirectoryName(content.TargetAssetPath),Path.GetDirectoryName(content.ShotAudioPath)};
                foreach(var scene in content.levelScenes){var dir=Path.GetDirectoryName(scene);if(!config.IncludeDirectories.Contains(dir))config.IncludeDirectories.Add(dir);}
                config.BuildRules.Add(new BundleBuildRule{Name="Scene assets",Priority=100,PathPatterns=new List<string>{"**/*.unity"},Strategy=BundleSplitStrategy.PerFile});
                config.ExcludePatterns.Add("**/FirstRepSession.prefab"); // Direct startup-scene dependency, not a resource-loaded actor/UI.
                config.DefaultStrategy=BundleSplitStrategy.PerDirectory;
                StaticMaterialVariantStripper.PrepareDependencies();
                var manifest=AssetBundleBuilder.BuildAll(config,BuildTarget.StandaloneLinux64);
                if(manifest.Assets==null || manifest.Assets.Count==0)throw new InvalidOperationException("Framework builder produced no resource assets");
                Debug.Log("FIRSTREP FRAMEWORK ASSETBUNDLES BUILT "+manifest.Assets.Count);
            }
            finally{Object.DestroyImmediate(config);}
        }
        public static void VerifyAndPrepare(){FrameworkDependencyCheck.Run();PrepareAssets();HelperFactoryGenerator.GenerateCli();MvvmAccessorGenerator.GenerateCli();}
        public static void ValidateBuiltResources()
        {
            const string root="Assets/StreamingAssets/AssetBundles/StandaloneLinux64";
            var manifest=JsonUtility.FromJson<EjoyFramework.Core.Resource.AssetManifest>(File.ReadAllText(Path.Combine(root,"manifest.json")));
            if(manifest.Platform!="StandaloneLinux64" || manifest.Assets.Count==0 || manifest.Bundles.Count==0)
                throw new InvalidOperationException("Prepared Linux resource manifest is empty or incompatible");
            var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
            foreach(var path in new[]{content.MainMenuAssetPath,SliceContent.HudPath,SliceContent.PausePath,SliceContent.PlayerEntityPath,SliceContent.ImpactEntityPath,content.TargetAssetPath,content.ShotAudioPath}.Concat(content.levelScenes))
                if(!manifest.Assets.Any(asset=>asset.Name==path))throw new InvalidOperationException("Prepared resources do not cover the current menu/scene workflow: "+path);
            foreach(var scene in content.levelScenes)
                if(!manifest.Assets.Any(asset=>asset.Name==scene && asset.IsScene))throw new InvalidOperationException("Scene bundle entry is missing: "+scene);
            foreach(var bundle in manifest.Bundles)
            {
                var path=Path.GetFullPath(Path.Combine(root,bundle.RelativePath));
                if(!path.StartsWith(Path.GetFullPath(root)+Path.DirectorySeparatorChar,StringComparison.Ordinal))
                    throw new InvalidOperationException("Bundle path leaves prepared resource directory");
                using(var stream=File.OpenRead(path))
                using(var md5=System.Security.Cryptography.MD5.Create())
                {
                    if(stream.Length!=bundle.Size || BitConverter.ToString(md5.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=bundle.Md5)
                        throw new InvalidOperationException("Prepared bundle integrity failed: "+bundle.Name);
                }
            }
            FrameworkDependencyCheck.Run();
            Debug.Log("FIRSTREP PREPARED ASSETBUNDLES VERIFIED "+manifest.Assets.Count);
        }
    }
}
