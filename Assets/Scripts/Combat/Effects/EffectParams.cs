using System.Collections.Generic;
using UnityEngine;

namespace BuildTheGun.Combat.Effects
{
    /// <summary>
    /// 효과에 붙는 수치·조건 파라미터 저장소. BuffAmplifier 적용 시
    /// <see cref="Set(string,float,bool)"/>에서 지정한 항목만 곱한다.
    /// </summary>
    public class EffectParams
    {
        private readonly Dictionary<string, float> _values = new();

        // BuffAmplifier로 곱하지 않을 키(임계값·횟수·슬롯 등).
        private readonly HashSet<string> _unscaledKeys = new();

        /// <summary>
        /// 키에 값을 넣는다.
        /// </summary>
        /// <param name="key">데이터/JSON과 동일한 키.</param>
        /// <param name="value">저장할 값.</param>
        /// <param name="scaleWithBuffAmplifier">
        /// true면 <see cref="Scale"/> 시 발사 총알의 BuffAmplifier를 곱한다.
        /// false면 임계값·횟수·슬롯 등 증폭하면 안 되는 값이다.
        /// </param>
        public void Set(string key, float value, bool scaleWithBuffAmplifier = true)
        {
            _values[key] = value;
            if (scaleWithBuffAmplifier)
                _unscaledKeys.Remove(key);
            else
                _unscaledKeys.Add(key);
        }

        public float GetFloat(string key, float defaultValue = 0f)
        {
            return _values.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            return _values.TryGetValue(key, out var v) ? Mathf.RoundToInt(v) : defaultValue;
        }

        /// <summary>
        /// 발사 총알에 pending 버프를 적용할 때, BuffAmplifier만큼 수치를 키운 복사본을 만든다.
        /// 증폭 대상이 아닌 키는 값이 그대로 복사된다.
        /// </summary>
        public EffectParams Scale(float amplifier)
        {
            if (Mathf.Approximately(amplifier, 1f))
                return this;

            var scaled = new EffectParams();
            foreach (var kv in _values)
            {
                var key = kv.Key;
                var multiply = !_unscaledKeys.Contains(key);
                scaled._values[key] = multiply ? kv.Value * amplifier : kv.Value;
                if (!multiply)
                    scaled._unscaledKeys.Add(key);
            }

            return scaled;
        }
    }
}
