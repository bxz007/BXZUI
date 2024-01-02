using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace Gala.FrameworkEditorTools
{
    public partial class PrefabFormatWindow
    {
        public static string AtlasPathBase = "Assets/LoadResources/UISpriteAtlas/Sprites/";
        static StringBuilder _sb1;
        static StringBuilder _sb2;
        static int _prefabFuncIndex;

        #region--------------------命名方面------------------
        static List<string> destribeList1 = new List<string>
    {
        "首字母大写|包含空格|首字母数字", //命名方面 
        "localPosition含小数|position z值为0|localScale不为1|localScale含小数",  // 小数方面
        "image为空|image包含默认backage,UISprite|image为空但Alpha不为0|引用其他图集|图片边缘空白一像素", //image 方面
        "字体为空|字体为默认文本|字体包含默认文本", //text方面
        $"按钮没选择image|按钮相应区域小于{ButtonLimitSize}", //button方面
        "使用了uimask，是否用RectMask2D", //mask方面
    };

        // 处理命名方面
        private static bool HandleName(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                string name = trans[i].name;
                if (item.itemList[0].isSelect)
                {
                    if (CompareChar(name[0]))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:命名首字母小写 name = " + name).AppendLine();
                    }
                }
                if (item.itemList[1].isSelect)
                {
                    if (name.Contains(" "))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:命名含空格 name = " + name).AppendLine();
                    }
                }
                if (item.itemList[2].isSelect)
                {
                    if (CompareCharNumber(name[0]))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:命名首字母含数字 name = " + name).AppendLine();
                    }
                }
            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;

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

        #endregion----------------命名方面------------------
        #region--------------------浮点数方面------------------

        // 处理浮点数方面
        // "localPosition含小数|position z值为0|localScale不为1|localScale含小数",  // 小数方面
        private static bool HandleFloat(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                RectTransform rectTrans = trans[i].GetComponent<RectTransform>();
                Vector3 localPos = rectTrans != null ? rectTrans.anchoredPosition3D : trans[i].localPosition;
                Vector3 localScale = trans[i].localScale;
                string name = trans[i].name;
                if (item.itemList[0].isSelect)
                {
                    if (IsNought(localPos))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:LocalPosition含有浮点数 Pos = (" + localPos.x + "," + localPos.y + "," + localPos.z + ")").AppendLine();
                    }
                }
                if (item.itemList[1].isSelect)
                {
                    if (localPos.z != 0)
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:Position z值为0 Pos.z = " + localPos.z).AppendLine();
                    }
                }
                if (item.itemList[2].isSelect)
                {
                    if (localScale.x != 1 || localScale.x != 1 || localScale.z != 1)
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:localScale 不全为1 Scale = (" + localScale.x + "," + localScale.y + "," + localScale.z + ")").AppendLine();
                    }
                }
                if (item.itemList[3].isSelect)
                {
                    if (IsNought(localScale))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点:localScale 含浮点数 Scale = (" + localScale.x + "," + localScale.y + "," + localScale.z + ")").AppendLine();
                    }
                }

            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;
        }

        private static bool IsNought(Vector3 data)
        {
            // Debug.LogError(data.ToString());
            return IsNought(data.x) || IsNought(data.y) || IsNought(data.z);
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
        #endregion----------------浮点数方面------------------
        #region--------------------image方面------------------

        // 处理image方面
        //"image为空|image包含默认backage,UISprite|image为空但Alpha不为0|引用其他图集|图片边缘空白一像素", //image 方面
        private static bool HandleImage(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                Image image = trans[i].GetComponent<Image>();
                string name = trans[i].name;
                if (image != null)
                {
                    if (item.itemList[0].isSelect)
                    {
                        if (image.sprite == null)
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点 节点Image.Sprite为空").AppendLine();
                        }
                    }
                    if (item.itemList[1].isSelect && image.sprite != null)
                    {
                        if (image.sprite.name == "Background" || image.sprite.name == "UISprite" || image.sprite.name == "UIMask")
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + " 节点Image.Sprite.Name为默认图片名= " + image.sprite.name).AppendLine();
                        }
                    }
                    if (item.itemList[2].isSelect && image.sprite == null)
                    {
                        Color imageColor = image.color;
                        Vector2 sizeDelta = trans[i].GetComponent<RectTransform>().sizeDelta;
                        if (imageColor.a != 0 && (sizeDelta.x > 0.1f || sizeDelta.y > 0.1f))
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点Image sprite为空但alpha不为0并且size不为0 Color = (" + imageColor.r + "," + imageColor.g + "," + imageColor.b + "," + imageColor.a + ");size = " + "(" + sizeDelta.x + "," + sizeDelta.y + ")").AppendLine();
                        }
                    }
                    if (item.itemList[3].isSelect && image.sprite != null)
                    {
                        string path = AssetDatabase.GetAssetPath(image.sprite);
                        int uiIndex = path.IndexOf(AtlasPathBase);
                        if (uiIndex != -1)
                        {
                            string path1 = path.Replace(AtlasPathBase, "");
                            uiIndex = path1.IndexOf("/");
                            if (uiIndex != -1)
                            {
                                path1 = path1.Substring(0, uiIndex);
                                path1 = path1.Replace("Atlas", "");
                                if (path1 != "Common" && path1 != prefabName)
                                {

                                    if (prefabName.Contains(path1))
                                    {
                                        // GUI.color = Color.yellow;
                                        // rect1.x -= 180;
                                        // EditorGUI.LabelField(rect1, index++ + ":图片引用了" + path + "图集资源");
                                    }
                                    else if (prefabName.Contains(path1))
                                    {

                                    }
                                    else if (prefabName.ToUpper() == path1.ToUpper())
                                    {

                                    }
                                    else
                                    {
                                        index++;
                                        hasData = true;
                                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点Image图片引用了" + path1 + "图集资源").AppendLine();
                                    }
                                }
                            }
                        }
                    }
                }

            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;
        }
        #endregion----------------image方面------------------
        #region--------------------text方面------------------

        // 处理text方面
        //"字体为空|字体为默认文本|字体包含默认文本", //text方面
        private static bool HandleText(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                Text text = trans[i].GetComponent<Text>();
                string name = trans[i].name;
                if (text != null)
                {
                    Font font = text.font;
                    if (item.itemList[0].isSelect)
                    {
                        if (font == null)
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点: Text.Font 为空").AppendLine();
                        }
                    }
                    if (item.itemList[1].isSelect && font != null)
                    {
                        if (font.name == "Arial")
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点: Text.Font使用了默认字体 Arial").AppendLine();
                        }
                    }
                    if (item.itemList[2].isSelect)
                    {
                        if (!string.IsNullOrEmpty(text.text))
                        {
                            Component language = trans[i].GetComponent("LanguageComponent");
                            if (language == null)
                            {
                                index++;
                                hasData = true;
                                sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点: Text 含有默认文本 text = " + text.text).AppendLine();
                            }
                        }

                    }
                }


            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;
        }
        #endregion----------------text方面------------------
        #region--------------------button方面------------------

        // 处理button方面
        private static bool HandleButton(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                Button button = trans[i].GetComponent<Button>();
                string name = trans[i].name;
                if (button != null)
                {
                    Image image = button.image;
                    if (item.itemList[0].isSelect)
                    {
                        if (image == null)
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点: Button没选择Image").AppendLine();
                        }
                    }
                    if (item.itemList[1].isSelect && image != null)
                    {
                        RectTransform tran = image.transform.GetComponent<RectTransform>();
                        //    Debug.LogError(trans.rect.width);
                        if (tran.rect.width < ButtonLimitSize || tran.rect.height < ButtonLimitSize)
                        {
                            index++;
                            hasData = true;
                            sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点: Button按钮响应区域太小，为(" + tran.rect.height + "," + tran.rect.width + ")").AppendLine();
                        }

                    }
                }

            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;
        }
        #endregion----------------button方面------------------
        #region--------------------mask方面------------------
        // 处理mask方面
        private static bool HandleMask(Transform[] trans, CheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            string prefabName = trans[0].name;
            int index = 0;
            bool hasData = false;
            for (int i = 1, num = trans.Length; i < num; i++)
            {
                Mask mask = trans[i].GetComponent<Mask>();
                string name = trans[i].name;
                if (item.itemList[0].isSelect)
                {
                    if (mask != null)
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + prefabName + "预制体 " + name + "节点 使用了Mask,看是否需要换成RectMask2D").AppendLine();
                    }
                }

            }
            if (hasData)
            {
                Append2(_prefabFuncIndex + "." + trans[0].name + "");
                Append2(sb.ToString());
            }
            return hasData;
        }
        #endregion----------------mask方面------------------
    }
}