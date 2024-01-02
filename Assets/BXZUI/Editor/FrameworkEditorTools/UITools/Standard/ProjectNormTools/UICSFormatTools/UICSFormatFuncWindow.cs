using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;


namespace Gala.FrameworkEditorTools
{
    public partial class UICSFormatWindow
    {
        public static string AtlasPathBase = "Assets/Scripts/Hotfix/Module";
        private static List<string> dispatchNameList = new List<string>{
         "Platform.EventDispatcher.AddEventListener",
         "Platform.EventDispatcher.RemoveEvent",
         "EventDispatcher.AddEventListener",
         "EventDispatcher.RemoveEvent",
         "Platform.EventDispatcher",
         "Platform.EventDispatcher.TriggerEvent",
         "PlatformHotfix.EventDispatcher.TriggerEvent",
    };

        private const string SendMessageSign = "SendNetworkMessage";
        private const string ResourcesLoad = "ResourceMgr.Instance.Load";
        private const string ResourcesUnLoad = "ResourceMgr.Instance.Unload";

        static StringBuilder _sb1;
        static int _csFuncIndex;
        static bool _inAnnotate = false;
        static bool _eventMustHandle = false;
        static Dictionary<string, List<string>> mainEventDict = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> hotfixEventDict = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> mainTrgDict = new Dictionary<string, List<string>>();
        static Dictionary<string, List<string>> hotTrgEventDict = new Dictionary<string, List<string>>();

        #region-------------------事件方面------------------
        // 处理命名方面
        private static bool HandleEventDispatch(string[] lines, string csName, CSCheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool hasData = false;
            _inAnnotate = false;
            Dictionary<string, int> mainDispitchDict = new Dictionary<string, int>();
            Dictionary<string, int> hotfixDispitchDict = new Dictionary<string, int>();

            for (int i = 0, num = lines.Length; i < num; i++)
            {
                string context = lines[i];
                if (!NeedHandle(ref context)) continue;
                if (item.itemList[0].isSelect)
                {
                    _eventMustHandle = true;
                    // 引用计数可能存在错误，比如先移除后加入，计数为0，最好的方法是再Ondestroy方法中做
                    string mainAdd = GetEventName(context, true, true);
                    if (!string.IsNullOrEmpty(mainAdd))
                    {
                        if (mainDispitchDict.TryGetValue(mainAdd, out int mainAddCount))
                        {
                            mainDispitchDict[mainAdd]++;
                        }
                        else
                        {
                            mainDispitchDict[mainAdd] = 1;
                        }
                        if (mainEventDict.TryGetValue(mainAdd, out List<string> mainEventList))
                        {
                            mainEventList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> eventList = new List<string>();
                            eventList.Add(csName + ":(" + i + "):" + context);
                            mainEventDict.Add(mainAdd, eventList);
                        }
                    }
                    string mainCnt = GetEventName(context, true, false);
                    if (!string.IsNullOrEmpty(mainCnt))
                    {
                        if (mainDispitchDict.TryGetValue(mainCnt, out int mainCntCount))
                        {
                            mainDispitchDict[mainCnt]--;
                        }
                        else
                        {
                            mainDispitchDict[mainAdd] = -1;
                        }
                        if (mainEventDict.TryGetValue(mainAdd, out List<string> mainEventList))
                        {
                            mainEventList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> eventList = new List<string>();
                            eventList.Add(csName + ":(" + i + "):" + context);
                            mainEventDict.Add(mainAdd, eventList);
                        }
                    }
                    string hotAdd = GetEventName(context, false, true);
                    if (!string.IsNullOrEmpty(hotAdd))
                    {
                        if (hotfixDispitchDict.TryGetValue(hotAdd, out int hotAddAddCount))
                        {
                            hotfixDispitchDict[hotAdd]++;
                        }
                        else
                        {
                            hotfixDispitchDict[hotAdd] = 1;
                        }
                        if (hotfixEventDict.TryGetValue(hotAdd, out List<string> hotEventList))
                        {
                            hotEventList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> eventList = new List<string>();
                            eventList.Add(csName + ":(" + i + "):" + context);
                            hotfixEventDict.Add(hotAdd, eventList);
                        }
                    }
                    string hotCnt = GetEventName(context, false, false);
                    if (!string.IsNullOrEmpty(hotCnt))
                    {
                        if (hotfixDispitchDict.TryGetValue(hotCnt, out int hotCntCount))
                        {
                            hotfixDispitchDict[hotCnt]--;
                        }
                        else
                        {
                            hotfixDispitchDict[hotCnt] = -1;
                        }
                        if (hotfixEventDict.TryGetValue(hotAdd, out List<string> hotEventList))
                        {
                            hotEventList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> eventList = new List<string>();
                            eventList.Add(csName + ":(" + i + "):" + context);
                            hotfixEventDict.Add(hotAdd, eventList);
                        }
                    }
                    string mainTri = GetTriEventName(context, true);
                    if (!string.IsNullOrEmpty(mainTri))
                    {
                        if (mainTrgDict.TryGetValue(mainTri, out List<string> mainTrgList))
                        {
                            mainTrgList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> TrgList = new List<string>();
                            TrgList.Add(csName + ":(" + i + "):" + context);
                            mainTrgDict.Add(mainTri, TrgList);
                        }
                    }
                    string hotTri = GetTriEventName(context, false);
                    if (!string.IsNullOrEmpty(hotTri))
                    {
                        if (hotTrgEventDict.TryGetValue(hotTri, out List<string> hotTrgList))
                        {
                            hotTrgList.Add(csName + ":(" + i + "):" + context);
                        }
                        else
                        {
                            List<string> TrgList = new List<string>();
                            TrgList.Add(csName + ":(" + i + "):" + context);
                            hotTrgEventDict.Add(hotTri, TrgList);
                        }
                    }
                }

            }

            foreach (KeyValuePair<string, int> pair in mainDispitchDict)
            {
                if (pair.Value > 0)
                {
                    index++;
                    hasData = true;
                    sb.Append("(" + index + ")." + csName + ":" + "主工程事件没移除，事件名 = " + pair.Key).AppendLine();
                }
            }
            foreach (KeyValuePair<string, int> pair in hotfixDispitchDict)
            {
                if (pair.Value > 0)
                {
                    index++;
                    hasData = true;
                    sb.Append("(" + index + ")." + csName + ":" + "热更工程事件没移除，事件名 = " + pair.Key).AppendLine();
                }
            }
            if (hasData)
            {
                Append1(_csFuncIndex + "." + csName + "");
                Append1(sb.ToString());
            }
            return hasData;

        }

        private static void HandleTriEventDispatch()
        {
            if (!_eventMustHandle) return;
            StringBuilder sb = new StringBuilder();
            foreach (var pair in mainTrgDict)
            {
                if (!mainEventDict.TryGetValue(pair.Key, out List<string> valueList))
                {
                    for (int i = 0, num = pair.Value.Count; i < num; i++)
                    {
                        sb.Append("(" + i + ")." + pair.Value[i] + ":" + "主工程没注册事件:" + pair.Key).AppendLine();
                    }
                }
            }
            foreach (var pair in hotTrgEventDict)
            {
                if (!hotfixEventDict.TryGetValue(pair.Key, out List<string> valueList))
                {
                    for (int i = 0, num = pair.Value.Count; i < num; i++)
                    {
                        //     Debug.LogError("(" + i + ")." + pair.Value[i] + ":" + "热更工程没注册事件:" + pair.Key);
                        sb.Append("(" + i + ")." + pair.Value[i] + ":" + "热更工程没注册事件:" + pair.Key).AppendLine();
                    }
                }
            }
            Append1(sb.ToString());
        }

        private static string GetTriEventName(string context, bool isMain)
        {
            int index = isMain ? 5 : 6;
            int mainIndex = context.IndexOf(dispatchNameList[index]);
            if (mainIndex != -1)
            {
                string text = context.Substring(mainIndex);
                mainIndex = text.IndexOf("\"");
                if (mainIndex != -1)
                {
                    text = text.Substring(mainIndex + 1);
                    mainIndex = text.IndexOf("\"");
                    text = text.Substring(0, mainIndex);

                }
                else
                {
                    mainIndex = text.IndexOf("(");
                    text = text.Substring(mainIndex + 1);
                    mainIndex = text.IndexOf(",");
                    if (mainIndex == -1)
                    {
                        mainIndex = text.IndexOf(")");
                    }
                    text = text.Substring(0, mainIndex).Replace(" ", "");
                }
                return text;
            }
            return string.Empty;
        }

        private static string GetEventName(string context, bool isMain, bool isAdd)
        {
            int index = isMain ? 1 : 3;
            index = isAdd ? index - 1 : index;
            _inAnnotate = false;
            int mainIndex = context.IndexOf(dispatchNameList[index]);
            if (mainIndex != -1)
            {
                if (!isMain && context.IndexOf(dispatchNameList[4]) != -1)
                {
                    return string.Empty;
                }
                string text = context.Substring(mainIndex);
                mainIndex = text.IndexOf("\"");
                if (mainIndex != -1)
                {
                    text = text.Substring(mainIndex + 1);
                    mainIndex = text.IndexOf("\"");
                    text = text.Substring(0, mainIndex);

                }
                else
                {
                    mainIndex = text.IndexOf("(");
                    text = text.Substring(mainIndex + 1);
                    mainIndex = text.IndexOf(",");
                    if (mainIndex == -1)
                    {
                        mainIndex = text.IndexOf(")");
                    }
                    text = text.Substring(0, mainIndex).Replace(" ", "");
                }
                return text;
            }
            return string.Empty;
        }

        static bool NeedHandle(ref string context)
        {
            if (context.IndexOf("/*") != -1)
            {
                _inAnnotate = true;
            }
            if (_inAnnotate && context.IndexOf("*/") != -1)
            {
                _inAnnotate = false;
            }
            if (_inAnnotate || context.Replace(" ", "").StartsWith("//") || context.Contains("Debug.Log"))
            {
                return false;
            }
            int index = context.IndexOf("//");
            if (index != -1)
            {
                context = context.Substring(0, index);
            }
            index = context.IndexOf(".Append");
            if (index != -1)
            {
                context = context.Substring(0, index);
            }

            index = context.IndexOf("#region");
            if (index != -1)
            {
                context = context.Substring(0, index);
            }
            index = context.IndexOf("#endregion");
            if (index != -1)
            {
                context = context.Substring(0, index);
            }
            return true;
        }

        private static bool IsView(string csName)
        {
            if (csName.Contains("View"))
                return true;
            return false;
        }

        #endregion----------------事件方面------------------   


        #region-------------------view方面------------------
        // 处理命名方面
        private static bool HandleView(string[] lines, string csName, CSCheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool hasData = false;
            _inAnnotate = false;

            for (int i = 0, num = lines.Length; i < num; i++)
            {
                string context = lines[i];
                if (!NeedHandle(ref context)) continue;
                if (item.itemList[0].isSelect)
                {
                    if (IsView(csName) && context.Contains(SendMessageSign))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + csName + ":(" + (i + 1) + ")" + "发送协议应移到Controller中").AppendLine();
                    }
                }
            }

            if (hasData)
            {
                Append1(_csFuncIndex + "." + csName + "");
                Append1(sb.ToString());
            }
            return hasData;

        }
        #endregion----------------view 方面------------------   

        #region-------------------语言方面------------------
        // 处理命名方面
        private static bool HandleLanguage(string[] lines, string csName, CSCheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool hasData = false;

            for (int i = 0, num = lines.Length; i < num; i++)
            {
                string context = lines[i];
                if (!NeedHandle(ref context)) continue;
                if (item.itemList[0].isSelect)
                {
                    if (HasChinese(context) && !context.Contains(".LogError"))
                    {
                        index++;
                        hasData = true;
                        sb.Append("(" + index + ")." + csName + ":(" + (i + 1) + ")" + "含有中文语言，需要移到语言包,代码为" + context).AppendLine();
                    }
                }
            }

            if (hasData)
            {
                Append1(_csFuncIndex + "." + csName + "");
                Append1(sb.ToString());
            }
            return hasData;

        }

        public static bool HasChinese(string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }
        #endregion----------------语言方面------------------   


        #region-------------------内存方面------------------
        // 处理命名方面
        private static bool HandleResources(string[] lines, string csName, CSCheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool hasData = false;
            Dictionary<string, int> loadDict = new Dictionary<string, int>();
            Dictionary<string, string> keyDict = new Dictionary<string, string>();

            for (int i = 0, num = lines.Length; i < num; i++)
            {
                string context = lines[i];
                if (!NeedHandle(ref context)) continue;
                if (item.itemList[0].isSelect)
                {
                    int resIndex = context.IndexOf(ResourcesLoad);
                    if (resIndex != -1)
                    {
                        string resName = GetLoadFixName(context, csName, i + 1, ref hasData, ref sb);
                        if (!string.IsNullOrEmpty(resName))
                        {
                            if (loadDict.TryGetValue(resName, out int mainAddCount))
                            {
                                loadDict[resName]++;
                            }
                            else
                            {
                                loadDict[resName] = 1;
                            }
                            if (keyDict.TryGetValue(resName, out string key))
                            {
                                loadDict[key] = loadDict[resName];
                            }
                        }

                    }
                    SetSprcialName(context, csName, i + 1, ref loadDict, ref keyDict);
                    resIndex = context.IndexOf(ResourcesUnLoad);
                    if (resIndex != -1)
                    {
                        string resName = GetUnLoadFixName(context, csName, (i + 1), resIndex, ref hasData, ref sb);
                        if (!string.IsNullOrEmpty(resName))
                        {
                            if (loadDict.TryGetValue(resName, out int mainAddCount))
                            {
                                loadDict[resName]--;
                            }
                            else
                            {
                                loadDict[resName] = -1;
                            }
                            if (keyDict.TryGetValue(resName, out string key))
                            {
                                loadDict[key] = loadDict[resName];
                            }
                        }
                    }
                }
            }

            foreach (KeyValuePair<string, int> pair in loadDict)
            {
                if (pair.Value > 0)
                {
                    index++;
                    hasData = true;
                    sb.Append("(" + index + ")." + csName + ":" + "内存没释放 = " + pair.Key).AppendLine();
                }
            }

            if (hasData)
            {
                Append1(_csFuncIndex + "." + csName + "");
                Append1(sb.ToString());
            }
            return hasData;

        }

        static void SetSprcialName(string str, string csName, int lineIndex, ref Dictionary<string, int> loadDic, ref Dictionary<string, string> keyDict)
        {
            int index = str.IndexOf(".result");
            string result = string.Empty;
            if (index != -1)
            {
                string context = str.Substring(0, index);
                int startIndex = index - 1;
                int endIndex = 0;
                bool hasSpace = false;
                for (int i = startIndex; i >= 0; i--)
                {
                    if (context[i] == '=')
                    {
                        endIndex = i;
                        break;
                    }
                    if (context[i] == ' ')
                    {
                        hasSpace = true;
                    }
                    if (hasSpace && context[i] != ' ')
                    {
                        return;
                    }
                }
                //if (res == null || res.result == null)
                string key = str.Substring(endIndex, startIndex - endIndex + 1).Replace(" ", "");
                //     if (csName == "DebutEntrance")
                //         Debug.LogError("csname = " + csName + ";line = " + lineIndex + ";key = " + key + ";endIndex = " + endIndex);
                index = context.IndexOf("=");
                if (index != -1)
                {
                    context = context.Substring(0, index);
                    startIndex = -1;
                    endIndex = 0;
                    for (int i = index - 1; i >= 0; i--)
                    {
                        if (startIndex == -1)
                        {
                            if (context[i] != ' ')
                            {
                                startIndex = i;
                            }
                        }
                        else
                        {
                            if (context[i] == ' ' || context[i] == '=')
                            {
                                endIndex = i;
                                break;
                            }
                        }
                    }


                    if (startIndex > endIndex)
                    {
                        result = context.Substring(endIndex, startIndex - endIndex + 1).Replace(" ", "");
                        //         if (csName == "DebutEntrance")
                        //                Debug.LogError("csname = " + csName + ";line = " + lineIndex + ";key1 = " + result);
                        if (loadDic.ContainsKey(key))
                        {
                            loadDic[result] = loadDic[key];
                            keyDict[result] = key;
                            keyDict[key] = result;
                        }
                    }
                }

            }
        }

        static string GetLoadFixName(string str, string csName, int lineIndex, ref bool hasData, ref StringBuilder sb)
        {
            string result = string.Empty;
            int index = str.IndexOf("=");
            if (index != -1)
            {
                string context = str.Substring(0, index);
                int startIndex = -1;
                int endIndex = 0;
                for (int i = index - 1; i >= 0; i--)
                {
                    if (startIndex == -1)
                    {
                        if (context[i] != ' ')
                        {
                            startIndex = i;
                        }
                    }
                    else
                    {
                        if (context[i] == ' ' || context[i] == '=')
                        {
                            endIndex = i;
                            break;
                        }
                    }
                }
                if (startIndex > endIndex)
                    result = context.Substring(endIndex, startIndex - endIndex + 1).Replace(" ", "");
                //           if (csName == "DebutEntrance")
                //           Debug.LogError("csname = " + csName + ";line = " + lineIndex + ";result = " + result);
            }
            else
            {
                index++;
                hasData = true;
                sb.Append("(" + index + ")." + csName + ":(" + lineIndex + ")只含有加载逻辑" + str).AppendLine();
            }
            return result;
        }


        static string GetUnLoadFixName(string str, string csName, int lineIndex, int refIndex, ref bool hasData, ref StringBuilder sb)
        {
            string result = string.Empty;
            string context = str.Substring(refIndex, str.Length - refIndex);
            int startIndex = context.IndexOf("(");
            int endIndex = context.IndexOf(")");
            if (startIndex == -1 || endIndex == -1)
            {

                //       Debug.LogError("csname = " + csName + ";line = " + lineIndex + ";result = " + result);
            }
            else
            {
                result = context.Substring(startIndex + 1, endIndex - startIndex - 1).Replace(" ", "").Replace(".result", "");
                //     Debug.LogError("result = " + result);
            }
            return result;
        }
        #endregion----------------内存方面------------------   
    }
}