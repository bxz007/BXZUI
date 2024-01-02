using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;


namespace Gala.FrameworkEditorTools
{
    public class CleanUpMissingScripts : EditorWindow
    {

        public static void Open()
        {
            EditorWindow.GetWindow(typeof(CleanUpMissingScripts), true);
        }

        public string path = "Assets/LoadResources";

        public Vector2 scrollViewPos = Vector2.zero;
        private List<string> gamesPathList = new List<string>();


        private bool firstInit = false;
        void OnGUI()
        {
            EditorGUILayout.LabelField("文件夹路径 :" + path);

            EditorGUILayout.Space(10);

            if (!firstInit)
            {
                firstInit = true;
                gamesPathList.Clear();
                if (!string.IsNullOrEmpty(path))
                {
                    GetAllPrefabs(path);
                }
            }

            if (GUILayout.Button("查找", GUILayout.Height(40)))
            {
                CleanupMissingScripts();
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


        void CleanupMissingScripts()
        {
            foreach (var item in gamesPathList)
            {
                int index = item.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
                string newPath = item.Substring(index);


                GameObject obj = AssetDatabase.LoadAssetAtPath(newPath, typeof(GameObject)) as GameObject;
                var components = obj.GetComponents<Component>();
                bool isNone = false;
                for (int j = 0; j < components.Length; j++)
                {
                    if (components[j] == null)
                    {
                        isNone = true;
                        break;
                    }
                }
                if (isNone)
                {
                    //实例化物体
                    GameObject go = PrefabUtility.InstantiatePrefab(obj) as GameObject;
                    //递归删除
                    searchChild(go);
                    // 将数据替换到asset

                    PrefabUtility.SaveAsPrefabAsset(go, newPath);
                    AssetDatabase.SaveAssets();
                    AssetDatabase.Refresh();

                    go.hideFlags = HideFlags.HideAndDontSave;
                    //删除掉实例化的对象
                    DestroyImmediate(go);

                    //Debug.Log(newPath);
                }

            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("", "检查结束", "OK");
        }

        static public void searchChild(GameObject gameObject)
        {
            int number = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
            if (gameObject.transform.childCount > 0)
            {
                for (int i = 0; i < gameObject.transform.childCount; i++)
                {
                    searchChild(gameObject.transform.GetChild(i).gameObject);
                }
            }
        }
    }
}