using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;


namespace Gala.FrameworkEditorTools
{
    public class ChangeFontWindow : EditorWindow
    {
        public static void Open()
        {
            /*第一个参数窗口类型，决定窗口操作逻辑
             * 第二个参数确定是否为浮动窗口，选择false可有停靠效果
             * 第三个参数 显示窗口的标题
             * 第四个 目前不知道什么意思
             */
            EditorWindow.GetWindow(typeof(ChangeFontWindow), true);
        }
        Font change;
        static Font changeFont;
        //public Font toChange = new Font("Arial");
        public Font toChange;
        static Font toChangeFont;

        void OnGUI()
        {
            change = (Font)EditorGUILayout.ObjectField("目标字体", change, typeof(Font), true, GUILayout.MinWidth(100f));
            changeFont = change;
            toChange = (Font)EditorGUILayout.ObjectField("选择需要更换的字体", toChange, typeof(Font), true, GUILayout.MinWidth(100));
            toChangeFont = toChange;
            if (GUILayout.Button("确认更换"))
            {
                Change();
            }
        }

        public static void Change()
        {
            Object[] Texts = Selection.GetFiltered(typeof(Text), SelectionMode.Deep);
            foreach (Object text in Texts)
            {
                if (text)
                {
                    //如果是UGUI讲UILabel换成Text就可以  
                    Text TempText = (Text)text;
                    Undo.RecordObject(TempText, TempText.gameObject.name);
                    if (TempText.font == changeFont || TempText.font == null)
                    {
                        TempText.font = toChangeFont;
                        Debug.Log(text.name + ":" + TempText.text);
                        EditorUtility.SetDirty(TempText);
                    }

                }
            }
        }
    }
}