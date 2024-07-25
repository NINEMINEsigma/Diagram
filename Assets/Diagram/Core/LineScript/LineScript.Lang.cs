using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Diagram.Arithmetic;
using Unity.VisualScripting;
using UnityEngine;

#pragma warning disable IDE1006 // 命名样式

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
    }

    public abstract class SystemKeyWord : LineWord
    {
        public override bool IsKeyWord => true;
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
                    foreach (var method in type.GetMethods())
                    {
                        core.CreatedSymbols.TryAdd(method.Name, new FunctionSymbolWord(method.Name, class_name, new(method, method.GetParameters().Length)));
                        string method_name = "";
                        foreach (var parameter in method.GetParameters())
                        {
                            method_name += "_" + parameter.ParameterType.Name;
                        }
                        core.CreatedSymbols.Add(class_name + "." + method.Name + method_name, 
                            new FunctionSymbolWord(method.Name, core.MainUsingInstances[class_name], new(method, method.GetParameters().Length)));
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
        /// If the literal-value or symbol-word is equal to 0, the result is false
        /// </summary>
        public class if_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                throw new NotImplementedException();
                return next;
            }
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
        public class while_Key : SystemKeyWord
        {
            public override bool AllowLinkLiteralValue => true;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                throw new NotImplementedException();
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>break</b></list>
        /// Exit the current block immediately
        /// </summary>
        public class break_Key : SystemKeyWord
        {
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                throw new NotImplementedException();
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>continue</b></list>
        /// Immediately move to the tail of the current block
        /// </summary>
        public class continue_Key : SystemKeyWord
        {
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                throw new NotImplementedException();
                return next;
            }
        }
        /// <summary>
        /// <list type="bullet"><b>define</b> <see langword="symbol"/>(will be defined) literal-value/symbol-word</list>
        /// <list type="bullet"><b>define</b>(<see langword="symbol"/>) literal-value/symbol-word</list>
        /// Define a reference for aim word
        /// <list type="bullet"><b>define</b> <see langword="symbol"/> = literal-value</list>
        /// Define a expression on <see cref="Diagram.Arithmetic.ArithmeticExtension"/>
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
                    if (next is SymbolWord.OperatorKeyWord.OperatorAssign eqaulword)
                    {
                        is_equals_define = true;
                        return this;
                    }
                    else if (next is ReferenceSymbolWord symbol)
                    {
                        core.CreatedSymbols[this.symbol_name] = symbol;
                        this.symbol_name = null;
                    }
                    else if (next is LiteralValueWord literal)
                    {
                        core.CreatedInstances[this.symbol_name] = literal.Source;
                        this.symbol_name = null;
                    }
                    else if (next is SymbolWord dsy)
                    {
                        core.CreatedSymbols[this.symbol_name] = dsy;
                        this.symbol_name = null;
                    }
                    else
                    {
                        this.symbol_name = null;
                        throw new ParseException($"Unknown Parse Way On: define({this.symbol_name}) {next}");
                    }
                    return this;
                }
                else
                {
                    if (core.CreatedInstances.TryGetValue(next.As<SourceValueWord>().Source, out var obj) &&
                        DiagramType.CreateDiagramType(obj.GetType()).Share(out var dtype).IsPrimitive &&
                        dtype.IsValueType && dtype.type != typeof(char))
                    {
                        core.CreatedInstances[this.symbol_name] = obj.ToString();
                        symbol_name.InsertVariable(obj.ToString());
                    }
                    else if (obj is string)
                    {
                        core.CreatedInstances[this.symbol_name] = obj.ToString();
                        symbol_name.InsertVariable(obj.ToString());
                    }
                    else if (next is LiteralValueWord literal)
                    {
                        core.CreatedInstances[this.symbol_name] = literal.Source;
                        symbol_name.InsertVariable(literal.Source);
                    }
                    else symbol_name.InsertVariable(next.As<SourceValueWord>().Source);
                    this.symbol_name = null;
                    return next;
                }
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
            private int max = 0;
            private string name = "";
            private string classname = "";
            private object[] constructorslist = null;
            private ParameterInfo[] parameterInfos = null;
            public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
            {
                if (counter == 0)
                {
                    name = next.As<SourceValueWord>().Source;
                    counter++;
                    return this;
                }
                else if (counter == 1)
                {
                    classname = next.As<SourceValueWord>().Source;
                    Type type = ReflectionExtension.Typen(classname);
                    parameterInfos = type.GetConstructors()[0].GetParameters();
                    max = parameterInfos.Length;
                    constructorslist = new object[max];
                    counter++;
                    return this;
                }
                else if (counter - 2 < max)
                {
                    do
                    {
                        int index = counter - 2;
                        if (next is SymbolWord symbol)
                        {
                            if (core.MainUsingInstances.TryGetValue(symbol.source, out object main)) constructorslist[index] = main;
                            else if (core.CreatedInstances.TryGetValue(symbol.source, out object inst)) constructorslist[index] = inst;
                            else if (core.CreatedSymbols.TryGetValue(symbol.source, out var in_symbolWord))
                            {
                                if (parameterInfos[index].ParameterType == typeof(string)) constructorslist[index] = in_symbolWord.source;
                                else if (parameterInfos[index].ParameterType == typeof(double)) constructorslist[index] = in_symbolWord.source.Compute();
                                else if (parameterInfos[index].ParameterType == typeof(float)) constructorslist[index] = in_symbolWord.source.Computef();
                                else if (parameterInfos[index].ParameterType == typeof(int)) constructorslist[index] = in_symbolWord.source.Computei();
                                else if (parameterInfos[index].ParameterType == typeof(long)) constructorslist[index] = in_symbolWord.source.Computel();
                                else if (core.MainUsingInstances.TryGetValue(in_symbolWord.source, out object main2)) constructorslist[index] = main2;
                                else if (core.CreatedInstances.TryGetValue(in_symbolWord.source, out object inst2)) constructorslist[index] = inst2;
                                else break;
                            }
                        }
                        else if (next is LiteralValueWord literal)
                        {
                            if (parameterInfos[index].ParameterType == typeof(string)) constructorslist[index] = literal.source;
                            else if (parameterInfos[index].ParameterType == typeof(double)) constructorslist[index] = literal.source.Compute();
                            else if (parameterInfos[index].ParameterType == typeof(float)) constructorslist[index] = literal.source.Computef();
                            else if (parameterInfos[index].ParameterType == typeof(int)) constructorslist[index] = literal.source.Computei();
                            else if (parameterInfos[index].ParameterType == typeof(long)) constructorslist[index] = literal.source.Computel();
                            else break;
                        }
                        else break;
                        counter++;
                        return this;
                    } while (false);
                    counter = 0;
                    throw new ParseException(name + " is cannt created", classname);
                }
                counter = 0;
                if (name == "_")
                {
                    name = classname + "#@@" + UnityEngine.Random.value.ToString() + ":" + core.CreatedInstances.Count.ToString();
                }
                foreach (var assembly in Diagram.ReflectionExtension.GetAssemblies())
                {
                    if (assembly.CreateInstance(classname, false, ReflectionExtension.DefaultBindingFlags, null, constructorslist, null, null).Share(out var obj) != null)
                    {
                        core.CreatedInstances[name] = obj;
                        break;
                    }
                }
                this.constructorslist = null;
                this.max = 0;
                return next;
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
                else if(next is import_Key)
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

            public void OpenCall(LineScript core,string scriptStrings, params (string, object)[] args)
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

        public class OperatorKeyWord : SymbolWord
        {
            public OperatorKeyWord(string source) : base(source) { }
            public class OperatorEqual : OperatorKeyWord { private OperatorEqual() : base("==") { } public readonly static OperatorEqual instance = new(); }
            public class OperatorAssign : OperatorKeyWord { private OperatorAssign() : base("=") { } public readonly static OperatorAssign instance = new(); }
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
                    else return false;
                }
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
            core.CreatedInstances["@result"] = this.AnyFunctional.Invoke(CoreInvokerInstance, this.constructorslist);
            this.constructorslist = new object[AnyFunctional.ArgsTotal];
            return new ReferenceSymbolWord("@result");
        }
    }

    public class ReferenceSymbolWord : SymbolWord
    {
        public override bool AllowLinkKeyWord => true;
        public override bool AllowLinkSymbolWord => true;
        public override bool AllowLinkLiteralValue => true;
        public FunctionSymbolWord Functional;
        public object ReferenceInstance;
        bool isOperator = false;
        public ReferenceSymbolWord(string source) : base(source) { }

        public override LineWord ResolveToBehaviour(LineScript core, LineWord next)
        {
            if (next is SymbolWord.OperatorKeyWord.OperatorPointTo)
            {
                this.isOperator = true;
                if (ReferenceInstance == null)
                    core.CreatedInstances.TryGetValue(this.Source, out ReferenceInstance);
                return this;
            } 
            if (isOperator)
            {
                isOperator = false;
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
                    return this;
                }
                throw new ParseException($"Error Parse->{str}");
            }
            else
            {
                if (Functional != null)
                {
                    return Functional.ResolveToBehaviour(core, next);
                }
                else if (next == null)
                {
                    core.CreatedInstances["@result"] = ReferenceInstance;
                    return next;
                }
                throw new ParseException("Unknown Parse way");
            }
        }
    }
}

#pragma warning restore IDE1006 // 命名样式
