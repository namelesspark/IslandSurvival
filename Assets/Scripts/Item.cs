

using UnityEngine;

public enum ItemType
{
    Fruit,   // 허기+
    Water,     // 갈증+
    Worm, // 허기+ , 갈증+
    FirstAidKit     // HP+
}

public class Item : MonoBehaviour
{
    public ItemType itemType;
    public float value = 25f;
    public float cooldown = 10f;

    Collider2D _col;
    SpriteRenderer _sr;

    void Awake()
    {
        _col = GetComponent<Collider2D>();
        _sr = GetComponent<SpriteRenderer>();
    }

    // 호출되면 StatsSystem에 적용
    public bool TryApply(StatsSystem stats)
    {
        if (!_col.enabled || stats == null) return false;

        switch (itemType)
        {
            case ItemType.Fruit:
                stats.ApplyDelta(0f, +value, 0f, 0f);
                break;
            case ItemType.Water:
                stats.ApplyDelta(0f, 0f, +value, 0f);
                break;
            case ItemType.Worm:
                stats.ApplyDelta(0f, +value, +value, 0f);
                break;
            case ItemType.FirstAidKit:
                stats.ApplyDelta(+value, 0f, 0f, 0f);
                break;
        }

        // 사용 후 비활성화 + 쿨다운
        _col.enabled = false;
        if (_sr != null) _sr.enabled = false;
        if (cooldown > 0f)
            Invoke(nameof(Respawn), cooldown);

        return true;
    }

    void Respawn()
    {
        if (_col != null) _col.enabled = true;
        if (_sr != null) _sr.enabled = true;
    }
}
