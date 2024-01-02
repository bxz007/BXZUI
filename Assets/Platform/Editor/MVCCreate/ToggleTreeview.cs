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

namespace GalaFramework
{
    [System.Serializable]
    public class ToggleTreeview : TreeView
    {
        SearchField                                 _searchField = new SearchField();
        TreeViewItem                                _root;

        public Action<Rect, TreeViewItem, int>      CellCallBack;
        public Action<TreeViewItem>                 DoubleClicked;
        public Action<TreeViewItem>                 Clicked;
        public Func<TreeViewItem, string, bool>     SearchEvent;
        public Action<IList<int>>                   OnExpChange;

        internal ToggleTreeview(TreeViewState state, MultiColumnHeaderState mchs, TreeViewItem Root) : base(state, new MultiColumnHeader(mchs))
        {
            showBorder = true;
            rowHeight = 18f;
            showAlternatingRowBackgrounds = true;
            this._root = Root;
        }

        internal ToggleTreeview(string contentName ,string tips,float width, TreeViewItem Root, TreeViewState state = default) :base(state, new MultiColumnHeader(CreateDefaultMultiColumnHeaderState(contentName,tips,width)))
        {
            showBorder = true;
            rowHeight = 18f;
            showAlternatingRowBackgrounds = true;
            this._root = Root;
        }

        protected override TreeViewItem BuildRoot()
        {
            return _root;
        }

        public override void OnGUI(Rect rect)
        {
            Rect screct = rect;
            screct.height = 18f;
            searchString = _searchField.OnGUI(screct, searchString);
            screct.height = rect.height;
            screct.y += 18f;
            base.OnGUI(screct);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            for (int i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                if (CellCallBack != null)
                { 
                    CellCallBack(args.GetCellRect(i), args.item, args.GetColumn(i));
                }
            }
        }

        protected override bool DoesItemMatchSearch(TreeViewItem item, string search)
        {
            if (SearchEvent != null)
                return SearchEvent.Invoke(item, search);

            return base.DoesItemMatchSearch(item, search);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, _root);
            if (item != null)
            {
                if (Clicked != null)
                {
                    Clicked(item);
                }
            }
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, _root);
            if (item != null)
            {
                if (DoubleClicked != null)
                {
                    DoubleClicked(item);
                }
            }
        }

        public TreeViewItem GetItem(int id)
        {
            return FindItem(id, _root);
        }
       
        protected override void ExpandedStateChanged()
        {
            OnExpChange?.Invoke(GetExpanded());
        }

        static private MultiColumnHeaderState CreateDefaultMultiColumnHeaderState(string content,string tips,float width)
        {
            return new MultiColumnHeaderState(GetColumns(content,tips,width));
        }

        static private MultiColumnHeaderState.Column[] GetColumns(string content, string tips, float width)
        {
            var retVal = new MultiColumnHeaderState.Column[] { new MultiColumnHeaderState.Column() };

            retVal[0].headerContent = new GUIContent(content, tips);
            retVal[0].width = width;
            retVal[0].headerTextAlignment = TextAlignment.Left;
            retVal[0].canSort = true;
            retVal[0].autoResize = true;

            return retVal;
        }
    }
}