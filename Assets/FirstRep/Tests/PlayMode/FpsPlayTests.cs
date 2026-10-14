#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using EjoyFramework.Core;
using EjoyFramework.Core.Input;
using EjoyFramework.Core.Entity;
using EjoyFramework.Core.Procedure;
using EjoyFramework.Core.Resource;
using EjoyFramework.Core.UI;
namespace FirstRep.Fps.Tests {
[PrebuildSetup(typeof(FpsPlayTests.SceneListSetup))]
public class FpsPlayTests {
public class SceneListSetup : IPrebuildSetup { public void Setup(){ var c=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");var paths=new System.Collections.Generic.List<string>();if(System.IO.File.Exists("Assets/FirstRep/Scenes/Bootstrap.unity"))paths.Add("Assets/FirstRep/Scenes/Bootstrap.unity");paths.AddRange(c.levelScenes);EditorBuildSettings.scenes=paths.ConvertAll(p=>new EditorBuildSettingsScene(p,true)).ToArray();} }
sealed class ControlledInput : IInputHelper {
public bool IsTouchSupported=>false;
public string held,down;public float move;
public bool GetKey(string name)=>name==held;
public bool GetKeyDown(string name)=>name==down;
public bool GetKeyUp(string name)=>false;
public float GetAxisRaw(string name)=>name=="Horizontal"?move:0;
}
[System.Serializable] sealed class EntityIsolationEvidence {public int entityCount,activeTargets,uniqueTargetHandles;public bool aliased;public int[] targetIds;}
static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
[UnityTest] public IEnumerator ConcurrentSamePrefabEntitiesHaveIndependentHandles(){
Framework.Shutdown();
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");
var content=Object.Instantiate(original);content.artReviewComplete=true;
LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("FirstRep framework integration failed:.*issue #17"));
var boot=new GameObject("Entity isolation fixture");boot.SetActive(false);boot.AddComponent<SliceBootstrap>().content=content;boot.SetActive(true);
float deadline=Time.realtimeSinceStartup+120;
while(Framework.GetModule<IEntityManager>().EntityCount<1+content.targetPositions.Length && Time.realtimeSinceStartup<deadline)yield return null;
for(int frame=0;frame<20;frame++)yield return null;
var managed=Framework.GetModule<IEntityManager>().GetAllLoadedEntities().Where(e=>e.EntityGroup.Name=="Targets").ToArray();
var evidence=new EntityIsolationEvidence{entityCount=Framework.GetModule<IEntityManager>().EntityCount,activeTargets=Object.FindObjectsByType<TargetPractice>(FindObjectsSortMode.None).Length,uniqueTargetHandles=managed.Select(e=>e.Handle).Distinct().Count(),targetIds=managed.Select(e=>e.Id).ToArray()};
evidence.aliased=evidence.uniqueTargetHandles!=managed.Length;System.IO.File.WriteAllText("Reports/framework-entity-isolation.json",JsonUtility.ToJson(evidence,true));
Assert.That(evidence.uniqueTargetHandles,Is.EqualTo(content.targetPositions.Length),"Upstream #17: simultaneous entity handles must be independent; no consumer pool workaround");
Object.Destroy(content);
}
[UnityTearDown] public IEnumerator ReleaseFrameworkSession(){
foreach(var bootstrap in Object.FindObjectsByType<SliceBootstrap>(FindObjectsSortMode.None))Object.Destroy(bootstrap.gameObject);
yield return null;Framework.Shutdown();Time.timeScale=1;
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");
foreach(var path in original.levelScenes){var scene=UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path);if(scene.IsValid() && scene.isLoaded)yield return UnityEngine.SceneManagement.SceneManager.UnloadSceneAsync(scene);}
}
[UnityTest] public IEnumerator SingleTargetIntegrationExercisesFrameworkModulesAndGameplay(){
Framework.Shutdown(); // Isolate this fixture from package tests that keep module state.
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");Assert.NotNull(original);
var content=Object.Instantiate(original);content.targetPositions=new[]{original.targetPositions[0]};content.artReviewComplete=true; // Test-only fixture; never marks the saved art review approved.

var boot=new GameObject("Integration fixture");boot.SetActive(false);boot.AddComponent<SliceBootstrap>().content=content;boot.SetActive(true);
FpsPlayer player=null;float deadline=Time.realtimeSinceStartup+120;
while(!player && Time.realtimeSinceStartup<deadline){player=Object.FindFirstObjectByType<FpsPlayer>();yield return null;}
Assert.NotNull(player,"Ejoy scene loading failed to create player");yield return null;
if(player.IsPaused)player.GetType().GetMethod("Resume",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
Time.timeScale=1;
ShaderUtil.allowAsyncCompilation=false;
var input=new ControlledInput();Framework.GetModule<IInputManager>().SetHelper(input);
for(int i=0;i<20;i++)yield return null;
var bootstrap=boot.GetComponent<SliceBootstrap>();
Assert.That(player.Entity,Is.Not.Null,"Player bypassed the framework entity lifecycle");
Assert.That(player.Entity.Id,Is.EqualTo(1));
Assert.That(Framework.GetModule<IEntityManager>().EntityCount,Is.EqualTo(1+content.targetPositions.Length));
var managedTargets=Framework.GetModule<IEntityManager>().GetAllLoadedEntities().Where(e=>e.EntityGroup.Name=="Targets").ToArray();
var evidence=new EntityIsolationEvidence{entityCount=Framework.GetModule<IEntityManager>().EntityCount,activeTargets=Object.FindObjectsByType<TargetPractice>(FindObjectsSortMode.None).Length,uniqueTargetHandles=managedTargets.Select(e=>e.Handle).Distinct().Count(),targetIds=managedTargets.Select(e=>e.Id).ToArray()};
evidence.aliased=evidence.uniqueTargetHandles!=managedTargets.Length;
System.IO.File.WriteAllText("Reports/framework-entity-isolation.json",JsonUtility.ToJson(evidence,true));
Assert.That(evidence.uniqueTargetHandles,Is.EqualTo(content.targetPositions.Length),"Upstream framework concurrently aliases target entity handles; do not mask this with a consumer pool");
Assert.That(Framework.GetModule<IResourceManager>().CachedAssetCount,Is.GreaterThanOrEqualTo(6),"Content was not pinned by framework AssetBatch");
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PlayOriginalLevel>());
Assert.IsTrue(Framework.GetModule<IUIManager>().HasUIForm(SliceContent.HudPath),"HUD bypassed IUIManager");
bootstrap.Pause();yield return null;
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PauseOriginalLevel>());
Assert.AreEqual(0,Time.timeScale);
Assert.IsTrue(Framework.GetModule<IUIManager>().HasUIForm(SliceContent.PausePath));
bootstrap.Resume();yield return null;
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PlayOriginalLevel>());
Assert.AreEqual(1,Time.timeScale);
Assert.IsTrue(Physics.Raycast(player.transform.position+Vector3.up,Vector3.down,out var floor,10,~((1<<10)|(1<<11)),QueryTriggerInteraction.Ignore),"Original scene has no ground under spawn");
if(SystemInfo.graphicsDeviceType != UnityEngine.Rendering.GraphicsDeviceType.Null){
var rt=new RenderTexture(640,360,24);rt.Create();player.view.targetTexture=rt;
for(int frame=0;frame<8;frame++)yield return null;
RenderTexture.active=rt;var capture=new Texture2D(640,360,TextureFormat.RGB24,false);capture.ReadPixels(new Rect(0,0,640,360),0,0);capture.Apply();Assert.IsTrue(System.Array.Exists(capture.GetPixels(),c=>c.maxColorComponent>0.05f),"Runtime camera renders only black");System.IO.File.WriteAllBytes("Reports/Visual/playmode.png",capture.EncodeToPNG());RenderTexture.active=null;player.view.targetTexture=null;Object.Destroy(rt);Object.Destroy(capture);
}
var start=player.transform.position;input.move=1;yield return new WaitForSeconds(0.4f);input.move=0;
Assert.Greater(Vector3.Distance(start,player.transform.position),0.05f,"WASD binding did not move real CharacterController");
float beforeJump=player.transform.position.y;input.down="Space";yield return null;input.down=null;yield return new WaitForSeconds(0.15f);Assert.Greater(player.transform.position.y,beforeJump+0.05f,"Jump binding did not lift the controller");yield return new WaitForSeconds(0.8f);
input.held="Mouse1";yield return new WaitForSeconds(0.4f);Assert.Less(player.view.fieldOfView,65,"Aim did not narrow FOV");
var target=Object.FindFirstObjectByType<TargetPractice>();Assert.NotNull(target);
var hitVolume=target.GetComponent<Collider>();Assert.NotNull(hitVolume);
target.transform.position += player.view.transform.position+player.view.transform.forward*1.5f-hitVolume.bounds.center;Physics.SyncTransforms();
input.held="Mouse0";yield return null;input.held=null;yield return null;
var magazine=Field<WeaponMagazine>(player,"magazine");Assert.AreEqual(29,magazine.Rounds,"Fire binding did not consume one round");
var health=Field<object>(target,"health");Assert.Less((float)health.GetType().GetProperty("CurrentValue").GetValue(health),100,"Real hitscan did not damage target");
input.down="R";yield return null;input.down=null;yield return new WaitForSeconds(2.1f);Assert.AreEqual(30,magazine.Rounds,"Ejoy reload timer did not finish");
target.Hit(100);
int targetId=target.Entity.Id;
Assert.IsFalse(Framework.GetModule<IEntityManager>().HasEntity(targetId),"Lethal hit did not return target to the framework pool");
Assert.IsFalse(target.gameObject.activeInHierarchy);
yield return new WaitForSeconds(4.2f);Assert.IsTrue(Framework.GetModule<IEntityManager>().HasEntity(targetId));Assert.IsTrue(System.Array.Exists(target.GetComponentsInChildren<Renderer>(),r=>r.enabled),"Ejoy timer did not respawn original target");
Object.Destroy(boot);Object.Destroy(content);Time.timeScale=1;yield return null;
}
}
}

#endif
