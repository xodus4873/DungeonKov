using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using static TownBuilder;
using Canvas = TownBuilder.Canvas;

// One-time authored scene redesign. Normal Build town never regenerates this layout.
public static class TownExpansion
{
    static Transform ground,buildings,decor;
    static System.Random random;
    static SpriteRenderer Put(string name,Sprite sprite,Vector2 logical,Transform parent,int order=0)=>Place(name,sprite,MosslightGame.Project(logical),parent,order);
    static Sprite Tile(string name,int kind,int seed)
    {
        var d=new Canvas(64,32);var rng=new System.Random(seed);
        Color[] colors={C("66714d"),C("a29980"),C("b4ab8c"),C("795a3e"),C("427880"),C("ac8251")};
        for(int y=0;y<32;y++)for(int x=0;x<64;x++) {
            float a=(x-31.5f)/32f,b=(y-15.5f)/16f;if(Mathf.Abs(a)+Mathf.Abs(b)>1)continue;
            float u=(a+b+1)*16,v=(b-a+1)*16;
            float noise=(float)rng.NextDouble()*.065f-.032f;Color c=colors[kind];
            if(kind==1||kind==2) {
                bool seam=((int)v%8==0)||((int)(u+((int)v/8%2)*4)%8==0);
                if(seam)c*=.71f;else if((int)v%8==1)c*=1.07f;
            }
            if(kind==3&&((int)u%5==0))c*=.65f;
            if(kind==4&&((int)(x+y*3)%27<3))c*=1.24f;
            if(kind==5&&((int)v%5==0))c*=.65f;
            c.r+=noise;c.g+=noise;c.b+=noise;c.a=1;d.texture.SetPixel(x,y,c);
        }
        return Save(name,d,new Vector2(.5f,.5f),32);
    }
    static Sprite Cottage(string name,string roof)
    {
        var d=new Canvas(80,90);Color wood=C("4d3d32");
        d.Poly(C("344737"),V(1,11),V(42,0),V(79,14),V(42,28));
        d.Poly(C("c3ad83"),V(9,15),V(44,5),V(44,45),V(9,56));
        d.Poly(C("9d8b72"),V(44,5),V(73,18),V(73,55),V(44,45));
        d.Poly(C(roof),V(3,56),V(28,83),V(79,61),V(45,40));
        for(int i=0;i<5;i++)d.Line(6+i*5,57+i*5,47+i*6,42+i*4,C("d0a16b"));
        d.Line(9,15,44,5,wood,3);d.Line(44,5,73,18,wood,3);
        d.Line(9,15,9,55,wood,3);d.Line(43,5,43,45,wood,3);d.Line(71,18,71,54,wood,3);
        d.Line(10,37,44,26,wood,2);d.Line(10,17,41,42,wood,2);
        d.Rect(26,12,12,24,wood);d.Rect(29,14,7,20,C("795b41"));d.Rect(34,23,2,2,C("dfc077"));
        d.Rect(13,40,10,12,wood);d.Rect(15,42,6,8,C("ead098"));
        d.Rect(53,29,12,16,wood);d.Rect(55,31,8,12,C("e3bf79"));d.Rect(58,31,2,12,wood);
        d.Rect(58,64,9,15,C("8e8878"));d.Rect(56,77,13,3,C("c0b59a"));
        d.Rect(49,20,20,3,C("62744a"));d.Rect(52,22,3,3,C("d69977"));d.Rect(60,22,3,3,C("d9c58a"));
        return Save(name,d,new Vector2(.5f,.13f),20);
    }
    static Sprite SmallProp(string name,int kind)
    {
        var d=new Canvas(40,48);
        if(kind==0) { // Market stall
            d.Rect(3,6,3,29,C("684e38"));d.Rect(34,6,3,29,C("684e38"));
            d.Rect(2,8,36,12,C("9e794c"));d.Rect(3,18,34,3,C("cfad70"));
            for(int i=0;i<6;i++){d.Rect(4+i*5,21,4,4,C(i%2==0?"b95745":"a4ab62"));d.Rect(i*6+2,32,6,10,C(i%2==0?"ad5f4d":"e1c48b"));}
            d.Rect(0,30,40,4,C("b99666"));
        } else if(kind==1) { // Flower bed
            d.Poly(C("67523f"),V(2,8),V(20,0),V(38,8),V(20,18));
            for(int i=0;i<12;i++){int x=5+i*11%28,y=6+i*7%9;d.Rect(x,y,2,5,C("476745"));d.Rect(x-1,y+4,4,3,C(i%2==0?"c08aa0":"e2c688"));}
        } else if(kind==2) { // Crates
            d.Rect(5,0,27,24,C("8f6843"));d.Rect(7,2,23,20,C("b58b57"));d.Line(7,2,29,21,C("684c34"),2);d.Line(7,21,29,2,C("684c34"),2);d.Rect(5,22,27,3,C("dfb879"));
        } else if(kind==3) { // Wheat
            for(int i=0;i<6;i++){int x=4+i*6;d.Rect(x,0,1,12+i%3*2,C("a89752"));d.Rect(x-1,9,3,7,C("d2b66a"));}
        } else { // Banner
            d.Rect(19,0,2,43,C("78664e"));d.Rect(5,39,19,2,C("d5b47a"));d.Rect(6,16,13,23,C("577d89"));d.Rect(11,20,3,16,C("e1c18a"));d.Rect(7,27,11,3,C("e1c18a"));
        }
        return Save(name,d,new Vector2(.5f,0),20);
    }
    static void Block(SpriteRenderer r,Vector2 size){r.gameObject.AddComponent<TownObstacle>().groundSize=size;}
    static void House(Sprite sprite,Vector2 p,string name)
    {
        var r=Put(name,sprite,p,buildings);Block(r,new Vector2(2.5f,2.5f));
    }
    public static void Redesign()
    {
        Directory.CreateDirectory("Assets/Scenes/Backups");
        string backup="Assets/Scenes/Backups/Town_before_expansion_"+DateTime.Now.ToString("yyyyMMdd_HHmmss")+".unity";
        if(!AssetDatabase.CopyAsset("Assets/Scenes/Town.unity",backup))throw new Exception("Cannot back up original town");
        EditorSceneManager.OpenScene("Assets/Scenes/Town.unity");
        var old=UnityEngine.Object.FindObjectsByType<TownNpc>(FindObjectsSortMode.None);
        var names=new string[3];var lines=new string[3];var prices=new int[3];
        foreach(var npc in old){int i=(int)npc.service;names[i]=npc.displayName;lines[i]=npc.greeting;prices[i]=npc.price;}
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var root=new GameObject("Stonehaven — 16x expanded town").transform;
        ground=new GameObject("Ground — 76 x 76 editable tiles").transform;ground.SetParent(root);
        buildings=new GameObject("Districts — buildings and landmarks").transform;buildings.SetParent(root);
        decor=new GameObject("Landscape — gardens fields and river").transform;decor.SetParent(root);
        var people=new GameObject("NPCs — dialogue and services").transform;people.SetParent(root);
        random=new System.Random(149);
        var grass=new Sprite[3];var stone=new Sprite[3];
        for(int i=0;i<3;i++){grass[i]=Tile("GrassV2_"+i,0,i+20);stone[i]=Tile("CobbleV2_"+i,1,i+40);}
        var marble=Tile("PlazaV2",2,60);var soil=Tile("FurrowsV2",3,61);var water=Tile("RiverV2",4,62);var deck=Tile("BridgeV2",5,63);
        for(int x=0;x<76;x++)for(int y=0;y<76;y++) {
            Vector2 p=new Vector2(x-37.5f,y-37.5f);
            bool river=Mathf.Abs(p.x-24)<2.5f;
            bool bridge=Mathf.Abs(p.y)<2||Mathf.Abs(p.y-16)<2||Mathf.Abs(p.y+16)<2;
            bool road=Mathf.Abs(p.x)<1.5f||Mathf.Abs(p.y)<1.5f||Mathf.Abs(Mathf.Abs(p.x)-14)<1||Mathf.Abs(Mathf.Abs(p.y)-14)<1;
            bool plaza=Mathf.Abs(p.x)<5.5f&&Mathf.Abs(p.y)<5.5f;
            bool church=Mathf.Abs(p.x-12)<5&&Mathf.Abs(p.y-12)<5;
            bool farm=p.x>-30&&p.x<-18&&p.y>-24&&p.y<-7;
            Sprite s=river?(bridge?deck:water):plaza||church?marble:road?stone[(x+y)%3]:farm?soil:grass[random.Next(3)];
            var r=Put("Tile "+x+","+y,s,p,ground,-20000);
            if((plaza&&(Mathf.Abs(p.x)==4.5f||Mathf.Abs(p.y)==4.5f))||(church&&(x+y)%4==0))r.color=C("c2b18c");
        }
        // River collision is split at each of the three walkable bridges.
        foreach(var span in new[]{new Vector2(-37,-18),new Vector2(-14,-2),new Vector2(2,14),new Vector2(18,37)}) {
            var go=new GameObject("River bank — impassable");go.transform.SetParent(decor);go.transform.position=MosslightGame.Project(new Vector2(24,(span.x+span.y)/2));
            go.AddComponent<TownObstacle>().groundSize=new Vector2(5,span.y-span.x);
        }
        var tree=AssetSprite("Assets/Art/tree.asset");var flower=SmallProp("FlowerbedV2",1);var crate=SmallProp("CratesV2",2);var wheat=SmallProp("WheatV2",3);var flag=SmallProp("BannerV2",4);
        var cottage=Cottage("CottageOchreV2","927149");var cottage2=Cottage("CottageSlateV2","63767c");var cottage3=Cottage("CottageClayV2","9b6552");
        for(int i=0;i<5;i++) {
            House(i%2==0?cottage:cottage2,new Vector2(-8-i*5,-8),"South residence "+i);
            House(i%2==0?cottage3:cottage,new Vector2(-8,-16-i*4),"West residence "+i);
            House(i%2==0?cottage2:cottage3,new Vector2(8+i*3.2f,-22),"Riverside residence "+i);
            House(cottage,new Vector2(-22,5+i*5),"Garden residence "+i);
        }
        var lamp=Prop("Lantern",false);
        for(int i=-32;i<=32;i+=8) {
            Put("Road lantern",lamp,new Vector2(i,2.5f),decor);
            Put("Road lantern",lamp,new Vector2(2.5f,i),decor);
        }
        for(int x=-29;x<-18;x+=2)for(int y=-23;y<-7;y+=2)Put("Wheat crop",wheat,new Vector2(x,y),decor);
        for(int i=0;i<60;i++) {
            Vector2 p=new Vector2(random.Next(-35,36),random.Next(-35,36));
            if(Mathf.Abs(p.x)<4||Mathf.Abs(p.y)<4||Mathf.Abs(p.x-24)<5||Mathf.Abs(Mathf.Abs(p.x)-14)<3||Mathf.Abs(Mathf.Abs(p.y)-14)<3)continue;
            if((p.x>-31&&p.x<-17&&p.y<0)||Mathf.Abs(p.x)<17&&Mathf.Abs(p.y)<17)continue;
            var r=Put("Grove oak",tree,p,decor);r.color=C(i%3==0?"d1bc88":"a7bf9a");Block(r,Vector2.one*.5f);
        }
        for(int i=-35;i<=35;i+=4)foreach(int side in new[]{-1,1}) {
            var r=Put("Boundary oak",tree,new Vector2(i,side*36.5f),decor);Block(r,Vector2.one*.5f);
            r=Put("Boundary oak",tree,new Vector2(side*36.5f,i),decor);Block(r,Vector2.one*.5f);
        }
        var well=Put("Central stone well",Prop("Well",true),Vector2.zero,decor);well.transform.localScale=Vector3.one*1.5f;Block(well,Vector2.one);
        for(int i=0;i<4;i++) {
            var p=new Vector2(i<2?-4:4,i%2==0?-4:4);Put("Plaza flower bed",flower,p,decor);Put("Town banner",flag,p+Vector2.up,decor);
        }
        var market=SmallProp("MarketStallV2",0);
        for(int i=0;i<6;i++){var r=Put("Market stall "+i,market,new Vector2(7+i%3*3,-3-i/3*3),decor);Block(r,Vector2.one*.9f);}
        Vector2[] sites={new Vector2(-9,7),new Vector2(12,12),new Vector2(8,-8)};
        string[] signs={"THE EMBER FORGE","ST. ELYN ABBEY","THE TRAVELLER'S STORE"};
        var human=AssetSprite("Assets/Art/hero.asset");
        for(int i=0;i<3;i++) {
            var r=Put(signs[i],Building("LandmarkV2_"+i,i),sites[i],buildings);r.transform.localScale=Vector3.one*(i==1?1.6f:1.3f);Block(r,new Vector2(2.6f,2.6f));
            Sign(signs[i],(Vector2)r.transform.position+Vector2.up*(i==1?6:4.8f),r.transform);
            var npc=Put(names[i],human,sites[i]-Vector2.one*(i==1?2.7f:2.2f),people);npc.transform.localScale=Vector3.one*1.15f;
            npc.color=i==0?new Color(1,.69f,.48f):i==1?new Color(.82f,.88f,1):new Color(1,.76f,.85f);
            var n=npc.gameObject.AddComponent<TownNpc>();n.service=(TownNpc.Service)i;n.displayName=names[i];n.greeting=lines[i];n.price=prices[i];
            for(int j=0;j<4;j++)Put(i==0?"Workshop supplies":"Garden border",i==0?crate:flower,sites[i]+new Vector2(j-2,3.5f),decor);
        }
        // Small abbey garden and riverside orchard make the outskirts distinct.
        for(int x=7;x<=17;x+=2)Put("Abbey flowers",flower,new Vector2(x,18),decor);
        for(int x=29;x<=34;x+=3)for(int y=-25;y<=25;y+=5){var r=Put("Orchard tree",tree,new Vector2(x,y),decor);r.color=C("cfb287");Block(r,Vector2.one*.5f);}
        var game=new GameObject("Town — Settings").AddComponent<TownGame>();game.mapLimit=36;game.moveSpeed=5;game.cameraZoom=9;game.followPlayer=true;
        game.player=Put("Player — editable start",human,new Vector2(-3,-3),root).transform;
        game.moveMarker=Put("Move destination",AssetSprite("Assets/Art/tile.asset"),Vector2.zero,root,-19000);game.moveMarker.transform.localScale=Vector3.one*.2f;game.moveMarker.color=C("edc77c");game.moveMarker.enabled=false;
        var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=9;camera.transform.position=new Vector3(0,-2,-20);camera.backgroundColor=C("202d28");camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();game.townCamera=camera;
        AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,"Assets/Scenes/Town.unity");
        Debug.Log("TOWN_EXPANDED: 5776 tiles (361 x 16); playable area 72 x 72 (18 x 18 x 16)");
        RenderPreview(camera,"TownPreview.png",new Vector3(0,1,-20),49);
        RenderPreview(camera,"TownDistrictPreview.png",new Vector3(0,2,-20),16);
        TownBuilder.Build();
    }
    static void RenderPreview(Camera camera,string path,Vector3 position,float zoom)
    {
        if(SystemInfo.graphicsDeviceType==UnityEngine.Rendering.GraphicsDeviceType.Null)return;
        Vector3 old=camera.transform.position;float size=camera.orthographicSize;
        camera.transform.position=position;camera.orthographicSize=zoom;
        var target=new RenderTexture(1920,1200,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        var image=new Texture2D(1920,1200,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1920,1200),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());
        camera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);camera.transform.position=old;camera.orthographicSize=size;
    }
}
