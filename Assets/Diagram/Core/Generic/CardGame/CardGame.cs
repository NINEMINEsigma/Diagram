using System;
using UnityEngine;

namespace Diagram.Game
{
    [Serializable]
    /// <summary>
    /// Basic class of Playing Card
    /// </summary>
    public partial class Card<_CardConfig> : Role where _CardConfig : CardConfig
    {
        public _CardConfig config;

        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, config.CardType);
            base.BeforeLoadLS();
        }

        protected override void AfterLoadLS()
        {
            base.AfterLoadLS();
            new LineScript(("this",this)).Run(config.BehaviourScript);
        }
    }

    [Serializable]
    public class RoleCard<_CardConfig> : Card<_CardConfig> where _CardConfig : CardConfig
    {
        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, "Character");
            base.BeforeLoadLS();
        }

    }
    [Serializable]
    public class FunctionalCard<_CardConfig> : Card<_CardConfig> where _CardConfig : CardConfig
    {
        protected override void BeforeLoadLS()
        {
            InternalUitility.SetTag(this.gameObject, "Functional");
            base.BeforeLoadLS();
        }
    }

    #region Modules

    [Serializable]
    [CreateAssetMenu(fileName = "New CardConfig", menuName = "Diagram/Game/CardConfig", order = 0)]
    public partial class CardConfig: ScriptableObject
    {
        [Header("Core Attribute")]
        public string CardType = "default";
        public string CardBelong = "None";
        [TextArea(5, 20)] public string BehaviourScript;
        [Header("Attributes")]
        [TextArea(3, 10)] public string description;
    }

    #endregion
}
