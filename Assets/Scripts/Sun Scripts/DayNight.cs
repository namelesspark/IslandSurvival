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
    
    void Update()
    {
        currentTime += Time.deltaTime;
        float normalizedTime = (currentTime % cycleTime) / cycleTime;
        float cycleValue = Mathf.Sin(normalizedTime * Mathf.PI * 2);
        float t = (cycleValue + 1f) / 2f;
        
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