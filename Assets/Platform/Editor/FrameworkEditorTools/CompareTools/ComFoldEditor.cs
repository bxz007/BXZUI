using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
//using AddressableToolkit;
using Codice.Client.Common;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.ResourceLocations;
using UnityEngine.ResourceManagement.ResourceProviders;
using Object = UnityEngine.Object;
using Debug = UnityEngine.Debug;


namespace Gala.FrameworkEditorTools
{
    public class ObjectDependencyWindow : EditorWindow
    {
        private static UnityEngine.Object selectedObject;
        private static List<Object> result = new List<Object>();
        private static Dictionary<Object, string> paths = new Dictionary<Object, string>();
        private static Dictionary<string, string> depsAddressCache = new Dictionary<string, string>();
        Vector3 vector3;
        private bool toggle;

        // [MenuItem("Window/Object Dependency Viewer")]
        public static void Init()
        {
            ObjectDependencyWindow window =
                (ObjectDependencyWindow)EditorWindow.GetWindow(typeof(ObjectDependencyWindow));
            window.Show();
        }

        public static void Init(String path)
        {
            Debug.Log(path);
            UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
            if (obj != null)
            {
                selectedObject = obj;
                Init();
            }
        }

        void OnGUI()
        {
            selectedObject = (UnityEngine.Object)EditorGUILayout.ObjectField("Object to inspect", selectedObject,
                typeof(UnityEngine.Object), true);

            if (selectedObject != null)
            {
                toggle = GUILayout.Toggle(toggle, "只显示prefab");
                if (GUILayout.Button("Print Dependencies"))
                {
                    depsAddressCache.Clear();
                    result.Clear();
                    paths.Clear();
                    string[] dependencies = AssetDatabase.GetDependencies(AssetDatabase.GetAssetPath(selectedObject));

                    foreach (string dependency in dependencies)
                    {
                        UnityEngine.Object obj = AssetDatabase.LoadAssetAtPath(dependency, typeof(UnityEngine.Object));
                        if (obj != null)
                        {
                            if ((toggle && PrefabUtility.IsPartOfPrefabAsset(obj)) || !toggle)
                            {
                                result.Add(obj);
                                paths.Add(obj, dependency);
                            }
                        }
                        else
                        {
                            Debug.LogError(dependency);
                        }
                    }
                }

                vector3 = EditorGUILayout.BeginScrollView(vector3);
                //显示结果
                for (int i = 0; i < result.Count; i++)
                {
                    EditorGUILayout.BeginHorizontal();
                    EditorGUILayout.ObjectField(result[i], typeof(Object), true, GUILayout.Width(300));
                    string path = paths[result[i]];
                    if (!depsAddressCache.ContainsKey(path))
                    {
                        depsAddressCache.Add(path, ComFoldEditor.FindAsset(path));
                    }

                    if (!string.IsNullOrEmpty(depsAddressCache[path]))
                    {
                        EditorGUILayout.TextField("", depsAddressCache[path]);
                    }

                    EditorGUILayout.EndHorizontal();
                }

                EditorGUILayout.EndScrollView();
            }
        }
    }


    public class ComFoldEditor : EditorWindow
    {
        GUIStyle mfileStyle;
        private string basPath;
        private string updatePath;
        private string baseGitHash, hotfixGitHash;
        static long totalSize = 0;
        static IOrderedEnumerable<KeyValuePair<string, long>> sortResult;
        static Dictionary<string, List<string>> bundle4Key = new Dictionary<string, List<string>>();
        ToggleTreeview globaltree;
        static TreeViewItem globalRoot;


        // [MenuItem("Tools/@Addressable/比较Bundle")]
        public static void OpenWindow()
        {
            GetWindow<ComFoldEditor>().Show();
        }

        private void OnEnable()
        {
            mfileStyle = new GUIStyle();
            mfileStyle.normal.background = EditorGUIUtility.FindTexture("Folder Icon");
            string prefBasPath = EditorPrefs.GetString("basPath");
            if (Directory.Exists(prefBasPath))
            {
                basPath = prefBasPath;
            }

            string prefUpdatePath = EditorPrefs.GetString("updatePath");
            if (Directory.Exists(prefUpdatePath))
            {
                updatePath = prefUpdatePath;
            }



        }

        public void InitBundle4Key(string catalogPath)
        {
            Action<string, IList<IResourceLocation>> logBunSzie = (x, y) =>
            {
                foreach (IResourceLocation location in y)
                {
                    var sizeData = location.Data as AssetBundleRequestOptions;

                    if (sizeData != null)
                    {
                        if (!bundle4Key.TryGetValue(location.PrimaryKey, out var collectList))
                        {
                            collectList = new List<string>();
                            bundle4Key.Add(location.PrimaryKey, collectList);
                        }

                        if (!collectList.Contains(x))
                        {
                            collectList.Add(x);
                        }
                    }

                    break;
                }
            };

            try
            {
                var conetentData = JsonUtility.FromJson<ContentCatalogData>(File.ReadAllText(catalogPath));
                var resMap = conetentData.CreateLocator();
                foreach (var item in resMap.Locations)
                {
                    foreach (IResourceLocation location in item.Value.Distinct())
                    {
                        if (location.Data is AssetBundleRequestOptions ab)
                        {
                        }
                        else
                        {
                            logBunSzie(location.InternalId, location.Dependencies);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("热更bundle目录下catalog文件不存在");
                Debug.LogError(e);
                throw;
            }
        }

        public static string FindAsset(string fullPath)
        {
            if (globalRoot != null)
                foreach (var VARIABLE in globalRoot.children)
                {
                    if (VARIABLE.hasChildren)
                        foreach (var viewItem in VARIABLE.children)
                        {
                            if (fullPath.Contains(viewItem.displayName))
                            {
                                return viewItem.displayName;
                            }
                        }
                }

            return string.Empty;
        }

        private void InitTreeData()
        {
            if (globalRoot == null)
            {
                int id = -1;
                globalRoot = new TreeViewItem();
                globalRoot.id = id;
                globalRoot.depth = -1;
                foreach (var l in sortResult)
                {
                    var tItem = new TreeViewItem();
                    tItem.id = ++id;
                    tItem.depth = 0;
                    tItem.displayName = $"{l.Key}   {GetFileSize(l.Value)}";
                    globalRoot.AddChild(tItem);

                    if (bundle4Key.TryGetValue(l.Key, out var address))
                    {
                        foreach (var VARIABLE in address)
                        {
                            TreeViewItem addressName = new TreeViewItem();
                            addressName.displayName = VARIABLE;
                            addressName.id = ++id;
                            addressName.depth = 1;
                            tItem.AddChild(addressName);
                        }
                    }
                }
            }

            if (globaltree == null)
            {
                globaltree = new ToggleTreeview("", "", position.width, globalRoot, true, true, 18);
                // globaltree.CellCallBack += DrawItemCell;
                globaltree.Reload();
                globaltree.OnDoubleClicked = (item) =>
                {
                    string parentStr = String.Empty;
                    if (item.parent != null && !string.IsNullOrEmpty(item.parent.displayName))
                    {
                        parentStr = " parent:" + item.parent.displayName;
                    }

                    string copy = item.displayName.Split(' ')[0];
                    GUIUtility.systemCopyBuffer = copy + parentStr;
                    GetWindow<ComFoldEditor>().ShowNotification(new GUIContent("已copy"));
                };
                globaltree.OnContextClicked = (item) =>
                {
                    GenericMenu menu = new GenericMenu();
                    if (item.hasChildren)
                    {
                        menu.AddItem(new GUIContent("提取AssetBundle信息"), false, () =>
                        {
                            string bundleName = item.displayName.Split(' ')[0];
                            ExtractInfo(updatePath + Path.DirectorySeparatorChar + bundleName);
                        });
                    }
                    else
                    {
                        menu.AddItem(new GUIContent("打开依赖"), false, () =>
                        {
                            string copy = item.displayName.Split(' ')[0];
                            string fullPath = GetAddressableFullPath(copy);
                            ObjectDependencyWindow.Init(fullPath);
                        });
                    }

                    menu.DropDown(new Rect(Event.current.mousePosition, Vector2.zero));
                };

            }
        }

        private void ExtractInfo(string bundlePath)
        {
            Debug.Log("bundle Path:" + bundlePath);
            //Unity获取SDK目录
            string exePath = EditorApplication.applicationPath.Replace("Unity.exe", $"Data/Tools");
            string infoPath = bundlePath + "_data";
            if (!Directory.Exists(infoPath))
            {
                string webExtract = "WebExtract.exe";
                ShellHelper.ProcessCommandSync($"\"{webExtract}\" \"{bundlePath}\"", exePath);
                Debug.Log("webExtract success");
            }

            string binary2text = "binary2text.exe";
            string[] files = Directory.GetFiles(infoPath); // 获取目录下所有文件路径
            if (files.Length > 0)
            {
                string extractPath = files[0];
                foreach (string file in files)
                {
                    extractPath = file.Length < extractPath.Length ? file : extractPath;
                }

                Debug.Log("Extract Path:" + extractPath);
                ShellHelper.ProcessCommandSync($"\"{binary2text}\" \"{extractPath}\"", exePath);
                Debug.Log("binary2text success");
                Application.OpenURL(infoPath);
            }
        }

        public string GetAddressableFullPath(string name)
        {
            var aaSettings = AddressableAssetSettingsDefaultObject.GetSettings(false);
            foreach (var group in aaSettings.groups)
            {
                var entries = group.entries;
                foreach (var entry in entries)
                {
                    if (entry.AssetPath.Equals(name))
                    {
                        return entry.AssetPath;
                    }
                }
            }

            Debug.LogError($"未在项目中找到{name},请确保资源存在并且刷新过");
            return string.Empty;
        }

        private Vector3 mScrollPos;

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("底包bundle目录：", GUILayout.Width(100));
            basPath = EditorGUILayout.TextField(basPath, GUILayout.Width(420));
            if (GUILayout.Button("选择"))
            {
                basPath = EditorUtility.OpenFolderPanel("bundle目录", "", "");
                EditorPrefs.SetString("basPath", basPath);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("热更bundle目录：", GUILayout.Width(100));
            updatePath = EditorGUILayout.TextField(updatePath, GUILayout.Width(420));
            if (GUILayout.Button("选择"))
            {
                updatePath = EditorUtility.OpenFolderPanel("bundle目录", "", "");
                EditorPrefs.SetString("updatePath", updatePath);
            }

            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("底包Git-ShaHash", GUILayout.Width(100));
            baseGitHash = EditorGUILayout.TextField(baseGitHash, GUILayout.Width(200));

            GUILayout.Label("热更Git-ShaHash", GUILayout.Width(100));
            hotfixGitHash = EditorGUILayout.TextField(hotfixGitHash, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            if (GUILayout.Button("比较"))
            {
                string catalogPath = updatePath + "/catalog_base.json";
                InitBundle4Key(catalogPath);
                CompareFold(basPath, updatePath);
                globalRoot = null;
                globaltree = null;
                InitTreeData();
            }

            if (GUILayout.Button("Git Diff With Big Changes"))
            {
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.FileName = "git";
                startInfo.Arguments = $"diff --name-status {baseGitHash}..{hotfixGitHash}";
                startInfo.RedirectStandardOutput = true;
                startInfo.UseShellExecute = false;

                Process process = new Process();
                process.StartInfo = startInfo;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                Debug.Log(output);

                if (sortResult == null)
                {
                    Debug.LogError("请先点击【比较】按钮才能比对Git提交依赖");
                    return;
                }

                var lines = output.Split('\n');
                foreach (var l in sortResult)
                {
                    if (l.Value < 1048576) continue;

                    if (bundle4Key.TryGetValue(l.Key, out var address))
                    {
                        foreach (var VARIABLE in address)
                        {
                            Debug.Log(VARIABLE);
                            string[] paths = AssetDatabase.GetDependencies(new string[] { VARIABLE });
                            foreach (string path in paths)
                            {
                                foreach (string line in lines)
                                {
                                    if (line.Contains(path))
                                    {
                                        Debug.LogError(line);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (totalSize >= 0)
            {
                GUILayout.Label("热更大小：" + GetFileSize(totalSize));
            }

            if (sortResult != null && sortResult.Any())
            {
                GUILayout.BeginArea(new Rect(10, 130, this.position.width - 20, this.position.height - 100));
                mScrollPos = GUILayout.BeginScrollView(mScrollPos);
                if (globaltree != null)
                {
                    globaltree.OnGUI(new Rect(0, 0, this.position.width - 20, this.position.height - 100));
                }

                GUILayout.EndScrollView();
                GUILayout.EndArea();
            }


        }

        public static void CompareFold(string basePath, string updatePath)
        {
            if (string.IsNullOrEmpty(basePath) || string.IsNullOrEmpty(updatePath))
            {
                Debug.LogError("请选择目录！");
                return;
            }

            List<string> baseBundleFullNames = new List<string>();
            CollectAllFilesName(basePath, baseBundleFullNames);
            List<string> baseBundleNames = baseBundleFullNames.ConvertAll(Path.GetFileName);
            // baseBundleNames.ForEach(Debug.Log);

            List<string> updateBundleFullNames = new List<string>();
            CollectAllFilesName(updatePath, updateBundleFullNames);


            Dictionary<string, long> result = new Dictionary<string, long>();
            foreach (var VARIABLE in updateBundleFullNames)
            {
                //名字一样  没有发生变化
                if (baseBundleNames.Contains(Path.GetFileName(VARIABLE)))
                {
                    continue;
                }

                //新增文件
                result.Add(Path.GetFileName(VARIABLE), new FileInfo(VARIABLE).Length);
            }


            sortResult = from d in result orderby d.Value descending select d;
            totalSize = 0;
            foreach (var l in sortResult)
            {
                totalSize += l.Value;
                Debug.Log(l.Key + "            " + GetFileSize(l.Value));
            }

            Debug.Log("热更 total size:" + GetFileSize(totalSize));
        }

        public static void CollectAllFilesName(string path, List<string> result)
        {
            if (result == null)
            {
                Debug.LogError("result list is null.");
            }

            foreach (var filePath in Directory.GetFiles(path))
            {
                if (filePath.EndsWith("bundle"))
                {
                    result.Add(filePath);
                }
            }

            foreach (var directory in Directory.GetDirectories(path))
            {
                CollectAllFilesName(directory, result);
            }
        }

        public static string GetFileSize(long byteCount)
        {
            string size = "0 B";
            if (byteCount >= 1073741824.0)
                size = $"{byteCount / 1073741824.0:##.##}" + " GB";
            else if (byteCount >= 1048576.0)
                size = $"{byteCount / 1048576.0:##.##}" + " MB";
            else if (byteCount >= 1024.0)
                size = $"{byteCount / 1024.0:##.##}" + " KB";
            else if (byteCount > 0 && byteCount < 1024.0)
                size = byteCount.ToString() + " B";

            return size;
        }
    }
}
