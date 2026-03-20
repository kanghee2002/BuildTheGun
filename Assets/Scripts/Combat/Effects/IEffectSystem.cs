namespace BuildTheGun.Combat.Effects
{
    /// <summary>
    /// 효과 실행 추상화. 구현체는 <see cref="EffectSystem"/>이며, 파이프라인은 이 인터페이스만 의존한다.
    /// </summary>
    public interface IEffectSystem
    {
        /// <summary>
        /// 스펙에 맞는 핸들러를 실행한다. 등록되지 않은 <see cref="EffectType"/>은 무시된다.
        /// </summary>
        void Execute(EffectSpec spec, EffectContext ctx);
    }
}
