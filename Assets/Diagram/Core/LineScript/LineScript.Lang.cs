using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Diagram.Arithmetic;
using Unity.VisualScripting;
using UnityEngine;
using static Diagram.ReflectionExtension;

#pragma warning disable IDE1006

namespace Diagram
{
    public abstract class LineWord
    {
        public static Dictionary<string, LineWord> WordPairs = new();
        public virtual bool IsLiteralValue { get => false; }
        public virtual bool IsKeyWord { get => false; }
        public virtual bool IsSymbolWord { get => false; }
        public virtual bool AllowLinkLiteralValue { get => false; }
        public virtual bool AllowLinkKeyWord { get => false; }
        public virtual bool AllowLinkSymbolWord { get => false; }
        /// <summary>
        /// Transform to target type's <see cref="LineWord"/>
        /// </summary>
        /// <exception cref="ParseException"></exception>
        public static LineWord Read(LineScript core, string source)
        {
            if (source[0] == '\"')
            {
                if (source[^1] == '\"') return new LiteralValueWord(source[1..^1]);
                else throw new ParseException(source);
            }
            else if (WordPairs.TryGetValue(source, out var word))
            {
                return word;
            }
            else
            {
                return core.GetSymbolWord(source);
            }
        }
        /// <summary>
        /// Whether the next word is a legitimate type
        /// </summary> 
        public bool DetectNext(LineWord next)
        {
            if (next.IsSymbolWord == true && this.AllowLinkSymbolWord == false) return false;
            if (next.IsKeyWord == true && this.AllowLinkKeyWord == false) return false;
            if (next.IsLiteralValue == true && this.AllowLinkLiteralValue == false) return false;
            return true;
        }
        /// <summary>
        /// Transform the word into an executable act
        /// </summary>
        /// <returns>Next controller word(is this? is next? or a new one)</returns>
        public virtual LineWord ResolveToBehaviour(LineScript core, LineWord next) { return next; }

        public string ForwardInformation;
    }

    public abstract class SystemKeyWord : LineWord
    {
        public override bool IsKeyWord => true;
        public class ControllerKeyWord : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkSymbolWord => true;

            protected bool ToolSet(LineScript core,object input,out bool result)
            {
                result = false;
                try
                {
                    if (input is string strword)
                    {
                        if (strword.TryCompute(out var dr)) result = dr != 0;
                        else if (strword.TryComputef(out var fr)) result = fr != 0;
                        else if (strword.TryComputei(out var ir)) result = ir != 0;
                        else if (strword.TryComputel(out var lr)) result = lr != 0;
                        else if (strword == "false" && strword == "False" && strword == "0") result = false;
                        else if (strword == "true" && strword == "True") result = false;
                        else result = false;
                    }
                    else if (input is bool)
                        result = (bool)input;
                    else
                        result = input.Equals(0) == false;
                }
                catch
                {
                    return false;
                }
                return true;
            }

            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if (next is LiteralValueWord lvw)
                {
                    ToolCheck(core, lvw.Source);
                }
                else if(next is SymbolWord symbol)
                {
                    if (core.CreatedInstances.ContainsKey(symbol.Source))
                    {
                        ToolCheck(core, symbol.Source);
                    }
                } 
                return next;

                void ToolCheck(LineScript core, string judgment)
                {
                    try
                    {
                        core.CurrentControlKey.Push(new(this, core.CurrentLineindex, Mathf.Approximately(judgment.Computef(), 0.0f) == false));
                    }
                    catch
                    {
                        core.CurrentControlKey.Push(new(this, core.CurrentLineindex, false == (judgment == "false" || judgment == "False" || judgment.Trim(' ') == "0")));
                    }
                }
            }
        }

        /// <summary>
        /// <list type="bullet"><b>using</b> class-name</list>
        /// In this script, this class will support functions and fields, which is needed
        /// </summary>
        public class using_Key : SystemKeyWord
        {
            public override bool AllowLinkSymbolWord => true;
            public override bool AllowLinkLiteralValue => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                string class_name = next.As<SourceValueWord>().Source;
                Type type = ReflectionExtension.Typen(class_name);
                if (type != null)
                {
                    object obj = type.IsStatic() ? null : System.Activator.CreateInstance(type);
                    core.MainUsingInstances.Add(class_name, obj);
                    foreach (var method in type.GetMethods(type.IsStatic() ? BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public : AllBindingFlags))
                    {
                        core.CreatedSymbols.TryAdd(method.Name, new FunctionSymbolWord(method.Name, core.MainUsingInstances[class_name], new(method, method.GetParameters().Length)));
                        string method_name = "";
                        foreach (var parameter in method.GetParameters())
                        {
                            method_name += "_" + (parameter.ParameterType == typeof(UnityEngine.Object) ? "UObject" : parameter.ParameterType.Name);
                        }
                        try
                        {
                            core.CreatedSymbols.Add(class_name + "." + method.Name + method_name,
                                new FunctionSymbolWord(method.Name, core.MainUsingInstances[class_name], new(method, method.GetParameters().Length)));
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                    return next;
                }
                throw new ParseException(class_name + " is cannt found");
            }
        }
        /// <summary>
        /// <list type="bullet"><b>import</b> script</list>
        /// Reference another script, and the code for that script will be sub script
        /// </summary>
        public class import_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                LineScript subcore = new();
                string path = next.As<SourceValueWord>().Source;
                using ToolFile file = new ToolFile(Path.Combine(LineScript.BinPath, path), false, true, true);
                subcore.Run(file.GetString(false, System.Text.Encoding.UTF8));
                core.SubLineScript(subcore);
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>include</b> script</list>
        /// Reference another script, and the code for that script will replace this line
        /// </summary>
        public class include_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                string path = next.As<SourceValueWord>().Source;
                using ToolFile file = new ToolFile(Path.Combine(LineScript.BinPath, path), false, true, true);
                core.Run(file.GetString(false, System.Text.Encoding.UTF8));
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>if</b> literal-value/symbol-word</list>
        /// If the literal-value or symbol-word is equal to 0 or "false" or "False", the result is false
        /// </summary>
        public class if_Key : ControllerKeyWord
        {

        }
        /// <summary>
        /// <list type="bullet"><b>else</b> <see langword="if"/></list>
        /// Else is always equivalent to a correct if, However, if the previous statement is executed from within the if block, the block will be ignored
        /// </summary>
        public class else_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true; public override bool AllowLinkKeyWord => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                throw new NotImplementedException();
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>while</b> literal-value/symbol-word</list>
        /// Exits only if the literal-value or symbol-word is equal to 0
        /// </summary>
        public class while_Key : ControllerKeyWord
        {

        }
        /// <summary>
        /// <list type="bullet"><b>continue</b></list>
        /// Immediately move to the tail of the current block
        /// </summary>
        public class continue_Key : SystemKeyWord
        {
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                core.CurrentControlKey.Peek().stats = false;
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>define</b> <see langword="symbol"/>(will be defined) literal-value/symbol-word</list>
        /// <list type="bullet"><b>define</b>(<see langword="symbol"/>) literal-value/symbol-word</list>
        /// Define a reference for aim word
        /// <list type="bullet"><b>define</b> <see langword="symbol"/> = literal-value</list>
        /// Define a expression on <see cref="Diagram.Arithmetic.ArithmeticExtension"/>
        /// <list type="bullet"><b>define</b> <see langword="symbol"/> = <see langword="typen"/></list>
        /// Define a alias of typen
        /// </summary>
        public class define_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkSymbolWord => true;

            private string symbol_name = null;
            private bool is_equals_define = false;

            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if (symbol_name == null)
                {
                    this.symbol_name = next.As<SourceValueWord>().Source;
                    is_equals_define = false;
                    return this;
                }
                else if (is_equals_define == false)
                {
                    string symbol_name = this.symbol_name;
                    this.symbol_name = null;
                    if (next is SymbolWord.OperatorKeyWord.OperatorAssign eqaulword)
                    {
                        is_equals_define = true;
                        return this;
                    }
                    else if (next is ReferenceSymbolWord symbol)
                    {
                        core.CreatedSymbols[symbol_name] = symbol;
                    }
                    else if (next is SourceValueWord svw)
                    {
                        if (Typen(svw.Source).Share(out var definedTypenAlias)!=null)
                        {
                            core.Typedefineds[symbol_name] = definedTypenAlias;
                        }
                        else if (svw is LiteralValueWord literal)
                        {
                            core.CreatedInstances[symbol_name] = literal.Source;
                        }
                        else if (svw is SymbolWord dsy)
                        {
                            core.CreatedSymbols[symbol_name] = dsy;
                        }
                        else
                        {
                            throw new ParseException($"Unknown Parse Way On: define({this.symbol_name}) {next}");
                        }
                    }
                    else
                    {
                        throw new ParseException($"Unknown Parse Way On: define({this.symbol_name}) {next}");
                    }
                    return this;
                }
                else
                {
                    NewDefineUsingAssign(core, next, this.symbol_name);
                    this.symbol_name = null;
                    return next;
                }
            }

            public static void NewDefineUsingAssign(LineScript core, LineWord next, string symbol_name)
            {
                if (core.CreatedInstances.TryGetValue(next.As<SourceValueWord>().Source, out var obj) &&
                    DiagramType.GetOrCreateDiagramType(obj.GetType()).Share(out var dtype).IsPrimitive &&
                    dtype.IsValueType && dtype.type != typeof(char))
                {
                    core.CreatedInstances[symbol_name] = obj.ToString();
                    symbol_name.InsertVariable(obj.ToString());
                }
                else if (obj is string)
                {
                    core.CreatedInstances[symbol_name] = obj.ToString();
                    symbol_name.InsertVariable(obj.ToString());
                }
                else if (next is LiteralValueWord literal)
                {
                    core.CreatedInstances[symbol_name] = literal.Source;
                    symbol_name.InsertVariable(literal.Source);
                }
                else symbol_name.InsertVariable(next.As<SourceValueWord>().Source);
            }
        }
        /// <summary>
        /// <b>Target class-type is recommended to have only one constructor</b>
        /// <list type="bullet"><b>new</b>(<see langword="symbol"/>) class-name([literal-value/symbol-word])</list>
        /// Generate a new instance of target type and named <see langword="symbol"/>, arguments is optional
        /// <list type="bullet"><b>new</b>(<see langword="_"/>) class-name([literal-value/symbol-word])</list>
        /// Generate a new anonymity instance of target type, arguments is optional
        /// </summary>
        public class new_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkSymbolWord => true;
            private int counter = 0;
            private string name = "";
            private string classname = "";
            private List<string> constructorslist = new();

            private object[] TransfromConstructorParametors(LineScript core)
            {
                return constructorslist.GetSubList<object, string>(T => true, T =>
                {
                    string str = null;
                    if (core.CreatedInstances.ContainsKey(T))
                        return core.CreatedInstances[T];
                    else if (core.CreatedSymbols.ContainsKey(T))
                    {
                        if (core.CreatedInstances.ContainsKey(core.CreatedSymbols[T].Source))
                            return core.CreatedInstances[core.CreatedSymbols[T].Source];
                        else
                        {
                            str = core.CreatedSymbols[T].Source;
                        }
                    }
                    str ??= T;
                    if (str.TryComputef(out var value))
                        return value;
                    else
                        return str;
                }).ToArray();
            }

            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if (counter == 0)
                {
                    name = next.As<SourceValueWord>().Source;
                    constructorslist.Clear();
                    counter++;
                    return this;
                }
                else if (counter == 1)
                {
                    classname = next.As<SourceValueWord>().Source;
                    counter++;
                    return this;
                }
                else if (counter == 2 && next != null)
                {
                    constructorslist.Add(next.As<SourceValueWord>().Source);
                    return this;
                }
                else if (next == null)
                {
                    counter = 0;
                    if (name == "_")
                        name = classname + "#@@" + UnityEngine.Random.value.ToString() + ":" + core.CreatedInstances.Count.ToString();
                    if (core.Typedefineds.ContainsKey(classname))
                        classname = core.Typedefineds[classname].FullName;
                    object[] constructorParamters = TransfromConstructorParametors(core);
                    foreach (var assembly in Diagram.ReflectionExtension.GetAssemblies())
                    {
                        if (assembly.CreateInstance(classname, false, ReflectionExtension.DefaultBindingFlags, null, constructorParamters, null, null).Share(out var obj) != null)
                        {
                            core.CreatedInstances[name] = obj;
                            break;
                        }
                    }
                    return next;
                }
                else return this;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>delete</b> symbol-word</list>
        /// Try to remove one reference of target core
        /// </summary>
        public class delete_Key : SystemKeyWord
        {
            public override bool AllowLinkSymbolWord => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                next.As<SourceValueWord>().Source.Share(out var source);
                if (core.MainUsingInstances.Remove(source)) { }
                else if (core.CreatedInstances.Remove(source)) { }
                else if (core.CreatedSymbols.Remove(source)) { }
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>call</b> script <see langword="import"/> (arg-name=arg-value)...</list>
        /// Reference other script and pass in parameters, it will run in a new core
        /// </summary>
        public class call_key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override bool AllowLinkKeyWord => true;
            public override bool AllowLinkSymbolWord => true;

            private List<string> scriptNames = new();
            private List<(string, object)> args = new();
            private bool is_args_import = false;

            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if (next == null)
                {
                    LineScript subScript = new LineScript
                    {
                        CreatedInstances = new(core.CreatedInstances),
                        CreatedSymbols = new(core.CreatedSymbols),
                        MainUsingInstances = new(core.MainUsingInstances)
                    };
                    is_args_import = false;
                    foreach (var arg in args)
                    {
                        subScript.CreatedInstances[arg.Item1] = arg.Item2;
                    }
                    args.Clear();
                    var temp = scriptNames.ToList();
                    scriptNames.Clear();
                    foreach (var scriptPath in temp)
                    {
                        using ToolFile file = new(Path.Combine(LineScript.BinPath, scriptPath), false, true, false);
                        if (file)
                            subScript.Run(file.GetString(false, System.Text.Encoding.UTF8));
                    }
                    core.SubLineScript(subScript);
                }
                else if (next is import_Key)
                {
                    is_args_import = true;
                    return this;
                }
                else if (is_args_import == false)
                {
                    if (next.As<SourceValueWord>(out var svw))
                        scriptNames.Add(svw.Source);
                    else
                        throw new ParseException("Need Path of LineScript, but current is " + next.GetType().Name);
                }
                else
                {
                    string[] strs = next.As<SourceValueWord>().Source.TrimStart('(', ' ').TrimEnd(')', ' ').Split('=');
                    if (strs.Length != 2) throw new ParseException($"{next.As<SourceValueWord>().Source}Args format is wrong");
                    args.Add((strs[0].Trim(' '), strs[1].Trim(' ')));
                }
                return this;
            }

            public void OpenCall(LineScript core, string scriptStrings, params (string, object)[] args)
            {
                var tempScriptNames = this.scriptNames.ToList();
                var tempArgs = this.args.ToList();
                this.scriptNames = new();
                this.args = new();
                is_args_import = false;
                LineScript subScript = new LineScript
                {
                    CreatedInstances = new(core.CreatedInstances),
                    CreatedSymbols = new(core.CreatedSymbols),
                    MainUsingInstances = new(core.MainUsingInstances)
                };
                foreach (var arg in args)
                {
                    subScript.CreatedInstances[arg.Item1] = arg.Item2;
                }
                subScript.Run(scriptStrings);
                this.scriptNames = tempScriptNames;
                this.args = tempArgs;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>end</b> block-name</list>
        /// End block
        /// </summary>
        public class end_key : SystemKeyWord
        {
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                var control_line = core.CurrentControlKey.Pop();
                Type control_type = control_line.word.GetType();
                if (control_type == typeof(while_Key) && control_line.stats)
                {
                    //Because the outer loop uses for and is advanced with i++, -1 is needed here
                    core.CurrentLineindex = control_line.line - 1;
                }
                return this;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>set</b> belong [-> belong...] = <see langword="value"/></list>
        /// Set object value
        /// </summary>
        public class set_key : SystemKeyWord
        {
            public override bool AllowLinkSymbolWord => true;
            public override bool AllowLinkLiteralValue => true;
            DiagramMember member = null;
            object instance = null;
            string symbol_name = null;
            bool IsAssign = false;
            DiagramMember right_member = null;
            object right_instance = null;
            string right_symbol_name = null;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if(next is SymbolWord.OperatorKeyWord.OperatorPointTo)
                    return this;
                if (next == null)
                {
                    SetValue();
                    ResetLeft();
                }
                else
                    EasyOpt
                        .MultIf(IsAssign, (ref DiagramMember member, ref object instance, ref string symbol_name) =>
                        {
                            string source = next.As<SourceValueWord>().Source;
                            if (instance == null)
                            {
                                symbol_name = source;
                            }
                            else if (member == null)
                            {
                                member = DiagramType.GetOrCreateDiagramType(instance.GetType()).GetMember(source);
                            }
                            else
                            {
                                instance = member.reflectedMember.GetValue(instance);
                                member = DiagramType.GetOrCreateDiagramType(instance.GetType()).GetMember(source);
                            }
                        }, ref right_member, ref right_instance, ref right_symbol_name)
                        .MultIf((next is SymbolWord.OperatorKeyWord.OperatorAssign) == false, ref member, ref instance, ref symbol_name)
                        .Else(() =>
                        {
                            if (next is SymbolWord.OperatorKeyWord.OperatorAssign)
                            {
                                IsAssign = true;
                                ResetRight();
                            }
                            else
                                throw new ParseException();
                        });
                return this;

                void SetValue()
                {
                    object right_value = right_member == null ? core.CreatedInstances[right_symbol_name] : right_member.reflectedMember.GetValue(right_instance);
                    if(member==null)
                    {
                        core.CreatedInstances[symbol_name] = right_value;
                    }
                    else
                    {
                        member.reflectedMember.SetValue(instance, right_value);
                    }
                }
                void ResetLeft()
                {
                    member = null;
                    instance = null;
                    symbol_name = null;
                }
                void ResetRight()
                {
                    right_member = null;
                    right_instance = null;
                    right_symbol_name = null;
                }
            }
        }
    }

    public abstract class SourceValueWord : LineWord { public abstract string Source { get; } }

    public class LiteralValueWord : SourceValueWord
    {
        public string source;
        public override string Source => source;
        public override bool IsLiteralValue => true;
        public override bool AllowLinkKeyWord => true;
        public LiteralValueWord(string source)
        {
            this.source = source;
        }
    }

    public class SymbolWord : SourceValueWord
    {
        public string source;
        public override string Source => source;
        public override bool IsSymbolWord => true;
        public override bool AllowLinkKeyWord => true;
        public SymbolWord(string source)
        {
            this.source = source;
        }

        protected bool ToolSet(LineScript core, string name, string strword)
        {
            try
            {
                Type targetType = core.CreatedInstances[name].GetType();
                object result = null;
                if (targetType == typeof(double)) result = strword.Compute();
                else if (targetType == typeof(float)) result = strword.Computef();
                else if (targetType == typeof(int)) result = strword.Computei();
                else if (targetType == typeof(long)) result = strword.Computel();
                else if (targetType == typeof(bool)) result = strword != "false" && strword != "False" && strword != "0";
                else if (targetType == typeof(Vector4))
                {
                    var strs = strword.Split(',');
                    result = new Vector4(strs[0].Computef(), strs[1].Computef(), strs[2].Computef(), strs[3].Computef());
                }
                else if (targetType == typeof(Vector3))
                {
                    var strs = strword.Split(',');
                    result = new Vector3(strs[0].Computef(), strs[1].Computef(), strs[2].Computef());
                }
                else if (targetType == typeof(Vector2))
                {
                    var strs = strword.Split(',');
                    result = new Vector2(strs[0].Computef(), strs[1].Computef());
                }
                else if (targetType == typeof(object)) result = strword;
                else result = strword;
                core.CreatedInstances[name] = result;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public class OperatorKeyWord : SymbolWord
        {
            public OperatorKeyWord(string source) : base(source) { }
            public class OperatorEqual : OperatorKeyWord { private OperatorEqual() : base("==") { } public readonly static OperatorEqual instance = new(); }
            public class OperatorAssign : OperatorKeyWord
            {
                public override bool AllowLinkLiteralValue => true;
                public override bool AllowLinkSymbolWord => true;
                private OperatorAssign() : base("=") { }
                public readonly static OperatorAssign instance = new();
                public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
                {
                    if (string.IsNullOrEmpty(this.ForwardInformation) != false)
                        throw new ParseException("Left word is missing");
                    else
                    {
                        next.As<SourceValueWord>(out var svw);
                        float tcvalue = 0;
                        if (
                            core.CreatedInstances.TryGetValue(this.ForwardInformation, out var obj) &&
                            DiagramType.GetOrCreateDiagramType(obj.GetType()).Share(out var dtype).IsPrimitive &&
                            dtype.IsValueType && dtype.type != typeof(char))
                        {
                            if (obj is string)
                            {
                                core.CreatedInstances[this.ForwardInformation] = svw.Source;
                                this.ForwardInformation.InsertVariable(svw.Source);
                            }
                            else if (obj is bool)
                            {
                                bool temp = svw.Source.TryComputef(out var result) && (Mathf.Approximately(result, 0));
                                core.CreatedInstances[this.ForwardInformation] = temp;
                                this.ForwardInformation.InsertVariable(temp ? "1" : "0");
                            }
                            if (svw.Source.TryComputef(out tcvalue))
                            {
                                core.CreatedInstances[this.ForwardInformation] = tcvalue;
                                this.ForwardInformation.InsertVariable(tcvalue.ToString());
                            }
                            else
                            {
                                core.CreatedInstances[this.ForwardInformation] = svw.Source;
                                this.ForwardInformation.InsertVariable(svw.Source);
                            }
                        }
                        else if (svw.Source.TryComputef(out tcvalue))
                        {
                            core.CreatedInstances[this.ForwardInformation] = tcvalue;
                            this.ForwardInformation.InsertVariable(tcvalue.ToString());
                        }
                        else// if (obj is string)
                        {
                            core.CreatedInstances[this.ForwardInformation] = svw.Source;
                            this.ForwardInformation.InsertVariable(svw.Source);
                        }
                        //else if (next is LiteralValueWord literal)
                        //{
                        //    core.CreatedInstances[this.ForwardInformation] = literal.Source;
                        //    this.ForwardInformation.InsertVariable(literal.Source);
                        //}
                        //else this.ForwardInformation.InsertVariable(next.As<SourceValueWord>().Source);
                        //else throw new BadImplemented();
                    }
                    return next;
                }
            }
            public class OperatorGreater : OperatorKeyWord { private OperatorGreater() : base(">") { } public readonly static OperatorGreater instance = new(); }
            public class OperatorPointTo : OperatorKeyWord { private OperatorPointTo() : base("->") { } public readonly static OperatorPointTo instance = new(); }
            public static bool GetOperatorSymbolWord(string source, out SymbolWord symbol)
            {
                symbol = null;
                switch (source)
                {
                    case "==":
                        {
                            symbol = OperatorEqual.instance;
                            return true;
                        }
                    case "=":
                        {
                            symbol = OperatorAssign.instance;
                            return true;
                        }
                    case ">":
                        {
                            symbol = OperatorGreater.instance;
                            return true;
                        }
                    case "->":
                        {
                            symbol = OperatorPointTo.instance;
                            return true;
                        }
                    default:
                        break;
                }
                return false;
            }
        }

        public static bool GetSymbolWord(string source, out SymbolWord symbol)
        {
            symbol = null;
            if (OperatorKeyWord.GetOperatorSymbolWord(source, out symbol)) return true;
            else return false;
        }
    }

    public class FunctionSymbolWord : SymbolWord
    {
        public override bool AllowLinkLiteralValue => true;
        public override bool AllowLinkSymbolWord => true;
        public override bool AllowLinkKeyWord => false;
        public ReflectionExtension.DiagramReflectedMethod AnyFunctional;
        public object CoreInvokerInstance;
        public FunctionSymbolWord(string source, object CoreInvokerInstance, ReflectionExtension.DiagramReflectedMethod method) : base(source)
        {
            this.AnyFunctional = method;
            this.constructorslist = new object[AnyFunctional.ArgsTotal];
            this.CoreInvokerInstance = CoreInvokerInstance;
        }
        //public FunctionSymbolWord(string source,object CoreInvokerInstance):base(source) 
        //{
        //    this.AnyFunctional = null;
        //    this.constructorslist = new object[AnyFunctional.ArgsTotal];
        //    this.CoreInvokerInstance = CoreInvokerInstance;
        //}

        private int counter = 0;
        private object[] constructorslist;

        private bool ToolSet(int index, object input)
        {
            try
            {
                if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == input.GetType()) this.constructorslist[index] = input;
                else if (input is string strword)
                {
                    if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(string)) this.constructorslist[index] = strword;
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(double)) this.constructorslist[index] = strword.Compute();
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(float)) this.constructorslist[index] = strword.Computef();
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(int)) this.constructorslist[index] = strword.Computei();
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(long)) this.constructorslist[index] = strword.Computel();
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(bool)) this.constructorslist[index] = strword != "false" && strword != "0";
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(Vector4))
                    {
                        var strs = strword.Split(',');
                        constructorslist[index] = new Vector4(strs[0].Computef(), strs[1].Computef(), strs[2].Computef(), strs[3].Computef());
                    }
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(Vector3))
                    {
                        var strs = strword.Split(',');
                        constructorslist[index] = new Vector3(strs[0].Computef(), strs[1].Computef(), strs[2].Computef());
                    }
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(Vector2))
                    {
                        var strs = strword.Split(',');
                        constructorslist[index] = new Vector2(strs[0].Computef(), strs[1].Computef());
                    }
                    else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(object)) this.constructorslist[index] = strword;
                    else return false;
                }
                else if (AnyFunctional.CoreMethod.GetParameters()[index].ParameterType == typeof(object)) this.constructorslist[index] = input;
                else return false;
            }
            catch
            {
                return false;
            }
            return true;
        }

        public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
        {
            //if(AnyFunctional==null)
            //{
            //    if(next is SymbolWord.OperatorKeyWord.OperatorPointTo)
            //    return false;
            //}
            if (counter < AnyFunctional.ArgsTotal)
            {
                do
                {
                    int index = counter;
                    if (next is SymbolWord symbol)
                    {
                        if (core.MainUsingInstances.TryGetValue(symbol.source, out object main) && ToolSet(index, main)) { }
                        else if (core.CreatedInstances.TryGetValue(symbol.source, out object inst) && ToolSet(index, inst)) { }
                        else if (core.CreatedSymbols.TryGetValue(symbol.source, out var in_symbolWord))
                        {
                            if (core.MainUsingInstances.TryGetValue(in_symbolWord.source, out object main2) && ToolSet(index, main2)) { }
                            else if (core.CreatedInstances.TryGetValue(in_symbolWord.source, out object inst2) && ToolSet(index, inst2)) { }
                            else if (ToolSet(index, in_symbolWord)) { }
                            else break;
                        }
                    }
                    else if (next is SourceValueWord source && ToolSet(index, source.Source)) { }
                    else break;
                    counter++;
                    return this;
                } while (false);
                counter = 0;
                throw new NotImplementedException();
            }
            counter = 0;
            core.CreatedInstances[LineScript.IntervalResultVarName] = this.AnyFunctional.Invoke(CoreInvokerInstance, this.constructorslist);
            this.constructorslist = new object[AnyFunctional.ArgsTotal];

            LineWord result = new ReferenceSymbolWord(LineScript.IntervalResultVarName);
            if (next != null)
            {
                result = result.ResolveToBehaviour(core, next);
            }
            return result;
        }
    }

    public class ReferenceSymbolWord : SymbolWord
    {
        public override bool AllowLinkKeyWord => true;
        public override bool AllowLinkSymbolWord => true;
        public override bool AllowLinkLiteralValue => true;
        public FunctionSymbolWord Functional;
        public object ReferenceInstance;
        OperatorKeyWord this_operKeyWord;
        public ReferenceSymbolWord(string source) : base(source) { }

        public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
        {
            OperatorKeyWord operKeyWord = this_operKeyWord;
            if (next == null)
            {
                this_operKeyWord = null;
                return this;
            }
            if (operKeyWord != null)
            {
                if (operKeyWord is SymbolWord.OperatorKeyWord.OperatorPointTo)
                {
                    if (Functional == null)
                    {
                        next.As<SourceValueWord>().Source.Share(out var str);
                        if (ReferenceInstance.GetType().GetMethods().FirstOrDefault(T => T.Name == str).Share(out var method) != null)
                        {
                            Functional = new(str, ReferenceInstance, new(method, method.GetParameters().Length));
                            return this;
                        }
                        else if (DiagramType.CreateDiagramType(ReferenceInstance.GetType()).GetMember(str).Share(out var member) != null)
                        {
                            ReferenceInstance = ReferenceInstance.GetFieldByName(str);
                            this.source = member.name;
                            this_operKeyWord = null;
                            return this;
                        }
#if UNITY_EDITOR
                        var testvar = DiagramType.CreateDiagramType(ReferenceInstance.GetType());
#endif
                        throw new ParseException($"Error Parse->{str}");
                    }
                    else
                    {
                        var result = Functional.ResolveToBehaviour(core, next);
                        if(result==Functional)
                        {
                            this_operKeyWord = null;
                            Functional = null;
                        }
                        return result;
                    }
                }
                else if (operKeyWord is SymbolWord.OperatorKeyWord.OperatorAssign assignOpt)
                {
                    /*if (core.CreatedInstances.ContainsKey(this.Source))
                    //{
                    //    if (next is ReferenceSymbolWord ref0)
                    //    {
                    //        core.CreatedInstances[this.Source] = core.CreatedInstances[ref0.Source];
                    //    }
                    //    else if (next is LiteralValueWord literalValue)
                    //    {
                    //        Debug.Log($"{this.source} = {literalValue.source}");
                    //        core.CreatedInstances[this.Source] = literalValue.Source;
                    //    }
                    //    else if(next is SymbolWord newsymbol)
                    //    {
                    //        if (core.CreatedInstances.TryGetValue(newsymbol.Source, out object target) == false)
                    //        {
                    //            if (core.CreatedSymbols.TryGetValue(newsymbol.Source, out var tempsymb) == false)
                    //            {
                    //                throw new ParseException($"Cannt Found {newsymbol.Source}");
                    //            }
                    //            else
                    //            {
                    //                return tempsymb;
                    //            }
                    //        }
                    //        else
                    //            core.CreatedInstances[this.Source] = target;
                    //    }
                    //    return this;
                    //}*/
                    assignOpt.ForwardInformation = this.Source;
                    return assignOpt.ResolveToBehaviour(core, next);
                }
            }
            else if (next is SymbolWord.OperatorKeyWord)
            {
                this_operKeyWord = next as OperatorKeyWord;
                ReferenceInstance ??= core.CreatedInstances[this.Source];
                return this;
            }
            throw new ParseException("Unknown Parse way");
        }
    }
}

#pragma warning restore IDE1006
