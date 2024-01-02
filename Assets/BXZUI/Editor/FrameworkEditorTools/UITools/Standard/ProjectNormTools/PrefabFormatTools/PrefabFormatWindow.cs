using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Gala.FrameworkEditorTools
{

    public class PrefabTreeItem
    {
        public bool isSelect = false;
        public string treeName = string.Empty;
    }

    public class CheckItem
    {
        public bool isFoldout = false;
        public HasChangeData func;
        public List<PrefabTreeItem> itemList = new List<PrefabTreeItem>();
    }

    public delegate bool HasChangeData(Transform[] trans, CheckItem item);

    public partial class PrefabFormatWindow : EditorWindow
    {
        const string UIPrefabPath = "/LoadResources/UI";
        const string RecordPath = "Prefab报告";
        const string RecordFuncName = "规范按功能.txt";
        const string RecordPrefabName = "规范按预制体.txt";

        const int ButtonLimitSize = 40;
        static string MainDestribe = "命名方面|小数点方面|image方面|text方面|button方面|mask方面";
        static List<string> destribeList = new List<string>
    {
        "首字母小写|包含空格|首字母数字", //命名方面 
        "localPosition含小数|position z值为0|localScale不为1|localScale含小数",  // 小数方面
        "image为空|image包含默认backage,UISprite|image为空但Alpha不为0并且size不为0|引用其他图集|图片边缘空白一像素", //image 方面
        "字体为空|字体为默认字体|字体包含默认文本", //text方面
        $"按钮没选择image|按钮相应区域小于{ButtonLimitSize}", //button方面
        "使用了uimask，是否用RectMask2D", //mask方面
    };
        // 响应事件
        static List<HasChangeData> funcList = new List<HasChangeData>
    {
        HandleName,HandleFloat,HandleImage,HandleText,HandleButton,HandleMask
    };

        static bool isSelectAll;
        static bool lastIsSelectAll;
        static string record1Path;
        static string record2Path;
        static Dictionary<string, CheckItem> treeItemDict = new Dictionary<string, CheckItem>();
        static List<string> prefabPathList = new List<string>();
        static int atlasUnfixPercent = 40;

        [MenuItem("Assets/规范/CheckPrefab")]
        public static void OpenPrefabWindow()
        {
            PrefabFormatWindow editorWindow = GetWindow<PrefabFormatWindow>(true, "CheckPrefab");
            InitTreeList();
            InitPath();
            InitPrefabList();
        }

        static void InitPath()
        {
            string dicPath = Application.dataPath.Replace("Assets", RecordPath);
            if (!Directory.Exists(dicPath))
            {
                Directory.CreateDirectory(dicPath);
            }
            record1Path = dicPath + "/" + RecordFuncName;
            record2Path = dicPath + "/" + RecordPrefabName;
            _sb1 = new StringBuilder();
            _sb2 = new StringBuilder();
        }

        static void InitPrefabList()
        {
            string uipath = Application.dataPath + UIPrefabPath;
            prefabPathList = GetAllFiles(uipath);
        }

        static void InitTreeList()
        {
            saveSuccess = 0;
            treeItemDict.Clear();
            isSelectAll = false;
            lastIsSelectAll = false;
            string[] itemArray = MainDestribe.Split("|".ToCharArray());
            for (int i = 0; i < itemArray.Length; i++)
            {
                CheckItem item = new CheckItem();
                List<PrefabTreeItem> itemList = new List<PrefabTreeItem>();
                string[] desArray = destribeList[i].Split("|".ToCharArray());
                for (int j = 0, num = desArray.Length; j < num; j++)
                {
                    bool isSelect = false;
                    if (EditorPrefs.HasKey(desArray[j]))
                    {
                        isSelect = EditorPrefs.GetBool(desArray[j]);
                    }
                    AddPrefabItem(isSelect, desArray[j], itemList);
                }
                item.isFoldout = true;
                item.itemList = itemList;
                item.func = funcList[i];
                treeItemDict.Add(itemArray[i], item);
            }
        }

        private static void AddPrefabItem(bool isSelect, string des, List<PrefabTreeItem> itemList)
        {
            PrefabTreeItem item = new PrefabTreeItem();
            item.isSelect = isSelect;
            item.treeName = des;
            itemList.Add(item);
        }

        static int saveSuccess = 0;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            int index = 1;
            Rect leftRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 800);
            EditorGUILayout.BeginScrollView(new Vector2(800, 0));
            foreach (KeyValuePair<string, CheckItem> pair in treeItemDict)
            {
                GUI.contentColor = Color.green;
                pair.Value.isFoldout = EditorGUILayout.Foldout(pair.Value.isFoldout, index++ + ":" + pair.Key);
                if (pair.Value.isFoldout)
                {
                    for (int i = 0, num = pair.Value.itemList.Count; i < num; i++)
                    {
                        GUI.contentColor = Color.white;
                        pair.Value.itemList[i].isSelect = EditorGUILayout.ToggleLeft(pair.Value.itemList[i].treeName, pair.Value.itemList[i].isSelect);
                    }
                }
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();

            EditorGUILayout.BeginHorizontal();
            isSelectAll = EditorGUILayout.ToggleLeft("全选", isSelectAll);
            if (lastIsSelectAll != isSelectAll)
            {
                lastIsSelectAll = isSelectAll;
                RefreshSelectAll(isSelectAll);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (saveSuccess > 0)
            {
                saveSuccess--;
                GUI.contentColor = Color.red;
                EditorGUILayout.LabelField("保存成功");
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.white;

            if (GUILayout.Button("保存配置"))
            {
                foreach (KeyValuePair<string, CheckItem> pair in treeItemDict)
                {
                    for (int i = 0, num = pair.Value.itemList.Count; i < num; i++)
                    {
                        EditorPrefs.SetBool(pair.Value.itemList[i].treeName, pair.Value.itemList[i].isSelect);
                        pair.Value.itemList[i].isSelect = EditorGUILayout.ToggleLeft(pair.Value.itemList[i].treeName, pair.Value.itemList[i].isSelect);
                    }
                }
                saveSuccess = 60;
            }
            if (GUILayout.Button("导出报告"))
            {
                OnHandleRefreshLog();
                if (EditorUtility.DisplayDialog("导出完成", String.Format("导出完成,是否打开{0}", record2Path), "打开", "关闭"))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(record2Path, 1);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public void OnHandleRefreshLog()
        {
            _prefabFuncIndex = 1;
            _sb1.Clear();
            _sb2.Clear();
            Append2("                  按预制体分析");
            for (int i = 0, num = prefabPathList.Count; i < num; i++)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathList[i]);
                Transform[] trans = root.GetComponentsInChildren<Transform>(true);
                bool hasData = false;
                foreach (KeyValuePair<string, CheckItem> pair in treeItemDict)
                {
                    if (pair.Value.func != null)
                    {
                        hasData = (pair.Value.func(trans, pair.Value) || hasData);
                    }
                }
                if (hasData)
                {
                    _prefabFuncIndex++;
                }
            }
            if (_prefabFuncIndex != 1)
            {
                WriteFile(_sb2, record2Path);

            }
        }

        public static List<string> GetAllFiles(string directoryPath)
        {
            List<string> result = new List<string>();
            DirectoryInfo theFolder = new DirectoryInfo(directoryPath);
            FileInfo[] fileInfo = theFolder.GetFiles("*.prefab", SearchOption.AllDirectories);
            foreach (FileInfo file in fileInfo)
            {
                //   Debug.LogError(file.FullName.Substring(file.FullName.IndexOf("Assets")));
                result.Add(file.FullName.Substring(file.FullName.IndexOf("Assets")));
            }
            return result;
        }


        private void RefreshSelectAll(bool select)
        {
            foreach (KeyValuePair<string, CheckItem> pair in treeItemDict)
            {
                for (int i = 0, num = pair.Value.itemList.Count; i < num; i++)
                {
                    pair.Value.itemList[i].isSelect = select;
                }
            }
        }

        private static void Append1(string context, bool useLine = true)
        {
            _sb1.Append(context);
            if (useLine) _sb1.AppendLine();
        }


        private static void Append2(string context, bool useLine = true)
        {
            _sb2.Append(context);
            if (useLine) _sb2.AppendLine();
        }

        private static void Append2()
        {

        }


        private static void WriteFile(StringBuilder sb, string path)
        {

            try
            {
                if (string.IsNullOrEmpty(path)) return;
                string end = "_" + System.DateTime.Now.Year + "_" + System.DateTime.Now.Month + "_" + System.DateTime.Now.Day + "_" + System.DateTime.Now.Hour + "_"
                + System.DateTime.Now.Minute + "_" + System.DateTime.Now.Second;
                //      path = path + end;
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
                //  string context = File.Exists(dataPath) ? File.ReadAllText(dataPath) : string.Empty;
                //设定书写的开始位置为文件的末尾  
                FileStream writer = new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                // writer.Position = 0;
                byte[] bytes = Encoding.GetEncoding("utf-8").GetBytes(sb.ToString());
                writer.BeginWrite(bytes, 0, bytes.Length, new AsyncCallback(EndWriteCallBack), writer);
                //context += _sb.ToString();
                //  File.WriteAllBytes(path,bytes);
            }
            catch (Exception e)
            {
                Debug.LogError(e.ToString());
            }
        }

        public static void EndWriteCallBack(IAsyncResult asyncResult)
        {
            FileStream stream = asyncResult.AsyncState as FileStream;//转化为FileStream类型
            if (stream != null)
            {
                stream.EndWrite(asyncResult);
                stream.Close();
            }
        }
    }



}