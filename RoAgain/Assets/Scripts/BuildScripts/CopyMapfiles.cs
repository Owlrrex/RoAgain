using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Callbacks;
using System.IO;

public class CopyMapfiles
{
    [PostProcessBuild(1)]
    public static void OnPostprocessbuild(BuildTarget buildTarget, string pathToBuiltProject)
    {
        string buildFolder = Path.GetDirectoryName(pathToBuiltProject);
        string mapfilesFolder = Path.Combine(Application.dataPath, "MapFiles");
        string targetFolder = Path.Combine(buildFolder, Application.productName + "_Data", "MapFiles");
        if(!Directory.Exists(targetFolder))
        {
            Directory.CreateDirectory(targetFolder);
        }

        foreach(string file in Directory.GetFiles(mapfilesFolder, "*.gatu"))
        {
            string pureFilename = Path.GetFileName(file);
            string targetPath = Path.Combine(targetFolder, pureFilename);
            Debug.Log($"Copying mapfile {file} to {targetPath}");
            File.Copy(file, targetPath, true);
        }
    }
}
#endif