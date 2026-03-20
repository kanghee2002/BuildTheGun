using System.Collections.Generic;
using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Effects;
using UnityEngine;

namespace BuildTheGun.Combat.Stats
{
    /// <summary>
    /// <see cref="PendingBuff"/>의 <see cref="EffectType"/>을 스탯 합산·딜레이 보정에 쓰는 값으로 풀어 쓴다.
    /// </summary>
    public static class PendingBuffStatExtractor
    {
        public static float Sum(IReadOnlyList<PendingBuff> buffs, StatCategory category, float amplifier)
        {
            var sum = 0f;
            if (buffs == null)
                return sum;

            foreach (var buff in buffs)
            {
                var p = buff.Params.Scale(amplifier);
                sum += Extract(buff.EffectType, p, category);
            }

            return sum;
        }

        private static float Extract(EffectType type, EffectParams p, StatCategory category)
        {
            switch (type)
            {
                case EffectType.DamageAdd when category == StatCategory.BaseDamage:
                    return p.GetFloat("amount");
                case EffectType.DamageIncrease when category == StatCategory.DamageIncrease:
                    return p.GetFloat("amount");
                case EffectType.PelletAdd when category == StatCategory.PelletCount:
                    return p.GetFloat("amount");
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// PelletSet이 있으면 가산 대신 덮어쓴 발사량(최소 1)을 반환하고, 없으면 null.
        /// </summary>
        public static int? TryGetPelletSetOverride(IReadOnlyList<PendingBuff> buffs, float amplifier)
        {
            if (buffs == null)
                return null;

            int? setValue = null;
            foreach (var buff in buffs)
            {
                if (buff.EffectType != EffectType.PelletSet)
                    continue;
                var p = buff.Params.Scale(amplifier);
                var v = Mathf.Max(1, p.GetInt("value"));
                setValue = v;
            }

            return setValue;
        }

        public static float SumDelayAdjustments(IReadOnlyList<PendingBuff> buffs, float amplifier)
        {
            var delta = 0f;
            var remove = false;
            if (buffs == null)
                return 0f;

            foreach (var buff in buffs)
            {
                var p = buff.Params.Scale(amplifier);
                switch (buff.EffectType)
                {
                    case EffectType.DelayRemove:
                        remove = true;
                        break;
                    case EffectType.DelayReduce:
                        delta -= Mathf.Abs(p.GetFloat("amountSec", p.GetFloat("amount")));
                        break;
                }
            }

            if (remove)
                return float.NegativeInfinity; // sentinel: remove all base delay

            return delta;
        }
    }
}
