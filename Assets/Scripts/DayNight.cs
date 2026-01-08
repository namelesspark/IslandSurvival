using UnityEngine;
using UnityEngine.UI;

public class DayNight : MonoBehaviour
{
    public Light directionalLight;
    public Image nightOverlay; // NightOverlay Image 연결
    public float cycleTime = 20f;
    
    private float currentTime = 0f;
    
    public Color dayColor = Color.white;
    public Color nightColor = new Color(0.3f, 0.3f, 0.5f);
    
    [Header("Night Threshold")]
    [Range(0f, 1f)]
    public float nightThreshold = 0.4f; // 이 값보다 어두우면 밤

    public float NormalizedTime { get; private set; }
    public float BrightnessValue { get; private set; } // t 값
    public bool IsNight { get; private set; }

    void Update()
    {
        currentTime += Time.deltaTime;
        NormalizedTime = (currentTime % cycleTime) / cycleTime;
        float cycleValue = Mathf.Sin(NormalizedTime * Mathf.PI * 2);
        float t = (cycleValue + 1f) / 2f;
        
        BrightnessValue = t;

        IsNight = t < nightThreshold;

        // 라이트 색상
        if (directionalLight != null)
        {
            directionalLight.color = Color.Lerp(nightColor, dayColor, t);
        }
        
        // Overlay 투명도 조정 (밤에 어둡게)
        if (nightOverlay != null)
        {
            Color overlayColor = nightOverlay.color;
            overlayColor.a = Mathf.Lerp(0.8f, 0f, t); // 밤: 80% 어둡게, 낮: 투명
            nightOverlay.color = overlayColor;
        }
    }
}