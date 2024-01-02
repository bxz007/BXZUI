using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using System.Linq;
namespace Gala.FrameworkEditorTools
{
    public class EditorTextureOfEtc : EditorWindow
    {
        class ImageItem
        {
            public bool isSelect; // 是否选中
            public bool isAlpha; // 是否具有alpha通道
            public bool isFixSuc;// 是否能修复成功
            public int width;
            public int height;
            public int ywidth; // 原图片大小
            public int yheight;
            public int fixWidth;// 修复后的大小
            public int fixHeight;
            public int androidMaxSize;
            public int shrinkWidth;
            public int shrinkHeight;
            public string relativePath;
            public Texture2D relativeTex;
            public ImageItem(int w, int h, string rs, Texture2D re, bool ia, int aMaxSize = -1, int ywid = -1, int yhei = -1)
            {
                this.width = w;
                this.height = h;
                this.ywidth = ywid;
                this.yheight = yhei;
                this.fixWidth = w;
                this.fixHeight = h;
                this.relativePath = rs;
                this.isSelect = false;
                this.relativeTex = re;
                this.isAlpha = ia;
                this.isFixSuc = true;
                if (aMaxSize != -1)
                {
                    this.androidMaxSize = aMaxSize;
                    if (ywidth == -1 || yheight == -1)
                    {
                        this.androidMaxSize = -1;
                    }
                    else
                    {
                        CalculateNewImgShrinkSize(ywidth, yheight, aMaxSize, out this.shrinkWidth, out this.shrinkHeight);
                    }
                    //Debug.Log($"relativeTex: {this.relativeTex}  aMaxSize{this.androidMaxSize} width:{width} height{height} shrinkWidth:{shrinkWidth} shrinkHeight{shrinkHeight}");
                }
                else
                {
                    this.androidMaxSize = -1;
                    this.shrinkWidth = this.width;
                    this.shrinkHeight = this.height;
                }
            }
        }
        public static void OpenWindow()
        {
            var editorTexWindows = EditorWindow.GetWindow<EditorTextureOfEtc>();
            editorTexWindows.titleContent = new GUIContent("EditorTextureOfEtc");
            editorTexWindows.minSize = new Vector2(700, 750);
            editorTexWindows.position = new Rect(0, 0, 700, 750);
        }

        List<ImageItem> noMatchImgItemList = new List<ImageItem>();  // 不是4的倍数的图片路径List 宽高
        List<ImageItem> selecAndFixedList = new List<ImageItem>();
        List<ImageItem> fixedImgItemList = new List<ImageItem>();
        List<ImageItem> cannotFixedImgItemList = new List<ImageItem>();
        List<ImageItem> maxSizeSmallImgItemList = new List<ImageItem>();  // 安卓平台的maxsize小于图片的宽高
                                                                          // 添加到Git 暂存区的列表
        List<ImageItem> addGitStorageImgItemList = new List<ImageItem>();

        // 折叠样式
        GUIStyle redFoldStyle = new GUIStyle(EditorStyles.foldout);
        GUIStyle greenFoldStyle = new GUIStyle(EditorStyles.foldout);
        private void OnEnable()
        {
            ResetStyle();
            ResetList();
            FindGitResPath();
        }
        // 找到Git本地仓库的路径
        private void FindGitResPath()
        {
            // 先检查当前项目下是否有git文件夹，如果有就当前目录，否则向上找两级
            string currentDirectory = Directory.GetCurrentDirectory();
            gitResPath = FindGitDirectory(currentDirectory, 2);
        }
        static string FindGitDirectory(string startDirectory, int maxLevels)
        {
            int currentLevel = 0;
            string currentDirectory = startDirectory;

            while (currentLevel <= maxLevels)
            {
                string gitPath = Path.Combine(currentDirectory, ".git");
                if (Directory.Exists(gitPath))
                {
                    return currentDirectory;
                }
                if (Directory.GetParent(currentDirectory) != null)
                {
                    currentDirectory = Directory.GetParent(currentDirectory).FullName;
                    currentLevel++;
                }
                else
                {
                    break;
                }
            }
            return null;
        }
        private void ResetStyle()
        {
            redFoldStyle = new GUIStyle(EditorStyles.foldout);
            redFoldStyle.normal.textColor = UnityEngine.Color.yellow;
            redFoldStyle.onNormal.textColor = UnityEngine.Color.yellow;
            redFoldStyle.fontSize = 14;

            greenFoldStyle = new GUIStyle(EditorStyles.foldout);
            greenFoldStyle.normal.textColor = UnityEngine.Color.green;
            greenFoldStyle.onNormal.textColor = UnityEngine.Color.green;
            greenFoldStyle.fontSize = 14;
        }
        private void ResetList()
        {
            noMatchImgItemList = noMatchImgItemList == null ? new List<ImageItem>() : noMatchImgItemList;
            cannotFixedImgItemList = cannotFixedImgItemList == null ? new List<ImageItem>() : cannotFixedImgItemList;
            fixedImgItemList = fixedImgItemList == null ? new List<ImageItem>() : fixedImgItemList;
            maxSizeSmallImgItemList = maxSizeSmallImgItemList == null ? new List<ImageItem>() : maxSizeSmallImgItemList;
            selecAndFixedList = selecAndFixedList == null ? new List<ImageItem>() : selecAndFixedList;
            addGitStorageImgItemList = addGitStorageImgItemList == null ? new List<ImageItem>() : addGitStorageImgItemList;

            noMatchImgItemList.Clear();
            cannotFixedImgItemList.Clear();
            fixedImgItemList.Clear();
            maxSizeSmallImgItemList.Clear();
            selecAndFixedList.Clear();
            addGitStorageImgItemList.Clear();
        }
        // 界面控件功能相关属性
        static string currentProjectPath = Directory.GetCurrentDirectory().Replace("\\", "/");
        static string gitResPath = "";
        static string gitExePath = "";  // git.exe的位置
        bool isGetGitExePathOnce = false;// 是否获取过git.exe地址
        static string selImagePathStr = $"{currentProjectPath}/Assets";
        string exceptImagePathStr = "";
        static string genTxtPathStr = $"{currentProjectPath}/CustomExternalData";
        const string CANNOTFIXIMGLISTTXTPATHSTR = "cannotfix_imglist_output.txt";
        const string MAXSIZESMALLIMGLISTTXTPATHSTR = "maxSizeSamll_imglist_output.txt";

        // 界面控件相关属性
        string defaultExceptImgPathStr = "UISpriteAtlas";
        private bool isDefaultExptImgPath = true;
        private bool isNoExceptAutoCompress = true; // 是否排除未选择压缩格式的图片
        private bool isAddToGitStorageCacheOfEnlarge = false; // 是否修改完就添加到Git暂存区
                                                              //private bool isAddToGitStorageCacheOfShrink = false; // 是否修改完就添加到Git暂存区

        private bool noMatchShowListBool = false;// 未修复
        private bool maxSizeSmallShowListBool = false;// maxsize太小
        private bool fixedShowListBool = false;// 修复好
        private bool cannotFixShowListBool = false;// 无法修复好
        private Vector2 noMatchscrollPositionVec2 = Vector2.zero;
        private Vector2 maxSizeSmallScrollPositionVec2 = Vector2.zero;
        private Vector2 fixedScrollPositionVec2 = Vector2.zero;
        private Vector2 cannotFixscrollPositionVec2 = Vector2.zero;
        private Vector2 wholeScreenScrollPositionVec2 = Vector2.zero;
        private const string NOMATCHTIPSTR = "列表：宽高不是4倍数的图片";
        private const string MAXSIZESMALLTIPSTR = "列表：安卓平台MaxSize小于源图片宽高";
        private const string FIXTIPSTR = "列表：修复好宽高的图片";
        private const string CANNOTFIXTIPSTR = "列表：无法修复的图片(不具有alpha通道或者修复失败)";


        private int itemHeight = 40;
        private void OnGUI()
        {
            //下面的UI垂直居中
            EditorGUILayout.BeginVertical();
            wholeScreenScrollPositionVec2 = EditorGUILayout.BeginScrollView(wholeScreenScrollPositionVec2, GUILayout.Width(position.width), GUILayout.Height(position.height));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("当前选择图片路径：", selImagePathStr);
            if (GUILayout.Button("修改图片路径", GUILayout.Width(150)))
            {
                string path = EditorUtility.OpenFolderPanel("选择图片路径", currentProjectPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.IndexOf(currentProjectPath + "/Assets") != -1)// 路径需要在当前项目的assets文件夹下
                    {
                        selImagePathStr = path;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "路径需要选择当前项目的Assets文件夹下", "确定");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(10);

            EditorGUILayout.BeginHorizontal();
            isDefaultExptImgPath = EditorGUILayout.Toggle(isDefaultExptImgPath, GUILayout.Width(14));
            EditorGUILayout.LabelField($"排除带有", GUILayout.Width(130));
            defaultExceptImgPathStr = EditorGUILayout.TextField(defaultExceptImgPathStr, GUILayout.Width(160));
            EditorGUILayout.LabelField($"字符串文件夹下的图片（多个用英文;隔开）");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("排除路径：", exceptImagePathStr);
            if (GUILayout.Button("修改排除路径", GUILayout.Width(150)))
            {
                string path = EditorUtility.OpenFolderPanel("修改排除路径", currentProjectPath, "");
                if (!string.IsNullOrEmpty(path))
                {
                    if (path.Equals(selImagePathStr))
                    {
                        EditorUtility.DisplayDialog("提示", "修改图片路径 与 排除路径 不能一样", "确定");
                    }
                    else if (path.IndexOf(currentProjectPath + "/Assets") != -1)
                    {
                        exceptImagePathStr = path;
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("提示", "路径需要选择当前项目的Assets文件夹下", "确定");
                    }
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            isNoExceptAutoCompress = EditorGUILayout.Toggle(isNoExceptAutoCompress, GUILayout.Width(14));
            EditorGUILayout.LabelField("不排除自动压缩格式的图片");
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(15);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("列出宽高不是4倍数的图片", GUILayout.Height(30)))
            {
                noMatchShowListBool = true;
                maxSizeSmallShowListBool = true;
                fixedShowListBool = false;
                cannotFixShowListBool = true;
                ResetList();
                GenListEleTextures();
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(15);

            DrawScrollViw(ref noMatchShowListBool, NOMATCHTIPSTR, noMatchImgItemList, ref noMatchscrollPositionVec2, 1);
            DrawScrollViw(ref maxSizeSmallShowListBool, MAXSIZESMALLTIPSTR, maxSizeSmallImgItemList, ref maxSizeSmallScrollPositionVec2, 2);
            DrawScrollViw(ref fixedShowListBool, FIXTIPSTR, fixedImgItemList, ref fixedScrollPositionVec2, 3);
            DrawScrollViw(ref cannotFixShowListBool, CANNOTFIXTIPSTR, cannotFixedImgItemList, ref cannotFixscrollPositionVec2, 4);


            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }
        // 绘制Scrollview函数
        // TODO:根据flag的int值来绘制不同的控件，不太好的方式，应该用enum
        GUIStyle curFoldStyle;
        private void DrawScrollViw(ref bool isShowItemList, string scrollTipstr, List<ImageItem> imgItemList, ref Vector2 positionVec2, int flag)
        {
            curFoldStyle = flag != 3 ? redFoldStyle : greenFoldStyle;// 如果flag == 3，表示是修复好的列表，需要用绿色字体
            isShowItemList = EditorGUILayout.Foldout(isShowItemList, scrollTipstr, curFoldStyle);
            if (isShowItemList)
            {
                if (imgItemList != null && imgItemList.Count > 0)
                {
                    // 创建一个容器，用于控制每行的布局
                    EditorGUILayout.BeginVertical(GUILayout.Width(position.width - 20));
                    if (flag != 3)
                    {
                        GUILayout.Space(8);    // 顶部间隔
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);   // 左边距

                        GUILayout.FlexibleSpace();
                        if (flag == 1 || flag == 2)
                        {
                            EditorGUILayout.BeginHorizontal();

                            if (flag == 1 && GUILayout.Button("修复此列表 全部/选择 的图片", GUILayout.Height(25), GUILayout.Width(500)))
                            {
                                if (!EditorUtility.DisplayDialog("警告", $"此操作会修复这列表的 全部/所选 的原图片是否继续？", "确定", "取消"))
                                {
                                    return;
                                }
                                FixTexture(3);
                            }
                            if (flag == 2 && GUILayout.Button($"修复此列表 全部/选择 的图片", GUILayout.Height(25), GUILayout.Width(500)))
                            {
                                if (!EditorUtility.DisplayDialog("警告", $"此操作会缩小这列表的 全部/所选 的原图片，建议联系美术修复，是否继续？", "确定", "取消"))
                                {
                                    return;
                                }
                                FixTexture(4);
                            }
                            isAddToGitStorageCacheOfEnlarge = EditorGUILayout.Toggle(isAddToGitStorageCacheOfEnlarge, GUILayout.Width(14));
                            EditorGUILayout.LabelField("修复完添加到Git暂存区", GUILayout.Width(130));
                            EditorGUILayout.EndHorizontal();
                        }
                        if (flag == 4 && GUILayout.Button($"导出txt记录此列表图片路径", GUILayout.Height(25), GUILayout.Width(500)))
                        {
                            WriteTxtRecordCannotFixTexture();
                        }
                        GUILayout.FlexibleSpace();

                        GUILayout.Space(20);   // 右边距
                        EditorGUILayout.EndHorizontal();
                    }
                    // 滚动区域 TODO:取消水平滑动条
                    float scrollViewWidth = position.width - 20;
                    int curHeight = imgItemList.Count != 1 ? itemHeight : itemHeight + 30;
                    float scrollViewHeight = Mathf.Min(imgItemList.Count * (curHeight + EditorGUIUtility.standardVerticalSpacing), 220);
                    // 第二个参数是水平滚动条，第三个参数是垂直滚动条
                    positionVec2 = EditorGUILayout.BeginScrollView(positionVec2, false, false, GUILayout.Width(scrollViewWidth), GUILayout.Height(scrollViewHeight), GUILayout.ExpandWidth(false));
                    EditorGUILayout.BeginVertical(GUILayout.Width(scrollViewWidth - 35));
                    GUILayout.Space(10);    // 顶部间隔
                    for (int i = 0; i < imgItemList.Count; i++)
                    {
                        // 创建一行控件，并设置水平方向布局方式
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(15);   // 左边距

                        ImageItem it = imgItemList[i];
                        if (it.relativeTex == null)
                        {
                            it.relativeTex = AssetDatabase.LoadAssetAtPath<Texture2D>(it.relativePath);
                        }
                        if (flag == 1 || flag == 2)
                        {
                            GUI.enabled = it.isAlpha;// 有alpha通道才
                            it.isSelect = EditorGUILayout.Toggle(it.isSelect, GUILayout.Width(14));
                            GUI.enabled = true;
                        }

                        EditorGUILayout.ObjectField(it.relativeTex, typeof(Texture2D), true, GUILayout.ExpandWidth(true));
                        if (it.relativeTex != null)
                        {
                            GUILayout.Label(string.Format("{0} * {1}  ", flag == 3 ? it.fixWidth : it.width, flag == 3 ? it.fixHeight : it.height), GUILayout.Width(100));
                        }
                        if (flag == 1 || flag == 2)
                        {
                            if (it.isAlpha && GUILayout.Button("修复", GUILayout.Width(50)))
                            {
                                FixTexture(flag, i, it.relativePath);
                            }
                        }
                        GUILayout.Space(20);   // 右边距
                        EditorGUILayout.EndHorizontal();
                        GUILayout.Space(EditorGUIUtility.standardVerticalSpacing);  // 控件间的竖直间距
                    }
                    EditorGUILayout.EndVertical();
                    EditorGUILayout.EndScrollView();
                    EditorGUILayout.EndVertical();
                }
            }
            EditorGUILayout.Space(10);
        }
        private void WriteTxtRecordCannotFixTexture()
        {
            WriteTxtRecord(genTxtPathStr, CANNOTFIXIMGLISTTXTPATHSTR, cannotFixedImgItemList);
        }
        private void WriteTxtRecordMaxSizeSmallTexture()
        {
            WriteTxtRecord(genTxtPathStr, MAXSIZESMALLIMGLISTTXTPATHSTR, maxSizeSmallImgItemList);
        }
        private void WriteTxtRecord(string floder, string filename, List<ImageItem> list)
        {
            string textureListStr = string.Join("\n", list.Select((x) => { return x.relativePath; }));
            if (!Directory.Exists(floder))
            {
                Directory.CreateDirectory(floder);
            }
            string txtPath = $"{floder}/{filename}";
            File.WriteAllText($"{txtPath}", textureListStr);
            string directoryPath = Path.GetDirectoryName($"{txtPath}");
            System.Diagnostics.Process.Start("explorer.exe", directoryPath);
        }
        private Texture2D sharedTexture;
        private byte[] fileData;
        public Texture2D LoadTextureFromFile(string filePath)// 从外部加载
        {
            fileData = System.IO.File.ReadAllBytes(filePath);
            if (sharedTexture == null)
            {
                //Debug.Log("sharedTexture null");
                sharedTexture = new Texture2D(2, 2);
            }
            if (sharedTexture.LoadImage(fileData))
            {
                return sharedTexture;
            }
            return null;
        }

        private Texture2D texture;
        private string[] textureUidArr;
        static string currentProjectPathLine = currentProjectPath + "/";
        const string POINTETCTEXCOMPRESSFORMATINPLATFORMSTR = "ETC";
        const string POINTAUTOTEXCOMPRESSFORMATINPLATFORMSTR = "AutomaticCompressed";// 未设置安卓平台的压缩格式会是自动的，也获取吧
        string[] exceptImgStrArr;
        int count = 0;
        public void GenListEleTextures()
        {
            // 排除指定目录下的图片
            string getExceptImgStr = exceptImagePathStr;
            if (!string.IsNullOrEmpty(getExceptImgStr) && getExceptImgStr.IndexOf(currentProjectPathLine) != -1)
            {
                getExceptImgStr = getExceptImgStr.Replace(currentProjectPathLine, "");// 得到相对路径，因为图片的是相对路径
            }
            // 排除selImagePathStr字符串中的当前目录路径,因为要得到相对路径
            string selTxtPath = selImagePathStr.Replace(currentProjectPathLine, "");
            // 是否根据输入的多个文件夹名称来排除
            if (isDefaultExptImgPath)
            {

#if UNITY_2021_1_OR_NEWER
                // 仅在 Unity 2021 版本及更新版本中执行的代码
                exceptImgStrArr = defaultExceptImgPathStr.Split(";");
#else
            exceptImgStrArr = defaultExceptImgPathStr.Split(';');
#endif
            }
            EditorUtility.DisplayProgressBar("Progress", "Retrieve images in", 0);
            // 获取Texture的所有UUID，并得到图片路径，然后得到Texture
            textureUidArr = AssetDatabase.FindAssets("t:Texture", new string[] { selTxtPath });
            foreach (var textureUid in textureUidArr)
            {
                var textureRelativePath = AssetDatabase.GUIDToAssetPath(textureUid);
                if (isDefaultExptImgPath)
                {
                    bool isNext = false;
                    foreach (var expStr in exceptImgStrArr)
                    {
                        if (!string.IsNullOrEmpty(expStr) && textureRelativePath.IndexOf(expStr) != -1)// 排除UISpriteAtlas文件夹下的图片
                        {
                            isNext = true;
                            break;
                        }
                    }
                    if (isNext)
                    {
                        continue;
                    }
                }
                else
                {
                    if (!string.IsNullOrEmpty(getExceptImgStr) && textureRelativePath.IndexOf(getExceptImgStr) != -1)// 排除UISpriteAtlas文件夹下的图片
                    {
                        continue;
                    }
                }
                // 获取文件名
                if (textureRelativePath.IndexOf(".png") != -1 || textureRelativePath.IndexOf(".tga") != -1 ||
                    textureRelativePath.IndexOf(".jpg") != -1)
                {
                    texture = AssetDatabase.LoadAssetAtPath(textureRelativePath, typeof(Texture2D)) as Texture2D;
                    if (texture == null && textureRelativePath.IndexOf(".tga") != -1)
                    {
                        Debug.LogError($"LoadAssetAtPath加载{textureRelativePath}失败，切换绝对路径加载");
                        texture = LoadTextureFromFile(Path.GetFullPath(textureRelativePath));
                    }
                    if (texture == null)
                    {
                        Debug.LogError($"Failed to load image from file: {textureRelativePath}");
                        continue;
                    }
                    count++;// 计数
                            // 宽高不匹配
                    if (texture.width % 4.0 != 0 || texture.height % 4.0 != 0)
                    {
                        //Debug.Log($"GenListEleTextures has alpha:{oldTexture.format.ToString()} ->{textureRelativePath} ->{oldTexture.width} {oldTexture.height}");
                        TextureImporter importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture)) as TextureImporter;
                        TextureImporterPlatformSettings androidSettings = importer.GetPlatformTextureSettings("Android");
                        if (androidSettings == null)
                        {
                            continue;
                        }
                        // 1.排除格式不正确的。
                        if (IsNeedExceptThisTexByCheckFormat(importer, androidSettings))
                        {
                            continue;
                        }
                        // 2.判断 texture是否有a通道
                        bool isAlpha = importer.DoesSourceTextureHaveAlpha();
                        if (isAlpha)
                        {
                            // tga无法修复的
                            if (textureRelativePath.IndexOf(".tga") != -1)
                            {
                                cannotFixedImgItemList.Add(new ImageItem(texture.width, texture.height, textureRelativePath, texture, true));
                                continue;
                            }
                            int androidMaxSize = -1, swidth = -1, sheight = -1;
                            // 判断安卓平台设置的maxsize是否小于源大小
                            bool isMLSS = CheckMaxSizeIsLessSourceImgSize(importer, androidSettings, ref androidMaxSize, ref swidth, ref sheight);
                            if (isMLSS)
                            {
                                maxSizeSmallImgItemList.Add(new ImageItem(texture.width, texture.height, textureRelativePath, texture, isAlpha, androidMaxSize, swidth, sheight));
                            }
                            else
                            {
                                noMatchImgItemList.Add(new ImageItem(texture.width, texture.height, textureRelativePath, texture, isAlpha, -1));
                            }
                        }
                        else
                        {
                            //Debug.Log($"纹理{textureRelativePath}不具有 Alpha 通道");
                            cannotFixedImgItemList.Add(new ImageItem(texture.width, texture.height, textureRelativePath, texture, isAlpha));
                        }
                    }
                }
                // 释放资源
                if (count % 50 == 0)
                {
                    EditorUtility.UnloadUnusedAssetsImmediate();
                }
                EditorUtility.DisplayProgressBar("Retrieve images in", textureRelativePath, count / (float)textureUidArr.Length);
            }
            EditorUtility.ClearProgressBar();
            if (noMatchImgItemList.Count > 0 || maxSizeSmallImgItemList.Count > 0)
            {
                noMatchShowListBool = true;
                maxSizeSmallShowListBool = true;
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"当前选择文件夹下没有需要修复的图片", "确定");
            }
        }
        // 检查格式是否不符合需求，需要排除的。逻辑有点小复杂
        private bool IsNeedExceptThisTexByCheckFormat(TextureImporter importer, TextureImporterPlatformSettings androidSettings)
        {
            string texComFormatInAd = androidSettings.format.ToString();
            var compression = androidSettings.overridden ? androidSettings.textureCompression.ToString() : importer.textureCompression.ToString();
            if (androidSettings.overridden) // 安卓平台设置
            {
                if (texComFormatInAd.IndexOf(POINTETCTEXCOMPRESSFORMATINPLATFORMSTR) == -1 || compression.IndexOf("Un") != -1)// 不是ETC，没有压缩质量就退出
                {
                    return true;
                }
            }
            else
            {
                // 没有安卓平台设置
                if (isNoExceptAutoCompress)// 不排除自动压缩的，就要检测是否符合规则
                {
                    if (texComFormatInAd.IndexOf(POINTAUTOTEXCOMPRESSFORMATINPLATFORMSTR) == -1 || compression.IndexOf("Un") != -1)// 不是自动压缩，没有压缩质量就退出
                    {
                        //Debug.Log($"{textureRelativePath} {texComFormatInAd} {compression}");
                        return true;
                    }
                }
                else
                {
                    // 要排除自动压缩的，就直接退出
                    return true;
                }
            }
            return false;
        }
        private bool CheckMaxSizeIsLessSourceImgSize(TextureImporter importer, TextureImporterPlatformSettings androidSettings, ref int androidMaxSize, ref int swidth, ref int sheight)
        {
            if (importer != null)
            {

#if UNITY_2021_1_OR_NEWER
                // 仅在 Unity 2021 版本及更新版本中执行的代码
                // 获取源大小
                importer.GetSourceTextureWidthAndHeight(out swidth, out sheight);
#else
            // 在更低版本的 Unity 中执行的代码，用反射
            object[] args = new object[2] { 0, 0 };
            MethodInfo mi = typeof(TextureImporter).GetMethod("GetWidthAndHeight", BindingFlags.NonPublic | BindingFlags.Instance);
            mi.Invoke(importer, args);

            swidth = (int)args[0];
            sheight = (int)args[1];
#endif
                // 获取安卓平台的纹理设置信息
                if (swidth == -1 || sheight == -1)
                {
                    return false;
                }
                //Debug.Log($"{importer.assetPath} {androidSettings.maxTextureSize}==");
                androidMaxSize = androidSettings.maxTextureSize;
                return androidSettings.maxTextureSize < Math.Max(swidth, sheight);
            }
            return false;
        }

        //////////////////////////////////////////////////////////////////////////////////////////
        // 修复图片函数的代码段//////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////
        private void BeforeFixTexture()// 修复前的准备工作
        {
            selecAndFixedList = selecAndFixedList == null ? new List<ImageItem>() : selecAndFixedList;
            selecAndFixedList.Clear();
        }
        // TODO:根据flag的int值来绘制不同的控件，不太好的方式，应该用enum
        private void FixTexture(int tag = 3, int idx = -1, string texRelativePath = null)// 修复中
        {
            BeforeFixTexture();
            if (tag == 1)
            {
                if (idx == -1)
                {
                    EditorUtility.DisplayDialog("提示", "修复错误，请重试", "确定");
                    return;
                }
                FixTextureEnlargeOneByNativeCode(idx, texRelativePath);
            }
            else if (tag == 2)
            {
                // 缩小
                if (idx == -1)
                {
                    EditorUtility.DisplayDialog("提示", "修复错误，请重试", "确定");
                    return;
                }
                if (!EditorUtility.DisplayDialog("警告", $"此图片将会等比缩小至{maxSizeSmallImgItemList[idx].shrinkWidth} * {maxSizeSmallImgItemList[idx].shrinkHeight}，建议联系美术修复，是否继续？", "确定", "取消"))
                {
                    return;
                }
                FixTextureShrinkOneByNativeCode(idx, texRelativePath);
            }
            else if (tag == 3)
            {
                if (noMatchImgItemList.Count <= 0)
                {
                    EditorUtility.DisplayDialog("提示", "此列表没有需要修复的图片", "确定");
                    return;
                }
                FixTextureEnlargeAllByNativeCode();
            }
            else if (tag == 4)
            {
                if (maxSizeSmallImgItemList.Count <= 0)
                {
                    EditorUtility.DisplayDialog("提示", "此列表没有需要修复的图片", "确定");
                    return;
                }
                FixTextureShrinkAllByNativeCode();
            }
            // 先刷新，再执行判断是否内部大小也修改成功函数
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            AfterFixTextureSet();
            // 添加到Git暂存区
            if (isAddToGitStorageCacheOfEnlarge)
            {
                BeforeAddFixTextureToGitStorage();
            }
        }

        /*
            修复完外部图片的宽高、Unity内部自动刷新压缩大小后，再次检查宽高是否压缩到合适的宽高
            是：修复成功，否：添加到修复失败列表中。
            TODO:可能要做个修复失败就恢复原图（很麻烦）。
        */
        List<int> cannotFixIdx = new List<int>();
        private void AfterFixTextureSet()// 修复后的处理工作
        {
            //Debug.Log("AfterFixTextureSet()");
            cannotFixIdx = cannotFixIdx == null ? new List<int>() : cannotFixIdx;
            cannotFixIdx.Clear();
            for (int i = 0; i < fixedImgItemList.Count; i++)
            {
                ImageItem item = fixedImgItemList[i];
                // 由于之前刷新了资源，所以此时item.tex是最新的texture2d
                item.fixWidth = item.relativeTex.width;
                item.fixHeight = item.relativeTex.height;
                //Debug.Log($"!{item.relativeTex.width} {item.relativeTex.height} {fixedImgItemList[i].relativeTex.width} {fixedImgItemList[i].relativeTex.height}");
                // 即使修复了原图片，压缩图片依旧不符合4的倍数，说明修复失败
                if (item.relativeTex.width % 4.0 != 0 || item.relativeTex.height % 4.0 != 0)
                {
                    Debug.LogError($"{item.relativePath} 无法修复");
                    cannotFixIdx.Add(i);
                }
            }
            for (int i = 0; i < cannotFixIdx.Count; i++)
            {
                ImageItem it = fixedImgItemList[i];
                cannotFixedImgItemList.Add(it);
                fixedImgItemList.Remove(it);
            }
            fixedShowListBool = true;
            cannotFixShowListBool = cannotFixIdx.Count > 0;
        }
        private void SetSelecAndFixedList(List<ImageItem> sellist)
        {
            int selCt = sellist.Count((x) => { return x.isSelect; });

            // selCt>0说明就修复几个选中的，<=0 要全部修复
            if (selCt > 0)
            {
                foreach (var item in sellist)
                {
                    if (item.isSelect)
                    {
                        selecAndFixedList.Add(item);
                    }
                }
            }
            else
            {
                selecAndFixedList = sellist;
            }
        }
        private void FixTextureEnlargeOneByNativeCode(int idx, string texRelativePath)
        {
            bool isSuc = FixTextureEnlargeByUnityEngineClass(Path.GetFullPath(texRelativePath));
            FixTextureOneCommonnFunc(noMatchImgItemList, idx, isSuc);
        }
        // 修复maxSizeSmallerList
        private void FixTextureShrinkOneByNativeCode(int idx, string texRelativePath)
        {
            bool isSuc = FixTextureShrinkByUnityEngineClass(Path.GetFullPath(texRelativePath), maxSizeSmallImgItemList[idx].androidMaxSize);
            // 刷新列表
            FixTextureOneCommonnFunc(maxSizeSmallImgItemList, idx, isSuc, 1);
        }
        private void FixTextureOneCommonnFunc(List<ImageItem> list, int idx, bool isSuc, int flag = -1)
        {
            if (list != null && list.Count > 0 && isSuc)
            {
                fixedImgItemList.Add(list[idx]);
                list.Remove(list[idx]);
                cannotFixShowListBool = false;
                fixedShowListBool = true;
                if (flag != -1)
                {
                    EditorUtility.DisplayDialog("提示", $"此图片修复成功", "确定");
                }
            }
            else
            {
                cannotFixedImgItemList.Add(list[idx]);
                list.Remove(list[idx]);
                cannotFixShowListBool = true;
                fixedShowListBool = false;
                EditorUtility.DisplayDialog("提示", $"此图片修复失败", "确定");
            }
        }
        private void FixTextureEnlargeAllByNativeCode()
        {
            SetSelecAndFixedList(noMatchImgItemList);
            int fixCount = 0;
            foreach (var item in selecAndFixedList)
            {
                if (FixTextureEnlargeByUnityEngineClass(Path.GetFullPath(item.relativePath)))
                {
                    fixCount++;
                    fixedImgItemList.Add(item);
                }
                else
                {
                    // 修改失败的图片
                    Debug.Log($"{item.relativePath}图片修复失败");
                    cannotFixedImgItemList.Add(item);
                    item.isFixSuc = false;
                }
            }
            EditorUtility.DisplayDialog("提示", $"共修复图片 {fixCount}/{selecAndFixedList.Count} ", "确定");
            // 全部修复完成
            if (fixCount >= selecAndFixedList.Count)
            {
                noMatchImgItemList = noMatchImgItemList.Except(selecAndFixedList).ToList();
            }
            else
            {
                // 部分修复完成
                noMatchImgItemList = noMatchImgItemList.Except(fixedImgItemList).ToList();
                noMatchImgItemList = noMatchImgItemList.Except(cannotFixedImgItemList).ToList();
            }
            cannotFixShowListBool = fixCount < selecAndFixedList.Count;// 修复的项小于选择的项，则要打开未修复列表
        }

        private void FixTextureShrinkAllByNativeCode()
        {
            SetSelecAndFixedList(maxSizeSmallImgItemList);
            int fixCount = 0;
            // 修复成功的去成功的列表，失败的去失败的列表
            foreach (var item in selecAndFixedList)
            {
                if (FixTextureShrinkByUnityEngineClass(Path.GetFullPath(item.relativePath), item.androidMaxSize))// 注意：不是调用FixTextureEnlargeByNativeCode函数，这里是缩放
                {
                    fixCount++;
                    fixedImgItemList.Add(item);
                }
                else
                {
                    // 修改失败的图片
                    Debug.Log($"{item.relativePath}图片修复失败");
                    cannotFixedImgItemList.Add(item);
                    item.isFixSuc = false;
                }
            }
            EditorUtility.DisplayDialog("提示", $"共缩放修复图片 {fixCount}/{selecAndFixedList.Count} ", "确定");
            // 全部修复完成
            if (fixCount >= selecAndFixedList.Count)
            {
                maxSizeSmallImgItemList = maxSizeSmallImgItemList.Except(selecAndFixedList).ToList();
            }
            else
            {
                // 部分修复完成
                maxSizeSmallImgItemList = maxSizeSmallImgItemList.Except(fixedImgItemList).ToList();
                maxSizeSmallImgItemList = maxSizeSmallImgItemList.Except(cannotFixedImgItemList).ToList();
            }
            cannotFixShowListBool = fixCount < selecAndFixedList.Count;// 修复的项小于选择的项，则要打开未修复列表
        }

        // UnityEngine的原生类来修复
        /*
            放大图片为4的倍数
            0.创建Texture2D旧图片对象，并加载图片数据
            1.计算放大后的图片大小和偏移位置
            2.根据放大后的图片大小新创建Texture2D新图片对象
            3.给新图片对象填充空白像素
            4.给新图片对象填充旧图片像素，不过有偏移
            5.将新图片对象数据输出到空文件里
        */
        private bool FixTextureEnlargeByUnityEngineClass(string imgPath)
        {
            try
            {
                // 从文件加载原始纹理
                byte[] oldImageData = File.ReadAllBytes(imgPath);
                Texture2D oldTexture = new Texture2D(2, 2);
                oldTexture.LoadImage(oldImageData);

                int oldWidth = oldTexture.width;
                int oldHeight = oldTexture.height;

                // 计算放大后的图片大小
                int newWidth = 4 * Mathf.CeilToInt(oldWidth / 4f);
                int newHeight = 4 * Mathf.CeilToInt(oldHeight / 4f);
                Texture2D newTexture = new Texture2D(newWidth, newHeight, TextureFormat.RGBA32, false);
                newTexture.SetPixels32(new Color32[newWidth * newHeight]);// 填充空白像素

                // 偏移位置
                int deltaW = newWidth - oldWidth;
                int deltaH = newHeight - oldHeight;
                int paddingX = deltaW / 2;
                int paddingY = deltaH / 2;

                newTexture.SetPixels(paddingX, paddingY, oldWidth, oldHeight, oldTexture.GetPixels());// 再偏移位置填充旧图片的像素到新图片中
                newTexture.Apply();

                // 保存纹理数据到文件
                byte[] newImageData = null;
                if (imgPath.IndexOf(".jpg") != -1)// 判断类型，输出到新文件
                {
                    newImageData = newTexture.EncodeToJPG();
                }
                else
                {
                    newImageData = newTexture.EncodeToPNG();// 默认为png
                }
                File.WriteAllBytes(imgPath, newImageData);

                Debug.Log(imgPath + " fix success! Resize to " + newWidth + "x" + newHeight);
                // 释放资源
                DestroyImmediate(oldTexture);
                DestroyImmediate(newTexture);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        /*
            等比缩放图片
            0.创建Texture2D旧图片对象，并加载图片数据
            1.先得到最小缩放率，用这个缩放率乘以旧图片宽高，得到新空图片的大小（由于缩放率<1，所以是缩小），且是4倍数的数值
            2.新图片大小作为参数得到Texture2D新图片对象、RenderTexture对象
            3.设置RenderTexture对象的模式，再将旧图片对象渲染到RenderTexture对象中，新图片对象从RenderTexture对象中读取数据
            4.将新图片对象数据输出到空文件里
        */
        private bool FixTextureShrinkByUnityEngineClass(string imgPath, int androidMaxSize) // 缩小图片为4的倍数
        {
            if (androidMaxSize == -1)
            {
                EditorUtility.DisplayDialog("提示", $"此图片修复失败", "确定");
                Debug.Log($"{imgPath} 此图片缩放失败 aMaxSize:{androidMaxSize}");
                return false;
            }
            try
            {
                // 从文件加载原始纹理
                Texture2D oldTexture = new Texture2D(2, 2);
                byte[] oldImageData = File.ReadAllBytes(imgPath);
                oldTexture.LoadImage(oldImageData);

                // 计算缩小后的图片大小和位置
                int newSizeWidth = oldTexture.width;
                int newSizeHeight = oldTexture.height;
                CalculateNewImgShrinkSize(oldTexture.width, oldTexture.height, androidMaxSize, out newSizeWidth, out newSizeHeight);

                // 创建新的缩小后的纹理
                Texture2D newTexture = new Texture2D(newSizeWidth, newSizeHeight);
                RenderTexture rt = RenderTexture.GetTemporary(newSizeWidth, newSizeHeight);
                rt.filterMode = FilterMode.Bilinear;

                // 将原始纹理渲染到缩小后的纹理
                UnityEngine.Graphics.Blit(oldTexture, rt);
                RenderTexture.active = rt;
                newTexture.ReadPixels(new Rect(0, 0, newSizeWidth, newSizeHeight), 0, 0);
                newTexture.Apply();

                // 保存缩小后的纹理到文件
                byte[] newImageData = null;
                if (imgPath.IndexOf(".jpg") != -1)// 判断类型，输出到新文件
                {
                    newImageData = newTexture.EncodeToJPG();
                }
                else
                {
                    newImageData = newTexture.EncodeToPNG();// 默认为png
                }
                File.WriteAllBytes(imgPath, newImageData);

                // 释放资源
                RenderTexture.ReleaseTemporary(rt);
                DestroyImmediate(oldTexture);
                DestroyImmediate(newTexture);

                Debug.Log($"{imgPath} shrink success! shrink to {newSizeWidth}x{newSizeHeight}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                return false;
            }
        }
        private static void CalculateNewImgShrinkSize(int imgWidth, int imgHeight, int androidMaxSize, out int newImgWidth, out int newImgHeight)
        {
            float widthRatio = (float)androidMaxSize / imgWidth;
            float heightRatio = (float)androidMaxSize / imgHeight;
            float ratio = Math.Min(widthRatio, heightRatio);
            int newWidth = (int)(imgWidth * ratio);
            int newHeight = (int)(imgHeight * ratio);
            newImgWidth = 4 * (int)Math.Floor(newWidth / 4f); // 调整宽度为 4 的倍数
            newImgHeight = 4 * (int)Math.Floor(newHeight / 4f); // 调整高度为 4 的倍数
        }
        // 新需求，添加到Git暂存区
        private void BeforeAddFixTextureToGitStorage()
        {
            if (!isGetGitExePathOnce)
            {
                gitExePath = GetGitExePath(); // 获取git exe的地址
                isGetGitExePathOnce = true;
            }
            if (string.IsNullOrEmpty(gitExePath))
            {
                return;
            }
            // 从fixlist中获取要添加到git暂存区的项，但是要排除在暂存区的项
            addGitStorageImgItemList = fixedImgItemList.Except(addGitStorageImgItemList).ToList();
            if (!string.IsNullOrEmpty(gitResPath))
            {
                AddFixTextureToGitStorage();
            }
            else
            {
                EditorUtility.DisplayDialog("提示", $"当前项目{currentProjectPath}下及父目录两级都未找到.git仓库，添加失败", "确定");
            }
        }
        private void AddFixTextureToGitStorage()
        {
            using (var process = new System.Diagnostics.Process())
            {
                process.StartInfo.FileName = gitExePath;
                process.StartInfo.WorkingDirectory = gitResPath;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;

                foreach (var item in addGitStorageImgItemList)
                {
                    process.StartInfo.Arguments = $"add {Path.GetFullPath(item.relativePath)}";
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();

                    process.WaitForExit();

                    if (!string.IsNullOrEmpty(output))
                    {
                        Debug.Log($"Output: {output}");
                    }

                    if (!string.IsNullOrEmpty(error))
                    {
                        Debug.Log($"Error: {error}");
                    }
                }
            }
        }
        // 获取Git路径
        private string GetGitExePath()
        {
            // 获取操作系统类型
            bool isWindows = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows);
            bool isMacOS = System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX);

            string gitCommand = "which git";  // MAC 系统使用 which 命令查找 git 工具路径

            if (isWindows)
            {
                gitCommand = "where git";  // Windows 系统使用 where 命令查找 git 工具路径
            }

            System.Diagnostics.ProcessStartInfo processInfo = new System.Diagnostics.ProcessStartInfo();
            processInfo.UseShellExecute = false;
            processInfo.RedirectStandardOutput = true;
            processInfo.FileName = "cmd.exe";

            if (isMacOS)
            {
                processInfo.FileName = "/bin/bash";  // 在 MAC 系统中，需要使用 bash 命令来执行 which 命令
                processInfo.Arguments = "-c \"" + gitCommand + "\"";
            }
            else
            {
                processInfo.Arguments = "/C " + gitCommand;
            }

            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo = processInfo;
            process.Start();

            string gitPath = process.StandardOutput.ReadLine().Trim();
            process.WaitForExit();
            process.Close();
            if (string.IsNullOrEmpty(gitPath))
            {
                EditorUtility.DisplayDialog("提示", "当前电脑未检测到Git.exe程序，请重试", "确定");
            }
            return gitPath;
        }
    }
}