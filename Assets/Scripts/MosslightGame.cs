using System;
using System.Collections.Generic;
using UnityEngine;

// A self-contained, asset-free prototype. Logical positions live on an isometric ground plane.
public sealed class MosslightGame : MonoBehaviour
{
    [Header("Player settings")]
    [Range(1,20)] public int maxHealth = 6;
    [Min(.1f)] public float moveSpeed = 3.25f;
    [Min(.1f)] public float dashSpeed = 12;
    [Min(.1f)] public float dashCooldown = 1.15f;
    [Min(.05f)] public float attackInterval = .38f;
    [Header("Editable scene references")]
    public Transform levelRoot;
    public Camera sceneCamera;
    public Sprite[] artwork;
    MosslightPlacement[] placements;
    Vector2 shrinePosition;
    Vector3 cameraOrigin;
    float cameraSize;
    int lightGoal = 12;
    public static Vector2 Project(Vector2 p) => new Vector2(p.x - p.y, (p.x + p.y) * .5f);
    public static Vector2 Unproject(Vector2 p) => new Vector2(p.y + p.x * .5f, p.y - p.x * .5f);
    const float Limit = 6.5f;
    class Actor { public Vector2 p; public SpriteRenderer body; public Transform shadow; public float hp = 3, timer, flash; public bool elite; }
    class Prop { public Vector2 p; public float radius; }
    class Drop { public Vector2 p; public SpriteRenderer body; }
    class Fx { public SpriteRenderer body; public Vector2 velocity; public float life, max; }
    readonly List<Actor> enemies = new List<Actor>();
    readonly List<Prop> obstacles = new List<Prop>();
    readonly List<Drop> drops = new List<Drop>();
    readonly List<Fx> effects = new List<Fx>();
    readonly List<GameObject> world = new List<GameObject>();
    readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
    System.Random rng;
    Camera cam;
    Actor hero;
    SpriteRenderer shrine;
    SpriteRenderer destinationMarker;
    readonly Queue<Vector2> movePath = new Queue<Vector2>();
    float nextPathTime;
    Vector2 facing = Vector2.down, dashDirection;
    float health, attackCD, dashCD, dashTime, hurtTime, clock, shake, messageTime;
    int wave, kills, crystals;
    bool dead, won, paused, started, smoke;
    string message = "";
    AudioSource speaker;
    AudioClip slashSound, hitSound, pickupSound;
    Texture2D white;
    GUIStyle label, title, small, button;
    readonly Color ink = new Color(.055f, .09f, .115f), mint = new Color(.58f,.91f,.7f);

    void Awake()
    {
        Application.targetFrameRate = 60;
        rng = new System.Random(23);
        cam = sceneCamera ? sceneCamera : new GameObject("Fixed quarter-view camera").AddComponent<Camera>();
        if (!sceneCamera) {
        cam.orthographic = true; cam.orthographicSize = 6.3f;
        cam.transform.position = new Vector3(0, 0, -20);
        cam.backgroundColor = new Color(.035f,.065f,.085f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.gameObject.AddComponent<AudioListener>();
        }
        cameraOrigin=cam.transform.position;cameraSize=cam.orthographicSize;
        speaker = gameObject.AddComponent<AudioSource>(); speaker.volume = .18f;
        slashSound = Tone(220, .10f); hitSound = Tone(110, .14f); pickupSound = Tone(740, .12f);
        white = Texture2D.whiteTexture;
        if(levelRoot) {
            foreach(var sprite in artwork)sprites[sprite.name]=sprite;
            placements=levelRoot.GetComponentsInChildren<MosslightPlacement>();
            lightGoal=0;
            foreach(var placement in placements)if(placement.kind==MosslightPlacement.Kind.EnemySpawn)lightGoal++;
        } else MakeSprites();
        ResetRun();
        if(TownVisit.saved)started=true;
        smoke = Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-test") >= 0;
        if (smoke) { started = true; SmokeTest(); }
    }

    AudioClip Tone(float hz, float length)
    {
        int n = (int)(22050 * length); var samples = new float[n];
        for (int i=0;i<n;i++) { float t = (float)i/n; samples[i] = Mathf.Sin(i * hz * 6.28318f / 22050 * (1 - t*.35f)) * (1-t) * .35f; }
        var clip = AudioClip.Create("Synth", n, 1, 22050, false); clip.SetData(samples,0); return clip;
    }
    void Sound(AudioClip clip) { if (!smoke) speaker.PlayOneShot(clip); }
    Sprite Pixels(string name, string[] rows, Dictionary<char, Color32> palette, float ppu = 16)
    {
        int h=rows.Length,w=rows[0].Length; var tex = new Texture2D(w,h,TextureFormat.RGBA32,false);
        tex.name=name; tex.filterMode=FilterMode.Point; tex.wrapMode=TextureWrapMode.Clamp;
        var colors=new Color32[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++) colors[(h-y-1)*w+x]=palette.TryGetValue(rows[y][x],out var c)?c:new Color32(0,0,0,0);
        tex.SetPixels32(colors); tex.Apply();
        var sprite=Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,0),ppu); sprites[name]=sprite; return sprite;
    }
    void MakeSprites()
    {
        var p=new Dictionary<char,Color32> {
            {'X',new Color32(24,37,43,255)}, {'g',new Color32(48,91,66,255)}, {'G',new Color32(78,130,77,255)},
            {'l',new Color32(134,175,92,255)}, {'b',new Color32(70,54,47,255)}, {'B',new Color32(117,86,61,255)},
            {'w',new Color32(237,234,192,255)}, {'s',new Color32(211,156,108,255)}, {'h',new Color32(114,66,53,255)},
            {'c',new Color32(64,142,139,255)}, {'C',new Color32(113,203,180,255)}, {'d',new Color32(41,66,80,255)},
            {'r',new Color32(194,92,87,255)}, {'R',new Color32(239,155,105,255)}, {'a',new Color32(107,123,128,255)},
            {'A',new Color32(168,180,166,255)}, {'t',new Color32(40,61,53,255)} };
        Pixels("hero",new[]{"......XXXX......",".....XhhhhX.....","....XhhhhhXX....","....XsssssXX....","....XsXssXsX....",".....XssssX.....","....XXCCCXX.....","...XcCCCCCcX....","...XsccCccsX....","...XscCCCCsX....","....XddddX......","....XdXXdX......","....Xb..bX......","...XXb..bXX....."},p);
        Pixels("slime",new[]{".....lllll......","...llGGGGGll....","..lGGGGGGGGGl...",".lGGGGGGGGGGGl..",".GGXwGGGXwGGGG..",".GGXXGGGXXGGGG..",".GGGGGGGGGGGGG..","..GGGgggggGGG...","...ggggggggg....","....XXXXXXX....."},p);
        Pixels("tree",new[]{"...........XX...........","..........XllX..........",".........XlGGlX.........","........XlGGGGlX........",".......XlGGGGGGlX.......","......XlGGGGGGGGlX......",".....XGGGGGGGGGGGGX.....","....XGGGlGGGGGGGGGGX....",".....XXGGGGGGGGGGXX.....","....XlGGGGGGGGGGGGGX....","...XlGGGGGGGGGGGGGGGX...","..XGGGGGGGGGGGGGGGGGGX..",".XGGGGGGGGGGGGGGGGGGGGX.","..XXXggggGGGGGGggggXXX..","....XggggggggggggggX....",".....XXXXggggggXXXX.....",".........XBBbX..........",".........XBBbX..........",".........XBBbX..........","........XBBBbbX........."},p,12);
        Pixels("rock",new[]{".....XXXXX......","...XXAAAAAXX....","..XAAAAAaaaAX...",".XAAAAAaaaaaaX..",".XAAaaaaaaaddX..","..XaaaaaadddX...","...XXXXXXXXX...."},p);
        Pixels("crystal",new[]{"....C....","...CwC...","..CwwwC..","..CwwCC..","..CCCCc..","...Ccc...","....c...."},p,20);
        Pixels("shrine",new[]{".......CC.......","......CwwC......","......CwwC......",".......CC.......","................",".....AAAAAA.....","....AaaaaaaA....","....AaCCCCaA....","....AaCwwCaA....","....AaCCCCaA....","....AaaaaaaA....","...AAaaaaaaAA...","..AAAAAAAAAAAA..","..adddddddddda..","...dddddddddd..."},p,12);
        Pixels("grass",new[]{"..l.....","..G..l..","G.G.G...",".GGG...."},p);
        Pixels("flower",new[]{"..w..",".wRw.","..G..",".GG.."},p);
        Pixels("spark",new[]{"ww","ww"},p,16);
        var tex=new Texture2D(32,16,TextureFormat.RGBA32,false); tex.filterMode=FilterMode.Point;
        for(int y=0;y<16;y++)for(int x=0;x<32;x++) {
            float d=Mathf.Abs((x-15.5f)/16)+Mathf.Abs((y-7.5f)/8);
            tex.SetPixel(x,y,d<1?Color.white:Color.clear);
        }
        tex.Apply(); sprites["tile"]=Sprite.Create(tex,new Rect(0,0,32,16),new Vector2(.5f,.5f),16);
        var st=new Texture2D(16,8,TextureFormat.RGBA32,false);st.filterMode=FilterMode.Point;
        for(int y=0;y<8;y++)for(int x=0;x<16;x++) st.SetPixel(x,y,Mathf.Pow((x-7.5f)/8,2)+Mathf.Pow((y-3.5f)/4,2)<1?new Color(0,0,0,.24f):Color.clear);
        st.Apply(); sprites["shadow"]=Sprite.Create(st,new Rect(0,0,16,8),new Vector2(.5f,.5f),16);
    }
    SpriteRenderer Render(string sprite, Vector2 screen, int order, Color? tint=null)
    {
        var go=new GameObject(sprite); world.Add(go); var r=go.AddComponent<SpriteRenderer>();
        r.sprite=sprites[sprite];r.sortingOrder=order;r.color=tint??Color.white;go.transform.position=screen;return r;
    }
    int Depth(Vector2 p) => 1000-Mathf.RoundToInt(Project(p).y*100);
    Actor NewActor(Vector2 p,bool player=false,bool elite=false)
    {
        var a=new Actor{p=p,body=Render(player?"hero":"slime",Project(p),Depth(p)),elite=elite,hp=elite?6:3};
        a.shadow=Render("shadow",Project(p),-2).transform;
        if(elite)a.body.transform.localScale=Vector3.one*1.4f;
        return a;
    }
    void ResetRun()
    {
        foreach(var go in world)if(go)Destroy(go);world.Clear();enemies.Clear();obstacles.Clear();drops.Clear();effects.Clear();
        rng=new System.Random(23);health=maxHealth;wave=0;kills=0;crystals=0;dead=false;won=false;paused=false;
        attackCD=dashCD=dashTime=hurtTime=clock=shake=0;
        movePath.Clear();nextPathTime=0;facing=Vector2.down;
        if(levelRoot) {
            hero=null;shrine=null;
            foreach(var placement in placements) {
                if(!placement || !placement.gameObject.activeInHierarchy)continue;
                Vector2 p=Unproject(placement.transform.position);
                var r=placement.GetComponent<SpriteRenderer>();
                switch(placement.kind) {
                    case MosslightPlacement.Kind.Obstacle:
                        obstacles.Add(new Prop{p=p,radius=placement.collisionRadius*Mathf.Max(Mathf.Abs(placement.transform.lossyScale.x),Mathf.Abs(placement.transform.lossyScale.y))});break;
                    case MosslightPlacement.Kind.PlayerStart:
                        hero=FromPlacement(placement,true);r.enabled=false;break;
                    case MosslightPlacement.Kind.EnemySpawn:r.enabled=false;break;
                    case MosslightPlacement.Kind.Shrine:shrine=r;shrinePosition=p;break;
                }
            }
            if(hero==null||!shrine)throw new InvalidOperationException("Scene needs a PlayerStart and Shrine placement.");
        } else {
        for(int x=-7;x<=7;x++)for(int y=-7;y<=7;y++) {
            float shade=(float)rng.NextDouble()*.035f;
            bool path=Mathf.Abs(x-y)<=1 || Mathf.Abs(x+y)<=1;
            Render("tile",Project(new Vector2(x,y)),-1000,new Color(path?.25f+shade:.13f+shade,path?.29f+shade:.23f+shade,path?.23f+shade:.19f+shade));
            if(!path&&rng.NextDouble()<.45) Render(rng.NextDouble()<.2?"flower":"grass",Project(new Vector2(x,y))+new Vector2(.2f,-.1f),-10);
        }
        for(int n=-7;n<=7;n+=2) { AddProp(new Vector2(n,-7),true);AddProp(new Vector2(-7,n),true);AddProp(new Vector2(n,7),true);AddProp(new Vector2(7,n),true); }
        foreach(var p in new[]{new Vector2(-4,-2),new Vector2(3,4),new Vector2(4,-3),new Vector2(-3,4)})AddProp(p,true);
        foreach(var p in new[]{new Vector2(2,-3),new Vector2(-4,3),new Vector2(4,1)})AddProp(p,false);
        shrine=Render("shrine",Project(Vector2.zero),Depth(Vector2.zero));
        hero=NewActor(new Vector2(-1.5f,-1.5f),true);
        }
        destinationMarker=Render("tile",Project(hero.p),-5,new Color(.58f,.91f,.7f,.65f));
        destinationMarker.transform.localScale=Vector3.one*.22f;destinationMarker.enabled=false;
        NextWave();Tell("The grove is fading. Recover its light.");
    }
    void AddProp(Vector2 p,bool tree)
    {
        obstacles.Add(new Prop{p=p,radius=tree?.45f:.42f});
        Render("shadow",Project(p),-3).transform.localScale=Vector3.one*(tree?1.7f:1);
        Render(tree?"tree":"rock",Project(p),Depth(p));
    }
    void NextWave()
    {
        wave++; int count=wave+2;
        if(levelRoot) {
            foreach(var placement in placements)
                if(placement&&placement.gameObject.activeInHierarchy&&placement.kind==MosslightPlacement.Kind.EnemySpawn&&placement.wave==wave)
                    enemies.Add(FromPlacement(placement));
        } else {
        for(int i=0;i<count;i++) {
            float a=i*Mathf.PI*2/count+wave*.55f;var p=new Vector2(Mathf.Cos(a),Mathf.Sin(a))*5;
            enemies.Add(NewActor(p,false,wave==3&&i==0));
        }
        }
        Tell("WAVE "+wave+" / 3   —   Clear the grove");
    }
    Actor FromPlacement(MosslightPlacement placement,bool player=false)
    {
        var actor=NewActor(Unproject(placement.transform.position),player,placement.elite);
        var source=placement.GetComponent<SpriteRenderer>();
        actor.body.sprite=source.sprite;actor.body.color=source.color;
        actor.body.transform.localScale=placement.transform.lossyScale;
        actor.body.transform.rotation=placement.transform.rotation;
        return actor;
    }

    // Called once by the editor migration, never during normal play or build.
    public Dictionary<string,Sprite> CreateEditableLayout()
    {
        rng=new System.Random(23);MakeSprites();ResetRun();
        var root=new GameObject("Level — edit objects here").transform;
        var ground=new GameObject("Ground").transform;ground.SetParent(root);
        var props=new GameObject("Scenery").transform;props.SetParent(root);
        var spawns=new GameObject("Enemy Spawns — Wave 1 to 3").transform;spawns.SetParent(root);
        foreach(var go in world) {
            go.transform.SetParent(go.name=="tile"||go.name=="grass"||go.name=="flower"?ground:props,true);
            if(go.name=="tree"||go.name=="rock") {
                var placement=go.AddComponent<MosslightPlacement>();placement.kind=MosslightPlacement.Kind.Obstacle;
                placement.collisionRadius=go.name=="tree"?.45f:.42f;
            }
        }
        hero.body.gameObject.name="Player Start — move to change spawn";
        hero.body.gameObject.AddComponent<MosslightPlacement>().kind=MosslightPlacement.Kind.PlayerStart;
        DestroyImmediate(hero.shadow.gameObject);
        shrine.gameObject.name="Shrine";shrine.gameObject.AddComponent<MosslightPlacement>().kind=MosslightPlacement.Kind.Shrine;
        for(int w=1;w<=3;w++) {
            if(w>1)NextWave();
            foreach(var enemy in enemies) {
                if(enemy.body.GetComponent<MosslightPlacement>())continue;
                enemy.body.name="Wave "+w+(enemy.elite?" — Elite Slime":" — Slime");
                enemy.body.transform.SetParent(spawns,true);
                var placement=enemy.body.gameObject.AddComponent<MosslightPlacement>();
                placement.kind=MosslightPlacement.Kind.EnemySpawn;placement.wave=w;placement.elite=enemy.elite;
                DestroyImmediate(enemy.shadow.gameObject);
            }
        }
        DestroyImmediate(destinationMarker.gameObject);
        levelRoot=root;
        sceneCamera=new GameObject("Main Camera").AddComponent<Camera>();sceneCamera.tag="MainCamera";
        sceneCamera.orthographic=true;sceneCamera.orthographicSize=8;
        sceneCamera.transform.position=new Vector3(0,0,-20);
        sceneCamera.backgroundColor=new Color(.035f,.065f,.085f);sceneCamera.clearFlags=CameraClearFlags.SolidColor;
        sceneCamera.gameObject.AddComponent<AudioListener>();
        return sprites;
    }
    void Tell(string text){message=text;messageTime=3.5f;}
    bool CanMove(Vector2 p)
    {
        if(Mathf.Abs(p.x)>Limit||Mathf.Abs(p.y)>Limit)return false;
        foreach(var o in obstacles)if(Vector2.Distance(p,o.p)<o.radius+.22f)return false;
        return true;
    }
    void Move(Actor a,Vector2 delta)
    {
        // Substeps prevent dash tunnelling through narrow obstacles.
        int steps=Mathf.Max(1,Mathf.CeilToInt(delta.magnitude/.12f));delta/=steps;
        for(int i=0;i<steps;i++) {
            var p=a.p+new Vector2(delta.x,0);if(CanMove(p))a.p=p;
            p=a.p+new Vector2(0,delta.y);if(CanMove(p))a.p=p;
        }
    }
    bool ClearSegment(Vector2 from,Vector2 to)
    {
        int steps=Mathf.Max(1,Mathf.CeilToInt(Vector2.Distance(from,to)/.08f));
        for(int i=0;i<=steps;i++)if(!CanMove(Vector2.Lerp(from,to,(float)i/steps)))return false;
        return true;
    }
    void StopMoving(){movePath.Clear();destinationMarker.enabled=false;}
    void SetDestination(Vector2 target)
    {
        target=new Vector2(Mathf.Clamp(target.x,-Limit,Limit),Mathf.Clamp(target.y,-Limit,Limit));
        StopMoving();
        // A small flood search routes clicks around trees and rocks, including blocked destinations.
        const int side=27;
        var previous=new int[side*side];for(int i=0;i<previous.Length;i++)previous[i]=-2;
        Func<int,Vector2> point=id=>new Vector2(id%side*.5f-Limit,id/side*.5f-Limit);
        int start=-1;float bestStart=float.MaxValue;
        for(int i=0;i<previous.Length;i++) {
            float d=(point(i)-hero.p).sqrMagnitude;
            if(d<bestStart&&ClearSegment(hero.p,point(i))){bestStart=d;start=i;}
        }
        if(start<0)return;
        var open=new Queue<int>();open.Enqueue(start);previous[start]=-1;
        int nearest=start;float best=(point(start)-target).sqrMagnitude;
        while(open.Count>0) {
            int current=open.Dequeue();float distance=(point(current)-target).sqrMagnitude;
            if(distance<best){best=distance;nearest=current;}
            for(int y=-1;y<=1;y++)for(int x=-1;x<=1;x++) {
                if(x==0&&y==0)continue;
                int nx=current%side+x,ny=current/side+y;
                if(nx<0||ny<0||nx>=side||ny>=side)continue;
                int next=ny*side+nx;
                if(previous[next]!=-2||!ClearSegment(point(current),point(next)))continue;
                previous[next]=current;open.Enqueue(next);
            }
        }
        var reversed=new List<Vector2>();
        for(int i=nearest;i>=0;i=previous[i])reversed.Add(point(i));
        reversed.Reverse();
        // Prefer a direct segment when possible and remove visible intermediate corners.
        Vector2 end=CanMove(target)&&ClearSegment(point(nearest),target)?target:point(nearest);
        reversed.Add(end);Vector2 anchor=hero.p;
        for(int i=0;i<reversed.Count;) {
            int far=i;for(int j=i+1;j<reversed.Count;j++)if(ClearSegment(anchor,reversed[j]))far=j;
            if(Vector2.Distance(anchor,reversed[far])>.04f){movePath.Enqueue(reversed[far]);anchor=reversed[far];}
            i=far+1;
        }
        destinationMarker.transform.position=Project(end);destinationMarker.enabled=movePath.Count>0;
    }
    bool PointerOverHud()
    {
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
        Vector2 p=new Vector2((Input.mousePosition.x-(Screen.width-1280*scale)*.5f)/scale,
            (Screen.height-Input.mousePosition.y-(Screen.height-800*scale)*.5f)/scale);
        return new Rect(24,24,270,104).Contains(p)||new Rect(1000,24,256,104).Contains(p)
            ||new Rect(260,733,760,43).Contains(p)||new Rect(440,668,400,34).Contains(p)
            ||(messageTime>0&&new Rect(325,146,630,36).Contains(p));
    }
    void Update()
    {
        if(smoke)return;
        if(Input.GetKeyDown(KeyCode.Return)&&!started)started=true;
        if(Input.GetKeyDown(KeyCode.R)){ResetRun();started=true;}
        if(Input.GetKeyDown(KeyCode.Escape)&&started&&!dead&&!won)paused=!paused;
        if(!started||paused||dead||won)return;
        float dt=Mathf.Min(Time.deltaTime,.04f);clock+=dt;messageTime-=dt;attackCD-=dt;dashCD-=dt;hurtTime-=dt;
        Vector2 mouse=cam.ScreenToWorldPoint(Input.mousePosition);
        bool worldPointer=!PointerOverHud();
        bool shrineHover=shrine.bounds.Contains(new Vector3(mouse.x,mouse.y,shrine.transform.position.z));
        bool shrineClick=worldPointer&&shrineHover&&Input.GetMouseButtonDown(0);
        bool interact=Input.GetKeyDown(KeyCode.E)||shrineClick;
        if(worldPointer&&Input.GetMouseButton(1)&&(Input.GetMouseButtonDown(1)||clock>=nextPathTime)) {
            SetDestination(Unproject(mouse));nextPathTime=clock+.18f;
        }
        var direction=Vector2.zero;
        while(movePath.Count>0&&Vector2.Distance(hero.p,movePath.Peek())<.04f)movePath.Dequeue();
        if(movePath.Count>0)direction=(movePath.Peek()-hero.p).normalized;
        else destinationMarker.enabled=false;
        if(direction.sqrMagnitude>0)facing=Project(direction).normalized;
        if((Input.GetKeyDown(KeyCode.Space)||(worldPointer&&Input.GetMouseButtonDown(2)))&&dashCD<=0){
            dashTime=.18f;dashCD=dashCooldown;
            dashDirection=Unproject(mouse-Project(hero.p)).normalized;
            if(dashDirection.sqrMagnitude<.001f)dashDirection=Unproject(facing).normalized;
            StopMoving();
        }
        if(dashTime>0){dashTime-=dt;Move(hero,dashDirection*dashSpeed*dt);Burst(Project(hero.p)+Vector2.up*.35f,mint,1);}
        else {
            float step=moveSpeed*dt;if(movePath.Count>0)step=Mathf.Min(step,Vector2.Distance(hero.p,movePath.Peek()));
            Move(hero,direction*step);
        }
        if((worldPointer&&Input.GetMouseButton(0)&&!shrineHover)||Input.GetKey(KeyCode.J)) {
            StopMoving();
            if(Input.GetMouseButton(0)&&(mouse-Project(hero.p)).sqrMagnitude>.001f)facing=(mouse-Project(hero.p)).normalized;
            if(attackCD<=0)Attack();
        }
        if(shrineClick){StopMoving();if(Vector2.Distance(hero.p,shrinePosition)>=1.4f)Tell("Move closer to the shrine, then click it.");}
        foreach(var e in enemies.ToArray()) {
            e.timer-=dt;e.flash-=dt;
            float distance=Vector2.Distance(e.p,hero.p);
            if(e.timer<=.35f&&e.timer>0) { e.body.color=new Color(1,.65f,.5f); }
            else {
                Move(e,(hero.p-e.p).normalized*(e.elite?1.3f:.95f)*dt);
                e.body.color=e.flash>0?Color.white:e.elite?new Color(.85f,.65f,1):Color.white;
            }
            if(distance<.8f&&e.timer<=0) { e.timer=.65f; }
            if(e.timer>0&&e.timer-dt<=.32f&&e.timer>.32f&&distance<1.05f) DamageHero();
            Position(e,Mathf.Abs(Mathf.Sin(clock*5+e.p.x))*.065f);
        }
        foreach(var d in drops.ToArray()) {
            float distance=Vector2.Distance(d.p,hero.p);
            if(distance<1.9f)d.p=Vector2.MoveTowards(d.p,hero.p,dt*5);
            d.body.transform.position=Project(d.p)+Vector2.up*(.18f+Mathf.Sin(clock*5)*.05f);
            if(distance<.4f){crystals++;Sound(pickupSound);Destroy(d.body.gameObject);drops.Remove(d);}
        }
        if(enemies.Count==0&&drops.Count==0) {
            if(wave<3){health=Mathf.Min(maxHealth,health+1);NextWave();}
            else if(Vector2.Distance(hero.p,shrinePosition)<1.4f&&interact){won=true;Burst(Project(hero.p),mint,30);}
            else {message="The light is gathered. Return and left-click the shrine.";messageTime=1;}
        }
        if(interact&&enemies.Count>0&&Vector2.Distance(hero.p,shrinePosition)<1.4f)Tell("Clear all three waves to restore the shrine.");
        for(int i=effects.Count-1;i>=0;i--) {
            var f=effects[i];f.life-=dt;
            if(f.life<=0){Destroy(f.body.gameObject);effects.RemoveAt(i);continue;}
            f.body.transform.position+=(Vector3)(f.velocity*dt);var color=f.body.color;color.a=f.life/f.max;f.body.color=color;
        }
        hero.body.color=hurtTime>0&&Mathf.Sin(clock*50)>0?new Color(1,.45f,.4f):Color.white;
        Position(hero,direction.sqrMagnitude>0?Mathf.Abs(Mathf.Sin(clock*13))*.055f:0);
        hero.body.flipX=facing.x<-.1f;
        shrine.color=enemies.Count==0&&wave==3?new Color(1,1,.7f):Color.white;
        shake=Mathf.Max(0,shake-dt);var target=(Vector2)cameraOrigin+Project(hero.p)*.28f;
        cam.transform.position=Vector3.Lerp(cam.transform.position,new Vector3(target.x,target.y,-20),dt*4)+new Vector3(Mathf.Sin(clock*130),Mathf.Cos(clock*110),0)*shake*.08f;
        cam.orthographicSize=Mathf.Max(cameraSize,9f/cam.aspect);
    }
    void Position(Actor a,float bob)
    {
        Vector2 pos=Project(a.p);pos.x=Mathf.Round(pos.x*32)/32;pos.y=Mathf.Round(pos.y*32)/32;
        a.body.transform.position=pos+Vector2.up*bob;a.body.sortingOrder=Depth(a.p);a.shadow.position=Project(a.p);
    }
    void Attack()
    {
        attackCD=attackInterval;Sound(slashSound);
        for(int i=0;i<13;i++) {
            float angle=Mathf.Atan2(facing.y,facing.x)+Mathf.Lerp(-1.1f,1.1f,i/12f);
            Vector2 offset=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*.95f;
            var r=Render("spark",Project(hero.p)+Vector2.up*.3f+offset,2000,new Color(.95f,.89f,.62f));
            effects.Add(new Fx{body=r,life=.14f,max=.14f,velocity=offset*.6f});
        }
        foreach(var e in enemies.ToArray()) {
            var diff=Project(e.p)-Project(hero.p);
            if(Vector2.Distance(e.p,hero.p)>1.65f || (diff.magnitude>.25f&&Vector2.Dot(diff.normalized,facing)<-.15f))continue;
            e.hp--;e.flash=.12f;Move(e,(e.p-hero.p).normalized*.35f);Burst(Project(e.p)+Vector2.up*.3f,new Color(.7f,.88f,.42f),7);Sound(hitSound);shake=.12f;
            if(e.hp<=0){kills++;drops.Add(new Drop{p=e.p,body=Render("crystal",Project(e.p),1500)});Destroy(e.body.gameObject);Destroy(e.shadow.gameObject);enemies.Remove(e);}
        }
    }
    void DamageHero()
    {
        if(hurtTime>0||dashTime>0)return;
        health--;hurtTime=.8f;shake=.3f;Sound(hitSound);Burst(Project(hero.p)+Vector2.up*.4f,new Color(1,.4f,.3f),8);
        if(health<=0)dead=true;
    }
    void Burst(Vector2 at,Color color,int count)
    {
        for(int i=0;i<count;i++) {
            float angle=(float)rng.NextDouble()*Mathf.PI*2;var r=Render("spark",at,2001,color);
            effects.Add(new Fx{body=r,velocity=new Vector2(Mathf.Cos(angle),Mathf.Sin(angle))*(.5f+(float)rng.NextDouble()*2),life=.3f,max=.3f});
        }
    }
    void SmokeTest()
    {
        try {
            if(levelRoot) {
                var spawn=Array.Find(placements,p=>p.kind==MosslightPlacement.Kind.PlayerStart);
                Vector3 saved=spawn.transform.position;spawn.transform.position+=new Vector3(.1f,.1f,0);
                ResetRun();
                if(Vector2.Distance(hero.p,Unproject(spawn.transform.position))>.001f)throw new Exception("Edited player spawn ignored");
                spawn.transform.position=saved;
                var prop=Array.Find(placements,p=>p.kind==MosslightPlacement.Kind.Obstacle);
                saved=prop.transform.position;Vector3 scale=prop.transform.localScale;
                prop.transform.position+=Vector3.right*.25f;prop.transform.localScale*=1.5f;
                ResetRun();
                var obstacle=obstacles.Find(p=>Vector2.Distance(p.p,Unproject(prop.transform.position))<.001f);
                if(obstacle==null||Mathf.Abs(obstacle.radius-prop.collisionRadius*Mathf.Max(Mathf.Abs(prop.transform.lossyScale.x),Mathf.Abs(prop.transform.lossyScale.y)))>.001f)
                    throw new Exception("Edited obstacle transform ignored");
                prop.transform.position=saved;prop.transform.localScale=scale;ResetRun();
                Debug.Log("MOSSLIGHT_EDITABLE_RUNTIME_OK: edited spawn and obstacle transforms survive reset");
            }
            if(enemies.Count!=3)throw new Exception("Initial wave");
            var original=hero.p;Move(hero,new Vector2(100,0));if(Mathf.Abs(hero.p.x)>Limit)throw new Exception("Boundary collision");hero.p=original;
            health=6;dashTime=.1f;DamageHero();if(health!=6)throw new Exception("Dash immunity");dashTime=0;DamageHero();DamageHero();if(health!=5)throw new Exception("Damage cooldown");
            foreach(var e in enemies)e.p=hero.p+new Vector2(.2f,.2f);
            facing=Vector2.up;for(int n=0;n<3;n++) {foreach(var e in enemies)e.p=hero.p+new Vector2(.2f,.2f);Attack();}
            if(kills!=3||drops.Count!=3)throw new Exception("Combat and loot");
            NextWave();if(enemies.Count!=4)throw new Exception("Second wave");
            ResetRun();if(health!=6||kills!=0||drops.Count!=0||enemies.Count!=3)throw new Exception("Restart");
            Debug.Log("MOSSLIGHT_SMOKE_OK: movement bounds, dash immunity, damage cooldown, combat, loot, waves, restart");Application.Quit(0);
        } catch(Exception e){Debug.LogException(e);Application.Quit(1);}
    }
    void Box(Rect r,Color c){GUI.color=c;GUI.DrawTexture(r,white);GUI.color=Color.white;}
    public void ReturnToTown(){UnityEngine.SceneManagement.SceneManager.LoadScene("Town");}
    void Text(Rect r,string text,GUIStyle style,Color? color=null){var old=GUI.color;GUI.color=color??Color.white;GUI.Label(r,text,style);GUI.color=old;}
    void OnGUI()
    {
        if(label==null) {
            label=new GUIStyle(GUI.skin.label){fontSize=18,fontStyle=FontStyle.Bold};
            small=new GUIStyle(GUI.skin.label){fontSize=13};
            title=new GUIStyle(GUI.skin.label){fontSize=48,fontStyle=FontStyle.Bold,alignment=TextAnchor.MiddleCenter};
            button=new GUIStyle(GUI.skin.button){fontSize=18,fontStyle=FontStyle.Bold};
        }
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)*.5f,(Screen.height-800*scale)*.5f,0),Quaternion.identity,Vector3.one*scale);
        Box(new Rect(24,24,270,104),ink);Box(new Rect(24,24,4,104),mint);
        Text(new Rect(44,34,240,28),"MOSSLIGHT",label,mint);
        Text(new Rect(44,60,240,20),"WARDEN OF THE LITTLE GROVE",small,new Color(.6f,.7f,.72f));
        for(int i=0;i<maxHealth;i++){Box(new Rect(44+i*216f/maxHealth,91,210f/maxHealth,12),new Color(.18f,.25f,.27f));if(i<health)Box(new Rect(44+i*216f/maxHealth,91,210f/maxHealth,12),new Color(.89f,.43f,.37f));}
        Box(new Rect(1000,24,256,104),ink);
        Text(new Rect(1020,35,220,28),"THE FADING GROVE",label,mint);
        Text(new Rect(1020,66,220,24),"Wave  "+wave+" / 3       Foes  "+enemies.Count,small);
        Text(new Rect(1020,90,220,22),"Light recovered    "+crystals+" / "+lightGoal,small,new Color(.92f,.8f,.5f));
        Box(new Rect(260,733,760,43),ink);
        Text(new Rect(282,744,740,26),"RMB  Move     LMB  Attack / Shrine     MMB / SPACE  Dash     ESC  Pause",small);
        Box(new Rect(540,695,200,7),new Color(.15f,.22f,.24f));Box(new Rect(540,695,200*Mathf.Clamp01(1-dashCD/dashCooldown),7),mint);
        var center=new GUIStyle(small){alignment=TextAnchor.MiddleCenter};
        Text(new Rect(440,668,400,24),dashCD<=0?"DASH READY":"RECOVERING",center,mint);
        if(messageTime>0&&started&&!dead&&!won) {Box(new Rect(325,146,630,36),ink);Text(new Rect(335,153,610,28),message,center);}
        if(!started||paused||dead||won) {
            Box(new Rect(0,0,1280,800),new Color(.025f,.05f,.065f,.88f));
            Text(new Rect(240,208,800,32),"A SMALL ISOMETRIC ACTION RPG",center,mint);
            Text(new Rect(190,253,900,80),!started?"MOSSLIGHT":paused?"TAKE A BREATH":won?"THE GROVE AWAKENS":"YOUR LIGHT FADES",title);
            string desc=!started?"Three waves. Twelve fragments. One forgotten shrine.":paused?"The grove will wait for you.":won?"You brought the light home. Thank you, warden.":"Try again. Dash through danger and strike between attacks.";
            Text(new Rect(240,354,800,34),desc,center,new Color(.8f,.85f,.82f));
            Text(new Rect(300,409,680,70),"Right click / Hold  •  Move     Left click / Hold  •  Attack\nMiddle click / Space  •  Dash toward cursor     Click shrine  •  Restore\nOne heart recovered after each wave.",new GUIStyle(center){fontSize=16});
            if(GUI.Button(new Rect(470,520,340,52),!started?"ENTER THE GROVE":paused?"RESUME":"PLAY AGAIN",button)) {
                if(paused)paused=false;else {ResetRun();started=true;}
            }
            Text(new Rect(400,590,480,28),"R  Restart anytime    /    Prototype 01",center,new Color(.5f,.62f,.64f));
            if(TownVisit.saved&&GUI.Button(new Rect(470,637,340,48),"RETURN TO TOWN",button))ReturnToTown();
        }
    }
}

