using System;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using UnityEngine.UI;
using Object = UnityEngine.Object;
public class ResultData
{
    public Object prefab;
    public Object obj;
    public Object obj2;
    public string name;
}
public class SearchPrefabReferenceEditorWindow : EditorWindow
{
   
    /// <summary>
    /// 查找引用
    /// </summary>
    public static void SearchRefrence()
    {
        SearchPrefabReferenceEditorWindow window = (SearchPrefabReferenceEditorWindow)EditorWindow.GetWindow(typeof(SearchPrefabReferenceEditorWindow), false, "Search prefab Reference", true);
        window.Show();
    }

    private static Object searchObject;
    private List<Object> result = new List<Object>(100);
    private List<ResultData> singleResult = new List<ResultData>(100);
    private List<Object> resultImg = new List<Object>(100);

    private string _DstPath;
    private Vector2 scrPositon;
    private Vector2 scrPositon2;

    private bool openMaterial = true;
    private bool openShow4x4 = true;

    private void Awake()
    {
        _DstPath = PlayerPrefs.GetString("SearchRefrencePath","");
        openMaterial = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        searchObject = EditorGUILayout.ObjectField(searchObject, typeof(Object), true, GUILayout.Width(200));
        //_moduleName = "";
        GUI.color = Color.green;
        if (GUILayout.Button("Search References", GUILayout.Width(130)))
        {
            singleResult.Clear();

            if (searchObject == null)
                return;

            if (searchObject is GameObject)
            {
                string assetPath = AssetDatabase.GetAssetPath(searchObject);
                assetPath = assetPath.Replace("Assets/LoadResources/UI/", "");
                var len = assetPath.IndexOf("/");
                _moduleName = assetPath.Substring(0, len);
                var images = (searchObject as GameObject).GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    var sp = img.sprite;

                    if (sp!=null)
                    {
                        singleResult.Add(new ResultData(){
                            obj = sp.texture,
                            obj2= img,
                            name = img.name
                        });
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        
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
            }
        }
        if (GUILayout.Button("Search References", GUILayout.Width(130)))
        {
            singleResult.Clear();
            string[] allPath = AssetDatabase.FindAssets("t:Prefab", new string[] {_DstPath});
            if (allPath == null)
                return;

            for (int i = 0; i < allPath.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(allPath[i]);
                var obj = AssetDatabase.LoadAssetAtPath(assetPath, typeof(GameObject)) as GameObject;
                assetPath = assetPath.Replace("Assets/LoadResources/UI/", "");
                var len = assetPath.IndexOf("/");
                _moduleName = assetPath.Substring(0, len);
                
                var images = obj.GetComponentsInChildren<Image>();
                foreach (var img in images)
                {
                    var sp = img.sprite;
                    /*
                    if (sp != null)// filter img folder
                    {
                        var tex = sp.texture;
                        string assetPath1 = AssetDatabase.GetAssetPath(tex);
                        assetPath1 = assetPath1.Replace("Assets/LoadResources/UI/", "");
                        var len1 = assetPath1.IndexOf("/");
                        string module1 = assetPath1.Substring(0, len1);
                        if(module1!="Backpack") continue;
                    }
                */

                    if (sp!=null)
                    {
                        singleResult.Add(new ResultData(){
                            prefab = obj,
                            obj = sp.texture,
                            obj2= img,
                            name = img.name
                        });
                    }
                }
            }

        }
        EditorGUILayout.EndHorizontal();
        GUI.color = Color.white;

        scrPositon = EditorGUILayout.BeginScrollView(scrPositon);
        for (int i = 0; i < singleResult.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.ObjectField(singleResult[i].prefab, typeof(Object), true, GUILayout.Width(200));
            EditorGUILayout.ObjectField(singleResult[i].obj2, typeof(Object), true, GUILayout.Width(200));
            GUI.color = new Color(0.5f,0.8f,1f);
            EditorGUILayout.ObjectField(singleResult[i].obj, typeof(Object), true, GUILayout.Width(300));

            var tex = singleResult[i].obj as Texture;
            var name = "image:"+singleResult[i].name;
            string assetPath = AssetDatabase.GetAssetPath(tex);
            
            if (assetPath.IndexOf("UISpriteAtlas")>0)
            {
                var index = assetPath.IndexOf("Sprites");
                assetPath= assetPath.Substring(index + 8);
                var len = assetPath.IndexOf("/");
                GUI.color = assetPath.ToLower().IndexOf(_moduleName.ToLower())>=0||assetPath.IndexOf("Common/")>=0? Color.cyan:Color.yellow;
                assetPath = "Atlas:"+ assetPath.Substring(0, len);
            }else if (assetPath.IndexOf("Assets/LoadResources/UI/")>=0)
            {
                assetPath = assetPath.Replace("Assets/LoadResources/UI/", "");
                var len = assetPath.IndexOf("/");
                string module = assetPath.Substring(0, len);
                GUI.color = module== _moduleName?Color.white : Color.yellow;
                assetPath = module;
            }
            GUILayout.Label(assetPath);
            GUI.color = Color.white;
            
            
            GUILayout.Label(name);
           
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        
    }

    private List<GameObject> finded = new List<GameObject>();
    private int selectedIndex = 0;
    private string _moduleName;

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
            if (rawImage.texture!=null && rawImage.texture.name == name)
            {
                finded.Add(rawImage.gameObject);
            }
        }

        if (finded.Count > 0)
        {
            Selection.activeObject = finded[selectedIndex];
        }
    }
}
