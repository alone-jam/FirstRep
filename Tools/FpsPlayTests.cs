#if UNITY_EDITOR
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using EjoyFramework.Core;
using EjoyFramework.Core.Input;
namespace FirstRep.Fps.Tests {
public class FpsPlayTests {
sealed class ControlledInput : IInputHelper {
public bool IsTouchSupported=>false;
public string held,down;public float move;
public bool GetKey(string name)=>name==held;
public bool GetKeyDown(string name)=>name==down;
public bool GetKeyUp(string name)=>false;
public float GetAxisRaw(string name)=>name=="Horizontal"?move:0;
}
static T Field<T>(object obj,string name)=>(T)obj.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(obj);
[UnityTest] public IEnumerator OriginalLevelSupportsMovementAimFireReloadAndTargetRespawn(){
var original=AssetDatabase.LoadAssetAtPath<SliceContent>("Assets/FirstRep/SliceContent.asset");Assert.NotNull(original);
var content=Object.Instantiate(original);content.artReviewComplete=true; // Test-only fixture; never marks the saved art review approved.
EditorBuildSettings.scenes=System.Array.ConvertAll(content.levelScenes,p=>new EditorBuildSettingsScene(p,true));
var boot=new GameObject("Integration fixture");boot.SetActive(false);boot.AddComponent<SliceBootstrap>().content=content;boot.SetActive(true);
FpsPlayer player=null;float deadline=Time.realtimeSinceStartup+120;
while(!player && Time.realtimeSinceStartup<deadline){player=Object.FindFirstObjectByType<FpsPlayer>();yield return null;}
Assert.NotNull(player,"Ejoy scene loading failed to create player");yield return null;
if(Field<bool>(player,"paused"))player.GetType().GetMethod("Resume",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(player,null);
Time.timeScale=1;
var input=new ControlledInput();Framework.GetModule<IInputManager>().SetHelper(input);
for(int i=0;i<20;i++)yield return null;
Assert.IsTrue(Physics.Raycast(player.transform.position+Vector3.up,Vector3.down,out var floor,10,~((1<<10)|(1<<11)),QueryTriggerInteraction.Ignore),"Original scene has no ground under spawn");
var start=player.transform.position;input.move=1;yield return new WaitForSeconds(0.4f);input.move=0;
Assert.Greater(Vector3.Distance(start,player.transform.position),0.05f,"WASD binding did not move real CharacterController");
input.held="Mouse1";yield return new WaitForSeconds(0.4f);Assert.Less(player.view.fieldOfView,65,"Aim did not narrow FOV");
input.held="Mouse0";yield return null;input.held=null;yield return null;
var magazine=Field<WeaponMagazine>(player,"magazine");Assert.AreEqual(29,magazine.Rounds,"Fire binding did not consume one round");
input.down="R";yield return null;input.down=null;yield return new WaitForSeconds(2.1f);Assert.AreEqual(30,magazine.Rounds,"Ejoy reload timer did not finish");
var target=Object.FindFirstObjectByType<TargetPractice>();Assert.NotNull(target);target.Hit(100);
Assert.IsFalse(System.Array.Exists(target.GetComponentsInChildren<Renderer>(),r=>r.enabled),"Lethal hit did not hide target");
yield return new WaitForSeconds(4.2f);Assert.IsTrue(System.Array.Exists(target.GetComponentsInChildren<Renderer>(),r=>r.enabled),"Ejoy timer did not respawn original target");
Object.Destroy(boot);Object.Destroy(player.gameObject);Object.Destroy(content);Time.timeScale=1;yield return null;
}
}
}

#endif
