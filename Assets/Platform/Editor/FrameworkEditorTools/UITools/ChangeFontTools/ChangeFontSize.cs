using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;


namespace Gala.FrameworkEditorTools
{

    public class ChangeFontSize : EditorWindow
    {
        public static void Open()
        {
            EditorWindow.GetWindow(typeof(ChangeFontSize), true);
        }

        private GUIStyle m_tempFontStyle = new GUIStyle();

        public string path;
        public Font sourceFont;
        public Font targetFont;
        public int nowSize;
        public int targetSize;

        public Vector2 scrollViewPos = Vector2.zero;
        public bool selectedBestFit = false;
        private List<string> gamesPathList = new List<string>();
        List<string> tempGamesPathList = new List<string>();

        void OnGUI()
        {
            m_tempFontStyle.normal.textColor = Color.yellow;
            m_tempFontStyle.fontSize = 18;

            EditorGUILayout.LabelField("文件夹路径");
            //获得一个长300的框  
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(300));
            //将上面的框作为文本输入框  
            path = EditorGUI.TextField(rect, path);
            //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
            if ((Event.current.type == EventType.DragUpdated
              || Event.current.type == EventType.DragExited)
              && rect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表  
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    path = DragAndDrop.paths[0];
                }
            }
            EditorGUILayout.Space(10);

            sourceFont = (Font)EditorGUILayout.ObjectField("现在字体", sourceFont, typeof(Font), true, GUILayout.MinWidth(100f));
            EditorGUILayout.Space(10);

            targetFont = (Font)EditorGUILayout.ObjectField("目标字体(可选)", targetFont, typeof(Font), true, GUILayout.MinWidth(100f));
            EditorGUILayout.Space(10);


            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("现在字体大小", GUILayout.Width(100));
            nowSize = EditorGUILayout.IntField(nowSize, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("目标字体大小", GUILayout.Width(100));
            targetSize = EditorGUILayout.IntField(targetSize, GUILayout.Width(100));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            gamesPathList.Clear();
            if (!string.IsNullOrEmpty(path))
            {
                if (path.Contains(".prefab"))
                {
                    gamesPathList.Add(path);
                }
                else
                {
                    GetAllPrefabs(path);
                }
            }
            EditorGUILayout.LabelField("满足字体和大小的prefab列表，点击可高亮选中", m_tempFontStyle);
            EditorGUILayout.Space(10);
            tempGamesPathList.Clear();
            foreach (var item in gamesPathList)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                //Debug.LogError("gamesPathList[i] = " + gamesPathList[i]);
                Text[] Texts = go.GetComponentsInChildren<Text>(true);
                foreach (Text text in Texts)
                {
                    if (text)
                    {
                        if (text.font == sourceFont && text.fontSize == nowSize)
                        {
                            tempGamesPathList.Add(item);
                            break;
                        }
                    }
                }
            }
            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, new[] { GUILayout.MaxHeight(200) });
            for (int i = 0; i < tempGamesPathList.Count; i++)
            {
                if (GUILayout.Button(tempGamesPathList[i], GUILayout.Height(25)))
                {
                    Console.Clear();
                    //Debug.LogError(tempGamesPathList[i]);
                    GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(tempGamesPathList[i]);
                    Selection.activeGameObject = go;
                    EditorGUIUtility.PingObject(go);
                    AssetDatabase.OpenAsset(go);

                    Text[] Texts = go.GetComponentsInChildren<Text>(true);
                    foreach (Text text in Texts)
                    {
                        if (text)
                        {
                            if (text.font == sourceFont && text.fontSize == nowSize)
                            {
                                Debug.LogError(text.name);
                            }
                        }
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(10);

            selectedBestFit = GUILayout.Toggle(selectedBestFit, "选择修改BestFit的MaxSize", GUILayout.Width(200));
            EditorGUILayout.Space(10);
            if (GUILayout.Button("确认更换", GUILayout.Height(40)))
            {
                if (sourceFont == null)
                {
                    EditorUtility.DisplayDialog("", "字体为空", "OK");
                    return;
                }
                if (string.IsNullOrEmpty(path))
                {
                    EditorUtility.DisplayDialog("", "路径没拖拽", "OK");
                    return;
                }
                if (nowSize == 0 || targetSize == 0)
                {
                    EditorUtility.DisplayDialog("", "不能填写非数字", "OK");
                    return;
                }
                if (gamesPathList.Count == 0)
                {
                    EditorUtility.DisplayDialog("", "确定是有效路径吗？此目录不存在.prefab", "OK");
                    return;
                }
                Change();
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
                if (!gamesPathList.Contains(assetPaths[i])) gamesPathList.Add(assetPaths[i]);
            }
            foreach (var folder in subFolders)
            {
                GetAllPrefabs(folder);
            }
        }
        public void Change()
        {
            for (int i = 0; i < gamesPathList.Count; i++)
            {
                GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(gamesPathList[i]);
                //Debug.LogError("gamesPathList[i] = " + gamesPathList[i]);
                Text[] Texts = go.GetComponentsInChildren<Text>(true);
                foreach (Text text in Texts)
                {
                    if (text)
                    {
                        //Text TempText = (Text)text;
                        //Undo.RecordObject(TempText, TempText.gameObject.name);
                        if (text.font == sourceFont && text.fontSize == nowSize)
                        {
                            if (targetFont != null)
                            {
                                text.font = targetFont;
                                Debug.LogError(gamesPathList[i] + "的" + text.name + "的font从【" + sourceFont + "】变成了【" + targetFont + "】");
                            }
                            text.fontSize = targetSize;
                            Debug.LogError(gamesPathList[i] + "的" + text.name + "的fontsize从【" + nowSize + "】变成了【" + targetSize + "】");
                            if (selectedBestFit && text.resizeTextForBestFit)
                            {
                                text.resizeTextMaxSize = targetSize;
                            }
                            EditorUtility.SetDirty(text);
                        }
                    }
                }
                /* GameObject prefab = Instantiate(go);
                PrefabUtility.SaveAsPrefabAsset(prefab, gamesPathList[i]);
                DestroyImmediate(prefab); */
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("", "替换字体size结束", "OK");
        }
    }
}