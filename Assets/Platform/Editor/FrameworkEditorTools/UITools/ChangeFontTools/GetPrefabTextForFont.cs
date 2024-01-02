using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;


namespace Gala.FrameworkEditorTools
{
    public class GetPrefabTextForFont : EditorWindow
    {
        public static void Open()
        {
            EditorWindow.GetWindow(typeof(GetPrefabTextForFont), true);
        }

        static string path = "Assets/LoadResources/UI";

        private GUIStyle m_tempFontStyle = new GUIStyle();

        public Vector2 scrollViewPos1 = Vector2.zero;
        public Vector2 scrollViewPos2 = Vector2.zero;

        private List<string> gamesPathList = new List<string>();

        string curSubFolder = string.Empty;
        private GameObject selectedGo;
        private List<Text> selectedGoTexts = new List<Text>();


        void Reset()
        {
            GetFolderPath();
        }

        void OnGUI()
        {
            m_tempFontStyle.normal.textColor = Color.yellow;
            m_tempFontStyle.fontSize = 18;

            EditorGUILayout.LabelField("默认文件夹路径：" + path);
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
            EditorGUILayout.LabelField("选中文件夹的prefab列表，点击可高亮选中", m_tempFontStyle);
            EditorGUILayout.Space(10);

            scrollViewPos1 = EditorGUILayout.BeginScrollView(scrollViewPos1, false, false, new[] { GUILayout.MaxHeight(200) });
            for (int i = 0; i < gamesPathList.Count; i++)
            {
                if (GUILayout.Button(gamesPathList[i], GUILayout.Height(25)))
                {
                    selectedGo = AssetDatabase.LoadAssetAtPath<GameObject>(gamesPathList[i]);
                    Selection.activeGameObject = selectedGo;
                    EditorGUIUtility.PingObject(selectedGo);

                    AssetDatabase.OpenAsset(selectedGo);
                    selectedGoTexts.Clear();
                    Text[] Texts = selectedGo.GetComponentsInChildren<Text>(true);
                    foreach (Text text in Texts)
                    {
                        if (text)
                        {
                            selectedGoTexts.Add(text);
                        }
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.Space(10);

            EditorGUILayout.LabelField("选择对应的文本", m_tempFontStyle);
            EditorGUILayout.Space(10);

            scrollViewPos2 = EditorGUILayout.BeginScrollView(scrollViewPos2, false, false, new[] { GUILayout.MaxHeight(200) });

            for (int i = 0; i < selectedGoTexts.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(selectedGoTexts[i].name, GUILayout.Width(120), GUILayout.Height(25)))
                {
                    Selection.activeGameObject = selectedGoTexts[i].gameObject;
                    //EditorGUIUtility.PingObject(selectedGoTexts[i]);
                }
                EditorGUILayout.Space(10);
                if (selectedGoTexts[i].resizeTextForBestFit)
                {
                    selectedGoTexts[i].resizeTextMinSize = EditorGUILayout.IntField(selectedGoTexts[i].resizeTextMinSize, GUILayout.Width(50), GUILayout.Height(25));
                    selectedGoTexts[i].resizeTextMaxSize = EditorGUILayout.IntField(selectedGoTexts[i].resizeTextMaxSize, GUILayout.Width(50), GUILayout.Height(25));
                    if (GUILayout.Button("替换size", GUILayout.Width(80), GUILayout.Height(25)))
                    {
                        selectedGoTexts[i].fontSize = selectedGoTexts[i].resizeTextMaxSize;
                        EditorUtility.SetDirty(selectedGoTexts[i]);
                        AssetDatabase.SaveAssets();
                        AssetDatabase.Refresh();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
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


        List<string> floaderPathList = new List<string>();
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
                //Debug.LogError("zzz " + assetPaths[i]);
                if (!gamesPathList.Contains(assetPaths[i])) gamesPathList.Add(assetPaths[i]);
            }

            foreach (var folder in subFolders)
            {
                GetAllPrefabs(folder);
            }

        }
    }
}
