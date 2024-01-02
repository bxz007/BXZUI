using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class)]
public class SourceAttribute : Attribute
{
    public string SourcePath { get; set; }
    
    public SourceAttribute(string value)
    {
        SourcePath = value;
    }

}
