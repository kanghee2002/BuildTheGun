using BuildTheGun.Combat.Buffs;

namespace BuildTheGun.Combat.Effects.Handlers
{
    public class DamageAddHandler : IEffectHandler
    {
        public EffectType EffectType => EffectType.DamageAdd;

        public bool CanExecute(EffectContext ctx, EffectParams p) => true;

        public void Execute(EffectContext ctx, EffectParams p)
        {
            var spec = ctx.CurrentEffect;
            var nextN = spec.Target.Mode == BuffTargetMode.NextN ? spec.Target.Count : 0;
            ctx.Accumulator.Push(new PendingBuff
            {
                Target = spec.Target,
                EffectType = EffectType.DamageAdd,
                Params = p,
                RemainingCount = nextN,
                SourceSlotIndex = ctx.SlotIndex,
            });
        }
    }
}
