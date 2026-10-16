#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.IO;
using EjoyFramework.Core;
using EjoyFramework.Core.Timer;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.InputSystem;
namespace FirstRep.Fps {
// Opt-in executable verification. Normal gameplay never creates this component.
public sealed class BuildSmokeProbe : MonoBehaviour {
[Serializable] sealed class Result {public bool passed;public string unity,device,api,error;public int scenes,targets,nonBlackPixels,totalPixels;public bool ground;}
float deadline;bool scheduled;string output;Camera view;RenderTexture target;
bool windowReport,readbackPending;float nextReport;string exposureGpu="not sampled";
[Serializable] sealed class WindowResult {
public int frame,scenes,targets;public float realtime,timeScale,exposureCompensation,minEv,maxEv,saturation,contrast;
public bool focused,paused,post,sss,exposure,opaque,cameraStopNan,motionBlur,bloom,stopNan,colorGrading,tone,aa;public float bloomIntensity,motionIntensity;public Vector3 position;public Vector2 mouse;
public string hud,exposureMode,tonemapping,colorFilter,channelRed,channelGreen,channelBlue,lift,gamma,gain,exposureTexture,exposureGpu;
}
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)] static void Install(){
var args=Environment.GetCommandLineArgs();int report=Array.IndexOf(args,"-firstrep-window-report");int index=report>=0?report:Array.IndexOf(args,"-firstrep-smoke-test");if(index<0)return;
var go=new GameObject("Development build verification");DontDestroyOnLoad(go);var probe=go.AddComponent<BuildSmokeProbe>();probe.output=index+1<args.Length?args[index+1]:"player-smoke.json";probe.deadline=Time.realtimeSinceStartup+180;
probe.windowReport=report>=0;
}
void Update(){
if(windowReport){ReportWindow();return;}
if(scheduled)return;
var player=FindFirstObjectByType<FpsPlayer>();
if(!player){if(Time.realtimeSinceStartup>deadline)Finish("Player did not load in 180 seconds");return;}
scheduled=true;view=player.view;
if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null){target=new RenderTexture(640,360,24);target.Create();view.targetTexture=target;}
Framework.GetModule<ITimerManager>().AddTimer(3,()=>Finish(null),true);
}
void ReportWindow(){
if(Time.realtimeSinceStartup<nextReport)return;nextReport=Time.realtimeSinceStartup+1;
var player=FindFirstObjectByType<FpsPlayer>();if(!player)return;
var hd=HDCamera.GetOrCreate(player.view);var stack=hd.volumeStack;if(stack==null)return;
var e=stack.GetComponent<Exposure>();var t=stack.GetComponent<Tonemapping>();var c=stack.GetComponent<ColorAdjustments>();var mix=stack.GetComponent<ChannelMixer>();var lgg=stack.GetComponent<LiftGammaGain>();
var tex=Shader.GetGlobalTexture("_ExposureTexture");
if(tex && tex.width==1 && !readbackPending && SystemInfo.supportsAsyncGPUReadback){readbackPending=true;AsyncGPUReadback.Request(tex,0,TextureFormat.RFloat,r=>{readbackPending=false;exposureGpu=r.hasError?"readback error":r.GetData<float>()[0].ToString("R",System.Globalization.CultureInfo.InvariantCulture);});}
var result=new WindowResult{hud=player.HudText,frame=Time.frameCount,realtime=Time.realtimeSinceStartup,timeScale=Time.timeScale,focused=Application.isFocused,paused=player.IsPaused,scenes=SceneManager.sceneCount,targets=FindObjectsByType<TargetPractice>(FindObjectsSortMode.None).Length,position=player.transform.position,mouse=Mouse.current!=null?Mouse.current.position.ReadValue():Vector2.zero,post=hd.frameSettings.IsEnabled(FrameSettingsField.Postprocess),sss=hd.frameSettings.IsEnabled(FrameSettingsField.SubsurfaceScattering),exposure=hd.frameSettings.IsEnabled(FrameSettingsField.ExposureControl),opaque=hd.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects),cameraStopNan=player.view.GetComponent<HDAdditionalCameraData>().stopNaNs,motionBlur=hd.frameSettings.IsEnabled(FrameSettingsField.MotionBlur),bloom=hd.frameSettings.IsEnabled(FrameSettingsField.Bloom),stopNan=hd.frameSettings.IsEnabled(FrameSettingsField.StopNaN),colorGrading=hd.frameSettings.IsEnabled(FrameSettingsField.ColorGrading),tone=hd.frameSettings.IsEnabled(FrameSettingsField.Tonemapping),aa=hd.frameSettings.IsEnabled(FrameSettingsField.Antialiasing),bloomIntensity=stack.GetComponent<Bloom>().intensity.value,motionIntensity=stack.GetComponent<MotionBlur>().intensity.value,exposureMode=e.mode.value.ToString(),exposureCompensation=e.compensation.value,minEv=e.limitMin.value,maxEv=e.limitMax.value,tonemapping=t.mode.value.ToString(),saturation=c.saturation.value,contrast=c.contrast.value,colorFilter=c.colorFilter.value.ToString(),channelRed=new Vector3(mix.redOutRedIn.value,mix.redOutGreenIn.value,mix.redOutBlueIn.value).ToString(),channelGreen=new Vector3(mix.greenOutRedIn.value,mix.greenOutGreenIn.value,mix.greenOutBlueIn.value).ToString(),channelBlue=new Vector3(mix.blueOutRedIn.value,mix.blueOutGreenIn.value,mix.blueOutBlueIn.value).ToString(),lift=lgg.lift.value.ToString(),gamma=lgg.gamma.value.ToString(),gain=lgg.gain.value.ToString(),exposureTexture=tex?tex.name:"none",exposureGpu=exposureGpu};
Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output)));File.WriteAllText(output,JsonUtility.ToJson(result,true));File.AppendAllText(Path.ChangeExtension(output,"trace.jsonl"),JsonUtility.ToJson(result)+"\n");
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
