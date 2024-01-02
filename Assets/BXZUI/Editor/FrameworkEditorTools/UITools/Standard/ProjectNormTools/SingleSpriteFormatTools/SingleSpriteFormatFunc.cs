using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;

namespace Gala.FrameworkEditorTools
{

    public class WrongType
    {
        public const int MipMap = 1;
        public const int AlphaIsTransparency = 2;
        public const int ReadWrite = 4;
        public const int SizeIs4N = 8;
        public const int ShouldNotBeETC24Bit = 16;
        public const int ShouldNotBeETC28Bit = 32;
        public const int NotUseETC2 = 64;
        public const int UseAutoFormat = 128;
    }

    public enum CheckAlphaType
    {
        NotCheck = 1,
        NoAlpha = 2,
        HasAlpha = 3,
    }

    public class SingleSpriteWrong
    {
        public int wrongType;
        public string path;
        public Texture2D texture;
        public CheckAlphaType textureHasAlpha;
        public bool isSelected = false;
    }

    public partial class SingleSpriteFormatWindow
    {
        static StringBuilder _sb1;
        static int _csFuncIndex;
        static bool _inAnnotate = false;
        static Texture2D _previewTexture;
        private static List<SingleSpriteWrong> _wrongList = new List<SingleSpriteWrong>();
        private static bool hasData = false;
        private static StringBuilder sb = new StringBuilder();

        private static List<int> wrongTypeList = new List<int>()
    {
        1, 2, 4, 8, 16, 32, 64,128
    };
        private static bool HandleTexture(string texturePath, SingleSpriteCheckItem item)
        {
            sb.Clear();
            hasData = false;
            _inAnnotate = false;
            Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            bool hasAlpha = false;
            if (item.itemList[1].isSelect || item.itemList[5].isSelect) hasAlpha = CheckHasAlpha(tex);
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            int wrongType = 0;
            for (int i = 0; i < item.itemList.Count; i++)
            {
                if (!item.itemList[i].isSelect) continue;
                switch (i)
                {
                    case 0:
                        if (CheckMipMap(importer))
                        {
                            wrongType |= WrongType.MipMap;
                            hasData = true;
                        }
                        break;
                    case 1:
                        if (CheckAlphaIsTransparency(importer, hasAlpha))
                        {
                            wrongType |= WrongType.AlphaIsTransparency;
                            hasData = true;
                        }
                        break;
                    case 2:
                        if (CheckReadWrite(importer))
                        {
                            wrongType |= WrongType.ReadWrite;
                            hasData = true;
                        }
                        break;
                    case 3:
                        if (CheckSizeIs4N(importer))
                        {
                            wrongType |= WrongType.SizeIs4N;
                            hasData = true;
                        }
                        break;
                    case 4:
                        if (CheckEtc2(importer, ref wrongType))
                        {
                            hasData = true;
                        }
                        break;
                    case 5:
                        CheckAndroidFormat(importer, hasAlpha, ref wrongType);
                        break;
                }
            }

            if (wrongType != 0)
            {
                SingleSpriteWrong wrong = new SingleSpriteWrong();
                wrong.path = texturePath;
                wrong.wrongType = wrongType;
                wrong.texture = tex;
                if (item.itemList[1].isSelect || item.itemList[5].isSelect)
                {
                    wrong.textureHasAlpha = hasAlpha ? CheckAlphaType.HasAlpha : CheckAlphaType.NoAlpha;
                }
                else
                {
                    wrong.textureHasAlpha = CheckAlphaType.NotCheck;
                }
                _wrongList.Add(wrong);

                foreach (var type in wrongTypeList)
                {
                    if ((type & wrongType) == 0)
                    {
                        continue;
                    }

                    switch (type)
                    {
                        case WrongType.MipMap:
                            sb.Append(texturePath + "勾选了MipMap").AppendLine();
                            break;
                        case WrongType.ReadWrite:
                            sb.Append(texturePath + "勾选了Read/Write").AppendLine();
                            break;
                        case WrongType.AlphaIsTransparency:
                            sb.Append(texturePath + "存在透明度但是没有勾选了Alpha is Transparency").AppendLine();
                            break;
                        case WrongType.SizeIs4N:
                            sb.Append(texturePath + "长宽不是四的倍数").AppendLine();
                            break;
                        case WrongType.NotUseETC2:
                            sb.Append(texturePath + "安卓压缩格式没有使用ETC2").AppendLine();
                            break;
                        case WrongType.ShouldNotBeETC24Bit:
                            sb.Append(texturePath + "该图片需要压缩Aphla通道，不应该使用ETC2 4Bit 请确认").AppendLine();
                            break;
                        case WrongType.ShouldNotBeETC28Bit:
                            sb.Append(texturePath + "该图片不需要压缩Aphla通道，不应该使用ETC2 8Bit 请确认").AppendLine();
                            break;
                        case WrongType.UseAutoFormat:
                            sb.Append(texturePath + "该图片压缩格式为Automatic，unity将自动设置， 请确认是否正确").AppendLine();
                            break;
                    }
                    Append1(_csFuncIndex + "." + texturePath + "");
                    Append1(sb.ToString());
                }
            }
            return wrongType != 0;
        }


        public static bool CheckMipMap(TextureImporter importer)
        {
            bool res = importer.mipmapEnabled;
            return res;
        }

        public static bool CheckAlphaIsTransparency(TextureImporter importer, bool hasAlpha)
        {
            if (hasAlpha)
            {
                bool res = !importer.alphaIsTransparency;
                return res;
            }
            return false;
        }

        public static bool CheckEtc2(TextureImporter importer, ref int wrongType)
        {
            var settings = importer.GetPlatformTextureSettings("Android");
            bool hasChooseETC2 = settings.format == TextureImporterFormat.ETC2_RGB4 ||
                                 settings.format == TextureImporterFormat.ETC2_RGBA8 || settings.format == TextureImporterFormat.ETC2_RGBA8Crunched ||
                                 settings.format == TextureImporterFormat.Automatic || settings.format == TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA;
            if (!hasChooseETC2) wrongType |= WrongType.NotUseETC2;
            if (settings.format == TextureImporterFormat.Automatic)
            {
                wrongType |= WrongType.UseAutoFormat;
            }
            return !hasChooseETC2;
        }
        public static void CheckAndroidFormat(TextureImporter importer, bool hasAlpha, ref int wrongType)
        {
            var settings = importer.GetPlatformTextureSettings("Android");
            if (hasAlpha && settings.format == TextureImporterFormat.ETC2_RGB4)
            {
                wrongType |= WrongType.ShouldNotBeETC24Bit;
            }
            if (!hasAlpha && settings.format == TextureImporterFormat.ETC2_RGBA8)
            {
                wrongType |= WrongType.ShouldNotBeETC28Bit;
            }
        }

        public static bool CheckSizeIs4N(TextureImporter importer)
        {
            bool res = true;
            if (importer != null)
            {
                object[] args = new object[2];
                MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
                mi.Invoke(importer, args);
                int width = (int)args[0];
                int height = (int)args[1];
                res = width % 4 == 0 && height % 4 == 0;
            }
            return !res;
        }

        public static bool CheckReadWrite(TextureImporter importer)
        {
            bool res = importer.isReadable;
            return res;
        }

        private static bool CheckHasAlpha(Texture2D texture)
        {
            RenderTexture tmp = RenderTexture.GetTemporary(
                texture.width,
                texture.height,
                0,
                RenderTextureFormat.Default,
                RenderTextureReadWrite.Linear);
            Graphics.Blit(texture, tmp);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = tmp;
            Texture2D myTexture2D = new Texture2D(texture.width, texture.height);
            myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
            myTexture2D.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(tmp);

            Color[] pixels = myTexture2D.GetPixels();
            bool hasAlpha = false;
            for (int i = 0; i < pixels.Length; i++)
            {
                if (pixels[i].a < 0.95)
                {
                    hasAlpha = true;
                    break;
                }
            }
            return hasAlpha;
        }
    }
}