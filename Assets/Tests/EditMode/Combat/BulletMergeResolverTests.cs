using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Effects;
using NUnit.Framework;

namespace BuildTheGun.Combat.Tests
{
    public class BulletMergeResolverTests
    {
        [Test]
        public void Merge_SumsBaseStatsAndMergeRules()
        {
            var effects = EffectSystem.CreateWithDefaultHandlers();

            var a = BulletState.FromSpec(new BulletSpec
            {
                BulletType = BulletType.Fire,
                BaseDamage = 10f,
                BasePelletCount = 1,
                BaseDelaySec = 0.3f,
                MergeRules = new[]
                {
                    new MergeRuleSpec
                    {
                        RuleType = MergeRuleType.BaseDamageAdd,
                        Params = CreateParams("amount", 2f),
                    },
                },
            });

            var b = BulletState.FromSpec(new BulletSpec
            {
                BulletType = BulletType.Fire,
                BaseDamage = 8f,
                BasePelletCount = 2,
                BaseDelaySec = 0.2f,
            });

            var merged = BulletMergeResolver.Merge(a, b, effects);

            Assert.AreEqual(20f, merged.MergedBaseDamage, 0.001f);
            Assert.AreEqual(3, merged.MergedBasePelletCount);
            Assert.AreEqual(0.5f, merged.MergedBaseDelay, 0.001f);
        }

        private static EffectParams CreateParams(string key, float value)
        {
            var p = new EffectParams();
            p.Set(key, value);
            return p;
        }
    }
}
