using System.Collections.Generic;
using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Pipeline
{
    /// <summary>
    /// 한 번의 발사에 필요한 최종 수치와 OnFire 효과 목록. 투사체 스폰은 GunSystem/Behaviour가 이 데이터를 소비한다.
    /// </summary>
    public class FireCommand
    {
        public int SlotIndex;
        public float Damage;
        public int PelletCount;
        public List<EffectSpec> OnFireEffects = new();
    }
}
