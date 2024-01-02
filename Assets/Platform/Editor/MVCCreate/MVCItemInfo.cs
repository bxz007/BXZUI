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

namespace GalaFramework
{
    [System.Serializable]
    public class MVCInfo
    {
        public string       MvcName = "default";
        public string       ModuleName = "default";
        public bool         Controll;
        public bool         View = true;
        public ViewType     viewType;
        public bool         Model;
        public Object       Obj;
        public bool         isSelect;
        public bool         isHotfix = true;
        [System.NonSerialized]
        public bool         Created = false;
    }

}