using System;
using System.Collections.Generic;
using System.Linq;
using Diagram.Arithmetic;
using UnityEngine;

namespace Diagram
{
    [Serializable]
    public class BuffManager : BaseWrapper.BuffFrames
    {
        [_Init_]
        public BuffManager(MonoBehaviour targetBehaviour) : base(null)
        {
            target = targetBehaviour;
        }

        public MonoBehaviour target;

        /// <summary>
        /// 执行buff带有的函数
        /// </summary>
        public void ExecuteBuff()
        {
            this.Diffusing("Execute", this);
        }
        public void Resonance(Buff buff)
        {
            foreach (var wrapper in this.Buffer)
            {
                foreach (var item in wrapper.Value.As<BuffWrapper>().Buffs)
                {
                    item.Resonance(this, buff);
                }
            }
        }

        /// <summary>
        /// 寻找指定<see cref="Buff"/>
        /// </summary>
        public List<Buff> SeekBuffs(string buff)
        {
            if (this.Contains(buff))
                return this.GetFrame(buff).As<BuffWrapper>().Buffs;
            else return null;
        }
        /// <summary>
        /// 寻找指定<see cref="Buff"/>
        /// </summary>
        public Buff SeekBuff(string buff)
        {
            return SeekBuffs(buff).Share(out var buffs) == null ? null : buffs[0];
        }

        public InsertResult AddBuff(string key, Buff buff)
        {
            if (SeekBuffs(key).Share(out var li) == null)
            {
                this.Register(key, new BuffWrapper(buff));
                return InsertResult.IsSucceed;
            }
            else
            {
                li.Add(buff);
                return InsertResult.IsReplace;
            }
        }

        public InsertResult AddBuff(Buff buff)
        {
            return AddBuff(buff.Buffer, buff);
        }

        public void RemoveBuff(string key)
        {
            this.Unregister(key);
        }
        public void RemoveBuffByStats(string key)
        {
            if (this.Contains(key) == false) return;
            var buffs = this.SeekBuffs(key);
            buffs.RemoveAll(T => T.GetStats() == false);
        }
    }

    public class BuffWrapper : BaseWrapper.Model
    {
        public List<Buff> Buffs;
        public Buff Buff => Buffs[0];

        public BuffWrapper(params Buff[] buffs) : base(null) { Buffs = buffs.ToList(); }

        public override string ToString()
        {
            return Buffs.GetSubList(T => true, T => T.ToString()).LinkAndInsert(";");
        }
    }

    public class Buff
    {
        public const string ValueSymbol = "Value";
        public const string BuffSymbol = "Buff";

        public Buff(string script,string buff)
        {
            this.BuffScript = script;
            this.Buffer = buff;
        }

        public string BuffScript;
        public string Buffer;
        public string ValueExpr = $"{ValueSymbol}+{BuffSymbol}";
        public float ComputeExpr(float value)
        {
            string expr = new(ValueExpr);
            expr.Replace(ValueSymbol, "(" + value.ToString() + ")");
            expr.Replace(BuffSymbol, "(" + Value.ToString() + ")");
            return expr.Computef();
        }

        private Dictionary<string, float> m_values;
        public Dictionary<string, float> Values
        {
            get => m_values ??= new();
        }
        public virtual float Value
        {
            get
            {
                if(Values.ContainsKey("Value")==false)
                    Values.Add("Value", 0f);
                return Values["Value"];
            }
            set => Values["Value"] = Mathf.Clamp(value, Min, Max);
        }
        public float Max
        {
            get => Values.TryGetValue("Max", out var max) ? max : Mathf.Infinity;
            set => Values["Max"] = value;
        }
        public float Min
        {
            get=>Values.TryGetValue("Min",out var min)?min:Mathf.NegativeInfinity;
            set => Values["Min"] = value;
        }

        public void ChangeValue(string key,float value)
        {
            Values[key] = value;
        }
        public void RemoveValue(string key,float value)
        {
            Values.Remove(key);
        }
        public void AddValue(string key,float initValue)
        {
            Values.TryAdd(key, initValue);
        }
        public void InitValue(string key,float initValue)
        {
            if(Values.ContainsKey(key))
                Values[key] = initValue;
        }

        public virtual void Execute(object element)
        {
            if (string.IsNullOrEmpty(BuffScript)) return;
            LineScript.RunScript(BuffScript, ("this", element), ("buffer", this), ("target", element.As(out BuffManager buffManager) ? buffManager.target : null));
        }

        public virtual bool GetStats()
        {
            return true;
        }

        public virtual void Resonance(BuffManager manager,Buff buff)
        {

        }
    }
}
