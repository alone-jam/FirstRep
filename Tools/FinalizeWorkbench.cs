using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FirstRep.Fps.Editor {
public static class FinalizeWorkbench {
// Run only after the rendered best-effort presentation has been inspected.
public static void Prepare(){
var c=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
c.artReviewComplete=true;EditorUtility.SetDirty(c);AssetDatabase.SaveAssets();
SliceWorkbench.GenerateBootstrap();
}
public static void Audit(){
var c=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
foreach(var path in c.levelScenes)EditorSceneManager.OpenScene(path,OpenSceneMode.Additive);
var colliders=UnityEngine.Object.FindObjectsByType<MeshCollider>(FindObjectsSortMode.None);
File.WriteAllText("Reports/collision-runtime-audit.txt", "Enabled valid MeshColliders="+colliders.Count(x=>x.enabled && x.sharedMesh)+"\nEnabled missing MeshColliders="+colliders.Count(x=>x.enabled && !x.sharedMesh)+"\nOriginal collision nodes="+colliders.Count(x=>x.enabled && x.sharedMesh && x.name.StartsWith("collision_",StringComparison.OrdinalIgnoreCase))+"\n");
try{SliceWorkbench.Validate();}catch(Exception e){Debug.LogWarning(e.Message);}
}
}
}
