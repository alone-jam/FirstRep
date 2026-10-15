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
using EjoyFramework.Core.Unity;
using UnityEngine.UI;
using EjoyFramework.Core.Scene;
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
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");
var content=Object.Instantiate(original);content.artReviewComplete=true;
var boot=new GameObject("Entity isolation fixture");boot.SetActive(false);boot.AddComponent<SliceBootstrap>().content=content;boot.SetActive(true);
float deadline=Time.realtimeSinceStartup+120;
while(Framework.GetModule<IEntityManager>().EntityCount<1+content.targetPositions.Length && Time.realtimeSinceStartup<deadline)yield return null;
for(int frame=0;frame<20;frame++)yield return null;
var managed=Framework.GetModule<IEntityManager>().GetAllLoadedEntities().Where(e=>e.EntityGroup.Name=="Targets").ToArray();
var evidence=new EntityIsolationEvidence{entityCount=Framework.GetModule<IEntityManager>().EntityCount,activeTargets=Object.FindObjectsByType<TargetPractice>(FindObjectsSortMode.None).Length,uniqueTargetHandles=managed.Select(e=>e.Handle).Distinct().Count(),targetIds=managed.Select(e=>e.Id).ToArray()};
evidence.aliased=evidence.uniqueTargetHandles!=managed.Length;System.IO.File.WriteAllText("Reports/framework-three-target-isolation.json",JsonUtility.ToJson(evidence,true));
Assert.That(evidence.uniqueTargetHandles,Is.EqualTo(content.targetPositions.Length),"Simultaneous entity handles must be independent");
var first=managed[0];var second=managed[1];var third=managed[2];
var firstObject=(GameObject)first.Handle;var secondObject=(GameObject)second.Handle;var thirdObject=(GameObject)third.Handle;
Framework.GetModule<IEntityManager>().HideEntity(second.Id);
Assert.IsTrue(firstObject.activeInHierarchy && thirdObject.activeInHierarchy);Assert.IsFalse(secondObject.activeInHierarchy);
boot.GetComponent<SliceBootstrap>().Entities.ShowEntity(second.Id,content.TargetAssetPath,"Targets",0,new ActorSpawn(boot.GetComponent<SliceBootstrap>(),content.targetPositions[1],Quaternion.identity));
for(int frame=0;frame<10;frame++)yield return null;
Assert.AreSame(secondObject,Framework.GetModule<IEntityManager>().GetEntity(second.Id).Handle,"Hidden instance should be reused without borrowing other active instances");
Assert.IsTrue(firstObject.activeInHierarchy && secondObject.activeInHierarchy && thirdObject.activeInHierarchy);
Object.Destroy(content);
}
[UnityTearDown] public IEnumerator ReleaseFrameworkSession(){
foreach(var bootstrap in Object.FindObjectsByType<SliceBootstrap>(FindObjectsSortMode.None)){bootstrap.Exit();Object.Destroy(bootstrap.gameObject);}
var sessions=Object.FindObjectsByType<SliceSessionHost>(FindObjectsSortMode.None);
foreach(var session in sessions)session.BeginExit();
var statuses=sessions.Select(session=>session.ExitStatus).ToArray();
float deadline=Time.realtimeSinceStartup+120;
while(statuses.Any(status=>!status.Completed) && Time.realtimeSinceStartup<deadline)yield return null;
Assert.IsTrue(statuses.All(status=>status.Completed),"Production scene exit failed; tests must not unload or shut down on its behalf");
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");
foreach(var path in original.levelScenes)Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path).isLoaded,"Production left a real additive scene loaded");
}
static SliceBootstrap Fixture(out SliceContent content){
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");content=Object.Instantiate(original);content.targetPositions=new[]{original.targetPositions[0]};content.artReviewComplete=true;
var go=new GameObject("Production lifecycle fixture");go.SetActive(false);var boot=go.AddComponent<SliceBootstrap>();boot.content=content;go.SetActive(true);return boot;
}
static IEnumerator Ready(SliceBootstrap bootstrap){
float deadline=Time.realtimeSinceStartup+120;
while(!(Framework.GetModule<IProcedureManager>().CurrentProcedure is PlayOriginalLevel) && Time.realtimeSinceStartup<deadline)yield return null;
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PlayOriginalLevel>());
}
static IEnumerator Closed(SliceExitStatus status){
float deadline=Time.realtimeSinceStartup+120;
while(!status.Completed && Time.realtimeSinceStartup<deadline)yield return null;
Assert.IsTrue(status.Completed,status.Error??"Scene leases did not finish");Assert.IsTrue(status.ScenesReleased);
}
[UnityTest] public IEnumerator ConcurrentImpactEffectsKeepIndependentTransformsAndLifetimes(){
var boot=Fixture(out var content);yield return Ready(boot);
var a=content.spawnPosition+Vector3.up*2;var b=a+Vector3.right*2;
boot.ShowImpact(a,Quaternion.identity);boot.ShowImpact(b,Quaternion.identity);
float deadline=Time.realtimeSinceStartup+30;
while(Framework.GetModule<IEntityManager>().GetAllLoadedEntities().Count(e=>e.EntityGroup.Name=="Effects")<2 && Time.realtimeSinceStartup<deadline)yield return null;
var effects=Framework.GetModule<IEntityManager>().GetAllLoadedEntities().Where(e=>e.EntityGroup.Name=="Effects").ToArray();Assert.AreEqual(2,effects.Length);Assert.AreNotSame(effects[0].Handle,effects[1].Handle);
var first=(GameObject)effects[0].Handle;var second=(GameObject)effects[1].Handle;Assert.Greater(Vector3.Distance(first.transform.position,second.transform.position),1);
Framework.GetModule<IEntityManager>().HideEntity(effects[0].Id);Assert.IsFalse(first.activeInHierarchy);Assert.IsTrue(second.activeInHierarchy);
yield return new WaitForSeconds(5.3f);Assert.IsFalse(Framework.GetModule<IEntityManager>().HasEntity(effects[1].Id));
boot.Exit();yield return Closed(boot.ExitStatus);Object.Destroy(boot.gameObject);Object.Destroy(content);yield return null;
}
[UnityTest] public IEnumerator ProductionExitAndReentryUnloadActualAdditiveScenes(){
int baseline=UnityEngine.SceneManagement.SceneManager.sceneCount;var boot=Fixture(out var content);
for(int cycle=0;cycle<2;cycle++){
if(cycle!=0)boot.Enter();yield return Ready(boot);
Assert.AreEqual(baseline+content.levelScenes.Length,UnityEngine.SceneManagement.SceneManager.sceneCount,"Reentry duplicated real scene objects");
foreach(var path in content.levelScenes)Assert.AreEqual(1,Enumerable.Range(0,UnityEngine.SceneManagement.SceneManager.sceneCount).Count(i=>UnityEngine.SceneManagement.SceneManager.GetSceneAt(i).path==path));
boot.Pause();yield return null;
var menu=(FpsPauseForm)boot.UI.GetUIForm(SliceContent.PausePath);
menu.transform.Find("Exit").GetComponent<Button>().onClick.Invoke();
var status=boot.ExitStatus;Assert.IsTrue(status.Requested);yield return Closed(status);
Assert.AreEqual(baseline,UnityEngine.SceneManagement.SceneManager.sceneCount);
foreach(var path in content.levelScenes)Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path).isLoaded);
Assert.AreEqual(0,Object.FindObjectsByType<FpsPlayer>(FindObjectsSortMode.None).Length);
}
Object.Destroy(boot.gameObject);Object.Destroy(content);yield return null;
}
[UnityTest] public IEnumerator DestroyingBootstrapDuringNativeLoadsStillReleasesSceneLeases(){
int baseline=UnityEngine.SceneManagement.SceneManager.sceneCount;var boot=Fixture(out var content);float deadline=Time.realtimeSinceStartup+120;
while(boot.Lifetime.Scenes.Count==0 && Time.realtimeSinceStartup<deadline)yield return null;
Assert.IsTrue(boot.Lifetime.Scenes.Any(lease=>lease.State==SceneLeaseState.Loading),"Fixture did not reach an in-flight native load");
var status=boot.ExitStatus;Object.Destroy(boot.gameObject);yield return null;
Assert.IsTrue(status.Requested);yield return Closed(status);
Assert.AreEqual(baseline,UnityEngine.SceneManagement.SceneManager.sceneCount);
foreach(var path in content.levelScenes)Assert.IsFalse(UnityEngine.SceneManagement.SceneManager.GetSceneByPath(path).isLoaded);
Object.Destroy(content);yield return null;
}
[UnityTest] public IEnumerator SingleTargetIntegrationExercisesFrameworkModulesAndGameplay(){

var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");Assert.NotNull(original);
var content=Object.Instantiate(original);content.targetPositions=new[]{original.targetPositions[0]};content.artReviewComplete=true; // Test-only fixture; never marks the saved art review approved.

var boot=new GameObject("Integration fixture");boot.SetActive(false);boot.AddComponent<SliceBootstrap>().content=content;boot.SetActive(true);
FpsPlayer player=null;float deadline=Time.realtimeSinceStartup+120;
while(!player && Time.realtimeSinceStartup<deadline){player=Object.FindFirstObjectByType<FpsPlayer>();yield return null;}
Assert.NotNull(player,"Ejoy scene loading failed to create player");yield return null;
if(player.IsPaused)player.GetType().GetMethod("Resume",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
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
System.IO.File.WriteAllText("Reports/framework-single-target-isolation.json",JsonUtility.ToJson(evidence,true));
Assert.That(evidence.uniqueTargetHandles,Is.EqualTo(content.targetPositions.Length),"Upstream framework concurrently aliases target entity handles; do not mask this with a consumer pool");
Assert.That(Framework.GetModule<IResourceManager>().CachedAssetCount,Is.GreaterThanOrEqualTo(6),"Content was not pinned by framework AssetBatch");
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PlayOriginalLevel>());
Assert.IsTrue(Framework.GetModule<IUIManager>().HasUIForm(SliceContent.HudPath),"HUD bypassed IUIManager");
var originalHud=(FpsHudForm)bootstrap.UI.GetUIForm(SliceContent.HudPath);var oldModel=originalHud.ViewModel;var oldLabel=originalHud.transform.Find("Header").GetComponent<Text>();string oldText=oldLabel.text;
Assert.AreEqual(oldModel.Header,oldLabel.text,"Framework TextBinder did not apply initial view model");
bootstrap.Pause();yield return null;
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PauseOriginalLevel>());
Assert.AreEqual(0,Time.timeScale);
Assert.IsTrue(Framework.GetModule<IUIManager>().HasUIForm(SliceContent.PausePath));
oldModel.Header="CLOSED_MODEL_MUTATION";Assert.AreEqual(oldText,oldLabel.text,"Closed HUD stayed subscribed to its old model");
var pause=(FpsPauseForm)bootstrap.UI.GetUIForm(SliceContent.PausePath);var oldPauseModel=pause.ViewModel;
pause.transform.Find("Resume").GetComponent<Button>().onClick.Invoke();yield return null;
Assert.IsFalse(oldPauseModel.Resume.CanExecute(null),"Closed pause model retained a live bootstrap command");
var reopenedHud=(FpsHudForm)bootstrap.UI.GetUIForm(SliceContent.HudPath);
Assert.AreNotSame(oldModel,reopenedHud.ViewModel,"Framework OnReuseInit did not rebuild the model");
Assert.AreEqual(reopenedHud.ViewModel.Header,reopenedHud.transform.Find("Header").GetComponent<Text>().text);
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
input.down="R";yield return null;input.down=null;yield return null;yield return null;
Assert.IsTrue(reopenedHud.transform.Find("Header").GetComponent<Text>().text.Contains("RELOADING"),"MVVM reload status was lost");
yield return new WaitForSeconds(2.1f);Assert.AreEqual(30,magazine.Rounds,"Ejoy reload timer did not finish");
Assert.IsFalse(reopenedHud.transform.Find("Header").GetComponent<Text>().text.Contains("RELOADING"));
target.Hit(100);
int targetId=target.Entity.Id;
Assert.IsFalse(Framework.GetModule<IEntityManager>().HasEntity(targetId),"Lethal hit did not return target to the framework pool");
Assert.IsFalse(target.gameObject.activeInHierarchy);
yield return new WaitForSeconds(4.2f);Assert.IsTrue(Framework.GetModule<IEntityManager>().HasEntity(targetId));Assert.IsTrue(System.Array.Exists(target.GetComponentsInChildren<Renderer>(),r=>r.enabled),"Ejoy timer did not respawn original target");
bootstrap.Exit();yield return Closed(bootstrap.ExitStatus);Object.Destroy(boot);Object.Destroy(content);yield return null;
}
}
}

#endif
