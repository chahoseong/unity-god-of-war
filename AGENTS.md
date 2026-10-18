# AGENTS.md

## Tech Stack

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

## 커뮤니케이션(Communication)

- **자연스러운 한국어 구사**: 영어 표현을 기계적으로 직역한 어색한 번역투를 피하고, 한국 개발 실무에서 자연스럽게 읽히는 문장으로 작성한다.
- **도메인 및 엔지니어링 전문 용어 사용**: 모호한 일상어 대신 게임 개발 및 소프트웨어 엔지니어링의 정확한 전문 용어를 사용한다. 한국 실무에서 외래어 그대로 쓰이는 용어(예: 데미지, 스트레이프, 패링, 히트스톱 등)는 억지로 번역하지 않고 한글 음차로 표기하며, 필요시 `한글(English)`을 병기한다.
- **이해하기 쉬운 구조화**: 장황한 나열을 피하고, 읽는 사람이 맥락과 핵심 결론을 빠르게 파악할 수 있도록 두괄식 요약, 표, 목록 등을 활용해 논리적으로 구성한다.