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
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace GalaFramework
{

    public class MVCComment : Serialization<string, string>
    {
        public MVCComment(Dictionary<string, string> keyValuePairs) : base(keyValuePairs) { }

        static string InfoPath { get; } = "Assets/Editor/MVCComment.json";

        public string ToJson()
        {
            var config = JsonUtility.ToJson(this);
            return config;
        }

        public static MVCComment FromJson(string value)
        {
            var config = JsonUtility.FromJson<MVCComment>(value);
            if (config == null)
                return null;
            return config;
        }

        public string ToJsonAndSave()
        {
            var config = JsonUtility.ToJson(this);
            File.WriteAllText(InfoPath, config);
            return config;
        }

        public static MVCComment FromJson()
        {
            MVCComment string4Int = null;
            if (!File.Exists(InfoPath))
            {
                string4Int = new MVCComment(new Dictionary<string, string>());
                File.WriteAllText(InfoPath, string4Int.ToJson());
            }
            else
            {
                var JsonValue = File.ReadAllText(InfoPath);
                string4Int = FromJson(JsonValue);
            }
            return string4Int;
        }
    }

    [System.Serializable]
    public class Serialization<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField]
        List<TKey> keys;
        [SerializeField]
        List<TValue> values;

        Dictionary<TKey, TValue> target;
        public Dictionary<TKey, TValue> ToDictionary() { return target; }

        public Serialization(Dictionary<TKey, TValue> target)
        {
            this.target = target;
        }

        public void OnBeforeSerialize()
        {
            keys = new List<TKey>(target.Keys);
            values = new List<TValue>(target.Values);
        }

        public void OnAfterDeserialize()
        {
            var count = Math.Min(keys.Count, values.Count);
            target = new Dictionary<TKey, TValue>(count);
            for (var i = 0; i < count; ++i)
            {
                target.Add(keys[i], values[i]);
            }
        }
    }
}