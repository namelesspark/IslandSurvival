using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DynamicHazardZone : MonoBehaviour
{
    [Header("References")]
    public DayNight dayNightSystem;

    [Header("Day Settings (Heat)")]
    public float dayHungerDeltaPerSec = 0f;
    public float dayThirstDeltaPerSec = -2.0f;
    public float dayTempDeltaPerSec = 3.0f;
    public float dayHpDeltaPerSec = 0f;

    [Header("Night Settings (Cold)")]
    public float nightHungerDeltaPerSec = -0.5f;
    public float nightThirstDeltaPerSec = 0f;
    public float nightTempDeltaPerSec = -3.0f;
    public float nightHpDeltaPerSec = 0f;

    [Header("Visual Feedback")]
    public bool showWarningOnEnter = true;

    private StatsSystem currentStats = null;

    private void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            currentStats = stats;
            UpdateHazardValues();
            
            if (showWarningOnEnter)
            {
                string time = (dayNightSystem != null && dayNightSystem.IsNight) ? "Night" : "Day";
                Debug.LogWarning($"⚠️ Entered Desert ({time} - extreme conditions)!");
            }
        }
    }

    void Update()
    {
        // 밤/낮이 바뀌면 HazardZone 값도 업데이트
        if (currentStats != null)
        {
            UpdateHazardValues();
        }
    }

    void UpdateHazardValues()
    {
        if (currentStats == null || dayNightSystem == null) return;

        bool isNight = dayNightSystem.IsNight;

        if (isNight)
        {
            currentStats.SetInHazardZone(true, nightTempDeltaPerSec, nightHungerDeltaPerSec, nightThirstDeltaPerSec, nightHpDeltaPerSec);
        }
        else
        {
            currentStats.SetInHazardZone(true, dayTempDeltaPerSec, dayHungerDeltaPerSec, dayThirstDeltaPerSec, dayHpDeltaPerSec);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            stats.SetInHazardZone(false);
            currentStats = null;
            Debug.Log($"✅ Escaped Desert Zone");
        }
    }
}