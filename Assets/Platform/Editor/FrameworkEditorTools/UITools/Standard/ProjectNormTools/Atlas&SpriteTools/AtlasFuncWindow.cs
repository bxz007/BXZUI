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
    public partial class UIAtlasFormatWindow
    {
        static StringBuilder _sb1;
        static int _csFuncIndex;
        static bool _inAnnotate = false;
        static Texture2D _previewTexture;

        #region-------------------atlas方面------------------
        // 处理命名方面
        private static bool HandleAtlas(SpriteAtlas atlas, AtlasCheckItem item)
        {
            StringBuilder sb = new StringBuilder();
            int index = 0;
            bool hasData = false;
            _inAnnotate = false;

            if (item.itemList[0].isSelect)
            {
                RenderTexture tmp = RenderTexture.GetTemporary(
                            GetPreviewTextures(atlas)[0].width,
                            GetPreviewTextures(atlas)[0].height,
                            0,
                            RenderTextureFormat.Default,
                           RenderTextureReadWrite.Linear);

                // 将纹理上的像素 Blit 到 RenderTexture 
                Graphics.Blit(GetPreviewTextures(atlas)[0], tmp);


                // 备份当前设置的 RenderTexture 
                RenderTexture previous = RenderTexture.active;

                // 将当前的 RenderTexture 设置为我们创建的临时
                RenderTexture.active = tmp;
                // 创建一个新的可读 Texture2D 将像素复制到它
                Texture2D myTexture2D = new Texture2D(GetPreviewTextures(atlas)[0].width, GetPreviewTextures(atlas)[0].height);
                // 将像素从 RenderTexture 复制到新的 Texture 
                myTexture2D.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                myTexture2D.Apply();

                Color[] pixels = myTexture2D.GetPixels();
                int colorIndex = 0;
                for (int i = 0, num = pixels.Length; i < num; i++)
                {
                    if (pixels[i].r == 0 && pixels[i].g == 0 && pixels[i].b == 0 && pixels[i].a == 0)
                    {
                        colorIndex++;
                    }
                }
                if ((float)colorIndex / pixels.Length > unuseAtlasImage / 100f)
                {
                    index++;
                    hasData = true;
                    sb.Append("(" + index + ")." + atlas.name + "图集空相素比例= " + (float)colorIndex / pixels.Length).AppendLine();
                }
            }

            if (hasData)
            {
                Append1(_csFuncIndex + "." + atlas.name + "");
                Append1(sb.ToString());
            }
            return hasData;

        }

        // 从图集中获取资源
        public static Texture2D[] GetPreviewTextures(SpriteAtlas spriteAtlas)
        {
            MethodInfo methodInfo = typeof(SpriteAtlasExtensions).GetMethod("GetPreviewTextures", BindingFlags.NonPublic | BindingFlags.Static);
            if (methodInfo == null)
            {
                Debug.LogError("<color=red> 从 SpriteAtlasExtensions 获取方法为空！ </color>");
                return null;
            }
            else
            {
                return methodInfo.Invoke(null, new SpriteAtlas[] { spriteAtlas }) as Texture2D[];
            }
        }
        #endregion----------------atlas方面------------------   

    }
}