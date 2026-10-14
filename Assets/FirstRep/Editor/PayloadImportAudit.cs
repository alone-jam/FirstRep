using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FirstRep.Fps.Editor
{
    public static class PayloadImportAudit
    {
        [Serializable] private sealed class Input { public Record[] records; }
        [Serializable] private sealed class Record { public string path; public string sha256; }
        [Serializable] private sealed class Imported {
            public string path, guid, type, error;
            public int width, height, meshCount, animationClipCount;
            public long vertices;
            public List<string> clips = new List<string>();
        }
        [Serializable] private sealed class Result {
            public string unityVersion;
            public string scope = "Actual Unity binary import only; not material/scene/animation playback or visual fidelity validation.";
            public List<Imported> assets = new List<Imported>();
        }
        public static void Run()
        {
            var input = JsonUtility.FromJson<Input>(File.ReadAllText("Reports/original-payload-import-inputs.json"));
            var report = new Result { unityVersion = Application.unityVersion };
            foreach (var record in input.records)
            {
                var row = new Imported { path = record.path, guid = AssetDatabase.AssetPathToGUID(record.path) };
                try
                {
                    var asset = AssetDatabase.LoadMainAssetAtPath(record.path);
                    if (!asset) row.error = "No main imported asset";
                    else
                    {
                        row.type = asset.GetType().FullName;
                        if (asset is Texture texture) { row.width = texture.width; row.height = texture.height; }
                        if (Path.GetExtension(record.path).Equals(".fbx", StringComparison.OrdinalIgnoreCase))
                            foreach (var subAsset in AssetDatabase.LoadAllAssetsAtPath(record.path))
                            {
                                if (subAsset is Mesh mesh) { ++row.meshCount; row.vertices += mesh.vertexCount; }
                                if (subAsset is AnimationClip clip) { ++row.animationClipCount; row.clips.Add(clip.name); }
                            }
                    }
                }
                catch (Exception e) { row.error = e.GetType().Name + ": " + e.Message; }
                report.assets.Add(row);
                if (report.assets.Count % 32 == 0) EditorUtility.UnloadUnusedAssetsImmediate();
            }
            File.WriteAllText("Reports/unity-original-payload-import.json", JsonUtility.ToJson(report, true));
            Debug.Log("Original payload import audit completed: " + report.assets.Count + " inputs.");
        }
    }
}
