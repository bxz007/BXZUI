using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;



public class FindComponentItemKeyMissValue : EditorWindow
{

    [MenuItem("PlatformEditorTool/FindComponentItemKeyMissValue")]

    public static void Open()
    {
        EditorWindow.GetWindow(typeof(FindComponentItemKeyMissValue), true);
    }

    public string path = "Assets/LoadResources/UI";

    public Vector2 scrollViewPos = Vector2.zero;
    private List<string> gamesPathList = new List<string>();


    void Reset()
    {
        gamesPathList.Clear();
        if (!string.IsNullOrEmpty(path))
        {
            GetAllPrefabs(path);
        }
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("文件夹路径 :" + path);

        EditorGUILayout.Space(10);

        if (GUILayout.Button("查找", GUILayout.Height(40)))
        {
            FindComponentItemKeyMiss();
        }
    }

    public void GetAllPrefabs(string directory)
    {
        if (string.IsNullOrEmpty(directory) || !directory.StartsWith("Assets"))
            throw new ArgumentException("folderPath");

        string[] subFolders = Directory.GetDirectories(directory);
        string[] guids = null;
        string[] assetPaths = null;
        int i = 0, iMax = 0;
        guids = AssetDatabase.FindAssets("t:Prefab", new string[] { directory });
        assetPaths = new string[guids.Length];
        for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
        {
            assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
            if (!gamesPathList.Contains(assetPaths[i]))
            {
                //Debug.Log(assetPaths[i]);
                gamesPathList.Add(assetPaths[i]);
            }
        }
        foreach (var folder in subFolders)
        {
            GetAllPrefabs(folder);
        }
    }


    void FindComponentItemKeyMiss()
    {
        foreach (var item in gamesPathList)
        {
            int index = item.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
            string newPath = item.Substring(index);
            GameObject obj = AssetDatabase.LoadAssetAtPath(newPath, typeof(GameObject)) as GameObject;
            SearchChild(obj,newPath,obj.name);
        }

        EditorUtility.DisplayDialog("", "检查结束", "OK");
    }

    private void SearchChild(GameObject gameObject,string path ,string parentName)
    {
        ComponentItemKey component = gameObject.GetComponent<ComponentItemKey>();
        if (component != null)
        {
            //Debug.Log($"UI  Name == {gameObject.name}  , component.componentDatas.Count ==  {component.componentDatas.Count}");
            foreach (var componentItem in component.componentDatas)
            {
                if (componentItem.Value == null)
                {
                    Debug.LogError($"路径={path}，{parentName} 出现丢失：gameObject Name == {gameObject.name} , key == {componentItem.Key} 对应的 value == null");
                }
            }
        }

        if (gameObject.transform.childCount > 0)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                SearchChild(gameObject.transform.GetChild(i).gameObject,path,parentName);
            }
        }

    }
}