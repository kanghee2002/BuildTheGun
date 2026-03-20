namespace BuildTheGun.Combat.Effects
{
    public interface IEffectHandler
    {
        EffectType EffectType { get; }

        void Execute(EffectContext ctx, EffectParams p);

        bool CanExecute(EffectContext ctx, EffectParams p);
    }
}
