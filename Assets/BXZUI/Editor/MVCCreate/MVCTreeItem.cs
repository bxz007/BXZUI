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
using UnityEditor;
using System;
using Object = UnityEngine.Object;
using System.Reflection;

namespace GalaFramework
{
    [Serializable]
    public class MVCTreeItem : TreeViewItem
    {
        public Transform ItemTsf { get; set; }
        private bool isSelect;
        public bool IsSelect 
        {
            get => isSelect;
            set 
            {
                isSelect = value;
            }
        }
        public string CurrentType { get; set; }
        public List<string> TempHasComponent { get; set; }
        public int TempIndex { get; private set; }


        public void SetSelected(int left)
        {
            TempIndex |= 1 << left;
        }

        public void ChangeState(int left)
        {
            left = 1 << left;

            if(!Mixed())
            {
                TempIndex = left == 1 ? TempIndex : 0;
                TempIndex |= left;
            }
            else
            {
                if ((TempIndex & left) > 0)
                {
                    TempIndex ^= left;
                    var currentSelectds = GetSelecteds();
                    
                    if(currentSelectds.Length > 0 && currentSelectds[0] != 0)
                    {
                        TempIndex = 0;
                        TempIndex |= 1 << currentSelectds[0];
                    }
                    else if (currentSelectds.Length == 2)
                    {
                        TempIndex ^= 1;
                    }
                    
                }
                else
                {
                    TempIndex |= left;
                }
            }
        }

        private bool Mixed()
        {
            return (TempIndex & 1) != 0;
        }

        public int[] GetSelecteds()
        {
            var selecteds = new List<int>();
            for (int i = 0; i < TempHasComponent.Count + 1; i++)
            {
                if ((TempIndex & (1 << i)) > 0)
                {
                    selecteds.Add(i);
                }
            }
            return selecteds.ToArray();
        }

    }

    [Serializable]
    public class MVCMoudleItem : TreeViewItem
    {
        public Object Asset { get; set; }
        public MVCInfo mVC { get; set; }
        public bool isSelect { get; set; }
        public bool isParent { get; set; }
        public string ScriptName { get; set; }
        public bool isObj { get; set; }
        public int Type { get; set; }

        public MVCMoudleItem() { }

        public MVCMoudleItem(int id, int depth, int type, string displayName)
        {
            this.id = id;
            this.depth = depth;

            this.Type = type;
            this.displayName = displayName;
        }

        public void OpenAsset()
        {
            if (!isObj)
            {
                string path = MVCCodeCreator.GetScriptPath(mVC.ModuleName, ScriptName, Type, mVC.MvcName, mVC.isHotfix);
                Asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                AssetDatabase.OpenAsset(Asset);
            }
            else
            {
                Type type = null;
                var typeName = mVC.isHotfix ? $"PlatformHotfix.{mVC.MvcName}View" : $"Platform.{mVC.MvcName}View";
                foreach (var item in TypeCache.GetTypesWithAttribute<SourceAttribute>())
                {
                    if (item.FullName.Equals(typeName))
                    {
                        type = item;
                        break;
                    }
                }
                if (type != null)
                {
                    SourceAttribute sourceAttribute = type.GetCustomAttribute<SourceAttribute>();
                    if (sourceAttribute != null)
                    {
                        Object @object = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LoadResources/{sourceAttribute.SourcePath}.prefab");
                        if (@object == null)
                        {
                            @object = Resources.Load(sourceAttribute.SourcePath);
                        }
                        if (@object)
                        {
                            AssetDatabase.OpenAsset(@object);
                        }
                    }
                }
            }
        }

        public void Focus()
        {
            if (!isObj)
            {
                string path = MVCCodeCreator.GetScriptPath(mVC.ModuleName, ScriptName, Type, mVC.MvcName, mVC.isHotfix);
                Asset = AssetDatabase.LoadAssetAtPath(path, typeof(Object));
                EditorGUIUtility.PingObject(Asset);
            }
            else
            {
                Type type = null;
                var typeName = mVC.isHotfix ? $"PlatformHotfix.{mVC.MvcName}View" : $"Platform.{mVC.MvcName}View";
                foreach (var item in TypeCache.GetTypesWithAttribute<SourceAttribute>())
                {
                    if (item.FullName.Equals(typeName))
                    {
                        type = item;
                        break;
                    }
                }
                if (type != null)
                {
                    SourceAttribute sourceAttribute = type.GetCustomAttribute<SourceAttribute>();
                    if (sourceAttribute != null)
                    {
                        Object @object = AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/LoadResources/{sourceAttribute.SourcePath}.prefab");
                        if (@object == null)
                        {
                            @object = Resources.Load(sourceAttribute.SourcePath);
                        }
                        EditorGUIUtility.PingObject(@object.GetInstanceID());
                    }
                }
            }
        }
    }
}