using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Buffs
{
    /// <summary>
    /// 아직 소비되지 않은 버프. <see cref="BuffAccumulator"/>에 쌓였다가 슬롯 처리 시 매칭된다.
    /// </summary>
    public struct PendingBuff
    {
        public BuffTarget Target;
        public EffectType EffectType;
        public EffectParams Params;
        public int RemainingCount;
        public int SourceSlotIndex;
    }
}
