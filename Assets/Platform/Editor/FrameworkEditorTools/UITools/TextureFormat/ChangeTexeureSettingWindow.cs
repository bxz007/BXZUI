using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Gala.FrameworkEditorTools
{
    public class ChangeTexeureSettingWindow : EditorWindow
    {
        enum MaxSize
        {
            Size_32 = 32,
            Size_64 = 64,
            Size_128 = 128,
            Size_256 = 256,
            Size_512 = 512,
            Size_1024 = 1024,
            Size_2048 = 2048,
            Size_4096 = 4096,
            Size_8192 = 8192,
        }
        enum TargetPlatform
        {
            Standalone,
            iPhone,
            Android
        }

        public static void OpenChangeTexureSettingsWindow()
        {
            ChangeTexeureSettingWindow window = EditorWindow.GetWindow<ChangeTexeureSettingWindow>();
            window.Show();
        }

        string TexPath;
        string TexSuffix = "*.bmp|*.tga|*.jpg|*.gif|*.png|*.tif|*.psd";
        TargetPlatform SelectPlatform = TargetPlatform.Android;
        TextureImporterFormat WithAlpha = TextureImporterFormat.ASTC_4x4;
        TextureImporterFormat WithoutAlpha = TextureImporterFormat.PVRTC_RGB4;
        MaxSize textureSize = MaxSize.Size_1024;
        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            TexPath = EditorGUILayout.TextField("图片资源路径", TexPath);
            if (GUILayout.Button(EditorGUIUtility.IconContent("Folder Icon"), GUILayout.Width(18), GUILayout.Height(18)))
            {
                TexPath = EditorUtility.OpenFolderPanel("图片路径选择", "", "");
            }
            GUILayout.EndHorizontal();
            GUI.enabled = false;
            GUILayout.BeginHorizontal();
            TexSuffix = EditorGUILayout.TextField("图片格式", TexSuffix);
            GUILayout.EndHorizontal();
            GUI.enabled = true;
            GUILayout.BeginHorizontal();
            SelectPlatform = (TargetPlatform)Enum.Parse(typeof(TargetPlatform), EditorGUILayout.EnumPopup("选择目标平台", SelectPlatform).ToString());
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            textureSize = (MaxSize)EditorGUILayout.EnumPopup("尺寸:", textureSize);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            WithAlpha = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), EditorGUILayout.EnumPopup("有Alpha通道", WithAlpha).ToString());
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            WithoutAlpha = (TextureImporterFormat)Enum.Parse(typeof(TextureImporterFormat), EditorGUILayout.EnumPopup("没有Alpha通道", WithoutAlpha).ToString());
            GUILayout.EndHorizontal();
            GUILayout.Space(20);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("转换"))
            {
                if (string.IsNullOrEmpty(TexPath) || !Directory.Exists(TexPath))
                {
                    EditorUtility.DisplayDialog("错误", "路径不能为空或路径不存在", "确定");
                    return;
                }
                if (string.IsNullOrEmpty(TexSuffix))
                {
                    EditorUtility.DisplayDialog("错误", "路径不能为空或路径不存在", "确定");
                    return;
                }
                List<string> lst = GetAllTexPaths(TexPath);
                TextureImporterPlatformSettings settings = new TextureImporterPlatformSettings();
                settings.name = SelectPlatform.ToString();
                settings.crunchedCompression = true;
                settings.overridden = true;
                int i = 0;
                EditorUtility.DisplayProgressBar("修改", "修改图片格式", 0);
                for (; i < lst.Count; i++)
                {
                    Change(lst[i], settings);
                    //Debug.Log("path:" + lst[i]);
                    EditorUtility.DisplayProgressBar("转换", string.Format("修改图片格式    {0}/{1}", i, lst.Count), i / (float)lst.Count);
                }
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();

                EditorUtility.DisplayDialog("", "转换结束", "OK");
            }
            GUILayout.EndHorizontal();
        }
        private void Change(string path, TextureImporterPlatformSettings platformSettings)
        {
            path = path.Substring(path.IndexOf("Assets"));
            try
            {
                TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
                if (textureImporter.DoesSourceTextureHaveAlpha())
                {
                    platformSettings.format = WithAlpha;
                }
                else
                {
                    platformSettings.format = WithoutAlpha;
                }
                platformSettings.maxTextureSize = (int)textureSize;
                textureImporter.SetPlatformTextureSettings(platformSettings);
                textureImporter.SaveAndReimport();
                AssetDatabase.ImportAsset(path);
            }
            catch
            {
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }
        }


        private List<string> GetAllTexPaths(string rootPath)
        {
            List<string> lst = new List<string>();
            string[] types = TexSuffix.Split('|');
            for (int i = 0; i < types.Length; i++)
            {
                lst.AddRange(Directory.GetFiles(rootPath, types[i], SearchOption.AllDirectories));
            }
            return lst;
        }
    }
}