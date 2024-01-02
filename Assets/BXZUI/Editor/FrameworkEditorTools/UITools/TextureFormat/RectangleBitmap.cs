using System.Linq.Expressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Drawing;
public class RectangleBitmap : EditorWindow
{

    [MenuItem("PlatformTools/裁切图片", validate = false)]
    public static void Open()
    {
        var window = EditorWindow.GetWindow<RectangleBitmap>();
        window.titleContent = new GUIContent("自动裁切工具");
    }

    // private string _inputPath;
    // private string _outputPath;
    // public Vector2 scrollViewPos = Vector2.zero;
    // private void OnGUI()
    // {
    //     GUILayout.Space(10);
    //     
    //     EditorGUILayout.LabelField("选择输入图片路径：", _inputPath);
    //     GUILayout.Space(10);
    //     if (GUILayout.Button("修改图片路径", GUILayout.Width(150)))
    //     {
    //         string path = EditorUtility.OpenFolderPanel("选择图片路径", _inputPath, "");
    //         if (!string.IsNullOrEmpty(path))
    //         {
    //             if (path.IndexOf(_inputPath + "/Assets") != -1)// 路径需要在当前项目的assets文件夹下
    //             {
    //                 _inputPath = path;
    //             }
    //             else
    //             {
    //                 EditorUtility.DisplayDialog("提示", "路径需要选择当前项目的Assets文件夹下", "确定");
    //             }
    //         }
    //     }
    //
    //     if (!string.IsNullOrEmpty(_inputPath))
    //     {
    //         GetAllPngs(_inputPath);
    //     }
    //
    //
    //     GUILayout.Space(10);
    //
    //     EditorGUILayout.LabelField("选择输出图片路径：", _outputPath);
    //     GUILayout.Space(10);
    //     if (GUILayout.Button("修改图片路径", GUILayout.Width(150)))
    //     {
    //         string path = EditorUtility.OpenFolderPanel("选择图片路径", _outputPath, "");
    //         if (!string.IsNullOrEmpty(path))
    //         {
    //             if (path.IndexOf(_outputPath + "/Assets") != -1)// 路径需要在当前项目的assets文件夹下
    //             {
    //                 _outputPath = path;
    //             }
    //             else
    //             {
    //                 EditorUtility.DisplayDialog("提示", "路径需要选择当前项目的Assets文件夹下", "确定");
    //             }
    //         }
    //     }
    //
    //     GUILayout.Space(30);
    //
    //     scrollViewPos = EditorGUILayout.BeginScrollView(scrollViewPos, false, false, new[] { GUILayout.MaxHeight(200) });
    //     for (int i = 0; i < gamesPathList.Count; i++)
    //     {
    //         GUILayout.Label(gamesPathList[i], GUILayout.Height(20));
    //     }
    //     EditorGUILayout.EndScrollView();
    //
    //     GUILayout.Space(20);
    //
    //     if (GUILayout.Button("全部修改"))
    //     {
    //         if (string.IsNullOrEmpty(_inputPath))
    //         {
    //             EditorUtility.DisplayDialog("", "输入路径没设置", "OK");
    //             return;
    //         }
    //         if (string.IsNullOrEmpty(_outputPath))
    //         {
    //             EditorUtility.DisplayDialog("", "输出路径没设置", "OK");
    //             return;
    //         }
    //
    //         for (int i = 0; i < gamesPathList.Count; i++)
    //         {
    //             string[] paths = gamesPathList[i].Split('/');
    //
    //             Bitmap inputImage = new Bitmap(gamesPathList[i]);
    //             Rectangle nonTransparentRegion = FindNonTransparentRegion(inputImage);
    //             // Create a new bitmap with 4-byte aligned dimensions
    //             Bitmap outputImage = new Bitmap(((int)Math.Ceiling(nonTransparentRegion.Width / 4.0)) * 4, ((int)Math.Ceiling(nonTransparentRegion.Height / 4.0)) * 4);
    //
    //             for (int x = nonTransparentRegion.Left; x < nonTransparentRegion.Right; x++)
    //             {
    //                 for (int y = nonTransparentRegion.Top; y < nonTransparentRegion.Bottom; y++)
    //                 {
    //                     if (inputImage.GetPixel(x, y).A > 0)
    //                     {
    //                         outputImage.SetPixel(x - nonTransparentRegion.Left, y - nonTransparentRegion.Top, inputImage.GetPixel(x, y));
    //                     }
    //                 }
    //             }
    //
    //             // Save the output image
    //             outputImage.Save(_outputPath + "/" + paths[paths.Length - 1]);
    //         }
    //         AssetDatabase.SaveAssets();
    //         AssetDatabase.Refresh();
    //         EditorUtility.DisplayDialog("", "替换结束", "OK");
    //     }
    //
    // }
    //
    // private List<string> gamesPathList = new List<string>();
    //
    // private void GetAllPngs(string directory)
    // {
    //     gamesPathList.Clear();
    //
    //     if (string.IsNullOrEmpty(directory) /* || !directory.StartsWith("Assets") */)
    //         throw new ArgumentException("folderPath");
    //
    //     string[] subFolders = Directory.GetDirectories(directory);
    //     string[] guids = null;
    //     string[] assetPaths = null;
    //     int i = 0, iMax = 0;
    //     guids = AssetDatabase.FindAssets("t:Texture", new string[] { directory });
    //     assetPaths = new string[guids.Length];
    //     for (i = 0, iMax = assetPaths.Length; i < iMax; ++i)
    //     {
    //         assetPaths[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
    //         if (!gamesPathList.Contains(assetPaths[i]))
    //         {
    //             //Debug.Log(assetPaths[i]);
    //             gamesPathList.Add(assetPaths[i]);
    //         }
    //     }
    //     /* foreach (var folder in subFolders)
    //     {
    //         GetAllPngs(folder);
    //     } */
    // }

    // private static Rectangle FindNonTransparentRegion(Bitmap image)
    // {
    //     int left = image.Width;
    //     int right = 0;
    //     int top = image.Height;
    //     int bottom = 0;
    //
    //     // Find the bounds of the non-transparent region of the image
    //     for (int x = 0; x < image.Width; x++)
    //     {
    //         for (int y = 0; y < image.Height; y++)
    //         {
    //             if (image.GetPixel(x, y).A > 0)
    //             {
    //                 left = Math.Min(left, x);
    //                 right = Math.Max(right, x);
    //                 top = Math.Min(top, y);
    //                 bottom = Math.Max(bottom, y);
    //             }
    //         }
    //     }
    //
    //     // Expand the bounds to the nearest 4-byte aligned dimensions
    //     left = (left / 4) * 4;
    //     right = ((right / 4) + 1) * 4;
    //     top = (top / 4) * 4;
    //     bottom = ((bottom / 4) + 1) * 4;
    //
    //     return Rectangle.FromLTRB(left, top, right, bottom);
    // }

}
