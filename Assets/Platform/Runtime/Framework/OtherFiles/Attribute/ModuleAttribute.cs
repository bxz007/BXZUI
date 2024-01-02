using System;

[AttributeUsage (AttributeTargets.Class)]
public class ModuleAttribute : Attribute {
    public string ModuleName { get; set; }

    public ModuleAttribute (string Value) {
        ModuleName = Value;
    }
}

[AttributeUsage (AttributeTargets.Class)]
public class MvcAttribute : Attribute {
    public string MvcName { get; set; }

    public MvcAttribute (string Value) {
        MvcName = Value;
    }
}

[AttributeUsage (AttributeTargets.Class)]
public class NetMessageAttribute : Attribute {
    public string NetName { get; set; }
    public int CreateType { get; set; }

    public NetMessageAttribute (Type Value, int type = 0) {
        NetName = Value.Name;
        CreateType = type;
    }
}