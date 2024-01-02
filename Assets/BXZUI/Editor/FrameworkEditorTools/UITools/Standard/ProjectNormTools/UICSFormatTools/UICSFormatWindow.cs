using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;


namespace Gala.FrameworkEditorTools
{
    public class CSTreeItem
    {
        public bool isSelect = false;
        public string treeName = string.Empty;
    }

    public class CSCheckItem
    {
        public bool isFoldout = false;
        public CSHasChangeData func;
        public List<CSTreeItem> itemList = new List<CSTreeItem>();
    }

    public delegate bool CSHasChangeData(string[] lines, string csName, CSCheckItem item);

    public partial class UICSFormatWindow : EditorWindow
    {
        const string UICSPath = "/Scripts/Hotfix/Module";
        const string HOTFIXCSPath = "/Scripts/Hotfix/Module";
        const string RecordPath = "UI报告";
        const string RecordFuncName = "UICS问题列表.txt";

        static string MainDestribe = "EventDispatcher|view规范|语言方面|内存方面";
        static List<string> destribeList = new List<string>
    {
        "EventDispatcher", //监听事件
        "View不应直接发协议",
        "字符串语言检验",
        "资源没释放",
    };
        // 响应事件
        static List<CSHasChangeData> funcList = new List<CSHasChangeData>
    {
        HandleEventDispatch,HandleView,HandleLanguage,HandleResources
    };

        static bool isSelectAll;
        static bool lastIsSelectAll;
        static string recordPath;
        static Dictionary<string, CSCheckItem> treeItemDict = new Dictionary<string, CSCheckItem>();
        static List<string> CSPathList = new List<string>();

        [MenuItem("Assets/规范/CheckCS")]
        public static void OpenCSWindow()
        {
            UICSFormatWindow editorWindow = GetWindow<UICSFormatWindow>(true, "CheckCSFormat");
            InitTreeList();
            InitPath();
            InitCSList();
        }

        static void InitPath()
        {
            string dicPath = Application.dataPath.Replace("Assets", RecordPath);
            if (!Directory.Exists(dicPath))
            {
                Directory.CreateDirectory(dicPath);
            }
            recordPath = dicPath + "/" + RecordFuncName;
            _sb1 = new StringBuilder();
        }

        static void InitCSList()
        {
            string uipath = Application.dataPath + HOTFIXCSPath;
            CSPathList = GetAllFiles(uipath);
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
                CSCheckItem item = new CSCheckItem();
                List<CSTreeItem> itemList = new List<CSTreeItem>();
                string[] desArray = destribeList[i].Split("|".ToCharArray());
                for (int j = 0, num = desArray.Length; j < num; j++)
                {
                    bool isSelect = false;
                    if (EditorPrefs.HasKey(desArray[j]))
                    {
                        isSelect = EditorPrefs.GetBool(desArray[j]);
                    }
                    AddCSItem(isSelect, desArray[j], itemList);
                }
                item.isFoldout = true;
                item.itemList = itemList;
                item.func = funcList[i];
                treeItemDict.Add(itemArray[i], item);
            }
        }

        private static void AddCSItem(bool isSelect, string des, List<CSTreeItem> itemList)
        {
            CSTreeItem item = new CSTreeItem();
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

            foreach (KeyValuePair<string, CSCheckItem> pair in treeItemDict)
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
                foreach (KeyValuePair<string, CSCheckItem> pair in treeItemDict)
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
                if (EditorUtility.DisplayDialog("导出完成", String.Format("导出完成,是否打开{0}", recordPath), "打开", "关闭"))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(recordPath, 1);
                }
            }
            EditorGUILayout.EndHorizontal();
        }

        public void OnHandleRefreshLog()
        {
            _csFuncIndex = 1;
            _sb1.Clear();
            Append1("                  UICS问题列表");
            mainEventDict.Clear();
            hotfixEventDict.Clear();
            mainTrgDict.Clear();
            hotTrgEventDict.Clear();
            _eventMustHandle = false;
            for (int i = 0, num = CSPathList.Count; i < num; i++)
            {
                string[] csNameArray = CSPathList[i].Replace("\\", "/").Split("/".ToCharArray());
                string csName = csNameArray[csNameArray.Length - 1].Replace(".cs", "");
                string[] lines = File.ReadAllLines(CSPathList[i]);
                bool hasData = false;
                foreach (KeyValuePair<string, CSCheckItem> pair in treeItemDict)
                {
                    if (pair.Value.func != null)
                    {
                        hasData = (pair.Value.func(lines, csName, pair.Value) || hasData);
                    }
                }
                if (hasData)
                {
                    _csFuncIndex++;
                }
            }
            //  HandleTriEventDispatch();

            if (_csFuncIndex != 1)
            {
                WriteFile(_sb1, recordPath);
            }
        }

        public static List<string> GetAllFiles(string directoryPath)
        {
            List<string> result = new List<string>();
            DirectoryInfo theFolder = new DirectoryInfo(directoryPath);
            FileInfo[] fileInfo = theFolder.GetFiles("*.cs", SearchOption.AllDirectories);
            foreach (FileInfo file in fileInfo)
            {
                if (!file.FullName.EndsWith(".meta"))
                    result.Add(file.FullName.Substring(file.FullName.IndexOf("Assets")));
            }
            return result;
        }

        // static int ssss = 0;
        // static int sszzz = 0;
        // public static void GetAllFiles1(string directoryPath)
        // {
        //     List<string> result = new List<string>();
        //     DirectoryInfo theFolder = new DirectoryInfo(directoryPath);
        //     FileInfo[] fileInfo = theFolder.GetFiles("*.cs", SearchOption.AllDirectories);
        //     foreach (FileInfo file in fileInfo)
        //     {
        //         string[] aaas = file.FullName.Replace("\\", "/").Split("/".ToCharArray());
        //         if (aaas[aaas.Length - 1].Contains("View") && !file.FullName.Contains("ViewModel") && !file.FullName.Contains("ViewComponent") && !file.FullName.EndsWith(".meta"))
        //         {
        //             Debug.LogError(file.FullName);
        //             ssss++;
        //         }
        //         if (aaas[aaas.Length - 1].Contains("Controller")&& !file.FullName.EndsWith(".meta"))
        //         {
        //             sszzz++;
        //         }
        //         //   Debug.LogError(file.FullName.Substring(file.FullName.IndexOf("Assets")));
        //         result.Add(file.FullName.Substring(file.FullName.IndexOf("Assets")));
        //     }
        //     Debug.LogError("View = " + ssss + ";Controller = " +sszzz);
        //    // return result;
        // }

        private void RefreshSelectAll(bool select)
        {
            foreach (KeyValuePair<string, CSCheckItem> pair in treeItemDict)
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

