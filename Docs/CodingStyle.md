## BuildTheGun 코딩 스타일 (예시 초안)

### 1. 일반 원칙
- **언어/엔진**: Unity C# 기준으로 작성합니다.
- **OOP 지향**: 캡슐화, 상속, 다형성을 활용하되, 과도한 상속보다는 조합(Composition)을 우선합니다.
- **성능 고려**: 불필요한 `Update` 사용, `GC` 할당, 박싱/언박싱을 최소화합니다.

### 2. 네이밍 컨벤션
- **클래스/Struct/Enum**: PascalCase (예: `PlayerController`, `WeaponType`)
- **메서드**: PascalCase (예: `Fire()`, `ReloadWeapon()`)
- **필드 (private)**: `_camelCase` (예: `_currentAmmo`, `_rigidbody`)
- **필드 (public/SerializedField)**: PascalCase + `[SerializeField]` 사용 권장  
  - 예: `[SerializeField] private int _maxAmmo;`
- **상수**: `SCREAMING_SNAKE_CASE` (예: `MAX_WEAPON_SLOTS`)

### 3. 스크립트 구조
- **파일명 = 클래스명**을 원칙으로 합니다.
- 한 파일에는 **핵심 public 클래스 하나**만 두는 것을 기본으로 합니다.
- `MonoBehaviour`는 **역할이 명확한 단위**로 분리합니다. (예: 입력 처리, 상태 관리, UI 표시 등)

### 4. Unity 관련 규칙
- `Update` / `FixedUpdate` / `LateUpdate`는 **꼭 필요한 곳에만** 사용합니다.
- 코루틴 사용 시 **중단 조건과 예외 상황**을 명확히 합니다.
- `GetComponent`는 `Awake`/`Start`에서 캐싱하여 반복 호출을 피합니다.

### 5. 주석 및 문서화
- 복잡한 로직이나 의도가 드러나지 않는 부분에만 **의도 중심의 주석**을 작성합니다.
- 공개 API(다른 팀원이 자주 쓰는 클래스/메서드)는 `///` 요약 주석을 사용합니다.

### 6. 로그 사용
- 디버그용 로그는 `Debug.Log`/`LogWarning`/`LogError`를 구분해서 사용합니다.
- 최종 빌드 시 불필요한 로그가 남지 않도록, 필요 시 전용 Logger 래퍼를 통해 토글 가능하게 합니다.
