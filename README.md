# 🏝️ Island Survival - ML-Agents 강화학습 프로젝트

Unity ML-Agents를 활용한 무인도 생존 시뮬레이션 강화학습 프로젝트입니다. AI 에이전트가 무인도 환경에서 생존 스탯(HP, 배고픔, 갈증, 체온)을 관리하며 최대한 오래 생존하는 것을 학습합니다.

## 📋 프로젝트 개요

| 항목 | 내용 |
|------|------|
| **개발 환경** | Unity 6000, ML-Agents 0.26.0 |
| **학습 알고리즘** | PPO (Proximal Policy Optimization) |
| **관측 공간** | 9차원 (HP, 배고픔, 갈증, 체온, 위치 x/y, 속도 x/y, 시간대) |
| **행동 공간** | 이산형 - 이동(5방향) + 아이템 사용(2) |

## 🎮 게임 시스템

### 생존 스탯
- **HP**: 체력 (0이 되면 사망)
- **Hunger**: 배고픔 (시간에 따라 감소, 낮아지면 HP 감소)
- **Thirst**: 갈증 (시간에 따라 감소, 낮아지면 HP 감소)
- **Temperature**: 체온 (저체온/고체온 시 HP 감소, 이동속도 저하)

### 환경 요소
- **주야간 시스템**: 밤에는 체온 하락, 낮에는 체온 상승
- **모닥불 존**: 밤에 체온 회복
- **숲 존**: 낮에 더위로부터 보호
- **위험 존**: 전갈 등 적 출현, 스탯에 부정적 영향

### 아이템
- **음식 (Apple, Coconut 등)**: 배고픔 회복
- **물 (Water)**: 갈증 회복
- **응급키트 (FirstAidKit)**: HP 회복

## 🧠 강화학습 설계

### 관측(Observation) - 9차원
```
- HP / 100 (정규화)
- Hunger / 100
- Thirst / 100  
- Temperature / 100
- Position X, Y
- Velocity X, Y
- TimeOfDay (0~1)
```

### 행동(Action) - Discrete
```
- Branch 0: 이동 (0:정지, 1:상, 2:하, 3:좌, 4:우)
- Branch 1: 아이템 사용 (0:안함, 1:사용)
```

### 보상(Reward) 설계
| 상황 | 보상 |
|------|------|
| 매 스텝 생존 | +0.01 |
| 스탯 위험 구간 | -0.01 |
| 아이템 사용 성공 | +0.05 |
| 사망 | -1.0 |

## 📁 프로젝트 구조

```
IslandSurvival/
├── Assets/
│   ├── Scripts/
│   │   ├── SurvivalAgent.cs    # ML-Agents 에이전트
│   │   ├── StatsSystem.cs      # 생존 스탯 시스템
│   │   ├── DayNight.cs         # 주야간 시스템
│   │   ├── Item.cs             # 아이템 베이스
│   │   ├── BonfireZone.cs      # 모닥불 존
│   │   ├── ForestZone.cs       # 숲 존
│   │   ├── HazardZone.cs       # 위험 존
│   │   └── Scorpion.cs         # 적(전갈)
│   ├── Scenes/
│   │   └── Island.unity        # 메인 씬
│   └── PreFabs/                # 프리팹들
├── config/
│   └── SurvivalAgent.yaml      # 학습 설정
└── results/                    # 학습 결과
```

## 🚀 실행 방법

### 학습 실행
```bash
# Anaconda Prompt에서
cd IslandSurvival
mlagents-learn config/SurvivalAgent.yaml --run-id=island_test1
```

### 학습 이어서 하기
```bash
mlagents-learn config/SurvivalAgent.yaml --run-id=island_test1 --resume
```

### TensorBoard로 학습 모니터링
```bash
tensorboard --logdir=results
```

## ⚙️ 학습 설정 (PPO)

```yaml
trainer_type: ppo
hyperparameters:
  batch_size: 128
  buffer_size: 2048
  learning_rate: 3.0e-4
  beta: 0.01
  epsilon: 0.2
network_settings:
  normalize: true
  hidden_units: 256
  num_layers: 2
max_steps: 500000
```

## 📊 학습 결과

학습이 완료되면 `results/island_test1/` 폴더에 `.onnx` 모델 파일이 생성됩니다.

### 학습된 모델 적용
1. `.onnx` 파일을 Unity Assets 폴더로 복사
2. Agent의 Behavior Parameters → Model에 연결
3. Behavior Type을 "Inference Only"로 변경
4. Play로 테스트

## 👥 팀원

- 강화학습 담당: 민호

## 📝 라이선스

이 프로젝트는 교육 목적으로 제작되었습니다.
