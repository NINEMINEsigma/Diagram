using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditorInternal;
using UnityEngine;

namespace UnityEditorInternal { }

namespace Diagram
{
    public static class InternalUitility
    {
        public static void SetTag(GameObject obj,string tag)
        {
            if (InternalEditorUtility.tags.Contains(tag) == false)
                InternalEditorUtility.AddTag(tag);
            obj.tag = tag;
        }
    }
}
