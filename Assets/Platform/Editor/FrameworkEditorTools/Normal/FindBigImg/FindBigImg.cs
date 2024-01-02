using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Gala.FrameworkEditorTools
{

    public class FindBigImg : EditorWindow
    {
        static Rect windowRect;
        private Object fileTextField;
        private string exportPath;
        private string importPath;
        private string fileSizeStr;
        private int popupindex;

        public static void Open()
        {
            var FindBigImg = EditorWindow.GetWindow<FindBigImg>();
            FindBigImg.titleContent = new GUIContent("查找项目指定大小图片资源");
            //FindBigImg.windowRect = new Rect(new Vector2(300,300), new Vector2(100,200));
            windowRect = FindBigImg.position;

        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("查找大于多少M的图片资源：");
            Rect fileRect = EditorGUILayout.GetControlRect(GUILayout.Width(300), GUILayout.Height(20));
            fileSizeStr = EditorGUI.TextField(fileRect, fileSizeStr);

            GUILayout.Space(10);
            popupindex = EditorGUILayout.IntPopup("要查询的图片资源类型", popupindex, new[] { "*.png", "*.jpg", "*.tga", "*.psd" }, new[] { 1, 2, 3, 4 });

            GUILayout.Space(10);
            GUILayout.Label("图片资源要导出到的文件夹路径：");
            Rect rct = EditorGUILayout.GetControlRect(GUILayout.Width(300), GUILayout.Height(20));
            exportPath = EditorGUI.TextField(rct, exportPath);
            GUILayout.Space(20);
            if (GUILayout.Button("开始查找并导出", GUILayout.Height(20)))
            {
                if (string.IsNullOrEmpty(fileSizeStr) || string.IsNullOrEmpty(exportPath))
                {
                    Debug.Log("请输入要查找的相应参数值");
                    return;
                }

                if (popupindex == 0)
                {
                    Debug.Log("请选择要查找的图片资源类型");
                    return;
                }
                StartFindBigImg(exportPath, fileSizeStr, popupindex);
            }
            GUILayout.Space(150);
            GUILayout.Label("要导入的图片资源文件夹路径：");
            Rect rct1 = EditorGUILayout.GetControlRect(GUILayout.Width(300), GUILayout.Height(20));
            importPath = EditorGUI.TextField(rct1, importPath);
            GUILayout.Space(20);
            if (GUILayout.Button("开始导入", GUILayout.Height(20)))
            {
                ImportImg(importPath);
            }
        }

        private static void StartFindBigImg(string exportpath, string fileSizeStr, int imgType)
        {
            Debug.Log("开始查找大图");
            int fileSize = int.Parse(fileSizeStr);
            string laststr = exportpath.Substring(exportpath.Length - 1);
            if (laststr != "\\")
            {
                exportpath = exportpath + "\\";
            }
            string destFilePath = exportpath;
            string[] path = new[]{
            Application.dataPath + "/LoadResources",
            Application.dataPath + "/LoadResourcesLanguage",
            Application.dataPath + "/LoadWWWResources",
            Application.dataPath + "/LoadWWWResourcesLanguage"
        };
            DirectoryInfo theFolder;
            FileInfo[] fileInfo;
            string picType = "";
            switch (imgType)
            {
                case 1:
                    picType = "*.png";
                    break;
                case 2:
                    picType = "*.jpg";
                    break;
                case 3://tga
                    picType = "*.tga";
                    break;
                case 4://psd
                    picType = "*.psd";
                    break;
            }
            for (int i = 0; i < path.Length; i++)
            {
                theFolder = new DirectoryInfo(path[i]);
                fileInfo = theFolder.GetFiles(picType, SearchOption.AllDirectories);
                foreach (FileInfo f in fileInfo)
                {
                    if ((Mathf.Round((float)f.Length / 1024)) > 1024 * fileSize)
                    {
                        Debug.Log("文件名：" + f.Name + "   文件大小：" + (Mathf.Round((float)f.Length / (1024 * 1024))) + "M");
                        if (Directory.Exists(destFilePath) == false)
                        {
                            Directory.CreateDirectory(destFilePath);
                        }

                        string fileTempName = f.FullName.Substring(f.FullName.IndexOf("Assets"));
                        fileTempName = fileTempName.Replace('\\', '#');
                        File.Copy(f.FullName, destFilePath + fileTempName, true);
                    }
                }
            }

            Debug.Log("查找大图结束！！！！！！！！！！");
        }

        private static void ImportImg(string path)
        {
            string laststr = path.Substring(path.Length - 1);
            if (laststr != "\\")
            {
                path = path + "\\";
            }
            DirectoryInfo theFolder = new DirectoryInfo(path);
            FileInfo[] fileInfo = theFolder.GetFiles("*", SearchOption.AllDirectories);
            string fileNamePath = "";
            foreach (FileInfo f in fileInfo)
            {
                fileNamePath = f.Name.Replace('#', '/');
                fileNamePath = fileNamePath.Replace("Assets", "");
                Debug.Log("导入目录路径：" + Application.dataPath + fileNamePath);
                File.Copy(f.FullName, Application.dataPath + fileNamePath, true);
            }
            Debug.Log("图片资源导入完成");
        }
    }
}