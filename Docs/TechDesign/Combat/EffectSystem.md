# 효과 & 이벤트 시스템 설계

> 효과(Effect)의 데이터/핸들러 구조, 이벤트 버스(CombatEventBus),  
> 카운터 조건(CounterCondition), 투사체(Projectile) 시스템을 정의합니다.

---

## 1. EffectType — 효과 타입 (enum)

모든 효과 타입은 enum으로 정의한다. 새 효과 추가 시 enum에 1줄 + Handler 클래스 1개.

JSON 데이터에서는 문자열로 작성하고, 로드 시 `Enum.TryParse<EffectType>()`로 변환/검증한다.

```csharp
public enum EffectType
{
    // 스탯 수정
    DamageAdd,            // 기본 피해 +N
    DamageIncrease,       // 피해 증가 +N%
    PelletAdd,            // 발사량 +N
    PelletSet,            // 발사량 = N (무작위 발사량 등에 사용)
    DelayReduce,          // 딜레이 감소
    DelayRemove,          // 딜레이 제거 (0으로)
    ReloadTimeReduce,     // 재장전 시간 감소

    // 탄창 조작
    DeleteNextBullet,     // 다음 총알 삭제
    LoopToFirst,          // 첫 총알로 되돌리기

    // 동적 값 스케일링
    GoldScalingDamage,    // 골드 비례 피해
    RandomPellet,         // 무작위 발사량

    // 조건부 효과
    ThresholdDamageBoost, // 문턱 조건 피해 증가 (강화 총알 3개 이상 등)
    HighHPDamageBoost,   // 적 고체력(90% 이상) 시 피해 증가
    ShotCountDamage,      // 탄창 내 발사 수 비례 피해

    // 상태 효과 (Phase 2)
    // ApplyBurn,
    // ApplyElectric,
    // ApplyPoison,
}
```

### enum을 선택한 이유

| 기준 | `string` | `enum` |
|---|---|---|
| 컴파일 타임 안전성 | 없음 | 있음 |
| IDE 자동완성 | 없음 | 있음 |
| 비교 성능 | O(n) | O(1) |
| 새 효과 추가 비용 | string만 추가 | enum 1줄 추가 |

새 효과 추가 시 `IEffectHandler` 클래스를 반드시 작성해야 하므로 재컴파일은 필수. enum 1줄 추가는 추가 비용 0.

---

## 2. IEffectHandler — 효과 핸들러 인터페이스

각 EffectType마다 대응하는 Handler 클래스 1개. Handler Registry에 등록.

```csharp
public interface IEffectHandler
{
    EffectType EffectType { get; }

    /// <summary>
    /// 효과를 실행한다.
    /// </summary>
    void Execute(EffectContext ctx, EffectParams params);

    /// <summary>
    /// 실행 전 조건 검증. false면 스킵.
    /// </summary>
    bool CanExecute(EffectContext ctx, EffectParams params);
}
```

### EffectSystem — 실행 진입점(규약)

- 파이프라인· **외부 코드는 `IEffectHandler`를 직접 호출하지 않는다.** 레지스트리를 거치는 **`EffectSystem.Execute(EffectSpec spec, EffectContext ctx)`**(또는 동등 API) 한 경로만 사용한다.
- **`IEffectHandler.Execute` / `CanExecute`는 `EffectSystem` 구현 내부에서만** 호출된다(핸들러 클래스의 `Execute` 메서드는 인터페이스 구현일 뿐, 외부 진입점이 아니다).
- **`EffectSystem.Execute` 구현은 내부에서** 대응 핸들러를 찾은 뒤 **`CanExecute`가 false면 부수 효과 없이 반환**(no-op)한다. 호출자가 매번 `CanExecute`를 따로 부를 필요는 없다.
- 단위 테스트에서 특정 핸들러만 검증할 때는 예외적으로 핸들러를 직접 호출할 수 있다.

### 새 효과 추가 워크플로우

1. `EffectType` enum에 추가
2. `IEffectHandler` 구현 클래스 작성
3. EffectRegistry에 등록
4. 끝. Pipeline, BuffAccumulator, StatResolver 변경 불필요.

### 동적 값 핸들러 예시

무작위 발사량, 골드 비례 피해 등 "동적 값"은 Handler가 값을 결정한 뒤 확정된 수정자를 push한다.

```csharp
public class RandomPelletHandler : IEffectHandler
{
    public EffectType EffectType => EffectType.RandomPellet;

    public void Execute(EffectContext ctx, EffectParams p)
    {
        int value = Random.Range(p.GetInt("min"), p.GetInt("max") + 1);

        ctx.Accumulator.Push(new PendingBuff
        {
            Target = p.Target,
            EffectType = EffectType.PelletSet,
            Params = new EffectParams { { "value", value } },
        });
    }
}
```

위 `Execute`는 **인터페이스 구현**이며, 런타임에서는 **`EffectSystem`이 `EffectType.RandomPellet`에 대해 이 핸들러의 `Execute`를 호출**한다.

BuffAccumulator와 StatResolver는 항상 **확정된 값**만 받는다.
동적 계산은 전부 Handler 내부에서 해결한다.

---

## 3. EffectContext — 효과 실행 컨텍스트

모든 시스템이 공유하는 "현재 상황" 스냅샷. 위에서 아래로 내려가며 정보가 추가된다.

```csharp
public class EffectContext
{
    // CombatOrchestrator가 채움
    public ICombatState Combat;          // 플레이어 HP, 골드, 진행 상태
    public CombatEventBus EventBus;

    // GunSystem이 채움
    public GunState Gun;

    // BulletResolutionPipeline이 채움
    public MagazineState Magazine;
    public MagazineContext MagazineInfo;
    public BulletState SourceBullet;
    public int SlotIndex;
    public BuffAccumulator Accumulator;
    public List<PendingBuff> AppliedBuffs;  // 이 총알에 적용된 버프

    // 투사체 적중 시 (OnHit 트리거 전용)
    public ITargetable HitTarget;
    public Vector2 HitPoint;
}
```

---

## 4. EffectSpec — 효과 데이터 정의

```csharp
public class EffectSpec
{
    public EffectType EffectType;
    public TriggerType Trigger;        // 발동 타이밍
    public BuffTarget Target;          // 버프 대상 (강화 총알용)
    public EffectParams Params;        // 수치/조건 파라미터

    // 선택: 카운터 기반 활성 조건
    public CounterCondition CounterCondition;  // null = 즉시/표준 트리거
}
```

---

## 5. CombatEventBus — 이벤트 버스

트리거 기반 효과의 발동/구독/전파를 담당하는 Observer 패턴 허브.

```csharp
public class CombatEventBus
{
    public void Subscribe(TriggerType trigger, IEffectListener listener);
    public void Unsubscribe(TriggerType trigger, IEffectListener listener);
    public void Publish(TriggerType trigger, CombatEventData data);
}
```

### TriggerType

```csharp
public enum TriggerType
{
    None,           // 트리거 없음 (즉시/패시브)
    OnLoad,         // 탄창에 장전하는 순간 ("장전" 키워드)
    OnFire,         // 발사 시
    OnHit,          // 적중 시
    OnKill,         // 적 처치 시
    OnReload,       // 재장전 시
    OnMerge,        // 합체 시
    OnMagazineEmpty,// 탄창 소진 시
}
```

### 리스너 등록/해제 타이밍

| 대상 | 등록 시점 | 해제 시점 |
|---|---|---|
| 총알의 트리거 효과 (OnHit 등) | Fire 총알이 Pipeline에서 처리될 때 | 재장전 시 |
| 유물의 상시 효과 | 전투 시작 시 | 전투 종료 시 |
| 카운터 조건 리스너 | 전투 시작 시 | 전투 종료 시 |

---

## 6. CounterCondition — 카운터 기반 조건

"적 2마리 처치할 때마다", "영구: 적 10마리 처치할 때마다" 같은 조건.

```csharp
public class CounterCondition
{
    public CounterType CounterType;  // 무엇을 셀 것인가
    public int Threshold;            // 몇에 도달하면 발동
    public bool IsRepeating;         // true = "매 N마다", false = "N에 도달 시 1회"
    public Lifetime Lifetime;        // PerBattle or Permanent
}

public enum CounterType
{
    EnemyKills,
    HitsDealt,
    ReloadsPerformed,
    DamageTaken,
    BulletsPlayed,
    GoldSpent,
}

public enum Lifetime
{
    PerBattle,   // 전투 종료 시 리셋
    Permanent,   // 런 전체 유지, RunPersistentData에 저장
}
```

### 실행 흐름

```
적 사망
 → EventBus.Publish(OnKill)
 → CombatCounterManager 수신
   → EnemyKills 카운터 +1
   → 등록된 CounterCondition 중 threshold 도달한 것 탐색
     → 도달 시: 연결된 Effect 실행
     → IsRepeating이면 카운터 리셋
```

### 확장

새 카운터 추가:
1. `CounterType` enum에 추가
2. 해당 이벤트 발생 시점에서 `EventBus.Publish()` 추가
3. 끝. CounterCondition과 CombatCounterManager 코드 변경 없음.

---

## 7. 투사체(Projectile) 시스템

### 설계 원칙: Fire-and-Forget

투사체는 발사 시점에 **데이터 스냅샷**을 받아 독립적으로 작동한다.
적중 시 콜백을 통해 Domain 레이어의 `ProjectileHitProcessor`에 처리를 위임한다.

```
FireCommand (Pipeline 출력)
  → GunSystem이 ProjectileData 생성 (스탯 스냅샷)
  → ProjectileBehaviour.Initialize(data, onHitCallback)
  → 투사체 독립 이동
  → 충돌 시 콜백 → ProjectileHitProcessor → EventBus → 리스너들
```

### ProjectileData — 투사체 데이터 스냅샷

```csharp
public struct ProjectileData
{
    // 스탯 (FireCommand에서 복사)
    public float Damage;
    public int PelletIndex;
    public int TotalPellets;

    // 행동 파라미터 (GunSpec/GunBehavior에서)
    public float Speed;
    public float SpreadAngle;
    public bool IsPiercing;
    public int MaxPierceCount;

    // 적중 시 실행할 효과
    public List<EffectSpec> OnHitEffects;

    // 출처 정보 (추적/디버그용)
    public string SourceBulletId;
    public int SourceSlotIndex;
}
```

### ProjectileHitProcessor — 적중 처리 (Domain 레이어)

투사체 MonoBehaviour가 직접 EventBus를 호출하지 않고, Domain 레이어의 Processor에 위임한다.
이를 통해 Presentation → Domain 직접 의존을 방지한다.

```csharp
public class ProjectileHitProcessor
{
    private readonly CombatEventBus _eventBus;
    private readonly EffectSystem _effectSystem;

    /// <summary>
    /// 투사체가 적에게 적중했을 때 호출된다.
    /// 피해 계산, 이벤트 발행, OnHit 효과 실행을 수행한다.
    /// </summary>
    public void ProcessHit(ProjectileData data, ITargetable target, Vector2 hitPoint)
    {
        target.TakeDamage(data.Damage);

        var hitEvent = new HitEventData
        {
            Damage = data.Damage,
            Target = target,
            HitPoint = hitPoint,
            SourceBulletId = data.SourceBulletId,
        };

        _eventBus.Publish(TriggerType.OnHit, hitEvent);

        if (target.IsDead)
            _eventBus.Publish(TriggerType.OnKill, hitEvent);
    }
}
```

### ProjectileBehaviour — 투사체 MonoBehaviour (Presentation 레이어)

이동 + 충돌 감지만 담당. Domain 타입에 직접 의존하지 않는다.

```csharp
public class ProjectileBehaviour : MonoBehaviour
{
    private ProjectileData _data;
    private System.Action<ProjectileData, ITargetable, Vector2> _onHit;

    public void Initialize(ProjectileData data,
                           System.Action<ProjectileData, ITargetable, Vector2> onHit)
    {
        _data = data;
        _onHit = onHit;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<ITargetable>(out var target))
        {
            _onHit?.Invoke(_data, target, transform.position);

            if (!_data.IsPiercing)
                gameObject.SetActive(false); // pool 반환
        }
    }
}
```

---

## 8. 전체 발사 사이클 요약

```
 1. 플레이어 발사 입력
 2. CombatOrchestrator → GunSystem.RequestFire()
 3. GunSystem → IGunBehavior.BuildFireSequence(gun)
 4.   └→ BulletResolutionPipeline 시작
 5.       ├─ Slot 0: [Buff] 피해+10 → BuffAccumulator에 push
 6.       ├─ Slot 1: [Buff] 연발+1  → BuffAccumulator에 push
 7.       └─ Slot 2: [Fire] 기본탄
 8.           ├─ pending 버프 소비 → +10dmg, +1pellet 적용
 9.           ├─ BuffAmplifier 적용
10.           ├─ StatResolver → 최종 스탯 계산
11.           └─ FireCommand 생성 {damage:X, pellets:2}
12. GunSystem → IGunBehavior.ResolveProjectiles(fireCommand)
13.   └→ 투사체 2발 생성 (리볼버: 순차, 샷건: 동시)
14. EventBus.Publish(OnFire, data)
15. 투사체 이동 → 적 적중
16. ProjectileHitProcessor.ProcessHit()
17.   └→ EventBus.Publish(OnHit, data)
18.   └→ 적 사망 시 EventBus.Publish(OnKill, data)
19. 모든 슬롯 처리 완료 → 재장전
20. EventBus.Publish(OnReload, data)
```
