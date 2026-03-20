using BuildTheGun.Combat.Bullets;
using BuildTheGun.Combat.Stats;

namespace BuildTheGun.Combat.Magazine
{
    public class MagazineSlot
    {
        public BulletState Bullet;
        public bool IsActive = true;
        public StatModifierStack SlotModifiers = new();
    }
}
