namespace GalaFramework
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;
    using System;
    using System.Reflection;
    using UnityEditor;
    using System.Linq;
    

    /// <summary>
    /// 这里API和unity版本2018.4.x一致，如果版本升级到2019 需要通过github 重新比对API；
    /// </summary>
    public class StageNavigationReflection
    {
        private object Singleton; // ScriptableSingleton<StageNavigationManager>
        private EventInfo stageChanged; //Action<StageNavigationItem, StageNavigationItem>
        private EventInfo prefabStageReloaded; //Action<PrefabStage>
        private EventInfo prefabStageDirtinessChanged; //Action<PrefabStage>
        private PropertyInfo currentItem; //StageNavigationItem 
        private MethodInfo savePrefab; //保存预制体方法

        bool is2020 = false;

        public StageNavigationReflection(object singleton)
        {
            Singleton = singleton;
            Type stageNavigationReflection = Singleton.GetType();

            currentItem = stageNavigationReflection.GetProperty("currentItem", BindingFlags.Instance | BindingFlags.NonPublic);

            if (currentItem == null)
            {
                is2020 = true;
                currentItem = stageNavigationReflection.GetProperty("currentStage", BindingFlags.Instance | BindingFlags.NonPublic);
            }

            if (is2020)
            {
                this.stageChanged = Singleton.GetType().GetEvent("stageChanged", BindingFlags.Public | BindingFlags.Instance);
                this.prefabStageReloaded = typeof(UnityEditor.SceneManagement.PrefabStage).GetEvent("prefabStageReloaded", BindingFlags.NonPublic | BindingFlags.Static);
                this.prefabStageDirtinessChanged = typeof(UnityEditor.SceneManagement.PrefabStage).GetEvent("prefabStageDirtied", BindingFlags.Public | BindingFlags.Static);
            }
            else
            {
                foreach (var item in Singleton.GetType().GetRuntimeEvents())
                {
                    switch (item.Name)
                    {
                        case "stageChanged":
                            this.stageChanged = item;
                            break;
                        case "prefabStageReloaded":
                            this.prefabStageReloaded = item;
                            break;
                        case "prefabStageDirtinessChanged":
                            prefabStageDirtinessChanged = item;
                            break;
                    }
                }
            }       
        }

        public Type GetStageChangeType()
        {
            return stageChanged.EventHandlerType;
        }

        public Type GetPrefabStageReloadedType()
        {
            return prefabStageReloaded.EventHandlerType;
        }

        public Type GetPrefabStageDirtinessChangedType()
        {
            return prefabStageDirtinessChanged.EventHandlerType;
        }

        public void AddPrefabStageDirtinessChangedHandler(Delegate func)
        {
            if (is2020)
            {
                prefabStageDirtinessChanged.AddEventHandler(null, func);
            }
            else
            {
                prefabStageDirtinessChanged.AddEventHandler(Singleton, func);
            }
        }

        public void RemovePrefabStageDirtinessChangedHandler(Delegate func)
        {
            if (is2020)
            {
                prefabStageDirtinessChanged.RemoveEventHandler(null, func);
            }
            else
            {
                prefabStageDirtinessChanged.RemoveEventHandler(Singleton, func);
            }
        }

        public void AddStageChangedEventHandler(Delegate func)
        {
            stageChanged.AddEventHandler(Singleton,func);
        }

        public void RemoveStageChangedEventHandler(Delegate func)
        {
            stageChanged.RemoveEventHandler(Singleton, func);
        }

        public void AddPrefabStageReloadedEventHandler(Delegate func)
        {
            if (is2020)
            {
                var addMethod = prefabStageReloaded.GetAddMethod(true);
                addMethod.Invoke(null, new object[1] { func });
            }
            else
            {
                prefabStageReloaded.AddEventHandler(Singleton, func);
            }
        }

        public void RemovePrefabStageReloadedEventHandler(Delegate func)
        {
            if (is2020)
            {
                var remove = prefabStageReloaded.GetRemoveMethod(true);
                remove.Invoke(null, new object[1] { func });
            }
            else
            {
                prefabStageReloaded.RemoveEventHandler(Singleton, func);
            }
        }

        public string GetPrefabPath()
        {
            var itemObj = currentItem.GetValue(Singleton);
            Type stageNavigation = currentItem.GetValue(Singleton).GetType();
            if (is2020)
            {
                var property = stageNavigation.GetProperty("assetPath", BindingFlags.Public | BindingFlags.Instance);
                return property.GetValue(itemObj) as string;
            }
            else
            {
                var prefabPath = stageNavigation.GetRuntimeProperty("prefabAssetPath").GetValue(itemObj) as string;
                return prefabPath;
            }
        }

        public GameObject GetPrefabRoot()
        {
            var prefabStage = GetPrefabStage();
            if (prefabStage == null)
                return null;
            var propertyInfo = prefabStage.GetType().GetRuntimeProperty("prefabContentsRoot");
            var value = propertyInfo.GetValue(prefabStage);

            return value as GameObject;
        }

        public void CallSaveFunc()
        {
            var prefabStage = GetPrefabStage();
            if (savePrefab == null)
            {            
                foreach (var item in prefabStage.GetType().GetRuntimeMethods())
                {
                    if (item.Name.Equals("SavePrefab"))
                    {
                        savePrefab = item;
                        break;
                    }
                }
            }
            savePrefab.Invoke(prefabStage,null);
        }

        public object GetPrefabStage()
        {
            if (is2020)
            {
                return UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
            }
            else
            {
                var itemObj = currentItem.GetValue(Singleton);
                Type stageNavigation = itemObj.GetType();
                PropertyInfo propertyInfo = stageNavigation.GetRuntimeProperty("prefabStage");
                return propertyInfo.GetValue(itemObj);
            }
        }
    }


    public class BaseData {

        public static bool                      NeedRefresh;
        public static Assembly                  EditorAssembly;
        Dictionary<string, MVCInfo>             MainData = new Dictionary<string, MVCInfo>();
        Dictionary<string, IMvcData>            EditorData = new Dictionary<string, IMvcData>();
        List<IWindows>                          windows = new List<IWindows> ();
        public static MVCInfo                   CurrentOperate;
        StageNavigationReflection               navigationReflection;
        private static bool                     _mvvm;
        private MVCCache                        cache;

        public BaseData()
        {
            Init();
        }

        void Init()
        {
            _mvvm = EditorPrefs.GetBool("MVCEditor");
            windows.Clear();
            MainData.Clear();
            cache = MVCCache.LoadAsset();
            if (cache.mvcInfos == null || cache.mvcInfos.Count == 0)
            {
                RefreshMvc();
            }
            else
            {
                for (int i = 0; i < cache.mvcInfos.Count; i++)
                {
                    MainData.Add(cache.mvcInfos[i].MvcName, cache.mvcInfos[i]);
                }
            }
            CurrentOperate = null;
            EditorData.Clear();

            EditorAssembly = this.GetType().Assembly;

            Type dataType = typeof(IMvcData);
            foreach (var type in EditorAssembly.GetTypes())
            {
                if (dataType.IsAssignableFrom(type))
                {
                    var editorData = System.Activator.CreateInstance(type) as IMvcData;
                    editorData.Init(this);
                    EditorData.Add(type.Name, editorData);
                }
                var mvcWinodwsAttribute = type.GetCustomAttribute<MvcWinodwsAttribute>(true);
                if (mvcWinodwsAttribute != null)
                {
                    var currentWindow = System.Activator.CreateInstance(type) as IWindows;
                    currentWindow.Orde = mvcWinodwsAttribute.Orde;
                    windows.Add(currentWindow);
                }
            }
        }

        public bool Update()
        {
            if (NeedRefresh)
            {
                NeedRefresh = false;
                Refresh();
                return true;
            }
            return false;
        }

        public void SaveCache()
        {
            if (cache == null)
                return;

            cache.Save();
        }

        public void ClearCache()
        {
            if (cache == null)
                return;

            cache.mvcInfos = new List<MVCInfo>();
            cache.treeViewState = new UnityEditor.IMGUI.Controls.TreeViewState ();
            cache.Save();
        }

        public MVCCache GetCache()
        {
            return cache;
        }

        public void RefreshMvc()
        {
            cache.mvcInfos = new List<MVCInfo>();
            MainData.Clear();
            LoadAssemblyType();
            cache.Save();
        }

        void LoadAssemblyType()
        {
            var allMvcType = TypeCache.GetTypesWithAttribute<MvcAttribute>();
            foreach (var type in allMvcType)
            {
                var mvcAttributes = type.GetCustomAttribute<MvcAttribute>();
                var moduleAttribute = type.GetCustomAttribute<ModuleAttribute>();
                var mvcName = mvcAttributes.MvcName;
                MVCInfo mvcInfo;
                if (!MainData.TryGetValue(mvcName, out mvcInfo))
                {
                    mvcInfo = new MVCInfo() { };
                    MainData.Add(mvcName, mvcInfo);
                    cache.mvcInfos.Add(mvcInfo);
                }
                if (moduleAttribute != null)
                {
                    mvcInfo.ModuleName = moduleAttribute.ModuleName;
                }
                mvcInfo.isHotfix = type.Assembly.FullName.ToLower().Contains("hotfix") || type.Namespace.ToLower().Contains("hotfix");
                mvcInfo.MvcName = mvcName;
                TypeCheck(ref mvcInfo, type);
            }
        }

        public void SetEditorReflectionData(object stageNavionManagerSingleton)
        {
            navigationReflection = new StageNavigationReflection(stageNavionManagerSingleton);
        }

        public void Inject(Type injectType,object obj)
        {
            var allFields = injectType.GetRuntimeFields();
            foreach (var filed in allFields)
            {
                var attribute = filed.GetCustomAttribute<MvcInjectAttribute>();
                if (attribute != null)
                {
                    IMvcData mvcData;
                    if (EditorData.TryGetValue(filed.FieldType.Name, out mvcData))
                    {
                        filed.SetValue(obj, mvcData);
                    }
                }
            }
        }

        public void TypeCheck(ref MVCInfo mVCInfo, Type type)
        {
            if (type.Name.Contains("Controller"))
            {
                mVCInfo.Controll = true;
            }
            else if (type.Name.Contains("ViewModel"))
            {
                mVCInfo.Model = true;
            }
            else if (type.Name.Contains("View"))
            {
                mVCInfo.View = true;
            }
        }

        public void Refresh()
        {
            foreach (var item in EditorData)
            {
                item.Value.Refresh();
            }
        }

        public Dictionary<string, MVCInfo> GetMainData()
        {
            return MainData;
        }

        public List<IWindows> GetWindows()
        {
            return windows;
        }

        public StageNavigationReflection GetStageReflection()
        {
            return navigationReflection;
        }
    }

    public class IMvcData
    {
        public IMvcData() { }
        public virtual void Init(BaseData baseData) { }
        public virtual void Refresh() { }
    }

    public class MvcInjectAttribute : Attribute { }

}
