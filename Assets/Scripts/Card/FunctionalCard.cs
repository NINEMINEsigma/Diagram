using System.Collections;
using System.Collections.Generic;
using Diagram;
using TMPro;
using UnityEngine;

namespace DemoGame
{
    public class FunctionalCard : Card
    {
        public const string CostAttributeName = "Cost";

        protected override void DisplayAttackAnimation(AnyInfomation info)
        {

        }

        public override void UpdateStats()
        {
            base.UpdateStats();
            CostText.text = Cost.ToString();
        }

        public TMP_Text CostText;
        public int Cost
        {
            get
            {
                if (Attributes.Contains(CostAttributeName) == false)
                    AddBuff(CostAttributeName, new AttributeBuff(1, "cost"));
                return (int)this.BuffsGetFinalValue(CostAttributeName, 0);
            }
            set
            {
                if (Attributes.Contains(CostAttributeName) == false)
                    AddBuff(CostAttributeName, new AttributeBuff(1, "cost"));
                Attributes.SeekBuff(CostAttributeName, "cost").Value = value;
            }
        }
        public void SetCost(int value)
        {
            Cost = value;
        }
    }
}
