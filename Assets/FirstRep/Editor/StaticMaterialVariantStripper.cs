using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
namespace FirstRep.Fps.Editor {
// This slice never changes material keywords at runtime (property-block values
// and camera frame settings are independent). Keep every included material's
// shader_feature combination, and ALL multi_compile/render-pipeline states.
// Revisit/disable this policy if runtime material-keyword switching is added.
public sealed class StaticMaterialVariantStripper : IPreprocessShaders, IPreprocessBuildWithReport {
public int callbackOrder=>10000;
static string[] dependencies;
static bool deferredOnly;
static readonly Dictionary<Shader,List<Material>> Materials=new Dictionary<Shader,List<Material>>();
public void OnPreprocessBuild(BuildReport report){Materials.Clear();
var roots=EditorBuildSettings.scenes.Where(x=>x.enabled).Select(x=>x.path).ToList();roots.Add(SliceWorkbench.ContentPath);roots.Add(AssetDatabase.GetAssetPath(GraphicsSettings.defaultRenderPipeline));roots.Add("Assets/HDRPDefaultResources/HDRenderPipelineGlobalSettings.asset");
for(int i=0;i<QualitySettings.names.Length;i++){var rp=QualitySettings.GetRenderPipelineAssetAt(i);if(rp)roots.Add(AssetDatabase.GetAssetPath(rp));}
roots.AddRange(AssetDatabase.GetAllAssetPaths().Where(p=>p.StartsWith("Assets/") && p.Contains("/Resources/") && !AssetDatabase.IsValidFolder(p)));
dependencies=AssetDatabase.GetDependencies(roots.ToArray(),true);
var pipelines=dependencies.Select(p=>AssetDatabase.LoadAssetAtPath<HDRenderPipelineAsset>(p)).Where(p=>p).ToArray();
deferredOnly=pipelines.Length>0 && pipelines.All(p=>p.currentPlatformRenderPipelineSettings.supportedLitShaderMode==RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly) && GraphicsSettings.GetRenderPipelineSettings<ShaderStrippingSetting>().stripRuntimeDebugShaders;
File.WriteAllText("Reports/shader-variant-pruning.txt","Exact static-material shader_feature combinations only; multi_compile states unchanged.\n");}
static List<Material> GetMaterials(Shader shader){
 if(Materials.TryGetValue(shader,out var found))return found;
 found=dependencies.Where(p=>AssetDatabase.GetMainAssetTypeAtPath(p)==typeof(Material)).Select(p=>AssetDatabase.LoadAssetAtPath<Material>(p)).Where(m=>m && m.shader==shader).ToList();
 var fallback=GraphicsSettings.defaultRenderPipeline.defaultMaterial;if(fallback && fallback.shader==shader)found.Add(fallback);
 Materials[shader]=found;return found;
}
public void OnProcessShader(Shader shader,ShaderSnippetData snippet,IList<ShaderCompilerData> data){
 if(data.Count==0 || (shader.name!="HDRP/Lit" && shader.name!="HDRP/LayeredLit" && shader.name!="HDRP/LitTessellation" && shader.name!="HDRP/LayeredLitTessellation"))return;
 var mats=GetMaterials(shader).Where(m=>m.GetShaderPassEnabled(snippet.passName)).ToList();
 // HDRP LitShaderPreprocessor also removes opaque Forward variants in
 // DeferredOnly without DEBUG_DISPLAY. Constrain the material signature set
 // to the transparent materials that can actually use this pass.
 if(snippet.passName=="Forward" && deferredOnly)mats=mats.Where(m=>m.IsKeywordEnabled("_SURFACE_TYPE_TRANSPARENT")).ToList();
 if(mats.Count==0){File.AppendAllText("Reports/shader-variant-pruning.txt",shader.name+"/"+snippet.passName+": "+data.Count+" -> 0; no included material enables this pass.\n");data.Clear();return;}
 string path=AssetDatabase.GetAssetPath(shader);var package=UnityEditor.PackageManager.PackageInfo.FindForAssetPath(path);
 if(package!=null)path=Path.Combine(package.resolvedPath,path.Substring(("Packages/"+package.name+"/").Length));
 var features=new Dictionary<string,bool>();
 foreach(Match line in Regex.Matches(File.ReadAllText(path),@"#pragma\s+(shader_feature(?:_\w+)?)\s+([^\r\n]+)"))
 foreach(string word in line.Groups[2].Value.Split(new[]{' ','\t'},StringSplitOptions.RemoveEmptyEntries))
 {if(word.StartsWith("//"))break;if(word!="_" && Regex.IsMatch(word,@"^\w+$"))features[word]=line.Groups[1].Value.Contains("_local");}
 var names=features.Keys.OrderBy(n=>n).ToArray();
 var keys=names.Select(n=>features[n]?new ShaderKeyword(shader,n):new ShaderKeyword(n)).ToArray();
 // A feature optimized out of this pass must not constrain its variants.
 var active=Enumerable.Range(0,names.Length).Where(i=>data.Any(d=>d.shaderKeywordSet.IsEnabled(keys[i]))).ToArray();
 var signatures=new HashSet<string>(mats.Select(m=>string.Join("|",active.Where(i=>m.IsKeywordEnabled(names[i])).Select(i=>names[i]))));
 var keep=new bool[data.Count];int count=0;
 for(int j=0;j<data.Count;j++){var d=data[j];keep[j]=signatures.Contains(string.Join("|",active.Where(i=>d.shaderKeywordSet.IsEnabled(keys[i])).Select(i=>names[i])));if(keep[j])count++;}
 // Never erase a pass if an unexpected compiler keyword representation appears.
 if(count==0){File.AppendAllText("Reports/shader-variant-pruning.txt",shader.name+"/"+snippet.passName+": conservative fallback, kept all "+data.Count+"\n");return;}
 int before=data.Count;for(int j=data.Count-1;j>=0;j--)if(!keep[j])data.RemoveAt(j);
 File.AppendAllText("Reports/shader-variant-pruning.txt",shader.name+"/"+snippet.passName+": "+before+" -> "+data.Count+"; material signatures="+signatures.Count+"; features="+active.Length+"\n");
}
}
}
