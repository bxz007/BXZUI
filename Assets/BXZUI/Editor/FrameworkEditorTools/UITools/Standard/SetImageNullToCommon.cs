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

    public class SetImageNullToCommon : EditorWindow
    {

        public static void Open()
        {
            EditorWindow.GetWindow(typeof(SetImageNullToCommon), true);

            //可以打开多个windows的写法
            //SetImageNullToCommon mobwindow = (SetImageNullToCommon) EditorWindow.CreateInstance<SetImageNullToCommon>();
            //mobwindow.Show();
        }

        public string path = "Assets/LoadResources/UI";

        public Vector2 scrollViewPos = Vector2.zero;

        private GUIStyle m_tempFontStyle = new GUIStyle();

        private List<string> gamesPathList = new List<string>();
        private List<string> floaderPathList = new List<string>();
        private string curSubFolder = string.Empty;
        private GameObject selectedGo;

        private UnityEngine.Object changeImg;
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


            //获得一个长300的框  
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(300));
            //将上面的框作为文本输入框  
            changeImg = EditorGUI.ObjectField(rect, "请拖拽一个sprite上去", changeImg, typeof(Sprite), null);
            //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
            if ((Event.current.type == EventType.DragUpdated
              || Event.current.type == EventType.DragExited)
              && rect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表  
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    changeImg = DragAndDrop.objectReferences[0];
                }
            }
            EditorGUILayout.Space(10);

            if (GUILayout.Button("替换none", GUILayout.Height(40)))
            {
                DoImageNone();
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


        private void DoImageNone()
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
        private List<Image> imgObjs = new List<Image>();
        private void SearchChild(GameObject gameObject)
        {
            imgObjs.Clear();
            imgObjs = gameObject.GetComponentsInChildren<Image>(true).ToList();

            foreach (Image img in imgObjs)
            {
                if (img.sprite == null)
                {
                    //UnityEngine.Debug.LogError($"{img.name}对象身上的 sourceImage == none...");
                    if (changeImg != null && changeImg is Sprite)
                    {
                        //UnityEngine.Debug.LogError("changeImg == " + changeImg);
                        img.sprite = (Sprite)changeImg;
                        img.color = new Color(img.color.r, img.color.g, img.color.b, 0);
                        EditorUtility.SetDirty(img);
                    }
                }
            }
        }
    }
}