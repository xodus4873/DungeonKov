using UnityEngine;

public sealed class TownPortal : MonoBehaviour
{
    public string dungeonScene="Mosslight";
    [Min(.5f)] public float interactionRange=1.6f;
    void Update()
    {
        var r=GetComponent<SpriteRenderer>();
        r.sortingOrder=1000-Mathf.RoundToInt(transform.position.y*100);
        r.color=Color.Lerp(Color.white,new Color(.65f,.9f,1),(.5f+.5f*Mathf.Sin(Time.time*2))*.3f);
    }
}
