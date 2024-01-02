using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.VersionControl;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.U2D;
using FileMode = System.IO.FileMode;
using Task = System.Threading.Tasks.Task;

namespace Gala.FrameworkEditorTools
{
    public delegate bool SingleSpriteHasChangeData(string path, SingleSpriteCheckItem item);

    public class SingleSpriteTreeItem
    {
        public bool isSelect = false;
        public string treeName = string.Empty;
    }

    public class SingleSpriteCheckItem
    {
        public bool isFoldout = false;
        public SingleSpriteHasChangeData func;
        public List<SingleSpriteTreeItem> itemList = new List<SingleSpriteTreeItem>();
    }
    public partial class SingleSpriteFormatWindow : EditorWindow
    {
        const string UIAtlasPath = "Assets/LoadResources/UI";
        const string RecordPath = "单图报告";
        const string RecordFuncName = "单图问题列表.txt";
        private static SingleSpriteFormatWindow editorWindow;
        static string MainDestribe = "UI单图检测";
        private bool isSelectedAllTexture = false;
        private bool lastIsSelectedAllTexture = false;
        private static string selectPath = "";

        private bool selectedMipMap = false;
        private bool selectedAlpha = false;
        private bool selectedReadWrite = false;
        private bool selectedFormat = true;
        private Rect _leftPanel = new Rect();
        private Rect _rightPanel = new Rect();
        private Vector2 scrPositon;

        static List<string> destribeList = new List<string>
    {
        "MipMap勾选检测|Alpha is Transparency勾选检测|Read/Write Enable勾选检测|图片尺寸是否为4n|安卓压缩格式是否为ETC2检测|ETC2 4bit 8bit正确检测",
    };
        // 响应事件 
        static List<SingleSpriteHasChangeData> funcList = new List<SingleSpriteHasChangeData>
    {
        HandleTexture,
    };

        static bool isSelectAll;
        static bool lastIsSelectAll;
        static string recordPath;
        static Dictionary<string, SingleSpriteCheckItem> treeItemDict = new Dictionary<string, SingleSpriteCheckItem>();
        static List<string> texturePathList = new List<string>();

        [MenuItem("Assets/规范/CheckSingleSprite")]
        public static void OpenSpriteWindow()
        {
            editorWindow = GetWindow<SingleSpriteFormatWindow>(true, "CheckSingleSprite");
            InitTreeList();
            InitPath();
            _wrongList.Clear();
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

            if (string.IsNullOrEmpty(selectPath))
            {
                selectPath = EditorPrefs.GetString("SingleSpitePath", "");
            }
        }

        static void InitTextureList()
        {
            string chooseFolder = selectPath;
            if (string.IsNullOrEmpty(selectPath))
            {
                chooseFolder = UIAtlasPath;
            }
            chooseFolder = chooseFolder.Replace("Assets", "");
            string uipath = Application.dataPath + chooseFolder;
            texturePathList = GetAllFiles(uipath);
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
                SingleSpriteCheckItem item = new SingleSpriteCheckItem();
                List<SingleSpriteTreeItem> itemList = new List<SingleSpriteTreeItem>();
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

        }

        private static void AddAtlasItem(bool isSelect, string des, List<SingleSpriteTreeItem> itemList)
        {
            SingleSpriteTreeItem item = new SingleSpriteTreeItem();
            item.isSelect = isSelect;
            item.treeName = des;
            itemList.Add(item);
        }

        static int saveSuccess = 0;
        static int lastUnuseAtlasImage;

        private void OnGUI()
        {
            RefreshPanelRect();
            DrawLeftPanel();
            DrawRightPanel();
        }


        private void RefreshPanelRect()
        {
            if (!editorWindow) return;
            _leftPanel.x = 0;
            _leftPanel.y = 0;
            _leftPanel.height = editorWindow.position.height;
            _leftPanel.width = 300;
            _rightPanel.y = 0;
            _rightPanel.x = 300;
            _rightPanel.height = editorWindow.position.height;
            _rightPanel.width = Math.Max(editorWindow.position.width - 300, 0);
        }

        public void DrawRightPanel()
        {
            GUILayout.BeginArea(_rightPanel);

            if (wrongTypeList.Count == 0) return;
            EditorGUILayout.BeginVertical();
            scrPositon = EditorGUILayout.BeginScrollView(scrPositon);
            for (int i = 0; i < _wrongList.Count; i++)
            {
                var tex = _wrongList[i].texture;
                EditorGUILayout.BeginHorizontal();
                GUI.color = new Color(0.5f, 0.8f, 1f);
                _wrongList[i].isSelected = EditorGUILayout.ToggleLeft("选择", _wrongList[i].isSelected, GUILayout.Width(50));
                EditorGUILayout.ObjectField(tex, typeof(Texture), true, GUILayout.Width(200));
                foreach (var type in wrongTypeList)
                {
                    if ((type & _wrongList[i].wrongType) == 0)
                    {
                        continue;
                    }

                    switch (type)
                    {
                        case WrongType.MipMap:
                            GUI.color = Color.yellow;
                            GUILayout.Button("MipMap", GUILayout.Width(130));
                            break;
                        case WrongType.ReadWrite:
                            GUI.color = Color.yellow;
                            GUILayout.Button("ReadWrite", GUILayout.Width(130));
                            break;
                        case WrongType.AlphaIsTransparency:
                            GUI.color = Color.yellow;
                            GUILayout.Button("Alpha is Transparency", GUILayout.Width(150));
                            break;
                        case WrongType.SizeIs4N:
                            GUI.color = Color.red;
                            GUILayout.Button("尺寸不是4N", GUILayout.Width(130));
                            break;
                        case WrongType.NotUseETC2:
                            GUI.color = Color.green;
                            GUILayout.Button("NotUseETC2", GUILayout.Width(130));
                            break;
                        case WrongType.ShouldNotBeETC24Bit:
                            GUI.color = Color.green;
                            GUILayout.Button("不应使用4Bit", GUILayout.Width(130));
                            break;
                        case WrongType.ShouldNotBeETC28Bit:
                            GUI.color = Color.green;
                            GUILayout.Button("不应使用8Bit", GUILayout.Width(130));
                            break;
                        case WrongType.UseAutoFormat:
                            GUI.color = Color.green;
                            GUILayout.Button("AutoFormat", GUILayout.Width(130));
                            break;
                    }
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
            EditorGUILayout.BeginHorizontal();
            GUI.color = Color.white;
            isSelectedAllTexture = EditorGUILayout.ToggleLeft("选择所有图片", isSelectedAllTexture, GUILayout.Width(150));
            if (lastIsSelectedAllTexture != isSelectedAllTexture)
            {
                lastIsSelectedAllTexture = isSelectedAllTexture;
                RefreshSelectAllTexture(isSelectedAllTexture);
            }
            GUI.color = Color.green;
            selectedFormat = EditorGUILayout.ToggleLeft("安卓压缩格式", selectedFormat, GUILayout.Width(150));
            GUI.color = Color.yellow;
            selectedMipMap = EditorGUILayout.ToggleLeft("取消勾选MipMap", selectedMipMap, GUILayout.Width(150));
            selectedAlpha = EditorGUILayout.ToggleLeft("勾选Alpha is Transparency", selectedAlpha, GUILayout.Width(200));
            selectedReadWrite = EditorGUILayout.ToggleLeft("取消勾选Read/Write", selectedReadWrite, GUILayout.Width(150));
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }
        public void DrawLeftPanel()
        {
            GUILayout.BeginArea(_leftPanel);
            EditorGUILayout.BeginVertical();
            int index = 1;
            EditorGUILayout.BeginScrollView(new Vector2(800, 0));

            foreach (KeyValuePair<string, SingleSpriteCheckItem> pair in treeItemDict)
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
            EditorGUILayout.LabelField("搜索路径(拖拽文件夹获得路径，默认忽略UI/Backpack)：");
            GUI.contentColor = Color.green;
            var generateRect = EditorGUILayout.GetControlRect();
            selectPath = EditorGUI.TextField(generateRect, selectPath);

            if ((Event.current.type == EventType.DragUpdated
                 || Event.current.type == EventType.DragExited)
                && generateRect.Contains(Event.current.mousePosition))
            {
                //改变鼠标的外表
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
                if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                {
                    selectPath = DragAndDrop.paths[0];
                    EditorPrefs.SetString("SingleSpitePath", selectPath);
                }
            }

            EditorGUILayout.BeginHorizontal();
            GUI.contentColor = Color.white;
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
                foreach (KeyValuePair<string, SingleSpriteCheckItem> pair in treeItemDict)
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
                if (!string.IsNullOrEmpty(selectPath) && selectPath.StartsWith("Assets/LoadResources/UISpriteAtlas"))
                {
                    EditorUtility.DisplayDialog("路径错误", "只可检测单图，请勿用于图集", "ok");
                    return;
                }
                InitTextureList();
                OnHandleRefreshLog();
                if (EditorUtility.DisplayDialog("导出完成", String.Format("导出完成,是否打开{0}", recordPath), "打开", "关闭"))
                {
                    UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(recordPath, 1);
                }
            }
            if (GUILayout.Button("一键修改"))
            {
                if (EditorUtility.DisplayDialog("一键修改", "将修改勾选图片的所有勾选问题", "确定", "取消"))
                {
                    SolutionAll();
                    OnHandleRefreshLog();
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            GUILayout.EndArea();
        }

        private void RefreshSelectAllTexture(bool selected)
        {
            foreach (var wrong in _wrongList)
            {
                wrong.isSelected = selected;
            }
        }
        private void SolutionAll()
        {
            foreach (var wrong in _wrongList)
            {
                if (!wrong.isSelected) continue;
                TextureImporter importer = AssetImporter.GetAtPath(wrong.path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogError(wrong.path + "NO TextureImporter");
                    continue;
                }
                if (((wrong.wrongType | WrongType.NotUseETC2) != 0 || (wrong.wrongType | WrongType.ShouldNotBeETC24Bit) != 0 ||
                    (wrong.wrongType | WrongType.ShouldNotBeETC28Bit) != 0 || (wrong.wrongType | WrongType.UseAutoFormat) != 0) && selectedFormat)
                {
                    TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings("Android");
                    settings.overridden = true;
                    bool hasAlpha = wrong.textureHasAlpha == CheckAlphaType.HasAlpha;
                    if (wrong.textureHasAlpha == CheckAlphaType.NotCheck)
                    {
                        hasAlpha = CheckHasAlpha(wrong.texture);
                    }
                    settings.format = hasAlpha ? TextureImporterFormat.ETC2_RGBA8 : TextureImporterFormat.ETC2_RGB4;
                    importer.SetPlatformTextureSettings(settings);
                }

                if (selectedMipMap && (wrong.wrongType | WrongType.MipMap) != 0)
                {
                    importer.mipmapEnabled = false;
                }
                if (selectedAlpha && (wrong.wrongType | WrongType.AlphaIsTransparency) != 0)
                {
                    importer.alphaIsTransparency = true;
                }
                if (selectedReadWrite && (wrong.wrongType | WrongType.ReadWrite) != 0)
                {
                    importer.isReadable = false;
                }
                // AssetDatabase.WriteImportSettingsIfDirty(wrong.path);
                importer.SaveAndReimport();
            }
        }
        public void OnHandleRefreshLog()
        {
            _csFuncIndex = 1;
            _sb1.Clear();
            _wrongList.Clear();
            Append1("                  单图问题列表");
            for (int i = 0, num = texturePathList.Count; i < num; i++)
            {
                if (texturePathList[i].StartsWith("Assets/LoadResources/UI/Backpack") || texturePathList[i].StartsWith("Assets/LoadResources/UISpriteAtlas")) continue;
                bool hasData = false;
                foreach (KeyValuePair<string, SingleSpriteCheckItem> pair in treeItemDict)
                {
                    if (pair.Value.func != null)
                    {
                        hasData = (pair.Value.func(texturePathList[i], pair.Value) || hasData);
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
            List<string> assetList = new List<string>();

            string[] filePathArray = Directory.GetFiles(directoryPath, "*", SearchOption.AllDirectories);

            foreach (string filePath in filePathArray)
            {
                string filePathx = filePath.Replace("\\", "/").Replace(Application.dataPath, "Assets");

                if (filePathx.EndsWith(".Png") || filePathx.EndsWith(".PNG") || filePathx.EndsWith(".png") ||
                    filePathx.EndsWith(".Jpg") || filePathx.EndsWith(".JPG") || filePathx.EndsWith(".jpg"))
                {
                    assetList.Add(filePathx);
                }
            }
            return assetList;
        }

        private void RefreshSelectAll(bool select)
        {
            foreach (KeyValuePair<string, SingleSpriteCheckItem> pair in treeItemDict)
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
