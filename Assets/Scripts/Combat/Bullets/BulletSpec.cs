using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Bullets
{
    public class BulletSpec
    {
        public string Id;
        public BulletType BulletType;
        public float BaseDamage;
        public float BaseDelaySec;
        public int BasePelletCount;
        public float BuffAmplifier = 1f;
        public Keyword[] Keywords = System.Array.Empty<Keyword>();
        public EffectSpec[] Effects = System.Array.Empty<EffectSpec>();
        public MergeRuleSpec[] MergeRules = System.Array.Empty<MergeRuleSpec>();
        public Rarity Rarity;
        public int Cost;
    }
}
