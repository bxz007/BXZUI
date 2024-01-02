using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEditor;
using System.Reflection;

namespace GalaFramework
{

    [MvcWinodws(3)]
    public class ModuleTabWindow : IWindows
    {
        public Rect position { get; set; }
        public int Orde { get; set; }

        ToggleTreeview toggleTreeview;
        MVCMoudleItem Root;
        Action repaint;

        [MvcInject]
        ModuleTabData tabData;
        int id = 0;
        int depth;

        public static Action RecreateAllScripts;

        public void Init(Action Repaint)
        {
            repaint = Repaint;
            InitRoot();
            InitToggleTreeView();
            RecreateAllScripts = Recreate;
        }

        void Recreate()
        {
            var childRen = Root.children;
            ForeChildReCreate(childRen);
        }

        void ForeChildReCreate(List<TreeViewItem> children)
        {
            for(int i = 0 ;i<children.Count;i++)
            {
                var currentChild = children[i];
                var childs = currentChild.children;
                if(childs!=null &&children.Count>0)
                    ForeChildReCreate(childs);
                MVCMoudleItem item = currentChild as MVCMoudleItem;
                if(item.isObj)
                {
                    item.OpenAsset();
                    ComponentWindow.CreateAction();
                }
            }
        }

        public void OnClose()
        {
            if (tabData == null || toggleTreeview == null || tabData == null) return;
            var chacheData = tabData.GetCache();
            if (chacheData == null) return;
            chacheData.treeViewState = toggleTreeview.state;
            tabData.SaveCache();

            toggleTreeview = null;
            Root = null;
        }

        /// <summary>
        /// 初始化ToggleTree
        /// </summary>
        void InitToggleTreeView()
        {
            if (Root.children == null)
            {
                Debug.LogError("暂无数据");
                return;
            }
            if (toggleTreeview != null)
                return;

            toggleTreeview = new ToggleTreeview("Module", "勾选加入设置", 300, Root, tabData.GetCache().treeViewState);
            toggleTreeview.CellCallBack = CellGUI;
            toggleTreeview.DoubleClicked = DoubleClick;
            toggleTreeview.Clicked = ClickItem;
            toggleTreeview.SearchEvent = SearchOverr;
            toggleTreeview.multiColumnHeader.ResizeToFit();
            toggleTreeview.Reload();
        }

        #region 根节点初始化
        void InitRoot()
        {
            if (Root != null)
                return;
            Root = new MVCMoudleItem(0, -1, 0, "");
            Root.isSelect = false;
            tabData.mvcParent = new Dictionary<string, MVCMoudleItem>();
            for (int i = 0; i < tabData.foreachList.Count; i++)
            {
                MVCMoudleItem mVCMoudleItem;
                if (!tabData.mvcParent.TryGetValue(tabData.foreachList[i].ModuleName, out mVCMoudleItem))
                {
                    mVCMoudleItem = new MVCMoudleItem(++id, 0, 4, $"[{tabData.foreachList[i].ModuleName}]");
                    mVCMoudleItem.mVC = tabData.foreachList[i];
                    if (tabData.commentDic.ContainsKey(tabData.foreachList[i].ModuleName))
                    {
                        mVCMoudleItem.displayName += $"  {tabData.commentDic[tabData.foreachList[i].ModuleName]}";
                    }
                    tabData.mvcParent.Add(tabData.foreachList[i].ModuleName, mVCMoudleItem);
                    Root.AddChild(mVCMoudleItem);
                }
            }
            
            for (int i = 0; i < tabData.foreachList.Count; i++)
            {
                var ModuleRoot = new MVCMoudleItem(++id, 1, 5, $"{tabData.foreachList[i].MvcName}");
                ModuleRoot.isParent = true;
                ModuleRoot.mVC = tabData.foreachList[i];
                if (tabData.commentDic.ContainsKey(ModuleRoot.mVC.MvcName))
                {
                    ModuleRoot.displayName += $"    {tabData.commentDic[ModuleRoot.mVC.MvcName]}";
                }
                ModuleRoot.ScriptName = "";                
                InitTree(ModuleRoot);
                MVCMoudleItem parent;

                if (tabData.mvcParent.TryGetValue(tabData.foreachList[i].ModuleName, out parent))
                {
                    parent.AddChild(ModuleRoot);
                }
            }
        }
        #endregion

        /// <summary>
        /// 右键单击操作
        /// </summary>
        /// <param name="obj"></param>
        private void ClickItem(TreeViewItem obj)
        {
            var Value = (MVCMoudleItem)obj;
            Value.Focus();
            if (Value.parent != null && !string.IsNullOrEmpty(this.toggleTreeview.searchString))
            {
                this.toggleTreeview.searchString = "";
                this.toggleTreeview.CollapseAll();
                if (Value.parent != null)
                {
                    toggleTreeview.SetExpanded(Value.parent.id,true);
                    toggleTreeview.state.scrollPos = new Vector2(0, Value.parent.id * 20 + 50);
                }
                toggleTreeview.SetExpanded(Value.id, true);               
                this.toggleTreeview.FrameItem(Value.parent.id);
                this.repaint?.Invoke();
            }
        }

        /// <summary>
        /// 左键双击操作
        /// </summary>
        /// <param name="obj"></param>
        private void DoubleClick(TreeViewItem obj)
        {
            var Value = (MVCMoudleItem)obj;
            Value.OpenAsset();
        }

        /// <summary>
        /// 是否搜索成功判断回调
        /// </summary>
        /// <param name="arg1"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        private bool SearchOverr(TreeViewItem arg1, string arg2)
        {
            var Value = (MVCMoudleItem)arg1;

            if (Value.Type != 5 || Value.depth == 0 || Value.isObj)
            {
                return false;
            }
            string ModuleName = Value.mVC.MvcName.ToLower();
            string MvcName = Value.mVC.ModuleName.ToLower();
            string displayName = arg1.displayName.ToLower();
            string SearchName = arg2.ToLower();

            if (Value.parent != null && Value.parent.displayName.Contains(SearchName))
            {
                return true;
            }

            bool search = ModuleName.Contains(SearchName) || MvcName.Contains(SearchName) || displayName.Contains(SearchName);
            return search;
        }

        #region 单项绘制
        private void CellGUI(Rect rect, TreeViewItem _assetTreeItem, int column)
        {
            var Togglerect = rect;
            var TreeItem = (MVCMoudleItem)_assetTreeItem;
            var Depth = TreeItem.depth;
            var displayName = TreeItem.displayName;

            if (TreeItem.isParent)
            {
                // Togglerect.x = 14f + 14f * Depth;
                // Togglerect.width = 10;

                // var needSelect = EditorGUI.Toggle(Togglerect, false, (GUIStyle)"Radio");
                // if (needSelect)
                // {
                //     MVCMoudleItem tempItem = TreeItem.children[TreeItem.children.Count - 1] as MVCMoudleItem;

                //     if (tempItem.isObj)
                //     {
                //         tempItem.OpenAsset();
                //     }
                // }
            }

            Togglerect.Set(16 + 14 * Depth, rect.y, 300, 16);
            if (TreeItem.isParent)
            {
                // Togglerect.x += 14;
            }
            EditorGUI.LabelField(Togglerect, displayName);

            Togglerect.x = rect.width - 15;
            Togglerect.width = 30f;
            if (Depth <= 1)
            {
                TextEditorDraw(Togglerect, TreeItem, Depth);
            }
        }

        void TextEditorDraw(Rect Togglerect, MVCMoudleItem TreeItem, int Depth)
        {
            if (GUI.Button(Togglerect, "", (GUIStyle)"CN EntryInfoIconSmall"))
            {
                TextEitorWindows.Open(() =>
                {
                    string key = Depth == 0 ? TreeItem.mVC.ModuleName : TreeItem.mVC.MvcName;
                    if (tabData.commentDic.ContainsKey(key))
                    {
                        TreeItem.displayName = Depth == 0 ? $"{TreeItem.mVC.ModuleName}  {TextEitorWindows.InputText}" : $"{TreeItem.mVC.MvcName}    {TextEitorWindows.InputText}";
                        tabData.commentDic[key] = TextEitorWindows.InputText;
                    }
                    else
                    {
                        TreeItem.displayName += $"   {TextEitorWindows.InputText}";
                        tabData.commentDic.Add(key, TextEitorWindows.InputText);
                    }
                    tabData.SaveCommentDic();
                    this.repaint();
                }, new Vector2(position.x + position.width - 200, position.y + Togglerect.y - toggleTreeview.state.scrollPos.y + 50));
            }
        }

        #endregion
        void InitTree(MVCMoudleItem module)
        {
            if (module.mVC.View)
            {
                module.AddChild(CreateMvc(module.mVC, module.mVC.MvcName + "View", 0));
            }
            if (module.mVC.Controll)
            {
                module.AddChild(CreateMvc(module.mVC, module.mVC.MvcName + "Controller", 1));
            }
            if (module.mVC.Model)
            {
                module.AddChild(CreateMvc(module.mVC, module.mVC.MvcName + "ViewModel", 2));
            }

            if (module.mVC.View)
            {
                var item = CreateMvc(module.mVC, "Assets.Scripts." + module.mVC.MvcName + "View", 5);
                item.isObj = true;
                item.displayName = "GameObject";
                module.AddChild(item);
            }
        }

        MVCMoudleItem CreateMvc(MVCInfo info, string ScriptName, int Type)
        {
            var ModuleChild = new MVCMoudleItem(++id, 2, Type, ScriptName);
            ModuleChild.isParent = false;
            ModuleChild.mVC = info;
            ModuleChild.ScriptName = ScriptName;
            return ModuleChild;
        }

        public void OnGUI(Rect position)
        {
            this.position = position;
            Rect rect = EditorGUILayout.BeginVertical();
            GUILayout.BeginScrollView(Vector2.zero);

            if (toggleTreeview != null)
            {
                toggleTreeview.OnGUI(new Rect(0, 0, rect.width, rect.height - 15));
            }

            GUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Refresh Cache", (GUIStyle)"MiniToolbarButton"))
            {
                OnClose();
                tabData.RefreshCache();
                Init(this.repaint);
                GUIUtility.ExitGUI();
            }

            
            if(BaseData.CurrentOperate != null && !BaseData.CurrentOperate.Created && toggleTreeview != null && toggleTreeview.HasSelection() && toggleTreeview.HasFocus())
            {
                var selection = toggleTreeview.GetSelection()[0];
                var mvcItem = (MVCMoudleItem)toggleTreeview.GetItem(selection);
                if(mvcItem != null)
                {
                    BaseData.CurrentOperate.ModuleName = mvcItem.mVC.ModuleName;
                }

            }
        }

        public class TextEitorWindows : EditorWindow
        {
            public static string InputText { get; set; } = "";
            Action action;

            public static void Open(Action closeCallBack, Vector2 pos)
            {
                TextEitorWindows textEitorWindows = GetWindowWithRect<TextEitorWindows>(new Rect(pos.x, pos.y, 150, 40));
                textEitorWindows.titleContent = new GUIContent("Desc");
                textEitorWindows.position = new Rect(pos, new Vector2(100, 40));
                textEitorWindows.action = closeCallBack;
            }

            private void OnGUI()
            {
                InputText = EditorGUILayout.TextField(InputText);
                if (GUILayout.Button("确认"))
                {
                    action?.Invoke();
                    this.Close();
                }
            }

            private void OnLostFocus()
            {
                this.Close();
            }
        }

        public class ModuleTabData : IMvcData
        {
            /// <summary>
            /// 模块注释
            /// </summary>
            public Dictionary<string, string> commentDic;
            public Dictionary<string, MVCMoudleItem> mvcParent;
            public List<MVCInfo> foreachList;
            private BaseData baseData;
            public ModuleTabData() { }

            public override void Init(BaseData baseData)
            {
                this.baseData = baseData;
                commentDic = MVCComment.FromJson().ToDictionary();
                InitForeachlist();
            }

            void InitForeachlist()
            {
                var vDic = (from objDic in baseData.GetMainData() orderby objDic.Value.ModuleName ascending select objDic);
                foreachList = new List<MVCInfo>();
                foreach (var item in vDic)
                {
                    item.Value.Created = true;
                    foreachList.Add(item.Value);
                }
            }

            public void SaveCache()
            {
                baseData.SaveCache();
            }

            public MVCCache GetCache()
            {
                return baseData.GetCache();
            }

            public void RefreshCache()
            {
                baseData.ClearCache();
                baseData.RefreshMvc();
                InitForeachlist();
            }

            public void SaveCommentDic()
            {
                MVCComment mVCComment = new MVCComment(commentDic);
                mVCComment.ToJsonAndSave();
            }

            public override void Refresh() { }
        }
    }
}
