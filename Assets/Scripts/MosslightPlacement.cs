using UnityEngine;

// Scene-authored placement; its Transform is the source of truth for gameplay.
[ExecuteAlways]
public sealed class MosslightPlacement : MonoBehaviour
{
    public enum Kind { Obstacle, PlayerStart, EnemySpawn, Shrine }
    public Kind kind;
    [Min(0)] public float collisionRadius = .45f;
    [Range(1,3)] public int wave = 1;
    public bool elite;
    void Update()
    {
        if (!Application.isPlaying && TryGetComponent<SpriteRenderer>(out var r))
            r.sortingOrder = 1000 - Mathf.RoundToInt(transform.position.y * 100);
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = kind == Kind.Obstacle ? Color.yellow : Color.cyan;
        float radius = kind == Kind.Obstacle ? collisionRadius * Mathf.Max(Mathf.Abs(transform.lossyScale.x), Mathf.Abs(transform.lossyScale.y)) : .25f;
        Vector3 last = transform.position + (Vector3)MosslightGame.Project(Vector2.right * radius);
        for (int i=1;i<=32;i++) {
            float angle=i*Mathf.PI*2/32;
            Vector3 next=transform.position+(Vector3)MosslightGame.Project(new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*radius);
            Gizmos.DrawLine(last,next);last=next;
        }
    }
}
