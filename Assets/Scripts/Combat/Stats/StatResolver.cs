using System.Collections.Generic;
using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Guns;
using BuildTheGun.Combat.Magazine;
using UnityEngine;

namespace BuildTheGun.Combat.Stats
{
    /// <summary>
    /// 총알·버프·총·슬롯 수정자를 합쳐 최종 피해·연발 수를 계산한다 (BulletPipeline §5).
    /// </summary>
    public class StatResolver
    {
        /// <summary>
        /// 발사 총알에 적용할 pending 버프는 <paramref name="buffAmplifier"/>로 스케일된 뒤 합산된다.
        /// </summary>
        public FinalStats Resolve(
            BulletState bullet,
            IReadOnlyList<PendingBuff> buffs,
            float buffAmplifier,
            GunState gun,
            MagazineSlot slot)
        {
            var baseDamage = bullet.MergedBaseDamage
                             + PendingBuffStatExtractor.Sum(buffs, StatCategory.BaseDamage, buffAmplifier)
                             + gun.BattleModifiers.Sum(StatCategory.BaseDamage)
                             + bullet.PermanentValues.GetOrDefault(StatCategory.BaseDamage)
                             + slot.SlotModifiers.Sum(StatCategory.BaseDamage);

            var damageIncrease = PendingBuffStatExtractor.Sum(buffs, StatCategory.DamageIncrease, buffAmplifier);

            var pelletAdditive = bullet.MergedBasePelletCount
                               + PendingBuffStatExtractor.Sum(buffs, StatCategory.PelletCount, buffAmplifier)
                               + bullet.RuntimeModifiers.Sum(StatCategory.PelletCount)
                               + gun.BattleModifiers.Sum(StatCategory.PelletCount)
                               + slot.SlotModifiers.Sum(StatCategory.PelletCount);

            var pelletMult = bullet.RuntimeModifiers.Product(StatCategory.PelletMultiplier)
                             * gun.BattleModifiers.Product(StatCategory.PelletMultiplier)
                             * slot.SlotModifiers.Product(StatCategory.PelletMultiplier);

            var pelletOverride = PendingBuffStatExtractor.TryGetPelletSetOverride(buffs, buffAmplifier);
            int pelletCount;
            if (pelletOverride.HasValue)
                pelletCount = Mathf.Max(1, pelletOverride.Value);
            else
                pelletCount = Mathf.Max(1, Mathf.FloorToInt(pelletAdditive * pelletMult));

            var finalDamage = baseDamage * (1f + damageIncrease);

            return new FinalStats
            {
                Damage = finalDamage,
                PelletCount = pelletCount,
            };
        }

        /// <summary>
        /// 발사 총알: (pelletCount - 1) * BurstDelaySec + baseDelay. Buff 총알: baseDelay만.
        /// DelayRemove/DelayReduce는 pending에서 반영한다.
        /// </summary>
        public static float ComputeSlotDelaySec(
            BulletState bullet,
            FinalStats stats,
            bool isFireBullet,
            float burstDelaySec,
            IReadOnlyList<PendingBuff> buffs,
            float buffAmplifier)
        {
            var baseDelay = bullet.MergedBaseDelay;
            var delayDelta = PendingBuffStatExtractor.SumDelayAdjustments(buffs, buffAmplifier);

            if (float.IsNegativeInfinity(delayDelta))
                baseDelay = 0f;
            else
                baseDelay = Mathf.Max(0f, baseDelay + delayDelta);

            if (!isFireBullet)
                return baseDelay;

            var between = stats.PelletCount > 1 ? (stats.PelletCount - 1) * burstDelaySec : 0f;
            return between + baseDelay;
        }
    }
}
