using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace GalaFramework
{
    public class MVCCache : ScriptableObject
    {
        [SerializeField]
        public List<MVCInfo> mvcInfos = new List<MVCInfo> ();
        [SerializeField]
        public TreeViewState treeViewState;
        const string savePath = "Assets/Editor/MVCCache";
        const string saveName = "Cache.asset";

        public void Create()
        {
            if (!System.IO.Directory.Exists(savePath))
            {
                System.IO.Directory.CreateDirectory(savePath);
            }
            AssetDatabase.CreateAsset(this, $"{savePath}/{saveName}");
        }

        public static MVCCache LoadAsset()
        {
            if (!System.IO.File.Exists($"{savePath}/{saveName}"))
            {
               var mVCCache = ScriptableObject.CreateInstance<MVCCache>();
                mVCCache.Create();
                return mVCCache;
            }
            return AssetDatabase.LoadAssetAtPath<MVCCache>($"{savePath}/{saveName}");
        }

        public static void Clear()
        {
            AssetDatabase.DeleteAsset($"{savePath}/{saveName}");
        }

        public void Save()
        {
            EditorUtility.SetDirty(this);
        }
    }
}