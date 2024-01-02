using System.Security.AccessControl;
using System.Runtime.CompilerServices;
using System.Globalization;
using UnityEngine;
using System.Diagnostics;
using System;
using UnityEditor;
using System.IO;
using System.Xml;
using System.Collections.Generic;
using UnityEditorInternal;
using System.Text;
using Debug = UnityEngine.Debug;

namespace Gala.FrameworkEditorTools
{
    [Serializable]
    public class ImgInfo
    {
        public string pathStr;
        public TextureImporterType textureType = TextureImporterType.Default;
        public TextureImporterFormat androidFormat = TextureImporterFormat.ASTC_4x4;
        public TextureImporterFormat iOSFormat = TextureImporterFormat.ASTC_4x4;
        public ImageAutoChangeGUIWin.MaxSize textureSize = ImageAutoChangeGUIWin.MaxSize.Size_1024;
        public string type;

        public ImgInfo(string commandStr, TextureImporterType textureType, TextureImporterFormat androidformat, TextureImporterFormat iOSformat, ImageAutoChangeGUIWin.MaxSize size, string type)
        {
            this.pathStr = commandStr;
            this.textureType = textureType;
            this.androidFormat = androidformat;
            this.iOSFormat = iOSformat;
            this.textureSize = size;
            this.type = type;
        }

    }

    public class ImgInfoObject : ScriptableObject
    {
        public List<string> imgTypeList = new List<string>();
        public List<ImgInfo> imgInfoList = new List<ImgInfo>();
    }

    public class ImageAutoChangeGUIWin : EditorWindow
    {
        public enum MaxSize
        {
            Size_32 = 32,
            Size_64 = 64,
            Size_128 = 128,
            Size_256 = 256,
            Size_512 = 512,
            Size_1024 = 1024,
            Size_2048 = 2048,
            Size_4096 = 4096,
        }

        private void OnDestroy() 
        {
            imgInfoObject = null;
        }

        void Reset()
        {
            InitXML();
        }

        public const string ImgInfoFileName = "ImgInfo";
        static string ImgInfoFilePath = "";

        public static void OpenImgInfoGUIWin()
        {
            ImgInfoFilePath = Path.Combine(Application.dataPath, "Resources", ImgInfoFileName + ".xml");

            ImageAutoChangeGUIWin window = EditorWindow.GetWindow(typeof(ImageAutoChangeGUIWin), false, "IMG指令信息修改", false) as ImageAutoChangeGUIWin;
            window.Show();
        }

        #region 解析xml
        private ImgInfoObject imgInfoObject;
        private void InitXML()
        {
            TextAsset imgTextAsset = Resources.Load(ImgInfoFileName) as TextAsset;
            if (imgTextAsset != null)
            {
                XmlDocument XmlDoc = new XmlDocument();
                XmlDoc.LoadXml(imgTextAsset.text);
                XmlNodeList imgInfoXmlNodeList = XmlDoc.GetElementsByTagName("ImgInfo");

                imgInfoObject = ScriptableObject.CreateInstance<ImgInfoObject>();

                foreach (XmlNode node in imgInfoXmlNodeList)
                {
                    ImgInfo info = new ImgInfo(node.Attributes["ImgPath"].Value,
                                                (TextureImporterType)Enum.Parse(typeof(TextureImporterType), node.Attributes["TextureType"].Value),
                                                (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), node.Attributes["AndroidFormat"].Value),
                                                (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), node.Attributes["IOSFormat"].Value),
                                                (MaxSize)Enum.Parse(typeof(MaxSize), node.Attributes["Size"].Value),
                                                node.Attributes["Type"].Value);
                    imgInfoObject.imgInfoList.Add(info);
                    imgInfoObject.imgTypeList.Add(info.type);

                }
            }
        }
        #endregion

        private int toolBar;
        private string[] toolList = { "图片类型", "图片指令修改" };
        private ReorderableList imgTypeReorderableList;
        private int selectionGrid;
        private SerializedObject serializedObject = null;
        private bool imgInfoObjectNullFlag = false;

        private void OnEnable()
        {
            imgInfoObjectNullFlag = (imgInfoObject == null);
            if (imgInfoObjectNullFlag)
            {
                return;
            }
            serializedObject = new SerializedObject(imgInfoObject);
            imgTypeReorderableList = new ReorderableList(imgInfoObject.imgTypeList, typeof(string), true, false, true, true);
            imgTypeReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                SerializedProperty imgTypeListSP = serializedObject.FindProperty("imgTypeList");
                SerializedProperty itemData = imgTypeListSP.GetArrayElementAtIndex(index);
                itemData.stringValue = EditorGUI.TextField(new Rect(rect.x + 10, rect.y, rect.width, rect.height), itemData.stringValue);
            };
            imgTypeReorderableList.onAddCallback = (ReorderableList reorderableList) =>
            {
                imgTypeReorderableList.list.Add("新类别");
            };

        }

        private void OnGUI()
        {
            GUIStyle middleTextGUIStyle = new GUIStyle();
            middleTextGUIStyle.alignment = TextAnchor.MiddleCenter;
            middleTextGUIStyle.stretchWidth = true;
            middleTextGUIStyle.stretchHeight = true;
            if (imgInfoObjectNullFlag)
            {
                GUILayout.Label("加载失败", middleTextGUIStyle);
                if (GUILayout.Button("创建一个xml"))
                {
                    SaveAll("创建成功");
                }
                return;
            }
            if (imgInfoObject == null || imgInfoObject.imgTypeList == null || imgInfoObject.imgInfoList == null)
            {
                GUILayout.Label("加载中", middleTextGUIStyle);
                return;
            }
            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal("Box");

            int lastToolBar = toolBar;
            toolBar = GUILayout.Toolbar(toolBar, toolList);

            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (toolBar == 0)
            {
                serializedObject.Update();
                imgTypeReorderableList.DoLayoutList();
                serializedObject.ApplyModifiedProperties();

                if (GUILayout.Button("批量刷图片格式"))
                {
                    ChangeFormat();
                }
            }
            else if (toolBar == 1)
            {
                serializedObject.Update();
                serializedObject.ApplyModifiedProperties();

                GUILayout.BeginHorizontal("Box");
                List<string> imgTypeFullList = new List<string>();
                foreach (string imgType in imgInfoObject.imgTypeList)
                {
                    imgTypeFullList.Add(imgType);
                }

                selectionGrid = GUILayout.SelectionGrid(selectionGrid, imgTypeFullList.ToArray(), 4);
                GUILayout.EndHorizontal();

                RefreshImgInfo();

                GUILayout.FlexibleSpace();
                GUILayout.Space(20);
                if (GUILayout.Button("保存"))
                {
                    SaveAll("保存成功");
                }
            }

            GUILayout.Space(20);
            GUILayout.EndVertical();
        }

        private void RefreshImgInfo()
        {
            if (imgInfoObject.imgTypeList.Count == 0) return;

            string imgTypeName = imgInfoObject.imgTypeList[selectionGrid];

            ImgInfo imgInfo = imgInfoObject.imgInfoList.Find(x => x.type == imgTypeName);
            if (imgInfo == null)
            {
                imgInfo = new ImgInfo("Assets/LoadResources/UI", TextureImporterType.Default, TextureImporterFormat.ETC2_RGB4, TextureImporterFormat.ASTC_5x5, MaxSize.Size_1024, imgTypeName);
                imgInfoObject.imgInfoList.Add(imgInfo);
                Debug.LogError("创建一个新的路径 " + imgTypeName);
            }
            GUILayout.BeginHorizontal();
            imgInfo.pathStr = EditorGUILayout.TextField("图片资源路径", imgInfo.pathStr);
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(18), GUILayout.Height(18)))
            {
                imgInfo.pathStr = EditorUtility.OpenFolderPanel("图片路径选择", "", "");
            }
            GUILayout.EndHorizontal();

            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField("图片类型");
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            imgInfo.textureType = (TextureImporterType)Enum.Parse(typeof(TextureImporterType), EditorGUILayout.EnumPopup("选择图片类型：", imgInfo.textureType).ToString());
            GUILayout.EndHorizontal();


            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField("Android格式");
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            imgInfo.androidFormat = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), EditorGUILayout.EnumPopup("选择Android格式：", imgInfo.androidFormat).ToString());
            GUILayout.EndHorizontal();

            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField("iOS格式");
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            imgInfo.iOSFormat = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), EditorGUILayout.EnumPopup("选择iOS格式：", imgInfo.iOSFormat).ToString());
            GUILayout.EndHorizontal();

            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            EditorGUILayout.TextField("图片尺寸大小");
            GUILayout.EndHorizontal();

            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            imgInfo.textureSize = (MaxSize)EditorGUILayout.EnumPopup("尺寸：", imgInfo.textureSize);
            GUILayout.EndHorizontal();

        }


        private void SaveAll(string desc)
        {
            XmlDocument XmlDoc = new XmlDocument();
            XmlDeclaration dec = XmlDoc.CreateXmlDeclaration("1.0", "utf-8", null);//设置声明 
            XmlDoc.AppendChild(dec);
            XmlElement root = XmlDoc.CreateElement("ImgInfoTree");//加入根节点 
            XmlDoc.AppendChild(root);

            if (imgInfoObject != null)
            {

                XmlElement imgInfoGroupElement = XmlDoc.CreateElement("ImgInfoGroup");
                XmlDoc.DocumentElement.AppendChild(imgInfoGroupElement);

                foreach (var imgInfo in imgInfoObject.imgInfoList)
                {
                    if (!imgInfoObject.imgTypeList.Contains(imgInfo.type)) continue;

                    XmlElement imgInfoElement = XmlDoc.CreateElement("ImgInfo");
                    imgInfoElement.SetAttribute("ImgPath", imgInfo.pathStr);
                    imgInfoElement.SetAttribute("TextureType", imgInfo.textureType.ToString());
                    imgInfoElement.SetAttribute("AndroidFormat", imgInfo.androidFormat.ToString());
                    imgInfoElement.SetAttribute("IOSFormat", imgInfo.iOSFormat.ToString());
                    imgInfoElement.SetAttribute("Size", imgInfo.textureSize.ToString());
                    imgInfoElement.SetAttribute("Type", imgInfo.type);
                    imgInfoGroupElement.AppendChild(imgInfoElement);
                }
            }
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            using (var xmlTextWriter = XmlWriter.Create(ImgInfoFilePath, settings))
            {
                XmlDoc.Save(xmlTextWriter);
                xmlTextWriter.Flush();
            }
            AssetDatabase.Refresh();
            EditorUtility.DisplayDialog("", desc, "OK");
        }

        private void ChangeFormat()
        {
            foreach (var imgInfo in imgInfoObject.imgInfoList)
            {
                List<string> lst = GetAllTexPaths(imgInfo.pathStr);
                TextureImporterPlatformSettings androidPlatformSettings = new TextureImporterPlatformSettings();
                androidPlatformSettings.name = "Android";
                androidPlatformSettings.crunchedCompression = true;
                androidPlatformSettings.overridden = true;
                int i = 0;
                EditorUtility.DisplayProgressBar("修改", imgInfo.pathStr + "修改图片格式", 0);
                for (; i < lst.Count; i++)
                {
                    ChangeOne(lst[i], imgInfo);
                    EditorUtility.DisplayProgressBar("转换", string.Format("修改图片格式    {0}/{1}", i, lst.Count), i / (float)lst.Count);
                }
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
            EditorUtility.DisplayDialog("", "转换结束", "OK");
        }

        private void ChangeOne(string path, ImgInfo imgInfo)
        {
            path = path.Substring(path.IndexOf("Assets"));
            try
            {
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                textureImporter.textureType = imgInfo.textureType;
                //--设置平台格式--
                TextureImporterPlatformSettings importerSettings_Andorid = new TextureImporterPlatformSettings();
                importerSettings_Andorid.overridden = true;
                importerSettings_Andorid.name = "Android";
                importerSettings_Andorid.crunchedCompression = true;
                importerSettings_Andorid.overridden = true;
                importerSettings_Andorid.textureCompression = TextureImporterCompression.Compressed;
                importerSettings_Andorid.maxTextureSize = (int)imgInfo.textureSize;
                importerSettings_Andorid.format = imgInfo.androidFormat;

                TextureImporterPlatformSettings importerSettings_IOS = new TextureImporterPlatformSettings();
                importerSettings_IOS.overridden = true;
                importerSettings_IOS.name = "iPhone";
                importerSettings_IOS.crunchedCompression = true;
                importerSettings_IOS.overridden = true;
                importerSettings_IOS.textureCompression = TextureImporterCompression.Compressed;
                importerSettings_IOS.maxTextureSize = (int)imgInfo.textureSize;
                importerSettings_IOS.format = imgInfo.iOSFormat;
                //-----
                textureImporter.SetPlatformTextureSettings(importerSettings_Andorid);
                textureImporter.SetPlatformTextureSettings(importerSettings_IOS);

                textureImporter.SaveAndReimport();
                AssetDatabase.ImportAsset(path);
            }
            catch
            {
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
        }

        string TexSuffix = "*.bmp|*.tga|*.jpg|*.gif|*.png|*.tif|*.psd";
        private List<string> GetAllTexPaths(string rootPath)
        {
            List<string> lst = new List<string>();
            string[] types = TexSuffix.Split('|');
            for (int i = 0; i < types.Length; i++)
            {
                lst.AddRange(Directory.GetFiles(rootPath, types[i], SearchOption.AllDirectories));
            }
            return lst;
        }

    }
}