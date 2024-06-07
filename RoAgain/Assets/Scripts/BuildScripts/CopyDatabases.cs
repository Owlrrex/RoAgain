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

        CopyAllFilesFromToFolder(
            Path.Combine(Application.dataPath, "Server", "Databases"),
            Path.Combine(buildFolder, Application.productName + "_Data", "Server", "Databases"),
            "*.db");

        CopyAllFilesFromToFolder(
            Path.Combine(Application.dataPath, "Client", "Tables"),
            Path.Combine(buildFolder, Application.productName + "_Data", "Client", "Tables"),
            "*.db");
    }

    private static void CopyAllFilesFromToFolder(string sourceFolder, string targetFolder, string nameMask)
    {
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach (string file in Directory.GetFiles(sourceFolder, nameMask))
        {
            string pureFilename = Path.GetFileName(file);
            string targetPath = Path.Combine(targetFolder, pureFilename);
            Debug.Log($"Copying file {file} to {targetPath}");
            File.Copy(file, targetPath, true);
        }
    }
}
#endif