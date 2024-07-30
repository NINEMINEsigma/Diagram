using System;
using System.Reflection;
using System.Reflection.Emit;
using Diagram.DeepReflection;

namespace Diagram.Emit
{
    /// <summary>
    /// Provides utilities for using the <see langword="System.Reflection.Emit"/> namespace.<para></para>
    /// This class is due for refactoring. Use at your own peril.
    /// </summary>
    public static class EmitUtilities
    {
        private static Assembly EditorAssembly = typeof(UnityEditor.Editor).Assembly;
        private static Assembly EngineAssembly = typeof(UnityEngine.Object).Assembly;

        /// <summary>
        /// Gets a value indicating whether emitting is supported on the current platform.
        /// true if the current platform can emit; otherwise, false.
        /// </summary>
        public static bool CanEmit => true;

        private static bool EmitIsIllegalForMember(MemberInfo member)
        {
            if (member.DeclaringType != null)
            {
                if (!(member.DeclaringType.Assembly == EditorAssembly))
                {
                    return member.DeclaringType.Assembly == EngineAssembly;
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a delegate which gets the value of a field. If emitting is not supported on the current platform, the delegate will use reflection to get the value.
        /// </summary>
        /// <typeparam name="FieldType">
        /// The type of the field to get a value from.
        /// </typeparam>
        /// <param name="fieldInfo">
        /// The <see cref="System.Reflection.FieldInfo"/> instance describing the field to create a getter for.
        /// </param>
        /// <returns>
        /// A delegate which gets the value of the given field.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// The fieldInfo parameter is null.
        /// </exception>
        public static Func<FieldType> CreateStaticFieldGetter<FieldType>(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException("fieldInfo");
            }

            if (!fieldInfo.IsStatic)
            {
                throw new ArgumentException("Field must be static.");
            }

            fieldInfo = fieldInfo.DeAliasField();
            if (fieldInfo.IsLiteral)
            {
                FieldType value = (FieldType)fieldInfo.GetValue(null);
                return () => value;
            }

            if (EmitIsIllegalForMember(fieldInfo))
            {
                return () => (FieldType)fieldInfo.GetValue(null);
            }

            string name = fieldInfo.ReflectedType.FullName + ".get_" + fieldInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, typeof(FieldType), new Type[0], restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldsfld, fieldInfo);
            iLGenerator.Emit(OpCodes.Ret);
            return (Func<FieldType>)dynamicMethod.CreateDelegate(typeof(Func<FieldType>));
        }

        /// <summary>
        /// Creates a delegate which gets the value of a field. If emitting is not supported on the current platform, the delegate will use reflection to get the value.
        /// </summary>
        /// <param name="fieldInfo">The <see cref="System.Reflection.FieldInfo"/> instance describing the field to create a getter for.</param>
        /// <returns>A delegate which gets the value of the given field.</returns>
        /// <exception cref="ArgumentNullException">The fieldInfo parameter is null.</exception>
        public static Func<object> CreateWeakStaticFieldGetter(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException("fieldInfo");
            }

            if (!fieldInfo.IsStatic)
            {
                throw new ArgumentException("Field must be static.");
            }

            fieldInfo = fieldInfo.DeAliasField();
            if (EmitIsIllegalForMember(fieldInfo))
            {
                return () => fieldInfo.GetValue(null);
            }

            string name = fieldInfo.ReflectedType.FullName + ".get_" + fieldInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, typeof(object), new Type[0], restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldsfld, fieldInfo);
            if (fieldInfo.FieldType.IsValueType)
            {
                iLGenerator.Emit(OpCodes.Box, fieldInfo.FieldType);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Func<object>)dynamicMethod.CreateDelegate(typeof(Func<object>));
        }

        /// <summary>
        /// Creates a delegate which sets the value of a field. If emitting is not supported on the current platform, the delegate will use reflection to set the value.
        /// </summary>
        /// <typeparam name="FieldType">The <see cref="System.Reflection.FieldInfo"/>  instance describing the field to create a setter for.</typeparam>
        /// <param name="fieldInfo">The type of the field to set a value to.</param>
        /// <returns>A delegate which sets the value of the given field.</returns>
        /// <exception cref="ArgumentNullException">The fieldInfo parameter is null.</exception>
        public static Action<FieldType> CreateStaticFieldSetter<FieldType>(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException("fieldInfo");
            }

            if (!fieldInfo.IsStatic)
            {
                throw new ArgumentException("Field must be static.");
            }

            fieldInfo = fieldInfo.DeAliasField();
            if (fieldInfo.IsLiteral)
            {
                throw new ArgumentException("Field cannot be constant.");
            }

            if (EmitIsIllegalForMember(fieldInfo))
            {
                return delegate (FieldType value)
                {
                    fieldInfo.SetValue(null, value);
                };
            }

            string name = fieldInfo.ReflectedType.FullName + ".set_" + fieldInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, null, new Type[1] { typeof(FieldType) }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            iLGenerator.Emit(OpCodes.Stsfld, fieldInfo);
            iLGenerator.Emit(OpCodes.Ret);
            return (Action<FieldType>)dynamicMethod.CreateDelegate(typeof(Action<FieldType>));
        }

        /// <summary>
        /// Creates a delegate which sets the value of a field. If emitting is not supported on the current platform, the delegate will use reflection to set the value.
        /// </summary>
        /// <param name="fieldInfo">The <see cref="System.Reflection.FieldInfo"/> instance describing the field to create a setter for.</param>
        /// <returns>A delegate which sets the value of the given field.</returns>
        /// <exception cref="ArgumentNullException">The fieldInfo parameter is null.</exception>
        public static Action<object> CreateWeakStaticFieldSetter(FieldInfo fieldInfo)
        {
            if (fieldInfo == null)
            {
                throw new ArgumentNullException("fieldInfo");
            }

            if (!fieldInfo.IsStatic)
            {
                throw new ArgumentException("Field must be static.");
            }

            fieldInfo = fieldInfo.DeAliasField();
            if (EmitIsIllegalForMember(fieldInfo))
            {
                return delegate (object value)
                {
                    fieldInfo.SetValue(null, value);
                };
            }

            string name = fieldInfo.ReflectedType.FullName + ".set_" + fieldInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, null, new Type[1] { typeof(object) }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_0);
            if (fieldInfo.FieldType.IsValueType)
            {
                iLGenerator.Emit(OpCodes.Unbox_Any, fieldInfo.FieldType);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Castclass, fieldInfo.FieldType);
            }

            iLGenerator.Emit(OpCodes.Stsfld, fieldInfo);
            iLGenerator.Emit(OpCodes.Ret);
            return (Action<object>)dynamicMethod.CreateDelegate(typeof(Action<object>));
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given parameterless instance method and returns the result.
        /// </summary>
        /// <typeparam name="InstanceType">The type of the class which the method is on.</typeparam>
        /// <typeparam name="ReturnType">The type which is returned by the given method info.</typeparam>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        public static Func<InstanceType, ReturnType> CreateMethodReturner<InstanceType, ReturnType>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            return (Func<InstanceType, ReturnType>)Delegate.CreateDelegate(typeof(Func<InstanceType, ReturnType>), methodInfo);
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given parameterless static method.
        /// </summary>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        public static Action CreateStaticMethodCaller(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (!methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is an instance method when it has to be static.");
            }

            if (methodInfo.GetParameters().Length != 0)
            {
                throw new ArgumentException("Given method cannot have any parameters.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            return (Action)Delegate.CreateDelegate(typeof(Action), methodInfo);
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given parameterless weakly typed instance method.
        /// </summary>
        /// <typeparam name="TArg1"></typeparam>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        public static Action<object, TArg1> CreateWeakInstanceMethodCaller<TArg1>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must have exactly one parameter.");
            }

            if (parameters[0].ParameterType != typeof(TArg1))
            {
                throw new ArgumentException("The first parameter of the method '" + methodInfo.Name + "' must be of type " + typeof(TArg1)?.ToString() + ".");
            }

            methodInfo = methodInfo.DeAliasMethod();
            if (EmitIsIllegalForMember(methodInfo))
            {
                return delegate (object classInstance, TArg1 arg)
                {
                    methodInfo.Invoke(classInstance, new object[1] { arg });
                };
            }

            Type declaringType = methodInfo.DeclaringType;
            string name = methodInfo.ReflectedType.FullName + ".call_" + methodInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, null, new Type[2]
            {
            typeof(object),
            typeof(TArg1)
            }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            if (declaringType.IsValueType)
            {
                LocalBuilder local = iLGenerator.DeclareLocal(declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Unbox_Any, declaringType);
                iLGenerator.Emit(OpCodes.Stloc, local);
                iLGenerator.Emit(OpCodes.Ldloca_S, local);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Castclass, declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Action<object, TArg1>)dynamicMethod.CreateDelegate(typeof(Action<object, TArg1>));
        }

        public static Action<object> CreateWeakInstanceMethodCaller(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.GetParameters().Length != 0)
            {
                throw new ArgumentException("Given method cannot have any parameters.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            if (EmitIsIllegalForMember(methodInfo))
            {
                return delegate (object classInstance)
                {
                    methodInfo.Invoke(classInstance, null);
                };
            }

            Type declaringType = methodInfo.DeclaringType;
            string name = methodInfo.ReflectedType.FullName + ".call_" + methodInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, null, new Type[1] { typeof(object) }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            if (declaringType.IsValueType)
            {
                LocalBuilder local = iLGenerator.DeclareLocal(declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Unbox_Any, declaringType);
                iLGenerator.Emit(OpCodes.Stloc, local);
                iLGenerator.Emit(OpCodes.Ldloca_S, local);
                iLGenerator.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Castclass, declaringType);
                iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            }

            if (methodInfo.ReturnType != null && methodInfo.ReturnType != typeof(void))
            {
                iLGenerator.Emit(OpCodes.Pop);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Action<object>)dynamicMethod.CreateDelegate(typeof(Action<object>));
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given weakly typed instance method with one argument and returns a value.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TArg1">The type of the first argument.</typeparam>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///  Given method ' + methodInfo.Name + ' is static when it has to be an instance
        ///  method. or Given method ' + methodInfo.Name + ' must return type + typeof(TResult)
        ///  + . or Given method ' + methodInfo.Name + ' must have exactly one parameter.
        ///  or The first parameter of the method ' + methodInfo.Name + ' must be of type
        ///  + typeof(TArg1) + .
        /// </exception>
        public static Func<object, TArg1, TResult> CreateWeakInstanceMethodCaller<TResult, TArg1>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.ReturnType != typeof(TResult))
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must return type " + typeof(TResult)?.ToString() + ".");
            }

            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must have exactly one parameter.");
            }

            if (!typeof(TArg1).InheritsFrom(parameters[0].ParameterType))
            {
                throw new ArgumentException("The first parameter of the method '" + methodInfo.Name + "' must be of type " + typeof(TArg1)?.ToString() + ".");
            }

            methodInfo = methodInfo.DeAliasMethod();
            if (EmitIsIllegalForMember(methodInfo))
            {
                return (object classInstance, TArg1 arg1) => (TResult)methodInfo.Invoke(classInstance, new object[1] { arg1 });
            }

            Type declaringType = methodInfo.DeclaringType;
            string name = methodInfo.ReflectedType.FullName + ".call_" + methodInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, typeof(TResult), new Type[2]
            {
            typeof(object),
            typeof(TArg1)
            }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            if (declaringType.IsValueType)
            {
                LocalBuilder local = iLGenerator.DeclareLocal(declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Unbox_Any, declaringType);
                iLGenerator.Emit(OpCodes.Stloc, local);
                iLGenerator.Emit(OpCodes.Ldloca_S, local);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Castclass, declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Func<object, TArg1, TResult>)dynamicMethod.CreateDelegate(typeof(Func<object, TArg1, TResult>));
        }

        public static Func<object, TResult> CreateWeakInstanceMethodCallerFunc<TResult>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.ReturnType != typeof(TResult))
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must return type " + typeof(TResult)?.ToString() + ".");
            }

            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length != 0)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must have no parameter.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            if (EmitIsIllegalForMember(methodInfo))
            {
                return (object classInstance) => (TResult)methodInfo.Invoke(classInstance, null);
            }

            Type declaringType = methodInfo.DeclaringType;
            string name = methodInfo.ReflectedType.FullName + ".call_" + methodInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, typeof(TResult), new Type[1] { typeof(object) }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            if (declaringType.IsValueType)
            {
                LocalBuilder local = iLGenerator.DeclareLocal(declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Unbox_Any, declaringType);
                iLGenerator.Emit(OpCodes.Stloc, local);
                iLGenerator.Emit(OpCodes.Ldloca_S, local);
                iLGenerator.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Castclass, declaringType);
                iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Func<object, TResult>)dynamicMethod.CreateDelegate(typeof(Func<object, TResult>));
        }

        public static Func<object, TArg, TResult> CreateWeakInstanceMethodCallerFunc<TArg, TResult>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.ReturnType != typeof(TResult))
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must return type " + typeof(TResult)?.ToString() + ".");
            }

            ParameterInfo[] parameters = methodInfo.GetParameters();
            if (parameters.Length != 1)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' must have one parameter.");
            }

            if (!parameters[0].ParameterType.IsAssignableFrom(typeof(TArg)))
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' has an invalid parameter type.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            if (EmitIsIllegalForMember(methodInfo))
            {
                return (object classInstance, TArg arg) => (TResult)methodInfo.Invoke(classInstance, new object[1] { arg });
            }

            Type declaringType = methodInfo.DeclaringType;
            string name = methodInfo.ReflectedType.FullName + ".call_" + methodInfo.Name;
            DynamicMethod dynamicMethod = new DynamicMethod(name, typeof(TResult), new Type[2]
            {
            typeof(object),
            typeof(TArg)
            }, restrictedSkipVisibility: true);
            ILGenerator iLGenerator = dynamicMethod.GetILGenerator();
            if (declaringType.IsValueType)
            {
                LocalBuilder local = iLGenerator.DeclareLocal(declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Unbox_Any, declaringType);
                iLGenerator.Emit(OpCodes.Stloc, local);
                iLGenerator.Emit(OpCodes.Ldloca_S, local);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Call, methodInfo);
            }
            else
            {
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Castclass, declaringType);
                iLGenerator.Emit(OpCodes.Ldarg_1);
                iLGenerator.Emit(OpCodes.Callvirt, methodInfo);
            }

            iLGenerator.Emit(OpCodes.Ret);
            return (Func<object, TArg, TResult>)dynamicMethod.CreateDelegate(typeof(Func<object, TArg, TResult>));
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given parameterless instance method.
        /// </summary>
        /// <typeparam name="InstanceType">The type of the class which the method is on.</typeparam>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        public static Action<InstanceType> CreateInstanceMethodCaller<InstanceType>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.GetParameters().Length != 0)
            {
                throw new ArgumentException("Given method cannot have any parameters.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            return (Action<InstanceType>)Delegate.CreateDelegate(typeof(Action<InstanceType>), methodInfo);
        }

        /// <summary>
        /// Creates a fast delegate method which calls a given instance method with a given argument.
        /// </summary>
        /// <typeparam name="InstanceType">The type of the class which the method is on.</typeparam>
        /// <typeparam name="Arg1">The type of the argument with which to call the method.</typeparam>
        /// <param name="methodInfo">The method info instance which is used.</param>
        /// <returns>
        /// A delegate which calls the method and returns the result, except it's hundreds
        /// of times faster than MethodInfo.<see langword="Invoke"/>.
        /// </returns>
        public static Action<InstanceType, Arg1> CreateInstanceMethodCaller<InstanceType, Arg1>(MethodInfo methodInfo)
        {
            if (methodInfo == null)
            {
                throw new ArgumentNullException("methodInfo");
            }

            if (methodInfo.IsStatic)
            {
                throw new ArgumentException("Given method '" + methodInfo.Name + "' is static when it has to be an instance method.");
            }

            if (methodInfo.GetParameters().Length != 1)
            {
                throw new ArgumentException("Given method must have only one parameter.");
            }

            methodInfo = methodInfo.DeAliasMethod();
            return (Action<InstanceType, Arg1>)Delegate.CreateDelegate(typeof(Action<InstanceType, Arg1>), methodInfo);
        }
    }
}
