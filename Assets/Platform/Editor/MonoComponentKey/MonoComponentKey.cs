using GalaFramework;
using MVCCreator;
using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;

using UnityEngine;

public class MonoComponentKey
{
    static string target;

    [MenuItem("CONTEXT/Component/Bind Component Key", true)]
    public static bool GenerateScriptValidate(MenuCommand command)
    {
        return MenuValidate(command) && CodeWindowMain.GetInspectedInstancedId() == command.context.GetType().Name;
    }
    [MenuItem("CONTEXT/Component/Inspect Component Key")]
    public static void SetInspect(MenuCommand command)
    {
        CodeWindowMain.SetInspect(command.context.GetType().Name, command.context.GetInstanceID());
    }

    [MenuItem("CONTEXT/Component/Bind Component Key")]
    public static void MakeMonoComponentKey(MenuCommand command)
    {
        if (!IsPartialClass(target))
        {
            Debug.LogError($"类型\"{command.context.GetType().Name}\"的声明缺少partial修饰符");
            target = null;
            return;
        }

        var componentData = AddComponentItemKey(command.context.GetType().Name);
        CreateViewComponentCodeByDom(target, componentData, GetNamespace(target));
        target = null;
    }



    static bool MenuValidate(MenuCommand command)
    {
        target = null;
        // if (UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage() == null)
        // {
        //     return false;
        // }

        var allScripts = AssetDatabase.FindAssets("t:Script", new string[]
        { 
            //"Assets/Scripts/Main/TA",
        });
        foreach (var guid in allScripts)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(path) == command.context.GetType().Name && CodeWindowMain.GetInspectedInstancedId() == command.context.GetType().Name)
            {
                target = path;
                break;
            }
        }
        return target != null;
    }

    static List<ComponentData> AddComponentItemKey(string componentName)
    {
        // var componentKey = CodeWindowMain.GetComponentKey();
        // var prefabStage = UnityEditor.SceneManagement.PrefabStageUtility.GetCurrentPrefabStage();
        // ComponentItemKey cpt = prefabStage.prefabContentsRoot.GetComponent<ComponentItemKey>();
        // if (cpt == null)
        // {
        //     cpt = prefabStage.prefabContentsRoot.AddComponent<ComponentItemKey>();
        // }

        // if(cpt.componentDatas == null)
        // {
        //     cpt.componentDatas = new List<ComponentData>();
        // }
         var result = new List<ComponentData>();
        // var componentData = cpt.componentDatas;
        // var sb = new StringBuilder(componentName);
        // foreach (var item in componentKey)
        // {
        //     var splitComponentsName = item.Value.Split('#');
        //     foreach (var component in splitComponentsName)
        //     {
        //         var typeName = component.Remove(0, component.LastIndexOf('.') + 1);
        //         var key = typeName + "_" + item.Key.name;
        //         var index = componentData.FindIndex(x => x.Key == key);
                
        //         if (index >= 0)
        //         {
        //             sb.Append("#").Append(index);
        //             result.Add(componentData[index]);
        //         }
        //         else
        //         {
        //             sb.Append("#").Append(componentData.Count);
        //             componentData.Add(new ComponentData()
        //             {
        //                 Key = key,
        //                 Type = component,
        //                 Value = item.Key.gameObject.GetComponent(typeName)
        //             });
        //             result.Add(componentData[componentData.Count - 1]);
        //         }
        //     }
        // }
        // if(cpt.selectedOfGameObject == null)
        // {
        //     cpt.selectedOfGameObject = new List<string>();
        // }
        // var existIndex = cpt.selectedOfGameObject.FindIndex(x => x.Contains(componentName));
        // if(existIndex >= 0)
        // {
        //     cpt.selectedOfGameObject[existIndex] = sb.ToString();
        // }
        // else
        // {
        //     cpt.selectedOfGameObject.Add(sb.ToString());
        // }

        // EditorUtility.SetDirty(prefabStage.prefabContentsRoot);
        // EditorUtility.SetDirty(cpt);
        // CodeWindowMain.SetComponentWindowStatus(false);
        // foreach (var item in prefabStage.GetType().GetRuntimeMethods())
        // {
        //     if (item.Name.Equals("SavePrefab"))
        //     {
        //         item.Invoke(prefabStage, null);
        //         break;
        //     }
        // }
        

        return result;
    }

    #region CodeDom

    static bool IsPartialClass(string scriptPath)
    {
        string txt = File.ReadAllText(scriptPath);
        Regex regex = new Regex(@"(\w+)\sclass");
        var match = regex.Match(txt);
        if (match.Success)
        {
            if (match.Groups[1].Value == "partial")
            {
                return true;
            }
        }
        return false;
    }

    static string GetNamespace(string scriptPath)
    {
        string txt = File.ReadAllText(scriptPath);
        Regex regex = new Regex(@"namespace\s(\w+)");
        var match = regex.Match(txt);
        if (match.Success)
        {
            return match.Groups[1].Value;
        }
        return null;

    }

    static void CreateViewComponentCodeByDom(string filePath, List<ComponentData> components, string nameSpace = "")
    {
        string scriptPath = filePath.Replace(".", "PartialComponent.");

        CreateCodeHelper createCodeHelper = new CreateCodeHelper();
        if (string.IsNullOrEmpty(nameSpace))
        {
            createCodeHelper.SetNameSpace(nameSpace);
        }

        createCodeHelper.SetClassName(Path.GetFileNameWithoutExtension(filePath));
        createCodeHelper.SetPartial(true);


        VariableInfo variableInfo = new VariableInfo() { Type = "ComponentItemKey", Name = "_componentItemKey" };
        createCodeHelper.AddVariable(variableInfo);

        List<string> componentGetStatement = new List<string>();
        componentGetStatement.Add($"\t\t\tif(_componentItemKey == null) _componentItemKey = transform.GetComponent<ComponentItemKey>(); return _componentItemKey;");
        createCodeHelper.AddProperty(new MVCCreator.PropertyInfo()
        {
            Type = "ComponentItemKey",
            Name = "componentItemKey",
            MethodAtt = MemberAttributes.Family | MemberAttributes.Final,
            GetStatementsStr = componentGetStatement
        });

        List<string> releaseMethodStatements = new List<string>()
        {
            "\t\t\t_componentItemKey = null;" ,
        };

        foreach (var item in components)
        {
            var variableName = item.Key;
            createCodeHelper.AddVariable(new VariableInfo()
            {
                Name = $"_{variableName}",
                Type = item.Type,
                MethodAtt = MemberAttributes.Private
            });

            List<string> getStatements = new List<string>();
            getStatements.Add($"\t\t\tif(_{variableName} == null)  _{variableName} = componentItemKey.GetObject<{item.Type}>(\"{variableName}\"); return _{variableName};");

            releaseMethodStatements.Add($"\t\t\t_{variableName} = null;");

            createCodeHelper.AddProperty(new MVCCreator.PropertyInfo()
            {
                Type = item.Type,
                Name = variableName,
                GetStatementsStr = getStatements,
                MethodAtt = MemberAttributes.Public | MemberAttributes.Final
            });
        }

        createCodeHelper.AddMethodInofs(new MVCCreator.MethodInfo() { MethodAtt = MemberAttributes.Family, MethodStatements = releaseMethodStatements, MethodName = "ReleaseComponent" });

        createCodeHelper.Create(scriptPath);
        Debug.Log($"Success Create PartialComponent Code : {scriptPath}");
        AssetDatabase.Refresh();
    }

    #endregion

}
