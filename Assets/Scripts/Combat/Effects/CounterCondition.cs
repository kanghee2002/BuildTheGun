namespace BuildTheGun.Combat.Effects
{
    public enum CounterType
    {
        /// <summary>적 처치 횟수 (정수 카운트). "EnemyKilled" 한 건이 아니라 누적 횟수.</summary>
        EnemyKills,
        HitsDealt,
        ReloadsPerformed,
        DamageTaken,
        BulletsPlayed,
        GoldSpent,
    }

    /// <summary>
    /// 카운터 값의 수명: 전투 한 판만 유지할지, 런 전체로 유지할지.
    /// </summary>
    public enum Lifetime
    {
        PerBattle,
        Permanent,
    }

    /// <summary>
    /// "적 N마리 처치 시" 등 조건 충족 시 효과 발동에 사용한다.
    /// </summary>
    public class CounterCondition
    {
        public CounterType CounterType;
        public int Threshold;
        public bool IsRepeating;
        public Lifetime Lifetime;
    }
}
