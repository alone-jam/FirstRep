using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace FirstRep.Fps.Editor {
public static class ImporterSettingsAudit {
[Serializable] private class Input { public Record[] records; }
[Serializable] private class Record { public string path; }
[Serializable] private class Row { public string path, importer, serializedSettings; public List<string> objectIds = new List<string>(); }
[Serializable] private class Result { public string unityVersion; public List<Row> assets = new List<Row>(); }
public static void Run() {
var input=JsonUtility.FromJson<Input>(File.ReadAllText("Reports/original-payload-import-inputs.json"));
var result=new Result { unityVersion=Application.unityVersion };
foreach(var record in input.records) {
var importer=AssetImporter.GetAtPath(record.path);
var row=new Row {path=record.path, importer=importer ? importer.GetType().Name : "MISSING", serializedSettings=importer ? EditorJsonUtility.ToJson(importer) : ""};
if(importer is ModelImporter) foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(record.path)) {
if(AssetDatabase.TryGetGUIDAndLocalFileIdentifier(asset,out string guid,out long id)) row.objectIds.Add(id+"|"+asset.GetType().Name+"|"+asset.name);
}
result.assets.Add(row);
if(result.assets.Count%32==0) EditorUtility.UnloadUnusedAssetsImmediate();
}
File.WriteAllText("Reports/unity-importer-settings.json",JsonUtility.ToJson(result,true));
Debug.Log("Importer settings audit completed: "+result.assets.Count);
}
}}
