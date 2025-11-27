using UnityEngine;
using Unity.MLAgents.Policies;

[ExecuteAlways]
public class BehaviorNameSetter : MonoBehaviour
{
    public string behaviorName = "SurvivalAgent";

    void OnValidate()
    {
        Apply();
    }

    void Reset()
    {
        Apply();
    }

    void Apply()
    {
        var bp = GetComponent<BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorName = behaviorName;
        }
    }
}
