using System.Collections.Generic;
using BuildTheGun.Combat.Effects;
using BuildTheGun.Combat.Stats;

namespace BuildTheGun.Combat.Bullets
{
    /// <summary>
    /// 탄창에 장전된 한 발의 런타임 상태. <see cref="BulletSpec"/>은 불변이고, 여기서만 합체·전투 수정이 반영된다.
    /// </summary>
    public class BulletState
    {
        public BulletSpec BaseSpec;
        public StatModifierStack RuntimeModifiers = new();
        public Dictionary<StatCategory, float> PermanentValues = new();

        /// <summary>
        /// <see cref="BulletMergeResolver.Merge"/>로 만들어진 탄이면 true. 단일 스펙 탄은 false.
        /// </summary>
        public bool IsMerged { get; set; }

        public List<BulletSpec> MergedFrom;
        public List<EffectSpec> MergedEffects;

        public float MergedBaseDamage;
        public int MergedBasePelletCount;
        public float MergedBaseDelay;
        public float MergedBuffAmplifier;

        /// <summary>기본 스펙의 <see cref="BulletSpec.BulletType"/>.</summary>
        public BulletType BulletType => BaseSpec != null ? BaseSpec.BulletType : BulletType.Fire;

        /// <summary>
        /// 파이프라인에서 발사 시 적용할 효과 목록. 합체 탄은 <see cref="MergedEffects"/>, 그 외는 <see cref="BulletSpec.Effects"/>.
        /// </summary>
        public IEnumerable<EffectSpec> EnumerateActiveEffects()
        {
            if (IsMerged)
                return MergedEffects != null && MergedEffects.Count > 0
                    ? MergedEffects
                    : System.Array.Empty<EffectSpec>();

            return BaseSpec != null ? BaseSpec.Effects : System.Array.Empty<EffectSpec>();
        }

        /// <summary>
        /// <see cref="BulletSpec"/>만으로 상태를 채운다. 합체 탄이 아니다.
        /// </summary>
        public static BulletState FromSpec(BulletSpec spec)
        {
            return new BulletState
            {
                BaseSpec = spec,
                IsMerged = false,
                MergedBaseDamage = spec.BaseDamage,
                MergedBasePelletCount = spec.BasePelletCount,
                MergedBaseDelay = spec.BaseDelaySec,
                MergedBuffAmplifier = spec.BuffAmplifier,
            };
        }

        /// <summary>
        /// 키워드가 <see cref="Keyword.OnLoad"/> 하나뿐인지. 장전 시 효과는 이미 처리됐으므로
        /// 전투 중 파이프라인에서는 발사/버프 효과를 다시 실행하지 않고, 딜레이(슬롯 비용)만 적용한다.
        /// </summary>
        public bool HasOnlyOnLoadKeyword()
        {
            if (BaseSpec?.Keywords == null || BaseSpec.Keywords.Length == 0)
                return false;
            return BaseSpec.Keywords.Length == 1 && BaseSpec.Keywords[0] == Keyword.OnLoad;
        }
    }
}
