using System.Collections.Generic;

namespace BuildTheGun.Combat.Stats
{
    public static class PermanentValuesExtensions
    {
        public static float GetOrDefault(this Dictionary<StatCategory, float> values, StatCategory category)
        {
            return values != null && values.TryGetValue(category, out var v) ? v : 0f;
        }
    }
}
