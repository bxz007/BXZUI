using System.Diagnostics;
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Gala.FrameworkEditorTools
{

    public class EnabledRaycastTarget : EditorWindow
    {

        public static void Open()
        {
            EditorWindow.GetWindow(typeof(EnabledRaycastTarget), true);
        }

        public string path = "Assets/LoadResources/UI";

        public Vector2 scrollViewPos = Vector2.zero;

        private GUIStyle m_tempFontStyle = new GUIStyle();

        private List<string> gamesPathList = new List<string>();
        private List<string> floaderPathList = new List<string>();
        private string curSubFolder = string.Empty;
        private GameObject selectedGo;

        void Reset()
        {
            GetFolderPath();
        }

        void OnGUI()
        {
            m_tempFontStyle.normal.textColor = Color.yellow;
            m_tempFontStyle.fontSize = 18;

            EditorGUILayout.LabelField("文件夹路径 :" + path);

            if (GUILayout.Button("Change Folder", GUILayout.Width(500))) //当点击按钮时，显示下拉菜单
            {
                ShowGenericMenu();
            }
            gamesPathList.Clear();
            if (!string.IsNullOrEmpty(curSubFolder))
            {
                GetAllPrefabs(curSubFolder);
            }

            EditorGUILayout.Space(10);


            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("选中文件夹的prefab列表，点击可高亮选中", m_tempFontStyle);
            EditorGUILayout.Space(10);

            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, new[] { GUILayout.MaxHeight(200) });
            for (int i = 0; i < gamesPathList.Count; i++)
            {
                if (GUILayout.Button(gamesPathList[i], GUILayout.Height(25)))
                {
                    selectedGo = AssetDatabase.LoadAssetAtPath<GameObject>(gamesPathList[i]);
                    Selection.activeGameObject = selectedGo;
                    EditorGUIUtility.PingObject(selectedGo);

                    AssetDatabase.OpenAsset(selectedGo);
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);

            if (GUILayout.Button("关闭RaycastTarget", GUILayout.Height(40)))
            {
                DoEnabledRaycastTarget();
            }
        }

        void ShowGenericMenu()
        {
            curSubFolder = string.Empty;

            GenericMenu menu = new GenericMenu(); //初始化GenericMenu 
            for (int i = 0; i < floaderPathList.Count; i++)
            {
                menu.AddItem(new GUIContent(floaderPathList[i]), false, ChangeFolder, floaderPathList[i]);
            }
            menu.ShowAsContext(); //显示菜单
        }
        private void ChangeFolder(object obj)
        {
            curSubFolder = $"Assets/LoadResources/UI/{(string)obj}";
            //Debug.LogError(curSubFolder);
        }

        private void GetFolderPath()
        {
            floaderPathList.Clear();
            string[] subFolders = Directory.GetDirectories(path);
            for (int i = 0; i < subFolders.Length; i++)
            {
                //Debug.LogError(subFolders[i]);
                subFolders[i] = subFolders[i].Replace("\\", "/");
                string[] tfs = subFolders[i].Split('/');
                string tf = tfs[tfs.Length - 1];

                floaderPathList.Add(tf);
                //Debug.LogError(tf);
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


        private void DoEnabledRaycastTarget()
        {
            foreach (var item in gamesPathList)
            {
                int index = item.IndexOf("Assets/", StringComparison.CurrentCultureIgnoreCase);
                string newPath = item.Substring(index);
                GameObject obj = AssetDatabase.LoadAssetAtPath(newPath, typeof(GameObject)) as GameObject;
                SearchChild(obj);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("", "替换结束", "OK");
        }
        private List<Graphic> graphicObjs = new List<Graphic>();
        private void SearchChild(GameObject gameObject)
        {
            graphicObjs.Clear();

            graphicObjs = gameObject.GetComponentsInChildren<Graphic>(true).ToList();

            //UnityEngine.Debug.Log($"{gameObject.name}开始索引");

            foreach (Graphic item in graphicObjs)
            {
                if (item.GetComponent<Button>() != null || item.GetComponent<InputField>() != null)
                {
                    UnityEngine.Debug.LogWarning($"{item.name}对象为按钮跳过操作...");
                    continue;
                }
                if (item.raycastTarget)
                {
                    item.raycastTarget = false;
                    EditorUtility.SetDirty(item);
                    UnityEngine.Debug.LogError($"{item.name}对象身上的 Raycast Target 已经取消勾选...");
                }
            }
        }
    }
}