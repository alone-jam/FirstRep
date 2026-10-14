using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace FirstRep.Fps.Editor {
public static class RestoreDiffusionProfiles {
[Serializable] class Original {public string name;public Color scatteringDistance,transmissionTint;public int texturingMode,transmissionMode;public Vector2 thicknessRemap;public float worldScale,ior;}
[Serializable] class Originals {public Original[] profiles;}
public static void Review(){Run();VisualCheck.Run();}
public static void Run(){
var originals=JsonUtility.FromJson<Originals>(File.ReadAllText("Tools/original-diffusion-profiles.json")).profiles;
var profiles=new DiffusionProfileSettings[originals.Length];var lines=new List<string>();
for(int i=0;i<profiles.Length;i++){
 var o=originals[i];string path="Assets/FirstRep/Recovered/Diffusion_"+(i+1)+"_"+o.name+".asset";
 var p=AssetDatabase.LoadAssetAtPath<DiffusionProfileSettings>(path);
 if(!p){p=ScriptableObject.CreateInstance<DiffusionProfileSettings>();AssetDatabase.CreateAsset(p,path);}
 var settings=new SerializedObject(p);
 settings.FindProperty("profile.texturingMode").enumValueIndex=o.texturingMode;
 settings.FindProperty("profile.transmissionMode").enumValueIndex=o.transmissionMode;
 settings.FindProperty("profile.thicknessRemap").vector2Value=o.thicknessRemap;
 settings.FindProperty("profile.worldScale").floatValue=o.worldScale;
 settings.FindProperty("profile.ior").floatValue=o.ior;
 settings.ApplyModifiedPropertiesWithoutUndo();
 p.scatteringDistance=o.scatteringDistance;p.transmissionTint=o.transmissionTint;
 typeof(DiffusionProfileSettings).GetMethod("OnEnable",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(p,null);
 EditorUtility.SetDirty(p);profiles[i]=p;lines.Add((i+1)+" -> "+path);
}
var vp=GraphicsSettings.GetRenderPipelineSettings<HDRPDefaultVolumeProfileSettings>().volumeProfile;
if(!vp.TryGet<DiffusionProfileList>(out var list)){list=vp.Add<DiffusionProfileList>(true);AssetDatabase.AddObjectToAsset(list,vp);}
list.diffusionProfiles.Override(profiles);EditorUtility.SetDirty(list);EditorUtility.SetDirty(vp);
foreach(var guid in AssetDatabase.FindAssets("t:Material",new[]{"Assets/Materials"})){
 var path=AssetDatabase.GUIDToAssetPath(guid);var m=AssetDatabase.LoadAssetAtPath<Material>(path);
 // The old fields are retained in serialized properties even when the new shader has no matching field.
 string yaml=File.ReadAllText(path);
 foreach(Match match in Regex.Matches(yaml,@"- _DiffusionProfile([0-3]?): (\d+)")){
  int index=int.Parse(match.Groups[2].Value);string suffix=match.Groups[1].Value;
  if(index<1 || index>profiles.Length || !m.HasProperty("_DiffusionProfileHash"+suffix))continue;
  var p=profiles[index-1];var so=new SerializedObject(p);uint hash=(uint)so.FindProperty("profile.hash").longValue;
  m.SetFloat("_DiffusionProfileHash"+suffix,BitConverter.ToSingle(BitConverter.GetBytes(hash),0));
  var method=typeof(HDRenderPipelineAsset).Assembly.GetType("UnityEngine.Rendering.HighDefinition.HDUtils").GetMethod("ConvertGUIDToVector4",BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);
  m.SetVector("_DiffusionProfileAsset"+suffix,(Vector4)method.Invoke(null,new object[]{AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(p))}));
  lines.Add(path+" layer="+suffix+" original="+index+" hash="+hash);EditorUtility.SetDirty(m);
 }
 // Old neutral profile 0 means no scattering. Modern SSS with no profile
 // instead produces an error color, so use standard Lit for this exact case.
 bool anyProfile=false;foreach(Match old in Regex.Matches(yaml,@"- _DiffusionProfile[0-3]?: (\d+)"))if(int.Parse(old.Groups[1].Value)>0)anyProfile=true;
 if(m.HasProperty("_MaterialID") && m.GetFloat("_MaterialID")==0 && !anyProfile){m.SetFloat("_MaterialID",1);EditorUtility.SetDirty(m);lines.Add(path+": original neutral/no-scattering profile -> standard Lit material feature (textures retained).");}
 HDShaderUtils.ResetMaterialKeywords(m);
}
AssetDatabase.SaveAssets();File.WriteAllLines("Reports/diffusion-profile-migration.txt",lines);
}
}
}
