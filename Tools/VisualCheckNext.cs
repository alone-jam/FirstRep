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
public static void Run(){
var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
if(!content)throw new Exception("No content");
for(int i=0;i<content.levelScenes.Length;i++)EditorSceneManager.OpenScene(content.levelScenes[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
var go=new GameObject("FirstRep visual audit camera");var camera=go.AddComponent<Camera>();camera.nearClipPlane=0.03f;camera.farClipPlane=1500;camera.fieldOfView=75;var hd=go.AddComponent<HDAdditionalCameraData>();hd.volumeLayerMask=~0;camera.cullingMask=~((1<<8)|(1<<12)|(1<<20)|(1<<21)|(1<<26));
go.transform.SetPositionAndRotation(content.spawnPosition+Vector3.up*1.65f,Quaternion.Euler(0,content.spawnYaw,0));
var profile=ScriptableObject.CreateInstance<VolumeProfile>();var exposure=profile.Add<Exposure>(true);exposure.mode.Override(ExposureMode.Fixed);exposure.fixedExposure.Override(10);
var volume=new GameObject("FirstRep calibrated exposure").AddComponent<Volume>();volume.isGlobal=true;volume.priority=100;volume.sharedProfile=profile;
var fp=Object.Instantiate(content.firstPersonPrefab,go.transform,false);
foreach(var a in fp.GetComponentsInChildren<Animator>()){a.Rebind();a.Update(0);}
fp.GetComponent<FirstPersonPresentation>().AlignOriginalRig();
foreach(var pos in content.targetPositions)Object.Instantiate(content.targetPrefab,pos,Quaternion.Euler(0,0,0));
Debug.Log("VISUAL DIAGNOSTIC: pipeline="+GraphicsSettings.currentRenderPipeline+" lights="+string.Join(";",Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l=>l.type==LightType.Directional).Select(l=>l.name+":"+l.intensity+":"+l.enabled))+" volumes="+string.Join(";",Object.FindObjectsByType<Volume>(FindObjectsSortMode.None).Select(v=>v.name+":"+v.gameObject.layer+":"+v.enabled+":"+v.sharedProfile)));
var rt=new RenderTexture(480,270,24,RenderTextureFormat.ARGB32);rt.Create();
for(int i=0;i<3;i++)RenderPipeline.SubmitRenderRequest(camera,new RenderPipeline.StandardRequest{destination=rt});
RenderTexture.active=rt;var image=new Texture2D(rt.width,rt.height,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,rt.width,rt.height),0,0);image.Apply();Directory.CreateDirectory("Reports/Visual");File.WriteAllBytes("Reports/Visual/first-person.png",image.EncodeToPNG());
var report="Device="+SystemInfo.graphicsDeviceName+" API="+SystemInfo.graphicsDeviceType+" spawn="+go.transform.position+"\n";
report+="Arms hierarchy:\n"+string.Join("\n",fp.GetComponentsInChildren<SkinnedMeshRenderer>().Select(x=>x.name+" bounds="+x.bounds+" enabled="+x.enabled+" material="+string.Join(",",x.sharedMaterials.Select(m=>m?m.name+":"+m.shader.name:"NULL"))))+"\n";
report+="Animators="+string.Join("\n",fp.GetComponentsInChildren<Animator>().Select(a=>a.name+" position="+a.transform.position+" controller="+a.runtimeAnimatorController));
File.WriteAllText("Reports/visual-check.txt",report);Debug.Log(report);
}
}
}
