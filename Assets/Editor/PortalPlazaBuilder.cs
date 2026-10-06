using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TownBuilder;

public static class PortalPlazaBuilder
{
    public static void Upgrade()
    {
        string backup="Assets/Scenes/Backups/Town_before_portal_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!AssetDatabase.CopyAsset("Assets/Scenes/Town.unity",backup))throw new Exception("Backup failed");
        EditorSceneManager.OpenScene("Assets/Scenes/Town.unity");
        var well=GameObject.Find("Central stone well");if(well)UnityEngine.Object.DestroyImmediate(well);
        if(!UnityEngine.Object.FindAnyObjectByType<TownPortal>()) {
            var root=new GameObject("Portal Plaza — dungeon entrance").transform;
            var floor=new TownBuilder.Canvas(192,96);
            for(int y=0;y<96;y++)for(int x=0;x<192;x++) {
                float dx=(x-95.5f)/92,dy=(y-47.5f)/44,r=Mathf.Sqrt(dx*dx+dy*dy);
                if(r>1)continue;
                Color c=C("53616a");
                if(r>.94f||r>.78f&&r<.82f)c=C("b0c0b7");
                if(r>.62f&&r<.65f)c=C("55c9d0");
                if(r<.57f)c=C("364c5c");
                if(r>.66f&&r<.76f&&Mathf.Sin(Mathf.Atan2(dy,dx)*12)>.85f)c=C("90e7df");
                if((x+y*2)%27==0)c*=.86f;
                floor.texture.SetPixel(x,y,c);
            }
            var platform=Save("PortalPlazaDisc",floor,new Vector2(.5f,.5f),24);
            Place("Runic platform",platform,Vector2.zero,root,-18000);
            var art=new TownBuilder.Canvas(80,100);
            for(int y=8;y<96;y++)for(int x=7;x<73;x++) {
                float dx=(x-40f)/29,dy=(y-49f)/42,r=Mathf.Sqrt(dx*dx+dy*dy);
                if(r>1.08f)continue;
                Color c;
                if(r>.91f)c=(x+y)%8<2?C("768695"):C("475868");
                else if(r>.79f)c=C("87e6e5");
                else {
                    float wave=Mathf.Sin(r*21-Mathf.Atan2(dy,dx)*3);
                    c=Color.Lerp(C("243d72"),C("42adcf"),(wave+1)*.5f);
                    if(r<.15)c=C("c0f7ec");
                }
                art.texture.SetPixel(x,y,c);
            }
            art.Rect(5,4,70,7,C("8d9b9c"));art.Rect(11,0,58,5,C("51636e"));
            var portal=Place("Dungeon Portal — click to enter",Save("DungeonPortal",art,new Vector2(.5f,.08f),24),Vector2.zero,root);
            portal.gameObject.AddComponent<TownPortal>();
            Sign("DUNGEON GATE",new Vector2(0,4.3f),root);
            PrefabUtility.SaveAsPrefabAsset(root.gameObject,"Assets/Town/Prefabs/PortalPlaza.prefab");
        }
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
        var game=UnityEngine.Object.FindAnyObjectByType<TownGame>();
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
            var cam=game.townCamera;Vector3 old=cam.transform.position;float zoom=cam.orthographicSize;
            cam.transform.position=new Vector3(0,1,-20);cam.orthographicSize=8;
            var target=new RenderTexture(1280,800,24);cam.targetTexture=target;cam.Render();RenderTexture.active=target;
            var image=new Texture2D(1280,800,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1280,800),0,0);image.Apply();File.WriteAllBytes("PortalPlazaPreview.png",image.EncodeToPNG());
            cam.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);cam.transform.position=old;cam.orthographicSize=zoom;
        }
        TownBuilder.Build();
    }
}
