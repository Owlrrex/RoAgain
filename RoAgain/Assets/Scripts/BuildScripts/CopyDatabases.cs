using UnityEngine;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;

public class CopyDatabases
{
    [PostProcessBuild(2)]
    public static void OnPostprocessbuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        string sourceFolder = Path.Combine(Application.dataPath, "Server", "Databases");
        string targetFolder = Path.Combine(buildFolder, Application.productName + "_Data", "Databases");
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach (string file in Directory.GetFiles(sourceFolder, "*.db"))
        {
            string pureFilename = Path.GetFileName(file);
            string targetPath = Path.Combine(targetFolder, pureFilename);
            Debug.Log($"Copying mapfile {file} to {targetPath}");
            File.Copy(file, targetPath, true);
        }
    }
}
#endif