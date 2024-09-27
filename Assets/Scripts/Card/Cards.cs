using Diagram;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DemoGame
{
    #region Attribute Buffs

    public class DamageAttribute : AttributeBuff
    {
        public DamageAttribute(float initValue) : base(initValue, CardConfig.DamageAttributeName) { }
    }

    #endregion

    public abstract class Card : Diagram.Game.RoleCard<CardConfig>
    {
#if UNITY_EDITOR
        public BuffManager EDITOR__Attribute;
#endif

        public const string RoundClockModuleName = "Round";

        #region Init
        public virtual void UpdateStats()
        {
            if (config.Damage >= 0)
            {
                DamageObject.SetActive(true);
                DamageText.text = this.GetAttribute(CardConfig.DamageAttributeName).Value.ToString();
            }
            else if (DamageObject != null) DamageObject.SetActive(false);
            if (config.HP >= 0)
            {
                HPObject.SetActive(true);
                HPText.text = this.GetHealthAttribute().Value.ToString();
            }
            else if (HPObject != null) HPObject.SetActive(false);
        }

        public GameObject DamageObject;
        public TMP_Text DamageText;
        public GameObject HPObject;
        public TMP_Text HPText;

        public Image NameTextImage;
        public TMP_Text NameText;

        public void SetName(string name)
        {
            NameText.text = name;
        }

        protected override void BeforeLoadLS()
        {
#if UNITY_EDITOR
            EDITOR__Attribute = Attributes;
#endif
            if (ClockMan.TimeModules.ContainsKey(RoundClockModuleName) == false)
            {
                ClockMan.InitTime(RoundClockModuleName, 0, 0);
            }
            if (config != null)
            {
                if (config.Damage >= 0)
                {
                    this.AddBuff(CardConfig.DamageAttributeName, new DamageAttribute(config.Damage));
                }
                this.InitHealth(CardConfig.HPAttributeName, new HP(config.HP));
            }
            base.BeforeLoadLS();
        }
        protected override void AfterLoadLS()
        {
            base.AfterLoadLS();
            UpdateStats();
        }
        #endregion

        #region Attack About
        /// <summary>
        /// 播放攻击动画
        /// </summary>
        protected abstract void DisplayAttackAnimation(AnyInfomation info);
        /// <summary>
        /// 接收被攻击者的受击反馈,计算效果,形成新的反馈
        /// </summary>
        public virtual AnyInfomation HandleAttackFeedback(AnyInfomation info) => AnyInfomation.NoneInfo;
        /// <summary>
        /// 单体攻击,立即生效
        /// </summary>
        /// <param name="to"></param>
        public virtual AnyInfomation Attack(Card to)
        {
            var attack = this.MakeAttackInfo();
            var feedback = to.UnderAttack(attack);
            DisplayAttackAnimation(feedback);
            return HandleAttackFeedback(feedback);
        }
        #endregion
    }
}

