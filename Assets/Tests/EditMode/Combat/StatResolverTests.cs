using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Guns;
using BuildTheGun.Combat.Magazine;
using BuildTheGun.Combat.Stats;
using NUnit.Framework;

namespace BuildTheGun.Combat.Tests
{
    public class StatResolverTests
    {
        [Test]
        public void Resolve_AppliesBuffAmplifier_ToPendingDamage()
        {
            var bullet = BulletState.FromSpec(new BulletSpec
            {
                BaseDamage = 5f,
                BasePelletCount = 1,
                BulletType = BulletType.Fire,
            });
            bullet.MergedBuffAmplifier = 2f;

            var gun = new GunState { Spec = new GunSpec() };
            var slot = new MagazineSlot();

            var buffs = new[]
            {
                new PendingBuff
                {
                    EffectType = EffectType.DamageAdd,
                    Params = CreateParams("amount", 10f),
                },
            };

            var resolver = new StatResolver();
            var final = resolver.Resolve(bullet, buffs, 2f, gun, slot);

            Assert.AreEqual(25f, final.Damage, 0.001f);
        }

        [Test]
        public void ComputeSlotDelay_Fire_IncludesBurstAndBase()
        {
            var bullet = BulletState.FromSpec(new BulletSpec { BaseDelaySec = 0.3f, BulletType = BulletType.Fire });
            var stats = new FinalStats { Damage = 10f, PelletCount = 3 };
            var delay = StatResolver.ComputeSlotDelaySec(
                bullet, stats, isFireBullet: true, burstDelaySec: 0.1f, System.Array.Empty<PendingBuff>(), 1f);

            Assert.AreEqual(0.5f, delay, 0.001f);
        }

        private static EffectParams CreateParams(string key, float value)
        {
            var p = new EffectParams();
            p.Set(key, value);
            return p;
        }
    }
}
