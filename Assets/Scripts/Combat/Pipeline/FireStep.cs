namespace BuildTheGun.Combat.Pipeline
{
    /// <summary>
    /// 한 슬롯 처리 단위: 발사 명령(있을 경우)과 그 뒤 슬롯 딜레이.
    /// </summary>
    public class FireStep
    {
        public int SlotIndex;

        /// <summary> 발사가 없는 슬롯(Buff 전용, OnLoad-only 등)이면 null. </summary>
        public FireCommand Fire;

        public float DelayAfterSec;
    }
}
