using System;
using System.IO;
using UnityEditor.PackageManager;
using UnityEngine;
namespace FirstRep.Fps.Editor
{
    public static class FrameworkDependencyCheck
    {
        public const string Revision = "c36cc4fff162994ec7f05f47a2f30cc0ec4e414c";
        public const string GitUrl = "https://github.com/alone-jam/EjoyFramework.git";
        [Serializable] sealed class Result { public string name, source, packageId, resolvedPath; public bool passed; }
        public static void Run()
        {
            var info = PackageInfo.FindForAssetPath("Packages/com.ejoy.framework/package.json");
            if (info == null) throw new Exception("UPM did not resolve EjoyFramework");
            var r = new Result { name = info.name, source = info.source.ToString(), packageId = info.packageId, resolvedPath = info.resolvedPath };
            r.passed = info.source == PackageSource.Git && info.packageId == info.name + "@" + GitUrl + "#" + Revision;
            var reportDirectory = Path.Combine("Reports", "dependency-" + DateTime.UtcNow.ToString("yyyyMMddTHHmmssfffffffZ"));
            Directory.CreateDirectory(reportDirectory);
            File.WriteAllText(Path.Combine(reportDirectory, "framework-dependency-resolution.json"), JsonUtility.ToJson(r, true));
            if (!r.passed) throw new Exception("Expected pinned alone-jam fork Git dependency, without embedded override");
            Debug.Log("FIRSTREP FRAMEWORK GIT DEPENDENCY VERIFIED " + info.packageId);
        }
    }
}
