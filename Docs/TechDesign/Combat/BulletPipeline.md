# 총알(Bullet) & 파이프라인 시스템 설계

> 총알(Bullet)의 데이터 구조, 탄창 순차 처리(Pipeline), 버프 누적(BuffAccumulator),  
> 스탯 계산(StatResolver), 합체(Merge) 시스템을 정의합니다.

---

## 1. BulletSpec — 총알 정의 데이터

| 필드 | 타입 | 설명 |
|---|---|---|
| `Id` | `string` | 유일 식별자 |
| `BulletType` | `BulletType` | `Fire` / `Buff` |
| `BaseDamage` | `float` | 기본 피해량 (Fire 총알) |
| `BaseDelaySec` | `float` | 기본 딜레이 |
| `BasePelletCount` | `int` | 기본 발사량 (= 투사체 수 = 연발 수) |
| `BuffAmplifier` | `float` | 버프 증폭 배율 (기본 1.0, 증폭형 2.0) |
| `Keywords` | `Keyword[]` | `[Permanent, OnLoad, ...]` |
| `Effects` | `EffectSpec[]` | 효과 목록 |
| `MergeRules` | `MergeRuleSpec[]` | 합체 시 1회 적용되는 후처리 규칙 목록 (합산된 기본 스탯에 적용) |
| `Rarity` | `Rarity` | 레어도 |
| `Cost` | `int` | 골드 비용 |

```csharp
public enum BulletType
{
    Fire,
    Buff,
}
```

---

## 2. BulletState — 런타임 총알 인스턴스

```csharp
public class BulletState
{
    public BulletSpec BaseSpec;

    // 런타임 수정값 (전투 중 변동, 전투 종료 시 폐기)
    public StatModifierStack RuntimeModifiers;

    // 영구 키워드 누적값 (런 전체 유지)
    public Dictionary<StatCategory, float> PermanentValues;

    // 합체 정보
    public List<BulletSpec> MergedFrom;        // UI용 원본 총알 목록
    public List<EffectSpec> MergedEffects;   // 합산된 효과 리스트 (합체 시 생성)

    // 합체로 변경된 기본 스탯 (합체 안 했으면 BaseSpec과 동일)
    public float MergedBaseDamage;
    public int MergedBasePelletCount;
    public float MergedBaseDelay;
    public float MergedBuffAmplifier;
}
```

---

## 3. BulletResolutionPipeline — 총알 순차 처리

탄창의 총알을 순서대로 처리하며 버프 누적 → 발사 해석을 수행한다.

### 처리 흐름

```
매 슬롯 처리:
  (0) 비활성 슬롯(IsActive == false) → 건너뜀

  (1) 이 총알에 적용 가능한 pending 버프 소비
      → BulletTypeFilter 기준 매칭
        - FireOnly: 발사 총알만 소비, 강화 총알은 통과
        - Any: 무조건 소비 (딜레이/발사량 관련)
      → 매칭된 버프를 이 총알의 스탯에 적용

  (2) 최종 스탯 계산 (StatResolver)

  (3a) 강화 총알:
      → finalPelletCount만큼 반복하여 자신의 효과를 Accumulator에 push
      → 강화 총알은 피해량 등 “발사 전투 스탯” 버프를 받지 않음
         (BulletTypeFilter가 이를 자연스럽게 처리: 해당 버프는 FireOnly)

  (3b) 발사 총알:
      → FireCommand 생성 (최종 스탯 + 효과 목록)
      → OnFire 트리거 효과 실행
      → GunBehavior에 전달하여 투사체 발사

  (3c) "장전" 키워드만 있는 총알:
      → 효과 처리 없음 (OnLoad 효과는 장전 시 이미 실행됨)

  (4) 딜레이 대기 (모든 총알 타입 동일하게 적용)

  모든 슬롯 처리 완료 → 재장전 페이즈
```

### "장전" 총알의 전투 중 처리

"장전" 키워드 총알은 탄창에 넣는 순간 효과가 실행되므로, 전투 중에는 효과를 다시 실행하지 않는다.
단, **딜레이는 그대로 적용**되어 슬롯을 차지하는 비용이 존재한다.

---

## 4. BuffAccumulator — 버프 누적기

강화 총알의 효과를 모아뒀다가, 각 총알을 처리할 때 Mode 기반으로 매칭·적용하는 단일 리스트 구조.

- **재장전 시**: 아무것도 하지 않음 (모든 버프가 전투 내내 유지)
- **전투 종료 시**: 리스트 전체 폐기

### 버프 타겟 분류

```csharp
public struct BuffTarget
{
    public BuffTargetMode Mode;
    public BulletTypeFilter Filter;
    public int Count;              // NextN일 때 남은 적용 횟수
}

public enum BuffTargetMode
{
    Next,        // 다음 매칭 총알 1개 → 적용 후 제거
    NextN,       // 다음 N개 매칭 총알 → 카운트 감소, 0이면 제거
    Last,        // 마지막 슬롯 총알에만 적용 → 유지
    All,         // 모든 매칭 총알에 적용 → 유지
    Remaining,   // 이 총알 이후 모든 매칭 총알에 적용 → 유지
}

public enum BulletTypeFilter
{
    Any,         // 발사/강화 불문 (딜레이 감소, 발사량 증가 등)
    FireOnly,    // 발사 총알만 (피해 증가 등)
}
```

### 기획 카드의 타겟 매핑

| 총알 | Mode | Filter |
|---|---|---|
| 3-1. 다음 총알 피해량 강화 | `Next` | `FireOnly` |
| 3-2. 다음 총알 연발 +1 | `Next` | `Any` |
| 3-3. 다음 총알 딜레이 제거 | `Next` | `Any` |
| 3-4. 이후 전부 발사 총알이면 연발+1 | `Remaining` | `FireOnly` |
| 3-5. 마지막 총알 연발+1 | `Last` | `Any` |
| 3-8. 다음 N장 딜레이 감소 | `NextN` | `Any` |

### 내부 구조

```csharp
public class BuffAccumulator
{
    private readonly List<PendingBuff> _buffs = new();

    public void Push(PendingBuff buff) => _buffs.Add(buff);

    /// <summary>
    /// 해당 슬롯의 총알에 매칭되는 버프를 수집한다.
    /// 소비형(Next/NextN)은 매칭 시 제거/감소, 범위형은 그대로 유지.
    /// </summary>
    public List<PendingBuff> Collect(int slotIndex, BulletTypeFilter filter, bool isLastSlot)
    {
        var matched = new List<PendingBuff>();

        for (int i = _buffs.Count - 1; i >= 0; i--)
        {
            var buff = _buffs[i];
            if (!MatchesFilter(buff.Target.Filter, filter)) continue;

            bool applies = buff.Target.Mode switch
            {
                BuffTargetMode.Next      => true,
                BuffTargetMode.NextN     => true,
                BuffTargetMode.All       => true,
                BuffTargetMode.Remaining => slotIndex > buff.SourceSlotIndex,
                BuffTargetMode.Last      => isLastSlot,
                _ => false,
            };
            if (!applies) continue;

            matched.Add(buff);

            if (buff.Target.Mode == BuffTargetMode.Next)
            {
                _buffs.RemoveAt(i);
            }
            else if (buff.Target.Mode == BuffTargetMode.NextN)
            {
                buff.RemainingCount--;
                if (buff.RemainingCount <= 0)
                    _buffs.RemoveAt(i);
            }
        }

        return matched;
    }

    public void OnBattleEnd() => _buffs.Clear();
}

public struct PendingBuff
{
    public BuffTarget Target;
    public EffectType EffectType;
    public EffectParams Params;
    public int RemainingCount;     // NextN용
    public int SourceSlotIndex;    // Remaining 기준점 + 디버그/UI용
}
```

### 강화 총알에 버프가 적용되는 경우

강화 총알은 피해량, 피해 증가 등 전투 스탯을 받지 않는다. `FireOnly` 타겟 버프는 강화 총알을 건너뛴다.

강화 총알에 적용되는 버프는 `Any` 필터를 가진 것만:
- **발사량(연발) +1**: 강화 총알의 발동 횟수를 증가시킴 → 효과를 여러 번 push
- **딜레이 감소/제거**: 강화 총알의 처리 딜레이를 줄임

예시: `[Buff A: 다음 총알 연발+1, Any] [Buff B: 피해+10, FireOnly] [Fire: 기본탄]`

```
Slot 0: Buff A → push {PelletAdd(1), Next/Any}
Slot 1: Buff B → pending에서 Next/Any 매칭 → Buff B의 pelletCount 1+1=2
                  → Buff B가 2회 발동 → push {DamageAdd(10)} × 2
Slot 2: Fire  → flush → +10 +10 = +20 damage
```

### BuffAmplifier (강화 효과 증폭형, 2-1)

발사 총알의 `BuffAmplifier` 속성. 기본 1.0, 증폭형은 2.0.
Accumulator가 아니라 **발사 총알에 버프를 적용하는 시점**에서 처리한다.

```csharp
var flushedBuffs = _accumulator.FlushForBullet(slotIndex, context);
float amplifier = fireBullet.BuffAmplifier;

foreach (var buff in flushedBuffs)
{
    var amplifiedParams = buff.Params.Scale(amplifier);
    statResolver.Apply(amplifiedParams, fireBullet.RuntimeModifiers);
}
```

---

## 5. StatResolver — 스탯 계산

### 효과 카테고리 (GDD 8.1 기반)

```csharp
public enum StatCategory
{
    BaseDamage,        // 기본 피해 (flat +N, 동종 가산)
    DamageIncrease,    // 피해 증가 (+30%, 동종 가산)
    PelletCount,       // 발사량/연발 +N (동종 가산)
    PelletMultiplier,  // 연발 ×2 등 배율 (동종 곱; 스택 비면 1)
}
```

### 피해 공식

```
최종 피해 = 기본 피해 × (1 + 피해 증가 합)
```

- 같은 카테고리: **가산(+)**으로 합산
- 다른 카테고리: **승산(×)**으로 곱함
- 연발은 투사체 횟수에 가산 적용

```csharp
public class StatResolver
{
    public FinalStats Resolve(BulletState bullet, List<PendingBuff> buffs,
                              GunState gun, MagazineSlot slot)
    {
        float baseDamage = bullet.MergedBaseDamage
                         + Sum(buffs, StatCategory.BaseDamage)
                         + gun.BattleModifiers.Sum(StatCategory.BaseDamage)
                         + bullet.PermanentValues.GetOrDefault(StatCategory.BaseDamage)
                         + slot.SlotModifiers.Sum(StatCategory.BaseDamage);

        float damageIncrease = Sum(buffs, StatCategory.DamageIncrease);

        int pelletAdditive = bullet.MergedBasePelletCount
                           + Sum(buffs, StatCategory.PelletCount)
                           + bullet.RuntimeModifiers.Sum(StatCategory.PelletCount)
                           + gun.BattleModifiers.Sum(StatCategory.PelletCount)
                           + slot.SlotModifiers.Sum(StatCategory.PelletCount);

        float pelletMult = bullet.RuntimeModifiers.Product(StatCategory.PelletMultiplier)
                         * gun.BattleModifiers.Product(StatCategory.PelletMultiplier)
                         * slot.SlotModifiers.Product(StatCategory.PelletMultiplier);

        float finalDamage = baseDamage * (1f + damageIncrease);

        int pelletCount = Mathf.Max(1, Mathf.FloorToInt(pelletAdditive * pelletMult));

        return new FinalStats
        {
            Damage = finalDamage,
            PelletCount = pelletCount,
        };
    }
}
```

### 스탯 합산 순서 (BaseDamage 기준)

```
BulletSpec.BaseDamage (또는 합체 시 MergedBaseDamage)
  + PermanentValues (영구 누적)
  + GunState.BattleModifiers (전투 내 OnReload 누적)
  + BuffAccumulator 결과 (탄창 사이클 내 버프)
  + SlotModifiers (슬롯 강화)
  = 카테고리 합계

카테고리 간 승산: BaseDamage × (1 + DamageIncrease)
= 최종 피해

연발: PelletCount 가산 합 → PelletMultiplier Product → FloorToInt(버림) → 최종 PelletCount
```

### 딜레이 공식

```
발사 총알 총 딜레이 = (pelletCount - 1) × 0.1초 + 총알 기본 딜레이
```

강화 총알은 투사체가 없으므로 기본 딜레이만 적용한다.

---

## 6. 합체(Merge) 시스템

### 규칙

- **발사 총알끼리만** 합체 가능 (강화 총알은 합체 불가)
- 합체 결과는 **평범한 BulletState 하나**. Pipeline은 합체를 인지하지 않음
- 한 번 발사, 합체된 모든 총알의 효과가 하나의 발사에 적용
- 딜레이는 합산
- 추가 조정은 `mergeRule`로 처리한다. (합체 여부를 Pipeline/StatResolver에서 분기하지 않기 위함)
- `mergeRule`은 "합산된 기본 스탯"에 적용되는 후처리 규칙이다. (예: 합체할 때마다 기본 피해 +N, 합체 시 딜레이 -X 등)
- 적용 시점: `BulletMergeResolver.Merge()`에서 스탯/효과 합산이 끝난 직후 1회 적용한다.
- 별도의 `OnMerge` 이벤트 브로드캐스트는 두지 않는다. 합체 관련 로직은 merge 단계에서 완결한다.
- 모든 `mergeRule`은 **합체 1회당 1번만** 적용된다. 합체 누적 횟수 같은 런타임 상태(`MergeCount`)는 두지 않는다.

### MergeRuleSpec (데이터)

`mergeRule`은 합체 결과(`merged`)의 **합산된 기본 스탯(`MergedBase*`)**에만 적용되는 후처리 규칙이다.  
규칙 적용은 `BulletMergeResolver.Merge()` 내부에서 **합산 직후 1회** 수행되며, 합체 누적 상태는 사용하지 않는다.

```csharp
public class MergeRuleSpec
{
    public MergeRuleType RuleType;
    public EffectParams Params;     // 예: { "amount": 2 }, { "amountSec": -0.1 }
}

public enum MergeRuleType
{
    BaseDamageAdd,   // 합체 1회당 기본 피해 +N
    BaseDelayAdd,    // 합체 1회당 기본 딜레이 +N초 (음수면 감소)
    BasePelletAdd,   // 합체 1회당 기본 발사량 +N
}
```

### BulletMergeResolver

```csharp
public static class BulletMergeResolver
{
    public static BulletState Merge(BulletState primary, BulletState secondary)
    {
        Debug.Assert(primary.BulletType == BulletType.Fire
                  && secondary.BulletType == BulletType.Fire);

        var merged = new BulletState
        {
            BulletType = BulletType.Fire,

            // 스탯 합산
            MergedBaseDamage = primary.MergedBaseDamage + secondary.MergedBaseDamage,
            MergedBasePelletCount = primary.MergedBasePelletCount + secondary.MergedBasePelletCount,
            MergedBaseDelay = primary.MergedBaseDelay + secondary.MergedBaseDelay,
            MergedBuffAmplifier = Math.Max(primary.MergedBuffAmplifier, secondary.MergedBuffAmplifier),

            MergedFrom = CollectOriginals(primary, secondary),
        };

        // 효과 통합 (순서 유지: primary → secondary)
        merged.MergedEffects = new List<EffectSpec>();
        foreach (var effect in primary.AllEffects.Concat(secondary.AllEffects))
        {
            if (effect.Trigger == TriggerType.OnMerge)
                ExecuteMergeTimeEffect(effect, merged); // → EffectSystem.Execute (EffectSystem.md §2)
            else
                merged.MergedEffects.Add(effect);
        }

        return merged;
    }
}
```

### 합체 예시

```
Fire1: damage=10, pellet=1, delay=0.3, effects=[OnHit: 영구 피해+1]
Fire2: damage=8,  pellet=2, delay=0.2, effects=[OnFire: 다음 총알 삭제]

Merged: damage=18, pellet=3, delay=0.5
        effects=[OnHit: 영구 피해+1, OnFire: 다음 총알 삭제]
```

한 번 발사 → 투사체 3발, 적중 시 영구 피해+1, 발사 시 다음 총알 삭제.

### OnMerge 트리거

합체 시점에만 실행되는 특수 효과 (예: "합체 총알 딜레이 제거", "합체 시 특수 효과").
`BulletMergeResolver.Merge()` 내부에서 **`EffectSystem.Execute`**로 처리한다 (`EffectSystem.md` §2 규약). 
