using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Effects;
using BuildTheGun.Combat.Guns;
using BuildTheGun.Combat.Magazine;
using BuildTheGun.Combat.Pipeline;
using NUnit.Framework;

namespace BuildTheGun.Combat.Tests
{
    public class BulletPipelineIntegrationTests
    {
        [Test]
        public void BuffThenFire_AccumulatesDamageOnFire()
        {
            var effects = EffectSystem.CreateWithDefaultHandlers();
            var pipeline = new BulletResolutionPipeline(effects);

            var buffSpec = new BulletSpec
            {
                BulletType = BulletType.Buff,
                BasePelletCount = 1,
                Effects = new[]
                {
                    new EffectSpec
                    {
                        EffectType = EffectType.DamageAdd,
                        Trigger = TriggerType.None,
                        Target = new BuffTarget
                        {
                            Mode = BuffTargetMode.Next,
                            Filter = BulletTypeFilter.FireOnly,
                        },
                        Params = CreateParams("amount", 10f),
                    },
                },
            };

            var fireSpec = new BulletSpec
            {
                BulletType = BulletType.Fire,
                BaseDamage = 5f,
                BasePelletCount = 1,
            };

            var magazine = new MagazineState
            {
                Slots = new[]
                {
                    new MagazineSlot { Bullet = BulletState.FromSpec(buffSpec), IsActive = true },
                    new MagazineSlot { Bullet = BulletState.FromSpec(fireSpec), IsActive = true },
                },
            };

            var gun = new GunState
            {
                Spec = new GunSpec(),
                Magazines = new[] { magazine },
            };

            var ctx = new EffectContext { MagazineInfo = new MagazineContext(magazine) };
            var sequence = pipeline.ProcessMagazine(gun, magazine, ctx);

            Assert.AreEqual(2, sequence.Steps.Count);
            Assert.IsNull(sequence.Steps[0].Fire);
            Assert.IsNotNull(sequence.Steps[1].Fire);
            Assert.AreEqual(15f, sequence.Steps[1].Fire.Damage, 0.001f);
        }

        private static EffectParams CreateParams(string key, float value)
        {
            var p = new EffectParams();
            p.Set(key, value);
            return p;
        }
    }
}
