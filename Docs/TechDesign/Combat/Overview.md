# 아키텍처 개요

> 이 문서는 총·총알 시스템의 전체 아키텍처를 정의합니다.  
> 개별 시스템의 상세 설계는 각 문서(`GunSystem.md`, `BulletPipeline.md`, `EffectSystem.md`, `RelicSystem.md`)를 참고합니다.
>
> 용어 기준: 게임 내 “총알”은 시스템/데이터 레벨에서 **Bullet**로 통일한다.

---

## 1. 설계 원칙

- **Spec / State 분리**: 정적 데이터(Spec)와 런타임 상태(State)를 철저히 분리한다. Spec은 절대 런타임에 변경하지 않는다.
- **Open-Closed Principle**: 새 총기/효과 추가 시 기존 코드를 수정하지 않고 새 클래스를 등록한다.
- **단방향 의존**: 모든 시스템 간 호출은 위→아래 방향. 역방향 통신은 CombatEventBus를 경유한다.
- **도메인 / Presentation 분리**: 도메인 로직은 Unity API에 직접 의존하지 않는다. MonoBehaviour는 Presentation 레이어에서만 사용한다.

---

## 2. 시스템 구조

```
CombatOrchestrator (전투 루프 제어, 시스템 간 협업 조율)
 │
 ├── GunSystem ─────────────────→ 상세: GunSystem.md
 │    ├── GunState (GunSpec 기반)
 │    │    └── MagazineState[] ── MagazineSlot[] ── BulletState
 │    └── IGunBehavior (총기별 Strategy)
 │
 ├── BulletResolutionPipeline ──→ 상세: BulletPipeline.md
 │    ├── BuffAccumulator (버프 수집/분배)
 │    ├── SlotConditionEvaluator (슬롯 조건 평가)
 │    └── StatResolver (최종 스탯 계산)
 │
 ├── EffectSystem ──────────────→ 상세: EffectSystem.md
 │    ├── IEffectHandler Registry (효과 핸들러 등록소)
 │    └── ProjectileHitProcessor (투사체 적중 처리)
 │
 ├── RelicEventBridge / 유물 패시브 적용 ──→ 상세: RelicSystem.md
 │
 ├── CombatEventBus (이벤트 전파)
 │
 └── CombatCounterManager (카운터 조건 추적)
```

---

## 3. 의존성 방향

```
CombatOrchestrator
  → GunSystem → BulletResolutionPipeline → EffectSystem → CombatEventBus
                                                              ↑
  CombatCounterManager ───────────────────────────────────────┘
```

- **직접 호출**: 항상 위→아래 (Orchestrator → GunSystem → Pipeline → EffectSystem)
- **간접 통신**: 아래→위는 CombatEventBus의 Observer 패턴으로만 수행
- **순환 참조**: 구조적으로 불가능

### 시스템 간 통신 방식

| 통신 | 방식 |
|---|---|
| Orchestrator → GunSystem | 직접 호출 (`RequestFire()`) |
| GunSystem → Pipeline | 직접 호출 (`ProcessMagazine()`) |
| Pipeline → EffectSystem | 직접 호출 (`Execute()`) |
| EffectSystem → EventBus | 직접 호출 (`Subscribe()`, `Publish()`) |
| EventBus → 리스너 | Observer 패턴 |
| CounterManager → EffectSystem | EventBus 경유 |

---

## 4. Spec (정적 데이터) vs State (런타임 상태)

### Spec — 정의 데이터 (ScriptableObject / JSON)

| Spec | 설명 |
|---|---|
| `GunSpec` | 총기 정의 (탄창 크기, 재장전 시간, 고유 효과 등) |
| `BulletSpec` | 총알 정의 (타입, 기본 스탯, 효과 목록, 딜레이, 키워드 등) |
| `RelicSpec` | 유물 정의 |

### State — 런타임 인스턴스 (전투마다 생성/파기)

| State | 설명 |
|---|---|
| `GunState` | 현재 총기 상태 (BattleModifiers, 재장전 진행 등) |
| `MagazineState` | 탄창 상태 (슬롯 배열, 현재 발사 인덱스) |
| `BulletState` | 총알 인스턴스 (RuntimeModifiers, PermanentValues, 합체 정보) |

---

## 5. 전투 라이프사이클

### 전투 시작 (BattleStart)

```
1. GunState 생성 (GunSpec 기반, Modifier 없는 깨끗한 상태)
2. MagazineState 생성 (현재 덱 구성 기반)
3. BulletState마다:
   ├── RuntimeModifiers = 비어 있음
   └── PermanentValues = RunPersistentData에서 로드 (영구 효과만)
4. CombatEventBus 초기화
5. CombatCounterManager 초기화 (Permanent 카운터만 복원)
6. 유물 (`RelicSystem.md`):
   ├── 패시브 → `GunState.BattleModifiers` 등에 반영
   └── 이벤트형 → `RelicEventBridge`가 EventBus에 구독 등록
```

**핵심**: State를 **새로 생성**하는 것이지, 이전 State에서 버프를 "제거"하는 것이 아니다. 이를 통해 "버프 해제 누락" 버그를 원천 차단한다.

### 재장전 (Reload)

**규칙**: `BuffAccumulator`는 전투 종료까지 유지되며, Reload로 초기화되지 않는다.

```
1. MagazineState.CurrentIndex = 0
2. 비활성화된 슬롯 정리 (삭제된 총알)
3. EventBus.Publish(OnReload)
```

### 전투 종료 (BattleEnd)

```
1. BulletState마다:
   ├── PermanentValues → RunPersistentData에 저장
   └── RuntimeModifiers 폐기
2. GunState.BattleModifiers 폐기
3. CombatCounterManager:
   ├── Permanent 카운터 → RunPersistentData에 저장
   └── PerBattle 카운터 폐기
4. CombatEventBus 전체 구독 해제
5. GunState, MagazineState 폐기
```

---

## 6. 시간 범위별 데이터 수명

| 범위 | 저장 위치 | 초기화 시점 | 예시 |
|---|---|---|---|
| Per-Battle (전투 1회) | BuffAccumulator (소비형+범위형), BulletState.RuntimeModifiers, GunState.BattleModifiers | 전투 종료 시 | 강화 총알 버프 누적, OnReload 누적 피해, 적중 횟수 |
| Per-Run (런 전체) | RunPersistentData | 런 종료 시 | "영구" 키워드 효과 |

---

## 7. 구현 단계

| Phase | 대상 | 목표 |
|---|---|---|
| **1** | Spec 데이터 클래스 + StatResolver | 데이터 정의와 스탯 계산 기반 |
| **2** | Magazine + BulletResolutionPipeline | 총알 순차 처리 + 버프 누적 로직 |
| **3** | EffectSystem (기본 핸들러 5~6개) | DamageAdd, PelletAdd, DelayReduce 등 기본 효과 |
| **4** | GunSystem + StandardGunBehavior | 리볼버 기준 기본 발사 |
| **5** | CombatEventBus + OnHit/OnFire 트리거 | 트리거 기반 효과 작동 |
| **6** | ShotgunBehavior, SniperBehavior 등 | 총기별 분기 행동 |
| **7** | 합체, 조건부 효과, 유물 연동 | 고급 메카닉 |
