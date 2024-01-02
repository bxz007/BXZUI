/*******************************************************************
* Copyright(c) #YEAR# #COMPANY#
* All rights reserved.
*
* 文件名称: #SCRIPTFULLNAME#
* 简要描述:
* 
* 创建日期: #DATE#
* 作者:     #AUTHOR#
* 说明:  
******************************************************************/
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.IMGUI.Controls;
using System;
using UnityEditor;

namespace Gala.FrameworkEditorTools
{
    public class ToggleTreeview : TreeView
    {
        SearchField _searchField = new SearchField();
        TreeViewItem Root;
        public Func<Rect, TreeViewItem, int,bool> CellCallBack;
        public Action<TreeViewItem> OnDoubleClicked;
        public Action<TreeViewItem> OnContextClicked;
        public Action<TreeViewItem> OnClicked;
        public Action<int,string> OnRename;
        public Func<TreeViewItem, string, bool> SearchEvent;
        public Action<IList<int>> OnSelectChange;

        internal ToggleTreeview(TreeViewState state, MultiColumnHeaderState mchs, TreeViewItem Root,float rowHeight = 18f, bool showAlternatingRowBackgrounds = true) : base(state, new MultiColumnHeader(mchs))
        {
            showBorder = false;
            this.rowHeight = rowHeight;
            this.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;
            this.Root = Root;
            this.searchString = "";
        }

        internal ToggleTreeview(string contentName, string tips, float width, TreeViewItem Root, bool showBorder, bool showAlternatingRowBackgrounds, float rowHeight = 18f) : base(new TreeViewState())
        {
            this.rowHeight = rowHeight;
            this.showBorder = showBorder;
            this.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;
            showAlternatingRowBackgrounds = true;
            this.Root = Root;
            this.searchString = "";
        }

        protected override TreeViewItem BuildRoot()
        {
            return Root;
        }

        public override void OnGUI(Rect rect)
        {
            Rect screct = rect;
            screct.height = 18f;
            searchString = _searchField.OnGUI(screct, searchString);
            screct.height = rect.height;
            screct.height -= 18f;
            screct.y += 18f;
            base.OnGUI(screct);
        }

        public void SetRowHeight(float rowHeight)
        {
            this.rowHeight = rowHeight;
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (SearchEvent != null)
                return SearchEvent.Invoke(item, search);
            return base.DoesItemMatchSearch(item, search);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            OnSelectChange?.Invoke(selectedIds);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            if (CellCallBack != null)
            {
                if (CellCallBack.Invoke(args.rowRect, args.item, args.item.id))
                {
                    return;
                }
            }
            base.RowGUI(args);
        }

        protected override void SingleClickedItem(int id)
        {
            var item = FindItem(id, Root);
            if (item != null)
            {
                if (OnClicked != null)
                {
                    OnClicked(item);
                }
            }
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, Root);
            if (item != null)
            {
                if (OnContextClicked != null)
                {
                    OnContextClicked(item);
                }
            }
        }

        public TreeViewItem GetItem(int ID)
        {
            return FindItem(ID,Root);
        }

        public void SetClickItem(int id)
        {
            var selectionId = GetItem(id);
            if (selectionId != null)
                SelectionClick(selectionId, true);
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return base.CanRename(item);
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            OnRename?.Invoke(args.itemID, args.newName);        
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, Root);
            if (item != null)
            {
                if (OnDoubleClicked != null)
                {
                    OnDoubleClicked(item);
                }
            }
        }
    }
}