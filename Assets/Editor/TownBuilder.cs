using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Build.Reporting;

public static class TownBuilder
{
    const string ScenePath="Assets/Scenes/Town.unity";
    public sealed class Canvas
    {
        public Texture2D texture;
        readonly int width,height;
        public Canvas(int w,int h){width=w;height=h;texture=new Texture2D(w,h,TextureFormat.RGBA32,false);texture.filterMode=FilterMode.Point;texture.SetPixels(new Color[w*h]);}
        public void Rect(int x,int y,int w,int h,Color color){for(int j=y;j<y+h;j++)for(int i=x;i<x+w;i++)if(i>=0&&j>=0&&i<width&&j<height)texture.SetPixel(i,j,color);}
        public void Poly(Color color,params Vector2[] points)
        {
            for(int y=0;y<height;y++)for(int x=0;x<width;x++) {
                bool inside=false;for(int i=0,j=points.Length-1;i<points.Length;j=i++)
                    if((points[i].y>y)!=(points[j].y>y)&&x<(points[j].x-points[i].x)*(y-points[i].y)/(points[j].y-points[i].y)+points[i].x)inside=!inside;
                if(inside)texture.SetPixel(x,y,color);
            }
        }
        public void Line(int x,int y,int xx,int yy,Color c,int thickness=1)
        {
            int n=Mathf.Max(Mathf.Abs(xx-x),Mathf.Abs(yy-y));
            for(int i=0;i<=n;i++)Rect(Mathf.RoundToInt(Mathf.Lerp(x,xx,(float)i/Mathf.Max(1,n))),Mathf.RoundToInt(Mathf.Lerp(y,yy,(float)i/Mathf.Max(1,n))),thickness,thickness,c);
        }
    }
    public static Color C(string hex){ColorUtility.TryParseHtmlString("#"+hex,out var c);return c;}
    public static Vector2 V(int x,int y)=>new Vector2(x,y);
    public static Sprite Save(string name,Canvas canvas,Vector2 pivot,float ppu)
    {
        string path="Assets/Town/Art/"+name+".asset";
        var existing=AssetDatabase.LoadAllAssetsAtPath(path);foreach(var a in existing)if(a is Sprite s)return s;
        canvas.texture.Apply();canvas.texture.name=name+" Texture";
        var sprite=Sprite.Create(canvas.texture,new Rect(0,0,canvas.texture.width,canvas.texture.height),pivot,ppu);sprite.name=name;
        AssetDatabase.CreateAsset(canvas.texture,path);AssetDatabase.AddObjectToAsset(sprite,canvas.texture);return sprite;
    }
    public static Sprite Building(string name,int type)
    {
        var d=new Canvas(96,112);Color timber=C("493b35"),light=C("d4c095"),side=C("988575"),roof=C(type==0?"895449":type==1?"607889":"568674");
        d.Poly(C("283b37"),V(2,12),V(48,0),V(95,13),V(51,29));
        d.Poly(light,V(8,19),V(51,8),V(51,51),V(8,65));
        d.Poly(side,V(51,8),V(87,22),V(87,65),V(51,51));
        d.Poly(C("e3cf9e"),V(8,64),V(30,86),V(51,50));
        d.Poly(roof,V(3,66),V(29,94),V(94,72),V(53,47));
        d.Poly(C(type==0?"5b3e3b":type==1?"3b5065":"335e57"),V(29,94),V(94,72),V(88,64),V(29,85));
        for(int i=0;i<5;i++)d.Line(10+i*6,68+i*5,55+i*7,52+i*4,C(type==0?"b4775e":type==1?"879b9f":"78a58a"));
        d.Line(8,20,51,9,timber,3);d.Line(51,9,86,22,timber,3);
        d.Line(8,41,51,28,timber,2);d.Line(51,28,86,43,timber,2);
        d.Line(8,19,8,63,timber,3);d.Line(50,8,50,51,timber,3);d.Line(85,22,85,64,timber,3);
        d.Line(11,44,46,32,timber,2);d.Line(11,22,46,50,timber,2);
        d.Rect(29,15,15,30,timber);d.Rect(32,17,9,25,C("725744"));d.Rect(39,27,2,2,C("e9cc79"));
        d.Rect(14,43,10,13,timber);d.Rect(16,45,6,9,C("e7bd75"));d.Rect(19,45,1,9,timber);
        d.Rect(63,32,13,18,timber);d.Rect(65,34,9,14,C("e7bd75"));d.Rect(69,34,1,14,timber);d.Rect(65,40,9,1,timber);
        if(type==0) {
            d.Rect(68,76,12,26,C("706e66"));d.Rect(66,98,16,5,C("b1a78c"));
            d.Rect(74,105,7,3,C("9baba0"));d.Rect(80,109,9,2,C("b5bcb0"));
            d.Rect(56,15,26,17,C("453a37"));d.Rect(59,17,20,12,C("d96a34"));d.Rect(64,18,11,7,C("f2c06b"));
            d.Poly(C("92999b"),V(7,12),V(25,12),V(29,19),V(2,19));d.Rect(12,6,9,8,C("626d74"));
        } else if(type==1) {
            d.Rect(17,62,21,30,C("c4bda6"));d.Rect(21,71,13,16,C("4e656d"));
            d.Poly(C("677f8a"),V(13,91),V(28,107),V(43,91));
            d.Rect(27,99,3,13,C("efd796"));d.Rect(23,106,11,3,C("efd796"));
            d.Rect(24,75,7,9,C("dec887"));d.Rect(25,58,5,11,C("efdfb8"));d.Rect(22,62,11,3,C("efdfb8"));
        } else {
            d.Poly(C("e2c583"),V(8,33),V(50,21),V(56,36),V(13,49));
            for(int i=0;i<4;i++)d.Line(14+i*10,45-i*3,10+i*10,32-i*3,C("ad6656"),4);
            d.Rect(10,10,13,10,C("987043"));d.Rect(11,21,5,4,C("adbd65"));d.Rect(18,19,5,4,C("d29756"));
            d.Rect(72,12,13,10,C("987043"));d.Rect(73,23,10,4,C("b7be72"));
        }
        return Save(name,d,new Vector2(.5f,.15f),20);
    }
    public static Sprite Prop(string name,bool well)
    {
        var d=new Canvas(32,40);
        if(well){d.Poly(C("87918c"),V(2,8),V(16,1),V(30,8),V(30,18),V(16,25),V(2,18));d.Poly(C("344f5b"),V(5,18),V(16,23),V(27,18),V(16,13));d.Rect(4,16,3,20,C("7d644e"));d.Rect(26,16,3,20,C("7d644e"));d.Line(4,35,28,35,C("bb9b65"),3);d.Rect(16,17,1,18,C("d3bd8b"));}
        else{d.Rect(14,0,3,29,C("604b3b"));d.Rect(10,25,11,12,C("433e36"));d.Rect(12,27,7,8,C("f2cf84"));d.Rect(15,27,1,8,C("a77b4b"));d.Rect(9,37,13,2,C("737568"));}
        return Save(name,d,new Vector2(.5f,0),16);
    }
    public static Sprite AssetSprite(string path){foreach(var asset in AssetDatabase.LoadAllAssetsAtPath(path))if(asset is Sprite s)return s;throw new Exception("Sprite missing "+path);}
    public static SpriteRenderer Place(string name,Sprite sprite,Vector2 p,Transform parent,int order=0)
    {
        var go=new GameObject(name);go.transform.SetParent(parent);go.transform.position=p;
        var r=go.AddComponent<SpriteRenderer>();r.sprite=sprite;r.sortingOrder=order==0?1000-Mathf.RoundToInt(p.y*100):order;return r;
    }
    public static void Sign(string name,Vector2 p,Transform parent)
    {
        var go=new GameObject(name+" sign");go.transform.SetParent(parent);go.transform.position=p;
        var text=go.AddComponent<TextMesh>();text.text=name;text.fontSize=32;text.characterSize=.095f;text.anchor=TextAnchor.MiddleCenter;text.color=C("edd8a2");
        text.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");go.GetComponent<MeshRenderer>().sharedMaterial=text.font.material;go.GetComponent<MeshRenderer>().sortingOrder=3000;
    }
    public static void CreateAndBuild()
    {
        Directory.CreateDirectory("Assets/Town/Art");Directory.CreateDirectory("Assets/Town/Prefabs");
        if(!File.Exists(ScenePath)) {
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            var game=new GameObject("Town — Settings").AddComponent<TownGame>();
            var root=new GameObject("Town — editable map").transform;
            var ground=new GameObject("Ground and roads").transform;ground.SetParent(root);
            var buildings=new GameObject("Buildings").transform;buildings.SetParent(root);
            var people=new GameObject("NPCs — edit dialogue and prices").transform;people.SetParent(root);
            var decor=new GameObject("Trees and lamps").transform;decor.SetParent(root);
            var tile=AssetSprite("Assets/Art/tile.asset");var tree=AssetSprite("Assets/Art/tree.asset");var human=AssetSprite("Assets/Art/hero.asset");
            var grass=AssetSprite("Assets/Art/grass.asset");var random=new System.Random(61);
            for(int x=-9;x<=9;x++)for(int y=-9;y<=9;y++) {
                var p=MosslightGame.Project(new Vector2(x,y));bool road=Mathf.Abs(x-y)<=1||Mathf.Abs(x+y)<=1||Mathf.Abs(p.y-1)<.8f||p.magnitude<3;
                float shade=(float)random.NextDouble()*.04f;
                var r=Place("Tile "+x+", "+y,tile,p,ground,-1000);r.color=road?new Color(.44f+shade,.40f+shade,.31f+shade):new Color(.22f+shade,.32f+shade,.23f+shade);
                if(!road&&random.NextDouble()<.25)Place("Grass",grass,p,decor,-900);
            }
            var lamp=Prop("Lantern",false);
            for(int i=-8;i<=8;i+=4) {
                foreach(int side in new[]{-1,1}) {
                    var p=MosslightGame.Project(new Vector2(i,side*9));var r=Place("Oak",tree,p,decor);r.gameObject.AddComponent<TownObstacle>().groundSize=Vector2.one*.4f;
                    p=MosslightGame.Project(new Vector2(side*9,i));r=Place("Oak",tree,p,decor);r.gameObject.AddComponent<TownObstacle>().groundSize=Vector2.one*.4f;
                }
            }
            foreach(var p in new[]{new Vector2(-3,-2),new Vector2(3,-2),new Vector2(-3,2),new Vector2(3,2)})Place("Lantern",lamp,p,decor);
            var well=Place("Village well",Prop("Well",true),new Vector2(0,-1),decor);well.gameObject.AddComponent<TownObstacle>().groundSize=Vector2.one*.7f;
            string[] names={"BLACKSMITH","CHURCH","GENERAL STORE"};
            string[] npcNames={"대장장이 · 브람","사제 · 엘린","잡화상 · 미라"};
            string[] greetings={"좋은 검은 오래 버티는 법이지. 40골드면 검을 한 단계 강화해 주겠네. 최대 +3까지 가능하네.","돌빛 마을에 오신 것을 환영합니다. 다친 곳은 무료로 치료해 드릴게요. 축복을 받으면 최대 체력이 늘어납니다.","여행 준비는 여기서! 회복 물약은 15골드예요. 한 병으로 체력을 3 회복할 수 있어요."};
            Vector2[] locations={new Vector2(-7,1),new Vector2(0,5),new Vector2(7,1)};
            for(int i=0;i<3;i++) {
                var r=Place(names[i],Building(names[i],i),locations[i],buildings);r.gameObject.AddComponent<TownObstacle>().groundSize=new Vector2(2.6f,2.6f);
                Sign(names[i],locations[i]+new Vector2(0,3.4f),r.transform);
                PrefabUtility.SaveAsPrefabAsset(r.gameObject,"Assets/Town/Prefabs/"+names[i]+".prefab");
                var npc=Place(npcNames[i],human,locations[i]+new Vector2(0,-1.9f),people);npc.transform.localScale=Vector3.one*1.15f;
                npc.color=i==0?new Color(1,.69f,.48f):i==1?new Color(.82f,.88f,1):new Color(1,.76f,.85f);
                var behavior=npc.gameObject.AddComponent<TownNpc>();behavior.service=(TownNpc.Service)i;behavior.displayName=npcNames[i];behavior.greeting=greetings[i];behavior.price=i==0?40:i==1?0:15;
                PrefabUtility.SaveAsPrefabAsset(npc.gameObject,"Assets/Town/Prefabs/NPC_"+i+".prefab");
            }
            game.player=Place("Player — editable start",human,new Vector2(0,-5),root).transform;
            game.moveMarker=Place("Move destination",tile,Vector2.zero,root,-800);game.moveMarker.transform.localScale=Vector3.one*.2f;game.moveMarker.color=C("e6c77e");game.moveMarker.enabled=false;
            var camera=new GameObject("Main Camera").AddComponent<Camera>();camera.tag="MainCamera";camera.orthographic=true;camera.orthographicSize=12;
            camera.transform.position=new Vector3(0,1,-20);camera.backgroundColor=C("172720");camera.clearFlags=CameraClearFlags.SolidColor;camera.gameObject.AddComponent<AudioListener>();game.townCamera=camera;
            AssetDatabase.SaveAssets();EditorSceneManager.SaveScene(scene,ScenePath);
        }
        EditorSceneManager.OpenScene(ScenePath);
        var town=UnityEngine.Object.FindAnyObjectByType<TownGame>();
        if(!town||FindNpcCount()!=3)throw new Exception("Town scene validation");
        foreach(var r in UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))if(!r.sprite||!AssetDatabase.Contains(r.sprite))throw new Exception("Unsaved art");
        if(SystemInfo.graphicsDeviceType!=UnityEngine.Rendering.GraphicsDeviceType.Null) {
            var target=new RenderTexture(1440,900,24);town.townCamera.targetTexture=target;town.townCamera.Render();RenderTexture.active=target;
            var image=new Texture2D(1440,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1440,900),0,0);image.Apply();File.WriteAllBytes("TownPreview.png",image.EncodeToPNG());
            town.townCamera.targetTexture=null;RenderTexture.active=null;UnityEngine.Object.DestroyImmediate(target);UnityEngine.Object.DestroyImmediate(image);
        }
        Build();
    }
    static int FindNpcCount()=>UnityEngine.Object.FindObjectsByType<TownNpc>(FindObjectsSortMode.None).Length;
    [MenuItem("Mosslight/Build town")]
    public static void Build()
    {
        EditorBuildSettings.scenes=new[]{new EditorBuildSettingsScene(ScenePath,true),new EditorBuildSettingsScene("Assets/Scenes/Mosslight.unity",true)};
        PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=800;PlayerSettings.fullScreenMode=FullScreenMode.Windowed;PlayerSettings.resizableWindow=true;
        var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions{scenes=new[]{ScenePath,"Assets/Scenes/Mosslight.unity"},locationPathName="Build/Town/Stonehaven.exe",target=BuildTarget.StandaloneWindows64,options=BuildOptions.Development});
        if(report.summary.result!=BuildResult.Succeeded)throw new Exception("Town build failed");
        Debug.Log("TOWN_BUILD_OK");
    }
}

