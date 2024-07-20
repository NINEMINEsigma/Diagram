using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Diagram
{
    public static class LineLanguageUnit
    {
        public static void InitLineLanguage()
        {
            LineWord.WordPairs = new()
            {
                { "using",new SystemKeyWord.using_Key()},
                { "import",new SystemKeyWord.import_Key()},
                { "if",new SystemKeyWord.if_Key()},
                { "else",new SystemKeyWord.else_Key()},
                { "while",new SystemKeyWord.while_Key()},
                { "for",new SystemKeyWord.for_Key()},
                { "break",new SystemKeyWord.break_Key()},
                { "continue",new SystemKeyWord.continue_Key()},
                { "define",new SystemKeyWord.define_Key()}
            };
        }
    }

    public abstract class LineWord
    {
        public static Dictionary<string, LineWord> WordPairs = new();
        public virtual bool IsLiteralValue { get => false; }
        public virtual bool IsKeyWord { get => false; }
        public virtual bool IsSymbolWord { get => false; }
        public virtual bool AllowLinkLiteralValue { get => false; }
        public virtual bool AllowLinkKeyWord { get => false; }
        public virtual bool AllowLinkSymbolWord { get => false; }
        public static LineWord Read(string source)
        {
            if (source[0]=='\"')
            {
                if (source[^1] == '\"') return new LiteralValueWord(source[1..^1]);
                else throw new ParseException(source);
            }
            else if(WordPairs.TryGetValue(source,out var word))
            {
                return word;
            }
            else
            {
                return SymbolWord.GetSymbolWord(source);
            }
        }
        public bool DetectNext(LineWord next)
        {
            if (next.IsSymbolWord == true && this.AllowLinkSymbolWord==false) return false;
            if (next.IsKeyWord == true && this.AllowLinkKeyWord==false) return false;
            if (next.IsLiteralValue == true && this.AllowLinkLiteralValue == false) return false;
            return true;
        }
    }
    public abstract class SystemKeyWord : LineWord
    {
        public override bool IsKeyWord => true;
        public class using_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
        public class import_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
        public class if_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
        public class else_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; public override bool AllowLinkKeyWord => true; }
        public class while_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
        public class for_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
        public class break_Key : SystemKeyWord { }
        public class continue_Key : SystemKeyWord { }
        public class define_Key : SystemKeyWord { public override bool AllowLinkLiteralValue => true; }
    }

    public class LiteralValueWord : LineWord
    {
        public string source;
        public override bool IsLiteralValue => true;
        public override bool AllowLinkKeyWord => true;
        public LiteralValueWord(string source)
        {
            this.source = source;
        }
    }

    public class SymbolWord:LineWord
    {
        public string source;
        public override bool IsSymbolWord => true;
        public override bool AllowLinkKeyWord => true;
        public SymbolWord(string source)
        {
            this.source = source;
        }

        public class OperatorKeyWord:SymbolWord
        {
            public OperatorKeyWord(string source) : base(source) { }
        }

        public static SymbolWord GetSymbolWord(string source)
        {
            return new SymbolWord(source);
        }
    }
}
