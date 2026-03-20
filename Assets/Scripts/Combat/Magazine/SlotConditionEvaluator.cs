namespace BuildTheGun.Combat.Magazine
{
    /// <summary>
    /// Overview의 카드 조건(예: Remaining, Last)을 MagazineContext로 평가한다.
    /// </summary>
    public static class SlotConditionEvaluator
    {
        public static bool IsLastActiveSlot(MagazineContext ctx, int slotIndex) =>
            ctx != null && ctx.IsLastActiveSlot(slotIndex);

        public static bool AreAllRemainingFire(MagazineContext ctx, int fromIndex) =>
            ctx != null && ctx.AreAllRemainingSlotsFire(fromIndex);

        public static bool AreAllPrecedingBuff(MagazineContext ctx, int slotIndex) =>
            ctx != null && ctx.AreAllPrecedingSlotsBuff(slotIndex);
    }
}
