# GunSystem 설계

> 총기(Gun), 탄창(Magazine), 슬롯(Slot)의 데이터 구조와 총기별 행동 분기를 정의합니다.

---

## 1. GunSpec — 총기 정의 데이터

| 필드 | 타입 | 설명 |
|---|---|---|
| `Id` | `string` | 유일 식별자 (`gun.revolver`, `gun.shotgun` 등) |
| `GunType` | `GunType` enum | 총기 타입 → IGunBehavior 매핑 |
| `MagazineSize` | `int` | 기본 탄창 크기 (기본 6) |
| `ReloadTimeSec` | `float` | 기본 재장전 시간 |
| `SpreadAngle` | `float` | 탄퍼짐 각도 (해당 시) |
| `BurstDelaySec` | `float` | 투사체 간 발사 간격 (기본 0.1초) |
| `CanExpandMagazine` | `bool` | 탄창 확장 가능 여부 (더블 배럴 = false) |
| `MagazineExpandReplacement` | `EffectSpec?` | 확장 불가 시 대체 효과 (발사량+1 등) |
| `Effects` | `EffectSpec[]` | 총기 고유 패시브 효과 |

```csharp
public enum GunType
{
    Revolver,
    Shotgun,
    SniperRifle,
    BoltAction,
    AutoPistol,
    DualPistols,
    DoubleBarrel,
    DMR,
}
```

---

## 2. GunState — 런타임 총기 상태

```csharp
public class GunState
{
    public GunSpec Spec;
    public MagazineState[] Magazines;    // 일반: [1개], 쌍권총: [좌, 우]
    public IGunBehavior Behavior;
    public StatModifierStack BattleModifiers;  // 전투 내 누적 수정자 (OnReload 등)

    public MagazineState PrimaryMagazine => Magazines[0];
}
```

### BattleModifiers

OnReload 트리거로 누적되는 전투 내 수정자 (예: "재장전할 때마다 기본 피해 +N", "재장전할 때마다 재장전 시간 -0.2초").
- BuffAccumulator와 별개 (BuffAccumulator는 탄창 사이클 단위)
- 전투 종료 시 폐기

---

## 3. IGunBehavior — 총기별 행동 Strategy

총기 종류마다 발사 방식이 근본적으로 다르므로 Strategy Pattern으로 분리한다.

```csharp
public interface IGunBehavior
{
    /// <summary>
    /// 탄창에서 총알을 꺼내 발사 시퀀스를 생성한다.
    /// </summary>
    FireSequence BuildFireSequence(GunState gun);

    /// <summary>
    /// 발사량(pelletCount)을 실제 투사체 배치(위치, 각도)로 변환한다.
    /// </summary>
    ProjectileSpawnData[] ResolveProjectiles(int pelletCount, GunState gun);

    /// <summary>
    /// 재장전 요청을 생성한다. (쌍권총은 좌/우 독립)
    /// </summary>
    ReloadRequest BuildReloadRequest(GunState gun);
}
```

### 총기별 Behavior 구현

| 총기 | Behavior | 핵심 차이점 |
|---|---|---|
| 리볼버 | `StandardGunBehavior` | 기본 순차 발사 (0.1초 간격) |
| 샷건 | `ShotgunBehavior` | 발사량만큼 동시 발사, 고정 탄퍼짐(20°) |
| 저격총 | `SniperBehavior` | 단발 고위력, 긴 재장전 |
| 볼트액션 | `BoltActionBehavior` | 발사 간 볼트 조작 딜레이 |
| 자동권총 | `AutoPistolBehavior` | 짧은 발사 간격, 높은 연사력 |
| 쌍권총 | `DualPistolsBehavior` | 이중 탄창 독립 관리, 좌/우 교대 발사 |
| 더블 배럴 | `DoubleBarrelBehavior` | 2슬롯 고정, 시작 발사량 2, 탄창 확장 불가 |
| DMR | `DMRBehavior` | 반자동, 저격총과 볼트액션 사이 포지션 |

### 새 총기 추가 워크플로우

1. `GunType` enum에 추가
2. `IGunBehavior` 구현 클래스 작성
3. GunType → Behavior 매핑에 등록
4. 끝. 다른 시스템 코드 변경 불필요.

---

## 4. MagazineState — 탄창 상태

```csharp
public class MagazineState
{
    public MagazineSlot[] Slots;
    public int CurrentIndex;        // 현재 발사 진행 위치
}
```

### MagazineSlot — 탄창 슬롯

```csharp
public class MagazineSlot
{
    public BulletState Bullet;
    public bool IsActive;                       // false = 삭제/비활성
    public StatModifierStack SlotModifiers;     // 슬롯 강화 (약실 강화)
}
```

- `IsActive`: 전투 중 총알 삭제 시 배열 자체를 변경하지 않고 플래그로 처리. 순회 중 인덱스 안정성 보장.
- `SlotModifiers`: 총기 업그레이드("특정 약실 딜레이 감소")에서 부여. 어떤 총알을 넣든 슬롯 보너스 적용.

---

## 5. MagazineContext — 탄창 메타데이터 캐싱

매 재장전 시 한 번 계산하고, 탄창 변경(총알 삭제) 시 갱신한다.

```csharp
public class MagazineContext
{
    private MagazineState _magazine;

    public int TotalActiveSlots { get; }
    public int FireBulletCount { get; }
    public int BuffBulletCount { get; }

    /// <summary>
    /// 특정 인덱스 이후 활성 슬롯이 모두 발사 총알인지 (3-4 조건용).
    /// 비활성 슬롯은 제외하고 평가한다.
    /// </summary>
    public bool AreAllRemainingSlotsFire(int fromIndex);

    /// <summary>
    /// 특정 인덱스 앞 활성 슬롯이 모두 강화 총알인지 (2-5 조건용).
    /// </summary>
    public bool AreAllPrecedingSlotsBuff(int slotIndex);

    public bool IsFirstSlot(int slotIndex);
    public bool IsLastSlot(int slotIndex);

    public void MarkDirty();  // 총알 삭제 등으로 무효화
}
```

---

## 6. MagazineEditor — 탄창 편집 (전투 밖)

전투 중 탄창 조정은 불가능하다. 편집은 전투 밖(상점, 빌드 화면)에서만 수행한다.

```csharp
public class MagazineEditor
{
    public void LoadBullet(MagazineState magazine, int slotIndex, BulletState bullet);
    public BulletState UnloadBullet(MagazineState magazine, int slotIndex);
    public void SwapSlots(MagazineState magazine, int slotA, int slotB);
}
```

### "장전" 키워드 처리

`LoadBullet()` 시점에 총알의 `OnLoad` 트리거 효과를 실행한다.

```csharp
public void LoadBullet(MagazineState magazine, int slotIndex, BulletState bullet)
{
    magazine.Slots[slotIndex].Bullet = bullet;

    foreach (var effect in bullet.BaseSpec.Effects)
    {
        if (effect.Trigger == TriggerType.OnLoad)
            _effectSystem.Execute(effect, BuildLoadContext(magazine, slotIndex));
    }
}
```

"장전" 효과는 장전 시 1회 실행되어 BulletState에 영구 반영된다. 전투 중 Pipeline에서는 해당 효과를 다시 실행하지 않는다.

---

## 7. 쌍권총 지원

`GunState.Magazines`를 배열로 설계하여 구조 변경 없이 지원한다.

- 일반 총기: `Magazines.Length == 1`, `PrimaryMagazine`으로 접근
- 쌍권총: `Magazines.Length == 2`, `DualPistolsBehavior`가 양쪽 탄창 관리

### 영향 범위

| 시스템 | 변경 필요 | 이유 |
|---|---|---|
| BulletResolutionPipeline | 없음 | 하나의 Magazine만 처리. 호출 횟수가 2번이 될 뿐 |
| BuffAccumulator | 없음 | Pipeline과 1:1 |
| EffectSystem | 없음 | EffectContext에 어떤 Magazine인지만 담기면 됨 |
| CombatEventBus | 없음 | 이벤트가 2배 발생할 뿐 |
| GunState | 배열화 | `MagazineState` → `MagazineState[]` |
| StandardGunBehavior | 최소 수정 | `gun.PrimaryMagazine` 사용, 동작 불변 |

---

## 8. 대쉬 취소 처리

GDD: "발사 딜레이 또는 재장전 도중 대쉬 시 해당 동작이 즉시 취소되며, 이후 처음부터 수행해야 함"

Pipeline은 **동기적 계획(FireSequence)**을 생성하고, GunSystem은 이를 **시간에 따라 비동기 실행**한다. 대쉬 취소는 실행 레이어에서 처리한다.

```
BulletResolutionPipeline.ProcessMagazine() → FireSequence (동기적 계획)
GunSystem.ExecuteSequence(sequence)       → 코루틴/상태머신 (비동기, 중단 가능)
```

대쉬 입력 시:
- 현재 발사 중: 해당 총알의 딜레이 취소, 다음 입력 시 현재 총알부터 재시도
- 재장전 중: 재장전 타이머 리셋, 다음 입력 시 재장전 처음부터
