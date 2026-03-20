using System.Collections.Generic;
using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Buffs
{
    /// <summary>
    /// 강화 총알이 넣은 <see cref="PendingBuff"/>를 보관하고, 슬롯 처리 시 모드(Next, Last 등)에 맞게 꺼내 준다.
    /// 재장전으로 비우지 않으며, 전투 종료 시 <see cref="OnBattleEnd"/>로 폐기한다 (BulletPipeline §4).
    /// </summary>
    public class BuffAccumulator
    {
        private readonly List<PendingBuff> _buffs = new();

        /// <summary>대기 중인 버프 목록(디버그·테스트용).</summary>
        public IReadOnlyList<PendingBuff> Buffs => _buffs;

        /// <summary>강화 총알 처리 시 호출하여 버프를 큐에 넣는다.</summary>
        public void Push(PendingBuff buff) => _buffs.Add(buff);

        /// <summary>
        /// 현재 슬롯 인덱스·총알 종류(Fire/Buff)·마지막 활성 슬롯 여부에 맞는 버프만 꺼낸다.
        /// <see cref="BuffTargetMode.Next"/>는 매칭 시 제거, <see cref="BuffTargetMode.NextN"/>은 횟수만큼 소비된다.
        /// FireOnly 대상은 발사 총알 슬롯에서만 매칭된다.
        /// </summary>
        public List<PendingBuff> Collect(int slotIndex, bool isFireBullet, bool isLastActiveSlot)
        {
            var matched = new List<PendingBuff>();

            for (var i = _buffs.Count - 1; i >= 0; i--)
            {
                var buff = _buffs[i];
                if (!MatchesBulletFilter(buff.Target.Filter, isFireBullet))
                    continue;

                var applies = buff.Target.Mode switch
                {
                    BuffTargetMode.Next => true,
                    BuffTargetMode.NextN => true,
                    BuffTargetMode.All => true,
                    BuffTargetMode.Remaining => slotIndex > buff.SourceSlotIndex,
                    BuffTargetMode.Last => isLastActiveSlot,
                    _ => false,
                };
                if (!applies)
                    continue;

                matched.Add(buff);

                if (buff.Target.Mode == BuffTargetMode.Next)
                {
                    _buffs.RemoveAt(i);
                }
                else if (buff.Target.Mode == BuffTargetMode.NextN)
                {
                    var pb = _buffs[i];
                    pb.RemainingCount--;
                    _buffs[i] = pb;
                    if (pb.RemainingCount <= 0)
                        _buffs.RemoveAt(i);
                }
            }

            return matched;
        }

        /// <summary>
        /// <see cref="Collect"/>와 동일하다. 발사 총알 한 발에 대기 버프를 확정해 StatResolver에 넘기기 전에 호출하는 용도로 이름만 구분한다.
        /// </summary>
        public List<PendingBuff> FlushForBullet(int slotIndex, bool isFireBullet, bool isLastActiveSlot) =>
            Collect(slotIndex, isFireBullet, isLastActiveSlot);

        /// <summary>전투 종료 시 버프 큐를 비운다.</summary>
        public void OnBattleEnd() => _buffs.Clear();

        private static bool MatchesBulletFilter(BulletTypeFilter filter, bool isFireBullet)
        {
            return filter == BulletTypeFilter.Any || isFireBullet;
        }
    }
}
