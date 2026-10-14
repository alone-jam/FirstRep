#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using EjoyFramework.Core;
using EjoyFramework.Core.Timer;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace FirstRep.Fps {
// Opt-in executable verification. Normal gameplay never creates this component.
public sealed class BuildSmokeProbe : MonoBehaviour {
[Serializable] sealed class Result {public bool passed;public string unity,device,api,error;public int scenes,targets,nonBlackPixels,totalPixels;public bool ground;}
float deadline;bool scheduled;string output;Camera view;RenderTexture target;
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){
var args=Environment.GetCommandLineArgs();int index=Array.IndexOf(args,"-firstrep-smoke-test");if(index<0)return;
var go=new GameObject("Development build verification");DontDestroyOnLoad(go);var probe=go.AddComponent<BuildSmokeProbe>();probe.output=index+1<args.Length?args[index+1]:"player-smoke.json";probe.deadline=Time.realtimeSinceStartup+180;
}
void Update(){
if(scheduled)return;
var player=FindFirstObjectByType<FpsPlayer>();
if(!player){if(Time.realtimeSinceStartup>deadline)Finish("Player did not load in 180 seconds");return;}
scheduled=true;view=player.view;
if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){target=new RenderTexture(640,360,24);target.Create();view.targetTexture=target;}
Framework.GetModule<ITimerManager>().AddTimer(3,()=>Finish(null),true);
}
void Finish(string error){
try{
var player=FindFirstObjectByType<FpsPlayer>();var result=new Result{unity=Application.unityVersion,device=SystemInfo.graphicsDeviceName,api=SystemInfo.graphicsDeviceType.ToString(),scenes=SceneManager.sceneCount,targets=FindObjectsByType<TargetPractice>(FindObjectsSortMode.None).Length,error=error};
result.ground=player && Physics.Raycast(player.transform.position+Vector3.up,Vector3.down,10,~((1<<10)|(1<<11)),QueryTriggerInteraction.Ignore);
string dir=Path.GetDirectoryName(Path.GetFullPath(output));Directory.CreateDirectory(dir);
if(target){RenderTexture.active=target;var image=new Texture2D(640,360,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,640,360),0,0);image.Apply();var pixels=image.GetPixels();result.totalPixels=pixels.Length;foreach(var pixel in pixels)if(pixel.maxColorComponent>0.05f)result.nonBlackPixels++;File.WriteAllBytes(Path.Combine(dir,"player-smoke.png"),image.EncodeToPNG());RenderTexture.active=null;}
result.passed=error==null && player && result.scenes>=4 && result.targets==3 && result.ground && (!target || result.nonBlackPixels>result.totalPixels/50);
File.WriteAllText(output,JsonUtility.ToJson(result,true));Debug.Log("FIRSTREP PLAYER SMOKE: "+JsonUtility.ToJson(result));Application.Quit(result.passed?0:1);
}catch(Exception e){Debug.LogException(e);Application.Quit(1);}
}
}
}
#endif
