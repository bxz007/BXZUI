using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;


namespace Gala.FrameworkEditorTools
{
    public partial class AllTools : EditorWindow
    {
        private const string EDITOR_ALLTOOL_PREFS_KEY = "EDITOR_ALLTOOL_PREFS_KEY";
        private const int EDITOR_ALLTOOL_LIKE_MAX_NUM = 10;
        Vector2 _scrollPosition = new Vector2(0, 0);

        TextEditor _te = new TextEditor();

        enum EnumTab
        {
            Like,
            UI,
            Build,
            TA
        }
        GUIContent[] GUIArr;

        EnumTab _Tab = EnumTab.UI;
        static private Vector2 _winSize = new Vector2(800, 750);
        GUIStyle _tabBtnStyle;
        GUIStyle _tabBtnOnStyle;

        GUIStyle _helpBtnStyle;
        GUIStyle _textStyle;
        GUIStyle _labStyle;
        GUIStyle _tabTextSytel;
        GUIStyle _tabStyle;
        GUIStyle _iconNameStyle;
        GUIStyle _saveBtnStyle;
        GUIStyle _toggleStyle;
        GUIStyle _titleStyle;

        enum EnumAllToolsUIType
        {
            Develop = 0,
            Language = 1,
            Scene = 2,
            Format = 3,
            Font = 4,
            Texture = 5,
            Log = 6,
            Other = 7
        }

        enum EnumAllToolsTAType
        {
            Create = 0,
            Mesh = 1,
            Texture = 2,
            Animator = 3,
            RepertSource = 4,
            VariantCollection = 5,
            Package = 6,
            Other = 7
        }

        enum EnumAllToolsBuildType
        {
            Zentao = 0, //禅道
            Build = 1
        }

        class AllToolsItem
        {
            public bool isTag; // 是否是标题
            public string name; // 显示的名称
            public string helpInfo; // 展示的帮助内容
            public string wikiUrl;//详细描述
            public Action func; // 回调函数
            public int clickTimes; // 点击次数

            public AllToolsItem(bool lIsTag, string lName, Action lFunc, string lHelpInfo = "", string url = "")
            {
                isTag = lIsTag;
                name = lName;
                helpInfo = lHelpInfo;
                wikiUrl = url;
                func = lFunc;
            }
        }

        private List<AllToolsItem> _likeItems = new List<AllToolsItem>();

        private List<bool> _uiStates = new List<bool> { true, true, true, true, true, true, true, true, true };
        // ui标签下所有功能
        private Dictionary<int, List<AllToolsItem>> _uiNameDict = new Dictionary<int, List<AllToolsItem>>
    {
        {(int)EnumAllToolsUIType.Develop,new List<AllToolsItem>{
            new AllToolsItem(true,"开发工具",null),
            new AllToolsItem(false,"查找预制体引用关系",SearchPrefabReferenceEditorWindow.SearchRefrence),
            }},
        {(int)EnumAllToolsUIType.Language,new List<AllToolsItem>{
            new AllToolsItem(true,"多语言工具",null),
            }},
        {(int)EnumAllToolsUIType.Scene,new List<AllToolsItem>{
            new AllToolsItem(true,"场景工具",null),
            new AllToolsItem(false,"查找场景上所有cs引用",MyScriptsManagerEditor.Init,"点击查找当前场景引用的所有cs文件"),
            }},
        {(int)EnumAllToolsUIType.Format,new List<AllToolsItem>{
            new AllToolsItem(true,"规范以及纠错工具",null),
            new AllToolsItem(false,"查找Image等于none以及无效RaycastTarget",FindNone_RaycastTarget.Init,"查找prefab里面Image组件等于source == none还有查找无效RaycastTarget"),
            new AllToolsItem(false,"Image等于none替换",SetImageNullToCommon.Open,"设置prefab里面Image组件等于source == none以及替换为common的white图片，并设置alpha==0","https://qo11152cxm.feishu.cn/docx/LG7pd220PoI52mxBMpHc3qZ2n5f"),
            new AllToolsItem(false,"禁用无效RaycastTarget",EnabledRaycastTarget.Open,"禁用无效RaycastTarget，减少EventSystem.Update()函数时长","https://qo11152cxm.feishu.cn/docx/LG7pd220PoI52mxBMpHc3qZ2n5f"),
            new AllToolsItem(false,"Cleanup Missing Scripts",CleanUpMissingScripts.Open,"清理预制体上空引用脚本Missing Scripts"),
            new AllToolsItem(false,"Prefabs大小越界",FindPrefabSizeOverflow.Open,"Prefab的大小有点多，点击按钮同时打开wiki文档","https://qo11152cxm.feishu.cn/docx/TB00dHZaFog2n5xxgACcR5wxn8g"),
            new AllToolsItem(false,"预制体检测",PrefabFormatWindow.OpenPrefabWindow),
            new AllToolsItem(false,"单图检测",SingleSpriteFormatWindow.OpenSpriteWindow),
            new AllToolsItem(false,"代码检测",UICSFormatWindow.OpenCSWindow)
            }},

        {(int)EnumAllToolsUIType.Font,new List<AllToolsItem>{
            new AllToolsItem(true,"字体",null),
            new AllToolsItem(false,"字体替换工具",ChangeFontSize.Open,"转换当前字体到目标字体"),
            new AllToolsItem(false,"纯字体替换",ChangeFontWindow.Open,"转换当前字体到目标字体"),
            new AllToolsItem(false,"罗列所有Prefab的Text",GetPrefabTextForFont.Open,"一次性把一个Prefab里面的Text全部排列出来"),
            }},
        {(int)EnumAllToolsUIType.Texture,new List<AllToolsItem>{
            new AllToolsItem(true,"图片",null),
            new AllToolsItem(false,"手动选择目录转化图片格式1",StickersFormat.Open),
            new AllToolsItem(false,"手动选择目录转化图片格式2",ChangeTexeureSettingWindow.OpenChangeTexureSettingsWindow),
            new AllToolsItem(false,"定制化刷新图片格式",ImageAutoChangeGUIWin.OpenImgInfoGUIWin,"同级目录下有一个视频可以观看","https://alidocs.dingtalk.com/i/nodes/N7dx2rn0JbZNloayT135vnnjJMGjLRb3"),
            new AllToolsItem(false,"查询单张图片引用关系",SearchRefrenceEditorWindow.SearchRefrence,"查询单张图片被哪些prefab引用了"),
            new AllToolsItem(false,"查询单张图集引用关系",SingleSearchRefrenceEditorWindow.Init,"测试单张图集被那些prefab引用了，看看有没有被其他模块使用"),
            new AllToolsItem(false,"查询指定大小图片",FindBigImg.Open),
            new AllToolsItem(false,"图集检测",UIAtlasFormatWindow.OpenAtlasWindow),
            new AllToolsItem(false,"修复ETC图片宽高为4的倍数",EditorTextureOfEtc.OpenWindow, "演示视频","https://alidocs.dingtalk.com/i/nodes/NZQYprEoWoebpOPjuee63kpPJ1waOeDk"),
            }},
        {(int)EnumAllToolsUIType.Log,new List<AllToolsItem>{
            new AllToolsItem(true,"日志",null),
            }},
        {(int)EnumAllToolsUIType.Other,new List<AllToolsItem>{
            new AllToolsItem(true,"其他工具",null),
            new AllToolsItem(false,"打开PersistentDataPath",UsefullUrl.OpenPersistentDataPath),
            new AllToolsItem(false,"ClearPlayerPrefs",UsefullUrl.ClearPlayerPrefs,"清理Player本地缓存"),
            new AllToolsItem(false,"ClearEditorPrefs",UsefullUrl.ClearEditorPrefs,"清理Player本地缓存"),
            new AllToolsItem(false,"清理空文件夹",AltProg.CleanEmptyDir.MainWindow.ShowWindow,"清理游戏里面空的文件夹"),
            }}
    };

        private List<bool> _buildStates = new List<bool> { true, true, true, true, true, true, true, true, true };
        // ta标签下所有功能
        private Dictionary<int, List<AllToolsItem>> _buildNameDict = new Dictionary<int, List<AllToolsItem>>
    {
        {(int)EnumAllToolsBuildType.Zentao,new List<AllToolsItem>{
            new AllToolsItem(true,"打包工具",null),
        }},
        {(int)EnumAllToolsBuildType.Build,new List<AllToolsItem>{
            new AllToolsItem(true,"其他工具",null),
            new AllToolsItem(false,"复制文件夹并且复制依赖关系",CopFilesWithDependency.CopFiles,"选中Assets文件夹下某目录，点击这个按钮，就可以复制一个文件夹内的资源，但是依赖关系需要重置，不是继续依赖被复制的文件夹内的图集等","https://qo11152cxm.feishu.cn/docx/YjAsdvrfxoGGqCxS3ZYcvk4Xn7g"),
            new AllToolsItem(false,"打开禅道",UsefullUrl.OpenZenTao),
            new AllToolsItem(false,"打开禅道我的Bug",UsefullUrl.OpenZenTaoMyBug),
        }}

    };

        private List<bool> _taStates = new List<bool> { true, true, true, true, true, true, true, true, true };
        // ta标签下所有功能
        private Dictionary<int, List<AllToolsItem>> _taNameDict = new Dictionary<int, List<AllToolsItem>>
    {
        {(int)EnumAllToolsTAType.Create,new List<AllToolsItem>{
            new AllToolsItem(true,"创建工具",null),
            /* new AllToolsItem(false,"AudienceRenderSHDataAssetEditor",AudienceRenderSHDataAssetEditor.Create),
            new AllToolsItem(false,"AudienceFloorSliceFeatureAsset",AudienceStadiumFloorInfoEditor.Create),
            new AllToolsItem(false,"AudienceRenderSHDataAsset",AudienceRenderSHDataAsset.Create),
            new AllToolsItem(false,"Create Material Quality Assets",QualityMaterialCreator.CreateQualityMaterial),
            new AllToolsItem(false,"Create Material Quality Shader Variant",QualityMaterialCreator.CreateMaterialShaderVariants),
            new AllToolsItem(false,"Direct Renderer",GalaDirectRendererData.CreateForwardRendererData),
            new AllToolsItem(false,"Forward Renderer",GalaForwardRendererData.CreateForwardRendererData),
            new AllToolsItem(false,"CreateGamePlayer",GamePlayerBoneEditor.CreatePlayer), */
        }},
        {(int)EnumAllToolsTAType.Mesh,new List<AllToolsItem>{
            new AllToolsItem(true,"Mesh工具",null),
            /* new AllToolsItem(false,"Compress Mesh",MeshCompressionEditor.CompressMesh),
            new AllToolsItem(false,"Compress Mesh for Prefab",MeshCompressionEditor.CompressMeshPrefab),
            new AllToolsItem(false,"Compress Mesh on Directory",MeshCompressionEditor.CompressMeshOnDir),
            new AllToolsItem(false,"Compress Mesh on MeshsDir",MeshCompressionEditor.CompressMeshOnMesh),
            new AllToolsItem(false,"Compress Mesh for Scene",MeshCompressionEditor.CompressMeshOnScene),
            new AllToolsItem(false,"Reset Mesh for Prefab",MeshCompressionEditor.ResetMeshPrefab),
            new AllToolsItem(false,"Compress Mesh for Prefab #&w",MeshCompressionEditor.CompressAllMesh),
            new AllToolsItem(false,"Compress Mesh for Scene #&e",MeshCompressionEditor.CompressMeshForScene),
            new AllToolsItem(false,"ExportMesh",ExportMeshWizard.ExportWizard), */
            }},
        {(int)EnumAllToolsTAType.Texture,new List<AllToolsItem>{
            new AllToolsItem(true,"贴图格式",null),
            /* new AllToolsItem(false,"设置贴图格式(Assets路径)",TextureSettingEditor.ChangeTextureSettings),
            new AllToolsItem(false,"设置贴图格式（选中资源）",TextureSettingEditor.ChangeTextureSettings2),
            new AllToolsItem(false,"DoTextureFormatHandler",TextureFormatHandler.DoTextureFormatHandler),
            new AllToolsItem(false,"PlayerSkin_FormalHeadTextures",CheckTextureFormatEditor.SetTextureFormat_PlayerSkin_FormalHeadTextures),
            new AllToolsItem(false,"SetTextureFormat_StadiumScenes",CheckTextureFormatEditor.SetTextureFormat_StadiumScenes), */
            }},
        {(int)EnumAllToolsTAType.Animator,new List<AllToolsItem>{
            new AllToolsItem(true,"动画",null),
            /* new AllToolsItem(false,"动画压缩工具",AnimCompressTool.Init),
            new AllToolsItem(false,"压缩(3D/Player/1)下所有动画文件",AnimCompressTool.CompressAnim),
            new AllToolsItem(false,"AnimationCurveSetInstant",CameraBoneCurveSet.AnimationCurveSetInstant),
            new AllToolsItem(false,"CreateAnimatorController",AnimatorControllerCreater.CreateAnimatorController), */
            }},
        {(int)EnumAllToolsTAType.RepertSource,new List<AllToolsItem>{
            new AllToolsItem(true,"检测重复资源",null),
            /* new AllToolsItem(false,"CheckLoadResources",CheckRepeatResource.CheckLoadResources),
            new AllToolsItem(false,"Check3D",CheckRepeatResource.Check3D),
            new AllToolsItem(false,"CheckScene",CheckRepeatResource.CheckScene),
            new AllToolsItem(false,"CheckStadiumScenes",CheckRepeatResource.CheckStadiumScenes), */
            }},
        {(int)EnumAllToolsTAType.VariantCollection,new List<AllToolsItem>{
            new AllToolsItem(true,"shader相关",null),
            /* new AllToolsItem(false,"Reimport Shader",ReimportShader.ReimportShaderAction),
            new AllToolsItem(false,"ShaderVariantCollection",ShaderVariantCollectionWindow.OpenWindow,"shader 变体收集"),
            new AllToolsItem(false,"CollectShaderVariant/ALL",VariantCollectionEditor.CollectShaderVariant_ALL),
            new AllToolsItem(false,"CollectShaderVariant/LOW",VariantCollectionEditor.CollectShaderVariant_LOW),
            new AllToolsItem(false,"CollectShaderVariant/MEDIUM",VariantCollectionEditor.CollectShaderVariant_MEDIUM),
            new AllToolsItem(false,"CollectShaderVariant/HIGH",VariantCollectionEditor.CollectShaderVariant_HIGH),
            new AllToolsItem(false,"CollectShaderVariant/Clear_Low",VariantCollectionEditor.Clear_LOW),
            new AllToolsItem(false,"CollectShaderVariant/Clear_MEDIUM",VariantCollectionEditor.Clear_MEDIUM),
            new AllToolsItem(false,"CollectShaderVariant/Clear_HIGH",VariantCollectionEditor.Clear_HIGH), */
            }},
        {(int)EnumAllToolsTAType.Package,new List<AllToolsItem>{
            new AllToolsItem(true,"打包相关",null),
            /* new AllToolsItem(false,"CompressSequenceMaps",CompressSequenceMaps.CompressNormalAndAOSequenceMaps),
            new AllToolsItem(false,"Lightprobe Bake Util",LightProbeAssetBakeUtility.SaveLightprobeToBin),
            new AllToolsItem(false,"Scenes Baker",ScenesBaker.ShowWindow),
            new AllToolsItem(false,"Scene Lighting",SceneslightingBaker.ShowWindow),
            new AllToolsItem(false,"Hair Baked",HairBakerWindow.ShowWindow),
            new AllToolsItem(false,"Material LOD Creator",MaterialLODCreator.AddWindow),
            new AllToolsItem(false,"UpdateRenderPackage",RenderBaseGitHelper.UpdateRenderPackage),
            new AllToolsItem(false,"Character Window",SceneLightsSettingEditorWindow.AddWindow),
            new AllToolsItem(false,"ExportMesh2Obj/Wavefront OBJ",ObjExporter.DoExportWSubmeshes),
            new AllToolsItem(false,"ExportMesh2Obj/Wavefront OBJ (No Submeshes)",ObjExporter.DoExportWOSubmeshes), */
            }},
        {(int)EnumAllToolsTAType.Other,new List<AllToolsItem>{
            new AllToolsItem(true,"其他",null),

            /* new AllToolsItem(false,"SetCompressedConfigFalse",CompressSequenceMaps.SetCompressedConfigFalse),
            new AllToolsItem(false,"Mini Package Build",MiniPackageBuilder.AddWindow),
            new AllToolsItem(false,"B83/UVViewer",UVViewer.Init),
            new AllToolsItem(false,"Lightmap Capture",LightmapCapture.ShowWindow),
            new AllToolsItem(false,"Material Search Window",MaterialSearch.AddWindow),
            new AllToolsItem(false,"Material Trace Window",MaterialTracer.AddWindow), */
            }}

    };
        private Texture2D _helpTexture;

        [MenuItem("PlatformEditorTool/AllTools",priority = 0)]
        public static void AddWindow()
        {
            Rect wr = new Rect(0, 0, 600, 600);
            var editorAsm = typeof(Editor).Assembly;
            var inspectorWindow = editorAsm.GetType("UnityEditor.InspectorWindow");
            AllTools _window = (AllTools)EditorWindow.GetWindow<AllTools>(inspectorWindow);
            _window.titleContent = new GUIContent("通用开发工具");
            _window.minSize = new Vector2(200, 500);
        }

        //------------------------------------------------------
        private void OnGUI()
        {
            Init();
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            //for (int i = 0; i < TAB.Length; ++i)
            for (int i = 0; i < GUIArr.Length; ++i)
            {
                GUILayout.Space(3);

                if (i == (int)_Tab)
                    _tabStyle = _tabBtnOnStyle;
                else
                    _tabStyle = _tabBtnStyle;

                //if (GUILayout.Button(TAB[i], _tabStyle, GUILayout.Width(90)))
                if (i == 0 && _likeItems.Count == 0)
                {
                    continue;
                }
                if (GUILayout.Button(GUIArr[i], _tabStyle, GUILayout.Width(60)))
                {
                    if ((int)_Tab != i)
                    {
                        EditorPrefs.SetInt(EDITOR_ALLTOOL_PREFS_KEY, i);
                    }
                    _Tab = (EnumTab)i;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            switch (_Tab)
            {
                case EnumTab.UI:
                    DrawListEntity(_uiNameDict, _uiStates, EnumTab.UI.ToString());
                    break;
                case EnumTab.Build:
                    DrawListEntity(_buildNameDict, _buildStates, EnumTab.Build.ToString());
                    break;
                case EnumTab.TA:
                    DrawListEntity(_taNameDict, _taStates, EnumTab.TA.ToString(), 200);
                    break;
                case EnumTab.Like:
                    DrawLikeList();
                    break;
            }
        }

        // 常用绘制
        private void DrawLikeList()
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            int hSize = Mathf.Max(1, (int)this.position.width / 170);
            GUILayout.Space(7);
            GUI.contentColor = Color.white;
            GUILayout.BeginHorizontal();
            for (int j = 0, jNum = _likeItems.Count; j < jNum; j++)
            {
                GUILayout.Space((j + 1) % hSize == 1 ? 10 : 5);
                GUI.skin.button.wordWrap = true;
                if (GUILayout.Button(_likeItems[j].name, GUILayout.Width(200), GUILayout.Height(30)))
                {
                    _likeItems[j].clickTimes++;
                    string prefKey = EDITOR_ALLTOOL_PREFS_KEY + _likeItems[j].name;
                    EditorPrefs.SetInt(prefKey, _likeItems[j].clickTimes);
                    _likeItems[j].func.Invoke();
                }
                if (!string.IsNullOrEmpty(_likeItems[j].helpInfo))
                {
                    if (GUILayout.Button(_helpTexture, _helpBtnStyle, GUILayout.Width(15), GUILayout.Height(15)))
                    {
                        EditorUtility.DisplayDialog(_likeItems[j].name, _likeItems[j].helpInfo, "确定");
                    }
                }
                if ((j + 1) % hSize == 0 && j != jNum)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.EndScrollView();
        }

        // 绘制实体
        private void DrawListEntity(Dictionary<int, List<AllToolsItem>> nameDict, List<bool> _stateList, string key, int width = 180)
        {
            _scrollPosition = GUILayout.BeginScrollView(_scrollPosition);
            int hSize = Mathf.Max(1, (int)this.position.width / 170);
            for (int i = 0, num = nameDict.Count; i < num; i++)
            {
                GUILayout.Space(7);
                GUI.contentColor = Color.green;
                bool isFold = EditorGUILayout.Foldout(_stateList[i], i + ":" + nameDict[i][0].name, true);
                if (_stateList[i] != isFold)
                {
                    _stateList[i] = isFold;
                    EditorPrefs.SetBool(EDITOR_ALLTOOL_PREFS_KEY + key + i, isFold);
                }
                if (_stateList[i])
                {
                    GUI.contentColor = Color.white;
                    GUILayout.BeginHorizontal();
                    for (int j = 1, jNum = nameDict[i].Count; j < jNum; j++)
                    {
                        GUILayout.Space(j % hSize == 1 ? 10 : 5);
                        GUI.skin.button.wordWrap = true;
                        if (GUILayout.Button(nameDict[i][j].name, GUILayout.Width(width), GUILayout.Height(30)))
                        {
                            nameDict[i][j].clickTimes++;
                            string prefKey = EDITOR_ALLTOOL_PREFS_KEY + nameDict[i][j].name;
                            EditorPrefs.SetInt(prefKey, nameDict[i][j].clickTimes);
                            nameDict[i][j].func.Invoke();
                            RefreshLikeList(nameDict[i][j]);
                        }
                        if (!string.IsNullOrEmpty(nameDict[i][j].helpInfo))
                        {
                            if (GUILayout.Button(_helpTexture, _helpBtnStyle, GUILayout.Width(15), GUILayout.Height(15)))
                            {
                                EditorUtility.DisplayDialog(nameDict[i][j].name, nameDict[i][j].helpInfo, "确定");
                                if (!string.IsNullOrEmpty(nameDict[i][j].wikiUrl))
                                {
                                    Application.OpenURL(nameDict[i][j].wikiUrl);
                                }
                            }
                        }
                        if (j % hSize == 0 && j != jNum)
                        {
                            GUILayout.EndHorizontal();
                            GUILayout.BeginHorizontal();
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
        }

        private void RefreshLikeList(AllToolsItem item)
        {
            if (_likeItems.Count < EDITOR_ALLTOOL_LIKE_MAX_NUM)
            {
                if (!_likeItems.Contains(item))
                    _likeItems.Add(item);
                _likeItems.Sort((a, b) => a.clickTimes < b.clickTimes ? 1 : -1);
            }
            else
            {
                if (!_likeItems.Contains(item))
                {
                    _likeItems.Add(item);
                    _likeItems.Sort((a, b) => a.clickTimes < b.clickTimes ? 1 : -1);
                    _likeItems.RemoveAt(EDITOR_ALLTOOL_LIKE_MAX_NUM);
                }
                else
                {
                    _likeItems.Sort((a, b) => a.clickTimes < b.clickTimes ? 1 : -1);
                }
            }
        }

        //------------------------------------------------------
        private void InitIconTabs()
        {
            GUIArr = new GUIContent[]{
        EditorGUIUtility.TrTextContent("常用"),
        EditorGUIUtility.TrTextContent("工具") ,
        EditorGUIUtility.TrTextContent("打包"),
        //EditorGUIUtility.TrTextContent("TA"),
      };
        }

        private void Init()
        {
            if (_likeItems == null || _likeItems.Count == 0)
                InitPrefsSetting();
            InitHelpIcon();
            InitGUIStyle();
            InitIconTabs();
        }

        private void InitHelpIcon()
        {
            if (_helpTexture == null)
            {
                _helpTexture = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets\\LoadResources\\UI\\Common\\Image\\help.png");
            }
        }

        private void InitPrefsSetting()
        {
            if (EditorPrefs.HasKey(EDITOR_ALLTOOL_PREFS_KEY))
            {
                _Tab = (EnumTab)EditorPrefs.GetInt(EDITOR_ALLTOOL_PREFS_KEY);
                for (int i = 0; i < _uiStates.Count; i++)
                {
                    _uiStates[i] = EditorPrefs.GetBool(EDITOR_ALLTOOL_PREFS_KEY + "UI" + i);
                }
                for (int i = 0; i < _buildStates.Count; i++)
                {
                    _buildStates[i] = EditorPrefs.GetBool(EDITOR_ALLTOOL_PREFS_KEY + "Build" + i);
                }
                for (int i = 0; i < _taStates.Count; i++)
                {
                    _taStates[i] = EditorPrefs.GetBool(EDITOR_ALLTOOL_PREFS_KEY + "TA" + i);
                }
            }
            else
            {
                EditorPrefs.SetInt(EDITOR_ALLTOOL_PREFS_KEY, (int)_Tab);
                for (int i = 0; i < _uiStates.Count; i++)
                {
                    EditorPrefs.SetBool(EDITOR_ALLTOOL_PREFS_KEY + "UI" + i, true);
                }
                for (int i = 0; i < _buildStates.Count; i++)
                {
                    EditorPrefs.SetBool(EDITOR_ALLTOOL_PREFS_KEY + "Build" + i, true);
                }
                for (int i = 0; i < _taStates.Count; i++)
                {
                    EditorPrefs.SetBool(EDITOR_ALLTOOL_PREFS_KEY + "TA" + i, true);
                }
            }
            _likeItems.Clear();
            RefreshClickTimes(_uiNameDict);
            RefreshClickTimes(_buildNameDict);
            RefreshClickTimes(_taNameDict);
            _likeItems.Sort((a, b) => a.clickTimes < b.clickTimes ? 1 : -1);
            if (_likeItems.Count > EDITOR_ALLTOOL_LIKE_MAX_NUM)
            {
                _likeItems.RemoveRange(EDITOR_ALLTOOL_LIKE_MAX_NUM, _likeItems.Count - EDITOR_ALLTOOL_LIKE_MAX_NUM);
            }
        }

        // ---------------------------------------------------
        private void RefreshClickTimes(Dictionary<int, List<AllToolsItem>> dict)
        {
            for (int i = 0; i < dict.Count; i++)
            {
                for (int j = 1, jNum = dict[i].Count; j < jNum; j++)
                {
                    string key = EDITOR_ALLTOOL_PREFS_KEY + dict[i][j].name;
                    if (EditorPrefs.HasKey(key))
                    {
                        dict[i][j].clickTimes = EditorPrefs.GetInt(key);
                        if (dict[i][j].clickTimes > 0)
                        {
                            _likeItems.Add(dict[i][j]);
                        }
                    }
                }
            }
        }

        //------------------------------------------------------
        private void InitGUIStyle()
        {
            if (_tabTextSytel == null)
            {
                _tabTextSytel = new GUIStyle("BoldLabel");
            }
            if (_tabBtnStyle == null)
            {
                _tabBtnStyle = new GUIStyle("flow node 0");
                _tabBtnStyle.alignment = TextAnchor.MiddleCenter;
                _tabBtnStyle.fontStyle = _tabTextSytel.fontStyle;
                _tabBtnStyle.fontSize = 18;
                //color = _tabBtnStyle.normal.textColor;
            }
            if (_tabBtnOnStyle == null)
            {
                _tabBtnOnStyle = new GUIStyle("flow node 1 on");
                _tabBtnOnStyle.alignment = TextAnchor.MiddleCenter;
                _tabBtnOnStyle.fontStyle = _tabTextSytel.fontStyle;
                _tabBtnOnStyle.fontSize = 18;
            }
            if (_helpBtnStyle == null)
            {
                _helpBtnStyle = new GUIStyle();
                _helpBtnStyle.border = new RectOffset(1, 1, 1, 1);
                _helpBtnStyle.normal.background = _helpTexture;
                _helpBtnStyle.active.background = _helpTexture;
                _helpBtnStyle.alignment = TextAnchor.MiddleCenter;
                _helpBtnStyle.margin = new RectOffset(1, 1, 1, 1);
            }

            if (_textStyle == null)
            {
                _textStyle = new GUIStyle("HeaderLabel");
                _textStyle.fontSize = 20;
                _textStyle.alignment = TextAnchor.MiddleCenter;
            }
            if (_labStyle == null)
            {
                _labStyle = new GUIStyle("CenteredLabel");
                _labStyle.fontSize = 18;
            }
            if (_titleStyle == null)
            {
                _titleStyle = new GUIStyle("AM VuValue");
                _titleStyle.alignment = TextAnchor.MiddleRight;
                _titleStyle.fontSize = 15;
            }
            if (_iconNameStyle == null)
            {
                _iconNameStyle = new GUIStyle("WarningOverlay");
                _iconNameStyle.fontSize = 12;
            }
            if (_toggleStyle == null)
            {
                _toggleStyle = new GUIStyle("OL ToggleWhite");
            }
            if (_saveBtnStyle == null)
            {
                _saveBtnStyle = new GUIStyle("flow node 1");
                _saveBtnStyle.fontSize = 20;
                _saveBtnStyle.fixedHeight = 40;
                _saveBtnStyle.alignment = TextAnchor.MiddleCenter;
            }
        }
    }
}