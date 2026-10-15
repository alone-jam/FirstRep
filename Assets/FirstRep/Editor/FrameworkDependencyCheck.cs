using System;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
namespace FirstRep.Fps.Editor
{
    public static class FrameworkDependencyCheck
    {
        public const string Revision = "a5e1ec6f3472f741815108509ba303ce39b933c3";
        [Serializable] sealed class Result { public string name, source, packageId, resolvedPath; public bool passed; }
        public static void Run()
        {
            var info = PackageInfo.FindForAssetPath("Packages/com.ejoy.framework/package.json");
            if (info == null) throw new Exception("UPM did not resolve EjoyFramework");
            var r = new Result { name = info.name, source = info.source.ToString(), packageId = info.packageId, resolvedPath = info.resolvedPath };
            r.passed = info.source == PackageSource.Git && info.packageId.Contains(Revision);
            Directory.CreateDirectory("Reports");
            File.WriteAllText("Reports/framework-dependency-resolution.json", JsonUtility.ToJson(r, true));
            if (!r.passed) throw new Exception("Expected pinned upstream Git dependency, without embedded override");
            Debug.Log("FIRSTREP FRAMEWORK GIT DEPENDENCY VERIFIED " + info.packageId);
        }
    }
}
