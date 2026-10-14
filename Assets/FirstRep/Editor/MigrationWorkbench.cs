using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
namespace FirstRep.Fps.Editor {
public static class MigrationWorkbench {
const string Output="Assets/FirstRep/Recovered";
static List<string> changes=new List<string>();
static string PathOf(Transform t) => t.parent ? PathOf(t.parent)+"/"+t.name : t.name;
static void Note(string s){changes.Add(s);}
public static void Run(){
Directory.CreateDirectory(Output);Directory.CreateDirectory("Assets/FirstRep/Scenes");
try { Configure(); Materials(); Scenes(); Presentations(); AssetDatabase.SaveAssets(); }
finally {File.WriteAllLines("Reports/migration-differences.txt",changes);}
Debug.Log("FirstRep migration phase completed");
}
static void Configure(){
var pipeline=AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(Output+"/FirstRepHDRP.asset");
if(!pipeline){pipeline=ScriptableObject.CreateInstance<HDRenderPipelineAsset>();AssetDatabase.CreateAsset(pipeline,Output+"/FirstRepHDRP.asset");}
GraphicsSettings.defaultRenderPipeline=pipeline; QualitySettings.renderPipeline=pipeline;
typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDRenderPipelineGlobalSettings").GetMethod("Ensure",BindingFlags.Static|BindingFlags.NonPublic)?.Invoke(null,new object[]{true});
PlayerSettings.colorSpace=ColorSpace.Linear;
Note("HDRP17.6: new supported pipeline/global settings; old HDRP4 settings require visual comparison.");
}
static void Materials(){
foreach(string guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Materials"})){
 string path=AssetDatabase.GUIDToAssetPath(guid); var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)continue;
 string before=m.shader ? m.shader.name : "MISSING";
 // Original HDRP material properties and texture references remain on the material.
 if(!m.shader || before.Contains("InternalError") || before.Contains("Firstperson") || before.Contains("FirstPerson")){
  m.shader=Shader.Find("HDRP/Lit");Note(path+": shader "+before+" -> HDRP/Lit; original textures retained; legacy first-person projection omitted.");
 }
 if(m.shader && m.shader.name.StartsWith("HDRP/")) HDShaderUtils.ResetMaterialKeywords(m);
 EditorUtility.SetDirty(m);
}
}
static void Clean(GameObject root){
 if(PrefabUtility.IsPartOfPrefabInstance(root) && PrefabUtility.GetOutermostPrefabInstanceRoot(root)==root)
  PrefabUtility.UnpackPrefabInstance(root,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
 foreach(var t in root.GetComponentsInChildren<Transform>(true)){
  int missing=GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject);
  if(missing>0){Note("Unsupported original component: "+PathOf(t)+" count="+missing+"; source GUID/class inventory remains in render-dependency-decisions and compatibility-audit; local Ejoy gameplay replaces old stack.");GameObjectUtility.RemoveMonoBehavioursWithMissingScript(t.gameObject);}
  foreach(var c in t.GetComponents<Component>()){
   if(!c)continue;
   if(c is Camera cam)cam.enabled=false;
   if(c is AudioListener listener)Object.DestroyImmediate(listener);
   if(c is UnityEngine.Playables.PlayableDirector director){director.enabled=false;Note("Original Timeline requires old gameplay/audio bindings: disabled "+PathOf(t));}
   if(c is MeshCollider collider && !collider.sharedMesh){collider.enabled=false;Note("Unresolved original MeshCollider disabled, not rebound: "+PathOf(t));}
   if(c is MonoBehaviour mb && mb.GetType().Name=="ProjectLODLightmaps")mb.GetType().GetMethod("SetupRenderer")?.Invoke(mb,null);
  }
 }
 foreach(var renderer in root.GetComponentsInChildren<Renderer>(true)){
  if(renderer.sharedMaterials.Any(m=>!m)){renderer.enabled=false;Note("Missing original material: renderer disabled "+PathOf(renderer.transform));}
  if(renderer is MeshRenderer && renderer.TryGetComponent<MeshFilter>(out var mf) && !mf.sharedMesh){renderer.enabled=false;Note("Missing original mesh: renderer disabled "+PathOf(renderer.transform));}
  if(renderer.gameObject.layer==20 || renderer.gameObject.layer==21 || renderer.gameObject.layer==12)renderer.enabled=false;
 }
}
static Vector3 spawn;
static void Scenes(){
var paths=new List<string>();
foreach(string name in new[]{"Level_01_Background","Level_01_Gameplay","Level_01_Main"}){
 var scene=EditorSceneManager.OpenScene("Assets/Scenes/Level_01/"+name+".unity",OpenSceneMode.Single);
 foreach(var root in scene.GetRootGameObjects()){
  if(name=="Level_01_Gameplay")foreach(var t in root.GetComponentsInChildren<Transform>(true))if(t.name=="Girl Spawn 01")spawn=t.position;
  Clean(root);
 }
 string dest="Assets/FirstRep/Scenes/"+name+"_Recovered.unity";EditorSceneManager.SaveScene(scene,dest);paths.Add(dest);
 Note(name+": preserved root count="+scene.rootCount+" renderers="+scene.GetRootGameObjects().Sum(r=>r.GetComponentsInChildren<Renderer>(true).Length));
}
var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
if(!content){content=ScriptableObject.CreateInstance<SliceContent>();AssetDatabase.CreateAsset(content,SliceWorkbench.ContentPath);}
content.levelScenes=paths.ToArray();content.spawnPosition=spawn+Vector3.up;content.spawnYaw=180;content.artReviewComplete=false;
content.targetPositions=new[]{spawn+new Vector3(0,0,-8),spawn+new Vector3(4,0,-13),spawn+new Vector3(-4,0,-18)};
EditorUtility.SetDirty(content);Note("Original Girl Spawn 01 world position="+spawn);
}
static GameObject Original(string path){var asset=AssetDatabase.LoadAssetAtPath<GameObject>(path);if(!asset)throw new Exception("Missing original prefab "+path);var go=(GameObject)PrefabUtility.InstantiatePrefab(asset);Clean(go);return go;}
static AnimationClip Clip(string contains,string clipName=null){
foreach(string guid in AssetDatabase.FindAssets("t:Model",new[]{"Assets/Animation"})){
 string path=AssetDatabase.GUIDToAssetPath(guid);if(!path.Contains(contains))continue;
 var clip=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().FirstOrDefault(c=>!c.name.StartsWith("__preview__") && (clipName==null || c.name==clipName));if(clip)return clip;
}return null;
}
static AnimatorController Controller(string name,AnimationClip idle,AnimationClip fire,AnimationClip reload){
string path=Output+"/"+name+".controller";var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
if(controller)return controller;
controller=AnimatorController.CreateAnimatorControllerAtPath(path);var machine=controller.layers[0].stateMachine;
var idleState=machine.AddState("Idle");idleState.motion=idle;machine.defaultState=idleState;
foreach(var pair in new[]{("Fire",fire),("Reload",reload)})if(pair.Item2){var state=machine.AddState(pair.Item1);state.motion=pair.Item2;var exit=state.AddTransition(idleState);exit.hasExitTime=true;exit.exitTime=1;exit.duration=0.05f;}
Note("Local Animator state wiring "+name+": original clips idle="+(idle?idle.name:"MISSING")+", fire="+(fire?fire.name:"NONE")+", reload="+(reload?reload.name:"NONE")+". Fidget/legacy graph motions not wired.");return controller;
}
static void Presentations(){
EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
var root=new GameObject("FirstRep Original First Person");
var arms=Original("Assets/Prefabs/Characters/Terraformer/Terraformer_A_1P.prefab");arms.transform.SetParent(root.transform,false);
var weapon=Original("Assets/Prefabs/Weapons/TerraformerWeapon/Item_TerraformerWeapon_1P.prefab");weapon.transform.SetParent(root.transform,false);
var aa=arms.GetComponentInChildren<Animator>();var wa=weapon.GetComponentInChildren<Animator>();
if(aa)aa.runtimeAnimatorController=Controller("Arms",Clip("Firstperson--Idle.anim"),Clip("Firstperson--Shoot.anim","Shoot_Instant"),Clip("Firstperson--Reload.anim"));
if(wa)wa.runtimeAnimatorController=Controller("Weapon",Clip("Firstperson--Idle_Weapon.anim"),Clip("Firstperson--Shoot_Weapon.anim"),Clip("Firstperson--Reload_Weapon.anim"));
var p=root.AddComponent<FirstPersonPresentation>();p.arms=aa;p.weapon=wa;p.muzzle=weapon.GetComponentsInChildren<Transform>(true).FirstOrDefault(t=>t.name=="Muzzle");p.muzzleFlash=weapon.GetComponentsInChildren<ParticleSystem>(true).FirstOrDefault(x=>x.name=="PrimFireEffect");
Note("Arms animator="+(aa?PathOf(aa.transform):"MISSING")+" weapon animator="+(wa?PathOf(wa.transform):"MISSING"));
foreach(var a in root.GetComponentsInChildren<Animator>())a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
content.firstPersonPrefab=PrefabUtility.SaveAsPrefabAsset(root,Output+"/FirstPerson.prefab");Object.DestroyImmediate(root);
var target=Original("Assets/Prefabs/Characters/Robot/Robot_A_Client.prefab");
// Local practice targets retain the original visible rig; old network/ragdoll behaviours were not imported.
foreach(var a in target.GetComponentsInChildren<Animator>())a.runtimeAnimatorController=Controller("Robot",Clip("Robot/Clips/Stand--Idle"),null,null);
var b=new Bounds(target.transform.position,Vector3.zero);foreach(var rr in target.GetComponentsInChildren<Renderer>())if(rr.enabled)b.Encapsulate(rr.bounds);
if(!target.GetComponentsInChildren<Collider>().Any(c=>c.enabled)){var hit=target.AddComponent<CapsuleCollider>();hit.center=target.transform.InverseTransformPoint(b.center);hit.height=Mathf.Max(b.size.y,1.5f);hit.radius=0.45f;Note("Robot local hit volume added for Ejoy practice target; original missing HitColl/Avatar route not reused.");}
content.targetPrefab=PrefabUtility.SaveAsPrefabAsset(target,Output+"/RobotTarget.prefab");Object.DestroyImmediate(target);
var impact=Original("Assets/Effects/Impacts/Impact_Terraformer_Weapon_A/Impact_Terraformer_Weapon_A.prefab");var saved=PrefabUtility.SaveAsPrefabAsset(impact,Output+"/Impact.prefab");content.impactPrefab=saved.GetComponentInChildren<ParticleSystem>(true);Object.DestroyImmediate(impact);
content.shotAudio=AssetDatabase.FindAssets("t:AudioClip",new[]{"Assets/Audio"}).Select(AssetDatabase.GUIDToAssetPath).Where(s=>s.Contains("Girl") && (s.Contains("Fire")||s.Contains("Shoot"))).Select(AssetDatabase.LoadAssetAtPath<AudioClip>).FirstOrDefault();
EditorUtility.SetDirty(content);Note("Shot audio="+(content.shotAudio?AssetDatabase.GetAssetPath(content.shotAudio):"MISSING"));
}
}
}
