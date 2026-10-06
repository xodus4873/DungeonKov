using UnityEngine;

[ExecuteAlways]
public sealed class TownNpc : MonoBehaviour
{
    public enum Service { Blacksmith, Church, Shop }
    public Service service;
    public string displayName;
    [TextArea(2,5)] public string greeting;
    [Min(0)] public int price=20;
    void Update()
    {
        if(TryGetComponent<SpriteRenderer>(out var r))r.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100);
    }
}
