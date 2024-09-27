using System;
using Diagram;
using UnityEngine;

namespace DemoGame
{
    [Serializable]
    [CreateAssetMenu(fileName = "New CardConfig", menuName = "Demo/Game/CardConfig", order = 0)]
    public partial class CardConfig : Diagram.Game.CardConfig
    {
        [Header("DemoGames")]
        public Vector2 UnderAttackMinMax = new(0, 100);
        public AnimationCurve UnderAttackCurve = AnimationCurve.Linear(0, 1, 1, 1);
        public void UnderAttack(AnyInfomation info)
        {
            info.Value = Mathf.Ceil(info.Value * UnderAttackCurve.Evaluate(Mathf.Clamp01((info.Value - UnderAttackMinMax.x) / (UnderAttackMinMax.y - UnderAttackMinMax.x))));
        }
        [Header("Attack")]
        public const string DamageAttributeName = "damage";
        public float Damage = 0;
        [Header("HP")]
        public const string HPAttributeName = "HP";
        public float HP = 1;

    }
}

