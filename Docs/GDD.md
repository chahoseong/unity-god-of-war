# ⚔️ GodOfWar 전투 포트폴리오 — 게임 디자인 문서 (GDD)

## 1. 프로젝트 비전 (Project Vision)
『갓 오브 워(God of War, 2018)』의 전투 감각을 레퍼런스로 삼아, 캐릭터 등 뒤에 밀착된 카메라 시점에서 묵직한 검술 공방(타격, 후딜레이 캔슬, 회피, 저스트 패링)의 핵심 재미를 구현하는 3인칭 근접 액션 버티컬 슬라이스(Vertical Slice).

## 2. 핵심 디자인 방향 (Core Design Pillars)
1. **밀착된 시점과 현장감 (Over-the-Shoulder Immersion)**
   - 캐릭터 등 뒤에 가깝게 붙는 **숄더뷰(Over-the-Shoulder View)** 시점을 통해, 적의 공격 위협과 타격의 무게감을 눈앞에서 생생하게 전달한다.
2. **신중한 공격과 캔슬 플레이 (Committed Action & Cancel)**
   - 무작정 공격 버튼을 연타하는 방식을 지양한다. 모든 공격은 명확한 **선딜레이(Start-up)**와 **후딜레이(Recovery)**를 가지며, 적의 반격 타이밍에 맞춰 후딜레이를 회피나 가드로 **캔슬(Cancel)**하는 판단이 핵심이 된다.
3. **공격적인 수비와 템포 주도 (Aggressive Defense)**
   - 수비는 단순히 적의 공격을 받아내는 데 그치지 않고 공격권을 뺏어오는 수단이다. 짧은 **스텝 회피(Quick Step)**로 적의 공격만 살짝 흘려내거나, **저스트 패링(Just Parry)**으로 상대를 밀쳐낸 뒤 즉시 **패링 카운터(Parry Counter)**를 꽂아 넣는 공방 흐름을 지향한다.

## 3. 핵심 구현 범위 (Core MVP Scope)
- **카메라 및 캐릭터 이동 (Camera & Character Locomotion)**
  - 캐릭터 등 뒤에 밀착된 **숄더뷰(Over-the-Shoulder View)**를 기본으로 하며, 특정 적에게 시점을 고정하고 주변 적으로 대상을 바꿀 수 있는 **락온(Lock-On)**을 지원한다.
  - 항상 카메라 정면을 바라보며 8방향으로 발을 옮기는 **스트레이프 이동(Strafe Locomotion)**을 기본으로 하며, 전방 이동 중 **스프린트(Sprint)** 입력 시에만 앞으로 빠르게 질주한다.
- **2단 회피 시스템 (Two-Stage Evasion)**
  - 회피 1회 입력 시 근접 거리를 유지하는 짧은 **스텝 회피(Quick Step)**가 나가고, 스텝 도중 연속 입력 시 거리를 크게 벌리는 **구르기 회피(Dodge Roll)**로 이어진다.
- **콤보 및 캔슬 시스템 (Combo & Cancel System)**
  - 빠른 템포의 **약공격 콤보(Light Attack Combo)**와 무겁고 경직이 큰 **강공격 콤보(Heavy Attack Combo)**.
  - 조작이 씹히지 않도록 돕는 **선입력 버퍼링(Input Buffering)**과 공격 후딜레이 구간(Recovery Window)에서의 이동·회피·가드 **캔슬(Cancel)**.
- **가드 및 패링 공방 (Guard & Parry System)**
  - 적의 약공격을 막아내는 **가드(Guard / Block)**와, 적의 강공격을 일반 가드로 막았을 때 자세가 무너지는 **가드 브레이크(Guard Break)**.
  - 피격 직전 타이밍에 가드를 올려 적의 공격을 튕겨내는 **저스트 패링(Just Parry)** 및 직후 이어지는 전용 **패링 카운터(Parry Counter / Riposte)**.
- **타격 판정 및 피격 리액션 (Hit Detection & Hit Reaction)**
  - 무기 궤적을 따라 정밀하게 판정하는 **히트박스(Hitbox)**와 피격 방향(4방향) 및 타격 강도에 따른 **피격 리액션(Hit Reaction — 약경직/강경직)**.

## 4. 확장 구현 범위 — 타격감 및 전투 연출 (Extension Scope — Game Feel)
- **역경직 및 카메라 쉐이크 (Hitstop & Camera Shake)**
  - 타격 결과(적중, 가드, 가드 브레이크, 패링)와 공격 강도에 맞춰 공격자와 피격자에게 **역경직(Hitstop)**을 걸고 **카메라 쉐이크(Camera Shake)**를 주어 묵직한 손맛을 전달한다.
- **히트 플래시 (Hit Flash)**
  - 공격이 적중한 순간 피격자의 몸이 하얗게 번쩍이는 **히트 플래시(Hit Flash)** 효과로 타격이 들어갔음을 직관적으로 보여준다.
- **무기 궤적 (Weapon Trail)**
  - 무기를 휘두르는 궤적을 따라 **무기 궤적(Weapon Trail)**을 그려 공격의 궤도와 속도감을 선명하게 살린다.
- **타점 이펙트 (Impact VFX)**
  - 무기가 닿은 지점(Contact Point)에 판정 결과(약/강 적중, 가드, 가드 브레이크, 패링)에 맞는 스파크와 충격파 이펙트를 터뜨려 공방 결과를 시각적으로 구분한다.
- **콤보 피니셔와 넉다운 (Combo Finisher & Knockdown)**
  - 약·강공격 콤보의 마지막 타격에 맞은 적은 바닥에 쓰러지는 **넉다운(Knockdown)** 상태가 되며, 타격 순간과 몸이 바닥에 부딪히는 순간 두 번에 걸쳐 카메라가 흔들려 마무리 일격의 무게감을 강조한다.
