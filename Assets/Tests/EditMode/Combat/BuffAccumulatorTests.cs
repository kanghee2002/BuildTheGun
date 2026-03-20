using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Effects;
using NUnit.Framework;

namespace BuildTheGun.Combat.Tests
{
    public class BuffAccumulatorTests
    {
        [Test]
        public void NextN_AppliesNTimesThenRemoves()
        {
            var acc = new BuffAccumulator();
            acc.Push(new PendingBuff
            {
                Target = new BuffTarget { Mode = BuffTargetMode.NextN, Filter = BulletTypeFilter.Any, Count = 2 },
                EffectType = EffectType.DelayReduce,
                Params = new EffectParams(),
                RemainingCount = 2,
                SourceSlotIndex = -1,
            });

            var m0 = acc.Collect(0, isFireBullet: false, isLastActiveSlot: false);
            Assert.AreEqual(1, m0.Count);
            Assert.AreEqual(1, acc.Buffs.Count);

            var m1 = acc.Collect(1, isFireBullet: true, isLastActiveSlot: false);
            Assert.AreEqual(1, m1.Count);
            Assert.AreEqual(0, acc.Buffs.Count);
        }

        [Test]
        public void FireOnly_SkipsBuffBullet()
        {
            var acc = new BuffAccumulator();
            acc.Push(new PendingBuff
            {
                Target = new BuffTarget { Mode = BuffTargetMode.Next, Filter = BulletTypeFilter.FireOnly },
                EffectType = EffectType.DamageAdd,
                Params = new EffectParams(),
                RemainingCount = 0,
                SourceSlotIndex = 0,
            });

            var skip = acc.Collect(0, isFireBullet: false, isLastActiveSlot: false);
            Assert.AreEqual(0, skip.Count);
            Assert.AreEqual(1, acc.Buffs.Count);

            var fire = acc.Collect(1, isFireBullet: true, isLastActiveSlot: true);
            Assert.AreEqual(1, fire.Count);
            Assert.AreEqual(0, acc.Buffs.Count);
        }
    }
}
