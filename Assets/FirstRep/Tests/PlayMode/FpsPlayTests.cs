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
// Fault injection decorates the real helper; all other entity operations stay real.
sealed class FailingEntityHelper : IEntityHelper {
readonly DefaultEntityHelper inner=new DefaultEntityHelper(null);
readonly System.Type failedLogic;readonly bool failCreate;
public int Failures { get; private set; }
public FailingEntityHelper(System.Type failedLogic,bool failCreate=false){this.failedLogic=failedLogic;this.failCreate=failCreate;}
void RejectIfSelected(object value){if(value is GameObject go && go.GetComponent(failedLogic)!=null){Failures++;throw new FrameworkException("Injected entity display failure");}}
public object InstantiateEntity(object asset){if(!failCreate)RejectIfSelected(asset);return inner.InstantiateEntity(asset);}
public IEntity CreateEntity(object instance,IEntityGroup group,object data){if(failCreate)RejectIfSelected(instance);return inner.CreateEntity(instance,group,data);}
public void HideEntity(IEntity entity,bool shutdown,object data)=>inner.HideEntity(entity,shutdown,data);
public void UpdateEntity(IEntity entity,float elapsed,float real)=>inner.UpdateEntity(entity,elapsed,real);
public void AttachEntity(IEntity child,IEntity parent,object data)=>inner.AttachEntity(child,parent,data);
public void DetachEntity(IEntity child,IEntity parent,object data)=>inner.DetachEntity(child,parent,data);
public void ReleaseEntity(object asset,object instance)=>inner.ReleaseEntity(asset,instance);
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
[UnityTest] public IEnumerator WeaponCooldownFreezesWhilePausedAndResetsOnPooledReuse(){
var boot=Fixture(out var content);yield return Ready(boot);
Framework.GetModule<IInputManager>().SetHelper(new ControlledInput());
var player=((GameObject)boot.Entities.GetEntity(1).Handle).GetComponent<FpsPlayer>();
var magazine=Field<WeaponMagazine>(player,"magazine");
Assert.IsTrue(magazine.TryFire());Assert.IsFalse(magazine.TryFire());Assert.AreEqual(29,magazine.Rounds);
boot.Pause();yield return new WaitForSecondsRealtime(0.2f);
Assert.AreEqual(0,Time.timeScale);Assert.IsFalse(magazine.TryFire(),"Entity owner advanced ability cooldown during pause");Assert.AreEqual(29,magazine.Rounds);
boot.Resume();yield return new WaitForSeconds(0.14f);
Assert.IsTrue(magazine.TryFire(),"Entity update did not tick the real AbilitySystem");
var handle=player.gameObject;boot.Entities.HideEntity(1);
boot.Entities.ShowEntity(2,SliceContent.PlayerEntityPath,"Players",0,new ActorSpawn(boot,content.spawnPosition,Quaternion.identity));
float deadline=Time.realtimeSinceStartup+30;
while(!boot.Entities.HasEntity(2) && Time.realtimeSinceStartup<deadline)yield return null;
Assert.IsTrue(boot.Entities.HasEntity(2));Assert.AreSame(handle,boot.Entities.GetEntity(2).Handle);
var reused=((GameObject)boot.Entities.GetEntity(2).Handle).GetComponent<FpsPlayer>();
var fresh=Field<WeaponMagazine>(reused,"magazine");Assert.AreNotSame(magazine,fresh);Assert.AreEqual(30,fresh.Rounds);Assert.IsTrue(fresh.TryFire(),"Previous life retained a cooldown");
Object.Destroy(content);
}
[UnityTest] public IEnumerator UnitDamageEventsAndPooledIdentityDoNotRetainAStaleRespawn(){
var boot=Fixture(out var content);yield return Ready(boot);
Framework.GetModule<IInputManager>().SetHelper(new ControlledInput());
var target=Object.FindFirstObjectByType<TargetPractice>();var oldUnit=target.Unit;int oldId=target.Entity.Id;
Assert.AreEqual(oldId,oldUnit.Id);Assert.AreEqual(100,target.Health);
int deaths=0,source=-1;float applied=0;
oldUnit.OnDamaged+=(unit,amount,attacker)=>{applied=amount;source=attacker;};
oldUnit.OnDied+=(unit,attacker)=>{deaths++;source=attacker;};
var player=((GameObject)boot.Entities.GetEntity(1).Handle).GetComponent<FpsPlayer>();
target.Hit(-25,1);Assert.AreEqual(100,target.Health);
target.Hit(25,1);Assert.AreEqual(75,target.Health);Assert.AreEqual(25,applied);Assert.AreEqual(1,source);
target.Hit(1000,1);Assert.AreEqual(0,target.Health);Assert.AreEqual(75,applied);Assert.AreEqual(1,deaths);
target.Hit(1000,1);Assert.AreEqual(1,deaths);Assert.AreEqual(1,Field<int>(player,"kills"));
Assert.IsFalse(boot.Entities.HasEntity(oldId));
int newId=oldId+1;
boot.Entities.ShowEntity(newId,content.TargetAssetPath,"Targets",0,new ActorSpawn(boot,content.targetPositions[0],Quaternion.identity));
float deadline=Time.realtimeSinceStartup+30;
while(!boot.Entities.HasEntity(newId) && Time.realtimeSinceStartup<deadline)yield return null;
Assert.IsTrue(boot.Entities.HasEntity(newId));Assert.AreSame(target.gameObject,boot.Entities.GetEntity(newId).Handle);
Assert.AreNotSame(oldUnit,target.Unit);Assert.AreEqual(newId,target.Unit.Id);Assert.AreEqual(100,target.Health);
// The detached model can no longer publish hits or hide the reused Unity entity.
oldUnit.ConfigureHealth(100);oldUnit.TakeDamage(100,1);
Assert.AreEqual(1,Field<int>(player,"kills"));Assert.IsTrue(boot.Entities.HasEntity(newId));
yield return new WaitForSeconds(4.2f);
Assert.IsFalse(boot.Entities.HasEntity(oldId),"A cancelled prior-life timer respawned its old ID");
Assert.IsTrue(boot.Entities.HasEntity(newId));Assert.AreEqual(100,target.Health);
Object.Destroy(content);
}
[UnityTest] public IEnumerator ActorInstantiationFailureShowsFrameworkMenuAndAllowsExitAndReentry()=>FailedActorSession(false);
[UnityTest] public IEnumerator ActorCreationFailureShowsFrameworkMenuAndAllowsExitAndReentry()=>FailedActorSession(true);
static IEnumerator FailedActorSession(bool failCreate){
int baseline=UnityEngine.SceneManagement.SceneManager.sceneCount;
var boot=Fixture(out var content);var entities=Framework.GetModule<IEntityManager>();
var helper=new FailingEntityHelper(typeof(TargetPractice),failCreate);entities.SetEntityHelper(helper);
int reportedFailures=0;entities.ShowEntityFailure+=(sender,error)=>reportedFailures++;
LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("ShowEntity failed:.*Injected entity display failure"));
LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("FirstRep framework integration failed:.*Injected entity display failure"));
float deadline=Time.realtimeSinceStartup+120;
while(!(Framework.GetModule<IProcedureManager>().CurrentProcedure is FailedOriginalLevel) && Time.realtimeSinceStartup<deadline)yield return null;
Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<FailedOriginalLevel>(),"Entity failure left the real load procedure waiting forever");
Assert.AreEqual(1,helper.Failures);Assert.AreEqual(1,reportedFailures);StringAssert.Contains("Injected entity display failure",boot.Failure);
Assert.AreEqual(0,entities.EntityCount);Assert.IsTrue(boot.IsPaused);
deadline=Time.realtimeSinceStartup+30;
while(boot.UI.GetUIForm(SliceContent.PausePath)==null && Time.realtimeSinceStartup<deadline)yield return null;
var menu=(FpsPauseForm)boot.UI.GetUIForm(SliceContent.PausePath);Assert.IsNotNull(menu,"Failure did not use the preloaded framework UI form");
StringAssert.Contains("LEVEL LOAD FAILED",menu.transform.Find("Status").GetComponent<Text>().text);
StringAssert.Contains("Injected entity display failure",menu.transform.Find("Status").GetComponent<Text>().text);
Assert.IsFalse(menu.transform.Find("Resume").GetComponent<Button>().interactable);Assert.IsFalse(menu.ViewModel.Resume.CanExecute(null));
menu.transform.Find("Resume").GetComponent<Button>().onClick.Invoke();Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<FailedOriginalLevel>());
var model=menu.ViewModel;var exit=boot.ExitStatus;Assert.IsTrue(menu.transform.Find("Exit").GetComponent<Button>().interactable);
menu.transform.Find("Exit").GetComponent<Button>().onClick.Invoke();yield return Closed(exit);
Assert.AreEqual(baseline,UnityEngine.SceneManagement.SceneManager.sceneCount);Assert.IsFalse(model.Exit.CanExecute(null));
boot.Enter();yield return Ready(boot);Assert.IsNull(boot.Failure);Assert.AreEqual(2,boot.Entities.EntityCount);
boot.Exit();yield return Closed(boot.ExitStatus);Object.Destroy(boot.gameObject);Object.Destroy(content);yield return null;
}
[UnityTest] public IEnumerator EffectFailureAfterLoadDoesNotTriggerAnExpiredLoadListener(){
var boot=Fixture(out var content);yield return Ready(boot);
var helper=new FailingEntityHelper(typeof(ImpactEffect));Framework.GetModule<IEntityManager>().SetEntityHelper(helper);
LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex("ShowEntity failed:.*Injected entity display failure"));
boot.ShowImpact(content.spawnPosition+Vector3.up,Quaternion.identity);
float deadline=Time.realtimeSinceStartup+30;
while(helper.Failures==0 && Time.realtimeSinceStartup<deadline)yield return null;
for(int i=0;i<5;i++)yield return null;
Assert.AreEqual(1,helper.Failures);Assert.IsNull(boot.Failure);Assert.That(Framework.GetModule<IProcedureManager>().CurrentProcedure,Is.TypeOf<PlayOriginalLevel>());
Object.Destroy(content);
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
Assert.AreEqual("PAUSED",pause.transform.Find("Status").GetComponent<Text>().text);Assert.IsTrue(pause.transform.Find("Resume").GetComponent<Button>().interactable);
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
int shotSource=-1;target.Unit.OnDamaged+=(unit,amount,source)=>shotSource=source;
input.held="Mouse0";yield return null;input.held=null;yield return null;
var magazine=Field<WeaponMagazine>(player,"magazine");Assert.AreEqual(29,magazine.Rounds,"Fire binding did not consume one round");
Assert.AreEqual(75,target.Health,"Real hitscan did not damage the framework UnitModel");
Assert.AreEqual(player.Entity.Id,shotSource,"Actual hitscan did not propagate its framework entity identity");
input.down="R";yield return null;input.down=null;yield return null;yield return null;
Assert.IsTrue(reopenedHud.transform.Find("Header").GetComponent<Text>().text.Contains("RELOADING"),"MVVM reload status was lost");
input.held="Mouse0";yield return new WaitForSeconds(0.2f);input.held=null;Assert.AreEqual(29,magazine.Rounds,"Held fire bypassed the framework reload tag");
yield return new WaitForSeconds(2.1f);Assert.AreEqual(30,magazine.Rounds,"Ejoy reload timer did not finish");
Assert.IsFalse(reopenedHud.transform.Find("Header").GetComponent<Text>().text.Contains("RELOADING"));
target.Hit(100);
int targetId=target.Entity.Id;
Assert.IsFalse(Framework.GetModule<IEntityManager>().HasEntity(targetId),"Lethal hit did not return target to the framework pool");
Assert.IsFalse(target.gameObject.activeInHierarchy);
yield return new WaitForSeconds(4.2f);Assert.IsTrue(Framework.GetModule<IEntityManager>().HasEntity(targetId));Assert.AreEqual(100,target.Health);Assert.AreEqual(targetId,target.Unit.Id);Assert.IsTrue(System.Array.Exists(target.GetComponentsInChildren<Renderer>(),r=>r.enabled),"Ejoy timer did not respawn original target");
bootstrap.Exit();yield return Closed(bootstrap.ExitStatus);Object.Destroy(boot);Object.Destroy(content);yield return null;
}
}
}

#endif
