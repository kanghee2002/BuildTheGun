using System.Collections.Generic;
using BuildTheGun.Combat.Buffs;
using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Combat;
using BuildTheGun.Combat.Guns;
using BuildTheGun.Combat.Magazine;
using UnityEngine;

namespace BuildTheGun.Combat.Effects
{
    public class EffectContext
    {
        public ICombatState Combat;
        public CombatEventBus EventBus;

        public GunState Gun;
        public MagazineState Magazine;
        public MagazineContext MagazineInfo;
        public BulletState SourceBullet;
        public int SlotIndex;
        public BuffAccumulator Accumulator;
        public List<PendingBuff> AppliedBuffs;

        /// <summary> EffectSystem.Execute에서 현재 스펙을 핸들러에 전달하기 위해 설정. </summary>
        public EffectSpec CurrentEffect;

        public ITargetable HitTarget;
        public Vector2 HitPoint;
    }
}
