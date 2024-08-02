using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace Diagram.Serialization
{
    /// <summary>
    /// A virtual base class interface for runtime objects
    /// </summary>
    public interface IVirtualBase : IBase
    { 
    }

    /// <summary>
    /// A virtual base class interface for runtime objects which is the config
    /// </summary>
    public interface IGlobalVirtualConfig : IVirtualBase
    {
    }
    /// <summary>
    /// A virtual base class interface for disk-data object
    /// </summary>
    public interface IVirtualData : IBaseMap,ISerializable
    {
    }

    public abstract class VirtualData : IVirtualData
    {
        bool IBaseMap.FromObject(IBase from)
        {
            if (from.As<IGlobalVirtualConfig>(out var gvb))
            {
                FromGlobalConfig(gvb);
            }
            return from.As<IVirtualBase>(out var vb) && FromBase(vb);
        }
        public virtual bool FromBase([_In_]IVirtualBase vb) => false;
        public virtual void FromGlobalConfig([_In_] IVirtualBase gvb) { }

        [_Init_]
        public VirtualData(SerializationInfo info, StreamingContext context)
        {
            this.GUID = info.GetInt64("GUID");
        }

        [_Note_("Serialize")]
        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("GUID", this.GUID);
        }

        public void ToObject([_Out_] out IBase obj)
        {
            if (ToBase(out var vb) == false)
                obj = null;
            else 
                obj = vb;
        }
        public virtual bool ToBase([_Out_] out IVirtualBase vb)
        {
            vb = null;
            return false;
        }

        public virtual string GetVirtualToken()
        {
            return this.GetType().Name + m_GUID.ToString();
        }
        private long m_GUID = 0;
        private static HashSet<long> m_GUIDSet;
        public static void VirtualDataEnvBuildup()
        {
            m_GUIDSet = new();
        }
        [SerializeField]
        public long GUID
        {
            get => m_GUID;
            private set
            {
                while (m_GUIDSet.Contains(value))
                    value++;
                m_GUID = value;
                m_GUIDSet.Add(value);
            }
        }
        public VirtualData()
        {
            this.GUID = this.GetMemoryAddress().ToInt64();
        }
    }
}

namespace Diagram
{
    public partial class ToolFile
    {

    }
}