# 유물(Relic) 시스템 설계

> 장착 유물의 **패시브(상시 수정)**와 **이벤트형(OnKill 등)**을 어떻게 전투에 붙일지 정의한다.  
> 효과의 **데이터·실행 규칙**은 `EffectSystem.md`를 따르고, 본 문서는 **유물 전용 배선**에 집중한다.

---

## 1. 역할 분담

| 구성 요소 | 책임 |
|-----------|------|
| **장착 유물 소스** | 런/플레이어 빌드가 “지금 어떤 유물이 장착됐는지” 단일 소스로 제공 (`RelicSystem`, `RunState` 등 이름은 구현에서 결정). |
| **CombatOrchestrator** | `BattleStart` / `BattleEnd`에서 유물 관련 처리 **순서**를 조율한다. |
| **패시브 유물** | 버스 없이 **전투 시작 시 수정자 스택**에 반영. `StatResolver`·딜레이 계산이 이미 읽는 `GunState.BattleModifiers`, `MagazineSlot.SlotModifiers` 등에 합류 (`BulletPipeline.md` §5). |
| **이벤트형 유물** | `BattleStart`에 `CombatEventBus` **구독** 등록. 전투 중 **도메인 코드**가 `Publish`할 때 반응 → `EffectSystem`으로 실제 효과 실행. |
| **EffectSystem** | `EffectSpec` + `IEffectHandler`. 유물도 총알과 동일하게 **핸들러·스펙 확장**으로 추가하는 것을 원칙으로 한다. |

---

## 2. 패시브형 유물

**예**: 발사 카드 딜레이 40% 감소, 탄창 +1, 모든 발사 연발 ×2 + 피해 절반 (`DesignDetails/Relics.md`).

- **트리거**: 없음. `CombatEventBus`를 쓰지 않는다.
- **적용 시점**: `BattleStart` 안에서, 탄창·파이프라인 가동 **이전**(또는 스탯이 처음 해석되기 전)에 반영하는 것이 안전하다.
- **적용 방식**: 장착 유물 목록을 순회하며, 스펙에 정의된 대로 `GunState.BattleModifiers` 등에 **가산/승산 항목**을 넣는다. 대상 필터(발사만·강화만)는 스펙의 태그/타겟 규칙으로 처리한다.
- **해제**: `BattleEnd` 시 `GunState.BattleModifiers` 폐기와 함께 사라진다 (`Overview.md` §5).

---

## 3. 이벤트형 유물

**예**: 적 처치 시 보상, 재장전 시 이동속도 증가, 피격 N회마다 기절 등.

### 3.1 구독

- **시점**: `BattleStart`, `CombatEventBus` 초기화 직후.
- **주체**: 유물마다 `IEffectListener`를 두지 않고, **`RelicEventBridge`(가칭)** 한 계층이 구독을 담당하는 것을 권장한다.
- **패턴**: 트리거(`TriggerType`) **종류 수만큼만** 구독(또는 멀티플렉스 리스너 1개 + 내부 분기). `Publish` 시 **장착 유물 중 해당 트리거를 쓰는 스펙만** 골라 `EffectSystem.Execute`를 호출한다.  
  → 유물 개수가 늘어도 구독 수는 **O(트리거 수)**에 가깝게 유지된다.

### 3.2 발행(Publish)

- **주체**: 이벤트를 **일으키는 도메인** — 예: `ProjectileHitProcessor`(적중/처치), 재장전 페이즈(`OnReload`), 총알 파이프라인(`OnFire` 등, 설계에 따름).
- 버스는 **Observer 허브**일 뿐, 유물 로직을 알지 않는다.

### 3.3 실행

- 리스너(브리지)는 `CombatEventData`에서 필요한 정보를 꺼내 **`EffectContext`**를 채우고 `EffectSystem.Execute(effectSpec, ctx)`만 호출한다.
- 조건 검사는 **`IEffectHandler.CanExecute`** 또는 핸들러 내부에서 수행한다. 탄창·슬롯 조건은 `ctx.MagazineInfo`, `ctx.SlotIndex` 등을 사용한다 (`EffectSystem.md` §2~3).

### 3.4 상태가 무거운 유물

대부분은 스펙 + 핸들러로 충분하다. **적별 히트 수·스택**처럼 전투 중 인스턴스 상태가 필요하면, 유물 런타임 객체 + 소형 리스너/상태 홀더를 두는 **보조 패턴**을 허용한다. 실행 경로는 여전히 `EffectSystem`으로 수렴시킨다.

---

## 4. `IEffectListener`와 유물

- 유물 **데이터(Spec)** 가 `IEffectListener`를 구현할 필요는 없다.
- **이벤트형**은 원칙적으로 **`RelicEventBridge`가 `IEffectListener`를 구현**(또는 트리거당 1개)하고, 장착 목록을 위임 실행한다.
- `CombatEventBus`의 `Subscribe` / `BattleEnd` 시 `Clear` 또는 `Unsubscribe`는 **오케스트레이터 + 브리지**가 책임진다. 유물이 직접 버스에 접근하지 않아도 된다.

---

## 5. `BattleStart` / `BattleEnd`에서의 순서(권장)

**시작**

```
1. GunState / MagazineState 등 기존 Overview 순서
2. CombatEventBus 초기화
3. 유물 패시브 → GunState.BattleModifiers(등) 반영
4. RelicEventBridge → 이벤트형 유물용 Subscribe
5. (이후) 카운터·총알 파이프라인 등
```

**종료**

- `Overview.md`대로 버스 구독 해제·상태 폐기. 이벤트형 유물 전용 리스너도 함께 정리한다.

---

## 6. 관련 문서

- `EffectSystem.md` — `CombatEventBus`, `EffectContext`, `IEffectHandler`, `TriggerType`
- `Overview.md` — 전투 라이프사이클, 의존 방향
- `BulletPipeline.md` — `StatResolver`, 수정자 스택
- `DesignDetails/Relics.md` — 기획 예시 목록

---

## 7. 코드 스니펫 (참고용 의사코드)

### 7.1 BattleStart에서 패시브 + 구독

```csharp
void OnBattleStart(CombatSession session)
{
    session.EventBus.Clear();
    session.GunState.BattleModifiers.Clear(); // 깨끗한 GunState 전제

    foreach (var relic in session.Run.GetEquippedRelics())
    {
        if (relic.Spec.IsPassive) // 스키마에 맞는 플래그(또는 트리거 없음)
            ApplyPassiveToBattleModifiers(session.GunState, relic.Spec);
    }

    session.RelicBridge.Attach(session.EventBus, session.Run, session.EffectSystem);
}
```

### 7.2 이벤트 브리지(트리거당 1 구독 예시)

```csharp
public sealed class RelicEventBridge : IEffectListener
{
    public void Attach(CombatEventBus bus, IRunState run, EffectSystem effects)
    {
        _run = run;
        _effects = effects;
        bus.Subscribe(TriggerType.OnKill, this);
        // 필요한 TriggerType만 추가
    }

    public void OnEvent(TriggerType trigger, CombatEventData data)
    {
        foreach (var relic in _run.GetEquippedRelics())
        {
            foreach (var spec in relic.Spec.Events.Where(e => e.Trigger == trigger))
            {
                var ctx = BuildEffectContext(data); // Gun, MagazineInfo, Combat 등 채움
                _effects.Execute(spec.Effect, ctx); // 내부에서 CanExecute → Execute
            }
        }
    }
}
```

실제 프로젝트에서는 `EffectSystem` API, `RelicSpec` 스키마, `BuildEffectContext` 위치를 코드베이스에 맞게 조정한다.
