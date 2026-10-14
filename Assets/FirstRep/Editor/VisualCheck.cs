using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditor;
using UnityEditor.SceneManagement;
using Object = UnityEngine.Object;
namespace FirstRep.Fps.Editor {
public static class VisualCheck {
public static void Probe(){
EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
var camera=new GameObject("Render API diagnostic").AddComponent<Camera>();var hd=camera.gameObject.AddComponent<HDAdditionalCameraData>();
hd.clearColorMode=HDAdditionalCameraData.ClearColorMode.Color;hd.backgroundColorHDR=Color.red;hd.customRenderingSettings=true;
hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess]=true;
var rt=new RenderTexture(128,128,24);rt.Create();Graphics.Blit(Texture2D.whiteTexture,rt);
for(int i=0;i<3;i++){RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});Debug.Log("PROBE FRAME "+i);}
RenderTexture.active=rt;var texture=new Texture2D(128,128,TextureFormat.RGB24,false);texture.ReadPixels(new Rect(0,0,128,128),0,0);texture.Apply();File.WriteAllBytes("Reports/Visual/render-probe.png",texture.EncodeToPNG());Debug.Log("PROBE PIXEL "+texture.GetPixel(64,64));
}
public static void Run(){
string output=Environment.GetEnvironmentVariable("FIRSTREP_VISUAL_OUTPUT")??"Reports/Visual";Directory.CreateDirectory(output);
var pipeline=GraphicsSettings.defaultRenderPipeline as HDRenderPipelineAsset;var settings=new SerializedObject(pipeline);var color=settings.FindProperty("m_RenderPipelineSettings.colorBufferFormat");int originalFormat=color.intValue;
Debug.Log("DIAGNOSTIC Color format="+((UnityEngine.Experimental.Rendering.GraphicsFormat)originalFormat)+" color space="+QualitySettings.activeColorSpace);
if(Environment.GetCommandLineArgs().Contains("-firstrep-float-color")){color.intValue=(int)UnityEngine.Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat;settings.ApplyModifiedPropertiesWithoutUndo();}

ShaderUtil.allowAsyncCompilation=false;
var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
if(!content)throw new Exception("No content");
for(int i=0;i<content.levelScenes.Length;i++)EditorSceneManager.OpenScene(content.levelScenes[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
var go=new GameObject("FirstRep visual audit camera");var camera=go.AddComponent<Camera>();camera.nearClipPlane=0.03f;camera.farClipPlane=1500;camera.fieldOfView=75;var hd=go.AddComponent<HDAdditionalCameraData>();hd.volumeLayerMask=~0;
if(Environment.GetCommandLineArgs().Contains("-firstrep-no-post")){hd.customRenderingSettings=true;hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess]=true;}
if(Environment.GetCommandLineArgs().Contains("-firstrep-no-sss")){hd.customRenderingSettings=true;hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.SubsurfaceScattering,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.SubsurfaceScattering]=true;}
camera.cullingMask=~((1<<8)|(1<<12)|(1<<20)|(1<<21)|(1<<26));
go.transform.SetPositionAndRotation(content.spawnPosition+Vector3.up*1.65f,Quaternion.Euler(0,content.spawnYaw,0));
if(Environment.GetCommandLineArgs().Contains("-firstrep-restored-presentation")){PresentationSettings.Configure(camera);}else{
var profile=ScriptableObject.CreateInstance<VolumeProfile>();var exposure=profile.Add<Exposure>(true);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(10);
if(Environment.GetCommandLineArgs().Contains("-firstrep-neutral-grading")){profile.Add<Tonemapping>(true).mode.Override(TonemappingMode.ACES);profile.Add<ColorAdjustments>(true);profile.Add<ColorCurves>(true);profile.Add<WhiteBalance>(true);profile.Add<ChannelMixer>(true);profile.Add<LiftGammaGain>(true);profile.Add<ShadowsMidtonesHighlights>(true);profile.Add<SplitToning>(true);}
var volume=new GameObject("FirstRep calibrated exposure").AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=profile;
}
var fp=Object.Instantiate(content.firstPersonPrefab,go.transform,false);
foreach(var t in fp.GetComponentsInChildren<Transform>())t.gameObject.layer=11;
foreach(var a in fp.GetComponentsInChildren<Animator>()){a.Rebind();a.Update(0);}
fp.GetComponent<FirstPersonPresentation>().AlignOriginalRig();
foreach(var pos in content.targetPositions)Object.Instantiate(content.targetPrefab,pos,Quaternion.Euler(0,0,0));
File.WriteAllLines("Reports/visual-shader-diagnostics.txt",Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None).SelectMany(r=>r.sharedMaterials).Where(m=>m).Distinct().Select(m=>AssetDatabase.GetAssetPath(m)+" | "+(m.shader?m.shader.name:"NULL")+" | supported="+(m.shader && m.shader.isSupported)+" | errors="+(m.shader && ShaderUtil.ShaderHasError(m.shader))));
File.WriteAllLines("Reports/original-spawn-locations.txt",Object.FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t=>t.name.Contains("Spawn")).Select(t=>t.name+" "+t.position+" yaw="+t.eulerAngles.y));
Debug.Log("VISUAL DIAGNOSTIC: pipeline="+GraphicsSettings.currentRenderPipeline+" lights="+string.Join(";",Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).Select(l=>l.name+":"+l.intensity+":"+l.enabled))+" volumes="+string.Join(";",Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>v.name+":"+v.gameObject.layer+":"+v.enabled+":"+v.sharedProfile)));
var rt=new RenderTexture(640,360,24,RenderTextureFormat.ARGB32);rt.Create();
for(int i=0;i<8;i++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();Directory.CreateDirectory("Reports/Visual");File.WriteAllBytes(Path.Combine(output,"first-person.png"),image.EncodeToPNG());
hd.customRenderingSettings=true;hd.renderingPathCustomFrameSettings.SetEnabled(FrameSettingsField.Postprocess,false);hd.renderingPathCustomFrameSettingsOverrideMask.mask[(uint)FrameSettingsField.Postprocess]=true;
for(int i=0;i<8;i++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
RenderTexture.active=rt;image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();File.WriteAllBytes(Path.Combine(output,"first-person-no-post.png"),image.EncodeToPNG());

var report="Device="+SystemInfo.graphicsDeviceName+" API="+SystemInfo.graphicsDeviceType+" spawn="+go.transform.position+"\n";
report+="Arms hierarchy:\n"+string.Join("\n",fp.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x=>x.name+" bounds="+x.bounds+" enabled="+x.enabled+" material="+string.Join(",",x.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"NULL"))))+"\n";
report+="Animators="+string.Join("\n",fp.GetComponentsInChildren<Animator>().Select(a=>a.name+" position="+a.transform.position+" controller="+a.runtimeAnimatorController));
File.WriteAllText(Path.Combine(output,"visual-check.txt"),report);Debug.Log(report);
File.WriteAllText(Path.Combine(output,"volumes.json"),string.Join("\n",Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).SelectMany(v=>(v.profile??v.sharedProfile).components.Where(c=>c).Select(c=>v.name+" "+c.GetType().FullName+" "+JsonUtility.ToJson(c)))));
color.intValue=originalFormat;settings.ApplyModifiedPropertiesWithoutUndo();
}
}
}
