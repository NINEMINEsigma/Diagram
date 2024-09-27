using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Diagram;

namespace DemoGame
{
    public class Character : Card
    {
        protected override void DisplayAttackAnimation(AnyInfomation info)
        {

        }

        #region Attack About

        /// <summary>
        /// 播放受击动画
        /// </summary>
        protected virtual void DisplayUnderAttackAnimation(AnyInfomation info)
        {

        }
        /// <summary>
        /// 接收攻击信息,计算攻击伤害,扣除生命值
        /// </summary>
        /// <param name="info"></param>
        /// <returns></returns>
        public override AnyInfomation UnderAttack(AnyInfomation info)
        {
            DisplayUnderAttackAnimation(info);
            this.config.UnderAttack(info);
            var result = base.UnderAttack(info);
            return result;
        }

        #endregion
    }
}
