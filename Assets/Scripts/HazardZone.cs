

using UnityEngine;

public enum HazardType
{
    Heat,   // 더위존: 갈증↑ 소모, 체온↑
    Cold    // 추위존: 체온↓, HP or 체력 소모
}

[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    public HazardType hazardType;
    public float tickInterval = 0.5f;
    public float hungerDeltaPerTick = 0f;
    public float thirstDeltaPerTick = 0f;
    public float tempDeltaPerTick = 0f;
    public float hpDeltaPerTick = 0f;

    void OnTriggerStay2D(Collider2D other)
    {
        var stats = other.GetComponent<StatsSystem>();
        if (stats == null) return;

        // 간단하게 매 프레임 적용하는 버전 (원하면 코루틴으로 tick 처리)
        float dt = Time.deltaTime;
        stats.ApplyDelta(hpDeltaPerTick * dt,
                         hungerDeltaPerTick * dt,
                         thirstDeltaPerTick * dt,
                         tempDeltaPerTick * dt);
    }
}
