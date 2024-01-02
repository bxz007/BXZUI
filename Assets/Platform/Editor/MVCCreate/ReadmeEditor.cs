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
using UnityEditor;

public class ReadmeEditor 
{
    [MenuItem("Tools/MVCFramework_Readme")]
    static void OpenUrl()
    {
        Application.OpenURL("https://www.yuque.com/linzhihuan/galamvc/quickstart");
    }
}