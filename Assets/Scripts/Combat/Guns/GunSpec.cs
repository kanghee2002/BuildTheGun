using BuildTheGun.Combat.Effects;

namespace BuildTheGun.Combat.Guns
{
    public class GunSpec
    {
        public string Id;
        public GunType GunType;
        public int MagazineSize = 6;
        public float ReloadTimeSec;
        public float SpreadAngle;
        public float BurstDelaySec = 0.1f;
        public bool CanExpandMagazine = true;
        public EffectSpec MagazineExpandReplacement;
        public EffectSpec[] Effects = System.Array.Empty<EffectSpec>();
    }
}
