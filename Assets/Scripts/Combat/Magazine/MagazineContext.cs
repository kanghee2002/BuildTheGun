using BuildTheGun.Combat.Bullets;

namespace BuildTheGun.Combat.Magazine
{
    /// <summary>
    /// 탄창 메타데이터. 슬롯 변경 시 MarkDirty로 재계산.
    /// </summary>
    public class MagazineContext
    {
        private MagazineState _magazine;
        private bool _dirty = true;
        private int _totalActiveSlots;
        private int _fireBulletCount;

        public MagazineContext(MagazineState magazine)
        {
            _magazine = magazine;
        }

        public void Bind(MagazineState magazine)
        {
            _magazine = magazine;
            MarkDirty();
        }

        public int TotalActiveSlots
        {
            get
            {
                EnsureFresh();
                return _totalActiveSlots;
            }
        }

        public int FireBulletCount
        {
            get
            {
                EnsureFresh();
                return _fireBulletCount;
            }
        }

        public void MarkDirty() => _dirty = true;

        private void EnsureFresh()
        {
            if (!_dirty)
                return;
            Rebuild();
            _dirty = false;
        }

        private void Rebuild()
        {
            _totalActiveSlots = 0;
            _fireBulletCount = 0;
            if (_magazine?.Slots == null)
                return;

            foreach (var slot in _magazine.Slots)
            {
                if (!slot.IsActive || slot.Bullet == null)
                    continue;
                _totalActiveSlots++;
                if (slot.Bullet.BaseSpec.BulletType == BulletType.Fire)
                    _fireBulletCount++;
            }
        }

        /// <summary>
        /// 비활성 슬롯은 건너뛴다. fromIndex 이후에 활성 슬롯이 있고, 모두 Fire인지.
        /// </summary>
        public bool AreAllRemainingSlotsFire(int fromIndex)
        {
            if (_magazine?.Slots == null)
                return true;

            for (var i = fromIndex + 1; i < _magazine.Slots.Length; i++)
            {
                var slot = _magazine.Slots[i];
                if (!slot.IsActive || slot.Bullet == null)
                    continue;
                if (slot.Bullet.BaseSpec.BulletType != BulletType.Fire)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// slotIndex 이전의 활성 슬롯이 모두 Buff인지.
        /// </summary>
        public bool AreAllPrecedingSlotsBuff(int slotIndex)
        {
            if (_magazine?.Slots == null)
                return true;

            for (var i = 0; i < slotIndex; i++)
            {
                var slot = _magazine.Slots[i];
                if (!slot.IsActive || slot.Bullet == null)
                    continue;
                if (slot.Bullet.BaseSpec.BulletType != BulletType.Buff)
                    return false;
            }

            return true;
        }

        public bool IsFirstActiveSlot(int slotIndex)
        {
            if (_magazine?.Slots == null)
                return false;

            for (var i = 0; i < slotIndex; i++)
            {
                var slot = _magazine.Slots[i];
                if (slot.IsActive && slot.Bullet != null)
                    return false;
            }

            var s = _magazine.Slots[slotIndex];
            return s.IsActive && s.Bullet != null;
        }

        /// <summary>
        /// 활성 슬롯 중 마지막 인덱스인지.
        /// </summary>
        public bool IsLastActiveSlot(int slotIndex)
        {
            if (_magazine?.Slots == null)
                return false;

            for (var i = slotIndex + 1; i < _magazine.Slots.Length; i++)
            {
                var slot = _magazine.Slots[i];
                if (slot.IsActive && slot.Bullet != null)
                    return false;
            }

            var self = _magazine.Slots[slotIndex];
            return self.IsActive && self.Bullet != null;
        }
    }
}
