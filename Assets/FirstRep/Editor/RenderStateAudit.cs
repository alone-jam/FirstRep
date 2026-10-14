using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Rendering.HighDefinition;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using Object=UnityEngine.Object;
namespace FirstRep.Fps.Editor {
public static class RenderStateAudit {
public static void Run(){
var text=new StringBuilder();var content=AssetDatabase.LoadAssetAtPath<SliceContent>(SliceWorkbench.ContentPath);
for(int i=0;i<content.levelScenes.Length;i++)EditorSceneManager.OpenScene(content.levelScenes[i],i==0?OpenSceneMode.Single:OpenSceneMode.Additive);
text.AppendLine("ColorSpace="+QualitySettings.activeColorSpace);
foreach(var l in Object.FindObjectsByType<Light>())text.AppendLine("LIGHT "+l.name+" "+l.type+" intensity="+l.intensity+" color="+l.color+" enabled="+l.enabled+" bake="+l.lightmapBakeType);
foreach(var v in Object.FindObjectsByType<Volume>()){
text.AppendLine("VOLUME "+v.name+" enabled="+v.enabled+" priority="+v.priority+" global="+v.isGlobal+" weight="+v.weight+" profile="+AssetDatabase.GetAssetPath(v.sharedProfile));
if(v.sharedProfile)foreach(var c in v.sharedProfile.components)if(c)text.AppendLine(c.GetType().FullName+" "+JsonUtility.ToJson(c));}
foreach(var path in AssetDatabase.GetDependencies(new[]{SliceWorkbench.ContentPath}.Concat(content.levelScenes).ToArray(),true)){
var m=AssetDatabase.LoadAssetAtPath<Material>(path);if(!m)continue;
text.AppendLine("MATERIAL "+path+" shader="+m.shader+" keywords="+string.Join(",",m.shaderKeywords));
foreach(var property in new[]{"_MaterialID","_EmissiveIntensity","_UseEmissiveIntensity","_EmissiveExposureWeight","_ExposureWeight","_SurfaceType","_RefractionModel","_DiffusionProfileHash"})if(m.HasProperty(property))text.AppendLine(property+"="+m.GetFloat(property));
foreach(var property in new[]{"_BaseColor","_EmissiveColor","_EmissiveColorLDR"})if(m.HasProperty(property))text.AppendLine(property+"="+m.GetColor(property));
var copy=new Material(m);HDShaderUtils.ResetMaterialKeywords(copy);if(!copy.shaderKeywords.OrderBy(x=>x).SequenceEqual(m.shaderKeywords.OrderBy(x=>x)))text.AppendLine("KEYWORD_MISMATCH reset="+string.Join(",",copy.shaderKeywords));Object.DestroyImmediate(copy);
}
File.WriteAllText("Reports/render-state-audit.txt",text.ToString());Debug.Log("Render state audit complete");
}
}
}
