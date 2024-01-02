using Microsoft.CSharp;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MVCCreator
{

    public class AttributeInfo
    {
        public string Type;
        public object value;
    }

    public class MethodInfo
    {
        public string MethodName;
        public MemberAttributes MethodAtt;
        public string ReturnType;
        public List<AttributeInfo> AttributeInfos = new List<AttributeInfo>();
        public List<VariableInfo> Parameters = new List<VariableInfo>();
        public List<string> MethodStatements = new List<string>();
    }

    public class VariableInfo
    {
        public string Name;
        public string Type;
        public bool Parameter = false;
        public MemberAttributes MethodAtt;
        public List<AttributeInfo> AttributeInfos = new List<AttributeInfo>();
        public object DefaultValue;
    }

    public class PropertyInfo
    {
        public string Name;
        public string Type;
        public MemberAttributes MethodAtt;
        public List<AttributeInfo> AttributeInfos = new List<AttributeInfo>();
        public VariableInfo GetStatements;
        public List<string> GetStatementsStr;
        public VariableInfo SetStatements;
    }

    public class CreateCodeHelper
    {

        private string NameSpace;
        private string ClassName;
        private List<string> BaseTypes;
        private string CodeType = "C";
        private MemberAttributes ClassmemberAttributes = MemberAttributes.Public;
        private List<CodeCommentStatement> ClassComments;
        private List<AttributeInfo> AttributeInfos;
        private List<MethodInfo> MethodInfos;
        private List<VariableInfo> VariableInfos;
        private List<PropertyInfo> PropertyInfos;
        private List<string> UsingList;
        private bool isPartial;
        public bool BlankLinesBetweenMembers = true;

        public CreateCodeHelper()
        {
            BaseTypes = new List<string>();
            AttributeInfos = new List<AttributeInfo>();
            MethodInfos = new List<MethodInfo>();
            UsingList = new List<string>();
            VariableInfos = new List<VariableInfo>();
            PropertyInfos = new List<PropertyInfo>();
        }

        public void SetPartial(bool _value)
        {
            isPartial = _value;
        }

        public void SetNameSpace(string _value)
        {
            NameSpace = _value;
        }

        public void SetClassName(string _value)
        {
            ClassName = _value;
        }


        public void AddBaseType(string BaseType)
        {
            if (BaseTypes == null)
                BaseTypes = new List<string>();

            BaseTypes.Add(BaseType);
        }
        public void AddClassCommentStatement(string _value, bool _docComment = true)
        {
            if (ClassComments == null)
                ClassComments = new List<CodeCommentStatement>();
            ClassComments.Add(new CodeCommentStatement(_value, _docComment));
        }

        public void AddVariable(VariableInfo variableInfo)
        {
            if (VariableInfos == null)
                VariableInfos = new List<VariableInfo>();

            VariableInfos.Add(variableInfo);
        }

        public void SetMemberAttributes(MemberAttributes memberAttributes)
        {
            ClassmemberAttributes = memberAttributes;
        }

        public void AddAttribute(AttributeInfo attributeInfo)
        {
            if (AttributeInfos == null)
                AttributeInfos = new List<AttributeInfo>();

            AttributeInfos.Add(attributeInfo);
        }

        public void AddProperty(PropertyInfo property)
        {
            if (PropertyInfos == null)
                PropertyInfos = new List<PropertyInfo>();

            PropertyInfos.Add(property);
        }

        public void AddMethodInofs(MethodInfo metInfo)
        {
            if (MethodInfos == null)
                MethodInfos = new List<MethodInfo>();

            MethodInfos.Add(metInfo);
        }

        public void AddUsing(string _value)
        {
            if (UsingList == null)
                UsingList = new List<string>();
            UsingList.Add(_value);
        }

        public void Create(string ScriptPath)
        {
            var compileUnit = new CodeCompileUnit();
            var codeNameSpace = new CodeNamespace(NameSpace);
            compileUnit.Namespaces.Add(codeNameSpace);

            foreach (var usingType in UsingList)
            {
                codeNameSpace.Imports.Add(new CodeNamespaceImport(usingType));
            }

            var codeType = new CodeTypeDeclaration(ClassName);
            codeNameSpace.Types.Add(codeType);

            if(ClassComments != null)
            {
                codeType.Comments.AddRange(ClassComments.ToArray());
            }

            foreach (var baseType in BaseTypes)
            {
                codeType.BaseTypes.Add(new CodeTypeReference(baseType));
            }

            AddAttritype(codeType.CustomAttributes, AttributeInfos);

            foreach (var VariableType in VariableInfos)
            {
                CodeMemberField decl = new CodeMemberField() { Type = new CodeTypeReference(VariableType.Type), Name = VariableType.Name, Attributes = VariableType.MethodAtt };
                AddAttritype(decl.CustomAttributes, VariableType.AttributeInfos);
                codeType.Members.Add(decl);
            }

            foreach (var property in PropertyInfos)
            {
                CodeMemberProperty codeMemberProperty = new CodeMemberProperty();
                codeMemberProperty.Name = property.Name;
                codeMemberProperty.Type = new CodeTypeReference(property.Type);
                codeMemberProperty.Attributes = property.MethodAtt;

                if (property.GetStatementsStr != null && property.GetStatementsStr.Count > 0)
                {
                    for (int i = 0; i < property.GetStatementsStr.Count; i++)
                    {
                        codeMemberProperty.GetStatements.Add(new CodeSnippetStatement(property.GetStatementsStr[i]));
                    }
                }
                else
                {
                    codeMemberProperty.GetStatements.Add(new CodeMethodReturnStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), property.GetStatements.Name)));
                }
                if (property.SetStatements != null)
                {
                    codeMemberProperty.SetStatements.Add(new CodeAssignStatement(new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), property.SetStatements.Name), new CodePropertySetValueReferenceExpression()) { });
                }
                codeType.Members.Add(codeMemberProperty);
            }

            codeType.IsPartial = isPartial;
            codeType.Attributes = ClassmemberAttributes;

            foreach (var MemberType in MethodInfos)
            {
                CodeMemberMethod MethTemp = new CodeMemberMethod() { Name = MemberType.MethodName, Attributes = MemberType.MethodAtt };
                foreach (var parameter in MemberType.Parameters)
                {
                    CodeParameterDeclarationExpression p = new CodeParameterDeclarationExpression();
                    p.Name = parameter.Name;
                    p.Type = new CodeTypeReference(parameter.Type);

                    if (parameter.DefaultValue != null)
                    {

                    }
                    MethTemp.Parameters.Add(p);
                }
                for (int i = 0; i < MemberType.MethodStatements.Count; i++)
                {
                    MethTemp.Statements.Add(new CodeSnippetStatement(MemberType.MethodStatements[i]));
                }
                MethTemp.ReturnType = new CodeTypeReference(MemberType.ReturnType);
                codeType.Members.Add(MethTemp);
            }
            var provider = new CSharpCodeProvider();
            var options = new CodeGeneratorOptions();
            options.BlankLinesBetweenMembers = BlankLinesBetweenMembers;
            options.BracingStyle = CodeType;
            StreamWriter writer = new StreamWriter(Path.GetFullPath(ScriptPath),false,Encoding.UTF8);
   //         StreamWriter writer = new StreamWriter(File.Open(Path.GetFullPath(ScriptPath), FileMode.));
            provider.GenerateCodeFromCompileUnit(compileUnit, writer, options);
            writer.Close();

        }

        void AddAttritype(CodeAttributeDeclarationCollection codeAttributeDeclarationCollection, List<AttributeInfo> attributeInfo)
        {
            foreach (var AttriType in attributeInfo)
            {
                CodeAttributeDeclaration att;
                if (AttriType.value != null)
                {
                    CodeAttributeArgument codeAttr = new CodeAttributeArgument(new CodePrimitiveExpression(AttriType.value));
                    att = new CodeAttributeDeclaration(new CodeTypeReference(AttriType.Type), codeAttr);
                }
                else
                {
                    att = new CodeAttributeDeclaration(new CodeTypeReference(AttriType.Type));
                }
                codeAttributeDeclarationCollection.Add(att);
            }
        }
    }
}