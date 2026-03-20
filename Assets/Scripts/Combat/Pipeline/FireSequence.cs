using System.Collections.Generic;

namespace BuildTheGun.Combat.Pipeline
{
    /// <summary>
    /// 한 탄창 사이클에 대한 동기적 발사 계획. 순서대로 실행하면 재장전 전까지의 동작이 재현된다.
    /// </summary>
    public class FireSequence
    {
        public List<FireStep> Steps { get; } = new();
    }
}
