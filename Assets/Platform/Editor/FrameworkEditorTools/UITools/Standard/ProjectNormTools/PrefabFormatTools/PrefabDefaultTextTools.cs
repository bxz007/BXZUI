using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Newtonsoft.Json;

namespace Gala.FrameworkEditorTools
{

    public partial class PrefabDefaultTextTools : EditorWindow
    {
        const string DefaultTextPath = "DefaultText/DefaultText.txt";
        const string UIPrefabPath = "/LoadResources/UI";

        static string defaultTextPath;

        static List<string> prefabPathList = new List<string>();

        static void SaveNormalTextTools()
        {
            InitPath();
            InitPrefabList();
            HandleWriteJson();
        }

        static void InitPath()
        {
            defaultTextPath = Application.dataPath.Replace("Assets", DefaultTextPath);
        }

        static void InitPrefabList()
        {
            string uipath = Application.dataPath + UIPrefabPath;
            prefabPathList = PrefabFormatWindow.GetAllFiles(uipath);
        }

        static void HandleWriteJson()
        {
            Dictionary<string, Dictionary<string, string>> resultDict = new Dictionary<string, Dictionary<string, string>>();
            for (int i = 0, num = prefabPathList.Count; i < num; i++)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPathList[i]);
                Dictionary<string, string> prefabDict = new Dictionary<string, string>();
                SetTextKey(root.transform, root.transform.name, ref prefabDict);
                resultDict[root.transform.name] = prefabDict;
            }
            string text = JsonConvert.SerializeObject(resultDict);

            if (File.Exists(defaultTextPath))
            {
                File.Delete(defaultTextPath);
            }
            // 拆成带个json也可以
            File.WriteAllText(defaultTextPath, text);
            if (EditorUtility.DisplayDialog("导出完成", String.Format("导出完成,是否打开{0}", defaultTextPath), "打开", "关闭"))
            {
                UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(defaultTextPath, 1);
            }
        }

        static void SetTextKey(Transform root, string rootPath, ref Dictionary<string, string> defaultDict)
        {
            string result = rootPath;
            if (root.childCount != 0)
            {
                result += "|";
                for (int i = 0; i < root.childCount; i++)
                {
                    Text text = root.GetChild(i).GetComponent<Text>();
                    string resultEntity = result + root.GetChild(i).name + "/" + i;
                    /* if (text != null && !string.IsNullOrEmpty(text.text) && text.GetComponent<LanguageComponent>() == null)
                    {
                        defaultDict.Add(resultEntity, text.text);
                        if (root.childCount != 0)
                        {
                            SetTextKey(text.transform, resultEntity, ref defaultDict);
                        }
                    }
                    else
                    {
                        if (root.childCount != 0)
                        {
                            SetTextKey(root.GetChild(i), resultEntity, ref defaultDict);
                        }
                    } */
                }
            }
        }
    }

}

