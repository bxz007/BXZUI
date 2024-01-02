using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gala.FrameworkEditorTools
{
    public class CopFilesWithDependency
    {
        public static void CopFiles()
        {
            string oldFolderPath = AssetDatabase.GetAssetPath(Selection.objects[0]);
            string[] s = oldFolderPath.Split('/');
            string folderName = s[s.Length - 1];
            if (folderName.Contains("."))
            {
                Debug.LogError("该索引不是文件夹名字");
                return;
            }
            string copyedFolderPath = Path.GetFullPath(".") + Path.DirectorySeparatorChar + oldFolderPath;
            string newfolderName = folderName + "_copy";
            string tempFolderPath = Application.dataPath.Replace("Assets", "TempAssets") + "/" + oldFolderPath.Replace("Assets/", "").Replace(folderName, newfolderName);
            string newFoldrPath = tempFolderPath.Replace("TempAssets", "Assets");

            UtilFile.CopyDirectory(copyedFolderPath, tempFolderPath);
            //重新生成guids
            UtilGuids.RegenerateGuids(copyedFolderPath);
            UtilFile.CopyDirectory(copyedFolderPath, newFoldrPath);
            AssetDatabase.DeleteAsset(oldFolderPath);
            UtilFile.CopyDirectory(tempFolderPath, copyedFolderPath);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }
    }
}

