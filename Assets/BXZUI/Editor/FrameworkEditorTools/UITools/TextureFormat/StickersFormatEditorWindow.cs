//
//   贴图格式转换工具（Demo）
//

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

namespace Gala.FrameworkEditorTools
{

  public class StickersFormat : EditorWindow
  {
    //平台选择 默认PC
    private static int _selectOs = 0;

    //文件输入路径 
    private static string _inputPath = "";

    //文件贴图大小
    private static int _textureMaxSize = 2048;
    
    //转换的贴图格式 
    private static TextureImporterType _textureType = TextureImporterType.Default;
    
    //转换的贴图格式 
    private static TextureImporterFormat _textureFormat = TextureImporterFormat.RGBA32;
    
    //文件格式 后续可拓展
    private static readonly string[] _textureTypes = new[] {"*.BMP", "*.JPG", "*.GIF", "*.PNG"};
    
    //平台
    private static string[] _platformLabels = { "Standalone", "Web", "iPhone", "Android", "WebGL"};
    
    public static void Open()
    {
      var window = EditorWindow.GetWindow<StickersFormat>();
      window.titleContent = new GUIContent("贴图转换工具");
    }

    private void OnGUI()
    {
      GUILayout.BeginHorizontal();
      GUILayout.FlexibleSpace();

      _selectOs = GUILayout.Toolbar(_selectOs, _platformLabels, GUILayout.Width(500));
      
      GUILayout.FlexibleSpace();
      GUILayout.EndHorizontal();
      GUILayout.Space(20);
      
      if (Selection.activeObject != null)
      {
        _inputPath = AssetDatabase.GetAssetPath(Selection.activeObject);
      }
      
      GUILayout.Label("需要转换的文件夹路径（选中文件夹即可）:"+ _inputPath);
      _textureMaxSize = EditorGUILayout.IntField("文件贴图大小", _textureMaxSize);
      _textureType = (TextureImporterType)EditorGUILayout.EnumPopup("图片类型", _textureType);
      _textureFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("图片格式", _textureFormat);
      
      ButtonFunctions();
    }

    private void ButtonFunctions()
    {
      GUILayout.Space(20);
      
      if (GUILayout.Button("转换当前文件夹下文件格式", GUILayout.Height(40)))
      {
        SetTextureFormatSetting(_textureFormat);
      }
      
      GUILayout.Space(15);
      
      if (GUILayout.Button("转换当前文件夹下图片大小", GUILayout.Height(40)))
      {
        SetMaxTextureSize(_textureMaxSize);
      }
    }
    
    #region  Tools
    
    //获取当前路径下所有贴图
    private static List<string> GetAllPathTextures()
    {
      List<string> filePaths = new List<string>();
      Object[] objects = Selection.GetFiltered(typeof(Texture2D), SelectionMode.DeepAssets);
      _inputPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        
      for (int i = 0; i < objects.Length; i++)
      {
        filePaths.Add(AssetDatabase.GetAssetPath(objects[i]));
      }

      return filePaths;
    }
    
    //设置图片格式
    private static void SetTextureFormatSetting(TextureImporterFormat newFormat)
    {
      List<string> textures = GetAllPathTextures();
      
      foreach (string path in textures)
      {
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (textureImporter != null)
        {
#if UNITY_5_5_OR_NEWER
          textureImporter.textureType = _textureType;
          TextureImporterPlatformSettings setting = new TextureImporterPlatformSettings();
          setting.format = newFormat;
          setting.name = _platformLabels[_selectOs];
          setting.overridden = true;
          textureImporter.SetPlatformTextureSettings(setting);
#else
          textureImporter.textureFormat = newFormat;
#endif
          textureImporter.SaveAndReimport();
        }
        AssetDatabase.ImportAsset(path);
      }
      
      AssetDatabase.Refresh();
    }

    //设置图片大小
    private static void SetMaxTextureSize(int size)
    {
      List<string> textures = GetAllPathTextures();
      foreach (string path in textures)
      {
        TextureImporter textureImporter = AssetImporter.GetAtPath(path) as TextureImporter;
        if (textureImporter != null)
        {
#if UNITY_5_5_OR_NEWER
          TextureImporterPlatformSettings setting = new TextureImporterPlatformSettings();
          setting.maxTextureSize = size;
          textureImporter.SetPlatformTextureSettings(setting);
#else
          textureImporter.maxTextureSize = size;
#endif
        }
        AssetDatabase.ImportAsset(path);
      }
      AssetDatabase.Refresh();
    }

    #endregion
  }
}
