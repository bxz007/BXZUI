using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor.U2D;
using UnityEngine.UI;
using Object = UnityEngine.Object;


namespace Gala.FrameworkEditorTools
{
    public class SearchRefrenceEditorWindow : EditorWindow
    {
        /// <summary>
        /// 查找引用
        /// </summary>
        public static void SearchRefrence()
        {
            SearchRefrenceEditorWindow window = (SearchRefrenceEditorWindow)EditorWindow.GetWindow(typeof(SearchRefrenceEditorWindow), false, "SearchRefrence", true);
            window.Show();
        }

        private static Object searchObject;
        private List<Object> result = new List<Object>(100);
        private List<Object> singleResult = new List<Object>(100);
        private List<Object> resultImg = new List<Object>(100);

        private string _DstPath;
        private Vector2 scrPositon;
        private Vector2 scrPositon2;

        private bool openMaterial = true;
        private bool openShow4x4 = true;

        private void Awake()
        {
            _DstPath = PlayerPrefs.GetString("SearchRefrencePath", "");
            openMaterial = true;
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Object), true, GUILayout.Width(200));
            GUI.color = Color.green;
            if (GUILayout.Button("Search Refrences", GUILayout.Width(130)))
            {
                singleResult.Clear();

                if (searchObject == null)
                    return;

                string assetPath = AssetDatabase.GetAssetPath(searchObject);
                string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                //只检查prefab
                string[] guids = AssetDatabase.FindAssets("t:Prefab t:Material t:Scene", new[] { "Assets" });

                int length = guids.Length;
                for (int i = 0; i < length; i++)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayCancelableProgressBar("Checking", filePath, (float)i / length * 1.0f);

                    //检查是否包含guid
                    string content = File.ReadAllText(filePath);
                    if (content.Contains(assetGuid))
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                        singleResult.Add(fileObject);
                    }
                }
                EditorUtility.ClearProgressBar();
            }
            if (GUILayout.Button("Set image Slice", GUILayout.Width(130)))
            {
                singleResult.Clear();

                if (searchObject == null)
                    return;

                string assetPath = AssetDatabase.GetAssetPath(searchObject);
                string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
                string searchName = searchObject.name;
                //只检查prefab
                string[] guids = AssetDatabase.FindAssets("t:Prefab t:Material t:Scene", new[] { "Assets" });

                int length = guids.Length;
                for (int i = 0; i < length; i++)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayCancelableProgressBar("Checking", filePath, (float)i / length * 1.0f);

                    //检查是否包含guid
                    string content = File.ReadAllText(filePath);
                    if (content.Contains(assetGuid))
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                        GameObject obj = fileObject as GameObject;
                        if (obj != null)
                        {
                            bool set = false;
                            var images = obj.GetComponentsInChildren<Image>();
                            foreach (var img in images)
                            {
                                if (img.sprite != null && img.sprite.name == searchName && img.type == Image.Type.Simple)
                                {
                                    set = true;
                                    img.type = Image.Type.Sliced;
                                }
                            }

                            if (set)
                            {
                                singleResult.Add(fileObject);
                                EditorUtility.SetDirty(obj);
                            }

                        }

                    }
                }
                EditorUtility.ClearProgressBar();
            }

            GUI.color = Color.yellow;
            if (GUILayout.Button("Find In Hierarchy", GUILayout.Width(130)))
            {
                var spriteName = searchObject.name;
                FindSpriteUse(spriteName);
            }
            if (GUILayout.Button("Next", GUILayout.Width(50)))
            {
                if (finded.Count > 0)
                {
                    if (++selectedIndex >= finded.Count) selectedIndex = 0;
                    Selection.activeObject = finded[selectedIndex];
                }
            }
            if (GUILayout.Button("FindAltas", GUILayout.Width(100)))
            {
                if (searchObject == null)
                    return;
                var gameObj = searchObject as GameObject;
                if (gameObj)
                {
                    List<string> list = new List<string>();
                    StringBuilder singleImage = new StringBuilder();
                    singleImage.AppendLine("使用到的单图：");
                    StringBuilder atlas = new StringBuilder();
                    singleImage.AppendLine("使用到的图集：");
                    foreach (var item in gameObj.GetComponentsInChildren<Image>())
                    {
                        var path = AssetDatabase.GetAssetPath(item.sprite);
                        if (path.Contains("/UISpriteAtlas/Sprites/"))
                        {
                            var atlasName = GetAllAtlas(AssetDatabase.AssetPathToGUID(path));
                            if (string.IsNullOrEmpty(atlasName))
                            {
                                if (!list.Contains(path))
                                {
                                    singleImage.AppendLine(path);
                                    list.Add(path);
                                }
                            }
                            else
                            {
                                if (!list.Contains(atlasName))
                                {
                                    atlas.AppendLine(atlasName);
                                    list.Add(atlasName);
                                }
                            }
                        }
                        else
                        {
                            if (!list.Contains(path))
                            {
                                singleImage.AppendLine(path);
                                list.Add(path);
                            }
                        }
                    }

                    atlas.AppendLine(singleImage.ToString());
                    Debug.LogError(atlas.ToString());
                }
            }
            EditorGUILayout.EndHorizontal();

            scrPositon = EditorGUILayout.BeginScrollView(scrPositon);
            for (int i = 0; i < singleResult.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                GUI.color = new Color(0.5f, 0.8f, 1f);
                EditorGUILayout.ObjectField(singleResult[i], typeof(Object), true, GUILayout.Width(300));

                var tex = singleResult[i] as Texture2D;
                if (tex != null)
                {
                    if (tex.width % 4 == 0 && tex.height % 4 == 0)
                    {
                        if (GUILayout.Button(tex.width + " X " + tex.height, GUILayout.Width(130)))
                        {
                            // tex. = TextureFormat.ETC2_RGBA8;
                        }
                    }
                    else
                    {
                        GUILayout.Label(tex.width + " X " + tex.height);
                    }
                }
                GUI.color = Color.yellow;
                if (GUILayout.Button("Instance Prefab", GUILayout.Width(130)))
                {
                    var a = PrefabUtility.InstantiatePrefab(singleResult[i]);
                    if (a is GameObject b)
                    {
                        b.transform.SetParent(GameObject.Find("DefaultDisplay").transform);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

            GUILayout.Space(10);


            GUI.color = Color.red;
            EditorGUILayout.LabelField("搜索路径(可拖拽文件夹获得路径)：");
            GUI.color = Color.green;
            EditorGUILayout.BeginHorizontal();
            var generateRect = EditorGUILayout.GetControlRect();
            _DstPath = EditorGUI.TextField(generateRect, _DstPath);

            if ((Event.current.type == EventType.DragUpdated
                 || Event.current.type == EventType.DragExited)
                && generateRect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    _DstPath = DragAndDrop.paths[0];
                    PlayerPrefs.SetString("SearchRefrencePath", _DstPath);
                }
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            openMaterial = EditorGUILayout.Toggle("", openMaterial, GUILayout.Width(20f));
            GUILayout.Label("是否检查\"材质球\"和\"场景\"的引用");
            EditorGUILayout.EndHorizontal();

            if (GUILayout.Button("搜索所有没有被使用得图", GUILayout.Width(300)))
            {
                if (!string.IsNullOrEmpty(_DstPath))
                {
                    result.Clear();
                    string filter = openMaterial ? "t:Prefab t:Material t:Scene" : "t:Prefab";
                    string[] spriteGuids = AssetDatabase.FindAssets("t:Sprite", new[] { _DstPath });
                    string[] guids = AssetDatabase.FindAssets(filter, new[] { "Assets" });
                    int spriteLength = spriteGuids.Length;
                    for (int index = 0; index < spriteGuids.Length; index++)
                    {
                        string assetGuid = spriteGuids[index];

                        EditorUtility.DisplayCancelableProgressBar("Checking", assetGuid, (float)index / spriteLength * 1.0f);

                        int length = guids.Length;
                        bool isRefrence = false;
                        for (int i = 0; i < length; i++)
                        {
                            string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);

                            //检查是否包含guid
                            string content = File.ReadAllText(filePath);
                            if (content.Contains(assetGuid))
                            {
                                isRefrence = true;
                                break;
                            }
                        }

                        if (!isRefrence)
                        {
                            Object fileObject = AssetDatabase.LoadAssetAtPath(AssetDatabase.GUIDToAssetPath(assetGuid), typeof(Object));
                            result.Add(fileObject);
                        }
                    }

                    EditorUtility.ClearProgressBar();
                }
            }

            GUI.color = Color.white;
            //显示结果
            EditorGUILayout.BeginHorizontal();

            scrPositon = EditorGUILayout.BeginScrollView(scrPositon);

            for (int i = 0; i < result.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.ObjectField(result[i], typeof(Object), true, GUILayout.Width(300));
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();



            EditorGUILayout.EndHorizontal();



            GUI.color = Color.yellow;
            EditorGUILayout.LabelField("搜索图片格式(可拖拽文件夹获得路径,默认排除UISpriteAtlas文件夹)：");
            GUI.color = Color.green;
            EditorGUILayout.BeginHorizontal();
            var generateRect2 = EditorGUILayout.GetControlRect();
            _DstPath = EditorGUI.TextField(generateRect2, _DstPath);

            if ((Event.current.type == EventType.DragUpdated
                 || Event.current.type == EventType.DragExited)
                && generateRect2.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    _DstPath = DragAndDrop.paths[0];
                    PlayerPrefs.SetString("SearchRefrencePath", _DstPath);
                }
            }


            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            openShow4x4 = EditorGUILayout.Toggle("", openShow4x4, GUILayout.Width(20f));
            GUILayout.Label("是否显示符合4的倍数的图");
            EditorGUILayout.EndHorizontal();
            if (GUILayout.Button("搜索非ETC2格式的图", GUILayout.Width(300)))
            {
                string[] guids;
                if (!string.IsNullOrEmpty(_DstPath))
                {
                    guids = AssetDatabase.FindAssets("t:Texture", new[] { _DstPath });
                }
                else
                {
                    guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
                }
                resultImg.Clear();
                //只检查prefab


                int length = guids.Length;
                for (int i = 0; i < length; i++)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayCancelableProgressBar("Checking", filePath, (float)i / length * 1.0f);
                    if (filePath.IndexOf("UISpriteAtlas") >= 0 && _DstPath.IndexOf("UISpriteAtlas") == -1)
                    {
                        continue;
                    }
                    else
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                        Texture2D texture = fileObject as Texture2D;
                        if (texture != null && !(texture.format == TextureFormat.ETC2_RGB || texture.format == TextureFormat.ETC2_RGBA8 || texture.format == TextureFormat.ETC2_RGBA8Crunched || texture.format == TextureFormat.ETC2_RGBA1))
                        {
                            resultImg.Add(texture);
                        }
                    }

                }
                EditorUtility.ClearProgressBar();
            }
            if (GUILayout.Button("搜索ASTC格式的图", GUILayout.Width(300)))
            {
                string[] guids;
                if (!string.IsNullOrEmpty(_DstPath))
                {
                    guids = AssetDatabase.FindAssets("t:Texture", new[] { _DstPath });
                }
                else
                {
                    guids = AssetDatabase.FindAssets("t:Texture", new[] { "Assets" });
                }
                resultImg.Clear();
                //只检查prefab


                int length = guids.Length;
                for (int i = 0; i < length; i++)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(guids[i]);
                    EditorUtility.DisplayCancelableProgressBar("Checking", filePath, (float)i / length * 1.0f);
                    if (filePath.IndexOf("UISpriteAtlas") >= 0 && _DstPath.IndexOf("UISpriteAtlas") == -1)
                    {
                        continue;
                    }
                    else
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                        Texture2D texture = fileObject as Texture2D;
                        if (texture != null && (texture.format == TextureFormat.ASTC_5x5 || texture.format == TextureFormat.ASTC_6x6 || texture.format == TextureFormat.ASTC_8x8 || texture.format == TextureFormat.ASTC_10x10 || texture.format == TextureFormat.ASTC_12x12 || texture.format == TextureFormat.ASTC_4x4))
                        {
                            resultImg.Add(texture);
                        }
                    }

                }
                EditorUtility.ClearProgressBar();
            }
            scrPositon2 = EditorGUILayout.BeginScrollView(scrPositon2);
            for (int i = 0; i < resultImg.Count; i++)
            {
                var tex = resultImg[i] as Texture2D;
                if (tex != null && tex.width % 4 == 0 && tex.height % 4 == 0 && !openShow4x4)
                {
                    continue;
                }
                EditorGUILayout.BeginHorizontal();
                GUI.color = new Color(0.5f, 0.8f, 1f);
                EditorGUILayout.ObjectField(resultImg[i], typeof(Object), true, GUILayout.Width(300));

                if (tex != null)
                {
                    if (tex.width % 4 == 0 && tex.height % 4 == 0)
                    {
                        if (GUILayout.Button(tex.width + " X " + tex.height, GUILayout.Width(130)))
                        {
                            // tex. = TextureFormat.ETC2_RGBA8;
                        }
                    }
                    else
                    {
                        GUILayout.Label(tex.width + " X " + tex.height);
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();

        }

        private List<GameObject> finded = new List<GameObject>();
        private int selectedIndex = 0;

        private void FindSpriteUse(string name)
        {
            selectedIndex = 0;
            finded.Clear();
            var images = GameObject.FindObjectsOfType<Image>(true);
            var rawImages = GameObject.FindObjectsOfType<RawImage>(true);
            foreach (var image in images)
            {
                if (image.sprite != null && image.sprite.name == name)
                {
                    finded.Add(image.gameObject);
                }
            }

            foreach (var rawImage in rawImages)
            {
                if (rawImage.texture != null && rawImage.texture.name == name)
                {
                    finded.Add(rawImage.gameObject);
                }
            }

            if (finded.Count > 0)
            {
                Selection.activeObject = finded[selectedIndex];
            }
        }
        Dictionary<string, string> atlasContents = new Dictionary<string, string>();
        private string GetAllAtlas(string assetGuid)
        {
            if (atlasContents.Count <= 0)
            {
                var atlasGuids = AssetDatabase.FindAssets("t:spriteatlas", new[] { "Assets/LoadResources/UISpriteAtlas" });
                foreach (var atlasGuid in atlasGuids)
                {
                    string filePath = AssetDatabase.GUIDToAssetPath(atlasGuid);
                    var name = System.IO.Path.GetFileName(filePath);
                    //检查是否包含guid
                    string content = File.ReadAllText(filePath);

                    atlasContents.Add(name, content);
                    /*if (content.Contains(assetGuid))
                    {
                        Object fileObject = AssetDatabase.LoadAssetAtPath(filePath, typeof(Object));
                        singleResult.Add(fileObject);
                    }*/
                }
            }

            foreach (var item in atlasContents)
            {
                if (item.Value.Contains(assetGuid))
                {
                    return item.Key;
                }
            }

            return "";
        }
    }
}