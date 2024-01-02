using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace GalaFramework
{
    public interface IWindows
    {
        Rect position { get; set; }
        int Orde { get; set; }
        void Init(Action Repaint);
        void OnGUI(Rect rect);
        void OnClose();
    }

    public class MvcWinodwsAttribute : Attribute {
        public int Orde { get; set; }
        public MvcWinodwsAttribute(int level)
        {
            Orde = level;
        }
    }

}