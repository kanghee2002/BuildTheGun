namespace BuildTheGun.Combat.Effects
{
    public class EffectSpec
    {
        public EffectType EffectType;
        public TriggerType Trigger;
        public BuffTarget Target;
        public EffectParams Params = new();
        public CounterCondition CounterCondition;
    }
}
