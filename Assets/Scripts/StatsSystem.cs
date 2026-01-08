using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    [Header("Current Stats (0~100)")]
    [Range(0, 100)] public float hp = 80f;
    [Range(0, 100)] public float hunger = 70f;
    [Range(0, 100)] public float thirst = 70f;
    [Range(25, 45)] public float temperature = 36.5f;

    [Header("Natural decay (/sec)")]
    public float hungerDecay = 0.5f;
    public float thirstDecay = 0.7f;

    [Header("Temperature System")]
    public float normalTemp = 36.5f;
    public float nightTempDecay = 1.0f;
    public float dayTempIncrease = 1.0f;
    public float coldThreshold = 32f;
    public float hotThreshold = 40f;
    public float coldHpPenalty = 1.0f;
    public float hotHpPenalty = 1.0f;
    public float coldSpeedMultiplier = 0.5f;

    [Header("Zone Temperature Settings")]
    public float bonfireTargetTemp = 39.5f;
    public float bonfireRecoverySpeed = 3.0f;
    public float forestRecoverySpeed = 2.0f;

    [Header("Threshold & HP penalty")]
    public float lowThreshold = 20f;
    public float hpPenaltyAtLow = 1.5f;

    [Header("References")]
    public DayNight dayNightSystem;

    [Header("Training Start (optional)")]
    [Tooltip("학습 안정화를 위해 시작 스탯을 더 높게 줄지")]
    public bool trainingSafeStart = false;

    [Header("Debug")]
    public bool enableDebugLogs = false;

    // Zone 상태 (우선순위: Bonfire > Forest > Hazard > 기본)
    private bool isInBonfire = false;
    private bool isInForest = false;
    private bool isInHazardZone = false;

    public bool IsInBonfire => isInBonfire;
    public bool IsInForest => isInForest;
    public bool IsInHazard => isInHazardZone;

    // HazardZone에서 설정하는 값 (Zone이 없으면 0)
    private float hazardTempDelta = 0f;
    private float hazardHungerDelta = 0f;
    private float hazardThirstDelta = 0f;
    private float hazardHpDelta = 0f;

    public bool IsDead => hp <= 0f;
    public float TimeOfDay01 => dayNightSystem != null ? dayNightSystem.NormalizedTime : 0f;

    // ✅ 관측/리워드 설계에 쓰기 좋은 값들
    public float Hp01 => hp / 100f;
    public float Hunger01 => hunger / 100f;
    public float Thirst01 => thirst / 100f;

    // 온도는 25~45 범위라 0~1로 따로 정규화 (25 -> 0, 45 -> 1)
    public float Temperature01 => Mathf.InverseLerp(25f, 45f, temperature);

    // 위험도(0~1): 허기/갈증/온도 중 가장 위험한 정도
    public float RiskLevel01
    {
        get
        {
            float riskFood = hunger < lowThreshold ? Mathf.Clamp01((lowThreshold - hunger) / lowThreshold) : 0f;
            float riskWater = thirst < lowThreshold ? Mathf.Clamp01((lowThreshold - thirst) / lowThreshold) : 0f;

            float riskTemp = 0f;
            if (temperature < coldThreshold)
                riskTemp = Mathf.Clamp01((coldThreshold - temperature) / (coldThreshold - 25f));
            else if (temperature > hotThreshold)
                riskTemp = Mathf.Clamp01((temperature - hotThreshold) / (45f - hotThreshold));

            return Mathf.Max(riskFood, riskWater, riskTemp);
        }
    }

    public float CurrentSpeedMultiplier
    {
        get
        {
            if (temperature < coldThreshold)
                return coldSpeedMultiplier;
            return 1.0f;
        }
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // 기본 스텟 감소
        hunger = Mathf.Clamp(hunger - hungerDecay * dt, 0f, 100f);
        thirst = Mathf.Clamp(thirst - thirstDecay * dt, 0f, 100f);

        // 체온 변화 (우선순위 적용)
        UpdateTemperature(dt);

        // ✅ HazardZone의 허기/갈증/HP 변화는 "온도 변화 여부와 상관 없이" 항상 적용
        if (isInHazardZone)
        {
            hunger = Mathf.Clamp(hunger + hazardHungerDelta * dt, 0f, 100f);
            thirst = Mathf.Clamp(thirst + hazardThirstDelta * dt, 0f, 100f);
            hp = Mathf.Clamp(hp + hazardHpDelta * dt, 0f, 100f);
        }

        // 저체온/고체온 HP 감소
        if (temperature < coldThreshold)
        {
            hp = Mathf.Clamp(hp - coldHpPenalty * dt, 0f, 100f);
        }
        else if (temperature > hotThreshold)
        {
            hp = Mathf.Clamp(hp - hotHpPenalty * dt, 0f, 100f);
        }

        // 허기/갈증 낮으면 HP 감소
        if (hunger <= lowThreshold || thirst <= lowThreshold)
        {
            hp = Mathf.Clamp(hp - hpPenaltyAtLow * dt, 0f, 100f);
        }
    }

    void UpdateTemperature(float dt)
    {
        bool isNight = dayNightSystem != null && dayNightSystem.IsNight;

        // ★ 우선순위 1: 모닥불
        if (isInBonfire)
        {
            if (isNight)
            {
                temperature = Mathf.MoveTowards(temperature, bonfireTargetTemp, bonfireRecoverySpeed * dt);
            }
            else
            {
                temperature += dayTempIncrease * 1.5f * dt;
            }
            temperature = Mathf.Clamp(temperature, 25f, 45f);
            return;
        }

        // ★ 우선순위 2: 숲
        if (isInForest)
        {
            if (!isNight)
            {
                temperature = Mathf.MoveTowards(temperature, normalTemp, forestRecoverySpeed * dt);
            }
            else
            {
                temperature -= nightTempDecay * 0.5f * dt;
            }
            temperature = Mathf.Clamp(temperature, 25f, 45f);
            return;
        }

        // ★ 우선순위 3/4: HazardZone + 기본
        // 기본 주야 변화 먼저 적용
        if (isNight) temperature -= nightTempDecay * dt;
        else temperature += dayTempIncrease * dt;

        // HazardZone 온도 변화(추가) 적용 (0이면 영향 없음)
        if (isInHazardZone && hazardTempDelta != 0f)
        {
            temperature += hazardTempDelta * dt;
        }

        temperature = Mathf.Clamp(temperature, 25f, 45f);
    }

    // === Zone 상태 설정 메서드 ===

    public void SetInBonfire(bool inZone)
    {
        isInBonfire = inZone;
        if (enableDebugLogs) Debug.Log($"⚠️ Bonfire Zone: {inZone}");
    }

    public void SetInForest(bool inZone)
    {
        isInForest = inZone;
        if (enableDebugLogs) Debug.Log($"⚠️ Forest Zone: {inZone}");
    }

    public void SetInHazardZone(bool inZone, float tempDelta = 0f, float hungerDelta = 0f, float thirstDelta = 0f, float hpDelta = 0f)
    {
        isInHazardZone = inZone;
        if (inZone)
        {
            hazardTempDelta = tempDelta;
            hazardHungerDelta = hungerDelta;
            hazardThirstDelta = thirstDelta;
            hazardHpDelta = hpDelta;
        }
        else
        {
            hazardTempDelta = 0f;
            hazardHungerDelta = 0f;
            hazardThirstDelta = 0f;
            hazardHpDelta = 0f;
        }
        if (enableDebugLogs) Debug.Log($"⚠️ Hazard Zone: {inZone}");
    }

    // 아이템용
    public void ApplyDelta(float hpDelta, float hungerDelta, float thirstDelta, float tempDelta)
    {
        hp = Mathf.Clamp(hp + hpDelta, 0f, 100f);
        hunger = Mathf.Clamp(hunger + hungerDelta, 0f, 100f);
        thirst = Mathf.Clamp(thirst + thirstDelta, 0f, 100f);
        temperature = Mathf.Clamp(temperature + tempDelta, 25f, 45f);
    }

    public void ResetRandom()
    {
        if (trainingSafeStart)
        {
            // 학습 안정화용: 초반에 너무 빨리 위험 상태로 들어가지 않게
            hp = Random.Range(80f, 100f);
            hunger = Random.Range(75f, 100f);
            thirst = Random.Range(75f, 100f);
        }
        else
        {
            hp = Random.Range(60f, 90f);
            hunger = Random.Range(50f, 80f);
            thirst = Random.Range(50f, 80f);
        }

        temperature = normalTemp;

        // Zone 상태 초기화
        isInBonfire = false;
        isInForest = false;
        isInHazardZone = false;
        hazardTempDelta = 0f;
        hazardHungerDelta = 0f;
        hazardThirstDelta = 0f;
        hazardHpDelta = 0f;
    }
}
