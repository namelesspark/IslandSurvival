using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ForestZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        StatsSystem stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            stats.SetInForest(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        StatsSystem stats = other.GetComponent<StatsSystem>();
        if (stats != null)
        {
            stats.SetInForest(false);
        }
    }
}