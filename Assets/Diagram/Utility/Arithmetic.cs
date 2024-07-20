using System;
using System.Collections.Generic;
using System.Data;
using static Diagram.Arithmetic.ArithmeticException;

namespace Diagram.Arithmetic
{
    [Serializable]
    public class ArithmeticException : DiagramException
    {
        public ArithmeticException(string message) : base(message) { }
        public ArithmeticException(string message, Exception inner) : base(message, inner) { }
        protected ArithmeticException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }

        [Serializable]
        public class VariableExistException : ArithmeticException
        {
            public VariableExistException() : base("Variable Is Exist") { }
        }
        [Serializable]
        public class ParseException : ArithmeticException
        {
            public ParseException() : base("String Cannt Parse") { }
        }
    }

    public static class ArithmeticExtension
    {

        public static void InitArithmeticExtension()
        {
            DataTableHelper = new();
            ArithmeticVariable = new();
            ArithmeticMathFunction = new();
            foreach (var func in typeof(System.Math).GetMethods())
            {
                if (func.ReturnType == typeof(double) && func.GetParameters().Length == 1 && func.GetParameters()[0].ParameterType == typeof(double))
                {
                    ArithmeticMathFunction.Add(func.Name, (double value) => (double)func.Invoke(null, value.ToObjectArray()));
                }
            }
            ArithmeticExpressionFunction = new();
        }

        private static DataTable DataTableHelper = new();
        private static Dictionary<string, string> ArithmeticVariable = new();
        private static Dictionary<string, Func<double, double>> ArithmeticMathFunction = new();
        private static Dictionary<string, Func<string, string>> ArithmeticExpressionFunction = new();

        public static InsertResult RegisterVariable(this string self, string expression)
        {
            if (ArithmeticVariable.ContainsKey(self))
            {
                return InsertResult.IsFailed;
            }
            ArithmeticVariable[self] = expression;
            return InsertResult.IsSucceed;
        }
        public static InsertResult InsertVariable(this string self, string expression)
        {
            if (ArithmeticVariable.ContainsKey(self))
            {
                string temp = ArithmeticVariable[self];
                ArithmeticVariable[self] = expression;
                return new InsertResult.Replace(temp);
            }
            ArithmeticVariable[self] = expression;
            return InsertResult.IsSucceed;
        }
        public static InsertResult RegisterVariable(this string self, float expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult InsertVariable(this string self, float expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult RegisterVariable(this string self, double expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult InsertVariable(this string self, double expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult RegisterVariable(this string self, int expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult InsertVariable(this string self, int expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult RegisterVariable(this string self, long expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult InsertVariable(this string self, long expression)
        {
            return RegisterVariable(self, expression.ToString());
        }
        public static InsertResult RemoveVariable(this string self)
        {
            return ArithmeticVariable.Remove(self) ? InsertResult.IsSucceed : InsertResult.IsFailed;
        }

        public static InsertResult RegisterFunction(this string self, Func<double, double> expression)
        {
            if (ArithmeticMathFunction.ContainsKey(self))
            {
                return InsertResult.IsFailed;
            }
            ArithmeticMathFunction[self] = expression;
            return InsertResult.IsSucceed;
        }
        public static InsertResult InsertFunction(this string self, Func<double, double> expression)
        {
            if (ArithmeticMathFunction.ContainsKey(self))
            {
                var temp = ArithmeticMathFunction[self];
                ArithmeticMathFunction[self] = expression;
                return new InsertResult.Replace(temp);
            }
            ArithmeticMathFunction[self] = expression;
            return InsertResult.IsSucceed;
        }

        public static InsertResult RemoveFunction(this string self)
        {
            return ArithmeticMathFunction.Remove(self) ? InsertResult.IsSucceed : (ArithmeticExpressionFunction.Remove(self) ? InsertResult.IsSucceed : InsertResult.IsFailed);
        }

        public static InsertResult RegisterFunction(this string self, Func<string, string> expression)
        {
            if (ArithmeticExpressionFunction.ContainsKey(self))
            {
                return InsertResult.IsFailed;
            }
            ArithmeticExpressionFunction[self] = expression;
            return InsertResult.IsSucceed;
        }
        public static InsertResult InsertFunction(this string self, Func<string, string> expression)
        {
            if (ArithmeticExpressionFunction.ContainsKey(self))
            {
                var temp = ArithmeticExpressionFunction[self];
                ArithmeticExpressionFunction[self] = expression;
                return new InsertResult.Replace(temp);
            }
            ArithmeticExpressionFunction[self] = expression;
            return InsertResult.IsSucceed;
        }

        public static double Compute(this string self)
        {
            string expression = ToolReplace(self);
            return Convert.ToDouble(DataTableHelper.Compute(expression, ""));
        }
        public static float Computef(this string self)
        {
            string expression = ToolReplace(self);
            return (float)Convert.ToDouble(DataTableHelper.Compute(expression, ""));
        }
        public static int Computei(this string self)
        {
            string expression = ToolReplace(self);
            return (int)Convert.ToInt32(DataTableHelper.Compute(expression, ""));
        }
        public static long Computel(this string self)
        {
            string expression = ToolReplace(self);
            return (long)Convert.ToInt64(DataTableHelper.Compute(expression, ""));
        }

        private static string ToolReplace(string expression)
        {
            string result = expression;
            bool isContinue = false;
            do
            {
                isContinue = false;
                //Variable 
                foreach (var item in ArithmeticVariable)
                {
                    if (result.IndexOf(item.Key, StringComparison.CurrentCultureIgnoreCase).Share(out var index) != -1)
                    {
                        result = expression.Replace(item.Key, "(" + item.Value + ")");
                        isContinue = true;
                    }
                }
                //Function
                foreach (var item in ArithmeticMathFunction)
                {
                    if (result.IndexOf(item.Key, StringComparison.CurrentCultureIgnoreCase).Share(out var index) != -1)
                    {
                        result = result.Remove(index, item.Key.Length);
                        if (result[index] != '(') throw new ParseException(); else result = result.Remove(index, 1);
                        string temp = "";
                        for (int i = index; i < result.Length && result[i] != ')'; /*i++*/)
                        {
                            temp += result[i];
                            result = result.Remove(i, 1);
                        }
                        if (result[index] != ')') throw new ParseException(); else result = result.Remove(index, 1);
                        result = result.Insert(index, "(" + item.Value.Invoke(temp.Compute()).ToString() + ")");
                        isContinue = true;
                    }
                }
                //Function2
                foreach (var item in ArithmeticExpressionFunction)
                {
                    if (result.IndexOf(item.Key, StringComparison.CurrentCultureIgnoreCase).Share(out var index) != -1)
                    {
                        result = result.Remove(index, item.Key.Length);
                        if (result[index] != '(') throw new ParseException(); else result = result.Remove(index, 1);
                        string temp = "";
                        for (int i = index; i < result.Length && result[i] != ')';  /*i++*/)
                        {
                            temp += result[i];
                            result = result.Remove(i, 1);
                        }
                        if (result[index] != ')') throw new ParseException(); else result = result.Remove(index, 1);
                        result = result.Insert(index, "(" + item.Value.Invoke(temp) + ")");
                        isContinue = true;
                    }
                }
            } while (isContinue);
            return result;
        }
    }
}
