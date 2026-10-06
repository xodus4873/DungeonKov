using UnityEngine;

[ExecuteAlways]
public sealed class TownObstacle : MonoBehaviour
{
    public Vector2 groundSize=new Vector2(2,2);
    void Update()
    {
        if(TryGetComponent<SpriteRenderer>(out var r))r.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100);
    }
    public bool Contains(Vector2 ground)
    {
        Vector2 center=MosslightGame.Unproject(transform.position);
        Vector2 half=groundSize*.5f*Mathf.Max(Mathf.Abs(transform.lossyScale.x),Mathf.Abs(transform.lossyScale.y));
        return Mathf.Abs(ground.x-center.x)<half.x+.22f&&Mathf.Abs(ground.y-center.y)<half.y+.22f;
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color=Color.yellow;Vector2 h=groundSize*.5f;
        Vector2[] p={new Vector2(-h.x,-h.y),new Vector2(-h.x,h.y),new Vector2(h.x,h.y),new Vector2(h.x,-h.y)};
        for(int i=0;i<4;i++)Gizmos.DrawLine(transform.position+(Vector3)MosslightGame.Project(p[i]),transform.position+(Vector3)MosslightGame.Project(p[(i+1)%4]));
    }
}
