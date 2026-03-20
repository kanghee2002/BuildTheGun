using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Bullets
{
    public class MergeRuleSpec
    {
        public MergeRuleType RuleType;
        public EffectParams Params = new();
    }

    public enum MergeRuleType
    {
        BaseDamageAdd,
        BaseDelayAdd,
        BasePelletAdd,
    }
}
