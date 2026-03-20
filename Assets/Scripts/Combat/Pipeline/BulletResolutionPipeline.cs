using System.Collections.Generic;
using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Effects;
using BuildTheGun.Combat.Guns;
using BuildTheGun.Combat.Magazine;
using BuildTheGun.Combat.Stats;

namespace BuildTheGun.Combat.Pipeline
{
    /// <summary>
    /// 탄창 슬롯을 순서대로 처리하면서 버프를 소비·스탯을 확정하고, 실행 레이어가 재생할 <see cref="FireSequence"/>를 만든다 (BulletPipeline §3).
    /// 실제 발사 타이밍·대쉬 취소는 GunSystem 쪽이 담당한다.
    /// </summary>
    public class BulletResolutionPipeline
    {
        private readonly StatResolver _statResolver = new();
        private readonly IEffectSystem _effects;

        /// <param name="effects">버프 push·OnFire 등에 사용.</param>
        public BulletResolutionPipeline(IEffectSystem effects)
        {
            _effects = effects;
        }

        /// <summary>
        /// <paramref name="magazine"/>의 활성 슬롯만 순회한다. <paramref name="ctx"/>에 <see cref="EffectContext.Accumulator"/>가 없으면 새로 만든다.
        /// </summary>
        public FireSequence ProcessMagazine(GunState gun, MagazineState magazine, EffectContext ctx)
        {
            ctx.Gun = gun;
            ctx.Magazine = magazine;
            ctx.Accumulator ??= new BuffAccumulator();
            ctx.AppliedBuffs ??= new List<PendingBuff>();

            var sequence = new FireSequence();
            var magInfo = ctx.MagazineInfo ?? new MagazineContext(magazine);
            magInfo.Bind(magazine);

            if (gun?.Spec == null || magazine?.Slots == null)
                return sequence;

            for (var i = 0; i < magazine.Slots.Length; i++)
            {
                var slot = magazine.Slots[i];
                if (!slot.IsActive || slot.Bullet == null)
                    continue;

                var bullet = slot.Bullet;
                ctx.SourceBullet = bullet;
                ctx.SlotIndex = i;
                ctx.AppliedBuffs.Clear();

                var isFire = bullet.BaseSpec.BulletType == BulletType.Fire;
                var isLast = magInfo.IsLastActiveSlot(i);

                var pending = ctx.Accumulator.Collect(i, isFire, isLast);
                ctx.AppliedBuffs.AddRange(pending);

                var amplifier = isFire ? bullet.MergedBuffAmplifier : 1f;

                var finalStats = _statResolver.Resolve(bullet, pending, amplifier, gun, slot);

                if (bullet.BaseSpec.BulletType == BulletType.Buff)
                {
                    if (!bullet.HasOnlyOnLoadKeyword())
                    {
                        for (var iter = 0; iter < finalStats.PelletCount; iter++)
                        {
                            foreach (var effect in bullet.EnumerateActiveEffects())
                            {
                                if (effect.Trigger == TriggerType.OnLoad)
                                    continue;
                                if (effect.Trigger != TriggerType.None)
                                    continue;
                                _effects.Execute(effect, ctx);
                            }
                        }
                    }

                    var delayBuff = StatResolver.ComputeSlotDelaySec(
                        bullet, finalStats, false, gun.Spec.BurstDelaySec, pending, amplifier);
                    sequence.Steps.Add(new FireStep { SlotIndex = i, Fire = null, DelayAfterSec = delayBuff });
                    continue;
                }

                if (bullet.HasOnlyOnLoadKeyword())
                {
                    var delayOnly = StatResolver.ComputeSlotDelaySec(
                        bullet, finalStats, true, gun.Spec.BurstDelaySec, pending, amplifier);
                    sequence.Steps.Add(new FireStep { SlotIndex = i, Fire = null, DelayAfterSec = delayOnly });
                    continue;
                }

                var onFire = new List<EffectSpec>();
                foreach (var effect in bullet.EnumerateActiveEffects())
                {
                    if (effect.Trigger == TriggerType.OnFire)
                        onFire.Add(effect);
                }

                var cmd = new FireCommand
                {
                    SlotIndex = i,
                    Damage = finalStats.Damage,
                    PelletCount = finalStats.PelletCount,
                    OnFireEffects = onFire,
                };

                foreach (var effect in onFire)
                    _effects.Execute(effect, ctx);

                var delayFire = StatResolver.ComputeSlotDelaySec(
                    bullet, finalStats, true, gun.Spec.BurstDelaySec, pending, amplifier);

                sequence.Steps.Add(new FireStep { SlotIndex = i, Fire = cmd, DelayAfterSec = delayFire });
            }

            return sequence;
        }
    }
}
