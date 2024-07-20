using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diagram
{
    public abstract class LineWord
    {
        public static Dictionary<string, LineWord> WordPairs = new();
        public bool IsLiteralValue { get; protected set; } = false;
        public bool IsKeyWord { get; protected set; } = false;
        public bool IsSymbolWord { get; protected set; } = false;
        public bool AllowLinkLiteralValue { get; protected set; } = false;
        public bool AllowLinkKeyWord { get; protected set; } = false;
        public bool AllowLinkSymbolWord { get; protected set; } = false;
    }
    public abstract class SystemKeyWord : LineWord
    {
        public class Using:SystemKeyWord
        {
            public Using()
            {

            }
        }
    }
}
