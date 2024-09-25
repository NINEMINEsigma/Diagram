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

    /// <summary>
    /// Divide strings by line separators with spaces, construct morphemes(<see cref="LineWord"/>), and execute reflection-based scripting language <see langword="LineScript.Lang"/>
    /// </summary>
    public class LineScript
    {
        public static string BinPath = "";
        public static string IntervalResultVarName = "@result";

        #region Buildup
        /// <summary>
        /// Obtain a UTF8(<see cref="System.Text.Encoding.UTF8"/>) encoded text file in 
        /// the specified path<code>Path.Combine(<see cref="BinPath"/>, path)</code><para></para>
        /// and generate it if it does not exist
        /// </summary>
        public static LineScript GetScript(string path, out string str, params (string, object)[] values)
        {
            if (BinPath == "Resource")
            {
                str = Resources.Load<TextAsset>(path).text;
                return new LineScript(values);
            }
            else
            {
                using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
                str = file.GetString(false, System.Text.Encoding.UTF8);
                return new LineScript(values);
            }
        }
        /// <summary>
        /// Obtain a UTF8(<see cref="System.Text.Encoding.UTF8"/>) encoded text file in the specified path <code>Path.Combine(<see cref="BinPath"/>, path)</code><para></para>
        /// and generate it if it does not exist, then immediately execute the <see cref="LineScript"/> code that should exist in it
        /// </summary>
        public static LineScript RunScript(string path, params (string, object)[] values)
        {
            if (BinPath == "Resource")
            {
                new LineScript(values).Share(out var script).Run(Resources.Load<TextAsset>(path).text);
                return script;
            }
            else
            {
                using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
                new LineScript(values).Share(out var script).Run(file.GetString(false, System.Text.Encoding.UTF8));
                return script;
            }
        }
        /// <summary>
        /// Obtain a UTF8(<see cref="System.Text.Encoding.UTF8"/>) encoded text file in the specified path <code>Path.Combine(<see cref="BinPath"/>, path)</code><para></para>
        /// and generate it if it does not exist, then immediately execute the <see cref="LineScript"/> code that should exist in it
        /// </summary>
        public LineScript ImportScriptAndRun(string path)
        {
            if (BinPath == "Resource")
            {
                this.Run(Resources.Load<TextAsset>(path).text);
                return this;
            }
            else
            {
                using ToolFile file = new(Path.Combine(BinPath, path), true, true, false);
                this.Run(file.GetString(false, System.Text.Encoding.UTF8));
                return this;
            }
        }
        [_Init_]
        public LineScript(params (string, object)[] createdInstances)
        {
            this.CurrentControlKey = new();
            CurrentControlKey.Push(new(new SystemKeyWord.if_Key(), 0, true));
            foreach (var item in createdInstances)
            {
                CreatedInstances.TryAdd(item.Item1, item.Item2);
            }
        }
        #endregion

        #region env
        public Dictionary<string, object> MainUsingInstances = new();
        public Dictionary<string, object> CreatedInstances = new();
        public Dictionary<string, SymbolWord> CreatedSymbols = new();
        public Dictionary<string, Type> Typedefineds = new();
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

        #region Run
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
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
        }
        public static event Action<LineScript, bool> LineScriptRuntimeEvent;
        private void CoreRun(string[] ls)
        {
            LineScriptRuntimeEvent?.Invoke(this, true);
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
                CoreLineParse(ref i, words.ToArray());
            }
            LineScriptRuntimeEvent?.Invoke(this, false);
        }
        internal int CurrentLineindex = 0;
        internal class ControlLine
        {
            public SystemKeyWord word;
            public int line;
            public bool stats;

            public ControlLine(SystemKeyWord word, int line, bool stats)
            {
                this.word = word;
                this.line = line;
                this.stats = stats;
            }
        }
        internal Stack<ControlLine> CurrentControlKey = new();
        /// <summary>
        /// Convert words to morphemes(<see cref="LineWord"/>) and perform linear <see langword="inspection"/> and <see langword="execution"/> from left to right<para></para>
        /// Use the <see cref="CurrentControlKey"/> to control the control-layer<para></para>
        /// <b>Important</b>: Exceptions are considered errors
        /// </summary>
        /// <exception cref="ParseException">This error is thrown when the input does not meet the syntax requirements</exception>
        private void CoreLineParse(ref int lineindex, string[] words)
        {
            //Start Env
            if (words.Length > 0)
            {
                if (words[0].Length > 0 && words[0].StartsWith("//")) return;
                words[0] = words[0].TrimStart(' ');
                words[^1] = words[^1].TrimEnd(' ', '\r');
            }
            if (words.Length == 0) return;
            List<LineWord> LineWords = new();
            LineWord Controller = AllowFirstWord.StaticInstance;

            //Core Behaviour Loop
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;
                BuildEnv(words, i, ref lineindex, out LineWord lineWord);
                try
                {
                    if (CurrentControlKey.Peek().stats ||
                        lineWord is SystemKeyWord.end_key)
                    {
                        if (Controller.DetectNext(lineWord))
                            Controller = Controller.ResolveToBehaviour(this, lineWord);
                        else
                            ThrowBadParse(lineindex, words, Controller, i, lineWord);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    ThrowBadParse(lineindex, words, Controller, i, lineWord);
                }
                ApplyEnv(ref lineindex, ref lineWord);
            }

            //Last Behaviour
            BuildEnv(words, words.Length, ref lineindex);
            Controller.ResolveToBehaviour(this, null);
            ApplyEnv(ref lineindex);

            static void ThrowBadParse(int lineindex, string[] words, LineWord Controller, int i, LineWord lineWord)
            {
                throw new ParseException($"On {lineindex} {i}:...{(i > 0 ? words[i - 1] + "<" + Controller.GetType().Name + ">" : "")} {words[i]}<{lineWord.GetType().Name}>... is throw error");
            }
        }
        #endregion

        #region Utility
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
            else if (CreatedInstances.TryGetValue(source, out var symbol)) return new ReferenceSymbolWord(source);
            return new LiteralValueWord(source);
        }

        /// <summary>
        /// Used to create and save environments<para></para>
        /// <b>Related to this:</b><see cref="BuildEnv(string[], int, ref int)"/>
        /// </summary>
        private void BuildEnv([_In_] string[] words, [_In_] int i, [_In_] ref int lineindex, [_Out_] out LineWord lineWord)
        {
            //build next lineword
            lineWord = LineWord.Read(this, words[i]);
            //setup forward lineword
            lineWord.ForwardInformation = i > 0 ? words[i - 1] : null;

            BuildEnv(words, i, ref lineindex);
        }
        /// <summary>
        /// Used to identify and restore the environment<para></para>
        /// <b>Related to this:</b><see cref="ApplyEnv(ref int)"/>
        /// </summary>
        private void ApplyEnv([_In_] ref int lineindex, [_In_] ref LineWord lineWord)
        {
            //setup forward lineword
            lineWord.ForwardInformation = null;

            ApplyEnv(ref lineindex);
        }
        /// <summary>
        /// Used for isolated create and save environments
        /// </summary>
        private void BuildEnv([_In_] string[] words, [_In_] int i, [_In_] ref int lineindex)
        {
            //build current env
            CurrentLineindex = lineindex;
        }
        /// <summary>
        /// For siloed validation and recovery environments
        /// </summary>
        private void ApplyEnv([_In_] ref int lineindex)
        {
            //rebuild lineindex
            lineindex = CurrentLineindex;
        }
        #endregion
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
