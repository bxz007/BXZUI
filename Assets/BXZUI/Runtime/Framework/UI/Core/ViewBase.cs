using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace BUI
{
    public class ViewBase
    {
        protected Transform transform;
        protected  virtual void Init( object data)
        {
             
        }
        protected virtual void ReleaseComponent()
        {
           
        }
    }
}
