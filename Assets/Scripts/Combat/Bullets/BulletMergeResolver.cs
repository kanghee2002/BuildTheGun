using System.Collections.Generic;
using System.Linq;
using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Effects;
using BuildTheGun.Combat.Stats;
using UnityEngine;

namespace BuildTheGun.Combat.Bullets
{
    /// <summary>
    /// 두 발사(Fire) 총알을 하나의 <see cref="BulletState"/>로 합친다. 스탯 합산, MergeRule 적용, OnMerge 효과 실행 후 나머지를 <see cref="BulletState.MergedEffects"/>에 넣는다.
    /// </summary>
    public static class BulletMergeResolver
    {
        /// <summary>
        /// primary → secondary 순으로 효과를 이어 붙인다. 결과는 <see cref="BulletState.IsMerged"/>가 true이다.
        /// </summary>
        public static BulletState Merge(BulletState primary, BulletState secondary, IEffectSystem effects)
        {
            Debug.Assert(primary.BulletType == BulletType.Fire && secondary.BulletType == BulletType.Fire);

            var merged = new BulletState
            {
                BaseSpec = primary.BaseSpec,
                IsMerged = true,
                RuntimeModifiers = new StatModifierStack(),
                PermanentValues = new Dictionary<StatCategory, float>(),
                MergedBaseDamage = primary.MergedBaseDamage + secondary.MergedBaseDamage,
                MergedBasePelletCount = primary.MergedBasePelletCount + secondary.MergedBasePelletCount,
                MergedBaseDelay = primary.MergedBaseDelay + secondary.MergedBaseDelay,
                MergedBuffAmplifier = Mathf.Max(primary.MergedBuffAmplifier, secondary.MergedBuffAmplifier),
                MergedFrom = CollectOriginals(primary, secondary),
                MergedEffects = new List<EffectSpec>(),
            };

            ApplyMergeRules(merged, primary.BaseSpec.MergeRules);
            ApplyMergeRules(merged, secondary.BaseSpec.MergeRules);

            var mergeCtx = new EffectContext
            {
                SourceBullet = merged,
                Accumulator = new BuffAccumulator(),
                SlotIndex = -1,
            };

            foreach (var effect in EnumerateEffects(primary).Concat(EnumerateEffects(secondary)))
            {
                if (effect.Trigger == TriggerType.OnMerge)
                    effects.Execute(effect, mergeCtx);
                else
                    merged.MergedEffects.Add(effect);
            }

            return merged;
        }

        private static void ApplyMergeRules(BulletState merged, MergeRuleSpec[] rules)
        {
            if (rules == null)
                return;

            foreach (var rule in rules)
            {
                switch (rule.RuleType)
                {
                    case MergeRuleType.BaseDamageAdd:
                        merged.MergedBaseDamage += rule.Params.GetFloat("amount");
                        break;
                    case MergeRuleType.BaseDelayAdd:
                    {
                        var sec = rule.Params.GetFloat("amountSec");
                        if (Mathf.Approximately(sec, 0f))
                            sec = rule.Params.GetFloat("amount");
                        merged.MergedBaseDelay += sec;
                        break;
                    }
                    case MergeRuleType.BasePelletAdd:
                        merged.MergedBasePelletCount += Mathf.RoundToInt(rule.Params.GetFloat("amount"));
                        break;
                }
            }
        }

        private static IEnumerable<EffectSpec> EnumerateEffects(BulletState b)
        {
            if (b.BaseSpec?.Effects == null)
                yield break;

            foreach (var e in b.BaseSpec.Effects)
                yield return e;

            if (b.MergedEffects == null)
                yield break;

            foreach (var e in b.MergedEffects)
                yield return e;
        }

        private static List<BulletSpec> CollectOriginals(BulletState primary, BulletState secondary)
        {
            var list = new List<BulletSpec>();

            if (primary.MergedFrom != null && primary.MergedFrom.Count > 0)
                list.AddRange(primary.MergedFrom);
            else if (primary.BaseSpec != null)
                list.Add(primary.BaseSpec);

            if (secondary.MergedFrom != null && secondary.MergedFrom.Count > 0)
                list.AddRange(secondary.MergedFrom);
            else if (secondary.BaseSpec != null)
                list.Add(secondary.BaseSpec);

            return list;
        }
    }
}
