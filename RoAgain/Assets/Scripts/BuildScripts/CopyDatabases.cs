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

        CopyAllFilesFromToFolder(
            Path.Combine(Application.dataPath, "Server", "NpcDefs"),
            Path.Combine(buildFolder, Application.productName + "_Data", "Server", "NpcDefs"),
            "*.npc");

        CopyAllFilesFromToFolder(
            Path.Combine(Application.dataPath, "Server", "WarpDefs"),
            Path.Combine(buildFolder, Application.productName + "_Data", "Server", "WarpDefs"),
            "*.warp");
    }

    private static void CopyAllFilesFromToFolder(string sourceFolder, string targetFolder, string nameMask)
    {
        if (!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach (string file in Directory.GetFiles(sourceFolder, nameMask, SearchOption.AllDirectories))
        {
            string pureFilename = Path.GetFileName(file);
            string additionalFolders = Path.GetRelativePath(sourceFolder, Path.GetDirectoryName(file));
            string targetFileFolder = targetFolder;
            if (additionalFolders != ".")
            {
                targetFileFolder = Path.Combine(targetFolder, additionalFolders);
            }
            if (!Directory.Exists(targetFileFolder))
            {
                Directory.CreateDirectory(targetFileFolder);
            }

            string targetPath = Path.Combine(targetFileFolder, pureFilename);
            Debug.Log($"Copying file {file} to {targetPath}");
            File.Copy(file, targetPath, true);
        }
    }
}
#endif