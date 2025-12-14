using UnityEngine;
using TMPro;

public class StatsUI : MonoBehaviour
{
    public StatsSystem stats;
    public TextMeshProUGUI statsText;
    
    [Header("Display Settings")]
    public bool showDecimals = true;
    public Color normalColor = Color.white;
    public Color warningColor = Color.yellow;
    public Color dangerColor = Color.red;
    
    void Update()
    {
        if (stats == null || statsText == null) return;
        
        // 텍스트 업데이트
        string format = showDecimals ? "F1" : "F0";
        statsText.text = $"<b>HP:</b> {GetColoredStat(stats.hp, format)}\n" +
                        $"<b>Hunger:</b> {GetColoredStat(stats.hunger, format)}\n" +
                        $"<b>Thirst:</b> {GetColoredStat(stats.thirst, format)}\n" +
                        $"<b>Temp:</b> {GetColoredStat(stats.temperature, format)}\n" +
                        $"<b>Time:</b> {GetTimeOfDay()}";
    }
    
    string GetColoredStat(float value, string format)
    {
        Color color;
        if (value <= stats.lowThreshold)
            color = dangerColor;
        else if (value <= stats.lowThreshold * 1.5f)
            color = warningColor;
        else
            color = normalColor;
        
        string colorHex = ColorUtility.ToHtmlStringRGB(color);
        return $"<color=#{colorHex}>{value.ToString(format)}</color>";
    }
    
    string GetTimeOfDay()
    {
        float time = stats.TimeOfDay01;
        if (time < 0.25f || time > 0.75f)
            return "<color=#4444FF>Night</color>";
        else
            return "<color=#FFFF00>Day</color>";
    }
}