using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Diagram
{
    #region Infomation

    public sealed class AnyInfomation : Buff
    {
        public static AnyInfomation NoneInfo = new(null, 0);

        public Role from;

        private AnyInfomation(Role from, float value, params Buff[] buffs) : base(null, "info")
        {
            this.from = from;
            this.m_value = value;
            this.BuffList = buffs;
        }

        private float m_value;
        public override float Value
        {
            get => m_value; set => m_value = value;
        }

        public Buff[] BuffList = new Buff[0];

        private static Queue<AnyInfomation> s_queue = new();
        public static AnyInfomation Obtain(Role from, float value, params Buff[] buffs)
        {
            if (s_queue.Count == 0)
                return new(from, value);
            else
            {
                var result = s_queue.Dequeue();
                result.from = from;
                result.Value = value;
                result.BuffList = buffs;
                return result;
            }
        }
        public static void Back(AnyInfomation info)
        {
            s_queue.Enqueue(info);
        }

        public AnyInfomation SetupTag(string tag)
        {
            this.Buffer = tag;
            return this;
        }
        public bool TagIs(string tag)
        {
            return this.Buffer == tag;
        }
    }

    #endregion

    #region Role

    public partial class ClockRole : Role
    {
        public Dictionary<string, float> TimeModules = new() { { "Global", 0 } };
        public Dictionary<string, float> SpeedModules = new() { { "Global", 1 } };

        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, "Clock");
            base.BeforeLoadLS();
        }

        public void InitTime(string name, float speed = 1, float initVal = 0)
        {
            TimeModules[name] = initVal;
            SpeedModules[name] = speed;
        }
        public void RemoveTime(string name)
        {
            TimeModules.Remove(name);
            SpeedModules.Remove(name);
        }

        protected virtual void FixedUpdate()
        {
            foreach (var timeClock in TimeModules.GetSubListAboutKey())
            {
                TimeModules[timeClock] += SpeedModules[timeClock] * Time.fixedDeltaTime;
            }
        }
    }

    /// <summary>
    /// Basic Class of playing character
    /// </summary>
    public abstract partial class Role : LineBehaviour
    {
        private static ClockRole s_clockMan;
        public static ClockRole ClockMan
        {
            get
            {
                if (s_clockMan == null)
                    s_clockMan = new GameObject("ClockMan").AddComponent<ClockRole>();
                return s_clockMan;
            }
            set => s_clockMan = value;
        }

        #region Init

        /// <summary>
        /// reset values basiclly
        /// </summary>
        [_Init_]
        public override void Reset()
        {
            base.Reset();
            attributes = new(this);
        }

        /// <summary>
        /// Complete the reset and initialization
        /// </summary>
        [_Init_]
        private void Start()
        {
            Reset();
#if UNITY_EDITOR
            InternalUitility.SetTag(this.gameObject, nameof(Role));
#endif
            BeforeLoadLS();
            ReloadLineScript();
            AfterLoadLS();
        }
        protected virtual void BeforeLoadLS() { }
        protected virtual void AfterLoadLS() { }

        #endregion

        #region Buff Frames

        private BuffManager attributes;
        public BuffManager Attributes => attributes;
        public bool ContainsBuff(string key)
        {
            return Attributes.Contains(key);
        }
        public void RemoveBuff(string key)
        {
            Attributes.RemoveBuff(key);
        }
        public void RemoveBuffByStats(string key)
        {
            Attributes.RemoveBuffByStats(key);
        }
        public void AddBuff(string key, Buff buff)
        {
            Attributes.AddBuff(key, buff);
        }
        public void UpdateBuffTick()
        {
            Attributes.ExecuteBuff();
        }
        /// <summary>
        /// The final result of calculating the value named <b>Value</b> for all buffs in the same series is given by the expr expression
        /// </summary>
        /// <param name="key">series name</param>
        /// <param name="initValue">start value</param>
        /// <param name="expr">This is a binary expression, the current value is named <b>Value</b>, and the current buff value is named <b>Buff</b></param>
        /// <returns></returns>
        public float BuffsGetFinalValue(string key, float initValue)
        {
            var buffs = this.Attributes.SeekBuffs(key);
            if (buffs == null || buffs.Count == 0)
                return 0f;
            else
            {
                float result = initValue;
                foreach (var buff in buffs)
                {
                    result = buff.ComputeExpr(result);
                }
                return result;
            }
        }
        public Buff GetAttribute(string key)
        {
            return this.Attributes.SeekBuff(key);
        }

        #endregion

        #region HP Functional

        /// <summary>
        /// Loads health and names it
        /// </summary>
        public void InitHealth(string key, Buff stats)
        {
            this.AddBuff(key, stats);
            HealthAttributeName = key;
        }
        public Buff GetHealthAttribute()
        {
            return GetAttribute(HealthAttributeName);
        }
        protected virtual float MakeFinalDamage(float damage)
        {
            var old = this.GetHealthAttribute().Value;
            this.GetHealthAttribute().Value -= damage;
            return old - this.GetHealthAttribute().Value;
        }
        protected virtual float MakeFinalTreat(float treat)
        {
            var old = this.GetHealthAttribute().Value;
            this.GetHealthAttribute().Value += treat;
            return this.GetHealthAttribute().Value - old;
        }
        public virtual bool IsAlive()
        {
            return this.ContainsBuff(this.HealthAttributeName) ? this.GetHealthAttribute().Value > 0 : true;
        }
        public virtual AnyInfomation UnderAttack(AnyInfomation info)
        {
            AnyInfomation.Back(info);
            this.Attributes.Resonance(info);
            foreach (var buff in info.BuffList)
                this.Attributes.Resonance(buff);
            return AnyInfomation.Obtain(this, MakeFinalDamage(Mathf.Clamp(info.Value, 0, Mathf.Infinity))).SetupTag(AttackAboutBuff.AttackFeedback);
        }
        public virtual AnyInfomation ReceiveTreatment(AnyInfomation info)
        {
            AnyInfomation.Back(info);
            this.Attributes.Resonance(info);
            foreach (var buff in info.BuffList)
                this.Attributes.Resonance(buff);
            return AnyInfomation.Obtain(this, MakeFinalTreat(Mathf.Clamp(info.Value, 0, Mathf.Infinity))).SetupTag(TreatAboutBuff.TreatFeedback);
        }

        #endregion

        #region Attack Functional

        public virtual AnyInfomation MakeAttackInfo()
        {
            var attack = AnyInfomation.Obtain(this, 0);
            attack.SetupTag(AttackAboutBuff.MakeAttackAnyInfo);
            this.Attributes.Resonance(attack);
            attack.SetupTag(AttackAboutBuff.UnderAttackAnyInfo);
            return attack;
        }

        #endregion

        #region Treat Functional

        public virtual AnyInfomation MakeTreatInfo()
        {
            var treat = AnyInfomation.Obtain(this, 0);
            treat.SetupTag(TreatAboutBuff.MakeTreatAnyInfo);
            this.Attributes.Resonance(treat);
            treat.SetupTag(TreatAboutBuff.ReceiveOverTimeTreatmentAnyInfo);
            return treat;
        }

        #endregion

        #region Update Time

        protected void UpdateBuffsTickDependence(AnyInfomation info)
        {
            AnyInfomation.Back(info);
            this.Attributes.Resonance(info);
            foreach (var buff in info.BuffList)
                this.Attributes.Resonance(buff);
        }
        private void Update()
        {
            var info = AnyInfomation.Obtain(ClockMan, ClockMan.TimeModules[TimeObserverName]);
            this.UpdateBuffsTickDependence(info);
        }
        public void SwitchTimeObserver(string name)
        {
            if (ClockMan.TimeModules.ContainsKey(name) == false)
                ClockMan.InitTime(name);
            this.TimeObserverName = name;
        }

        #endregion

        #region Attributes

        public bool FriendlyDamage = true;
        public string HealthAttributeName = "HP";
        public string TimeObserverName = "Global";

        #endregion
    }

    #endregion

    #region Buff Definition

    public class AttributeBuff : Buff
    {
        public override float Value { get; set; }

        public AttributeBuff(float initValue, string buffname) : base(null, buffname)
        {
            this.Value = initValue;
        }

        public readonly static string[] AttributeNames = { HP.HPBuffKey };
        public readonly static string[] ExprParameterNames = { Buff.BuffSymbol, Buff.ValueSymbol };
        public readonly static string[] BuffNames =
        {
            AttackAboutBuff.MakeAttackAnyInfo,AttackAboutBuff.UnderAttackAnyInfo,
            TickAboutBuff.UpdateTickAnyInfo
        };
    }

    public class HP : AttributeBuff
    {
        public const string HPBuffKey = nameof(HP);
        public HP(float initValue) : base(initValue, nameof(HP)) { }
    }
    public class AttackAboutBuff : AttributeBuff
    {
        public const string UnderAttackAnyInfo = "Attack";
        public const string MakeAttackAnyInfo = "MakeAttack";
        public const string UnderOverTimeAttackAnyInfo = "AttackOverTime";
        public const string AttackFeedback = "AttackFeedback";

        public AttackAboutBuff(float initValue, string expr, string buff) : base(initValue, buff)
        {
            this.ValueExpr = expr;
        }

        /// <summary>
        /// 受到的攻击得到削弱,buff
        /// </summary>
        public class AttackReduction : AttackAboutBuff
        {
            public AttackReduction(float value, string buff) : base(value, $"{ValueSymbol}-${BuffSymbol}", buff) { }
            public AttackReduction(float value, string expr, string buff) : base(value, expr, buff) { }

            protected void Reduction(AnyInfomation info)
            {
                info.Value = this.ComputeExpr(info.Value);
            }
            public override void Resonance(BuffManager manager, Buff buff)
            {
                if (buff.Buffer == UnderAttackAnyInfo)
                {
                    Reduction(buff as AnyInfomation);
                }
            }
        }
        /// <summary>
        /// 受到的攻击得到强化,debuff
        /// </summary>
        public class AttackIncreased : AttackAboutBuff
        {
            public AttackIncreased(float value, string buff) : base(value, $"{ValueSymbol}+{BuffSymbol}", buff) { }
            public AttackIncreased(float value, string expr, string buff) : base(value, expr, buff) { }

            protected void Increase(AnyInfomation info)
            {
                info.Value = this.ComputeExpr(info.Value);
            }
            public override void Resonance(BuffManager manager, Buff buff)
            {
                if (buff.Buffer == UnderAttackAnyInfo)
                {
                    Increase(buff as AnyInfomation);
                }
            }
        }
        /// <summary>
        /// 发出的攻击得到削弱,debuff
        /// </summary>
        public class ReduceAttack : AttackAboutBuff
        {
            public ReduceAttack(float value, string buff) : base(value, $"{ValueSymbol}-{BuffSymbol}", buff) { }
            public ReduceAttack(float value, string expr, string buff) : base(value, expr, buff) { }

            protected void Reduction(AnyInfomation info)
            {
                info.Value = this.ComputeExpr(info.Value);
            }
            public override void Resonance(BuffManager manager, Buff buff)
            {
                if (buff.Buffer == MakeAttackAnyInfo)
                {
                    Reduction(buff as AnyInfomation);
                }
            }
        }
        /// <summary>
        /// 发出的攻击得到强化,buff
        /// </summary>
        public class IncreaseAttack : AttackAboutBuff
        {
            public IncreaseAttack(float value, string buff) : base(value, $"{ValueSymbol}+{BuffSymbol}", buff) { }
            public IncreaseAttack(float value, string expr, string buff) : base(value, expr, buff) { }

            protected void Increase(AnyInfomation info)
            {
                info.Value = this.ComputeExpr(info.Value);
            }
            public override void Resonance(BuffManager manager, Buff buff)
            {
                if (buff.Buffer == MakeAttackAnyInfo)
                {
                    Increase(buff as AnyInfomation);
                }
            }
        }
    }
    public class TreatAboutBuff : AttributeBuff
    {
        public const string ReceiveTreatmentAnyInfo = "Treat";
        public const string MakeTreatAnyInfo = "MakeTreat";
        public const string ReceiveOverTimeTreatmentAnyInfo = "TreatOverTime";
        public const string TreatFeedback = "TreakFeedback";

        public TreatAboutBuff(float initValue, string expr, string buffname) : base(initValue, buffname)
        {
            this.ValueExpr = expr;
        }
    }
    public class TickAboutBuff : AttributeBuff
    {
        public const string UpdateTickAnyInfo = "UpdateTick";

        public List<string> TargetBuffs;
        public float effectTime;

        public void SetupTargetBuffs(params string[] buffs)
        {
            TargetBuffs = buffs.ToList();
        }
        public void AddTargetBuffs(params string[] buffs)
        {
            TargetBuffs.AddRange(buffs);
        }
        public void RemoveTargetBuffs(params string[] buffs)
        {
            TargetBuffs.RemoveAll(T => buffs.Contains(T));
        }

        public TickAboutBuff(float effectTime, float initValue, string buffname) : base(initValue, buffname)
        {
            this.ValueExpr = $"{ValueSymbol}*{BuffSymbol}";
            this.effectTime = effectTime;
        }
        public TickAboutBuff(float effectTime, float initValue, string expr, string buffname) : base(initValue, buffname)
        {
            this.ValueExpr = expr;
            this.effectTime = effectTime;
        }

        public override bool GetStats()
        {
            return effectTime > 0;
        }
        protected virtual void Update(BuffManager manager, AnyInfomation info)
        {
            var effcetValue = this.ComputeExpr(info.Value);
            effectTime -= info.Value;
            foreach (var target in TargetBuffs)
            {
                foreach (var buff in manager.SeekBuffs(target))
                {
                    buff.Value += effcetValue;
                }
            }
        }
        public override sealed void Resonance(BuffManager manager, Buff buff)
        {
            if (buff.Buffer == UpdateTickAnyInfo)
            {
                Update(manager, buff as AnyInfomation);
            }
        }

        public class DamageOverTime : TickAboutBuff
        {
            public DamageOverTime(float effectTime, float value, string buff) : base(effectTime, value, buff) { }
            public DamageOverTime(float effectTime, float value, string expr, string buff) : base(effectTime, value, expr, buff) { }
            protected override void Update(BuffManager manager, AnyInfomation info)
            {
                var effcetValue = this.ComputeExpr(info.Value);
                effectTime -= info.Value;
                var role = manager.target.As<Role>();
                var treatInfo = AnyInfomation.Obtain(Role.ClockMan, effcetValue, this);
                treatInfo.Buffer = AttackAboutBuff.UnderOverTimeAttackAnyInfo;
                role.UnderAttack(treatInfo);
            }
        }
        public class TreatOverTime : TickAboutBuff
        {
            public TreatOverTime(float effectTime, float initValue, string buffname) : base(effectTime, initValue, buffname) { }
            public TreatOverTime(float effectTime, float initValue, string expr, string buffname) : base(effectTime, initValue, expr, buffname) { }
            protected override void Update(BuffManager manager, AnyInfomation info)
            {
                var effcetValue = this.ComputeExpr(info.Value);
                effectTime -= info.Value;
                var role = manager.target.As<Role>();
                var treatInfo = AnyInfomation.Obtain(Role.ClockMan, effcetValue, this);
                role.ReceiveTreatment(treatInfo);
            }
        }
        public class CheckBuffOverTime : TickAboutBuff
        {
            public const string DeleteBuff = "Delete";

            private string checkBuffSeries;
            private string checkBuffValueName;

            public string executeOperator;

            public CheckBuffOverTime(float effectTime, string checkTarget, string executeOperator, string buffname) : this(effectTime, checkTarget, "Value", executeOperator, buffname) { }
            public CheckBuffOverTime(float effectTime, string checkTarget, string checkTargetValueName, string executeOperator, string buffname) : base(effectTime, 0, buffname)
            {
                this.checkBuffSeries = checkTarget;
                this.checkBuffValueName = checkTargetValueName;
                this.executeOperator = executeOperator;
            }

            protected virtual bool CheckEveryBuff(Buff buff)
            {
                return this.ComputeExpr(buff.Values[checkBuffValueName]) > 0;
            }
            protected override void Update(BuffManager manager, AnyInfomation info)
            {
                var series = manager.SeekBuffs(checkBuffSeries);
                if (series == null) return;
                if (series.Any(T => this.CheckEveryBuff(T) == false) == false) return;
                switch (this.executeOperator)
                {
                    case DeleteBuff:
                        {
                            manager.RemoveBuff(checkBuffSeries);
                        }
                        break;
                    default:
                        {
                            foreach (var buff in series)
                            {
                                buff.Value = this.ComputeExpr(buff.Value);
                            }
                        }
                        break;
                }
            }
        }
    }

    #endregion

    #region Faction

    public abstract partial class FriendlyCharacter : Role
    {
        public bool IsEnableFriendlyDamage = true;
        public bool IsEnableEnemyHealing = false;
        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, nameof(FriendlyCharacter));
            base.BeforeLoadLS();
        }
        public override AnyInfomation UnderAttack(AnyInfomation info)
        {
            if (IsEnableFriendlyDamage == false && info.TagIs(this.gameObject.tag))
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(AttackAboutBuff.AttackFeedback);
            }
            else return base.UnderAttack(info);
        }
        public override AnyInfomation ReceiveTreatment(AnyInfomation info)
        {
            if (IsEnableEnemyHealing == false && info.TagIs(this.gameObject.tag) == false)
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(TreatAboutBuff.TreatFeedback);
            }
            else return base.ReceiveTreatment(info);
        }
    }
    public abstract partial class HostileCharacter : Role
    {
        public bool IsEnableFriendlyDamage = true;
        public bool IsEnableEnemyHealing = false;
        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, nameof(HostileCharacter));
            base.BeforeLoadLS();
        }
        public override AnyInfomation UnderAttack(AnyInfomation info)
        {
            if (IsEnableFriendlyDamage == false && info.TagIs(this.gameObject.tag))
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(AttackAboutBuff.AttackFeedback);
            }
            else return base.UnderAttack(info);
        }
        public override AnyInfomation ReceiveTreatment(AnyInfomation info)
        {
            if (IsEnableEnemyHealing == false && info.TagIs(this.gameObject.tag) == false)
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(TreatAboutBuff.TreatFeedback);
            }
            else return base.ReceiveTreatment(info);
        }
    }
    public abstract partial class NeutralCharacter : Role
    {
        public bool IsEnableDamage = true;
        public bool IsEnableTreat = true;
        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, nameof(NeutralCharacter));
            base.BeforeLoadLS();
        }
        public override AnyInfomation UnderAttack(AnyInfomation info)
        {
            if (IsEnableDamage == false)
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(AttackAboutBuff.AttackFeedback);
            }
            else return base.UnderAttack(info);
        }
        public override AnyInfomation ReceiveTreatment(AnyInfomation info)
        {
            if (IsEnableTreat)
            {
                AnyInfomation.Back(info);
                return AnyInfomation.Obtain(null, 0).SetupTag(TreatAboutBuff.TreatFeedback);
            }
            else return base.ReceiveTreatment(info);
        }
    }

    #endregion

    #region Attack Modules

    #endregion
}

