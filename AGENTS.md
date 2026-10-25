# AGENTS.md

## 기술 스택 (Tech Stack)

- Engine: Unity 6000.6.4f1
- Language: C# 9.0 (.NET Standard 2.1)
- Render Pipeline: Universal Render Pipeline(URP)
- Input: Input System Package

## 프로젝트 구조 (Project Structure)

```text
├── Docs/
│   ├── GDD.md          # 게임 디자인 및 구현 범위
│   └── ADRs/           # 아키텍처 의사결정 기록
│── Assets/
│   ├── _Project/       # 프로젝트에서 직접 제작, 관리하는 에셋
│   └── Synty/          # 서드파티 에셋
├── Packages/           # Unity 패키지 의존성
└── ProjectSettings/    # Unity 프로젝트 설정
```

### 작업 디렉토리 구조 (`Assets/_Project/`)

```text
Assets/_Project/
├── Scripts/
│   ├── Runtime/            # 게임 런타임 코드 (GodOfWar.Runtime.asmdef)
│   │   └── Core/           # 
│   ├── Editor/             # 에디터 전용 확장 코드 (GodOfWar.Editor.asmdef)
│   └── Tests/              # 자동화 테스트 코드
│       ├── Editor/         # EditMode 테스트 (GodOfWar.Tests.Editor.asmdef)
│       └── Runtime/        # PlayMode 테스트 (GodOfWar.Tests.Runtime.asmdef)
├── Data/                   # 게임 데이터 에셋 (ScriptableObject 등)
├── Prefabs/                # 게임 오브젝트 프리팹
├── Animations/             # 애니메이션 컨트롤러 및 클립
├── Materials/              # 프로젝트 전용 머티리얼
├── Input/                  # Input Actions 에셋
├── Scenes/                 # 게임 씬
└── Settings/               # 렌더 파이프라인 및 프로젝트 전역 설정 에셋
```

## 코딩 스타일 (Coding Style)

단일 런타임 어셈블리(`GodOfWar.Runtime`) 내의 의존성을 관리하고, 네임스페이스 구조를 일관되게 유지하며, Unity 타입과의 이름 충돌(CS0118)을 방지한다.

### 의존성 (Dependencies)

- **Core 독립**: `Core/` 안의 코드는 `GodOfWar.Core.*` 외의 `GodOfWar.*` 네임스페이스를 참조하지 않는다 (ADR-003).

### 네임스페이스 (Namespaces)

| 규칙 | 내용 |
|---|---|
| 네임스페이스 매핑 | asmdef의 `rootNamespace`에 asmdef 기준 상대 폴더 경로를 이어 붙인다. 하위 폴더도 모두 반영한다 |
| 테스트 네임스페이스 | 테스트 폴더 구조는 런타임 폴더 구조를 따른다 (예: `Tests/Editor/Input/` → `GodOfWar.Tests.Input`) |
| 금지 이름 (a) | 네임스페이스 세그먼트에 Unity 주요 타입 이름(`Camera`, `Editor`, `Animation`, `Physics` 등)을 사용하지 않는다 (예: `Camera` 대신 `CameraSystem`). *이유: 네임스페이스 내부에서 `Camera.main`, `: Editor` 등이 CS0118 심볼 충돌 에러를 유발함* |
| 금지 이름 (b) | 타입 이름을 자신이 속한 네임스페이스의 세그먼트와 동일하게 짓지 않는다 (예: `GodOfWar.Character` 네임스페이스 안에 `Character` 클래스 정의 금지) |

#### 어셈블리별 `rootNamespace`

| 어셈블리 | `rootNamespace` |
|---|---|
| `GodOfWar.Runtime` | `GodOfWar` |
| `GodOfWar.Editor` | `GodOfWar.EditorTools` |
| `GodOfWar.Tests.Editor` | `GodOfWar.Tests` |
| `GodOfWar.Tests.Runtime` | `GodOfWar.Tests` |

## 커뮤니케이션(Communication)

- **자연스러운 한국어 구사**: 영어 표현을 기계적으로 직역한 어색한 번역투를 피하고, 한국 개발 실무에서 자연스럽게 읽히는 문장으로 작성한다.
- **도메인 및 엔지니어링 전문 용어 사용**: 모호한 일상어 대신 게임 개발 및 소프트웨어 엔지니어링의 정확한 전문 용어를 사용한다. 한국 실무에서 외래어 그대로 쓰이는 용어(예: 데미지, 스트레이프, 패링, 히트스톱 등)는 억지로 번역하지 않고 한글 음차로 표기하며, 필요시 `한글(English)`을 병기한다.
- **이해하기 쉬운 구조화**: 장황한 나열을 피하고, 읽는 사람이 맥락과 핵심 결론을 빠르게 파악할 수 있도록 두괄식 요약, 표, 목록 등을 활용해 논리적으로 구성한다.