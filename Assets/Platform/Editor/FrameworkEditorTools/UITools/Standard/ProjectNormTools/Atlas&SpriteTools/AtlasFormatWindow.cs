using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.U2D;

namespace Gala.FrameworkEditorTools
{

    public partial class UIAtlasFormatWindow : EditorWindow
    {
        public class AtlasTreeItem
        {
            public bool isSelect = false;
            public string treeName = string.Empty;
        }

        public class AtlasCheckItem
        {
            public bool isFoldout = false;
            public AtlasHasChangeData func;
            public List<AtlasTreeItem> itemList = new List<AtlasTreeItem>();
        }

        public delegate bool AtlasHasChangeData(SpriteAtlas atlas, AtlasCheckItem item);


        const string UIAtlasPath = "/LoadResources/UISpriteAtlas";
        const string RecordPath = "Atlas报告";
        const string RecordFuncName = "Atlas问题列表.txt";

        static string MainDestribe = "Atlas";
        static int unuseAtlasImage = 30;
        static List<string> destribeList = new List<string>
    {
        $"图集空像素率大于{unuseAtlasImage}%", //图集利用率
      //  "",
    };
        // 响应事件
        static List<AtlasHasChangeData> funcList = new List<AtlasHasChangeData>
    {
        HandleAtlas,
    };

        static bool isSelectAll;
        static bool lastIsSelectAll;
        static string recordPath;
        static Dictionary<string, AtlasCheckItem> treeItemDict = new Dictionary<string, AtlasCheckItem>();
        static List<string> AtlasPathList = new List<string>();

        public static void OpenAtlasWindow()
        {
            UIAtlasFormatWindow editorWindow = GetWindow<UIAtlasFormatWindow>(true, "CheckAtlasFormat");
            InitTreeList();
            InitPath();
            InitAtlasList();
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

        static void InitAtlasList()
        {
            string uipath = Application.dataPath + UIAtlasPath;
            AtlasPathList = GetAllFiles(uipath);
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
                AtlasCheckItem item = new AtlasCheckItem();
                List<AtlasTreeItem> itemList = new List<AtlasTreeItem>();
                string[] desArray = destribeList[i].Split("|".ToCharArray());
                for (int j = 0, num = desArray.Length; j < num; j++)
                {
                    bool isSelect = false;
                    if (EditorPrefs.HasKey(desArray[j]))
                    {
                        isSelect = EditorPrefs.GetBool(desArray[j]);
                    }
                    AddAtlasItem(isSelect, desArray[j], itemList);
                }
                item.isFoldout = true;
                item.itemList = itemList;
                item.func = funcList[i];
                treeItemDict.Add(itemArray[i], item);
            }
            if (EditorPrefs.HasKey("UnuseAtlasImage"))
            {
                lastUnuseAtlasImage = EditorPrefs.GetInt("UnuseAtlasImage");
                treeItemDict["Atlas"].itemList[0].treeName = $"图集空像素率大于{unuseAtlasImage}%";
            }

        }

        private static void AddAtlasItem(bool isSelect, string des, List<AtlasTreeItem> itemList)
        {
            AtlasTreeItem item = new AtlasTreeItem();
            item.isSelect = isSelect;
            item.treeName = des;
            itemList.Add(item);
        }

        static int saveSuccess = 0;
        static int lastUnuseAtlasImage;

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical();
            int index = 1;
            Rect leftRect = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 800);
            EditorGUILayout.BeginScrollView(new Vector2(800, 0));

            foreach (KeyValuePair<string, AtlasCheckItem> pair in treeItemDict)
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

            unuseAtlasImage = EditorGUILayout.IntField("图集空像素率(%)", unuseAtlasImage);
            if (unuseAtlasImage > 100) unuseAtlasImage = 100;
            if (unuseAtlasImage < 0) unuseAtlasImage = 0;
            if (lastUnuseAtlasImage != unuseAtlasImage)
            {
                lastUnuseAtlasImage = unuseAtlasImage;
                treeItemDict["Atlas"].itemList[0].treeName = $"图集空像素率大于{unuseAtlasImage}%";
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("保存配置"))
            {
                foreach (KeyValuePair<string, AtlasCheckItem> pair in treeItemDict)
                {
                    for (int i = 0, num = pair.Value.itemList.Count; i < num; i++)
                    {
                        EditorPrefs.SetBool(pair.Value.itemList[i].treeName, pair.Value.itemList[i].isSelect);
                        pair.Value.itemList[i].isSelect = EditorGUILayout.ToggleLeft(pair.Value.itemList[i].treeName, pair.Value.itemList[i].isSelect);
                    }
                }
                EditorPrefs.SetInt("UnuseAtlasImage", unuseAtlasImage);
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
            Append1("                  图集问题列表");
            for (int i = 0, num = AtlasPathList.Count; i < num; i++)
            {
                SpriteAtlas atlas = AssetDatabase.LoadAssetAtPath<SpriteAtlas>(AtlasPathList[i]);
                bool hasData = false;
                foreach (KeyValuePair<string, AtlasCheckItem> pair in treeItemDict)
                {
                    if (pair.Value.func != null)
                    {
                        hasData = (pair.Value.func(atlas, pair.Value) || hasData);
                    }
                }
                if (hasData)
                {
                    _csFuncIndex++;
                }
            }
            if (_csFuncIndex != 1)
            {
                WriteFile(_sb1, recordPath);
            }
        }

        public static List<string> GetAllFiles(string directoryPath)
        {
            List<string> result = new List<string>();
            DirectoryInfo theFolder = new DirectoryInfo(directoryPath);
            FileInfo[] fileInfo = theFolder.GetFiles("*.spriteatlas", SearchOption.AllDirectories);
            foreach (FileInfo file in fileInfo)
            {
                if (!file.FullName.EndsWith(".meta"))
                    result.Add(file.FullName.Substring(file.FullName.IndexOf("Assets")));
            }
            return result;
        }

        private void RefreshSelectAll(bool select)
        {
            foreach (KeyValuePair<string, AtlasCheckItem> pair in treeItemDict)
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

