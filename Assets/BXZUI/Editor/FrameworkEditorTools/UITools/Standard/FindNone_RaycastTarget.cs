using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Gala.FrameworkEditorTools
{
    public class FindNone_RaycastTarget : EditorWindow
    {
        public static void Init()
        {
            GetWindow<FindNone_RaycastTarget>().Show();

            EditorSettings.serializationMode = SerializationMode.ForceText;

            string dir = string.Format("{0}/../Doc", Application.dataPath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (!File.Exists(dir + "/temp.txt"))
                File.Create(dir + "/temp.txt");
        }

        static string FileName1 = "透明uiBtn.xlsx";
        static string FileName2 = "Raycast排查.xlsx";
        int checkType = 1;

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("查找按钮source image == null", GUILayout.Height(40)))
            {
                checkType = 1;
                CheckPrefab();
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button("查找结束，打开文件", GUILayout.Height(40)))
            {
                checkType = 1;
                OpenCheck();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Raycast排查", GUILayout.Height(40)))
            {
                checkType = 2;
                CheckPrefab();
            }
            EditorGUILayout.Space(10);
            if (GUILayout.Button("查找结束，打开文件", GUILayout.Height(40)))
            {
                checkType = 2;
                OpenCheck();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(10);

            if (GUILayout.Button("打开目录文件夹", GUILayout.Height(40)))
            {
                string dir = $"{Application.dataPath}/../Doc/temp.txt";
                EditorUtility.RevealInFinder(dir);
            }
        }

        private void OpenCheck()
        {
            string fileName = checkType == 1 ? FileName1 : FileName2;
            string newFilePath = string.Format("{0}/../Doc/{1}", Application.dataPath, fileName);
            if (File.Exists(newFilePath))
                System.Diagnostics.Process.Start(newFilePath);
        }

        private List<string> _names = new List<string>();

        private void CheckPrefab()
        {
            _names.Clear();

            EditorUtility.DisplayCancelableProgressBar("", "正在查询...", 0);
            List<GameObject> _prefabList = GetAllPrefabByAssetDatabase(
                "Assets/LoadResources",
                "Assets/Resources"
            );
            int count = _prefabList.Count;
            Debug.Log($"_prefabList.Count:{count}");

            for (int i = 0; i < count; i++)
            {
                GameObject gobj = _prefabList[i];
                if (gobj == null)
                    continue;
                //透明按钮
                if (checkType == 1)
                {
                    CheckTransparencyBtn(gobj);
                }
                else
                {
                    //Raycast排查
                    CheckRaycastObj(gobj);
                }
                bool _cancel = EditorUtility.DisplayCancelableProgressBar(
                    "",
                    "正在查询（" + i + "/" + (float)_prefabList.Count + "...",
                    i / (float)_prefabList.Count
                );
                if (_cancel)
                    break;
            }
            string fileName = checkType == 1 ? FileName1 : FileName2;
            string newFilePath = string.Format("{0}/../Doc/{1}", Application.dataPath, fileName);
            WriteCsv(_names, newFilePath);
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("", "查询完成", "ok");
        }

        /// <summary>
        /// 透明按钮
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        void CheckTransparencyBtn(GameObject gobj)
        {
            List<Image> aorImages = new List<Image>(gobj.GetComponentsInChildren<Image>());
            if (aorImages.Count <= 0)
                return;
            bool isContain = false;
            foreach (var image in aorImages)
            {
                if (
                    (
                        image.sprite == null
                        && image.color.a <= 0.1f
                        && image.raycastTarget
                        && image.transform.GetComponent<Button>() != null
                    )
                )
                {
                    isContain = true;
                }
            }
            if (isContain)
            {
                string path = AssetDatabase.GetAssetPath(gobj);
                string item = $"{path},{gobj.name}";
                _names.Add(gobj.name);
            }
        }

        StringBuilder stringBuilder;

        /// <summary>
        /// Raycast排查
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        void CheckRaycastObj(GameObject gobj)
        {
            List<Graphic> graphices = new List<Graphic>(gobj.GetComponentsInChildren<Graphic>());
            if (graphices.Count <= 0)
                return;

            if (stringBuilder == null)
                stringBuilder = new StringBuilder();
            else
                stringBuilder.Clear();

            bool isContain = false;
            foreach (var graphic in graphices)
            {
                if (
                    graphic.raycastTarget
                    && (
                        graphic.transform.GetComponent<Button>() == null
                        || graphic.transform.GetComponent<InputField>() == null
                    )
                )
                {
                    isContain = true;
                    stringBuilder.Append(graphic.name).Append(" | ");
                }
            }

            if (isContain)
            {
                string path = AssetDatabase.GetAssetPath(gobj);
                string item = $"{path},{gobj.name},{stringBuilder.ToString()}";
                _names.Add(item);
            }
        }

        public List<GameObject> GetAllPrefabByAssetDatabase(params string[] path)
        {
            List<GameObject> _prefabList = new List<GameObject>();
            string[] _guids = AssetDatabase.FindAssets("t:Prefab", path);
            string _prefabPath = "";
            GameObject _prefab;
            foreach (var _guid in _guids)
            {
                _prefabPath = AssetDatabase.GUIDToAssetPath(_guid);
                _prefab =
                    AssetDatabase.LoadAssetAtPath(_prefabPath, typeof(GameObject)) as GameObject;
                _prefabList.Add(_prefab);
            }
            return _prefabList;
        }

        private string GetRelativeAssetsPath(string path)
        {
            return "Assets"
                + Path.GetFullPath(path)
                    .Replace(Path.GetFullPath(Application.dataPath), "")
                    .Replace('\\', '/');
        }

        public void WriteCsv(List<string> strs, string path)
        {
            if (!File.Exists(path))
            {
                File.Create(path).Dispose();
            }
            //UTF-8方式保存
            using (StreamWriter stream = new StreamWriter(path, false, Encoding.UTF8))
            {
                for (int i = 0; i < strs.Count; i++)
                {
                    if (strs[i] != null)
                        stream.WriteLine(strs[i]);
                }
            }
        }
    }
}
