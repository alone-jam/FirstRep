using System;
using System.Collections.Generic;
using System.IO;
using EjoyFramework.Core.Unity.Editor.Resource;
using UnityEditor;
using UnityEngine;
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
            SaveUI<FpsHudForm>(SliceContent.HudPath);SaveUI<FpsPauseForm>(SliceContent.PausePath);
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
        static void SaveUI<T>(string path) where T:Component
        {
            var form=new GameObject(typeof(T).Name,typeof(RectTransform));
            try{form.AddComponent<T>();PrefabUtility.SaveAsPrefabAsset(form,path);}finally{Object.DestroyImmediate(form);}
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
                config.DefaultStrategy=BundleSplitStrategy.PerDirectory;
                StaticMaterialVariantStripper.PrepareDependencies();
                var manifest=AssetBundleBuilder.BuildAll(config,BuildTarget.StandaloneLinux64);
                if(manifest.Assets==null || manifest.Assets.Count==0)throw new InvalidOperationException("Framework builder produced no resource assets");
                Debug.Log("FIRSTREP FRAMEWORK ASSETBUNDLES BUILT "+manifest.Assets.Count);
            }
            finally{Object.DestroyImmediate(config);}
        }
        public static void VerifyAndPrepare(){FrameworkDependencyCheck.Run();PrepareAssets();}
    }
}
