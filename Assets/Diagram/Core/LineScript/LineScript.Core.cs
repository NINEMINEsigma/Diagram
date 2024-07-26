using System;
using System.Collections.Generic;
using System.IO;
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
        public ParseException() : base("Bad Parse") { }
        public ParseException(string message) : base("Bad Parse->" + message) { }
        public ParseException(string message,string word) : base("Bad Parse->" + message) { this.word = word; }
        public string word;
    }

    public class LineScript
    {
        public static string BinPath = "";
        public static LineScript GetScript(string path, out string str, params (string, object)[] values)
        {
            using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
            str = file.GetString(false, System.Text.Encoding.UTF8);
            return new LineScript(values);
        }
        public static LineScript RunScript(string path,params (string, object)[] values)
        {
            using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
            new LineScript(values).Share(out var script).Run(file.GetString(false, System.Text.Encoding.UTF8));
            return script;
        }
        public LineScript ReadAndRun(string path)
        {
            using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
            this.Run(file.GetString(false, System.Text.Encoding.UTF8));
            return this;
        }
        public LineScript(params (string, object)[] createdInstances)
        {
            foreach (var item in createdInstances)
            {
                CreatedInstances.TryAdd(item.Item1, item.Item2);
            }
        }

        #region env
        public Dictionary<string, object> MainUsingInstances = new();
        public Dictionary<string, object> CreatedInstances = new();
        public Dictionary<string, SymbolWord> CreatedSymbols = new();
        #endregion

        /// <summary>
        /// Add a sub <see cref="LineScript"/> core
        /// </summary>
        /// <param name="core"></param>
        public void SubLineScript(LineScript core)
        {
            foreach (var item in core.MainUsingInstances)
            {
                this.MainUsingInstances.TryAdd(item.Key, item.Value);
            }
            foreach (var item in core.CreatedInstances)
            {
                this.CreatedInstances.TryAdd(item.Key, item.Value);
            }
            foreach (var item in core.CreatedSymbols)
            {
                this.CreatedSymbols.TryAdd(item.Key, item.Value);
            }
        }

        public void Run(LineScriptAssets ls)
        {
            Run(ls.text);
        }
        public void Run(string ls)
        {
            try
            {
                CoreRun(ls.Split('\n'));
            }
            catch(Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        private void CoreRun(string[] ls)
        {
            for (int i = 0; i < ls.Length; i++)
            {
                string line = ls[i];
                List<string> words = new();
                string current = "";
                bool isnot_in_literal_value = true;
                bool isnot_in_argslist = true;
                bool isnot_skip = true;
                foreach (var ch in line)
                {
                    switch (ch)
                    {
                        case ' ' when isnot_in_literal_value && isnot_in_argslist && isnot_skip:
                            {
                                if (current.Length != 0)
                                    words.Add(current);
                                current = "";
                            }
                            break;
                        case ',' when isnot_in_literal_value && isnot_in_argslist == false && isnot_skip:
                            {
                                if (current.Length != 0)
                                    words.Add(current);
                                current = "";
                            }
                            break;
                        case '\"' when isnot_in_literal_value /*&& isnot_in_argslist*/ && isnot_skip:
                            {
                                isnot_in_literal_value = false;
                                current += '\"';
                            }
                            break;
                        case '\"' when isnot_in_literal_value == false /*&& isnot_in_argslist*/ && isnot_skip:
                            {
                                isnot_in_literal_value = true;
                                current += '\"';
                            }
                            break;
                        case '\\' when isnot_in_literal_value == false && isnot_in_argslist && isnot_skip:
                            {
                                isnot_skip = false;
                                //current += '\\';
                            }
                            break;
                        case '(' when isnot_in_literal_value && isnot_in_argslist && isnot_skip:
                            {
                                isnot_in_argslist = false;
                                if (current.Length != 0)
                                    words.Add(current);
                                current = "";
                            }
                            break;
                        case ')' when isnot_in_literal_value && isnot_in_argslist == false && isnot_skip:
                            {
                                isnot_in_argslist = true;
                                if (current.Length != 0)
                                    words.Add(current);
                                current = "";
                            }
                            break;
                        default:
                            current += ch;
                            isnot_skip = true;
                            break;
                    }
                }
                if (current.Length > 0) words.Add(current);
                CoreLineParse(i, words.ToArray());
            }
        }
        private void CoreLineParse(int lineindex,string[] words)
        {
            if(words.Length>0)
            {
                if (words[0].Length > 0 && words[0].StartsWith("//")) return;
                words[0] = words[0].TrimStart(' ');
                words[^1] = words[^1].TrimEnd(' ', '\r');
            }
            if (words.Length == 0) return;
            List<LineWord> LineWords = new();
            LineWord Controller = AllowFirstWord.StaticInstance;
            for (int i = 0; i < words.Length; i++)
            {
                string word = words[i];
                if (word.Length == 0) continue;
                LineWord lineWord = LineWord.Read(this, word);
                lineWord.ForwardInformation = i > 0 ? words[i - 1] : null;
                try
                {
                    if (Controller.DetectNext(lineWord))
                    {
                        Controller = Controller.ResolveToBehaviour(this, lineWord);
                    }
                    else throw new ParseException($"On {lineindex} {i}:...{(i > 0 ? words[i - 1] + "<" + Controller.GetType().Name + ">" : "")} {word}<{lineWord.GetType().Name}>... is not allow");
                }
                catch(Exception ex)
                {
                    Debug.LogException(ex);
                    throw new ParseException($"On {lineindex} {i}:...{(i > 0 ? words[i - 1] + "<" + Controller.GetType().Name + ">" : "")} {word}<{lineWord.GetType().Name}>... is throw error");
                }
                lineWord.ForwardInformation = null;
            }
            Controller.ResolveToBehaviour(this,null);
        }
        private class AllowFirstWord : LineWord
        {
            public override bool AllowLinkKeyWord => true;
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkSymbolWord => true;
            public readonly static AllowFirstWord StaticInstance = new AllowFirstWord();
        }

        /// <summary>
        /// If it's already a defined word, it is parsed as a <see cref="SymbolWord"/>, otherwise it is parsed as a <see cref="LiteralValueWord"/>
        /// </summary>
        public LineWord GetSymbolWord(string source)
        {
            if (SymbolWord.GetSymbolWord(source, out var result)) return result;
            else if (CreatedSymbols.TryGetValue(source, out result)) return result; 
            else if(CreatedInstances.TryGetValue(source,out var symbol)) return new ReferenceSymbolWord(source);
            return new LiteralValueWord(source);
        }
    }

#if UNITY_EDITOR
    [UnityEditor.AssetImporters.ScriptedImporter(1, "ls")]
    public class LineScriptImporter : UnityEditor.AssetImporters.ScriptedImporter
    {
        public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
        {
            var lineTxt = File.ReadAllText(ctx.assetPath);

            Debug.Log("Import:" + ctx.assetPath);
            //转化为TextAsset，也可写个LuaAsset的类作为保存对象，但要继承Object的类
            var assetsText = new LineScriptAssets(lineTxt);

            ctx.AddObjectToAsset("main obj", assetsText, Resources.Load<Texture2D>("Editor/Icon/LineScript"));
            ctx.SetMainObject(assetsText);
        }
    }
#endif

    public class LineScriptAssets : TextAsset
    {
        public LineScriptAssets() : base("") { }
        public LineScriptAssets(string lines) : base(lines) { }
    }
}
