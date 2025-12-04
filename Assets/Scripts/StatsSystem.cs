using UnityEngine;

public class StatsSystem : MonoBehaviour
{
    [Header("Current Stats (0~100)")]
    [Range(0,100)] public float hp = 80f;
    [Range(0,100)] public float hunger = 70f;
    [Range(0,100)] public float thirst = 70f;
    [Range(0,100)] public float temperature = 70f;

    [Header("Natural decay (/sec)")]
    public float hungerDecay = 0.5f;
    public float thirstDecay = 0.7f;
    public float tempDrift   = 0.0f;   // 기본 온도 변화 (0이면 유지)

    [Header("Threshold & HP penalty")]
    public float lowThreshold = 20f;
    public float hpPenaltyAtLow = 1.5f;

    public bool IsDead => hp <= 0f;

    public float TimeOfDay01 { get; private set; } // 0~1, 낮/밤 표현용(선택)

    void Update()
    {
        float dt = Time.deltaTime;

        hunger = Mathf.Clamp(hunger - hungerDecay * dt, 0f, 100f);
        thirst = Mathf.Clamp(thirst - thirstDecay * dt, 0f, 100f);
        temperature = Mathf.Clamp(temperature + tempDrift * dt, 0f, 100f);

        bool low =
            hunger <= lowThreshold ||
            thirst <= lowThreshold ||
            temperature <= lowThreshold ||
            temperature >= 100f - lowThreshold;

        if (low)
        {
            hp = Mathf.Clamp(hp - hpPenaltyAtLow * dt, 0f, 100f);
        }

        // 간단한 낮/밤 (60초 주기)
        TimeOfDay01 = (Time.time % 60f) / 60f;
    }

    public void ApplyDelta(float hpDelta, float hungerDelta, float thirstDelta, float tempDelta)
    {
        hp          = Mathf.Clamp(hp + hpDelta,          0f, 100f);
        hunger      = Mathf.Clamp(hunger + hungerDelta,  0f, 100f);
        thirst      = Mathf.Clamp(thirst + thirstDelta,  0f, 100f);
        temperature = Mathf.Clamp(temperature + tempDelta, 0f, 100f);
    }

    public void ResetRandom()
    {
        hp          = Random.Range(60f, 90f);
        hunger      = Random.Range(50f, 80f);
        thirst      = Random.Range(50f, 80f);
        temperature = Random.Range(45f, 55f);
    }
}
