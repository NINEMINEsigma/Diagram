using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using Newtonsoft.Json;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using static Diagram.ReflectionExtension;
using Debug = UnityEngine.Debug;

namespace Diagram
{
    #region SAL

#pragma warning disable IDE1006
    //Diagram source code annotation language
    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class _In_Attribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class _Out_Attribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class _Ignore_Attribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class _Must_Attribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class _Const_Attribute : Attribute
    {

    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class _Change_Attribute : Attribute
    {
        public string target;

        public _Change_Attribute(string target) { this.target = target; }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class _Note_Attribute : Attribute
    {
        public string note;

        public _Note_Attribute(string note) { this.note = note; }
    }

    [System.AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
    public sealed class _NotSupport_Attribute : Attribute
    {
        public Type type;

        public _NotSupport_Attribute(Type type) { this.type = type; }
    }

    [System.AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class _Init_Attribute : Attribute
    {
        public _Init_Attribute() { }
    }

#pragma warning restore IDE1006 // 命名样式

    #endregion

    #region Type

    #region L

    [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
    [UnityEngine.Scripting.Preserve]
    public abstract class DiagramType
    {
        #region DiagramType

        public const string typeFieldName = "__type";

        public DiagramMember[] cycleMembers;
        public DiagramMember[] members;
        public Type type;
        public bool IsPrimitive { get; protected set; } = false;
        public bool IsValueType { get; protected set; } = false;
        public bool IsCollection { get; protected set; } = false;
        public bool IsDictionary { get; protected set; } = false;
        public bool IsTuple { get; protected set; } = false;
        public bool IsEnum { get; protected set; } = false;
        public bool IsReflectedType { get; protected set; } = false;
        public bool IsUnsupported { get; protected set; } = false;
        public int Priority { get; protected set; } = 0;

        protected DiagramType(Type type)
        {
            DiagramType.AddType(type, this);
            this.type = type;
            this.IsValueType = ReflectionExtension.IsValueType(type);
        }

        public DiagramMember GetMember(string name)
        {
            foreach (var member in members)
            {
                if(member.name == name) return member;
            }
            foreach (var member in cycleMembers)
            {
                if (member.name == name) return member;
            }
            return null;
        }

        #endregion

        #region DiagramTypeMgr

        private static object _lock = new object();

        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Dictionary<Type, DiagramType> types = null;

        // We cache the last accessed type as we quite often use the same type multiple times,
        // so this improves performance as another lookup is not required.
        private static DiagramType lastAccessedType = null;

        public static DiagramType GetOrCreateDiagramType(Type type, bool throwException = true)
        {
            if (types == null)
                Init();

            if (type != typeof(object) && lastAccessedType != null && lastAccessedType.type == type)
                return lastAccessedType;

            // If type doesn't exist, create one.
            if (types.TryGetValue(type, out lastAccessedType))
                return lastAccessedType;
            return (lastAccessedType = CreateDiagramType(type, throwException));
        }

        public static DiagramType GetDiagramType(Type type)
        {
            if (types == null)
                Init();

            if (types.TryGetValue(type, out lastAccessedType))
                return lastAccessedType;
            return null;
        }

        internal static void AddType(Type type, DiagramType adType)
        {
            if (types == null)
                Init();

            var existingType = GetDiagramType(type);
            if (existingType != null && existingType.Priority > adType.Priority)
                return;

            lock (_lock)
            {
                types[type] = adType;
            }
        }

        internal static DiagramType CreateDiagramType(Type type, bool throwException = true)
        {
            DiagramType adType;
            if (ReflectionExtension.IsEnum(type)) adType = CreateEnumType(type);
            else if (ReflectionExtension.TypeIsArray(type)) adType = CreateArrayType(type, throwException);
            else if (ReflectionExtension.IsGenericType(type)
                && ReflectionExtension.ImplementsInterface(type, typeof(IEnumerable))) adType = CreateGenericImplementsInterface(type, throwException);
            else if (ReflectionExtension.IsPrimitive(type)) adType = CreatePrimitiveType(type);
            else adType = CreateElseType(type);

            if (adType.type == null || adType.IsUnsupported)
            {
                if (throwException)
                    throw new NotSupportedException(string.Format("DiagramType.type is null when trying to create an DiagramType for {0}, possibly because the element type is not supported.", type));
                return null;
            }

            DiagramType.AddType(type, adType);

            return adType;
        }

        private static DiagramType CreateEnumType(Type type)
        {
            return new DiagramType_enum(type);
        }
        private static DiagramType CreateArrayType(Type type, bool throwException)
        {
            int rank = ReflectionExtension.GetArrayRank(type);
            if (rank == 1)
                return new DiagramArrayType(type);
            else if (rank == 2)
                return new Diagram2DArrayType(type);
            else if (rank == 3)
                return new Diagram3DArrayType(type);
            else if (throwException)
                throw new NotSupportedException("Only arrays with up to three dimensions are supported by DiagramType.");
            else
                return null;
        }
        private static DiagramType CreateGenericImplementsInterface(Type type, bool throwException)
        {
            Type genericType = ReflectionExtension.GetGenericTypeDefinition(type);
            if (typeof(List<>).IsAssignableFrom(genericType))
                return new DiagramListType(type);
            else if (typeof(IDictionary).IsAssignableFrom(genericType))
                return new DiagramDictionaryType(type);
            else if (genericType == typeof(Queue<>))
                return new DiagramQueueType(type);
            else if (genericType == typeof(Stack<>))
                return new DiagramStackType(type);
            else if (genericType == typeof(HashSet<>))
                return new DiagramHashSetType(type);
            else if (genericType == typeof(Unity.Collections.NativeArray<>))
                return new DiagramNativeArrayType(type);
            else if (throwException)
                throw new NotSupportedException("Generic type \"" + type.ToString() + "\" is not supported by Diagram.");
            else
                return null;
        }
        private static DiagramType CreatePrimitiveType(Type type)
        {
            return null;
        }

        private static DiagramType CreateElseType(Type type)
        {
            //if (ReflectionExtension.IsAssignableFrom(typeof(Component), type))
            //    return new DiagramReflectedComponentType(type);
            //else if (ReflectionExtension.IsValueType(type))
            if (ReflectionExtension.IsValueType(type))
                return new DiagramReflectedValueType(type);
            //else if (ReflectionExtension.IsAssignableFrom(typeof(ScriptableObject), type))
            //    return new DiagramReflectedScriptableObjectType(type);
            //else if (ReflectionExtension.IsAssignableFrom(typeof(UnityEngine.Object), type))
            //    return new DiagramReflectedUnityObjectType(type);
            else if (type.Name.StartsWith("Tuple`"))
                return new DiagramTupleType(type);
            else
                return new DiagramReflectedObjectType(type);
        }

        internal void GetMembers(bool safe)
        {
            GetMembers(safe, null);
        }
        internal void GetMembers(bool safe, string[] memberNames)
        {
            List<ReflectionExtension.DiagramReflectedMember> allsSrializedMembers = ReflectionExtension.GetSerializableMembers(type, safe, memberNames, true).ToList();
            members = allsSrializedMembers.GetSubList(T => !(T.MemberType == type && !ReflectionExtension.IsAssignableFrom(typeof(UnityEngine.Object), T.MemberType))
            , T => new DiagramMember(T)).ToArray();
            cycleMembers = allsSrializedMembers.GetSubList(T => (T.MemberType == type && !ReflectionExtension.IsAssignableFrom(typeof(UnityEngine.Object), T.MemberType))
            , T => new DiagramMember(T)).ToArray();
        }

        internal static void Init()
        {
            lock (_lock)
            {
                types = new Dictionary<Type, DiagramType>();
                ReflectionExtension.GetInstances<DiagramType>();

                // Check that the type list was initialised correctly.
                if (types == null || types.Count == 0)
                    throw new TypeLoadException("Type list could not be initialised");
            }
        }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Class)]
    public class DiagramPropertiesAttribute : System.Attribute
    {
        public readonly string[] members;

        public DiagramPropertiesAttribute(params string[] members)
        {
            this.members = members;
        }
    }

    public class DiagramMember
    {
        public string name;
        public Type type;
        public bool isProperty;
        public ReflectionExtension.DiagramReflectedMember reflectedMember;
        public bool useReflection = false;

        public DiagramMember(string name, Type type, bool isProperty)
        {
            this.name = name;
            this.type = type;
            this.isProperty = isProperty;
        }

        public DiagramMember(ReflectionExtension.DiagramReflectedMember reflectedMember)
        {
            this.reflectedMember = reflectedMember;
            this.name = reflectedMember.Name;
            this.type = reflectedMember.MemberType;
            this.isProperty = reflectedMember.isProperty;
            this.useReflection = true;
        }
    }

    public struct DiagramData
    {
        public DiagramType type;
        public byte[] bytes;

        public DiagramData(Type type, byte[] bytes)
        {
            this.type = type == null ? null : DiagramType.GetOrCreateDiagramType(type);
            this.bytes = bytes;
        }

        public DiagramData(DiagramType type, byte[] bytes)
        {
            this.type = type;
            this.bytes = bytes;
        }
    }

    //Collection Types

    [UnityEngine.Scripting.Preserve]
    public abstract class DiagramCollectionType : DiagramType
    {
        public DiagramType elementType;
        public DiagramCollectionType(Type type) : base(type)
        {
            elementType = DiagramType.GetOrCreateDiagramType(ReflectionExtension.GetElementTypes(type)[0], false);
            IsCollection = true;

            // If the element type is null (i.e. unsupported), make this ES3Type null.
            if (elementType == null)
                IsUnsupported = true;
        }

        public DiagramCollectionType(Type type, DiagramType elementType) : base(type)
        {
            this.elementType = elementType;
            IsCollection = true;
        }
    }

    #region Collection Type

    #region Array

    [UnityEngine.Scripting.Preserve]
    public class DiagramArrayType : DiagramCollectionType
    {
        public DiagramArrayType(Type type) : base(type) { }
        public DiagramArrayType(Type type, DiagramType elementType) : base(type, elementType) { }
    }

    public class Diagram2DArrayType : DiagramCollectionType
    {
        public Diagram2DArrayType(Type type) : base(type) { }
    }

    public class Diagram3DArrayType : DiagramCollectionType
    {
        public Diagram3DArrayType(Type type) : base(type) { }
    }

    #endregion

    [UnityEngine.Scripting.Preserve]
    public class DiagramConcurrentDictionaryType : DiagramType
    {
        public DiagramType keyType;
        public DiagramType valueType;

        protected ReflectionExtension.DiagramReflectedMethod readMethod = null;
        protected ReflectionExtension.DiagramReflectedMethod readIntoMethod = null;

        public DiagramConcurrentDictionaryType(Type type) : base(type)
        {
            var types = ReflectionExtension.GetElementTypes(type);
            keyType = DiagramType.GetOrCreateDiagramType(types[0], false);
            valueType = DiagramType.GetOrCreateDiagramType(types[1], false);

            // If either the key or value type is unsupported, make this type NULL.
            if (keyType == null || valueType == null)
                IsUnsupported = true; ;

            IsDictionary = true;
        }

        public DiagramConcurrentDictionaryType(Type type, DiagramType keyType, DiagramType valueType) : base(type)
        {
            this.keyType = keyType;
            this.valueType = valueType;

            // If either the key or value type is unsupported, make this type NULL.
            if (keyType == null || valueType == null)
                IsUnsupported = true; ;

            IsDictionary = true;
        }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramDictionaryType : DiagramType
    {
        public DiagramType keyType;
        public DiagramType valueType;

        protected ReflectionExtension.DiagramReflectedMethod readMethod = null;
        protected ReflectionExtension.DiagramReflectedMethod readIntoMethod = null;

        public DiagramDictionaryType(Type type) : base(type)
        {
            var types = ReflectionExtension.GetElementTypes(type);
            keyType = DiagramType.GetOrCreateDiagramType(types[0], false);
            valueType = DiagramType.GetOrCreateDiagramType(types[1], false);

            // If either the key or value type is unsupported, make this type NULL.
            if (keyType == null || valueType == null)
                IsUnsupported = true; ;

            IsDictionary = true;
        }

        public DiagramDictionaryType(Type type, DiagramType keyType, DiagramType valueType) : base(type)
        {
            this.keyType = keyType;
            this.valueType = valueType;

            // If either the key or value type is unsupported, make this type NULL.
            if (keyType == null || valueType == null)
                IsUnsupported = true; ;

            IsDictionary = true;
        }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramHashSetType : DiagramCollectionType
    {
        public DiagramHashSetType(Type type) : base(type) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramListType : DiagramCollectionType
    {
        public DiagramListType(Type type) : base(type) { }
        public DiagramListType(Type type, DiagramType elementType) : base(type, elementType) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramNativeArrayType : DiagramCollectionType
    {
        public DiagramNativeArrayType(Type type) : base(type) { }
        public DiagramNativeArrayType(Type type, DiagramType elementType) : base(type, elementType) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramQueueType : DiagramCollectionType
    {
        public DiagramQueueType(Type type) : base(type) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramStackType : DiagramCollectionType
    {
        public DiagramStackType(Type type) : base(type) { }
    }

    [UnityEngine.Scripting.Preserve]
    public class DiagramTupleType : DiagramType
    {
        public DiagramType[] adTypes;
        public Type[] subTypes;

        protected ReflectionExtension.DiagramReflectedMethod readMethod = null;
        protected ReflectionExtension.DiagramReflectedMethod readIntoMethod = null;

        public DiagramTupleType(Type type) : base(type)
        {
            subTypes = ReflectionExtension.GetElementTypes(type);
            adTypes = new DiagramType[subTypes.Length];

            for (int i = 0; i < subTypes.Length; i++)
            {
                adTypes[i] = DiagramType.GetOrCreateDiagramType(subTypes[i], false);
                if (adTypes[i] == null)
                    IsUnsupported = true;
            }

            IsTuple = true;
        }
    }

    #endregion

    //NET Types
    [UnityEngine.Scripting.Preserve]
    [DiagramProperties("bytes")]
    public class DiagramType_BigInteger : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_BigInteger() : base(typeof(BigInteger))
        {
            Instance = this;
        }
    }

    public class DiagramType_BigIntegerArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_BigIntegerArray() : base(typeof(BigInteger[]), DiagramType_BigInteger.Instance)
        {
            Instance = this;
        }
    }

    [UnityEngine.Scripting.Preserve]
    [DiagramProperties()]
    public class DiagramType_Type : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Type() : base(typeof(System.Type))
        {
            Instance = this;
        }
    }

    [UnityEngine.Scripting.Preserve]
    public abstract class DiagramObjectType : DiagramType
    {
        public DiagramObjectType(Type type) : base(type) { }
    }

    #endregion

    #region S

    #region String

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_string : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_string() : base(typeof(string))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_StringArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_StringArray() : base(typeof(string[]), DiagramType_string.Instance)
        {
            Instance = this;
        }
    }

    #endregion

    #region byteArray

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_byte : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_byte() : base(typeof(byte))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }


    [UnityEngine.Scripting.Preserve]
    public class DiagramType_byteArray : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_byteArray() : base(typeof(byte[]))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }
    #endregion

    #region bool

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_bool : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_bool() : base(typeof(bool))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_boolArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_boolArray() : base(typeof(bool[]), DiagramType_bool.Instance)
        {
            Instance = this;
        }
    }

    #endregion

    #region char

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_char : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_char() : base(typeof(char))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }
    public class DiagramType_charArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_charArray() : base(typeof(char[]), DiagramType_char.Instance)
        {
            Instance = this;
        }
    }

    #endregion

    #region DateTime

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_DateTime : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_DateTime() : base(typeof(DateTime))
        {
            Instance = this;
        }
    }

    public class DiagramType_DateTimeArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_DateTimeArray() : base(typeof(DateTime[]), DiagramType_DateTime.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region decimal

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_decimal : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_decimal() : base(typeof(decimal))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_decimalArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_decimalArray() : base(typeof(decimal[]), DiagramType_decimal.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region double

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_double : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_double() : base(typeof(double))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_doubleArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_doubleArray() : base(typeof(double[]), DiagramType_double.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region enum

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_enum : DiagramType
    {
        public static DiagramType Instance = null;
        private Type underlyingType = null;

        public DiagramType_enum(Type type) : base(type)
        {
            IsPrimitive = true;
            IsEnum = true;
            Instance = this;
            underlyingType = Enum.GetUnderlyingType(type);
        }
    }


    #endregion

    #region ref
    //public class DiagramRef
    //{
    //    public long id;
    //    public DiagramRef(long id)
    //    {
    //        this.id = id;
    //    }
    //}
    //
    //[UnityEngine.Scripting.Preserve]
    //public class DiagramType_DiagramRef : DiagramType
    /*{
        public static DiagramType Instance = new DiagramType_DiagramRef();

        public DiagramType_DiagramRef() : base(typeof(long))
        {
            IsPrimitive = true;
            Instance = this;
        }

        public override void Write(object obj, DiagramWriter writer)
        {
            writer.WritePrimitive(((long)obj).ToString());
        }

        public override object Read<T>(DiagramReader reader)
        {
            return (T)(object)new DiagramRef(reader.Read_ref());
        }
    }
    //
    //public class DiagramType_DiagramRefArray : DiagramArrayType
    {
        public static DiagramType Instance = new DiagramType_DiagramRefArray();

        public DiagramType_DiagramRefArray() : base(typeof(DiagramRef[]), DiagramType_DiagramRef.Instance)
        {
            Instance = this;
        }
    }
    //
    //public class DiagramType_DiagramRefDictionary : DiagramDictionaryType
    {
        public static DiagramType Instance = new DiagramType_DiagramRefDictionary();

        public DiagramType_DiagramRefDictionary() : base(typeof(Dictionary<DiagramRef, DiagramRef>), DiagramType_DiagramRef.Instance, DiagramType_DiagramRef.Instance)
        {
            Instance = this;
        }
    }*/


    #endregion

    #region float

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_float : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_float() : base(typeof(float))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_floatArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_floatArray() : base(typeof(float[]), DiagramType_float.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region int

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_int : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_int() : base(typeof(int))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_intArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_intArray() : base(typeof(int[]), DiagramType_int.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region intptr

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_IntPtr : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_IntPtr() : base(typeof(IntPtr))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_IntPtrArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_IntPtrArray() : base(typeof(IntPtr[]), DiagramType_IntPtr.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region long

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_long : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_long() : base(typeof(long))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_longArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_longArray() : base(typeof(long[]), DiagramType_long.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region sbyte

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_sbyte : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_sbyte() : base(typeof(sbyte))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_sbyteArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_sbyteArray() : base(typeof(sbyte[]), DiagramType_sbyte.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region short

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_short : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_short() : base(typeof(short))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_shortArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_shortArray() : base(typeof(short[]), DiagramType_short.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region uint

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_uint : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_uint() : base(typeof(uint))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_uintArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_uintArray() : base(typeof(uint[]), DiagramType_uint.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region uintptr

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_UIntPtr : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_UIntPtr() : base(typeof(UIntPtr))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_UIntPtrArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_UIntPtrArray() : base(typeof(UIntPtr[]), DiagramType_UIntPtr.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region ulong

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_ulong : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_ulong() : base(typeof(ulong))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_ulongArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_ulongArray() : base(typeof(ulong[]), DiagramType_ulong.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region ushort

    [UnityEngine.Scripting.Preserve]
    public class DiagramType_ushort : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_ushort() : base(typeof(ushort))
        {
            IsPrimitive = true;
            Instance = this;
        }
    }

    public class DiagramType_ushortArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_ushortArray() : base(typeof(ushort[]), DiagramType_ushort.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region Vector

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("x", "y")]
    public class DiagramType_Vector2 : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Vector2() : base(typeof(UnityEngine.Vector2))
        {
            Instance = this;
        }
    }

    public class DiagramType_Vector2Array : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_Vector2Array() : base(typeof(UnityEngine.Vector2[]), DiagramType_Vector2.Instance)
        {
            Instance = this;
        }
    }

    [UnityEngine.Scripting.Preserve]
    [DiagramProperties("x", "y", "z")]
    public class DiagramType_Vector3 : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Vector3() : base(typeof(UnityEngine.Vector3))
        {
            Instance = this;
        }
    }

    public class DiagramType_Vector3Array : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_Vector3Array() : base(typeof(UnityEngine.Vector3[]), DiagramType_Vector3.Instance)
        {
            Instance = this;
        }
    }

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("x", "y", "z", "w")]
    public class DiagramType_Vector4 : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Vector4() : base(typeof(UnityEngine.Vector4))
        {
            Instance = this;
        }

        public static bool Equals(UnityEngine.Vector4 a, UnityEngine.Vector4 b)
        {
            return (Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) && Mathf.Approximately(a.z, b.z) && Mathf.Approximately(a.w, b.w));
        }
    }

    public class DiagramType_Vector4Array : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_Vector4Array() : base(typeof(UnityEngine.Vector4[]), DiagramType_Vector4.Instance)
        {
            Instance = this;
        }
    }

    #endregion

    #region Color

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("r", "g", "b", "a")]
    public class DiagramType_Color : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Color() : base(typeof(Color))
        {
            Instance = this;
        }
    }

    public class DiagramType_ColorArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_ColorArray() : base(typeof(Color[]), DiagramType_Color.Instance)
        {
            Instance = this;
        }
    }

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("r", "g", "b", "a")]
    public class DiagramType_Color32 : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Color32() : base(typeof(Color32))
        {
            Instance = this;
        }

        public static bool Equals(Color32 a, Color32 b)
        {
            if (a.r != b.r || a.g != b.g || a.b != b.b || a.a != b.a)
                return false;
            return true;
        }
    }

    public class DiagramType_Color32Array : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_Color32Array() : base(typeof(Color32[]), DiagramType_Color32.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region Quaternion

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("x", "y", "z", "w")]
    public class DiagramType_Quaternion : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Quaternion() : base(typeof(UnityEngine.Quaternion))
        {
            Instance = this;
        }
    }

    public class DiagramType_QuaternionArray : DiagramArrayType
    {
        public static DiagramType Instance;

        public DiagramType_QuaternionArray() : base(typeof(UnityEngine.Quaternion[]), DiagramType_Quaternion.Instance)
        {
            Instance = this;
        }
    }


    #endregion

    #region Rect 

    [UnityEngine.Scripting.Preserve]
    [DiagramPropertiesAttribute("x", "y", "width", "height")]
    public class DiagramType_Rect : DiagramType
    {
        public static DiagramType Instance = null;

        public DiagramType_Rect() : base(typeof(UnityEngine.Rect))
        {
            Instance = this;
        }
    }


    #endregion

    #endregion

    #region Object

    [UnityEngine.Scripting.Preserve]
    internal class DiagramReflectedValueType : DiagramType
    {
        public DiagramReflectedValueType(Type type) : base(type)
        {
            IsReflectedType = true;
            GetMembers(true);
        }
    }


    [UnityEngine.Scripting.Preserve]
    internal class DiagramReflectedObjectType : DiagramObjectType
    {
        public DiagramReflectedObjectType(Type type) : base(type)
        {
            IsReflectedType = true;
            GetMembers(true);
        }
    }

    #endregion

    #endregion

    #region Reflection

    public static class ReflectionExtension
    {
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static string[] assemblyNames = new string[] { "Assembly-CSharp-firstpass", "Assembly-CSharp" };

        public static readonly BindingFlags PublicFlags = BindingFlags.Public | BindingFlags.Instance;
        public static readonly BindingFlags DefaultBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;
        public static readonly BindingFlags AllBindingFlags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance;
        public static readonly BindingFlags GetSerializableBindFlags = BindingFlags.Public
                                                                       | BindingFlags.NonPublic
                                                                       | BindingFlags.Instance
                                                                       | BindingFlags.Static
                                                                       | BindingFlags.DeclaredOnly;

        public static bool CreateInstance(this Assembly assembly, string fullName, out object obj)
        {
            obj = assembly.CreateInstance(fullName);
            return obj != null;
        }

        public static bool Run(this Assembly assembly, string typeName, string detecter, string targetFuncName)
        {
            var objs = UnityEngine.Object.FindObjectsOfType(assembly.GetType(typeName));
            string objName = detecter[..detecter.LastIndexOf('.')], methodName = detecter[detecter.LastIndexOf('.')..];
            var a = assembly.CreateInstance(objName);
            if (a == null) return false;
            a.GetType().GetMethod("DetecterInit")?.Invoke(a, new object[] { });
            var detecterFunc = a.GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (detecterFunc == null) return false;
            foreach (var obj in objs)
            {
                if (obj == null) continue;
                if ((bool)detecterFunc.Invoke(a, new object[] { obj }))
                {
                    var targetFunc = obj.GetType().GetMethod(targetFuncName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
                    if (targetFunc == null) return false;
                    targetFunc.Invoke(obj, new object[] { });
                    return true;
                }
            }
            return false;
        }

        public static bool Run(this Assembly assembly, string typeName, object detecter, string detecterFuncName, string targetFuncName)
        {
            var objs = UnityEngine.Object.FindObjectsOfType(assembly.GetType(typeName));
            if (detecter == null) return false;
            detecter.GetType().GetMethod("DetecterInit")?.Invoke(detecter, new object[] { });
            var detecterFunc = detecter.GetType().GetMethod(detecterFuncName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
            if (detecterFunc == null) return false;
            foreach (var obj in objs)
            {
                if (obj == null) continue;
                if ((bool)detecterFunc.Invoke(detecter, new object[] { obj }))
                {
                    var targetFunc = obj.GetType().GetMethod(targetFuncName, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static);
                    if (targetFunc == null) return false;
                    targetFunc.Invoke(obj, new object[] { });
                    return true;
                }
            }
            return false;
        }

        public class TypeResult
        {
            public Type type;
            public object target;
            public string CallingName;

            public void Init(Type type, object target, string CallingName = "@")
            {
                this.type = type;
                this.target = target;
                this.CallingName = CallingName;
            }
        }

        public class FullAutoRunResultInfo
        {
            public bool result = true;
            public Exception ex = null;
            public TypeResult[] typeResults = null;
        }

        private static object GetCurrentTargetWhenGetField(string currentCallingName, TypeResult current)
        {
            object currentTarget;
            FieldInfo data =
                current.target.GetType().GetField(currentCallingName, DefaultBindingFlags)
                ?? throw new FieldException();
            currentTarget = data.GetValue(current.target);
            return currentTarget;
        }

        public static Type ToType(this string self)
        {
            return Assembly.GetExecutingAssembly().GetType(self);
        }

        public static Type ToType(this string self, Assembly assembly)
        {
            return assembly.GetType(self);
        }

        public static Type Typen(string typeName, string singleTypeName = null)
        {
            Type type = null;
            Assembly[] assemblyArray = AppDomain.CurrentDomain.GetAssemblies();
            int assemblyArrayLength = assemblyArray.Length;
            for (int i = 0; i < assemblyArrayLength; ++i)
            {
                type = assemblyArray[i].GetType(typeName);
                if (type != null)
                {
                    return type;
                }
            }
            for (int i = 0; (i < assemblyArrayLength); ++i)
            {
                Type[] typeArray = assemblyArray[i].GetTypes();
                int typeArrayLength = typeArray.Length;
                for (int j = 0; j < typeArrayLength; ++j)
                {
                    if (typeArray[j].Name.Equals(singleTypeName ?? typeName))
                    {
                        return typeArray[j];
                    }
                }
            }
            return type;
        }

        public static Type Typen(this Assembly self, string typeName, string singleTypeName = null)
        {
            Type type = self.GetType(typeName);
            if (type != null)
            {
                return type;
            }
            Type[] typeArray = self.GetTypes();
            int typeArrayLength = typeArray.Length;
            for (int j = 0; j < typeArrayLength; ++j)
            {
                if (typeArray[j].Name.Equals(singleTypeName ?? typeName))
                {
                    return typeArray[j];
                }
            }
            return type;
        }

        public static object CreateInstance(this Type type)
        {
            return Activator.CreateInstance(type);
        }

        public static T CreateInstance<T>(this Type type)
        {
            return (T)Activator.CreateInstance(type);
        }

        public static object GetFieldByName(this object self, string fieldName)
        {
            return self.GetType().GetField(fieldName, DefaultBindingFlags).GetValue(self);
        }

        public static T GetFieldByName<T>(this object self, string fieldName)
        {
            return (T)self.GetType().GetField(fieldName, DefaultBindingFlags).GetValue(self);
        }

        public static object GetFieldByName(this object self, string fieldName, BindingFlags flags)
        {
            return self.GetType().GetField(fieldName, flags).GetValue(self);
        }

        public static T GetFieldByName<T>(this object self, string fieldName, BindingFlags flags)
        {
            return (T)self.GetType().GetField(fieldName, flags).GetValue(self);
        }

        public static object RunMethodByName(this object self, string methodName, BindingFlags flags, params object[] args)
        {
            return self.GetType().GetMethod(methodName, flags).Invoke(self, args);
        }

        public static bool TryRunMethodByName(this object self, string methodName, out object result, BindingFlags flags, params object[] args)
        {
            result = null;
            MethodInfo method = self.GetType().GetMethod(methodName, flags);
            if (method == null) return false;
            result = method.Invoke(self, args);
            return true;
        }

        public static FieldInfo[] GetAllFields(this object self)
        {
            return self.GetType().GetFields(AllBindingFlags);
        }

        public static PropertyInfo[] GetAllProperties(this object self)
        {
            return self.GetType().GetProperties(AllBindingFlags);
        }

        public static Type[] GetAllInterfaces(this object self)
        {
            return self.GetType().GetInterfaces();
        }

        public static bool GetInterface<T>(this object self, out Type _interface, out T result)
        {
            _interface = self.GetType().GetInterface(typeof(T).Name);
            result = (T)self;
            return _interface != null;
        }

        public static Type GetInterface<T>(this object self)
        {
            return self.GetType().GetInterface(typeof(T).Name);
        }

        public const string memberFieldPrefix = "m_";
        public const string componentTagFieldName = "tag";
        public const string componentNameFieldName = "name";
        public static readonly string[] excludedPropertyNames = new string[] { "runInEditMode", "useGUILayout", "hideFlags" };

        public static readonly Type serializableAttributeType = typeof(System.SerializableAttribute);
        public static readonly Type serializeFieldAttributeType = typeof(SerializeField);
        public static readonly Type obsoleteAttributeType = typeof(System.ObsoleteAttribute);
        public static readonly Type nonSerializedAttributeType = typeof(System.NonSerializedAttribute);
        public static readonly Type ignoreSerializedAttributeType = typeof(_Ignore_Attribute);
        public static readonly Type nonIgnoreSerializedAttributeType = typeof(_Must_Attribute);

        public static Type[] EmptyTypes = new Type[0];

        private static Assembly[] _assemblies = null;
        private static Assembly[] Assemblies
        {
            get
            {
                if (_assemblies == null)
                {
                    _assemblies = AppDomain.CurrentDomain.GetAssemblies();
                }
                return _assemblies;
            }
        }

        public static void AssembliesUpdate()
        {
            _assemblies = AppDomain.CurrentDomain.GetAssemblies();
        }

        public static Assembly[] GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies();
        }

        /*	
		 * 	Gets the element type of a collection or array.
		 * 	Returns null if type is not a collection type.
		 */
        public static Type[] GetElementTypes(Type type)
        {
            if (IsGenericType(type))
                return GetGenericArguments(type);
            else if (type.IsArray)
                return new Type[] { GetElementType(type) };
            else
                return null;
        }

        public static List<FieldInfo> DoGetSerializableFields(Type type,
                                                            List<FieldInfo> serializableFields,
                                                            bool safe,
                                                            string[] memberNames,
                                                            BindingFlags bindings,
                                                            bool IsSupportCycle)
        {
            if (type == null)
                return new List<FieldInfo>();

            var fields = type.GetFields(bindings);

            serializableFields ??= new List<FieldInfo>();

            foreach (var field in fields)
            {
                var fieldName = field.Name;

                // If a members array was provided as a parameter, only include the field if it's in the array.
                if (memberNames != null)
                    if (!memberNames.Contains(fieldName))
                        continue;

                var fieldType = field.FieldType;

                if (AttributeIsDefined(field, nonIgnoreSerializedAttributeType))
                {
                    // If this field is marked by nonIgnoreSerializedAttributeType ,it must try to serializable
                    serializableFields.Add(field);
                    continue;
                }

                if (safe)
                {
                    // If the field is private, only serialize it if it's explicitly marked as serializable.
                    if (!field.IsPublic && !AttributeIsDefined(field, serializeFieldAttributeType))
                        continue;
                }

                // Exclude const or readonly fields.
                if (field.IsLiteral || field.IsInitOnly)
                    continue;

                // Don't store fields whose type is the same as the class the field is housed in unless it's stored by reference (to prevent cyclic references)
                if (!IsSupportCycle)
                {
                    if (fieldType == type && !IsAssignableFrom(typeof(UnityEngine.Object), fieldType))
                        continue;
                }

                // If property is marked as obsolete or non-serialized or ignore, don't serialize it.
                if (AttributeIsDefined(field, nonSerializedAttributeType) ||
                    AttributeIsDefined(field, obsoleteAttributeType) ||
                    AttributeIsDefined(field, ignoreSerializedAttributeType))
                    continue;

                if (!TypeIsSerializable(field.FieldType))
                    continue;

                // Don't serialize member fields.
                if (safe && fieldName.StartsWith(memberFieldPrefix) && field.DeclaringType.Namespace != null && field.DeclaringType.Namespace.Contains("UnityEngine"))
                    continue;

                serializableFields.Add(field);
            }

            var baseType = BaseType(type);
            if (baseType != null && baseType != typeof(System.Object) && baseType != typeof(UnityEngine.Object))
                DoGetSerializableFields(BaseType(type), serializableFields, safe, memberNames, bindings, IsSupportCycle);

            return serializableFields;
        }

        public static List<FieldInfo> GetSerializableFields(Type type,
                                                            List<FieldInfo> serializableFields = null,
                                                            bool safe = true,
                                                            string[] memberNames = null,
                                                            BindingFlags bindings = BindingFlags.Public
                                                                                    | BindingFlags.NonPublic
                                                                                    | BindingFlags.Instance
                                                                                    | BindingFlags.Static
                                                                                    | BindingFlags.DeclaredOnly)
        {
            return DoGetSerializableFields(type, serializableFields, safe, memberNames, bindings, false);
        }

        public static List<FieldInfo> GetSerializableFieldsSupportCycle(Type type,
                                                            List<FieldInfo> serializableFields = null,
                                                            bool safe = true,
                                                            string[] memberNames = null,
                                                            BindingFlags bindings = BindingFlags.Public
                                                                                    | BindingFlags.NonPublic
                                                                                    | BindingFlags.Instance
                                                                                    | BindingFlags.Static
                                                                                    | BindingFlags.DeclaredOnly)
        {
            return DoGetSerializableFields(type, serializableFields, safe, memberNames, bindings, true);
        }

        public static List<PropertyInfo> DoGetSerializableProperties(Type type,
                                                                   List<PropertyInfo> serializableProperties,
                                                                   bool safe,
                                                                   string[] memberNames,
                                                                   BindingFlags bindings,
                                                                   bool IsSupportCycle)
        {
            bool isComponent = IsAssignableFrom(typeof(UnityEngine.Component), type);

            // Only get private properties if we're not getting properties safely.
            if (!safe)
                bindings = bindings | BindingFlags.NonPublic;

            var properties = type.GetProperties(bindings);

            if (serializableProperties == null)
                serializableProperties = new List<PropertyInfo>();

            foreach (var p in properties)
            {
                var propertyName = p.Name;

                if (AttributeIsDefined(p, nonIgnoreSerializedAttributeType))
                {
                    // If this property is marked by nonIgnoreSerializedAttributeType ,it must try to serializable
                    serializableProperties.Add(p);
                    continue;
                }

                if (excludedPropertyNames.Contains(propertyName))
                    continue;

                // If a members array was provided as a parameter, only include the property if it's in the array.
                if (memberNames != null)
                    if (!memberNames.Contains(propertyName))
                        continue;

                if (safe)
                {
                    // If safe serialization is enabled, only get properties which are explicitly marked as serializable.
                    if (!AttributeIsDefined(p, serializeFieldAttributeType))
                        continue;
                }

                var propertyType = p.PropertyType;

                // Don't store properties whose type is the same as the class the property is housed in unless it's stored by reference (to prevent cyclic references)
                if (!IsSupportCycle)
                {
                    if (propertyType == type && !IsAssignableFrom(typeof(UnityEngine.Object), propertyType))
                        continue;
                }

                if (!p.CanRead || !p.CanWrite)
                    continue;

                // Only support properties with indexing if they're an array.
                if (p.GetIndexParameters().Length != 0 && !propertyType.IsArray)
                    continue;

                // Check that the type of the property is one which we can serialize.
                // Also check whether an DiagramType exists for it.
                if (!TypeIsSerializable(propertyType))
                    continue;

                // Ignore certain properties on components.
                if (isComponent)
                {
                    // Ignore properties which are accessors for GameObject fields.
                    if (propertyName == componentTagFieldName || propertyName == componentNameFieldName)
                        continue;
                }

                // If property is marked as obsolete or non-serialized or ignore, don't serialize it.
                if (AttributeIsDefined(p, obsoleteAttributeType) ||
                    AttributeIsDefined(p, nonSerializedAttributeType) ||
                    AttributeIsDefined(p, ignoreSerializedAttributeType))
                    continue;

                serializableProperties.Add(p);
            }

            var baseType = BaseType(type);
            if (baseType != null && baseType != typeof(System.Object))
                DoGetSerializableProperties(baseType, serializableProperties, safe, memberNames, bindings, IsSupportCycle);

            return serializableProperties;
        }


        public static List<PropertyInfo> GetSerializableProperties(Type type,
                                                                   List<PropertyInfo> serializableProperties = null,
                                                                   bool safe = true,
                                                                   string[] memberNames = null,
                                                                   BindingFlags bindings = BindingFlags.Public
                                                                                           | BindingFlags.NonPublic
                                                                                           | BindingFlags.Instance
                                                                                           | BindingFlags.Static
                                                                                           | BindingFlags.DeclaredOnly)
        {
            return DoGetSerializableProperties(type, serializableProperties, safe, memberNames, bindings, false);
        }

        public static List<PropertyInfo> GetSerializablePropertiesSupportCycle(Type type,
                                                                   List<PropertyInfo> serializableProperties = null,
                                                                   bool safe = true,
                                                                   string[] memberNames = null,
                                                                   BindingFlags bindings = BindingFlags.Public
                                                                                           | BindingFlags.NonPublic
                                                                                           | BindingFlags.Instance
                                                                                           | BindingFlags.Static
                                                                                           | BindingFlags.DeclaredOnly)
        {
            return DoGetSerializableProperties(type, serializableProperties, safe, memberNames, bindings, true);
        }


        public static bool TypeIsSerializable(Type type)
        {
            if (type == null)
                return false;

            if (AttributeIsDefined(type, nonSerializedAttributeType))
                return false;

            if (IsPrimitive(type) || IsValueType(type) || IsAssignableFrom(typeof(UnityEngine.Component), type) || IsAssignableFrom(typeof(UnityEngine.ScriptableObject), type))
                return true;

            var adType = DiagramType.GetOrCreateDiagramType(type, false);

            if (adType != null && !adType.IsUnsupported)
                return true;

            if (TypeIsArray(type))
            {
                if (TypeIsSerializable(type.GetElementType()))
                    return true;
                return false;
            }

            var genericArgs = type.GetGenericArguments();
            for (int i = 0; i < genericArgs.Length; i++)
                if (!TypeIsSerializable(genericArgs[i]))
                    return false;

            /*if (HasParameterlessConstructor(type))
                return true;*/
            return false;
        }

        public static System.Object CreateInstance(Type type, params object[] args)
        {
            if (IsAssignableFrom(typeof(ScriptableObject), type))
                return ScriptableObject.CreateInstance(type);
            return Activator.CreateInstance(type, args);
        }

        public static Array ArrayCreateInstance(Type type, int length)
        {
            return Array.CreateInstance(type, new int[] { length });
        }

        public static Array ArrayCreateInstance(Type type, int[] dimensions)
        {
            return Array.CreateInstance(type, dimensions);
        }

        public static Type MakeGenericType(Type type, Type genericParam)
        {
            return type.MakeGenericType(genericParam);
        }

        public static DiagramReflectedMember[] GetSerializableMembers(Type type, bool safe = true, string[] memberNames = null, bool isSupportCycle = false)
        {
            if (type == null)
                return new DiagramReflectedMember[0];

            var fieldInfos = DoGetSerializableFields(type, new List<FieldInfo>(), safe, memberNames, GetSerializableBindFlags, isSupportCycle);
            var propertyInfos = DoGetSerializableProperties(type, new List<PropertyInfo>(), safe, memberNames, GetSerializableBindFlags, isSupportCycle);
            var reflectedFields = new DiagramReflectedMember[fieldInfos.Count + propertyInfos.Count];

            for (int i = 0; i < fieldInfos.Count; i++)
                reflectedFields[i] = new DiagramReflectedMember(fieldInfos[i]);
            for (int i = 0; i < propertyInfos.Count; i++)
                reflectedFields[i + fieldInfos.Count] = new DiagramReflectedMember(propertyInfos[i]);

            return reflectedFields;
        }

        public static DiagramReflectedMember GetDiagramReflectedProperty(Type type, string propertyName)
        {
            var propertyInfo = GetProperty(type, propertyName);
            return new DiagramReflectedMember(propertyInfo);
        }

        public static DiagramReflectedMember GetDiagramReflectedMember(Type type, string fieldName)
        {
            var fieldInfo = GetField(type, fieldName);
            return new DiagramReflectedMember(fieldInfo);
        }

        /*
		 * 	Finds all classes of a specific type, and then returns an instance of each.
		 * 	Ignores classes which can't be instantiated (i.e. abstract classes, those without parameterless constructors).
		 */
        public static IList<T> GetInstances<T>()
        {
            var instances = new List<T>();
            foreach (var assembly in Assemblies)
                foreach (var type in assembly.GetTypes())
                    if (IsAssignableFrom(typeof(T), type) && HasParameterlessConstructor(type) && !IsAbstract(type))
                        instances.Add((T)Activator.CreateInstance(type));
            return instances;
        }

        public static IList<Type> GetDerivedTypes(Type derivedType)
        {
            return
                (
                    from assembly in Assemblies
                    from type in assembly.GetTypes()
                    where IsAssignableFrom(derivedType, type)
                    select type
                ).ToList();
        }

        public static IList<Type> GetTagedTypes(Type attributeType)
        {
            return
                (
                    from assembly in Assemblies
                    from type in assembly.GetTypes()
                    where type.GetCustomAttributes(attributeType, false).Length == 1
                    select type
                ).ToList();
        }

        public static IList<Attribute> GetTagedTypesAttributes(Type attributeType)
        {
            return
                (
                    from assembly in Assemblies
                    from type in assembly.GetTypes()
                    where type.GetCustomAttribute(attributeType, false) != null
                    select type.GetCustomAttribute(attributeType, false)
                ).ToList();
        }

        public static IList<_AttributeType> GetTagedTypesAttributes<_AttributeType>() where _AttributeType : Attribute
        {
            var attributeType = typeof(_AttributeType);
            return
                (
                    from assembly in Assemblies
                    from type in assembly.GetTypes()
                    where type.GetCustomAttribute(attributeType, false) != null
                    select type.GetCustomAttribute(attributeType, false) as _AttributeType
                ).ToList();
        }

        public static IList<CustomAttributeData> GetTagedTypesAttributeDatas<_AttributeType>()
        {
            var attributeType = typeof(_AttributeType);
            return
                (
                    from assembly in Assemblies
                    from type in assembly.GetTypes()
                    from data in type.GetCustomAttributesData()
                    where data.AttributeType == attributeType
                    select data
                ).ToList();
        }

        public static bool IsAssignableFrom(Type a, Type b)
        {
            return a.IsAssignableFrom(b);
        }

        public static Type GetGenericTypeDefinition(Type type)
        {
            return type.GetGenericTypeDefinition();
        }

        public static Type[] GetGenericArguments(Type type)
        {
            return type.GetGenericArguments();
        }

        public static int GetArrayRank(Type type)
        {
            return type.GetArrayRank();
        }

        public static string GetAssemblyQualifiedName(Type type)
        {
            return type.AssemblyQualifiedName;
        }

        public static DiagramReflectedMethod GetMethod(Type type, string methodName, Type[] genericParameters, Type[] parameterTypes)
        {
            return new DiagramReflectedMethod(type, methodName, genericParameters, parameterTypes);
        }

        public static bool TypeIsArray(Type type)
        {
            return type.IsArray;
        }

        public static Type GetElementType(Type type)
        {
            return type.GetElementType();
        }
        public static bool IsAbstract(Type type)
        {
            return type.IsAbstract;
        }

        public static bool IsInterface(Type type)
        {
            return type.IsInterface;
        }

        public static bool IsGenericType(Type type)
        {
            return type.IsGenericType;
        }

        public static bool IsValueType(Type type)
        {
            return type.IsValueType;
        }

        public static bool IsEnum(Type type)
        {
            return type.IsEnum;
        }

        public static bool HasParameterlessConstructor(Type type)
        {
            if (IsValueType(type) || GetParameterlessConstructor(type) != null)
                return true;
            return false;
        }

        public static ConstructorInfo GetParameterlessConstructor(Type type)
        {
            var constructors = type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (var constructor in constructors)
                if (constructor.GetParameters().Length == 0)
                    return constructor;
            return null;
        }

        public static string GetShortAssemblyQualifiedName(Type type)
        {
            if (IsPrimitive(type))
                return type.ToString();
            return type.FullName + "," + type.Assembly.GetName().Name;
        }

        public static PropertyInfo GetProperty(Type type, string propertyName)
        {
            var property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (property == null && BaseType(type) != typeof(object))
                return GetProperty(BaseType(type), propertyName);
            return property;
        }

        public static FieldInfo GetField(Type type, string fieldName)
        {
            var field = type.GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null && BaseType(type) != typeof(object))
                return GetField(BaseType(type), fieldName);
            return field;
        }

        public static MethodInfo[] GetMethods(Type type, string methodName)
        {
            return type.GetMethods().Where(t => t.Name == methodName).ToArray();
        }

        public static bool IsPrimitive(Type type)
        {
            return (type.IsPrimitive || type == typeof(string) || type == typeof(decimal));
        }

        public static bool AttributeIsDefined(MemberInfo info, Type attributeType)
        {
            return Attribute.IsDefined(info, attributeType, true);
        }

        public static bool AttributeIsDefined(Type type, Type attributeType)
        {
            return type.IsDefined(attributeType, true);
        }

        public static bool ImplementsInterface(Type type, Type interfaceType)
        {
            return (type.GetInterface(interfaceType.Name) != null);
        }

        public static Type BaseType(Type type)
        {
            return type.BaseType;
        }

        public static Type GetType(string typeString)
        {
            switch (typeString)
            {
                case "bool":
                    return typeof(bool);
                case "byte":
                    return typeof(byte);
                case "sbyte":
                    return typeof(sbyte);
                case "char":
                    return typeof(char);
                case "decimal":
                    return typeof(decimal);
                case "double":
                    return typeof(double);
                case "float":
                    return typeof(float);
                case "int":
                    return typeof(int);
                case "uint":
                    return typeof(uint);
                case "long":
                    return typeof(long);
                case "ulong":
                    return typeof(ulong);
                case "short":
                    return typeof(short);
                case "ushort":
                    return typeof(ushort);
                case "string":
                    return typeof(string);
                case "Vector2":
                    return typeof(UnityEngine.Vector2);
                case "Vector3":
                    return typeof(UnityEngine.Vector3);
                case "Vector4":
                    return typeof(UnityEngine.Vector4);
                case "Color":
                    return typeof(Color);
                case "Transform":
                    return typeof(Transform);
                case "Component":
                    return typeof(UnityEngine.Component);
                case "GameObject":
                    return typeof(GameObject);
                case "MeshFilter":
                    return typeof(MeshFilter);
                case "Material":
                    return typeof(Material);
                case "Texture2D":
                    return typeof(Texture2D);
                case "UnityEngine.Object":
                    return typeof(UnityEngine.Object);
                case "System.Object":
                    return typeof(object);
                default:
                    return Type.GetType(typeString);
            }
        }

        public static string GetTypeString(Type type)
        {
            if (type == typeof(bool))
                return "bool";
            else if (type == typeof(byte))
                return "byte";
            else if (type == typeof(sbyte))
                return "sbyte";
            else if (type == typeof(char))
                return "char";
            else if (type == typeof(decimal))
                return "decimal";
            else if (type == typeof(double))
                return "double";
            else if (type == typeof(float))
                return "float";
            else if (type == typeof(int))
                return "int";
            else if (type == typeof(uint))
                return "uint";
            else if (type == typeof(long))
                return "long";
            else if (type == typeof(ulong))
                return "ulong";
            else if (type == typeof(short))
                return "short";
            else if (type == typeof(ushort))
                return "ushort";
            else if (type == typeof(string))
                return "string";
            else if (type == typeof(UnityEngine.Vector2))
                return "Vector2";
            else if (type == typeof(UnityEngine.Vector3))
                return "Vector3";
            else if (type == typeof(UnityEngine.Vector4))
                return "Vector4";
            else if (type == typeof(Color))
                return "Color";
            else if (type == typeof(Transform))
                return "Transform";
            else if (type == typeof(UnityEngine.Component))
                return "Component";
            else if (type == typeof(GameObject))
                return "GameObject";
            else if (type == typeof(MeshFilter))
                return "MeshFilter";
            else if (type == typeof(Material))
                return "Material";
            else if (type == typeof(Texture2D))
                return "Texture2D";
            else if (type == typeof(UnityEngine.Object))
                return "UnityEngine.Object";
            else if (type == typeof(object))
                return "System.Object";
            else
                return GetShortAssemblyQualifiedName(type);
        }

        /*
        * 	Allows us to use FieldInfo and PropertyInfo interchangably.
        */
        public struct DiagramReflectedMember
        {
            // The FieldInfo or PropertyInfo for this field.
            private FieldInfo fieldInfo;
            private PropertyInfo propertyInfo;
            public bool isProperty;

            public bool IsNull { get { return fieldInfo == null && propertyInfo == null; } }
            public string Name { get { return (isProperty ? propertyInfo.Name : fieldInfo.Name); } }
            public Type MemberType { get { return (isProperty ? propertyInfo.PropertyType : fieldInfo.FieldType); } }
            public bool IsPublic { get { return (isProperty ? (propertyInfo.GetGetMethod(true).IsPublic && propertyInfo.GetSetMethod(true).IsPublic) : fieldInfo.IsPublic); } }
            public bool IsProtected { get { return (isProperty ? (propertyInfo.GetGetMethod(true).IsFamily) : fieldInfo.IsFamily); } }
            public bool IsStatic { get { return (isProperty ? (propertyInfo.GetGetMethod(true).IsStatic) : fieldInfo.IsStatic); } }

            public DiagramReflectedMember(System.Object fieldPropertyInfo)
            {
                if (fieldPropertyInfo == null)
                {
                    this.propertyInfo = null;
                    this.fieldInfo = null;
                    isProperty = false;
                    return;
                }

                isProperty = IsAssignableFrom(typeof(PropertyInfo), fieldPropertyInfo.GetType());
                if (isProperty)
                {
                    this.propertyInfo = (PropertyInfo)fieldPropertyInfo;
                    this.fieldInfo = null;
                }
                else
                {
                    this.fieldInfo = (FieldInfo)fieldPropertyInfo;
                    this.propertyInfo = null;
                }
            }

            public void SetValue(System.Object obj, System.Object value)
            {
                if (isProperty)
                    propertyInfo.SetValue(obj, value, null);
                else
                    fieldInfo.SetValue(obj, value);
            }

            public System.Object GetValue(System.Object obj)
            {
                if (isProperty)
                    return propertyInfo.GetValue(obj, null);
                else
                    return fieldInfo.GetValue(obj);
            }
        }

        public class DiagramReflectedMethod
        {
            private MethodInfo method;
            public MethodInfo CoreMethod { get => method; private set => method = value; }
            public int ArgsTotal { get; private set; }

            public static DiagramReflectedMethod Temp(Action action) => new(action, 0);
            public static DiagramReflectedMethod Temp<T>(Action<T> action) => new(action, 1);
            public static DiagramReflectedMethod Temp<T1, T2>(Action<T1, T2> action) => new(action, 2);
            public static DiagramReflectedMethod Temp<T1, T2, T3>(Action<T1, T2, T3> action) => new(action, 3);
            public static DiagramReflectedMethod Temp<T1, T2, T3, T4>(Action<T1, T2, T3, T4> action) => new(action, 4);

            public static DiagramReflectedMethod Temp<Result>(Func<Result> action) => new(action, 0);
            public static DiagramReflectedMethod Temp<T, Result>(Func<T, Result> action) => new(action, 1);
            public static DiagramReflectedMethod Temp<T1, T2, Result>(Func<T1, T2, Result> action) => new(action, 2);
            public static DiagramReflectedMethod Temp<T1, T2, T3, Result>(Func<T1, T2, T3, Result> action) => new(action, 3);
            public static DiagramReflectedMethod Temp<T1, T2, T3, T4, Result>(Func<T1, T2, T3, T4, Result> action) => new(action, 4);

            public DiagramReflectedMethod(Delegate @delegate, int argsTotal)
            {
                this.CoreMethod = @delegate.Method;
                this.ArgsTotal = argsTotal;
            }

            public DiagramReflectedMethod(MethodInfo method, int argsTotal)
            {
                this.CoreMethod = method;
                ArgsTotal = argsTotal;
            }

            public DiagramReflectedMethod(Type type, string methodName, Type[] genericParameters, Type[] parameterTypes)
            {
                MethodInfo nonGenericMethod = type.GetMethod(methodName, parameterTypes);
                this.CoreMethod = nonGenericMethod.MakeGenericMethod(genericParameters);
                ArgsTotal = parameterTypes == null ? 0 : parameterTypes.Length;
            }

            public DiagramReflectedMethod(Type type, string methodName, Type[] genericParameters, Type[] parameterTypes, BindingFlags bindingAttr)
            {
                MethodInfo nonGenericMethod = type.GetMethod(methodName, bindingAttr, null, parameterTypes, null);
                this.CoreMethod = nonGenericMethod.MakeGenericMethod(genericParameters);
                ArgsTotal = parameterTypes == null ? 0 : parameterTypes.Length;
            }

            public DiagramReflectedMethod(Type type, string methodName, Type[] parameterTypes)
            {
                MethodInfo nonGenericMethod = type.GetMethod(methodName, parameterTypes);
                this.CoreMethod = nonGenericMethod;
                ArgsTotal = parameterTypes == null ? 0 : parameterTypes.Length;
            }

            public DiagramReflectedMethod(Type type, string methodName, Type[] parameterTypes, BindingFlags bindingAttr)
            {
                MethodInfo nonGenericMethod = type.GetMethod(methodName, bindingAttr, null, parameterTypes, null);
                this.CoreMethod = nonGenericMethod;
                ArgsTotal = parameterTypes == null ? 0 : parameterTypes.Length;
            }

            public object Invoke(object obj, object[] parameters)
            {
                return CoreMethod.Invoke(obj, parameters);
            }
        }

        public static void _ShowAttributeData(IList<CustomAttributeData> attributes)
        {
            foreach (CustomAttributeData cad in attributes)
            {
                UnityEngine.Debug.Log("   " + cad);
                UnityEngine.Debug.Log("      Constructor: '" + cad.Constructor + "'");

                UnityEngine.Debug.Log("      Constructor arguments:");
                foreach (CustomAttributeTypedArgument cata
                    in cad.ConstructorArguments)
                {
                    _ShowValueOrArray(cata);
                }

                UnityEngine.Debug.Log("      Named arguments:");
                foreach (CustomAttributeNamedArgument cana
                    in cad.NamedArguments)
                {
                    UnityEngine.Debug.Log("         MemberInfo: '" + cana.MemberInfo + "'");
                    _ShowValueOrArray(cana.TypedValue);
                }
            }
        }

        public static void _ShowValueOrArray(CustomAttributeTypedArgument cata)
        {
            if (cata.Value.GetType() == typeof(ReadOnlyCollection<CustomAttributeTypedArgument>))
            {
                UnityEngine.Debug.Log("         Array of '" + cata.ArgumentType + "':");

                foreach (CustomAttributeTypedArgument cataElement in
                    (ReadOnlyCollection<CustomAttributeTypedArgument>)cata.Value)
                {
                    Debug.Log("             Type: '" + cataElement.ArgumentType + "'  Value: '" + cataElement.Value + "'");
                }
            }
            else
            {
                Debug.Log("         Type: '" + cata.ArgumentType + "'  Value: '" + cata.Value + "'");
            }
        }

        public static CustomAttributeNamedArgument Find(this CustomAttributeData self, string name)
        {
            return self.NamedArguments.First(T => T.MemberName == name);
        }

        public static object GetValue(this CustomAttributeData self, string name)
        {
            return self.NamedArguments.First(T => T.MemberName == name).TypedValue.Value;
        }

    }

    #endregion

    #region File And Stream
    public static class ToolStreamEnum
    {
        public enum Location { File, PlayerPrefs, InternalMS, Resources, Cache };
        public enum Directory { PersistentDataPath, DataPath }
        public enum EncryptionType { None, AES };
        public enum CompressionType { None, Gzip };
        public enum Format { JSON };
        //public enum ReferenceMode { ByRef, ByValue, ByRefAndValue };
        public enum FileMode { Read, Write, Append }
    }

    public static class ToolHash
    {
#if NETFX_CORE
		public static string SHA1Hash(string input)
		{
			return System.Text.Encoding.UTF8.GetString(UnityEngine.Windows.Crypto.ComputeSHA1Hash(System.Text.Encoding.UTF8.GetBytes(input)));
		}
#else
        public static string SHA1Hash(string input)
        {
            using SHA1Managed sha1 = new SHA1Managed();
            return System.Text.Encoding.UTF8.GetString(sha1.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input)));
        }
#endif
    }

    public class DiagramFileStream : FileStream
    {
        private bool isDisposed = false;

        public DiagramFileStream(string path, ToolStreamEnum.FileMode fileMode, int bufferSize, bool useAsync)
            : base(GetPath(path, fileMode), GetFileMode(fileMode), GetFileAccess(fileMode), FileShare.None, bufferSize, useAsync)
        {
        }

        // Gets a temporary path if necessary.
        protected static string GetPath(string path, ToolStreamEnum.FileMode fileMode)
        {
            string directoryPath = ToolFile.GetDirectoryPath(path);
            // Attempt to create the directory incase it does not exist if we are storing data.
            if (fileMode != ToolStreamEnum.FileMode.Read && directoryPath != ToolFile.persistentDataPath)
                ToolFile.CreateDirectory(directoryPath);
            if (fileMode != ToolStreamEnum.FileMode.Write || fileMode == ToolStreamEnum.FileMode.Append)
                return path;
            return (fileMode == ToolStreamEnum.FileMode.Write) ? path + ToolFile.temporaryFileSuffix : path;
        }

        protected static FileMode GetFileMode(ToolStreamEnum.FileMode fileMode)
        {
            if (fileMode == ToolStreamEnum.FileMode.Read)
                return FileMode.Open;
            else if (fileMode == ToolStreamEnum.FileMode.Write)
                return FileMode.Create;
            else
                return FileMode.Append;
        }

        protected static FileAccess GetFileAccess(ToolStreamEnum.FileMode fileMode)
        {
            if (fileMode == ToolStreamEnum.FileMode.Read)
                return FileAccess.Read;
            else if (fileMode == ToolStreamEnum.FileMode.Write)
                return FileAccess.Write;
            else
                return FileAccess.Write;
        }

        protected override void Dispose(bool disposing)
        {
            // Ensure we only perform disposable once.
            if (isDisposed)
                return;
            isDisposed = true;

            base.Dispose(disposing);
        }
    }

    public class DiagramPlayerPrefsStream : MemoryStream
    {
        private string path;
        private bool append;
        private bool isWriteStream = false;
        private bool isDisposed = false;

        // This constructor should be used for read streams only.
        public DiagramPlayerPrefsStream(string path) : base(GetData(path, false))
        {
            this.path = path;
            this.append = false;
        }

        // This constructor should be used for write streams only.
        public DiagramPlayerPrefsStream(string path, int bufferSize, bool append = false) : base(bufferSize)
        {
            this.path = path;
            this.append = append;
            this.isWriteStream = true;
        }

        private static byte[] GetData(string path, bool isWriteStream)
        {
            if (!PlayerPrefs.HasKey(path))
                throw new FileNotFoundException("File \"" + path + "\" could not be found in PlayerPrefs");
            return System.Convert.FromBase64String(PlayerPrefs.GetString(path));
        }

        protected override void Dispose(bool disposing)
        {
            if (isDisposed)
                return;
            isDisposed = true;
            if (isWriteStream && this.Length > 0)
            {
                if (append)
                {
                    // Convert data back to bytes before appending, as appending Base-64 strings directly can corrupt the data.
                    var sourceBytes = System.Convert.FromBase64String(PlayerPrefs.GetString(path));
                    var appendBytes = this.ToArray();
                    var finalBytes = new byte[sourceBytes.Length + appendBytes.Length];
                    System.Buffer.BlockCopy(sourceBytes, 0, finalBytes, 0, sourceBytes.Length);
                    System.Buffer.BlockCopy(appendBytes, 0, finalBytes, sourceBytes.Length, appendBytes.Length);

                    PlayerPrefs.SetString(path, System.Convert.ToBase64String(finalBytes));

                    PlayerPrefs.Save();
                }
                else
                    PlayerPrefs.SetString(path + ToolFile.temporaryFileSuffix, System.Convert.ToBase64String(this.ToArray()));
                // Save the timestamp to a separate key.
                PlayerPrefs.SetString("timestamp_" + path, System.DateTime.UtcNow.Ticks.ToString());
            }
            base.Dispose(disposing);
        }
    }

    public class DiagramResourcesStream : MemoryStream
    {
        // Check that data exists by checking stream is not empty.
        public bool Exists { get { return this.Length > 0; } }

        // Used when creating 
        public DiagramResourcesStream(string path) : base(GetData(path))
        {
        }

        private static byte[] GetData(string path)
        {
            var textAsset = Resources.Load(path) as TextAsset;

            // If data doesn't exist in Resources, return an empty byte array.
            if (textAsset == null)
                return new byte[0];

            return textAsset.bytes;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }
    }

    public abstract class EncryptionAlgorithm
    {
        public abstract byte[] Encrypt(byte[] bytes, string password, int bufferSize);
        public abstract byte[] Decrypt(byte[] bytes, string password, int bufferSize);
        public abstract void Encrypt(Stream input, Stream output, string password, int bufferSize);
        public abstract void Decrypt(Stream input, Stream output, string password, int bufferSize);

        protected static void CopyStream(Stream input, Stream output, int bufferSize)
        {
            byte[] buffer = new byte[bufferSize];
            int read;
            while ((read = input.Read(buffer, 0, bufferSize)) > 0)
                output.Write(buffer, 0, read);
        }
    }

    public class AESEncryptionAlgorithm : EncryptionAlgorithm
    {
        private const int ivSize = 16;
        private const int keySize = 16;
        private const int pwIterations = 100;

        public override byte[] Encrypt(byte[] bytes, string password, int bufferSize)
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            Encrypt(input, output, password, bufferSize);
            return output.ToArray();
        }

        public override byte[] Decrypt(byte[] bytes, string password, int bufferSize)
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            Decrypt(input, output, password, bufferSize);
            return output.ToArray();
        }

        public override void Encrypt(Stream input, Stream output, string password, int bufferSize)
        {
            input.Position = 0;

#if NETFX_CORE
            // Generate an IV and write it to the output.
            var iv = CryptographicBuffer.GenerateRandom(ivSize);
            output.Write(iv.ToArray(), 0, ivSize);

            var pwBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);
            var keyDerivationProvider = KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(iv, pwIterations);
            // Create a key based on original key and derivation parmaters
            CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
            IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, keySize);

            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            // Get the input stream as an IBuffer.
            IBuffer msg;
            using(var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                msg = ms.ToArray().AsBuffer();
            }

            var buffEncrypt = CryptographicEngine.Encrypt(key, msg, iv);


            output.Write(buffEncrypt.ToArray(), 0, (int)buffEncrypt.Length);
            output.Dispose();
#else
            using (var alg = Aes.Create())
            {
                alg.Mode = CipherMode.CBC;
                alg.Padding = PaddingMode.PKCS7;
                alg.GenerateIV();
                var key = new Rfc2898DeriveBytes(password, alg.IV, pwIterations);
                alg.Key = key.GetBytes(keySize);
                // Write the IV to the output stream.
                output.Write(alg.IV, 0, ivSize);
                using var encryptor = alg.CreateEncryptor();
                using var cs = new CryptoStream(output, encryptor, CryptoStreamMode.Write);
                CopyStream(input, cs, bufferSize);
            }
#endif
        }

        public override void Decrypt(Stream input, Stream output, string password, int bufferSize)
        {
#if NETFX_CORE
            var thisIV = new byte[ivSize];
            input.Read(thisIV, 0, ivSize);
            var iv = thisIV.AsBuffer();

            var pwBuffer = CryptographicBuffer.ConvertStringToBinary(password, BinaryStringEncoding.Utf8);

            var keyDerivationProvider = KeyDerivationAlgorithmProvider.OpenAlgorithm("PBKDF2_SHA1");
            KeyDerivationParameters pbkdf2Parms = KeyDerivationParameters.BuildForPbkdf2(iv, pwIterations);
            // Create a key based on original key and derivation parameters.
            CryptographicKey keyOriginal = keyDerivationProvider.CreateKey(pwBuffer);
            IBuffer keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyOriginal, pbkdf2Parms, keySize);
            
            var provider = SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7);
            var key = provider.CreateSymmetricKey(keyMaterial);

            // Get the input stream as an IBuffer.
            IBuffer msg;
            using(var ms = new MemoryStream())
            {
                input.CopyTo(ms);
                msg = ms.ToArray().AsBuffer();
            }

            var buffDecrypt = CryptographicEngine.Decrypt(key, msg, iv);

            output.Write(buffDecrypt.ToArray(), 0, (int)buffDecrypt.Length);
#else
            using (var alg = Aes.Create())
            {
                var thisIV = new byte[ivSize];
                input.Read(thisIV, 0, ivSize);
                alg.IV = thisIV;

                var key = new Rfc2898DeriveBytes(password, alg.IV, pwIterations);
                alg.Key = key.GetBytes(keySize);

                using var decryptor = alg.CreateDecryptor();
                using var cryptoStream = new CryptoStream(input, decryptor, CryptoStreamMode.Read);
                CopyStream(cryptoStream, output, bufferSize);

            }
#endif
            output.Position = 0;
        }
    }

    public class UnbufferedCryptoStream : MemoryStream
    {
        private readonly Stream stream;
        private readonly bool isReadStream;
        private string password;
        private int bufferSize;
        private EncryptionAlgorithm alg;
        private bool disposed = false;

        public UnbufferedCryptoStream(Stream stream, bool isReadStream, string password, int bufferSize, EncryptionAlgorithm alg) : base()
        {
            this.stream = stream;
            this.isReadStream = isReadStream;
            this.password = password;
            this.bufferSize = bufferSize;
            this.alg = alg;


            if (isReadStream)
                alg.Decrypt(stream, this, password, bufferSize);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposed)
                return;
            disposed = true;

            if (!isReadStream)
                alg.Encrypt(this, stream, password, bufferSize);
            stream.Dispose();
            base.Dispose(disposing);
        }
    }

    [Serializable]
    public sealed class ToolFile : IDisposable
    {
        public static implicit operator bool(ToolFile file) => file.ErrorException == null;

        public string FilePath { get => m_FilePath; private set => m_FilePath = value; }
        public DateTime Timestamp { get; private set; } = DateTime.UtcNow;
        public bool IsError { get => isError; private set => isError = value; }
        public bool IsEmpty { get => isEmpty; private set => isEmpty = value; }
        public Exception ErrorException { get; private set; } = null;
        public bool IsKeepToolFileControl { get => isKeepToolFileControl; private set => isKeepToolFileControl = value; }
        private Stream FileStream;
        internal Stream _MyStream => FileStream;
        public byte[] FileData { get; private set; } = null;

        [SerializeField] private string m_FilePath = "";
        [SerializeField] private bool isDelete = false;
        [SerializeField] private bool isError = false;
        [SerializeField] private bool isEmpty = false;
        [SerializeField] private bool isKeepToolFileControl = false;

        public void Delete()
        {
            this.Dispose();
            DeleteFile(FilePath);
            ErrorException = null;
            IsError = false;
            IsEmpty = true;
            isDelete = true;
        }

        /// <summary>
        /// Just Use This ToolFile Delete , You Can Use This Function To Create
        /// </summary>
        /// <param name="isRefresh"></param>
        /// <param name="isKeepToolFileontrol"></param>
        /// <returns></returns>
        public bool Create(bool isRefresh = true, bool isKeepToolFileontrol = true)
        {
            if (isDelete)
            {
                try
                {
                    if (File.Exists(FilePath))
                    {
                        Timestamp = File.GetLastWriteTime(FilePath).ToUniversalTime();
                    }
                    else File.Create(FilePath);
                    InitFileStream(isRefresh, isKeepToolFileontrol);
                }
                catch (Exception ex)
                {
                    SetErrorStatus(ex);
                    return false;
                }
                return true;
            }
            return false;
        }

        ~ToolFile()
        {
            Dispose();
        }

        public ToolFile(string filePath, bool isTryCreate, bool isRefresh, bool isKeepToolFileControl)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                }
                else if (!isTryCreate)
                {
                    SetErrorStatus(new FileNotExist());
                    return;
                }
                else ToolFile.CreateFile(filePath);
                InitFileStream(isRefresh, isKeepToolFileControl);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ToolFile(string filePath, bool isTryCreate, bool isRefresh, Stream stream)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                }
                else if (!isTryCreate)
                {
                    SetErrorStatus(new FileNotExist());
                    return;
                }
                else ToolFile.CreateFile(filePath);
                InitFileStream(isRefresh, stream);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ToolFile(bool isCanOverwrite, string filePath, bool isRefresh, bool isKeepToolFileControl)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    if (isCanOverwrite)
                    {
                        SetErrorStatus(new FileExist());
                        return;
                    }
                }
                else ToolFile.CreateFile(filePath);
                Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                InitFileStream(isRefresh, isKeepToolFileControl);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        public ToolFile(bool isCanOverwrite, string filePath, bool isRefresh, Stream stream)
        {
            try
            {
                FilePath = filePath;
                if (File.Exists(filePath))
                {
                    if (isCanOverwrite)
                    {
                        SetErrorStatus(new FileExist());
                        return;
                    }
                }
                else ToolFile.CreateFile(filePath);
                Timestamp = File.GetLastWriteTime(filePath).ToUniversalTime();
                InitFileStream(isRefresh, stream);
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
            }
        }

        private void InitFileStream(bool isRefresh, bool isKeepToolFileontrol)
        {
            if (this.IsKeepToolFileControl = isKeepToolFileontrol)
            {
                FileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
            }
            if (isRefresh) UpdateFileData();
        }

        private void InitFileStream(bool isRefresh, Stream stream)
        {
            if (isRefresh) UpdateFileData(stream);
        }

        private void SetErrorStatus(Exception ex)
        {
            this.IsError = true;
            this.IsEmpty = true;
            this.ErrorException = ex;
            Timestamp = DateTime.UtcNow;
            Debug.LogException(ex);
        }

        private bool DebugMyself()
        {
            if (this.IsEmpty || this.ErrorException != null)
            {
                Debug.LogWarning("This File Was Drop in a error : " + this.ErrorException.Message);
                Debug.LogException(ErrorException);
                return true;
            }
            return false;
        }

        public void UpdateFileData()
        {
            if (DebugMyself()) return;
            if (this.IsKeepToolFileControl)
            {
                UpdateFileData(FileStream);
            }
            else
            {
                using var nFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite);
                UpdateFileData(nFileStream);
            }
        }

        public void UpdateFileData(Stream stream)
        {
            if (DebugMyself()) return;
            FileData = new byte[stream.Length];
            byte[] buffer = new byte[256];
            int len, i = 0;
            while ((len = stream.Read(buffer, 0, buffer.Length)) != 0)
            {
                for (int j = 0; j < len; j++)
                {
                    FileData[i++] = buffer[j];
                }
            }
        }

        public AssetBundle LoadAssetBundle()
        {
            if (DebugMyself()) return null;
            return AssetBundle.LoadFromMemory(FileData);
        }

        public T LoadObject<T>(bool isRefresh, Func<string, T> loader, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return default;
            if (isRefresh) UpdateFileData();
            string str = encoding.GetString(FileData);
            return loader(str);
        }

        public object LoadObject<T>(bool isRefresh, Func<string, object> loader, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            string str = encoding.GetString(FileData);
            return loader(str);
        }

        public T LoadObject<T>(bool isRefresh, Func<string, T> loader)
        {
            if (DebugMyself()) return default;
            if (isRefresh) UpdateFileData();
            string str = System.Text.Encoding.Default.GetString(FileData);
            return loader(str);
        }

        public object LoadObject<T>(bool isRefresh, Func<string, object> loader)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            string str = System.Text.Encoding.Default.GetString(FileData);
            return loader(str);
        }

        public string GetString(bool isRefresh, System.Text.Encoding encoding)
        {
            if (DebugMyself()) return null;
            if (isRefresh) UpdateFileData();
            return encoding.GetString(FileData);
        }

        /// <summary>
        /// Text Mode
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="isRefresh"></param>
        /// <param name="encoding"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Deserialize<T>(bool isRefresh, System.Text.Encoding encoding, out object obj)
        {
            if (DebugMyself())
            {
                obj = ErrorException;
                return false;
            }
            string source = "";
            try
            {
                source = GetString(isRefresh, encoding);
                if (typeof(T).IsPrimitive)
                {
                    obj = typeof(T).GetMethod("Parse").Invoke(source, null);
                    return true;
                }
                else if (typeof(T).GetAttribute<SerializableAttribute>() != null)
                {
                    obj = JsonConvert.DeserializeObject<T>(source);
                    if (obj != null) return true;
                    else return false;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                Debug.LogError("ToolFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath + "\nsource : " + source);
                Debug.LogException(ex);
            }
            obj = default(T);
            return false;
        }

        /// <summary>
        /// Binary Stream Mode(Load From Immediate Current File)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="isText"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool Deserialize<T>(out object obj)
        {
            if (DebugMyself())
            {
                obj = ErrorException;
                return false;
            }
            try
            {
                if (IsKeepToolFileControl)
                {
                    obj = new BinaryFormatter().Deserialize(FileStream);
                }
                else
                {
                    using FileStream fs = new(FilePath, FileMode.Open);
                    obj = new BinaryFormatter().Deserialize(fs);
                }
                return obj != null;
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
                Debug.LogError("ToolFile.Deserialize<T>() : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
                obj = default(T);
                return false;
            }
        }

        public bool Serialize<T>(T obj, System.Text.Encoding encoding, bool isAllowSerializeAsBinary = true)
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                if (typeof(T).GetAttribute<SerializableAttribute>() == null)
                {
                    Debug.LogWarning("this type is not use SerializableAttribute but you now is try to serialize it");
                    if (!isAllowSerializeAsBinary) throw new BadImplemented();
                    using MemoryStream ms = new();
                    new BinaryFormatter().Serialize(ms, obj);
                    FileData = ms.GetBuffer();
                    SaveFileData();
                    return true;
                }
                else
                {
                    if (IsKeepToolFileControl)
                    {
                        this.FileData = encoding.GetBytes(JsonConvert.SerializeObject(obj, Formatting.Indented));
                        FileStream.Write(this.FileData, 0, this.FileData.Length);
                    }
                    else
                    {
                        File.WriteAllText(FilePath, JsonConvert.SerializeObject(obj, Formatting.Indented), encoding);
                        UpdateFileData();
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
#if UNITY_EDITOR
                Debug.LogError("ToolFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
#endif
                return false;
            }
        }

        /// <summary>
        /// Saved entirely in binary
        /// </summary>
        public bool Serialize<T>(T obj)
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                using MemoryStream ms = new();
                new BinaryFormatter().Serialize(ms, obj);
                this.FileData = ms.GetBuffer();
                FileData = ms.GetBuffer();
                SaveFileData();
                return true;
            }
            catch (Exception ex)
            {
                SetErrorStatus(ex);
#if UNITY_EDITOR
                Debug.LogError("ToolFile.Deserialize<T>(bool,Encoding) : T is " + typeof(T).FullName + " , is failed on " + FilePath);
                Debug.LogException(ex);
#endif
                return false;
            }
        }

        public void Close()
        {
            if (IsKeepToolFileControl)
            {
                FileStream?.Close();
                FileStream?.Dispose();
                FileStream = null;
                IsKeepToolFileControl = false;
                IsError = false;
                IsEmpty = true;
            }
            this.FileData = null;
        }

        public void Keep(bool isRefresh)
        {
            if (!IsKeepToolFileControl)
            {
                Close();
                InitFileStream(isRefresh, true);
            }
        }

        public void Dispose()
        {
            this.Close();
            this.FileData = null;
        }

        public void Append(byte[] appendition)
        {
            byte[] newData = new byte[appendition.Length + FileData.Length];
            Array.Copy(FileData, 0, newData, 0, FileData.Length);
            Array.Copy(appendition, 0, newData, FileData.Length, appendition.Length);
            FileData = newData;
        }

        public void ReplaceAllData(byte[] data)
        {
            FileData = data;
        }

        public static byte[] ToBytes(object obj)
        {
            using MemoryStream ms = new();
            new BinaryFormatter().Serialize(ms, obj);
            return ms.GetBuffer();
        }

        public static object FromBytes(byte[] bytes)
        {
            using MemoryStream ms = new();
            ms.Write(bytes, 0, bytes.Length);
            ms.Position = 0;
            return new BinaryFormatter().Deserialize(ms);
        }

        public bool SaveFileData()
        {
            if (DebugMyself())
            {
                return false;
            }
            try
            {
                if (IsKeepToolFileControl)
                {
                    FileStream.Write(FileData, 0, FileData.Length);
                }
                else
                {
                    File.WriteAllBytes(FilePath, FileData);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
                return false;
            }
            return true;
        }

        private const string _ErrorCannotWriteToResourcesWhenEditorTime
            = "Cannot write directly to Resources folder. Try writing to a directory outside of Resources, and then manually move the file there.";
        private const string _ErrorCannotWriteToResourcesWhenRuntime
            = "Cannot write to Resources folder at runtime. Use a different save location at runtime instead.";

        public static void CopyTo(Stream source, Stream destination)
        {
#if UNITY_2019_1_OR_NEWER
            source.CopyTo(destination);
#else
            byte[] buffer = new byte[2048];
            int bytesRead;
            while ((bytesRead = source.Read(buffer, 0, buffer.Length)) > 0)
                destination.Write(buffer, 0, bytesRead);
#endif
        }

        #region a

        //获取成这个文件的文件路径（不包括本身）
        public static DirectoryInfo GetDirectroryOfFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new PathNotExist();
            var dir_name = Path.GetDirectoryName(filePath);
            if (Directory.Exists(dir_name))
            {
                return Directory.GetParent(dir_name);
            }
            return null;
        }

        //生成这个文件的文件路径（不包括本身）
        public static bool TryCreateDirectroryOfFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new PathNotExist();
            var dir_name = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir_name))
            {
                Directory.CreateDirectory(dir_name);
                return false;
            }
            else return true;
        }

        public static void ReCreateDirectroryOfFile(string filePath, bool recursive = true)
        {
            if (string.IsNullOrEmpty(filePath)) throw new PathNotExist();
            var dir_name = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir_name))
            {
                Directory.CreateDirectory(dir_name);
            }
            else
            {
                Directory.Delete(dir_name, recursive);
                Directory.CreateDirectory(dir_name);
            }
        }

        //生成这个文件的文件路径（不包含本身）
        public static DirectoryInfo CreateDirectroryOfFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) throw new PathNotExist();
            var dir_name = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir_name))
            {
                return Directory.CreateDirectory(dir_name);
            }
            else
            {
                return Directory.GetParent(dir_name);
            }
        }

        //移动整个路径
        public static void MoveFolder(string sourcePath, string destPath)
        {
            if (Directory.Exists(sourcePath))
            {
                if (!Directory.Exists(destPath))
                {
                    //目标目录不存在则创建 
                    try
                    {
                        Directory.CreateDirectory(destPath);
                    }
                    catch (Exception ex)
                    {
                        //throw new Exception(" public static void MoveFolder(string sourcePath, string destPath),Target Directory fail to create" + ex.Message);
                        Debug.LogWarning("public static void MoveFolder(string sourcePath, string destPath),Target Directory fail to create" + ex.Message);
                        return;
                    }
                }
                //获得源文件下所有文件 
                List<string> files = new(Directory.GetFiles(sourcePath));
                files.ForEach(c =>
                {
                    string destFile = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //覆盖模式 
                    if (File.Exists(destFile))
                    {
                        File.Delete(destFile);
                    }
                    File.Move(c, destFile);
                });
                //获得源文件下所有目录文件 
                List<string> folders = new List<string>(Directory.GetDirectories(sourcePath));

                folders.ForEach(c =>
                {
                    string destDir = Path.Combine(new string[] { destPath, Path.GetFileName(c) });
                    //Directory.Move必须要在同一个根目录下移动才有效，不能在不同卷中移动。 
                    //Directory.Move(c, destDir); 

                    //采用递归的方法实现 
                    MoveFolder(c, destDir);
                });
            }
            else
            {
                //throw new Exception(" public static void MoveFolder(string sourcePath, string destPath),sourcePath cannt find");
                Debug.Log("public static void MoveFolder(string sourcePath, string destPath),sourcePath cannt find");
            }
        }

        //拷贝整个路径
        public static void CopyFilefolder(string sourceFilePath, string targetFilePath)
        {
            //获取源文件夹中的所有非目录文件
            string[] files = Directory.GetFiles(sourceFilePath);
            string fileName;
            string destFile;
            //如果目标文件夹不存在，则新建目标文件夹
            if (!Directory.Exists(targetFilePath))
            {
                Directory.CreateDirectory(targetFilePath);
            }
            //将获取到的文件一个一个拷贝到目标文件夹中 
            foreach (string s in files)
            {
                fileName = Path.GetFileName(s);
                destFile = Path.Combine(targetFilePath, fileName);
                File.Copy(s, destFile, true);
            }
            //上面一段在MSDN上可以看到源码 

            //获取并存储源文件夹中的文件夹名
            string[] filefolders = Directory.GetFiles(sourceFilePath);
            //创建Directoryinfo实例 
            DirectoryInfo dirinfo = new DirectoryInfo(sourceFilePath);
            //获取得源文件夹下的所有子文件夹名
            DirectoryInfo[] subFileFolder = dirinfo.GetDirectories();
            for (int j = 0; j < subFileFolder.Length; j++)
            {
                //获取所有子文件夹名 
                string subSourcePath = sourceFilePath + "\\" + subFileFolder[j].ToString();
                string subTargetPath = targetFilePath + "\\" + subFileFolder[j].ToString();
                //把得到的子文件夹当成新的源文件夹，递归调用CopyFilefolder
                CopyFilefolder(subSourcePath, subTargetPath);
            }
        }

        //重命名文件
        public static void FileRename(string sourceFile, string newNameWithFullPath)
        {
            CopyFile(sourceFile, newNameWithFullPath);
            DeleteFile(sourceFile);
        }

        /*public static /*ExecutionResult void FileRename(string sourceFile, string destinationPath, string destinationFileName)
        {
            //ExecutionResult result;
            FileInfo tempFileInfo;
            FileInfo tempBakFileInfo;
            DirectoryInfo tempDirectoryInfo;

            //result = new ExecutionResult();
            tempFileInfo = new FileInfo(sourceFile);
            tempDirectoryInfo = new DirectoryInfo(destinationPath);
            tempBakFileInfo = new FileInfo(destinationPath + "\\" + destinationFileName);
            try
            {
                if (!tempDirectoryInfo.Exists)
                    tempDirectoryInfo.Create();
                if (tempBakFileInfo.Exists)
                    tempBakFileInfo.Delete();
                //move file to bak
                tempFileInfo.MoveTo(destinationPath + "\\" + destinationFileName);

            //    result.Status = true;
            //    result.Message = "Rename file OK";
            //    result.Anything = "OK";
            }
            catch (Exception ex)
            {
                //    result.Status = false;
                //    result.Anything = "Mail";
                //   result.Message = ex.Message;
                //    if (mesLog.IsErrorEnabled)
                //    {
                //        mesLog.Error(MethodBase.GetCurrentMethod().Name, "Rename file error. Msg :" + ex.Message);
                //        mesLog.Error(ex.StackTrace);
                //    }
                Debug.LogWarning(MethodBase.GetCurrentMethod().Name + "Rename file error. Msg :" + ex.Message);
            }

           // return result;
        }*/

        private static Dictionary<string, List<FileInfo>> Files = new();

        public static void ClearAllFiles()
        {
            Files.Clear();
        }

        public static bool TryGetFiles(string group, out List<FileInfo> fileInfos)
        {
            return Files.TryGetValue(group, out fileInfos);
        }

        public static List<FileInfo> GetFiles(string group)
        {
            Files.TryGetValue(group, out var files);
            return files;
        }

        public static void LoadFiles(string group, string dictionary, Predicate<string> _Right)
        {
            if (Files.ContainsKey(group))
                Files[group] = Files[group].Union(FindAll(dictionary, _Right)).ToList();
            else
            {
                var result = FindAll(dictionary, _Right);
                if (result != null)
                    Files[group] = result;
            }
        }

        /// <summary>
        /// 获取该文件夹下顶部文件夹子项中使用该扩展名的文件
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="extension"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        public static List<FileInfo> FindAll(string dictionary, string extension, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            DirectoryInfo direction = new(dictionary);
            if (extension[0] == '.') extension = "*" + extension[1..];
            else if (extension[0] != '*') extension = "*" + extension;
            FileInfo[] files = direction.GetFiles(extension, searchOption);
            List<FileInfo> result = new();
            foreach (var it in files) result.Add(it);
            return result.Count != 0 ? result : null;
        }

        /// <summary>
        /// 获取该文件夹下顶部文件夹子项中匹配的部分（默认）
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="_Right"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        public static List<FileInfo> FindAll(string dictionary, Predicate<string> _Right, SearchOption searchOption = SearchOption.TopDirectoryOnly)
        {
            DirectoryInfo direction = new(dictionary);
            FileInfo[] files = direction.GetFiles("*", searchOption);
            List<FileInfo> result = new();
            foreach (var it in files)
                if (_Right(it.Name)) result.Add(it);
            return result.Count != 0 ? result : null;
        }

        /// <summary>
        /// 获取该文件夹下全部子项（默认）
        /// </summary>
        /// <param name="dictionary"></param>
        /// <param name="searchOption"></param>
        /// <returns></returns>
        public static List<FileInfo> FindAll(string dictionary, SearchOption searchOption = SearchOption.AllDirectories)
        {
            DirectoryInfo direction = new(dictionary);
            FileInfo[] files = direction.GetFiles("*", searchOption);
            List<FileInfo> result = new();
            foreach (var it in files) result.Add(it);
            return result.Count != 0 ? result : null;
        }

        public static FileInfo First(DirectoryInfo direction, string name)
        {
            return First(direction, T => Path.GetFileNameWithoutExtension(T) == name);
        }

        public static FileInfo First(string dictionary, string name)
        {
            return First(dictionary, T => Path.GetFileNameWithoutExtension(T) == name);
        }

        public static FileInfo First(string dictionary, Predicate<string> _Right)
        {
            DirectoryInfo direction = new(dictionary);
            FileInfo[] files = direction.GetFiles("*");
            foreach (var it in files)
                if (_Right(it.Name)) return it;
            return null;
        }

        public static FileInfo First(DirectoryInfo direction, Predicate<string> _Right)
        {
            FileInfo[] files = direction.GetFiles("*");
            foreach (var it in files)
                if (_Right(it.Name)) return it;
            return null;
        }

        public static AssetBundle LoadAssetBundle(string path)
        {
            return AssetBundle.LoadFromFile(path);
        }

        public static AssetBundle LoadAssetBundle(string path, params string[] targetsName)
        {
            AssetBundle asset = AssetBundle.LoadFromFile(path);
            foreach (var item in targetsName)
            {
                asset.LoadAsset(item);
            }
            return asset;
        }

        /// <summary>
        /// 分段，断点下载文件
        /// </summary>
        /// <param name="loadPath">下载地址</param>
        /// <param name="savePath">保存路径</param>
        /// <returns></returns>
        public static IEnumerator BreakpointResume(MonoBehaviour sendObject, string loadPath, string savePath, double loadedBytes, UnityAction<float, string> callback)
        {
            UnityWebRequest headRequest = UnityWebRequest.Head(loadPath);
            yield return headRequest.SendWebRequest();

            if (!string.IsNullOrEmpty(headRequest.error))
            {
                callback(-1, headRequest.error + ":cannt found the file");
                yield break;
            }
            headRequest.Dispose();
            using UnityWebRequest Request = UnityWebRequest.Get(loadPath);
            //append设置为true文件写入方式为接续写入，不覆盖原文件。
            Request.downloadHandler = new DownloadHandlerFile(savePath, true);
            FileInfo file = new FileInfo(savePath);

            //请求网络数据从第fileLength到最后的字节；
            Request.SetRequestHeader("Range", "bytes=" + file.Length + "-");

            if (Request.downloadProgress < 1)
            {
                Request.SendWebRequest();
                while (!Request.isDone)
                {
                    callback(Request.downloadProgress * 100, "%");
                    //超过一定的字节关闭现在的协程，开启新的协程，将资源分段下载
                    if (Request.downloadedBytes >= loadedBytes)
                    {
                        sendObject.StopCoroutine(nameof(BreakpointResume));

                        //如果 UnityWebRequest 在进行中，就停止。
                        Request.Abort();
                        if (!string.IsNullOrEmpty(headRequest.error))
                        {
                            callback(0, headRequest.error + ":failed");
                            yield break;
                        }
                        yield return sendObject.StartCoroutine(BreakpointResume(sendObject, loadPath, savePath, loadedBytes, callback));
                    }
                    yield return null;
                }
            }
            if (string.IsNullOrEmpty(Request.error)) callback(1, "succeed");
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reservedInt = 0;
            public int flagsEx = 0;
        }

        public class LocalDialog
        {
            [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
            public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);
            public static bool GetOFN([In, Out] OpenFileName ofn)
            {
                return GetOpenFileName(ofn);
            }

            [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Auto)]
            public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);
            public static bool GetSFN([In, Out] OpenFileName ofn)
            {
                return GetSaveFileName(ofn);
            }
        }

        public static OpenFileName SelectFileOnSystem(string labelName, string subLabelName, params string[] fileArgs)
        {
            OpenFileName targetFile = new OpenFileName();
            targetFile.structSize = Marshal.SizeOf(targetFile);
            targetFile.filter = labelName + "(*" + subLabelName + ")\0";
            for (int i = 0; i < fileArgs.Length - 1; i++)
            {
                targetFile.filter += "*." + fileArgs[i] + ";";
            }
            if (fileArgs.Length > 0) targetFile.filter += "*." + fileArgs[^1] + ";\0";
            targetFile.file = new string(new char[256]);
            targetFile.maxFile = targetFile.file.Length;
            targetFile.fileTitle = new string(new char[64]);
            targetFile.maxFileTitle = targetFile.fileTitle.Length;
            targetFile.initialDir = Application.streamingAssetsPath.Replace('/', '\\');//默认路径
            targetFile.title = "Select A Song";
            targetFile.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;
            return targetFile;
        }

        public static OpenFileName SelectFileOnSystem(UnityAction<string> action, string labelName, string subLabelName, params string[] fileArgs)
        {
            OpenFileName targetFile = SelectFileOnSystem(labelName, subLabelName, fileArgs);
            if (LocalDialog.GetOpenFileName(targetFile) && targetFile.file != "")
            {
                action(targetFile.file);
            }
            return targetFile;
        }

        public static bool IsAbsolute(string path)
        {
            if (path.Length > 0 && (path[0] == '/' || path[0] == '\\'))
                return true;
            if (path.Length > 1 && path[1] == ':')
                return true;
            return false;
        }

        public static Stream CreateFileStream(string FilePath, bool isWriteStream, int bufferSize = 1024, bool IsTurnIntoGZipStream = false)
        {
            Stream stream = null;
            // Check that the path is in a valid format. This will throw an exception if not.
            new FileInfo(FilePath);

            try
            {
                // There's no point in creating an empty MemoryStream if we're only reading from it.
                if (!isWriteStream)
                    return null;
                stream = new MemoryStream(bufferSize);
                return CreateStream(stream, isWriteStream, IsTurnIntoGZipStream);
            }
            catch (Exception ex)
            {
                stream?.Dispose();
                throw ex;
            }
        }

        public static Stream CreatePlayerPrefsStream(string FilePath, bool isWriteStream, bool isAppend, int bufferSize = 1024, bool IsTurnIntoGZipStream = false)
        {
            Stream stream = null;

            // Check that the path is in a valid format. This will throw an exception if not.
            new FileInfo(FilePath);

            try
            {
                if (isWriteStream)
                    stream = new DiagramPlayerPrefsStream(FilePath, bufferSize, isAppend);
                else
                {
                    if (!PlayerPrefs.HasKey(FilePath))
                        return null;
                    stream = new DiagramPlayerPrefsStream(FilePath);
                }
                return CreateStream(stream, isWriteStream, IsTurnIntoGZipStream);
            }
            catch (Exception ex)
            {
                stream?.Dispose();
                throw ex;
            }
        }

        public static Stream CreateResourcesStream(string FilePath, bool isWriteStream)
        {
            Stream stream = null;

            // Check that the path is in a valid format. This will throw an exception if not.
            new FileInfo(FilePath);

            try
            {
                if (!isWriteStream)
                {
                    var resourcesStream = new DiagramResourcesStream(FilePath);
                    if (resourcesStream.Exists)
                        stream = resourcesStream;
                    else
                    {
                        resourcesStream.Dispose();
                        return null;
                    }
                }
                else
                if (UnityEngine.Application.isEditor)
                    throw new System.NotSupportedException("Cannot write directly to Resources folder." +
                        " Try writing to a directory outside of Resources, and then manually move the file there.");
                else
                    throw new System.NotSupportedException("Cannot write to Resources folder at runtime." +
                        " Use a different save location at runtime instead.");
                return CreateStream(stream, isWriteStream, false);
            }
            catch (System.Exception e)
            {
                if (stream != null)
                    stream.Dispose();
                throw e;
            }
        }

        public static Stream CreateStream(Stream stream, bool isWriteStream, bool IsTurnIntoGZipStream)
        {
            try
            {
                if (IsTurnIntoGZipStream && stream.GetType() != typeof(GZipStream))
                {
                    stream = isWriteStream ? new GZipStream(stream, CompressionMode.Compress) : new GZipStream(stream, CompressionMode.Decompress);
                }

                return stream;
            }
            catch (System.Exception e)
            {
                stream?.Dispose();
                if (e.GetType() == typeof(System.Security.Cryptography.CryptographicException))
                    throw new System.Security.Cryptography.CryptographicException("Could not decrypt file." +
                        " Please ensure that you are using the same password used to encrypt the file.");
                else throw e;
            }
        }

        #endregion

        #region S

        public static string GetExtension(string path)
        {
            return Path.GetExtension(path);
        }

        public static void DeleteFile(string filePath)
        {
            if (FileExists(filePath))
                File.Delete(filePath);
        }

        public static void CreateFile(string filePath)
        {
            File.Create(filePath).Close();
        }

        public static void CreateFile(string filePath, out FileStream fileStream)
        {
            fileStream = File.Create(filePath);
        }

        public static bool FileExists(string filePath) { return File.Exists(filePath); }
        public static void MoveFile(string sourcePath, string destPath) { File.Move(sourcePath, destPath); }
        public static void CopyFile(string sourcePath, string destPath) { File.Copy(sourcePath, destPath); }

        public static void MoveDirectory(string sourcePath, string destPath) { Directory.Move(sourcePath, destPath); }
        public static void CreateDirectory(string directoryPath) { Directory.CreateDirectory(directoryPath); }
        public static bool DirectoryExists(string directoryPath) { return Directory.Exists(directoryPath); }

        /*
		 * 	Given a path, it returns the directory that path points to.
		 * 	eg. "C:/myFolder/thisFolder/myFile.txt" will return "C:/myFolder/thisFolder".
		 */
        public static string GetDirectoryPath(string path, char seperator = '/')
        {
            //return Path.GetDirectoryName(path);
            // Path.GetDirectoryName turns forward slashes to backslashes in some cases on Windows, which is why
            // Substring is used instead.
            char slashChar = UsesForwardSlash(path) ? '/' : '\\';

            int slash = path.LastIndexOf(slashChar);
            // Ignore trailing slash if necessary.
            if (slash == (path.Length - 1))
                slash = path.Substring(0, slash).LastIndexOf(slashChar);
            if (slash == -1)
                Debug.LogError("Path provided is not a directory path as it contains no slashes.");
            return path.Substring(0, slash);
        }

        public static bool UsesForwardSlash(string path)
        {
            if (path.Contains("/"))
                return true;
            return false;
        }

        // Takes a directory path and a file or directory name and combines them into a single path.
        public static string CombinePathAndFilename(string directoryPath, string fileOrDirectoryName)
        {
            if (directoryPath[directoryPath.Length - 1] != '/' && directoryPath[directoryPath.Length - 1] != '\\')
                directoryPath += '/';
            return directoryPath + fileOrDirectoryName;
        }

        public static string[] GetDirectories(string path, bool getFullPaths = true)
        {
            var paths = Directory.GetDirectories(path);
            for (int i = 0; i < paths.Length; i++)
            {
                if (!getFullPaths)
                    paths[i] = Path.GetFileName(paths[i]);
                // GetDirectories sometimes returns backslashes, so we need to convert them to
                // forward slashes.
                paths[i].Replace("\\", "/");
            }
            return paths;
        }

        public static void DeleteDirectory(string directoryPath)
        {
            if (DirectoryExists(directoryPath))
                Directory.Delete(directoryPath, true);
        }

        public static string[] GetFiles(string path, bool getFullPaths = true)
        {
            var paths = Directory.GetFiles(path);
            if (!getFullPaths)
            {
                for (int i = 0; i < paths.Length; i++)
                    paths[i] = Path.GetFileName(paths[i]);
            }
            return paths;
        }

        public static byte[] ReadAllBytes(string path)
        {
            return File.ReadAllBytes(path);
        }

        public static void WriteAllBytes(string path, byte[] bytes)
        {
            File.WriteAllBytes(path, bytes);
        }

        #endregion

        #region SS

#if UNITY_SWITCH
        public static readonly string persistentDataPath = "";
        public static readonly string dataPath = "";
#else   
        public static readonly string persistentDataPath = Application.persistentDataPath;
        public static readonly string userPath =
#if PLATFORM_STANDALONE_WIN
            Application.streamingAssetsPath;
#else
            Application.persistentDataPath;
#endif
        public static readonly string dataPath = Application.dataPath;
#endif
        public const string backupFileSuffix = ".bac";
        public const string temporaryFileSuffix = ".tmp";

        public static DateTime GetTimestamp(string filePath)
        {
            if (!FileExists(filePath))
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, System.DateTimeKind.Utc);
            return File.GetLastWriteTime(filePath).ToUniversalTime();
        }

        #endregion
    }

    [Serializable]
    public class OfflineFile
    {
        public List<byte[]> MainMapDatas = new();
        public Dictionary<string, byte[]> SourceAssetsDatas = new();
        public Dictionary<string, string> PathRelayers = new();

        public void Add(ICanMakeOffline target, HashSet<object> passSet = null)
        {
            passSet ??= new();
            if (!passSet.Add(target)) return;
            MainMapDatas.Add(ToolFile.ToBytes(target));
            foreach (var path in target.GetFilePaths())
            {
                if (!SourceAssetsDatas.ContainsKey(path))
                {
                    SourceAssetsDatas.Add(path, File.ReadAllBytes(path));
                }
            }
            foreach (var child in DiagramType.GetOrCreateDiagramType(target.GetType()).members.GetSubList(T => T.type.IsSubclassOf(typeof(ICanMakeOffline))))
            {
                object cat = child.reflectedMember.GetValue(target);
                Add(cat, passSet);
            }
        }

        public void Add(object target, HashSet<object> passSet = null)
        {
            if (target.As<ICanMakeOffline>(out var mO)) Add(mO, passSet);
            else
            {
                MainMapDatas.Add(ToolFile.ToBytes(target));
            }
        }

        public void Build(string path)
        {
            using ToolFile file = new(path, true, false, true);
            file.ReplaceAllData(ToolFile.ToBytes(this));
            file.SaveFileData();
        }

        public static OfflineFile BuildFrom(string path)
        {
            return ToolFile.FromBytes(File.ReadAllBytes(path)) as OfflineFile;
        }

        public void ReleaseFile(string directory)
        {
            foreach (var asset in SourceAssetsDatas)
            {
                string fileName = Path.GetFileName(asset.Key);
                using ToolFile file = new(Path.Combine(directory, fileName), true, false, true);
                file.ReplaceAllData(SourceAssetsDatas[asset.Key]);
                file.SaveFileData();
                file.Dispose();
                PathRelayers.Add(asset.Key, file.FilePath);
            }
        }

        public void Reconnect(object target, HashSet<object> passSet = null)
        {
            passSet ??= new();
            if (!passSet.Add(target)) return;
            if (target.As<ICanMakeOffline>(out var icm))
                icm.ReplacePath(PathRelayers);
            foreach (var child in DiagramType.GetOrCreateDiagramType(target.GetType()).members)
            {
                object cat = child.reflectedMember.GetValue(target);
                Reconnect(cat, passSet);
            }
        }

        /// <summary>
        /// Used After <see cref="ReleaseFile(string)"/>
        /// </summary>
        /// <param name="origin"></param>
        /// <returns></returns>
        public string GetNewPath(string origin)
        {
            return PathRelayers.TryGetValue(origin, out var path) ? path : null;
        }
    }

    public interface ICanMakeOffline
    {
        public string[] GetFilePaths();

        public void ReplacePath(Dictionary<string, string> sourceAssetsDatas);
    }

    #endregion

    #region Exception And Log

    /// <summary>
    /// Commonly used exception
    /// </summary>
    [Serializable]
    public class DiagramException : Exception
    {
        public DiagramException() { }
        public DiagramException(string message) : base(message) { }
        public DiagramException(string message, Exception inner) : base(message, inner) { }
        protected DiagramException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class BadImplemented : DiagramException
    {
        public BadImplemented() : base("Bad Implemented") { }
    }
    [Serializable]
    public class TodoImplemented : DiagramException
    {
        public TodoImplemented() : base("Not Implemented") { }
    }
    [Serializable]
    public class TestingImplemented : DiagramException
    {
        public TestingImplemented() : base("Testing Implemented") { }
    }
    [Serializable]
    public class NotSupport : DiagramException
    {
        public NotSupport() : base("Not Support") { }
    }


    /// <summary>
    /// Commonly used exception, support to file system,
    /// see <see cref="ToolFile"/> and <see cref="OfflineFile"/>
    /// </summary>
    [Serializable]
    public class FileException : DiagramException
    {
        public FileException() { }
        public FileException(string message) : base(message) { }
        public FileException(string message, Exception inner) : base(message, inner) { }
        protected FileException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class FileExist : DiagramException
    {
        public FileExist() : base("File Is Exist") { }
    }
    [Serializable]
    public class FileNotExist : DiagramException
    {
        public FileNotExist() : base("File Is Not Exist") { }
    }
    [Serializable]
    public class PathExist : DiagramException
    {
        public PathExist() : base("Path Is Exist") { }
    }
    [Serializable]
    public class PathNotExist : DiagramException
    {
        public PathNotExist() : base("Path Is Not Exist") { }
    }

    /// <summary>
    /// Commonly used exception, support to reflectional operator,
    /// see <see cref="ReflectionExtension"/>
    /// </summary>
    [Serializable]
    public class ReflectionException : DiagramException
    {
        public ReflectionException() { }
        public ReflectionException(string message) : base(message) { }
        public ReflectionException(string message, Exception inner) : base(message, inner) { }
        protected ReflectionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
    [Serializable]
    public class FieldException : ReflectionException
    {
        public FieldException() : base("Error : Field") { }
    }
    [Serializable]
    public class PropertyException : ReflectionException
    {
        public PropertyException() : base("Error : Property") { }
    }
    [Serializable]
    public class MethodException : ReflectionException
    {
        public MethodException() : base("Error : Method") { }
    }

    /// <summary>
    /// A debug system when every UnityEngine.Debug logging it will log the message
    /// <para><see cref="LogPath"/> : Debug log file's path</para>
    /// <para><see cref="LogMethodEnabled"/> : Is DebugExtension enable to record some message from <see cref="Log"/> or <see cref="LogMessage(string)"/></para>
    /// </summary>
    public static class DebugExtension
    {
        public static string LogPath = Path.Combine(Application.persistentDataPath, "Debug.dat");

        public static bool LogMethodEnabled = true;

        public static string[] FilterdName = new string[] { "GetStackTraceModelName" };

        static DebugExtension()
        {
            ToolFile.DeleteFile(LogPath);
            ToolFile file = new(LogPath, true, false, false);
            file.Dispose();
            Application.logMessageReceived -= LogHandler;
            Application.logMessageReceived += LogHandler;
        }

        private static void LogHandler(string logString, string stackTrace, LogType type)
        {
            try
            {
                using StreamWriter sws = new(LogPath, true, System.Text.Encoding.UTF8);
                sws.WriteLine("{");
                sws.WriteLine("[time]:" + DateTime.Now.ToString());
                sws.WriteLine("[type]:" + type.ToString());
                sws.WriteLine("[exception message]:" + logString);
                sws.WriteLine("[stack trace]:\n" + stackTrace + "}");
            }
            catch (Exception ex)
            {
                using StreamWriter sws = new(LogPath + ".error", true, System.Text.Encoding.UTF8);
                sws.WriteLine("{");
                sws.WriteLine("[time]:" + DateTime.Now.ToString());
                sws.WriteLine("[type]:" + type.ToString());
                sws.WriteLine("[exception message]:" + logString);
                sws.WriteLine("[stack trace]:\n" + stackTrace + "}");
                sws.WriteLine("[_catch_error]:" + ex.Message);
            }
        }

        public static void Log()
        {
            try
            {
                if (LogMethodEnabled)
                {
                    using StreamWriter sws = new(LogPath, true, System.Text.Encoding.UTF8);
                    var temp = GetStackTraceModelName();
                    sws.WriteLine(System.DateTime.Now.ToString() + ": ");
                    foreach (var line in temp)
                    {
                        sws.WriteLine("\t->" + line);
                    }
                }
            }
            catch { }
        }

        public static void LogMessage(string message)
        {
            try
            {
#if UNITY_EDITOR
                if (LogMethodEnabled)
                {
                    using StreamWriter sws = new(LogPath, true, System.Text.Encoding.UTF8);
                    var temp = GetStackTraceModelName();
                    sws.WriteLine(System.DateTime.Now.ToString() + ": " + message + ": ");
                    foreach (var line in temp)
                    {
                        sws.WriteLine("\t->" + line);
                    }
                }
#else
                if (LogMethodEnabled)
                {
                    using StreamWriter sws = new(LogPath, true, System.Text.Encoding.UTF8);
                    sws.WriteLine(System.DateTime.Now.ToString() + " : " + message);
                }
#endif
            }
            catch { }
        }

        public static string[] GetStackTraceModelName()
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.StackFrame[] sfs = st.GetFrames();
            List<string> result = new();
            for (int i = sfs.Length - 1; i >= 0; i--)
            {
                //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) continue;

                string _methodName = sfs[i].GetMethod().Name;
                if (FilterdName.Contains(_methodName)) continue;
                result.Add(_methodName + "%" + sfs[i].GetFileName() + ":" + sfs[i].GetFileLineNumber());
            }
            return result.ToArray();
        }
        public static string[] GetStackTraceModelName(int depth)
        {
            System.Diagnostics.StackTrace st = new System.Diagnostics.StackTrace(true);
            System.Diagnostics.StackFrame[] sfs = st.GetFrames();
            List<string> result = new();
            for (int i = sfs.Length - 1; i >= 0; i--)
            {
                //非用户代码,系统方法及后面的都是系统调用，不获取用户代码调用结束
                if (System.Diagnostics.StackFrame.OFFSET_UNKNOWN == sfs[i].GetILOffset()) continue;
                if (depth-- <= 0) break;

                string _methodName = sfs[i].GetMethod().Name;
                if (FilterdName.Contains(_methodName)) continue;
                result.Add(_methodName + "%" + sfs[i].GetFileName() + ":" + sfs[i].GetFileLineNumber());
            }
            return result.ToArray();
        }
    }

    #endregion

    #region Enum

    public class InsertResult
    {
        protected InsertResult() { }
        public virtual bool Value { get; }
        public virtual bool IsReplaceValue { get; }
        public static readonly Succeed IsSucceed = new();
        public static readonly Failed IsFailed = new();
        public static readonly Replace IsReplace = new(null);

        public object ReplaceObject = null;

        public static implicit operator bool(InsertResult from) => from.Value;

        public class Succeed : InsertResult
        {
            public override bool Value => true;
            public override bool IsReplaceValue => false;
        }
        public class Failed : InsertResult
        {
            public override bool Value => false;
            public override bool IsReplaceValue => false;
        }
        public class Replace : InsertResult
        {
            public override bool Value => true;
            public override bool IsReplaceValue => true;

            public Replace(object past)
            {
                this.ReplaceObject = past;
            }
        }

        public static bool operator ==(InsertResult l, InsertResult r) => l.Value == r.Value && l.IsReplaceValue == r.IsReplaceValue && l.ReplaceObject == r.ReplaceObject;
        public static bool operator !=(InsertResult l, InsertResult r) => !(l == r);

        public override bool Equals(object obj)
        {
            if (obj is InsertResult r) return this == r;
            else return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            if (this is Succeed)
            {
                return this == InsertResult.IsSucceed ? 1 : IsSucceed.GetHashCode();
            }
            else if (this is Failed)
            {
                return this == InsertResult.IsFailed ? 0 : IsFailed.GetHashCode();
            }
            else if (this is Replace)
            {
                return this == InsertResult.IsReplace ? -1 : IsReplace.GetHashCode();
            }
            throw new NotImplementedException();
        }
    }

    #endregion

    #region Exchange Type

    /// <summary>
    /// Implementations for runtime entities or data , The relevant interfaces are : 
    /// <para><see cref="IBaseMap"/> is the type used to convert to local data</para>
    /// <para><see cref="IBase{T}"/> This type is used for stronger constraints and clear goals</para>
    /// </summary>
    public interface IBase
    {
        void ToMap(out IBaseMap BM);
        bool FromMap(IBaseMap from);
    }

    /// <summary>
    /// A strongly constrained version of <see cref="IBase"/>
    /// </summary>
    /// <typeparam name="T">The target type of <see cref="IBaseMap{T}"/> you want to match</typeparam>
    public interface IBase<T> : IBase where T : class, IBaseMap, new()
    {
        void ToMap(out T BM);
        bool FromMap(T from);
    }


    /// <summary>
    /// Implementations for cache data , The relevant interfaces are : 
    /// <para><see cref="IBase"/> is the type used to convert to runtime entities or data</para>
    /// <para><see cref="IBaseMap{T}"/> This type is used for stronger constraints and clear goals</para>
    /// </summary>
    public interface IBaseMap
    {
        void ToObject(out IBase obj);
        bool FromObject(IBase from);
        string Serialize();
        bool Deserialize(string source);
    }

    /// <summary>
    /// A strongly constrained version of <see cref="IBaseMap"/>
    /// </summary>
    /// <typeparam name="T">The target type of <see cref="IBase{T}"/> you want to match</typeparam>
    public interface IBaseMap<T> : IBaseMap where T : class, IBase, new()
    {
        void ToObject(out T obj);
        bool FromObject(T from);
    }

    public interface IExchangeListener
    {
        object MakeExchange(object from);
    }

    public interface IInvariant<T> where T : class { }

    public static class ExchangeTypeExtension
    {
        public delegate object ExchangeAction(object from);
        public static Dictionary<Type, Dictionary<Type, ExchangeAction>> Translaters;
        public delegate object ExchangeActionEx(object from);
        public static Dictionary<Type, Dictionary<Type, ExchangeActionEx>> TranslaterExs;

        public static InsertResult Add<To, From>(ExchangeAction action)
        {
            if (Translaters.TryGetValue(typeof(To), out var Froms))
            {
                if (Froms.ContainsKey(typeof(From)))
                {
                    return InsertResult.IsFailed;
                }
            }
            else
            {
                Translaters.Add(typeof(To), new());
            }
            Translaters[typeof(From)].Add(typeof(From), action);
            return InsertResult.IsSucceed;
        }
        public static InsertResult Insert<To, From>(ExchangeAction action)
        {
            if (Translaters.TryGetValue(typeof(To), out var Froms))
            {
                if (Froms.TryGetValue(typeof(From), out var past))
                {
                    Translaters[typeof(From)][typeof(From)] = action;
                    return new InsertResult.Replace(past);
                }
            }
            else
            {
                Translaters.Add(typeof(To), new());
            }
            Translaters[typeof(From)].Add(typeof(From), action);
            return InsertResult.IsSucceed;
        }

        #region Function 0.5.0

        public static T As<T>(this object self) where T : class
        {
            if (self is not IInvariant<T>)
            {
                return self as T;
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogWarning($"you try to use an Invariant<{typeof(T).FullName}> by As");
                return null;
            }
#endif
        }

        public static bool As<T>(this object self, out T result) where T : class
        {
            if (self is not IInvariant<T> && self != null)
            {
                result = self as T;
                return result != null;
            }
            else
            {
                result = null;
                return false;
            }
        }

        public static bool Convertible<T>(this object self) where T : class
        {
            if (self != null)
            {
                if (self is IInvariant<T>) return false;
                return self is T;
            }
            else return false;
        }

        public static bool Is<T>(this object self, out T result) where T : class
        {
            result = null;
            if (self is not IInvariant<T> && self is T r)
            {
                result = r;
                return true;
            }
            else return false;
        }

        public static bool IsAssignableFromOrSubClass(this Type self, Type target)
        {
            return self.IsAssignableFrom(target) || self.IsSubclassOf(target);
        }

        public static bool IsAssignableFromOrSubClass(this object self, object target)
        {
            return IsAssignableFromOrSubClass(self.GetType(), target.GetType());
        }

        public static bool IsAssignableFromOrSubClass<T, P>()
        {
            return IsAssignableFromOrSubClass(typeof(T), typeof(P));
        }

        public enum ClassCorrelation
        {
            None, Base, Derived
        }

        public static ClassCorrelation DetectCorrelation(Type _Left, Type _Target)
        {
            if (_Left.IsAssignableFrom(_Target)) return ClassCorrelation.Base;
            else if (_Left.IsSubclassOf(_Target)) return ClassCorrelation.Derived;
            else return ClassCorrelation.None;
        }

        public static ClassCorrelation DetectCorrelation(object _Left, object _Target)
        {
            return DetectCorrelation(_Left.GetType(), _Target.GetType());
        }

        public static GameObject PrefabInstantiate(this GameObject self)
        {
            return GameObject.Instantiate(self);
        }

        public static T PrefabInstantiate<T>(this T self) where T : Component
        {
            return GameObject.Instantiate(self.gameObject).GetComponent<T>();
        }

        public static T PrefabInstantiate<T, _PrefabType>(this _PrefabType self) where T : Component where _PrefabType : Component
        {
            return GameObject.Instantiate(self.gameObject).GetComponent<T>();
        }

        public static T ObtainComponent<T>(this GameObject self, out T[] components) where T : class
        {
            components = self.GetComponents<T>();
            return components.Length > 0 ? components[0] : null;
        }

        public static T ObtainComponent<T>(this GameObject self) where T : class
        {
            var components = self.GetComponents<T>();
            return components.Length > 0 ? components[0] : null;
        }

        public static bool ObtainComponent<T>(this GameObject self, out T component) where T : class
        {
            var components = self.GetComponents<T>();
            component = components.Length > 0 ? components[0] : null;
            return component != null;
        }

        public static T Fetch<T>(this T self, out T me) where T : class
        {
            return me = self;
        }

        public static T Fetch<T, P>(this T self, out P me) where T : class where P : class
        {
            me = self as P;
            return self;
        }

        public static T Share<T>(this T self, out T shared)
        {
            return shared = self;
        }

        public static T SeekComponent<T>(this GameObject self) where T : class
        {
            foreach (var item in self.GetComponents<MonoBehaviour>())
            {
                if (item.As<T>(out var result))
                {
                    return result;
                }
            }
            return null;
        }

        public static T SeekComponent<T>(this Component self) where T : class
        {
            return SeekComponent<T>(self.gameObject);
        }

        #endregion
    }

    #endregion

    #region Base Utility

    public static class ContainerExtension
    {
        public static object[] ToObjectArray(this object self)
        {
            return new object[1] { self };
        }

        public static List<P> GetSubList<T, P>(this List<T> self) where P : class
        {
            List<P> result = new();
            result.AddRange(from T item in self
                            where item.Convertible<P>()
                            select item as P);
            return result;
        }

        public static P SelectCast<T, P>(this List<T> self) where P : class
        {
            foreach (var item in self)
                if (item.As<P>(out var result)) return result;
            return null;
        }

        public static List<T> GetSubList<T>(this IEnumerable<T> self, Predicate<T> predicate)
        {
            List<T> result = new();
            result.AddRange(from T item in self
                            where predicate(item)
                            select item);
            return result;
        }

        public static List<Value> GetSubListAboutValue<Key, Value>(this Dictionary<Key, Value> self)
        {
            List<Value> result = new();
            foreach (var item in self)
            {
                result.Add(item.Value);
            }
            return result;
        }

        public static List<Key> GetSubListAboutKey<Key, Value>(this Dictionary<Key, Value> self)
        {
            List<Key> result = new();
            foreach (var item in self)
            {
                result.Add(item.Key);
            }
            return result;
        }

        public static List<T> GetSubListAboutValue<T, Key, Value>(this Dictionary<Key, Value> self) where Value : T
        {
            List<T> result = new();
            foreach (var item in self)
            {
                result.Add(item.Value);
            }
            return result;
        }

        public static List<T> GetSubListAboutKey<T, Key, Value>(this Dictionary<Key, Value> self) where Key : T
        {
            List<T> result = new();
            foreach (var item in self)
            {
                result.Add(item.Key);
            }
            return result;
        }

        public static List<Result> GetSubList<Result, T>(this IEnumerable<T> self, Func<T, bool> predicate, Func<T, Result> transformFunc)
        {
            List<Result> result = new();
            foreach (var item in self)
            {
                if (predicate(item)) result.Add(transformFunc(item));
            }
            return result;
        }

        public static List<T> UnPackage<T>(this List<List<T>> self)
        {
            List<T> result = new();
            foreach (var item in self)
            {
                result.AddRange(item);
            }
            return result;
        }

        public static T[] SubArray<T>(this T[] self, int start, int end)
        {
            T[] result = new T[end - start];
            for (int i = start; i < end; i++)
            {
                result[i] = self[i];
            }
            return result;
        }

        public static T[] SubArray<T>(this T[] self, T[] buffer, int start, int end)
        {
            if (buffer == null || buffer.Length < end - start) throw new DiagramException("Buffer is null or too small");
            for (int i = start; i < end; i++)
            {
                buffer[i] = self[i];
            }
            return buffer;
        }

        public static T[] SafeSubArray<T>(this T[] self, T[] buffer, int start, int end)
        {
            if (buffer == null) return SubArray(self, start, end);
            for (int i = start, e = Mathf.Min(start + buffer.Length, end); i < e; i++)
            {
                buffer[i] = self[i];
            }
            return buffer;
        }

        public static List<Value> GetSubListAboutSortValue<Key, Value>(this Dictionary<Key, Value> self) where Key : IComparable<Key>
        {
            List<(Key, Value)> temp = new();
            foreach (var item in self)
            {
                temp.Add((item.Key, item.Value));
            }
            temp.Sort((T, P) => T.Item1.CompareTo(P.Item1));
            List<Value> result = new();
            for (int i = 0, e = temp.Count; i < e; i++)
            {
                result.Add(temp[i].Item2);
            }
            return result;
        }

        public static T[] Expand<T>(this T[] self, params T[] args)
        {
            T[] result = new T[args.Length + self.Length];
            for (int i = 0, e = self.Length; i < e; i++)
            {
                result[i] = self[i];
            }
            for (int i = 0, e = args.Length, p = self.Length; i < e; i++)
            {
                result[i + p] = args[i];
            }
            return result;
        }

        public static List<Result> GetSubList<Result, KeyArgs, T>(this IEnumerable<T> self, Func<T, (bool, KeyArgs)> predicate, Func<T, KeyArgs, Result> transformFunc)
        {
            List<Result> result = new();
            foreach (var item in self)
            {
                if (predicate(item).Share(out var keyArgs).Item1) result.Add(transformFunc(item, keyArgs.Item2));
            }
            return result;
        }

        public static List<Result> Contravariance<Origin, Result>(this IEnumerable<Origin> self)
            where Result : class, Origin
        {
            List<Result> result = new();
            foreach (var item in self)
            {
                if (item.As<Result>(out Result cat))
                {
                    result.Add(cat);
                }
            }
            return result;
        }

        public static List<Result> Contravariance<Origin, Result>(this IEnumerable<Origin> self, Func<Origin, Result> transformer)
        {
            List<Result> result = new();
            foreach (var item in self)
            {
                result.Add(transformer(item));
            }
            return result;
        }

        public static List<Result> Covariance<Origin, Result>(this IEnumerable<Origin> self)
            where Origin : class, Result
        {
            List<Result> result = new();
            foreach (var item in self)
            {
                result.Add(item);
            }
            return result;
        }

        public static List<T> RemoveNull<T>(this List<T> self)
        {
            self.RemoveAll(T => T == null);
            return self;
        }

        public static List<T> RemoveNullAsNew<T>(this List<T> self)
        {
            List<T> result = new();
            foreach (var item in self)
            {
                if (item != null)
                    result.Add(item);
            }
            return result;
        }

        public static void CheckLength<T>(this T[] self, out T arg0)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 1) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 1");
            arg0 = self[0];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 2) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 2");
            arg0 = self[0];
            arg1 = self[1];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1, out T arg2)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 3) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 3");
            arg0 = self[0];
            arg1 = self[1];
            arg2 = self[2];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1, out T arg2, out T arg3)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 4) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 4");
            arg0 = self[0];
            arg1 = self[1];
            arg2 = self[2];
            arg3 = self[3];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1, out T arg2, out T arg3, out T arg4)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 5) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 5");
            arg0 = self[0];
            arg1 = self[1];
            arg2 = self[2];
            arg3 = self[3];
            arg4 = self[4];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1, out T arg2, out T arg3, out T arg4, out T arg5)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 6) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 6");
            arg0 = self[0];
            arg1 = self[1];
            arg2 = self[2];
            arg3 = self[3];
            arg4 = self[4];
            arg5 = self[5];
        }

        public static void CheckLength<T>(this T[] self, out T arg0, out T arg1, out T arg2, out T arg3, out T arg4, out T arg5, out T arg6)
        {
            if (self == null) throw new ArgumentNullException();
            if (self.Length != 7) throw new ArgumentException("Passed argument 'self' is invalid size. Expected size is 7");
            arg0 = self[0];
            arg1 = self[1];
            arg2 = self[2];
            arg3 = self[3];
            arg4 = self[4];
            arg5 = self[5];
            arg6 = self[6];
        }

        public static IEnumerator GetEmpty()
        {
            return new EmptyEnumerator();
        }

        public static IEnumerator<T> GetEmpty<T>()
        {
            return new EmptyEnumerator<T>();
        }

        public static float Totally(this IEnumerable<float> self)
        {
            float result = 0;
            foreach (var value in self)
            {
                result += value;
            }
            return result;
        }

        public static string LinkAndInsert(this IEnumerable<object> self, string key)
        {
            string result = "";
            int first = 0, end = self.Count();
            if (self == null || end == 0) return result;
            foreach (var item in self)
            {
                if (first >= end) break;
                first++;
                result += item.ToString() + key;
            }
            result += self.Last();
            return result;
        }

        public static string LinkAndInsert(this IEnumerable<object> self, char key)
        {
            string result = "";
            int first = 0, end = self.Count();
            if (self == null || end == 0) return result;
            foreach (var item in self)
            {
                if (first >= end) break;
                first++;
                result += item.ToString() + key;
            }
            result += self.Last();
            return result;
        }
    }

    class EmptyEnumerator : IEnumerator
    {
        public object Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }
    }

    class EmptyEnumerator<T> : IEnumerator<T>
    {
        public T Current
        {
            get
            {
                throw new InvalidOperationException();
            }
        }

        object IEnumerator.Current => Current;

        public void Dispose()
        {

        }

        public bool MoveNext()
        {
            return false;
        }

        public void Reset()
        {
        }
    }

    #endregion
}

namespace Diagram
{
    [Serializable]
    public class ArchitectureException : DiagramException
    {
        public ArchitectureException(string message) : base(message) { }
        public ArchitectureException(string message, Exception inner) : base(message, inner) { }
        [Serializable]
        public class Exist : ArchitectureException
        {
            public Exist() : base("Target Class/Typename Is Exist") { }
        }
        [Serializable]
        public class NotExist : ArchitectureException
        {
            public NotExist() : base("Target Class/Typename Is Not Exist") { }
        }
        [Serializable]
        public class WrongType : ArchitectureException
        {
            public WrongType() : base("Not the desired type") { }
        }
    }

    public interface ICommand
    {
        void Invoke();
    }

    public class BaseWrapper
    {
        private object arch_ontology = null;
        internal object Ontology => arch_ontology;
        public bool IsRegisterCallback { get; protected set; } = false;
        public BaseWrapper(object arch_ontology)
        {
            this.arch_ontology = arch_ontology;
        }

        public Architecture Arch { get; protected set; } = null;

        public override bool Equals(object obj)
        {
            if (obj is BaseWrapper cat)
            {
                return cat.arch_ontology == this.arch_ontology;
            }
            else return base.Equals(obj);
        }
        public override int GetHashCode()
        {
            return arch_ontology.GetHashCode();
        }
        public override string ToString()
        {
            return arch_ontology.ToString();
        }

        public class Architecture : BaseWrapper
        {
            internal Dictionary<Type, BaseWrapper> Components = new();
            internal Dictionary<BaseWrapper, Dictionary<Type, bool>> DependenciesLater = new();
            public Architecture(object arch) : base(arch)
            {
                arch.TryRunMethodByName("OnArchitectureInit", out object _, DefaultBindingFlags);
                this.Arch = this;
            }
            ~Architecture()
            {
                this.Ontology.TryRunMethodByName("OnArchitectureDestroy", out object _, DefaultBindingFlags);
            }
            public override int GetHashCode()
            {
                return this.Ontology.GetType().GetHashCode();
            }
            public override bool Equals(object obj)
            {
                return this.Ontology.GetType() == obj.GetType();
            }

            #region Register
            /// <summary>
            /// CallBack Timing: dependencies is all registered
            /// <list type="bullet">Core Method: <b>void OnDependencyCompleting()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Register<T>(BaseWrapper target, params Type[] dependences) where T : class
            {
                return Register(typeof(T), target, dependences.ToList());
            }
            /// <summary>
            /// CallBack Timing: dependencies is all registered
            /// <list type="bullet">Core Method: <b>void OnDependencyCompleting()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Register<T>(BaseWrapper target, List<Type> dependences) where T : class
            {
                return Register(typeof(T), target, dependences);
            }
            /// <summary>
            /// CallBack Timing: dependencies is all registered
            /// <list type="bullet">Core Method: <b>void OnDependencyCompleting()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Register(Type type, BaseWrapper target, params Type[] dependences)
            {
                return Register(type, target, dependences.ToList());
            }
            /// <summary>
            /// CallBack Timing: dependencies is all registered
            /// <list type="bullet">Core Method: <b>void OnDependencyCompleting()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Register(Type type, BaseWrapper target, List<Type> dependences)
            {
                if (Components.ContainsKey(type)) throw new ArchitectureException.Exist();
                target.Arch = this;
                Components.Add(type, target);
                DependenciesLater.Add(target, new Dictionary<Type, bool>().Share(out var dic));
                foreach (var item in dependences)
                {
                    dic.Add(item, Components.ContainsKey(item));
                }
                ToolDetectRegisteredsDependence((Dictionary<Type, bool> dic) =>
                {
                    if (dic.ContainsKey(type))
                        dic[type] = true;
                });
                return this;
            }
            /// <summary>
            /// CallBack Timing: now
            /// <para>Side Effect: DependenciesLater.Remove(target)</para>
            /// <list type="bullet">Core Method: <b>void OnDependencyCompleting()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            protected Architecture RegisterWithCallback(BaseWrapper target)
            {
                DependenciesLater.Remove(target);
                target.TryRunMethodByName("OnDependencyCompleting", out object _, AllBindingFlags);
                target.IsRegisterCallback = true;
                target.TryRunMethodByName("OnInit", out object _, AllBindingFlags);
                return this;
            }
            /// <summary>
            /// Detect Stats Change Just Now
            /// </summary>
            private void ToolDetectRegisteredsDependence(Action<Dictionary<Type, bool>> action)
            {
                List<BaseWrapper> result = new();
                //found targets
                foreach (var dependence in DependenciesLater)
                {
                    action(dependence.Value);
                    bool stats = true;
                    foreach (var types in dependence.Value)
                    {
                        stats = stats & types.Value;
                        if (!stats) break;
                    }
                    if (stats) result.Add(dependence.Key);
                }
                //call back
                foreach (var target in result)
                {
                    RegisterWithCallback(target);
                }
                if (result.Count != 0) ToolDetectRegisteredsDependence();
            }
            /// <summary>
            /// Detect Stats Change Just Now
            /// </summary>
            private void ToolDetectRegisteredsDependence()
            {
                List<BaseWrapper> result = new();
                //found targets
                foreach (var dependence in DependenciesLater)
                {
                    bool stats = true;
                    foreach (var types in dependence.Value)
                    {
                        stats = stats & types.Value;
                        if (!stats) break;
                    }
                    if (stats) result.Add(dependence.Key);
                }
                //call back
                foreach (var target in result)
                {
                    RegisterWithCallback(target);
                }
                if (result.Count != 0) ToolDetectRegisteredsDependence();
            }
            #endregion

            #region Unregister
            /// <summary>
            /// CallBack Timing: now
            /// <list type="bullet">Core Method: <b>void OnDependencyReleasing()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Unregister(params Type[] types)
            {
                foreach (var type in types)
                {
                    Unregister(type);
                }
                return this;
            }
            /// <summary>
            /// CallBack Timing: now
            /// <list type="bullet">Core Method: <b>void OnDependencyReleasing()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Unregister<T>() where T : class
            {
                return Unregister(typeof(T));
            }
            /// <summary>
            /// CallBack Timing: now
            /// <list type="bullet">Core Method: <b>void OnDependencyReleasing()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            public Architecture Unregister(Type type)
            {
                if (!Components.TryGetValue(type, out var wrapper)) throw new ArchitectureException.NotExist();
                if (!DependenciesLater.ContainsKey(Components[type]))
                {
                    UnregisterWithCallback(wrapper);
                }
                wrapper.Arch = null;
                Components.Remove(type);
                DependenciesLater.Remove(wrapper);
                foreach (var dependence in DependenciesLater)
                {
                    if (dependence.Value.ContainsKey(type))
                        dependence.Value[type] = false;
                }
                return this;
            }
            /// <summary>
            /// CallBack Timing: now
            /// <list type="bullet">Core Method: <b>void OnDependencyReleasing()</b></list>
            /// <list type="bullet">Conventional Method: <b>void OnInit()</b></list>
            /// </summary>
            protected Architecture UnregisterWithCallback(BaseWrapper target)
            {
                DependenciesLater.Remove(target);
                target.TryRunMethodByName("OnDependencyReleasing", out object _, DefaultBindingFlags);
                target.IsRegisterCallback = false;
                return this;
            }
            #endregion

            #region Obtain

            public System GetSystem<T>() where T : class => GetComponent<System>(typeof(T));
            public System GetSystem(Type type) => GetComponent<System>(type);
            public Model GetModel<T>() where T : class => GetComponent<Model>(typeof(T));
            public Model GetModel(Type type) => GetComponent<Model>(type);
            public Controller GetController<T>() where T : class => GetComponent<Controller>(typeof(T));
            public Controller GetController(Type type) => GetComponent<Controller>(type);
            public bool Contains(Type type) => Components.ContainsKey(type);

            private T GetComponent<T>(Type type) where T : BaseWrapper
            {
                return Components.TryGetValue(type, out var cat) ? cat as T : null;
            }

            #endregion

            #region Diff

            public Architecture Diffusing(Type type)
            {
                if (Components.TryGetValue(type, out var temp))
                {
                    if (temp is ICommand command)
                    {
                        command.Invoke();
                    }
                    foreach (var component in Components)
                    {
                        component.Value.ListenToCommand(type, this);
                    }
                }
                else throw new ArchitectureException.WrongType();
                return this;
            }
            public Architecture SendCommand(Type type)
            {
                if (Components.TryGetValue(type, out var temp))
                {
                    if (temp is ICommand command)
                    {
                        command.Invoke();
                    }
                }
                else throw new ArchitectureException.WrongType();
                return this;
            }

            #endregion
        }
        public class System : BaseWrapper
        {
            public System(object system) : base(system)
            {

            }
        }
        public class Model : BaseWrapper
        {
            public Model(object model) : base(model)
            {

            }

            public void Init()
            {
                if (!this.Ontology.TryRunMethodByName("Init", out object _, DefaultBindingFlags))
                    this.Ontology.TryRunMethodByName("OnInit", out object _, DefaultBindingFlags);
            }
        }
        /// <summary>
        /// use "On"+command type's name to call controller's defined method, like <b>void OnCommand()</b> is the which type: "Command" want to call
        /// </summary>
        public class Controller : BaseWrapper
        {
            private Dictionary<Type, Action> listenCommands;

            public Controller(object controller, Dictionary<Type, Action> commands) : base(controller)
            {
                this.listenCommands = commands;
            }

            public override void ListenToCommand(Type type, Architecture arch)
            {
                if (this.listenCommands.TryGetValue(type, out var action))
                {
                    action.Invoke();
                }
                this.arch_ontology.TryRunMethodByName("On" + type.Name, out object _, DefaultBindingFlags);
            }
        }

        virtual public void ListenToCommand(Type type, Architecture arch) { }
        public void ListenToCommand<_Command>(Architecture arch)
        {
            this.ListenToCommand(typeof(_Command), arch);
        }
        public T To<T>() where T : class
        {
            return this.Ontology as T;
        }
    }

    public class ObserveEachOther
    {

    }

    [UnityEngine.Scripting.Preserve]
    public static class ArchitectureDiagram
    {
        private readonly static System.Collections.Generic.Dictionary<Type, BaseWrapper.Architecture> AllArchs = new();
        public static void OnInit()
        {
            AllArchs.Clear();
        }

        /// <summary>
        /// <list type="bullet">Core Method: <b>void OnArchitectureInit()</b> will be call when Register</list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arch"></param>
        public static BaseWrapper.Architecture RegisterArchitecture<T>(T arch) where T : class
        {
            return RegisterArchitecture(typeof(T), arch);
        }
        /// <summary>
        /// <list type="bullet">Core Method: <b>void OnArchitectureInit()</b> will be call when Register</list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arch"></param>
        public static BaseWrapper.Architecture RegisterArchitecture(Type type, object arch)
        {
            AllArchs.Add(type, new BaseWrapper.Architecture(arch).Share(out var result));
            return result;
        }

        /// <summary>
        /// <list type="bullet">Core Method: <b>void OnArchitectureDestroy()</b> will be call when Unregister</list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arch"></param>
        public static void UnregisterArchitecture<T>() where T : class
        {
            UnregisterArchitecture(typeof(T));
        }

        /// <summary>
        /// <list type="bullet">Core Method: <b>void OnArchitectureDestroy()</b> will be call when Unregister</list>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="arch"></param>
        public static void UnregisterArchitecture(Type type)
        {
            if (!AllArchs.Remove(type))
                throw new ArchitectureException.NotExist();
        }

        /// <summary>
        /// Get Target Architecture with <see cref="BaseWrapper.Architecture"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static BaseWrapper.Architecture Architecture<T>(this object self) where T : class
        {
            return Architecture(self, typeof(T));
        }
        /// <summary>
        /// Get Target Architecture with <see cref="BaseWrapper.Architecture"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static BaseWrapper.Architecture Architecture(this object self, Type type)
        {
            if (AllArchs.ContainsKey(type)) return AllArchs[type];
            else throw new ArchitectureException.NotExist();
        }
        /// <summary>
        /// Get Target Architecture with <see cref="BaseWrapper.Architecture"/>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self"></param>
        /// <returns></returns>
        public static BaseWrapper.Architecture Architecture(Type type)
        {
            if (AllArchs.ContainsKey(type)) return AllArchs[type];
            else throw new ArchitectureException.NotExist();
        }

        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.System RegisterSystemOn<T, Arch>(this T self, params Type[] dependences) where T : class where Arch : class
        {
            return RegisterSystemOn(self, typeof(T), typeof(Arch), dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.System RegisterSystemOn<T>(this T self, Type archType, params Type[] dependences) where T : class
        {
            return RegisterSystemOn(self, typeof(T), archType, dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.System RegisterSystemOn(this object self, Type type, Type archType, params Type[] dependences)
        {
            Architecture(archType).Register(type, new BaseWrapper.System(self).Share(out var result), dependences);
            return result;
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Architecture RegisterSystem(this BaseWrapper.Architecture self, Type type, object target, params Type[] dependences)
        {
            return self.Register(type, new BaseWrapper.System(self), dependences);
        }

        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Model RegisterModelOn<T, Arch>(this T self, params Type[] dependences) where T : class where Arch : class
        {
            return RegisterModelOn(self, typeof(T), typeof(Arch), dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Model RegisterModelOn<T>(this T self, Type archType, params Type[] dependences) where T : class
        {
            return RegisterModelOn(self, typeof(T), archType, dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Model RegisterModelOn(this object self, Type type, Type archType, params Type[] dependences)
        {
            Architecture(archType).Register(type, new BaseWrapper.Model(self).Share(out var result), dependences);
            return result;
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Architecture RegisterModel(this BaseWrapper.Architecture self, Type type, object target, params Type[] dependences)
        {
            return self.Register(type, new BaseWrapper.Model(self), dependences);
        }

        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Controller RegisterControllerOn<T, Arch>(this T self, Dictionary<Type, Action> commandsCallback, params Type[] dependences) where T : class where Arch : class
        {
            return RegisterControllerOn(self, typeof(T), typeof(Arch), commandsCallback, dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Controller RegisterControllerOn<T>(this T self, Type archType, Dictionary<Type, Action> commandsCallback, params Type[] dependences) where T : class
        {
            return RegisterControllerOn(self, typeof(T), archType, commandsCallback, dependences);
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Controller RegisterControllerOn(this object self, Type type, Type archType, Dictionary<Type, Action> commandsCallback, params Type[] dependences)
        {
            Architecture(archType).Register(type, new BaseWrapper.Controller(self, commandsCallback ?? new()).Share(out var result), dependences);
            return result;
        }
        /// <summary>
        /// This type triggers a registration callback only when all registrations are dependent
        /// </summary>
        public static BaseWrapper.Architecture RegisterController(this BaseWrapper.Architecture self, Type type, object target, Dictionary<Type, Action> commandsCallback, params Type[] dependences)
        {
            return self.Register(type, new BaseWrapper.Controller(self, commandsCallback ?? new()), dependences);
        }

        public static bool Contains(this Type arch,Type type)
        {
            return Architecture(arch).Contains(type);
        }
    }
}

namespace Diagram.Collections
{
    [System.Serializable]
    public class SerializableDictionary<TKey, TVal> : Dictionary<TKey, TVal>, ISerializationCallbackReceiver
    {
        [Serializable]
        class Entry
        {
            public Entry() { }

            public Entry(TKey key, TVal value)
            {
                Key = key;
                Value = value;
            }

            public TKey Key;
            public TVal Value;
        }

        [SerializeField, JsonIgnore]
        private List<Entry> Data;

        public UnityEvent<TKey> OnAdd = new(), OnTryAdd = new(), OnRemove = new();
        public UnityEvent<TKey, bool> OnReplace = new();

        public virtual void OnBeforeSerialize()
        {
            Data = new();
            foreach (KeyValuePair<TKey, TVal> pair in this)
            {
                try
                {
                    Data.Add(new Entry(pair.Key, pair.Value));
                }
                catch { }
            }
        }

        // load dictionary from lists
        public virtual void OnAfterDeserialize()
        {
            if (Data == null) return;
            base.Clear();
            for (int i = 0; i < Data.Count; i++)
            {
                if (Data[i] != null)
                {
                    if (!base.TryAdd(Data[i].Key, Data[i].Value))
                    {
                        Type typeTkey = typeof(TKey);
                        if (typeTkey == typeof(string)) (this as Dictionary<string, TVal>).Add("New Key", default);
                        else if (typeTkey.IsSubclassOf(typeof(object))) base.Add(default, default);
                        else if (ReflectionExtension.IsPrimitive(typeTkey)) base.Add(default, default);
                    }
                }
            }

            Data = null;
        }

        public int RemoveNullValues()
        {
            var nullKeys = this.Where(pair => pair.Value == null)
                .Select(pair => pair.Key)
                .ToList();
            foreach (var nullKey in nullKeys)
                base.Remove(nullKey);
            return nullKeys.Count;
        }

        public new void Add(TKey key, TVal value)
        {
            base.Add(key, value);
            OnAdd.Invoke(key);
        }

        public new bool TryAdd(TKey key, TVal value)
        {
            bool result = base.TryAdd(key, value);
            OnTryAdd.Invoke(key);
            return result;
        }

        public new bool Remove(TKey key)
        {
            bool result = base.Remove(key);
            OnRemove.Invoke(key);
            return result;
        }

        public new TVal this[TKey key]
        {
            get
            {
                return base[key];
            }
            set
            {
                var result = base.ContainsKey(key);
                base[key] = value;
                this.OnReplace.Invoke(key, result);
            }
        }
    }

    [System.Serializable]
    public class SegmentTree
    {
        public class Node
        {
            public int left;
            public int right;
            public Node leftchild;
            public Node rightchild;
            public int Sum;
            public int Min;
            public int Max;
        }

        private Node nodeTree = new Node();
        private int[] nums;
         
        /// <summary>
        /// Buildup by new datas
        /// </summary>
        /// <returns>Tree's root</returns>
        public Node Build(int[] nums)
        {
            this.nums = nums;
            return Build(nodeTree, 0, nums.Length - 1);
        } 
        /// <summary>
        /// Buildup by nums's data to a new node
        /// </summary>
        /// <param name="left">Using nums's left boundary</param>
        /// <param name="right">Using nums's right boundary</param>
        /// <returns>node or new one node</returns>
        public Node Build(Node node, int left, int right)
        {
            //Indicating that it has reached the root, the max, sum, and min values of the current node
            //(counting the values of the previous node interval during backtracking)
            if (left == right)
            {
                return new Node
                {
                    left = left,
                    right = right,
                    Max = nums[left],
                    Min = nums[left],
                    Sum = nums[left]
                };
            }
            //value init
            if (node == null) node = new Node(); 
            node.left = left;
            node.right = right;
            node.leftchild = Build(node.leftchild, left, (left + right) / 2);
            node.rightchild = Build(node.rightchild, (left + right) / 2 + 1, right);
            //count value
            node.Min = Math.Min(node.leftchild.Min, node.rightchild.Min);
            node.Max = Math.Max(node.leftchild.Max, node.rightchild.Max);
            node.Sum = node.leftchild.Sum + node.rightchild.Sum;

            return node;
        }

        public int Query(int left, int right)
        { 
            return Query(nodeTree, left, right); 
        }
        public int Query(Node node, int left, int right)
        {
            int sum = 0;
            return Query(node, left, right, ref sum);
        }
        public int Query(Node node, int left, int right,ref int sum)
        { 
            if (left <= node.left && right >= node.right)
            {
                sum += node.Sum;
                return sum;
            }
            else
            {
                if (node.left > right || node.right < left) return sum;
                var middle = (node.left + node.right) / 2;

                if (left <= middle)
                {
                    Query(node.leftchild, left, right, ref sum);
                }
                if (right >= middle)
                {
                    Query(node.rightchild, left, right, ref sum);
                }
                return sum;
            }
        }
         
        public void Update(int index, int key)
        {
            Update(nodeTree, index, key);
        }
        public void Update(Node node, int index, int key)
        {
            if (node == null) return;
            var middle = (node.left + node.right) / 2;
            if (index >= node.left && index <= middle)
                Update(node.leftchild, index, key);
            if (index <= node.right && index >= middle + 1)
                Update(node.rightchild, index, key);
            if (index >= node.left && index <= node.right)
            {
                //说明找到了节点
                if (node.left == node.right)
                {
                    nums[index] = key;

                    node.Sum = node.Max = node.Min = key;
                }
                else
                {
                    //回溯时统计左右子树的值(min，max，sum)
                    node.Min = Math.Min(node.leftchild.Min, node.rightchild.Min);
                    node.Max = Math.Max(node.leftchild.Max, node.rightchild.Max);
                    node.Sum = node.leftchild.Sum + node.rightchild.Sum;
                }
            }
        } 
    }

    [System.Serializable]
    public class SegmentTreeWithData<T> where T : class,new()
    {
        public class Node
        {
            public int left;
            public int right;
            public Node leftchild;
            public Node rightchild;
            public int Sum;
            public int Min;
            public int Max;
            public T data = null;

            public void ReadAllData(ref List<T> datas)
            {
                if (this.data != null) datas.Add(data);
                leftchild?.ReadAllData(ref datas);
                rightchild?.ReadAllData(ref datas);
            }
        }

        private Node nodeTree = new Node();
        private int[] nums;

        /// <summary>
        /// Buildup by new datas
        /// </summary>
        /// <returns>Tree's root</returns>
        public Node Build(int[] nums)
        {
            this.nums = nums;
            return Build(nodeTree, 0, nums.Length - 1);
        }
        /// <summary>
        /// Buildup by nums's data to a new node
        /// </summary>
        /// <param name="left">Using nums's left boundary</param>
        /// <param name="right">Using nums's right boundary</param>
        /// <returns>node or new one node</returns>
        public Node Build(Node node, int left, int right)
        {
            //Indicating that it has reached the root, the max, sum, and min values of the current node
            //(counting the values of the previous node interval during backtracking)
            if (left == right)
            {
                return new Node
                {
                    left = left,
                    right = right,
                    Max = nums[left],
                    Min = nums[left],
                    Sum = nums[left]
                };
            }
            //value init
            if (node == null) node = new Node();
            node.left = left;
            node.right = right;
            node.leftchild = Build(node.leftchild, left, (left + right) / 2);
            node.rightchild = Build(node.rightchild, (left + right) / 2 + 1, right);
            //count value
            node.Min = Math.Min(node.leftchild.Min, node.rightchild.Min);
            node.Max = Math.Max(node.leftchild.Max, node.rightchild.Max);
            node.Sum = node.leftchild.Sum + node.rightchild.Sum;

            return node;
        }

        public int Query(int left, int right, ref List<Node> datas)
        {
            return Query(nodeTree, left, right,ref datas);
        }
        public int Query(Node node, int left, int right , ref List<Node> datas)
        {
            int sum = 0;
            return Query(node, left, right, ref sum,ref datas);
        }
        public int Query(Node node, int left, int right, ref int sum, ref List<Node> datas)
        {
            if (left <= node.left && right >= node.right)
            {
                sum += node.Sum;
                datas.Add(node);
                return sum;
            }
            else
            {
                if (node.left > right || node.right < left) return sum;
                var middle = (node.left + node.right) / 2;

                if (left <= middle)
                {
                    Query(node.leftchild, left, right, ref sum,ref datas);
                }
                if (right >= middle)
                {
                    Query(node.rightchild, left, right, ref sum,ref datas);
                }
                return sum;
            }
        }

        public Node First(int index)
        {
            return First(index, out Node target) ? target : null;
        }
        public bool First(int index,out Node target)
        {
            return First(index,out target);
        }
        public bool First(Node node, int index, out Node target)
        {
            if (index == node.left && index == node.right)
            {
                target = node;
                return true;
            }
            else
            {
                target = null;
                if (node.left > index || node.right < index) return false;
                var middle = (node.left + node.right) / 2;

                if (index <= middle)
                {
                    if (First(node.leftchild, index, out target)) return true;
                }
                if (index >= middle)
                {
                    if (First(node.rightchild, index, out target)) return true;
                }
                return false;
            }
        }

        public void Update(int index, int key)
        {
            Update(nodeTree, index, key);
        }
        public void Update(Node node, int index, int key)
        {
            if (node == null) return;
            var middle = (node.left + node.right) / 2;
            if (index >= node.left && index <= middle)
                Update(node.leftchild, index, key);
            if (index <= node.right && index >= middle + 1)
                Update(node.rightchild, index, key);
            if (index >= node.left && index <= node.right)
            {
                //说明找到了节点
                if (node.left == node.right)
                {
                    nums[index] = key;

                    node.Sum = node.Max = node.Min = key;
                }
                else
                {
                    //回溯时统计左右子树的值(min，max，sum)
                    node.Min = Math.Min(node.leftchild.Min, node.rightchild.Min);
                    node.Max = Math.Max(node.leftchild.Max, node.rightchild.Max);
                    node.Sum = node.leftchild.Sum + node.rightchild.Sum;
                }
            }
        }
    }
}

namespace Diagram.Message
{
    public static class CacheAssets
    {
        public static string Messages = ""; 
    }

    public class GetCache
    {
        public string message;
        public GetCache(int index)
        {
            var strs = CacheAssets.Messages.Split(';');
            if (strs.Length > index) message = strs[index];
            else message = "";
        }
    }

    public class SetCache
    {
        public string message;
        public SetCache(string message)
        {
            CacheAssets.Messages = message;
            this.message = message;
        }
    }

    public class AddCache
    {
        public string message;
        public AddCache(string message)
        {
            CacheAssets.Messages += (CacheAssets.Messages.Length == 0 ? "" : ";") + message;
        }
    }
}
