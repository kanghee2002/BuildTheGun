using BuildTheGun.Combat.Buffs;

namespace BuildTheGun.Combat.Effects.Handlers
{
    public class DelayReduceHandler : IEffectHandler
    {
        public EffectType EffectType => EffectType.DelayReduce;

        public bool CanExecute(EffectContext ctx, EffectParams p) => true;

        public void Execute(EffectContext ctx, EffectParams p)
        {
            var spec = ctx.CurrentEffect;
            var nextN = spec.Target.Mode == BuffTargetMode.NextN ? spec.Target.Count : 0;
            ctx.Accumulator.Push(new PendingBuff
            {
                Target = spec.Target,
                EffectType = EffectType.DelayReduce,
                Params = p,
                RemainingCount = nextN,
                SourceSlotIndex = ctx.SlotIndex,
            });
        }
    }
}
