using BuildTheGun.Combat.Stats;

namespace BuildTheGun.Combat.Guns
{
    public class GunState
    {
        public GunSpec Spec;
        public MagazineState[] Magazines; // 일반: 길이 1
        public IGunBehavior Behavior;
        public StatModifierStack BattleModifiers = new();

        public MagazineState PrimaryMagazine => Magazines != null && Magazines.Length > 0 ? Magazines[0] : null;
    }
}
