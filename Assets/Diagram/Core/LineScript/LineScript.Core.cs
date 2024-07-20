using System.Collections;
using System.Collections.Generic;
using System.IO; 
using UnityEditor.AssetImporters;
using UnityEngine;

namespace Diagram
{

    [System.Serializable]
    public class LineScriptException : DiagramException
    {
        public LineScriptException() { }
        public LineScriptException(string message) : base(message) { }
        public LineScriptException(string message, System.Exception inner) : base(message, inner) { }
        protected LineScriptException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [System.Serializable]
    public class ParseException : LineScriptException
    {
        public ParseException(string message) : base("Bad Parse: " + message) { }
    }

    public class LineScript
    {
        #region env
        public Dictionary<string, object> CreatedInstances = new();
        #endregion

        public void Run(LineScriptAssets ls)
        {
            Run(ls.text);
        }
        public void Run(string ls)
        {
            CoreRun(ls.Split('\n'));
        }
        private void CoreRun(string[] ls)
        {
            foreach (var line in ls)
            {
                List<string> words = new();
                string current = "";
                bool isnot_in_literal_value = false;
                bool isnot_skip = false;
                foreach (var ch in line)
                {
                    switch (ch)
                    {
                        case ' ' when isnot_in_literal_value&& isnot_skip:
                            {
                                if (current.Length != 0)
                                    words.Add(current);
                                current = "";
                            }
                            break;
                        case '\"' when isnot_in_literal_value&& isnot_skip:
                            {
                                isnot_in_literal_value = true;
                                current += '\"';
                            }
                            break;
                        case '\"' when isnot_in_literal_value==false && isnot_skip:
                            {
                                isnot_in_literal_value = false;
                                current += '\"';
                            }
                            break;
                        case '\\' when isnot_in_literal_value==false && isnot_skip:
                            {
                                isnot_skip = true;
                                current += '\\';
                            }
                            break;
                        default:
                            current += ch;
                            isnot_skip = false;
                            break;
                    }
                }
                if (current.Length > 0) words.Add(current);
                CoreLineParse(words.ToArray());
            }
        }
        private void CoreLineParse(string[] words)
        {
            if (words.Length == 0) return;
            List<LineWord> LineWords = new();
            LineWord Controller = AllowFirstWord.StaticInstance;
            foreach (var word in words)
            {
                LineWord lineWord = LineWord.Read(word);
                if (Controller.DetectNext(lineWord))
                {
                    //TODO
                }
                else throw new ParseException($"\"{word}\" is not allow");
                Controller = lineWord;
            }
        }
        private class AllowFirstWord:LineWord
        {
            public override bool AllowLinkKeyWord => true;
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkSymbolWord => true;
            public readonly static AllowFirstWord StaticInstance = new AllowFirstWord();
        }
    }

    [ScriptedImporter(2, ".ls")]
    public class LineScriptImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var lineTxt = File.ReadAllText(ctx.assetPath);

            Debug.Log("Import:" + ctx.assetPath);
            //转化为TextAsset，也可写个LuaAsset的类作为保存对象，但要继承Object的类
            var assetsText = new LineScriptAssets(lineTxt);

            ctx.AddObjectToAsset("main obj", assetsText, Resources.Load<Texture2D>("Editor/Icon/LineScript"));
            ctx.SetMainObject(assetsText);
        }
    }

    public class LineScriptAssets : TextAsset
    {
        public LineScriptAssets() : base("") { }
        public LineScriptAssets(string lines) : base(lines) { }


    }
}
