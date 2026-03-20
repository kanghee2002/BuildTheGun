using BuildTheGun.Combat.Buffs;

namespace BuildTheGun.Combat.Effects.Handlers
{
    public class DelayRemoveHandler : IEffectHandler
    {
        public EffectType EffectType => EffectType.DelayRemove;

        public bool CanExecute(EffectContext ctx, EffectParams p) => true;

        public void Execute(EffectContext ctx, EffectParams p)
        {
            var spec = ctx.CurrentEffect;
            var nextN = spec.Target.Mode == BuffTargetMode.NextN ? spec.Target.Count : 0;
            ctx.Accumulator.Push(new PendingBuff
            {
                Target = spec.Target,
                EffectType = EffectType.DelayRemove,
                Params = p,
                RemainingCount = nextN,
                SourceSlotIndex = ctx.SlotIndex,
            });
        }
    }
}
