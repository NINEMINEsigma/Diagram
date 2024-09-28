using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if UNITY_EDITOR
using UnityEditorInternal;
#endif
using UnityEngine;

namespace UnityEditorInternal { }

namespace Diagram
{
    public static class InternalUitility
    {
        public static void SetTag(GameObject obj,string tag)
        {
#if UNITY_EDITOR
            if (InternalEditorUtility.tags.Contains(tag) == false)
                InternalEditorUtility.AddTag(tag);
#endif
            obj.tag = tag;
        }
    }
}
