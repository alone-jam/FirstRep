using System.IO;
using UnityEngine;
using UnityEditor;
namespace FirstRep.Fps.Editor {
public static class FixVisualDependencies {
public static void Run(){
var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
foreach(var path in new[]{"Assets/FirstRep/Recovered/FirstPerson.prefab","Assets/FirstRep/Recovered/Impact.prefab"}){
 var go=PrefabUtility.LoadPrefabContents(path);
 var presentation=go.GetComponent<FirstPersonPresentation>();
 if(presentation){ presentation.cameraAnchor=System.Array.Find(presentation.arms.GetComponentsInChildren<Transform>(),t=>t.name=="Camera"); presentation.itemAnchor=System.Array.Find(presentation.arms.GetComponentsInChildren<Transform>(),t=>t.name=="Right_Hand_Attach"); presentation.weaponRoot=presentation.weapon.transform; }
 foreach(var p in go.GetComponentsInChildren<ParticleSystemRenderer>(true)){
  var original=path.Contains("FirstPerson")?"Assets/Materials/Effects/Muzzleflashes/Muzzleflash_Terraformer_Weapon_A/Muzzleflash_1P_Terraformer_Weapon_A.mat":"Assets/Materials/Effects/Impacts/Impact_Terraformer_Weapon_A/Impact_Terraformer_Weapon_A.mat";
  var m=AssetDatabase.LoadAssetAtPath<Material>(original);
  if(!m)throw new System.Exception("Original particle material unavailable: "+original);
  p.sharedMaterial=m;p.enabled=true;
  File.AppendAllText("Reports/migration-differences.txt","\nRestored exact original particle main material: "+path+"/"+p.name+" -> "+original+". Unassigned optional trail material is not a missing main material.\n");
 }
 PrefabUtility.SaveAsPrefabAsset(go,path);PrefabUtility.UnloadPrefabContents(go);
}
AssetDatabase.SaveAssets();
}
}
}
