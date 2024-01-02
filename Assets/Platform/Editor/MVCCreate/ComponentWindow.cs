using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GalaFramework
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using UnityEditor;
    using UnityEditor.IMGUI.Controls;
    using UnityEditor.SceneManagement;
    using UnityEngine.UI;

    [MvcWinodws(2)]
    public class ComponentWindow : IWindows
    {
        public static string AtlasPathBase = "Assets/LoadResources/UISpriteAtlas/Sprites/";
        public static string CloseBtnImage = "UI_common_windos_close_bg|UI_common_windos_btn_bg";
        public static string CloseBtnPos = "25|25";

        public static int PrefabSize = 300;


        public Rect position { get; set; }
        public int Orde { get; set; }

        Vector2 ScrollPos;

        [MvcInject]
        ComponentData componentData;

        string prefabPath;

        GameObject root;
        int id;
        int firstId;
        Color color2 = new Color(0.2f, 0.2f, 0.2f, 0.6f);

        public bool Modify;
        private string currentInspectorName;
        private readonly string defaultInspectorName = "View";

        EventInfo stageOpend = null;
        EventInfo stageClose = null;
        EventInfo stageSave = null;

        Delegate stageOpendChange;
        Delegate stageCloseChange;
        Delegate valueChange;
        Delegate prefabReloaded;
        Delegate prefabSaved;

        Dictionary<int, MVCTreeItem> mVCTreeItems = new Dictionary<int, MVCTreeItem>();
        Action repaint;

        public static HierarchyWindowItemCustom HierarchyWindowItemCustomer { get; set; }

        string[] NormalType = { "UnityEngine.Transform" };
        public static Action CreateAction;
        public void Init(Action Repaint)
        {
            CreateAction = () => { if (BaseData.CurrentOperate != null) OnCreateBtn(BaseData.CurrentOperate.isHotfix); };
            repaint = Repaint;

            var reflection = componentData.GetReflection();

            var methodinfo = this.GetType().GetMethod(nameof(ValueChange));
            valueChange = Delegate.CreateDelegate(reflection.GetStageChangeType(), this, methodinfo);
            reflection.AddStageChangedEventHandler(valueChange);

            var reloadmethodinfo = this.GetType().GetMethod(nameof(PrefabReloaded));
            prefabReloaded = Delegate.CreateDelegate(reflection.GetPrefabStageReloadedType(), this, reloadmethodinfo);
            reflection.AddPrefabStageReloadedEventHandler(prefabReloaded);

            var prefabStage = typeof(Editor).Assembly.GetType("UnityEditor.Experimental.SceneManagement.PrefabStage");
            if (prefabStage == null)
            {
                prefabStage = typeof(Editor).Assembly.GetType("UnityEditor.SceneManagement.PrefabStage");
            }
            // PrefabStage.prefabSaved += OnPrefabOpen;
            // PrefabStage.prefabStageClosing += OnPrefabClose;
            // PrefabStage.prefabStageOpened += OnPrefabOpen;

            foreach (var item in prefabStage.GetRuntimeEvents())
            {
                switch (item.Name)
                {
                    case "prefabStageOpened":
                        stageOpend = item;
                        break;
                    case "prefabStageClosing":
                        stageClose = item;
                        break;
                    case "prefabSaved":
                        stageSave = item;
                        break;
                }
            }


            var StageChangemethodinfo = this.GetType().GetMethod(nameof(OnPrefabClose));
            stageCloseChange = Delegate.CreateDelegate(reflection.GetPrefabStageDirtinessChangedType(), this, StageChangemethodinfo);
            stageClose.AddEventHandler(null, stageCloseChange);

            stageOpendChange = Delegate.CreateDelegate(reflection.GetPrefabStageDirtinessChangedType(), this, this.GetType().GetMethod(nameof(OnPrefabOpen)));
            stageOpend.AddEventHandler(null, stageOpendChange);

            prefabSaved = Delegate.CreateDelegate(stageSave.EventHandlerType, this, this.GetType().GetMethod(nameof(OnPrefabOpen)));
            stageSave.AddEventHandler(null, prefabSaved);

            SetRoot();
  
            EditorApplication.hierarchyWindowItemOnGUI += RecallValue;
        }

        string rootName = string.Empty;
        string rootPath = string.Empty;
        List<string> closeBtnList = new List<string>();
        long goSize = 0;

        public void SetRoot()
        {
            var reflection = componentData.GetReflection();
            var value = reflection.GetPrefabRoot();
            if (value == null)
            {
                return;
            }
            EditorApplication.hierarchyWindowItemOnGUI -= RecallValue;
            EditorApplication.hierarchyWindowItemOnGUI += RecallValue;
            prefabPath = reflection.GetPrefabPath();

            root = value as GameObject;
            rootName = root.transform.name;
            rootNameFirstwordLower = "";
            for (int i = 1; i < rootName.Length; i++)
            {
                if (Char.IsUpper(rootName[i]))
                {
                    break;
                }
                else
                {
                    rootNameFirstwordLower += rootName[i];
                }
            }
            rootNameFirstwordLower = rootNameFirstwordLower.ToLower();
            goSize = new FileInfo(prefabPath).Length;

            InitMvcInfo(rootName);
            InitRoot(root.transform);
            InitCommonAtlasRoot();
        }

        private void InitCommonAtlasRoot()
        {
            string path = PlayerPrefs.GetString("AtlasPathBase");
            if (!string.IsNullOrEmpty(path)) AtlasPathBase = path;
            closeBtnList.Clear();
            string closeBtnName = PlayerPrefs.GetString("CloseBtnName");
            if (!string.IsNullOrEmpty(path)) CloseBtnImage = closeBtnName;
            string[] closeNameArray = CloseBtnImage.Split("|".ToCharArray());
            closeBtnList.AddRange(closeNameArray);
            rootPath = AssetDatabase.GetAssetPath(root);
            string closePos = PlayerPrefs.GetString("CloseBtnPos");
            if (!string.IsNullOrEmpty(path)) CloseBtnPos = closePos;
            string[] closePosArray = CloseBtnPos.Split("|".ToCharArray());
            CloseBtnPosV2 = new Vector2(Convert.ToInt32(closePosArray[0]), Convert.ToInt32(closePosArray[1]));
            btnColorDict.Clear();
            btnVertor2Dict.Clear();
        }

        public void OnPrefabOpen(object value)
        {
            EditorApplication.hierarchyWindowItemOnGUI -= RecallValue;
            EditorApplication.hierarchyWindowItemOnGUI += RecallValue;
            bool discard = true;
            if (Modify)
            {
                discard = EditorUtility.DisplayDialog("未保存内容", "是否丢弃未生成ComponentItemKey标签", "丢弃", "保留");
            }
            if (discard)
            {
                SetRoot();
                Modify = false;
                repaint?.Invoke();
            }
        }

        public void OnPrefabClose(object value)
        {
            Modify = false;
            currentInspectorName = null;
            var reflection = componentData.GetReflection();
            var tValue = reflection.GetPrefabRoot();
            if (tValue != null)
                return;
            BaseData.CurrentOperate = null;
            EditorApplication.hierarchyWindowItemOnGUI -= RecallValue;
            repaint?.Invoke();
        }

        public void PrefabReloaded(object handler)
        {
            SetRoot();
        }

        public void ValueChange(object sender, object handler)
        {
            SetRoot();
        }

        public void OnClose()
        {
            stageClose.RemoveEventHandler(null, stageCloseChange);
            stageOpend.RemoveEventHandler(null, stageOpendChange);
            stageSave.RemoveEventHandler(null, prefabSaved);
            componentData.GetReflection().RemoveStageChangedEventHandler(valueChange);
            componentData.GetReflection().RemovePrefabStageReloadedEventHandler(prefabReloaded);
            EditorApplication.hierarchyWindowItemOnGUI -= RecallValue;
        }

        private void InitMvcInfo(string mvcName)
        {
            componentData.MainData().TryGetValue(mvcName, out BaseData.CurrentOperate);
            if (BaseData.CurrentOperate == null)
            {
                BaseData.CurrentOperate = new MVCInfo();
                BaseData.CurrentOperate.ModuleName = string.Empty;
                BaseData.CurrentOperate.Model = false;
                BaseData.CurrentOperate.View = true;
                BaseData.CurrentOperate.Controll = false;
                BaseData.CurrentOperate.MvcName = mvcName;
            }
            componentData.CurrentCreateInfo.Clear();
            componentData.CurrentCreateInfo.Add(BaseData.CurrentOperate, new List<Transform>());
        }

        private static bool CompareChar(char c)
        {
            if (c >= 'a' && c <= 'z')
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        private static bool CompareCharNumber(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return true;
            }
            else
            {
                return false;
            }
        }
        // 判断一个数字是否是小数
        private static bool IsNought(float f)
        {
            //    Debug.LogError("f = " + f);
            string str = f.ToString();
            int index = str.IndexOf(".");
            //   Debug.LogError("index = " + index);
            if (index < 0)
            {
                return false;
            }
            else
            {
                for (int i = index + 1; i < str.Length; i++)
                {
                    //       Debug.LogError("str[i] = " + str[i]);
                    if (str[i] != '0')
                    {
                        return true;
                    }
                }
                return false;
            }
        }

        private static bool IsNought(Vector3 data)
        {
            // Debug.LogError(data.ToString());
            return IsNought(data.x) || IsNought(data.y) || IsNought(data.z);
        }

        public static Vector3 GetLocalCenterPos(RectTransform trans)
        {
            return new Vector3(25 + (trans.pivot.x * trans.rect.width), 25 - ((1f - trans.pivot.y) * trans.rect.height), trans.localPosition.z);
        }
        string rootNameFirstwordLower;
        Vector2 CloseBtnPosV2 = new Vector2(25, 25);
        Dictionary<string, Color[]> btnColorDict = new Dictionary<string, Color[]>();
        Dictionary<string, Vector2> btnVertor2Dict = new Dictionary<string, Vector2>();

        Action<Transform, Rect, Func<bool>> DrawStandardizeEventHandler;

        private void DrawStandardize(MVCTreeItem TreeItem, Rect rect1)
        {
            StringBuilder helpBoxStr = new StringBuilder();
            StringBuilder errorStr = new StringBuilder();
            int index = 1;
            rect1.x -= 20;

            if (TreeItem.ItemTsf != null)
            {
                Image image = TreeItem.ItemTsf.GetComponent<Image>();
                if (image != null)
                {
                    if (image.sprite == null)
                    {
                        //helpBoxStr.AppendLine(index++ + ".image 节点sprite为空");
                        //GUI.color = Color.red;
                        //rect1.x -= 132;
                        //EditorGUI.LabelField(rect1, index++ + ":image 节点sprite为空");
                        if (image.color.a != 0)
                        {
                            //GUI.color = Color.red;
                            //rect1.x -= 172;
                            //EditorGUI.HelpBox(rect1);
                            errorStr.AppendLine(index++ + ".image为空Alpha不设0会白屏");
                        }
                    }
                    else
                    {
                        if (image.sprite.name == "Background")
                        {
                            // GUI.color = Color.red;
                            //rect1.x -= 170;
                            errorStr.AppendLine(index++ + ".image 包含默认Background");
                            //EditorGUI.LabelField(rect1, index++ + ":image 包含默认Background");
                        }
                        if (image.sprite.name == "UISprite")
                        {
                            //GUI.color = Color.red;
                            //rect1.x -= 160;
                            errorStr.AppendLine(index++ + ".image 包含默认UISprite");
                            //EditorGUI.LabelField(rect1, index++ + ":image 包含默认UISprite");
                        }
                        if (image.sprite.name == "UIMask")
                        {
                            //GUI.color = Color.red;
                            //rect1.x -= 160;
                            errorStr.AppendLine(index++ + ".image 包含默认UIMask");
                            // EditorGUI.LabelField(rect1, index++ + ":image 包含默认UIMask");
                        }
                        if (closeBtnList.Contains(image.sprite.name))
                        {
                            Vector3 pos = image.transform.GetComponent<RectTransform>().anchoredPosition3D;
                            if (image.transform.GetComponent<RectTransform>().anchorMax != Vector2.one || image.transform.GetComponent<RectTransform>().anchorMax != Vector2.one)
                            {
                                //GUI.color = Color.red;
                                //rect1.x -= 140;
                                errorStr.AppendLine(index++ + ".close btn锚点不是右上");
                                //EditorGUI.LabelField(rect1, index++ + ":close btn锚点不是右上");
                            }
                            else if (pos.x != CloseBtnPosV2.x || pos.y != CloseBtnPosV2.y)
                            {
                                //GUI.color = Color.red;
                                // rect1.x -= 203;
                                errorStr.AppendLine(index++ + ".close btn位置不是右上" + CloseBtnPosV2);
                                //EditorGUI.LabelField(rect1, index++ + ":close btn位置不是右上" + CloseBtnPosV2);
                            }
                        }
                        string path = AssetDatabase.GetAssetPath(image.sprite);
                        int uiIndex = path.IndexOf(AtlasPathBase);
                        if (uiIndex != -1)
                        {
                            string path1 = path.Replace(AtlasPathBase, "");
                            uiIndex = path1.IndexOf("/");
                            if (uiIndex != -1)
                            {
                                path1 = path1.Substring(0, uiIndex);
                                var lower = path1.ToLower();
                                if (!lower.Contains("common") && lower != rootNameFirstwordLower)
                                {
                                    if (rootNameFirstwordLower.Contains(lower) || lower.Contains(rootNameFirstwordLower))
                                    {

                                    }
                                    else
                                    {
                                        //GUI.color = Color.red;
                                        //rect1.x -= 180;
                                        errorStr.AppendLine(index++ + ".图片引用了" + path1 + "图集资源");
                                        //EditorGUI.LabelField(rect1, index++ + ":图片引用了" + path1 + "图集资源");
                                    }
                                }
                            }
                        }
                        else
                        {
                            //GUI.color = Color.yellow;
                            //rect1.x -= 125;
                            helpBoxStr.AppendLine(index++ + ".引用图片不在图集内");
                            //EditorGUI.LabelField(rect1, index++ + ":引用图片不在图集内");
                        }

                        // Texture2D texture2d = image.sprite.texture;
                        // Color[] pixels;
                        // Vector2 texSize = Vector2.zero;
                        // if (!btnColorDict.TryGetValue(texture2d.name, out pixels))
                        // {
                        //     if (texture2d.isReadable)
                        //     {
                        //         pixels = texture2d.GetPixels();
                        //         btnVertor2Dict.Add(texture2d.name, new Vector2(texture2d.height, texture2d.width));
                        //         btnColorDict.Add(texture2d.name, pixels);
                        //     }
                        //     else
                        //     {

                        //         AssetImporter impor = AssetImporter.GetAtPath(path);
                        //         if (impor != null)
                        //         {
                        //             TextureImporter importer = impor as TextureImporter;
                        //             importer.isReadable = true;
                        //             importer.SaveAndReimport();
                        //             pixels = texture2d.GetPixels();
                        //             btnVertor2Dict.Add(texture2d.name, new Vector2(texture2d.height, texture2d.width));
                        //             btnColorDict.Add(texture2d.name, pixels);
                        //             importer.isReadable = false;
                        //             importer.SaveAndReimport();
                        //         }
                        //     }
                        // }
                        // if (pixels != null)
                        // {
                        //     btnVertor2Dict.TryGetValue(texture2d.name, out texSize);
                        //     if (texSize.x >= 2 && texSize.y >= 2)
                        //     {
                        //         int topLeftIndex = 0;
                        //         int topRightIndex = (int)texSize.y - 1;
                        //         int bottomRightIndex = pixels.Length - 1;
                        //         int centerLeftIndex = (int)(texSize.x / 2) * (int)texSize.y;
                        //         int centerRightIndex = centerLeftIndex + (int)texSize.y - 1;
                        //         int centerTopIndex = (int)(texSize.y / 2);
                        //         int centerBottomIndex = bottomRightIndex - (int)(texSize.y / 2);
                        //         int bottomLeftIndex = bottomRightIndex - (int)texSize.y + 1;


                        //         if (((pixels[topLeftIndex].a <= 0.5f && pixels[topLeftIndex + 1].a >= 0.5f) && (pixels[bottomLeftIndex].a <= 0.5f && pixels[bottomLeftIndex + 1].a >= 0.5f)
                        //         && (pixels[centerLeftIndex + 1].a <= 0.5f) && (pixels[centerLeftIndex].a >= 0.5f)) ||

                        //         ((pixels[topRightIndex].a <= 0.5f && pixels[topRightIndex - 1].a >= 0.5f) && (pixels[centerRightIndex].a <= 0.5f && pixels[centerRightIndex - 1].a >= 0.5f)
                        //         && (pixels[bottomRightIndex].a <= 0.5f) && (pixels[bottomRightIndex - 1].a >= 0.5f)) ||

                        //           ((pixels[topLeftIndex].a <= 0.5f && pixels[topLeftIndex + (int)texSize.y].a >= 0.5f) && (pixels[centerTopIndex].a <= 0.5f && pixels[centerTopIndex + (int)texSize.y].a >= 0.5f)
                        //         && (pixels[topRightIndex].a <= 0.5f) && (pixels[topRightIndex + (int)texSize.y].a >= 0.5f)) ||

                        //           (pixels[bottomLeftIndex].a <= 0.5f && pixels[bottomLeftIndex - (int)texSize.y].a >= 0.5f) && (pixels[centerBottomIndex].a <= 0.5f && pixels[centerBottomIndex - (int)texSize.y].a >= 0.5f)
                        //         && (pixels[bottomRightIndex].a <= 0.5f) && (pixels[bottomRightIndex - (int)texSize.y].a >= 0.5f))
                        //         {
                        //             GUI.color = Color.red;
                        //             string textTip = index++ + ":图片" + texture2d.name + "边缘空白一个像素";
                        //             rect1.x -= 220;
                        //             EditorGUI.LabelField(rect1, textTip);
                        //         }
                        //     }

                        // }



                    }
                }
                string name = TreeItem.ItemTsf.name;
                if (name.Contains(" "))
                {
                    //GUI.color = Color.red;
                    //rect1.x -= 75;
                    errorStr.AppendLine(index++ + ".命名含空格");
                    //EditorGUI.LabelField(rect1, index++ + ":命名含空格");
                }
                if (CompareChar(name[0]))
                {
                    //GUI.color = Color.yellow;
                    //rect1.x -= 100;
                    helpBoxStr.AppendLine(index++ + ".命名首字母小写");
                    //EditorGUI.LabelField(rect1, index++ + ":命名首字母小写");
                }
                if (CompareCharNumber(name[0]))
                {
                    //GUI.color = Color.yellow;
                    // rect1.x -= 100;
                    //EditorGUI.LabelField(rect1, index++ + ":命名首字符数字");
                    helpBoxStr.AppendLine(index++ + ".命名首字符数字");
                }
                //GUI.color = Color.white;
                Mask mask = TreeItem.ItemTsf.GetComponent<Mask>();
                if (mask != null)
                {
                    //GUI.color = Color.yellow;
                    //rect1.x -= 134;
                    helpBoxStr.AppendLine(index++ + ".是否使用RectMask2D");
                    //EditorGUI.LabelField(rect1, index++ + ":是否使用RectMask2D");
                }
                //GUI.color = Color.white;
                Text text = TreeItem.ItemTsf.GetComponent<Text>();
                if (text != null)
                {
                    Font font = text.font;
                    if (font == null)
                    {
                        //GUI.color = Color.red;
                        //rect1.x -= 63;
                        errorStr.AppendLine(index++ + ".字体为空");
                        //EditorGUI.LabelField(rect1, index++ + ":字体为空");
                    }
                    else if (font.name == "Arial")
                    {
                        //GUI.color = Color.red;
                        //rect1.x -= 100;
                        errorStr.AppendLine(index++ + ".字体使用了Arial");
                        //EditorGUI.LabelField(rect1, index++ + ":字体使用了Arial");
                    }
                    if (!string.IsNullOrEmpty(text.text))
                    {
                        Component language = TreeItem.ItemTsf.GetComponent("LanguageComponent");
                        if (language == null)
                        {
                            //GUI.color = Color.red;
                            // rect1.x -= 100;
                            errorStr.AppendLine(index++ + ".默认文本不为空");
                            //EditorGUI.LabelField(rect1, index++ + ":默认文本不为空");
                        }
                    }
                }

                // image

                RectTransform rectTrans = TreeItem.ItemTsf.GetComponent<RectTransform>();
                Vector3 localPos = rectTrans != null ? rectTrans.anchoredPosition3D : TreeItem.ItemTsf.localPosition;

                if (IsNought(localPos))
                {
                    //GUI.color = Color.red;
                    //rect1.x -= 114;
                    errorStr.AppendLine(index++ + ".Position含小数点");
                    //EditorGUI.LabelField(rect1, index++ + ":Position含小数点");
                }
                if (localPos.z != 0)
                {
                    //GUI.color = Color.red;
                    // rect1.x -= 95;
                    errorStr.AppendLine(index++ + ".Pos z值不为零");
                    //EditorGUI.LabelField(rect1, index++ + ":Pos z值不为零");
                }
                if (IsNought(TreeItem.ItemTsf.localScale))
                {
                    //GUI.color = Color.red;
                    //rect1.x -= 121;
                    errorStr.AppendLine(index++ + ".localScale含小数点");
                    //EditorGUI.LabelField(rect1, index++ + ":localScale含小数点");
                }
                if (TreeItem.ItemTsf.localScale.x != 1 || TreeItem.ItemTsf.localScale.y != 1 || TreeItem.ItemTsf.localScale.z != 1)
                {
                    //GUI.color = Color.yellow;
                    //rect1.x -= 88;
                    errorStr.AppendLine(index++ + ".Scale不全为1");
                    //EditorGUI.LabelField(rect1, index++ + ":Scale不全为1");
                }
                // GUI.color = Color.white;

                Button btn = TreeItem.ItemTsf.GetComponent<Button>();
                if (btn != null)
                {
                    if (btn.image == null)
                    {
                        //GUI.color = Color.red;
                        //rect1.x -= 114;
                        helpBoxStr.AppendLine(index++ + ".按钮没选择image");
                        //EditorGUI.LabelField(rect1, index++ + ":按钮没选择image");
                    }
                    else
                    {
                        RectTransform trans = btn.image.transform.GetComponent<RectTransform>();
                        //    Debug.LogError(trans.rect.width);
                        if (trans.rect.width < 40 || trans.rect.height < 40)
                        {
                            // GUI.color = Color.red;
                            //rect1.x -= 114;
                            errorStr.AppendLine(index++ + ".按钮响应区域过小");
                            //EditorGUI.LabelField(rect1, index++ + ":按钮响应区域过小");
                        }

                    }

                }
                if (goSize >= PrefabSize * 1024)
                {
                    //GUI.color = Color.red;
                    // rect1.x -= 210;
                    errorStr.AppendLine(index++ + ".预制体大小" + GetFormatSize(goSize) + ",超过" + PrefabSize + "k,需拆分");
                    //EditorGUI.LabelField(rect1, "预制体大小" + GetFormatSize(goSize) + ",超过" + PrefabSize + "k,需拆分");
                }

            }

            var endValue = helpBoxStr.ToString();
            var errorValue = errorStr.ToString();
            if (!string.IsNullOrEmpty(endValue))
            {
                GUIContent gUIContent = new GUIContent(EditorGUIUtility.FindTexture("d_console.warnicon"), endValue);
                EditorGUI.LabelField(rect1, gUIContent);
                rect1.x -= 20;
            }
            if (!string.IsNullOrEmpty(errorValue))
            {
                GUIContent gUIContent = new GUIContent(EditorGUIUtility.FindTexture("console.erroricon"), errorValue);
                EditorGUI.LabelField(rect1, gUIContent);
            }
        }

        private string GetFormatSize(long size)
        {
            if (size > 1048576)
            {
                return (size / 1048576f).ToString("F2") + "m";
            }
            else if (size > 1024)
            {
                return (size / 1024f).ToString("F2") + "k";
            }
            return size.ToString();
        }

        private void RecallValue(int instanceID, Rect selectionRect)
        {
            MVCTreeItem TreeItem;
            if (!mVCTreeItems.TryGetValue(instanceID, out TreeItem))
            {
                if (root == null)
                {
                    return;
                }
                var children = root.transform.GetComponentsInChildren<Transform>();
                foreach (var item in children)
                {
                    if (item.gameObject.GetInstanceID() != instanceID) continue;
                    TreeItem = new MVCTreeItem()
                    {
                        ItemTsf = item,
                        IsSelect = false,
                        id = instanceID,
                    };
                    mVCTreeItems.Add(instanceID, TreeItem);
                    break;
                }
                if (TreeItem == null)
                {
                    return;
                }
            }

            Rect rect1 = new Rect(selectionRect);
            var select = TreeItem.IsSelect;

            if (instanceID == root.GetInstanceID())
            {
                var titleRect = new Rect(selectionRect);
                titleRect.x += EditorGUIUtility.singleLineHeight + root.name.Length * 7.2f;
                var titleStyle = new GUIStyle() { alignment = TextAnchor.UpperLeft };
                titleStyle.normal.textColor = Color.grey;
                GUI.Label(titleRect, "[" + currentInspectorName + "]", titleStyle);
            }

            Rect rect = new Rect(selectionRect);
            if (select)
            {
                rect.x += selectionRect.width - 120;
                rect1.x += selectionRect.width - 120;
            }
            else
            {
                rect.x += selectionRect.width - 20;
                rect1.x += selectionRect.width - 20;
            }

            if (HierarchyWindowItemCustomer != null && HierarchyWindowItemCustomer.Runnable)
            {
                HierarchyWindowItemCustomer.OnGUI(TreeItem, rect1);
            }
            else
            {
                DrawStandardize(TreeItem, rect1);
            }
            rect.width = 20;

            if (componentData.CurrentCreateInfo.ContainsKey(BaseData.CurrentOperate))
            {
                if (!TreeItem.IsSelect && componentData.CurrentCreateInfo[BaseData.CurrentOperate].Contains(TreeItem.ItemTsf))
                {
                    TreeItem.IsSelect = true;
                    if (componentData.Tsf4String.ContainsKey(TreeItem.ItemTsf))
                    {
                        TreeItem.CurrentType = componentData.Tsf4String[TreeItem.ItemTsf];
                    }
                }
            }


            if (select != EditorGUI.Toggle(rect, select))
            {
                select = !select;
                TreeItem.IsSelect = select;
            }

            if (select)
            {
                string[] TypeList = null;
                rect.x += 20;
                rect.width = 100;
                if (!componentData.CurrentCreateInfo[BaseData.CurrentOperate].Contains(TreeItem.ItemTsf))
                {
                    componentData.CurrentCreateInfo[BaseData.CurrentOperate].Add(TreeItem.ItemTsf);
                }

                if (TreeItem.TempHasComponent == null)
                {
                    TreeItem.TempHasComponent = new List<string>();
                    TreeItem.TempHasComponent.AddRange(NormalType);

                    var components = TreeItem.ItemTsf.GetComponents<Component>();
                    foreach (var component in components)
                    {
                        if (NormalType.Contains(component.GetType().FullName)) continue;
                        if (component)
                        {
                            TreeItem.TempHasComponent.Add(component.GetType().FullName);
                        }
                    }
                    bool hasValue = false;
                    if (!string.IsNullOrEmpty(TreeItem.CurrentType))
                    {
                        var splitComponents = TreeItem.CurrentType.Split('#');
                        if (splitComponents.Length > 1)
                        {
                            TreeItem.ChangeState(0);
                        }
                        for (int i = 0; i < splitComponents.Length; i++)
                        {
                            var selectedIndex = TreeItem.TempHasComponent.IndexOf(splitComponents[i]);
                            if (selectedIndex >= 0)
                            {
                                TreeItem.SetSelected(selectedIndex + 1);
                                hasValue = true;
                            }
                        }
                    }
                    if (!hasValue)
                    {
                        TreeItem.SetSelected(1);
                    }
                }

                List<string> showStr = new List<string>();
                TreeItem.TempHasComponent.ForEach(x => showStr.Add(x.Substring(x.LastIndexOf(".") + 1)));
                TypeList = new string[showStr.Count + 1];
                TypeList[0] = "Mixed";
                for (int i = 0; i < showStr.Count; i++)
                {
                    TypeList[i + 1] = showStr[i];
                }
                var labelName = TypeList[TreeItem.GetSelecteds()[0]];
                if (GUI.Button(rect, labelName, (GUIStyle)"MiniPopup"))
                {
                    var enables = Enumerable.Repeat(true, TypeList.Length).ToArray();
                    enables[0] = enables.Length != 2;
                    var separator = new bool[TypeList.Length];
                    var selecteds = TreeItem.GetSelecteds();
                    EditorUtility.DisplayCustomMenuWithSeparators(rect, TypeList, enables, separator, selecteds, (userData, options, selected) =>
                    {
                        var item = (MVCTreeItem)userData;
                        item.ChangeState(selected);
                        var currentSelecteds = TreeItem.GetSelecteds();
                        if (currentSelecteds[0] != 0)
                        {
                            TreeItem.CurrentType = TreeItem.TempHasComponent[currentSelecteds[0] - 1];
                        }
                        else
                        {
                            var selectedComponents = new List<string>();
                            for (int i = 1; i < currentSelecteds.Length; i++)
                            {
                                selectedComponents.Add(TreeItem.TempHasComponent[currentSelecteds[i] - 1]);
                            }
                            TreeItem.CurrentType = string.Join("#", selectedComponents);
                        }
                        Modify = true;
                    }, TreeItem);
                    Event.current.Use();
                }
                if (TreeItem.IsSelect && TreeItem.CurrentType == null)
                {
                    var currentSelecteds = TreeItem.GetSelecteds();
                    TreeItem.CurrentType = TreeItem.TempHasComponent[currentSelecteds[0] - 1];
                }

                if (!componentData.Tsf4String.ContainsKey(TreeItem.ItemTsf))
                {
                    componentData.Tsf4String.Add(TreeItem.ItemTsf, TreeItem.CurrentType);
                }
                else
                {
                    componentData.Tsf4String[TreeItem.ItemTsf] = TreeItem.CurrentType;
                }
            }
            else
            {
                if (componentData.CurrentCreateInfo[BaseData.CurrentOperate].Contains(TreeItem.ItemTsf))
                {
                    componentData.CurrentCreateInfo[BaseData.CurrentOperate].Remove(TreeItem.ItemTsf);
                }
                if (componentData.Tsf4String.ContainsKey(TreeItem.ItemTsf))
                {
                    componentData.Tsf4String.Remove(TreeItem.ItemTsf);
                }
            }
            GUI.color = Color.white;
        }


        public void OnGUI(Rect rect)
        {
            if (BaseData.CurrentOperate == null)
            {
                return;
            }
            var verRect = EditorGUILayout.BeginVertical();
            EditorGUI.DrawRect(verRect, color2);
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            GUILayout.Label("Module:", GUILayout.Width(50));
            BaseData.CurrentOperate.ModuleName = EditorGUILayout.TextField(BaseData.CurrentOperate.ModuleName, (GUIStyle)"BoldTextField");
            GUILayout.Space(10);
            GUILayout.Label("C", GUILayout.Width(15));
            BaseData.CurrentOperate.Controll = EditorGUILayout.Toggle(BaseData.CurrentOperate.Controll, (GUIStyle)"OL Toggle", GUILayout.Width(15));
            GUILayout.Label("V", GUILayout.Width(15));
            BaseData.CurrentOperate.viewType = (ViewType)EditorGUILayout.EnumPopup("", BaseData.CurrentOperate.viewType, GUILayout.Width(80));
            GUILayout.Label("HotFix", GUILayout.Width(50));
            BaseData.CurrentOperate.isHotfix = EditorGUILayout.Toggle(BaseData.CurrentOperate.isHotfix, (GUIStyle)"OL Toggle", GUILayout.Width(15));
            GUILayout.EndHorizontal();
            GUILayout.Space(2);
            GUILayout.BeginHorizontal();
            GUILayout.Space(rect.width - 152 + 78);
            if (GUILayout.Button("Create", "PreButton", GUILayout.Width(70)))
            {
                OnCreateBtn(BaseData.CurrentOperate.isHotfix);
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            EditorGUILayout.EndVertical();
        }

        void OnCreateBtn(bool isHotfix)
        {
            bool hasSameName = false;
            MVCCodeCreator mVCCodeCreator = new MVCCodeCreator();
            foreach (var item in componentData.CurrentCreateInfo)
            {
                if (string.IsNullOrEmpty(item.Key.ModuleName))
                {
                    EditorWindow.GetWindow<CodeWindowMain>().ShowNotification(new GUIContent("模块名不能为空"));
                    continue;
                }

                Dictionary<UnityEngine.Object, string> valuePairs = new Dictionary<UnityEngine.Object, string>();
                List<string> prefabName = new List<string>();
                for (int i = 0; i < item.Value.Count; i++)
                {
                    if (item.Value[i] == null)
                        continue;
                    if (prefabName.Contains(item.Value[i].name))
                    {
                        prefabName.Clear();
                        hasSameName = true;
                        EditorWindow.GetWindow<CodeWindowMain>().ShowNotification(new GUIContent($"组件名称相同“{item.Value[i].name}”"));
                        break;
                    }
                    else
                    {
                        prefabName.Add(item.Value[i].name);
                    }
                    valuePairs.Add(item.Value[i], componentData.Tsf4String[item.Value[i]]);
                    ChildMark tempMakr = item.Value[i].GetComponent<ChildMark>();
                    if (tempMakr == null)
                    {
                        tempMakr = item.Value[i].gameObject.AddComponent<ChildMark>();
                    }
                    var serilaizationValue = new SerializedObject(tempMakr);
                    serilaizationValue.FindProperty("ChildType").stringValue = componentData.Tsf4String[item.Value[i]];
                    serilaizationValue.ApplyModifiedPropertiesWithoutUndo();
                    EditorUtility.SetDirty(item.Value[i]);
                }
                if (hasSameName)
                    break;
                if (valuePairs.Count > 0)
                {
                    Modify = false;
                    stageSave.RemoveEventHandler(null, prefabSaved);
                    componentData.GetReflection().CallSaveFunc();
                    mVCCodeCreator.CreateCode(item.Key, item.Key.MvcName, valuePairs, prefabPath, isHotfix);
                    item.Key.Created = true;
                }
            }
            componentData.ClearCache();
        }

        static void GetName(Transform value, System.Text.StringBuilder name)
        {
            if (value.parent != null)
            {
                GetName(value.parent, name);

                if (value.parent.parent != null)
                {
                    name.Append("/");
                }

                name.Append(value.name);
            }
            else
            {
                return;
            }
        }

        void InitRoot(Transform tsfParent)
        {
            if (string.IsNullOrEmpty(currentInspectorName))
            {
                currentInspectorName = defaultInspectorName;
            }

            var RelRoot = new MVCTreeItem()
            {
                ItemTsf = tsfParent,
                IsSelect = false,
                id = tsfParent.gameObject.GetInstanceID()
            };
            mVCTreeItems.Clear();
            mVCTreeItems.Add(RelRoot.id, RelRoot);
            firstId = RelRoot.id;
            var componentItem = tsfParent.GetComponent<ComponentItemKey>();
            if (componentItem)
            {
                componentData.Tsf4String.Clear();
                if (componentItem.selectedOfGameObject == null || componentItem.selectedOfGameObject.Count == 0 || defaultInspectorName.Equals(currentInspectorName))
                {
                    InitComponentData(componentItem.componentDatas);
                }
                else
                {
                    var index = componentItem.selectedOfGameObject.FindIndex(x => x.Contains(currentInspectorName));

                    var inspector = new List<global::ComponentData>();
                    if (index >= 0)
                    {
                        var selecteds = componentItem.selectedOfGameObject[index].Split('#');
                        for (int i = 1; i < selecteds.Length; i++)
                        {
                            inspector.Add(componentItem.componentDatas[int.Parse(selecteds[i])]);
                        }
                    }
                    InitComponentData(inspector);
                }
            }
            FindComponents(tsfParent, RelRoot);
            FindAllChild(RelRoot, 0);
        }

        void InitComponentData(List<global::ComponentData> componentDatas)
        {
            for (int i = 0; i < componentDatas.Count; i++)
            {
                var tValue = componentDatas[i];
                if (tValue.Value == null)
                {
                    continue;
                }
                if (componentData.CurrentCreateInfo.ContainsKey(BaseData.CurrentOperate))
                {
                    var tCompoent = tValue.Type.Equals("UnityEngine.Transform") ? tValue.Value as Transform : (tValue.Value as Component);
                    if (tCompoent != null)
                    {
                        if (!componentData.CurrentCreateInfo[BaseData.CurrentOperate].Contains(tCompoent.gameObject.transform))
                        {
                            componentData.CurrentCreateInfo[BaseData.CurrentOperate].Add(tCompoent.gameObject.transform);
                            if (!componentData.Tsf4String.ContainsKey(tCompoent.gameObject.transform))
                            {
                                componentData.Tsf4String.Add(tCompoent.gameObject.transform, tValue.Type);
                            }
                        }
                        else
                        {
                            componentData.Tsf4String[tCompoent.gameObject.transform] += "#" + tValue.Type;
                        }
                    }
                }
            }
        }

        void FindAllChild(MVCTreeItem parent, int depth)
        {
            parent.children = new List<TreeViewItem>();
            depth++;

            foreach (Transform item in parent.ItemTsf)
            {
                id++;
                MVCTreeItem childList = new MVCTreeItem();
                childList.ItemTsf = item;
                childList.id = item.gameObject.GetInstanceID();
                mVCTreeItems.Add(childList.id, childList);
                FindComponents(item, childList);
                parent.children.Add(childList);
                if (item.childCount != 0)
                {
                    FindAllChild(childList, childList.depth);
                }
            }
        }

        void FindComponents(Transform item, MVCTreeItem childList)
        {
            if (componentData.CurrentCreateInfo.ContainsKey(BaseData.CurrentOperate))
            {
                if (componentData.CurrentCreateInfo[BaseData.CurrentOperate].Contains(childList.ItemTsf))
                {
                    childList.CurrentType = componentData.Tsf4String[childList.ItemTsf];
                    childList.IsSelect = true;
                }
            }
        }

        #region tools
        public Dictionary<Transform, string> GetComponentKey()
        {
            var keys = new Dictionary<Transform, string>();

            foreach (var pair in componentData.Tsf4String)
            {
                keys.Add(pair.Key, pair.Value);
            }
            return keys;
        }

        public void SetInspect(string name, int instanceId)
        {
            currentInspectorName = name;
            componentData.Tsf4String.Clear();
            SetRoot();
            EditorApplication.RepaintHierarchyWindow();
        }
        public string GetInspectedInstancedId()
        {
            return currentInspectorName;
        }


        #endregion
    }

    public class ComponentData : IMvcData
    {
        public Dictionary<MVCInfo, List<Transform>> CurrentCreateInfo = new Dictionary<MVCInfo, List<Transform>>();
        public Dictionary<Transform, string> Tsf4String = new Dictionary<Transform, string>();
        BaseData baseData;

        public override void Init(BaseData baseData)
        {
            this.baseData = baseData;
        }

        public StageNavigationReflection GetReflection()
        {
            return baseData.GetStageReflection();
        }

        public void ClearCache()
        {
            this.baseData.ClearCache();
        }

        public override void Refresh()
        {
        }

        public Dictionary<string, MVCInfo> MainData()
        {
            return this.baseData.GetMainData();
        }
    }

    public class HierarchyWindowItemCustom
    {
        List<StandardizeLabel> standardizes { get; set; }
        Func<string, int> CalLabelTextLengthFunc;
        Font sample;
        int fontSize = 12;

        public bool Runnable
        {
            get
            {
                return standardizes != null && standardizes.Count > 0;
            }
        }

        HierarchyWindowItemCustom()
        {
            standardizes = new List<StandardizeLabel>();
        }

        public HierarchyWindowItemCustom(Func<string, int> calLabelTextLengthFunc) : base()
        {
            CalLabelTextLengthFunc = calLabelTextLengthFunc;
        }
        public HierarchyWindowItemCustom(Font font, int fontSize = 14) : base()
        {
            sample = font;
            this.fontSize = fontSize;
            CalLabelTextLengthFunc = CalLabelTextLength;
        }

        public void OnGUI(MVCTreeItem TreeItem, Rect rect)
        {
            int index = 1;
            if (standardizes != null && standardizes.Count > 0)
            {
                string value;
                var temp = new Dictionary<Type, Component>();
                for (int i = 0; i < standardizes.Count; i++)
                {
                    if (!temp.TryGetValue(standardizes[i].componentType, out Component cpt))
                    {
                        cpt = TreeItem.ItemTsf.GetComponent(standardizes[i].componentType);
                        temp.Add(standardizes[i].componentType, cpt);
                    }
                    if (!cpt)
                    {
                        continue;
                    }
                    if (standardizes[i].drawable(cpt))
                    {
                        GUI.color = standardizes[i].labelColor;
                        value = index++ + ":" + standardizes[i].label;
                        rect.x -= CalLabelTextLengthFunc(value) + 4;
                        EditorGUI.LabelField(rect, value);
                    }
                    GUI.color = Color.white;
                }
                return;
            }
        }

        public void ClearDrawStandardize()
        {
            if (standardizes != null)
            {
                standardizes.Clear();
            }
        }
        public void AddLabel<T>(string label, Color labelColor, Func<T, bool> drawable) where T : Component
        {
            if (standardizes == null)
            {
                standardizes = new List<StandardizeLabel>();
            }
            standardizes.Add(new StandardizeStruct<T>() { label = label, labelColor = labelColor, drawableBool = drawable });
        }

        public void AddLabel<T>(Color labelColor, Func<T, string> drawable) where T : Component
        {
            if (standardizes == null)
            {
                standardizes = new List<StandardizeLabel>();
            }
            standardizes.Add(new StandardizeStruct<T>() { labelColor = labelColor, drawableString = drawable });
        }

        private int CalLabelTextLength(string text)
        {
            if (sample == null)
            {
                return 0;
            }
            sample.RequestCharactersInTexture(text, fontSize, FontStyle.Normal);
            var width = 0;
            for (int j = 0; j < text.Length; j++)
            {
                sample.GetCharacterInfo(text[j], out CharacterInfo info);
                width += info.advance;
            }
            return width;
        }

        interface StandardizeLabel
        {
            string label { get; }
            bool drawable(Component cpt);
            Color labelColor { get; }
            Type componentType { get; }
        }

        struct StandardizeStruct<T> : StandardizeLabel where T : Component
        {
            public string label;
            public Func<T, bool> drawableBool;
            public Func<T, string> drawableString;
            public Color labelColor;
            public Type componentType => typeof(T);

            string StandardizeLabel.label => label;

            Color StandardizeLabel.labelColor => labelColor;

            bool StandardizeLabel.drawable(Component cpt)
            {
                if (drawableBool != null)
                {
                    return drawableBool((T)cpt);
                }
                else if (drawableString != null)
                {
                    return !string.IsNullOrEmpty(label = drawableString((T)cpt));
                }
                return false;
            }
        }
    }

}