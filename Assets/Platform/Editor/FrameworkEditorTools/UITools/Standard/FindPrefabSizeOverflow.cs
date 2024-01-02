using System.Text;
using UnityEngine;
using System.Collections;
using UnityEditor;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;
using System;



namespace Gala.FrameworkEditorTools
{

    public class FindPrefabSizeOverflow : EditorWindow
    {

        public struct OverflowFile
        {
            public string filePath;
            public long fileLen;

        }
        public static void Open()
        {
            EditorWindow.GetWindow(typeof(FindPrefabSizeOverflow), true);
        }

        const string path = "Assets/LoadResources/UI";
        string prefabFilterSize = "300";
        static long longPrefabFilterSize = 300;
        private void Reset()
        {
            GetFolderPath();
        }

        List<string> floaderPathList = new List<string>();
        private void GetFolderPath()
        {
            floaderPathList.Clear();
            string[] subFolders = Directory.GetDirectories(path);
            floaderPathList.AddRange(subFolders);
        }

        List<OverflowFile> overflowList = new List<OverflowFile>();
        void OnGUI()
        {
            EditorGUILayout.LabelField("默认文件夹路径：" + path);
            EditorGUILayout.Space(20);
            EditorGUILayout.LabelField("查询多少KB以上的：比如300KB，就输入300");
            prefabFilterSize = GUILayout.TextField(prefabFilterSize);
            EditorGUILayout.Space(20);
            if (GUILayout.Button("统计数据"))
            {
                overflowList.Clear();
                longPrefabFilterSize = int.Parse(prefabFilterSize);
                for (int i = 0; i < floaderPathList.Count; i++)
                {
                    GetAllPrefabs(floaderPathList[i]);
                }
                EditorUtility.DisplayDialog("", "统计数据结束，可以点击\"数据复制到粘贴板\"按钮", "OK");
            }
            EditorGUILayout.Space(20);
            if (GUILayout.Button("数据复制到粘贴板"))
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < overflowList.Count; i++)
                {
                    sb.Append($"文件路径 = {overflowList[i].filePath}，prefab size = {overflowList[i].fileLen / 1000} KB \n");
                }

                GUIUtility.systemCopyBuffer = sb.ToString();

                EditorUtility.DisplayDialog("", "已经复制到粘贴板了", "OK");
            }
        }
        public void GetAllPrefabs(string directory)
        {
            if (string.IsNullOrEmpty(directory) || !directory.StartsWith("Assets"))
                throw new ArgumentException("folderPath");

            string[] subFolders = Directory.GetDirectories(directory);
            string[] guids = null;
            string[] assetPaths = null;
            int i = 0, iMax = 0;

            guids = AssetDatabase.FindAssets("t:Prefab", new string[] { directory });
            assetPaths = new string[guids.Length];
            for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
            {
                assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                long fileLen = new FileInfo(assetPaths[i]).Length;
                if (fileLen >= (longPrefabFilterSize * 1000))
                {
                    Debug.LogError("file name = " + assetPaths[i] + " file len = " + fileLen);
                    OverflowFile of = new OverflowFile();
                    of.filePath = assetPaths[i];
                    of.fileLen = fileLen;

                    overflowList.Add(of);
                }
            }

            foreach (var folder in subFolders)
            {
                GetAllPrefabs(folder);
            }

        }
    }

}