using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;
using UnityEngine;
using Newtonsoft.Json;
using System.IO;
// image 添加 Cull Transparent Mesh

namespace Gala.FrameworkEditorTools
{

    [CustomEditor(typeof(RectTransform))]
    public class RectTransformEditorWindow : Editor
    {
        private Editor m_Target;
        private RectTransform targetTransform;
        private Transform realRoot;
        private bool floatIncludeChildren = false;
        private bool textIncludeChildren = true;
        private bool beautifyPrefabRoot = false;
        const string DefaultTextPath = "DefaultText/DefaultText.txt";
        void Awake()
        {
            m_Target = Editor.CreateEditor(target,
                Assembly.GetAssembly(typeof(Editor)).GetType("UnityEditor.RectTransformEditor", true));
            targetTransform = target as RectTransform;
            string rootName = string.Empty;
            beautifyPrefabRoot = false;
            if (targetTransform.root != null)
            {
                if (!targetTransform.root.name.Contains("Canvas ("))
                {
                    if (targetTransform.root == targetTransform)
                    {
                        beautifyPrefabRoot = true;
                        realRoot = targetTransform.transform;
                    }
                }
                else
                {
                    if (targetTransform.root.childCount != 0)
                    {
                        if (targetTransform.root.GetChild(0) == targetTransform)
                        {
                            beautifyPrefabRoot = true;
                            realRoot = targetTransform.root.GetChild(0);
                        }
                    }
                }
            }
        }

        public override void OnInspectorGUI()
        {
            m_Target.OnInspectorGUI();
            // UnityEngine.Debug.LogError(targetTransform.root);
            GUI.color = Color.green;

            if (beautifyPrefabRoot)
            {
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("规范美化预制体", GUILayout.Height(24f)))
                {
                    ResetTransformFloat(true);
                    ResetTransformText(true);
                    ResetTransformName();
                    HandleImageShake();
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("导入默认文本", GUILayout.Height(24f)))
                {
                    ReimportDefaultText(realRoot);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                textIncludeChildren = EditorGUILayout.Toggle(textIncludeChildren, GUILayout.Width(15));
                GUILayout.Label("保存默认值配置", GUILayout.Width(80));
                if (GUILayout.Button("清理文本默认值", GUILayout.Height(24f)))
                {
                    ResetTransformText(textIncludeChildren);
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.BeginHorizontal();
                floatIncludeChildren = EditorGUILayout.Toggle(floatIncludeChildren, GUILayout.Width(15));
                GUILayout.Label("包含子节点", GUILayout.Width(80));
                if (GUILayout.Button("清理浮点数位(4舍5入)", GUILayout.Height(24f)))
                {
                    ResetTransformFloat(floatIncludeChildren);
                }
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("清理资源(spr=null&alpha!=0&size!=0)", GUILayout.Height(24f)))
                {
                    HandleImageShake();
                }
                EditorGUILayout.EndHorizontal();

                if (GUILayout.Button("勾选CullTransparentMesh", GUILayout.Height(24f)))
                {
                    CullTransparentMesh();
                }

                if (GUILayout.Button("FindAlpha 1", GUILayout.Height(24f)))
                {
                    FindAlpha0();
                }
            }



        }

        public void FindAlpha0()
        {
            bool hasChange = false;
            var rectTrans = targetTransform.GetComponentsInChildren<Image>(true);
            foreach (var rectTran in rectTrans)
            {
                if (rectTran.color.a <= 3 / 255f)
                {
                    if (rectTran.color.a > 0.0001f)
                    {
                        Debug.LogError(rectTran.name + " alpha: " + rectTran.color.a);
                        rectTran.color = new Color(rectTran.color.r, rectTran.color.g, rectTran.color.b, 0);
                        hasChange = true;
                    }
                }
            }
            if (hasChange)
                EditorUtility.SetDirty(this.target);
        }

        public void CullTransparentMesh()
        {
            bool hasChange = false;
            var rectTrans = targetTransform.GetComponentsInChildren<CanvasRenderer>(true);
            foreach (var rectTran in rectTrans)
            {
                rectTran.cullTransparentMesh = true;
                hasChange = true;
            }
            if (hasChange)
                EditorUtility.SetDirty(this.target);
        }

        // 重置预制体浮点数
        public void ResetTransformFloat(bool includeChildren)
        {
            bool hasChange = false;
            if (includeChildren)
            {
                var rectTrans = targetTransform.GetComponentsInChildren<RectTransform>(true);
                foreach (var rectTran in rectTrans)
                {
                    var pos = rectTran.anchoredPosition;
                    rectTran.anchoredPosition = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y));
                    var sizeDelta = rectTran.sizeDelta;
                    rectTran.sizeDelta = new Vector2(Mathf.Round(sizeDelta.x), Mathf.Round(sizeDelta.y));
                    hasChange = true;
                }
            }
            else
            {
                var pos = targetTransform.anchoredPosition;
                targetTransform.anchoredPosition = new Vector3(Mathf.Round(pos.x), Mathf.Round(pos.y));
                var sizeDelta = targetTransform.sizeDelta;
                targetTransform.sizeDelta = new Vector2(Mathf.Round(sizeDelta.x), Mathf.Round(sizeDelta.y));
                hasChange = true;
            }
            if (hasChange)
                EditorUtility.SetDirty(this.target);
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
                    if (text != null && !string.IsNullOrEmpty(text.text) && text.GetComponent("LanguageComponent") == null)
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
                    }
                }
            }
        }

        // 重置预制体默认文本
        public void ResetTransformText(bool saveFile)
        {
            bool hasChange = false;

            if (saveFile)
            {
                string defaultTextPath = Application.dataPath.Replace("Assets", DefaultTextPath);
                if (File.Exists(defaultTextPath))
                {
                    if (EditorUtility.DisplayDialog("保存默认值配置？", String.Format("是否需要保存当前默认值配置？"), "保存", "关闭"))
                    {
                        string jsonText = File.ReadAllText(defaultTextPath);
                        Dictionary<string, Dictionary<string, string>> textDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonText);
                        Dictionary<string, string> pathDict = new Dictionary<string, string>();
                        SetTextKey(realRoot, realRoot.name, ref pathDict);
                        textDict[realRoot.name] = pathDict;
                        string text = JsonConvert.SerializeObject(textDict);
                        File.Delete(defaultTextPath);
                        // 拆成带个json也可以
                        File.WriteAllText(defaultTextPath, text);
                        if (EditorUtility.DisplayDialog("导出完成", String.Format("导出完成,是否打开{0}", defaultTextPath), "打开", "关闭"))
                        {
                            UnityEditorInternal.InternalEditorUtility.OpenFileAtLineExternal(defaultTextPath, 1);
                        }
                    }
                }
            }

            var texts = targetTransform.GetComponentsInChildren<Text>(true);
            foreach (var text in texts)
            {
                Component languageComp = text.transform.GetComponent("LanguageComponent");
                if (languageComp == null)
                {
                    text.text = string.Empty;
                    hasChange = true;
                }
            }

            if (hasChange)
                EditorUtility.SetDirty(this.target);
        }

        // 文本重命名
        public void ResetTransformName()
        {
            bool hasChange = false;
            var transforms = targetTransform.GetComponentsInChildren<Transform>(true);
            foreach (var trans in transforms)
            {
                string name = trans.name;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Contains(" "))
                    {
                        hasChange = true;
                        name = name.Replace(" ", "");
                        trans.name = name;
                    }
                    if (CompareChar(name[0]))
                    {
                        name = name.Substring(0, 1).ToUpper() + name.Substring(1);
                        Debug.LogError(name);

                        trans.name = name;
                    }

                }

            }
            if (hasChange)
                EditorUtility.SetDirty(this.target);
        }

        // 导入默认文本
        private void ReimportDefaultText(Transform root)
        {
            string defaultTextPath = Application.dataPath.Replace("Assets", DefaultTextPath);
            if (File.Exists(defaultTextPath))
            {
                string jsonText = File.ReadAllText(defaultTextPath);
                Dictionary<string, Dictionary<string, string>> textDict = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonText);
                if (textDict.TryGetValue(root.name, out Dictionary<string, string> pathDict))
                {
                    foreach (KeyValuePair<string, string> pair in pathDict)
                    {
                        string[] keyArray = pair.Key.Split("|".ToCharArray());
                        if (keyArray[0] != root.name)
                        {
                            UnityEngine.Debug.LogError("root 节点不正确 name = " + root.name);
                            return;
                        }
                        Transform parent = root;
                        for (int i = 1, num = keyArray.Length; i < num; i++)
                        {
                            string[] realKey = keyArray[i].Split("/".ToCharArray());
                            int index = int.Parse(realKey[1]);
                            if (parent.childCount != 0)
                            {
                                Transform transItem = parent.GetChild(index);
                                if (i == num - 1 && transItem != null && transItem.GetComponent<Text>() != null && string.IsNullOrEmpty(transItem.GetComponent<Text>().text) && transItem.GetComponent("LanguageComponent") == null)
                                {
                                    transItem.GetComponent<Text>().text = pair.Value;
                                }
                                else if (transItem != null && transItem.childCount != 0)
                                {
                                    parent = transItem;
                                }
                            }
                        }
                    }
                }
            }
        }

        // 清理img闪屏资源
        public void HandleImageShake()
        {
            bool hasChange = false;
            var imagesTrans = targetTransform.GetComponentsInChildren<UnityEngine.UI.Image>(true);
            foreach (var image in imagesTrans)
            {
                RectTransform rect = image.GetComponent<RectTransform>();
                if (image.sprite == null && image.color.a != 0 && (rect.sizeDelta.x > 0.1f || rect.sizeDelta.y > 0.1f))
                {
                    //      rect.sizeDelta = Vector2.zero;
                    image.color = new Color(image.color.r, image.color.g, image.color.b, 0);
                    CanvasRenderer ren = rect.GetComponent<CanvasRenderer>();
                    if (ren != null)
                    {
                        ren.cullTransparentMesh = true;
                    }
                    hasChange = true;
                }
            }
            if (hasChange)
                EditorUtility.SetDirty(this.target);
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
    }

}