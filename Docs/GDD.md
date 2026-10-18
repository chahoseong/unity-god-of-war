# ⚔️ GodOfWar 전투 포트폴리오 — 게임 디자인 문서 (GDD)

## 1. 프로젝트 비전 (Project Vision)
『갓 오브 워(God of War, 2018)』의 전투 감각을 레퍼런스로 삼아, 캐릭터 등 뒤에 밀착된 카메라 시점에서 묵직한 검술 공방(타격, 후딜레이 캔슬, 회피, 저스트 패링)의 핵심 재미를 구현하는 3인칭 근접 액션 버티컬 슬라이스(Vertical Slice).

## 2. 핵심 디자인 방향 (Core Design Pillars)
1. **밀착된 시점과 현장감 (Over-the-Shoulder Immersion)**
   - 캐릭터 등 뒤에 가깝게 붙는 **숄더뷰(Over-the-Shoulder View)** 시점을 통해, 적의 공격 위협과 타격의 무게감을 눈앞에서 생생하게 전달한다.
2. **신중한 공격과 캔슬 플레이 (Committed Action & Cancel)**
   - 무작정 공격 버튼을 연타하는 방식을 지양한다. 모든 공격은 명확한 **선딜레이(Start-up)**와 **후딜레이(Recovery)**를 가지며, 적의 반격 타이밍에 맞춰 후딜레이를 회피나 가드로 **캔슬(Cancel)**하는 판단이 핵심이 된다.
3. **공격적인 수비와 템포 주도 (Aggressive Defense)**
   - 수비는 단순히 데미지(Damage)를 줄이는 수단이 아니라 공격권을 뺏어오기 위한 수단이다. 짧은 **스텝 회피(Quick Step)**로 적의 공격만 살짝 흘려내거나, **저스트 패링(Just Parry)**으로 공격을 튕겨낸 뒤 즉시 **패링 카운터(Parry Counter)**를 꽂아 넣는 공방 흐름을 지향한다.

## 3. 핵심 구현 범위 (Core MVP Scope)
- **카메라 및 캐릭터 이동 (Camera & Character Locomotion)**
  - 캐릭터 등 뒤에 밀착된 **숄더뷰(Over-the-Shoulder View)**를 기본으로 하며, 특정 적을 중심으로 시점을 고정하는 **락온(Lock-On)**을 지원한다.
  - 평상시 항상 카메라 정면을 바라보며 8방향으로 발을 옮기는 **스트레이프 이동(Strafe Locomotion)**을 기본으로 하며, **스프린트(Sprint)** 입력 시에만 이동 방향으로 몸을 틀어 달린다.
- **2단 회피 시스템 (Two-Stage Evasion)**
  - 회피 1회 입력 시 근접 거리를 유지하는 짧은 **스텝 회피(Quick Step)**가 나가고, 스텝 도중 연속 입력 시 거리를 크게 벌리는 **구르기 회피(Dodge Roll)**로 이어진다.
- **콤보 및 캔슬 시스템 (Combo & Cancel System)**
  - 빠른 템포의 **약공격 콤보(Light Attack Combo)**와 무겁고 경직이 큰 **강공격 콤보(Heavy Attack Combo)**.
  - 조작이 씹히지 않도록 돕는 **선입력 버퍼링(Input Buffering)**과 공격 후딜레이 구간(Recovery Window)에서의 이동·회피·가드 **캔슬(Cancel)**.
- **가드 및 패링 공방 (Guard & Parry System)**
  - 일반 공격의 데미지를 막아내는 **가드(Guard / Block)**와, 적의 강공격을 일반 가드로 막았을 때 자세가 무너지는 **가드 브레이크(Guard Break)**.
  - 피격 직전 타이밍에 가드를 올려 적의 공격을 튕겨내는 **저스트 패링(Just Parry)** 및 직후 이어지는 전용 **패링 카운터(Parry Counter / Riposte)**.
- **타격 판정 및 전투 피드백 (Hit Detection & Combat Feel)**
  - 무기 궤적을 따라 정밀하게 판정하는 **히트박스(Hitbox)**와 피격 방향(4방향) 및 타격 강도에 따른 **피격 리액션(Hit Reaction — 약경직/강경직)**.
  - 타격 순간의 손맛을 살려주는 **역경직(Hitstop)**과 **카메라 쉐이크(Camera Shake)**.

## 4. 확장 구현 범위 (Extension Scope — Post-MVP)
1. **스턴 게이지 및 처형 (Stun Gauge & Execution)**: 타격과 패링으로 적의 스턴 게이지(Stun Gauge)를 채워 **그로기(Groggy / Stunned)** 상태로 만들고 전용 **처형기(Execution)**를 발동하는 시스템.
2. **쿨타임 스킬 (Cooldown Skill)**: 기본 콤보 외에 재사용 대기시간(Cooldown)을 두고 사용하는 강력한 특수 공격 스킬.
3. **보스 AI 전투 (Boss AI Encounter)**: 가드 불가 패턴과 연타 패턴을 구사하는 거대 보스와의 1:1 실전 전투.
