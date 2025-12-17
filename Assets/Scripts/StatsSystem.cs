using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    [Header("Current Stats (0~100)")]
    [Range(0,100)] public float hp = 80f;
    [Range(0,100)] public float hunger = 70f;
    [Range(0,100)] public float thirst = 70f;
    [Range(25,45)] public float temperature = 36.5f;

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
    public float bonfireTargetTemp = 39.5f;    // 모닥불 목표 체온
    public float bonfireRecoverySpeed = 3.0f;
    public float forestRecoverySpeed = 2.0f;

    [Header("Threshold & HP penalty")]
    public float lowThreshold = 20f;
    public float hpPenaltyAtLow = 1.5f;

    [Header("References")]
    public DayNight dayNightSystem;

    // Zone 상태 (우선순위: Bonfire > Forest > Hazard > 기본)
    private bool isInBonfire = false;
    private bool isInForest = false;
    private bool isInHazardZone = false;
    
    // HazardZone에서 설정하는 값 (Zone이 없으면 0)
    private float hazardTempDelta = 0f;
    private float hazardHungerDelta = 0f;
    private float hazardThirstDelta = 0f;
    private float hazardHpDelta = 0f;

    public bool IsDead => hp <= 0f;
    public float TimeOfDay01 => dayNightSystem != null ? dayNightSystem.NormalizedTime : 0f;

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

        // ★ 우선순위 1: 모닥불 (가장 높음)
        if (isInBonfire)
        {
            if (isNight)
            {
                // 밤 + 모닥불 = 목표 체온으로 회복
                temperature = Mathf.MoveTowards(temperature, bonfireTargetTemp, bonfireRecoverySpeed * dt);
            }
            else
            {
                // 낮 + 모닥불 = 더워짐 (위험)
                temperature += dayTempIncrease * 1.5f * dt;
            }
            temperature = Mathf.Clamp(temperature, 25f, 45f);
            return; // 다른 Zone 효과 무시
        }

        // ★ 우선순위 2: 숲
        if (isInForest)
        {
            if (!isNight)
            {
                // 낮 + 숲 = 정상 체온으로 회복
                temperature = Mathf.MoveTowards(temperature, normalTemp, forestRecoverySpeed * dt);
            }
            else
            {
                // 밤 + 숲 = 여전히 추움 (하지만 HazardZone보다는 약함)
                temperature -= nightTempDecay * 0.5f * dt;
            }
            temperature = Mathf.Clamp(temperature, 25f, 45f);
            return;
        }

        // ★ 우선순위 3: HazardZone (체온 변화만 여기서 적용)
        if (isInHazardZone && hazardTempDelta != 0f)
        {
            temperature += hazardTempDelta * dt;
            temperature = Mathf.Clamp(temperature, 25f, 45f);
            
            // HazardZone의 다른 효과도 적용 (허기, 갈증, HP)
            hunger = Mathf.Clamp(hunger + hazardHungerDelta * dt, 0f, 100f);
            thirst = Mathf.Clamp(thirst + hazardThirstDelta * dt, 0f, 100f);
            hp = Mathf.Clamp(hp + hazardHpDelta * dt, 0f, 100f);
            return;
        }

        // ★ 우선순위 4: 기본 (아무 Zone도 아님)
        if (isNight)
        {
            temperature -= nightTempDecay * dt;
        }
        else
        {
            temperature += dayTempIncrease * dt;
        }
        temperature = Mathf.Clamp(temperature, 25f, 45f);
    }

    // === Zone 상태 설정 메서드 ===
    
    public void SetInBonfire(bool inZone)
    {
        isInBonfire = inZone;
        Debug.Log($"🔥 Bonfire Zone: {inZone}");
    }

    public void SetInForest(bool inZone)
    {
        isInForest = inZone;
        Debug.Log($"🌲 Forest Zone: {inZone}");
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
        Debug.Log($"⚠️ Hazard Zone: {inZone}");
    }

    // 아이템용 (직접 스텟 변경)
    public void ApplyDelta(float hpDelta, float hungerDelta, float thirstDelta, float tempDelta)
    {
        hp = Mathf.Clamp(hp + hpDelta, 0f, 100f);
        hunger = Mathf.Clamp(hunger + hungerDelta, 0f, 100f);
        thirst = Mathf.Clamp(thirst + thirstDelta, 0f, 100f);
        temperature = Mathf.Clamp(temperature + tempDelta, 25f, 45f);
    }

    public void ResetRandom()
    {
        hp = Random.Range(60f, 90f);
        hunger = Random.Range(50f, 80f);
        thirst = Random.Range(50f, 80f);
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