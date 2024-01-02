using System;


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class InjectAttribute : Attribute
{
    public string MvvmName { get; private set; }
    public bool IsCommon { get; private set; }

    public InjectAttribute(string name = "", bool isCommon = false)
    {
        this.MvvmName = name;
        this.IsCommon = isCommon;
    }
}

