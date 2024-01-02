using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GalaFramework
{
    public class CodeWindowMain : EditorWindow
    {
        static List<IWindows> windows = new List<IWindows>();
        static BaseData mvcData;

        [MenuItem("Assets/代码编辑器")]
        public static void Open()
        {
            MVCCache.Clear();
            var editorAsm = typeof(Editor).Assembly;
            var inspectorWindow = editorAsm.GetType("UnityEditor.InspectorWindow");
            var mVCWindows = EditorWindow.GetWindow<CodeWindowMain>(inspectorWindow);
            mVCWindows.titleContent = new GUIContent("代码编辑器");
        }

        void Init()
        {
            if (mvcData == null)
            {
                mvcData = new BaseData();
            }
            CloseAutoSave();

            this.InitWindowsList();

            for (int i = 0; i < windows.Count; i++)
            {
                var currentChild = windows[i];
                currentChild.Init(this.Repaint);
            }
            this.minSize = new Vector2(300, 200);
            this.maxSize = new Vector2(300,1400);
        }

        private void OnEnable()
        {
            this.Init();
        }

        private void OnDisable()
        {
            windows.ForEach(value => value.OnClose());
        }

        /// <summary>
        /// 获得所有mvc可配置窗口
        /// </summary>
        void InitWindowsList()
        {
            if (windows != null && windows.Count != 0)
                return;

            windows = mvcData.GetWindows();
            for (int i = 0; i < windows.Count; i++)
            {
                mvcData.Inject(windows[i].GetType(), windows[i]);
            }
            windows.Sort((x, y) => x.Orde.CompareTo(y.Orde));
        }

        /// <summary>
        /// 关闭Unity 2018以上预制体界面的autoSave，防止更改被覆盖
        /// </summary>
        void CloseAutoSave()
        {
            var assemblyK = typeof(Editor).Assembly;
            Type StageNavionManagertype = typeof(Editor).Assembly.GetType("UnityEditor.SceneManagement.StageNavigationManager");
            PropertyInfo autoSaveValue = null;
            foreach (var item in StageNavionManagertype.GetRuntimeProperties())
            {
                if (item.Name.Equals("autoSave"))
                {
                    autoSaveValue = item;
                }
            }

            Type scriptableSingleton = typeof(ScriptableSingleton<>).MakeGenericType(StageNavionManagertype);
            PropertyInfo scriptableSingletonInstance = scriptableSingleton.GetProperty("instance");
            object StageNavionManagerSingleton = scriptableSingletonInstance.GetValue(null, null);
            autoSaveValue.SetValue(StageNavionManagerSingleton, false);
            mvcData.SetEditorReflectionData(StageNavionManagerSingleton);
        }

        private void OnGUI()
        {
            for (int i = 0; i < windows.Count; i++)
            {
                var currentChild = windows[i];
                currentChild.OnGUI(position);
            }
            if (mvcData != null)
            {
                if (mvcData.Update())
                {
                    this.Repaint();
                }
            }
        }

        #region public api
        private static ComponentWindow GetComponentWindow()
        {
            ComponentWindow componentWindow = null;
            foreach (var window in windows)
            {
                if (window is ComponentWindow cw)
                {
                    componentWindow = cw;
                    break;
                }
            }
            return componentWindow;
        }

        public static Dictionary<Transform, string> GetComponentKey()
        {
            return GetComponentWindow().GetComponentKey();
        }

        public static void SetInspect(string name, int instanceId)
        {
            GetComponentWindow().SetInspect(name, instanceId);
        }
        public static string GetInspectedInstancedId()
        {
            return GetComponentWindow().GetInspectedInstancedId();
        }
        public static void SetComponentWindowStatus(bool status)
        {
            GetComponentWindow().Modify = status;
        }

        #endregion

    }
}
