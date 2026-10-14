using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using UnityEditor;
using UnityEngine;

namespace FirstRep.Fps.Editor
{
    // Fail closed for visual payloads: wrappers/material conversions cannot introduce
    // replacement meshes, textures, clips or sounds behind an art-review checkbox.
    internal static class OriginalArtValidator
    {
        [Serializable] private sealed class Record { public string guid; public string sourcePath; public string sha256; }
        [Serializable] private sealed class Manifest { public string sourceCommit; public Record[] records; }
        private static readonly HashSet<string> PayloadExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
            ".fbx", ".obj", ".blend", ".dae", ".asset", ".anim", ".png", ".jpg", ".jpeg", ".tif", ".tiff",
            ".tga", ".psd", ".psb", ".exr", ".hdr", ".dds", ".cubemap", ".wav", ".ogg", ".mp3", ".aif", ".aiff" };

        public static void Validate(SliceContent content, List<string> errors)
        {
            const string manifestPath = "Assets/FirstRep/Editor/OriginalArtManifest.json";
            if (!File.Exists(manifestPath)) { errors.Add("Original-art manifest missing; run Tools/prepare_provenance.py."); return; }
            var manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(manifestPath));
            if (manifest == null || manifest.records == null || manifest.sourceCommit != "c8375e7cf29cd0bcc3764b0650028a99aecb7475")
            { errors.Add("Original-art manifest has missing records or an unexpected source revision."); return; }
            var sources = new Dictionary<string, Record>(StringComparer.Ordinal);
            foreach (var record in manifest.records)
            {
                if (string.IsNullOrEmpty(record.guid) || sources.ContainsKey(record.guid))
                { errors.Add("Invalid/duplicate original-art GUID in manifest."); return; }
                sources.Add(record.guid, record);
            }
            var roots = new List<string>(content.levelScenes ?? Array.Empty<string>());
            foreach (UnityEngine.Object asset in new UnityEngine.Object[] {
                content.firstPersonPrefab, content.targetPrefab, content.impactPrefab, content.shotAudio })
                if (asset) roots.Add(AssetDatabase.GetAssetPath(asset));
            foreach (string path in AssetDatabase.GetDependencies(roots.Where(p => !string.IsNullOrEmpty(p)).Distinct().ToArray(), true))
            {
                // Unity's package resources are validated through package/renderer checks;
                // this ledger covers project art sourced from FPSSample.
                if (!path.StartsWith("Assets/", StringComparison.Ordinal) || !PayloadExtensions.Contains(Path.GetExtension(path))) continue;
                var mainType = AssetDatabase.GetMainAssetTypeAtPath(path);
                if (Path.GetExtension(path).Equals(".asset", StringComparison.OrdinalIgnoreCase) &&
                    mainType != typeof(Mesh) && !typeof(Texture).IsAssignableFrom(mainType ?? typeof(object)) &&
                    mainType != typeof(AnimationClip) && mainType != typeof(AudioClip)) continue;
                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!sources.TryGetValue(guid, out var record))
                { errors.Add("Art payload is not identified in original FPSSample source: " + path); continue; }
                using (var stream = File.OpenRead(path))
                using (var sha = SHA256.Create())
                {
                    string actual = BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                    if (!string.Equals(actual, record.sha256, StringComparison.Ordinal))
                        errors.Add("Original art bytes changed: " + path + " (source " + record.sourcePath + "). Lossless conversions need a separately reviewed migration record; not automatically accepted.");
                }
            }
        }
    }
}
