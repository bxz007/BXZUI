using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.U2D;

namespace Gala.FrameworkEditorTools
{
    public class SingleSearchRefrenceEditorWindow : EditorWindow
    {
        /// <summary>
        /// 查找引用
        /// </summary>

        private string atlasName = "";

        public static void Init()
        {
            GetWindow<SingleSearchRefrenceEditorWindow>().Show();
        }

        private SpriteAtlas changeAtlas;
        private Dictionary<string, List<string>> gamesPathList = new Dictionary<string, List<string>>();
        public Vector2 scrollViewPos = Vector2.zero;
        private void OnGUI()
        {
            EditorGUILayout.LabelField("测试单张图集被那些prefab引用了，看看有没有被其他模块使用");
            EditorGUILayout.Space(10);
            //获得一个长300的框  
            Rect rect = EditorGUILayout.GetControlRect(GUILayout.Width(300));
            //将上面的框作为文本输入框  
            changeAtlas = (SpriteAtlas)EditorGUI.ObjectField(rect, "请拖拽一个SpriteAtlas上去", changeAtlas, typeof(SpriteAtlas), null);

            //如果鼠标正在拖拽中或拖拽结束时，并且鼠标所在位置在文本输入框内  
            if ((Event.current.type == EventType.DragUpdated
              || Event.current.type == EventType.DragExited)
              && rect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表  
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    changeAtlas = (SpriteAtlas)DragAndDrop.objectReferences[0];
                    atlasName = changeAtlas.name;
                }
            }
            EditorGUILayout.Space(10);
            //atlasName = EditorGUILayout.TextField("Atlas Name", atlasName);

            if (GUILayout.Button("Find"))
            {
                gamesPathList.Clear();
                string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
                foreach (string prefabGuid in prefabGuids)
                {
                    string prefabPath = AssetDatabase.GUIDToAssetPath(prefabGuid);
                    string[] dependencies = AssetDatabase.GetDependencies(prefabPath);
                    foreach (string dependency in dependencies)
                    {
                        if (dependency.Contains(atlasName))
                        {
                            if (!gamesPathList.ContainsKey(prefabPath))
                            {
                                gamesPathList.Add(prefabPath, new List<string>());
                            }
                            List<string> dependencyList = null;
                            gamesPathList.TryGetValue(prefabPath, out dependencyList);
                            dependencyList.Add(dependency);

                            break;
                        }
                    }
                }
            }

            EditorGUILayout.Space(10);

            scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, new[] { GUILayout.MaxHeight(200) });

            foreach (var item in gamesPathList.Keys)
            {
                if (GUILayout.Button(item, GUILayout.Height(25)))
                {
                    GameObject selectedGo = AssetDatabase.LoadAssetAtPath<GameObject>(item);
                    Selection.activeGameObject = selectedGo;
                    EditorGUIUtility.PingObject(selectedGo);

                    AssetDatabase.OpenAsset(selectedGo);
                    List<string> dependencyList = null;
                    gamesPathList.TryGetValue(item, out dependencyList);

                    for (int i = 0; i < dependencyList.Count; i++)
                    {
                        Debug.LogError(item + " uses atlas " + dependencyList[i]);
                    }
                }
            }

            EditorGUILayout.EndScrollView();
        }
    }
}