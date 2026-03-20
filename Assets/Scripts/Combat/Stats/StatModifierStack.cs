using System.Collections.Generic;

namespace BuildTheGun.Combat.Stats
{
    /// <summary>
    /// 카테고리별 가산 합계와 연발 배율(곱)을 보관한다.
    /// </summary>
    public class StatModifierStack
    {
        private readonly Dictionary<StatCategory, float> _additive = new();
        private readonly Dictionary<StatCategory, List<float>> _multipliers = new();

        public void Add(StatCategory category, float value)
        {
            if (_additive.TryGetValue(category, out var existing))
                _additive[category] = existing + value;
            else
                _additive[category] = value;
        }

        public void Multiply(StatCategory category, float factor)
        {
            if (!_multipliers.TryGetValue(category, out var list))
            {
                list = new List<float>();
                _multipliers[category] = list;
            }

            list.Add(factor);
        }

        public float Sum(StatCategory category)
        {
            return _additive.TryGetValue(category, out var v) ? v : 0f;
        }

        /// <summary>
        /// 곱할 항목이 없으면 1. 여러 개면 전부 곱한다.
        /// </summary>
        public float Product(StatCategory category)
        {
            if (!_multipliers.TryGetValue(category, out var list) || list.Count == 0)
                return 1f;

            float p = 1f;
            for (var i = 0; i < list.Count; i++)
                p *= list[i];
            return p;
        }

        public void Clear()
        {
            _additive.Clear();
            foreach (var list in _multipliers.Values)
                list.Clear();
        }
    }
}
