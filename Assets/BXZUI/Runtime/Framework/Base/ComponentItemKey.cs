using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ComponentData
{
    public string Key;
    public string Type;
    public UnityEngine.Object Value;

    public override string ToString()
    {
        return string.Format("{0}, {1}, {2}", Key, Type, Value);
    }
}

public class ComponentItemKey : MonoBehaviour, ISerializationCallbackReceiver
{
    public Dictionary<string, UnityEngine.Object> dic = new Dictionary<string, UnityEngine.Object>();

    public List<ComponentData> componentDatas;

#if UNITY_EDITOR
    // instanceId#index1#index2
    [HideInInspector]public List<string> selectedOfGameObject;
#endif

    public T GetObject<T>(string key) where T:Component
    {
        T t = null;
        if (key != null)
        {
            if (dic.ContainsKey(key))
            {
                t = dic[key] as T;
            }
        }
        if(t == null)
        {
            Debug.LogError($" ##Error## gameObject.name == {gameObject.name}  -- key == {key} ，value == null，");
        }
        return t;
    }

    void OnDestroy()
    {
        componentDatas.Clear();
        dic.Clear();
    }

    public void OnAfterDeserialize()
    {
        dic.Clear();
        if (componentDatas != null)
        {
            for (int i = 0; i < componentDatas.Count; i++)
            {
                dic.Add(componentDatas[i].Key, componentDatas[i].Value);
            }
        }

    }

    public void OnBeforeSerialize()
    {

    }
}
