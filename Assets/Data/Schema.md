# 데이터 스키마(초안) - Bullets / Guns / Relics

이 문서는 게임 콘텐츠(총알/총기/유물)를 **데이터로 정의할 때의 최소 규칙**을 정리합니다. 목표는 “나중에 검증기/시뮬레이터를 붙일 수 있을 정도로” 필드를 일관되게 만드는 것입니다.

---

## 1) 공통 용어
- **Spec**: 작성(저장)용 정의 데이터(예: `BulletSpec`, `RelicSpec`)
- **Effect**: 실제 동작 단위(예: `OnHit 추가 피해`, `OnReload 딜레이 감소`)
- **Trigger**: 발동 타이밍(예: `OnHit`, `OnFire`, `OnReload`)
- **Keyword**: 효과를 구분하는 태그/키워드(예: `Electric`, `CC`, `Reload`, `Fire`)
- **Constraint**: 밸런스/안전 제약(예: 무한 루프 방지, 상한/하한)

---

## 2) 최소 스펙 필드(권장 고정 형태)
모든 콘텐츠 데이터는 아래와 같은 “공통 뼈대”를 가집니다.

- `id`: 문자열(유일 키) 예: `bullet.fire_dual_stack_v1`
- `name`: 표시명
- `type`: 종류 예: `Bullet` / `Gun` / `Relic`
- `rarity`(선택): 레어도
- `tags`: 키워드 목록(예: `[ "Fire", "Electric" ]`)
- `effects`: Effect 목록

### BulletSpec 추가 필드(합체)
- `mergeRules`(선택): 합체 시점에 **1회** 적용되는 후처리 규칙 목록. (합산된 기본 스탯에 적용, 누적 합체 횟수 같은 별도 런타임 상태는 두지 않음)

### MergeRule 최소 필드(권장)
- `ruleType`: 규칙 식별자 예: `BaseDamageAdd`, `BaseDelayAdd`, `BasePelletAdd`
- `params`: 수치 파라미터(예: `{ "amount": 2 }`, `{ "amountSec": -0.1 }`)

### Effect 최소 필드(권장)
- `effectType`: 효과 식별자 예: `DamageAdd`, `DelayReduce`, `StackOnHit`, `ApplyCC`
- `trigger`: 발동 타이밍 예: `OnHit` / `OnReload`
- `target`: 대상 규칙 예: `NextBullet` / `Self` / `NearbyEnemies`
- `params`: 수치/조건 파라미터(예: `{ "amount": 0.2, "durationSec": 3 }`)
- `constraints`(선택): 이 effect의 안전/밸런스 상한/금지조건

---

## 3) 제약(Validation 기준 - 최소 체크)
나중에 검증기(Validator)를 붙일 때 확인할 “최소 금지/필수”를 여기 적어둡니다.

- 필수 필드 누락 금지: `id`, `type`, `effects`는 항상 존재
- `trigger` 값은 사전 정의된 목록 중 하나여야 함
- `effectType`은 사전 정의된 목록과 매핑되어야 함
- `params`는 `effectType`에 맞는 스키마로 채워져야 함(누락/타입 불일치 금지)
- 밸런스 안전장치:
  - 무한 루프 가능성 높은 조합 후보(예: “되돌리기/리셋/재발동” 계열)에는 상한/가드가 있어야 함
  - CC/스택 계열은 최소한의 상한 또는 종료 조건이 있어야 함

---

## 4) 버전 관리 규칙(간단)
- 데이터에 `schemaVersion`을 두고(예: `1`), 스펙 변경 시 버전을 올립니다.
- 버전이 바뀌면 Validator도 함께 업데이트합니다.

---

## 5) 다음에 붙일 도구(목표)
- `Validator`: 스키마/제약 위반 자동 탐지
- `Runner`: 특정 덱/빌드로 고정 시나리오를 반복 실행
- `Report`: Runner 로그를 요약해 “이상 조합/원인 후보/다음 실험”을 문서로 저장

