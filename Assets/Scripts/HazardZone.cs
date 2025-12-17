using UnityEngine;

public enum HazardType
{
    Heat,   // 더위존: 갈증↑ 소모, 체온↑
    Cold    // 추위존: 체온↓, HP or 체력 소모
}

[RequireComponent(typeof(Collider2D))]
public class HazardZone : MonoBehaviour
{
    [Header("Hazard Settings")]
    public HazardType hazardType = HazardType.Heat;

    [Header("Stat Changes (per second)")]
    public float hungerDeltaPerSec = 0f;
    public float thirstDeltaPerSec = 0f;
    public float tempDeltaPerSec = 0f;
    public float hpDeltaPerSec = 0f;

    [Header("Visual Feedback")]
    public bool showWarningOnEnter = true;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            // Zone 진입 시 상태와 delta 값 전달
            stats.SetInHazardZone(true, tempDeltaPerSec, hungerDeltaPerSec, thirstDeltaPerSec, hpDeltaPerSec);
            
            if (showWarningOnEnter)
            {
                Debug.LogWarning($"⚠️ Entered {hazardType} Hazard Zone!");
            }
        }
    }

    // OnTriggerStay2D 제거! - StatsSystem에서 처리

    private void OnTriggerExit2D(Collider2D other)
    {
        var stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            stats.SetInHazardZone(false);
            Debug.Log($"✅ Escaped {hazardType} Hazard Zone");
        }
    }
}