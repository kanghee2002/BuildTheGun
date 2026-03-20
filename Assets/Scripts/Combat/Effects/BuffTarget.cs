namespace BuildTheGun.Combat.Effects
{
    /// <summary>
    /// pending 버프가 어떤 슬롯·몇 번까지 적용되는지. 강화 총알 효과 push 시 설정한다.
    /// </summary>
    public enum BuffTargetMode
    {
        Next,
        NextN,
        Last,
        All,
        Remaining,
    }

    /// <summary>
    /// 버프가 발사 총알만 받을지(FireOnly), 강화 총알에도 적용될지(Any) 구분한다.
    /// </summary>
    public enum BulletTypeFilter
    {
        Any,
        FireOnly,
    }

    /// <summary>
    /// <see cref="BuffAccumulator"/>가 매칭할 때 사용하는 대상 모드·필터·NextN일 때 횟수(<see cref="Count"/>).
    /// </summary>
    public struct BuffTarget
    {
        public BuffTargetMode Mode;
        public BulletTypeFilter Filter;
        public int Count;
    }
}
