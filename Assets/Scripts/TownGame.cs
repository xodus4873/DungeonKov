using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class TownGame : MonoBehaviour
{
    public Camera townCamera;
    public Transform player;
    public SpriteRenderer moveMarker;
    [Min(.1f)] public float moveSpeed=3.25f;
    [Range(1,20)] public int startingHealth=4;
    [Min(0)] public int startingGold=120;
    TownNpc[] npcs;
    TownObstacle[] obstacles;
    Vector4[] collisionBounds;
    readonly Queue<Vector2> path=new Queue<Vector2>();
    Vector2 position;
    TownNpc pending, talking;
    TownPortal portal;
    bool portalPending,portalDialog;
    int gold,health,maxHealth=6,potions,sword;
    bool blessed;
    float retarget;
    string notice="우클릭으로 이동하세요. NPC를 좌클릭하면 다가가 대화합니다.";
    GUIStyle text,title,small,button;
    [Header("Map and camera")]
    [Min(9)] public float mapLimit=36;
    public bool followPlayer=true;
    [Range(6,20)] public float cameraZoom=9;
    float Limit=>mapLimit;
    bool[] navigation;
    int gridSize;
    bool mapOpen;
    void Awake()
    {
        npcs=FindObjectsByType<TownNpc>(FindObjectsSortMode.None);
        obstacles=FindObjectsByType<TownObstacle>(FindObjectsSortMode.None);
        collisionBounds=new Vector4[obstacles.Length];
        for(int i=0;i<obstacles.Length;i++) {
            var o=obstacles[i];Vector2 center=MosslightGame.Unproject(o.transform.position);
            Vector2 half=o.groundSize*.5f*Mathf.Max(Mathf.Abs(o.transform.lossyScale.x),Mathf.Abs(o.transform.lossyScale.y))+Vector2.one*.22f;
            collisionBounds[i]=new Vector4(center.x-half.x,center.y-half.y,center.x+half.x,center.y+half.y);
        }
        position=MosslightGame.Unproject(player.position);gold=startingGold;health=Mathf.Min(startingHealth,maxHealth);
        portal=FindAnyObjectByType<TownPortal>();
        if(TownVisit.saved) {
            gold=TownVisit.gold;health=TownVisit.health;maxHealth=TownVisit.maxHealth;
            potions=TownVisit.potions;sword=TownVisit.sword;blessed=TownVisit.blessed;
            if(portal){position=MosslightGame.Unproject(portal.transform.position)-Vector2.one*2;player.position=MosslightGame.Project(position);}
            notice="포탈 광장으로 돌아왔습니다.";
        }
        moveMarker.enabled=false;
        gridSize=Mathf.RoundToInt(Limit*4)+1;
        navigation=new bool[gridSize*gridSize];
        for(int i=0;i<navigation.Length;i++)navigation[i]=Walkable(Node(i));
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--town-smoke-test")>=0)SmokeTest();
    }
    bool Walkable(Vector2 p)
    {
        if(Mathf.Abs(p.x)>Limit||Mathf.Abs(p.y)>Limit)return false;
        foreach(var b in collisionBounds)if(p.x>b.x&&p.x<b.z&&p.y>b.y&&p.y<b.w)return false;
        return true;
    }
    bool Segment(Vector2 a,Vector2 b)
    {
        if(!Walkable(a)||!Walkable(b))return false;
        // Exact segment/AABB intersection avoids thousands of sampled obstacle queries.
        Vector2 delta=b-a;
        foreach(var box in collisionBounds) {
            float enter=0,leave=1;
            if(ClipAxis(a.x,delta.x,box.x,box.z,ref enter,ref leave)&&ClipAxis(a.y,delta.y,box.y,box.w,ref enter,ref leave))return false;
        }
        return true;
    }
    static bool ClipAxis(float start,float delta,float min,float max,ref float enter,ref float leave)
    {
        if(Mathf.Abs(delta)<.000001f)return start>min&&start<max;
        float a=(min-start)/delta,b=(max-start)/delta;
        if(a>b){float temp=a;a=b;b=temp;}
        enter=Mathf.Max(enter,a);leave=Mathf.Min(leave,b);return enter<leave;
    }
    void Go(Vector2 target)
    {
        path.Clear();moveMarker.enabled=false;
        int size=gridSize;
        Func<int,Vector2> at=i=>new Vector2(i%size*.5f-Limit,i/size*.5f-Limit);
        int[] prev=new int[size*size];for(int i=0;i<prev.Length;i++)prev[i]=-2;
        int start=-1;float near=float.MaxValue;
        int cx=Mathf.Clamp(Mathf.RoundToInt((position.x+Limit)*2),0,size-1),cy=Mathf.Clamp(Mathf.RoundToInt((position.y+Limit)*2),0,size-1);
        for(int y=Mathf.Max(0,cy-3);y<=Mathf.Min(size-1,cy+3);y++)for(int x=Mathf.Max(0,cx-3);x<=Mathf.Min(size-1,cx+3);x++) {
            int i=y*size+x;float d=(at(i)-position).sqrMagnitude;if(navigation[i]&&d<near&&Segment(position,at(i))){start=i;near=d;}
        }
        if(start<0)return;
        int goal=start;near=float.MaxValue;
        for(int i=0;i<navigation.Length;i++)if(navigation[i]){float d=(at(i)-target).sqrMagnitude;if(d<near){goal=i;near=d;}}
        var queue=new SortedSet<(float score,int id)>();
        var costs=new float[prev.Length];for(int i=0;i<costs.Length;i++)costs[i]=float.PositiveInfinity;
        costs[start]=0;queue.Add((Vector2.Distance(at(start),at(goal)),start));prev[start]=-1;int best=start;near=(at(start)-target).sqrMagnitude;
        while(queue.Count>0) {
            var item=queue.Min;queue.Remove(item);int current=item.id;
            float d=(at(current)-target).sqrMagnitude;if(d<near){best=current;near=d;}
            if(current==goal){best=current;break;}
            for(int x=-1;x<=1;x++)for(int y=-1;y<=1;y++) {
                int nx=current%size+x,ny=current/size+y;if(nx<0||ny<0||nx>=size||ny>=size)continue;
                int next=ny*size+nx;float cost=costs[current]+(x!=0&&y!=0?.7071068f:.5f);
                if(next==current||!navigation[next]||cost>=costs[next]||!Segment(at(current),at(next)))continue;
                queue.Remove((costs[next]+Vector2.Distance(at(next),at(goal)),next));
                costs[next]=cost;prev[next]=current;queue.Add((cost+Vector2.Distance(at(next),at(goal)),next));
            }
        }
        var points=new List<Vector2>();for(int i=best;i>=0;i=prev[i])points.Add(at(i));points.Reverse();
        if(Segment(at(best),target))points.Add(target);
        Vector2 anchor=position;
        for(int i=0;i<points.Count;) {
            int far=i;for(int j=points.Count-1;j>i;j--)if(Segment(anchor,points[j])){far=j;break;}
            if(Vector2.Distance(anchor,points[far])>.03f){path.Enqueue(points[far]);anchor=points[far];}i=far+1;
        }
        moveMarker.transform.position=MosslightGame.Project(anchor);moveMarker.enabled=path.Count>0;
    }
    Vector2 Node(int i)=>new Vector2(i%gridSize*.5f-Limit,i/gridSize*.5f-Limit);
    Rect MapRect=>mapOpen?new Rect(280,120,720,550):new Rect(20,125,210,165);
    void LateUpdate()
    {
        if(!followPlayer)return;
        cameraZoom=Mathf.Clamp(cameraZoom-Input.mouseScrollDelta.y,6,20);
        townCamera.orthographicSize=Mathf.Lerp(townCamera.orthographicSize,cameraZoom,Time.deltaTime*8);
        townCamera.transform.position=Vector3.Lerp(townCamera.transform.position,new Vector3(player.position.x,player.position.y+1,-20),Time.deltaTime*7);
    }
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))mapOpen=!mapOpen;
        if(Input.GetKeyDown(KeyCode.Escape)){mapOpen=false;talking=null;pending=null;portalDialog=false;portalPending=false;path.Clear();moveMarker.enabled=false;}
        if(talking||portalDialog)return;
        retarget-=Time.deltaTime;
        bool mapHit=MapRect.Contains(GuiPoint());
        if(mapHit&&Input.GetMouseButtonDown(1)) {
            var r=MapRect;var q=GuiPoint();pending=null;portalPending=false;
            Go(new Vector2((q.x-r.x)/r.width*2*Limit-Limit,Limit-(q.y-r.y)/r.height*2*Limit));
            mapOpen=false;
        }
        bool overUi=GuiPoint().y<110||GuiPoint().y>680||mapHit||mapOpen;
        Vector2 mouse=townCamera.ScreenToWorldPoint(Input.mousePosition);
        if(!overUi&&Input.GetMouseButtonDown(0)&&portal&&portal.GetComponent<SpriteRenderer>().bounds.Contains(new Vector3(mouse.x,mouse.y,0))) {
            pending=null;portalPending=true;Go(MosslightGame.Unproject(portal.transform.position)-Vector2.one*.7f);
        }
        if(!overUi&&Input.GetMouseButtonDown(0))foreach(var npc in npcs) {
            if(npc.GetComponent<SpriteRenderer>().bounds.Contains(new Vector3(mouse.x,mouse.y,0))) {
                pending=npc;portalPending=false;Go(MosslightGame.Unproject(npc.transform.position));break;
            }
        }
        if(!overUi&&Input.GetMouseButton(1)&&retarget<=0){pending=null;portalPending=false;Go(MosslightGame.Unproject(mouse));retarget=.2f;}
        if(path.Count>0) {
            Vector2 next=Vector2.MoveTowards(position,path.Peek(),moveSpeed*Mathf.Min(Time.deltaTime,.05f));
            player.GetComponent<SpriteRenderer>().flipX=MosslightGame.Project(next-position).x<0;
            position=next;if(Vector2.Distance(position,path.Peek())<.02f)path.Dequeue();
        }
        else moveMarker.enabled=false;
        player.position=MosslightGame.Project(position);
        player.GetComponent<SpriteRenderer>().sortingOrder=1000-Mathf.RoundToInt(player.position.y*100);
        if(portalPending&&Vector2.Distance(position,MosslightGame.Unproject(portal.transform.position))<portal.interactionRange) {
            portalPending=false;portalDialog=true;path.Clear();moveMarker.enabled=false;
        }
        if(pending&&Vector2.Distance(position,MosslightGame.Unproject(pending.transform.position))<1.15f) {
            talking=pending;pending=null;path.Clear();moveMarker.enabled=false;
        }
    }
    public void EnterDungeon()
    {
        if(!portal||!Application.CanStreamedLevelBeLoaded(portal.dungeonScene)){notice="던전 장면을 찾을 수 없습니다.";return;}
        TownVisit.saved=true;TownVisit.gold=gold;TownVisit.health=health;TownVisit.maxHealth=maxHealth;
        TownVisit.potions=potions;TownVisit.sword=sword;TownVisit.blessed=blessed;
        UnityEngine.SceneManagement.SceneManager.LoadScene(portal.dungeonScene);
    }
    public bool UseService(TownNpc npc)
    {
        int cost=Mathf.Max(0,npc.price);
        if(npc.service==TownNpc.Service.Church) {
            health=maxHealth;notice="체력을 모두 회복했습니다. 평안한 여행 되세요.";return true;
        }
        if(npc.service==TownNpc.Service.Blacksmith&&sword>=3){notice="검이 이미 최대 단계입니다.";return false;}
        if(gold<cost){notice="골드가 부족합니다.";return false;}
        gold-=cost;
        if(npc.service==TownNpc.Service.Blacksmith){sword++;notice="검을 +"+sword+" 단계로 강화했습니다.";}
        else{potions++;notice="회복 물약을 구입했습니다. 아래 물약 사용 버튼으로 마실 수 있습니다.";}
        return true;
    }
    bool Bless()
    {
        if(blessed||gold<20)return false;
        gold-=20;blessed=true;maxHealth+=2;health=maxHealth;notice="축복을 받았습니다. 최대 체력이 2 증가했습니다.";return true;
    }
    bool Drink()
    {
        if(potions==0||health>=maxHealth){notice="물약이 없거나 체력이 이미 가득 찼습니다.";return false;}
        potions--;health=Mathf.Min(maxHealth,health+3);notice="체력을 3 회복했습니다.";return true;
    }
    Vector2 GuiPoint()
    {
        float s=Mathf.Min(Screen.width/1280f,Screen.height/800f);
        return new Vector2((Input.mousePosition.x-(Screen.width-1280*s)/2)/s,(Screen.height-Input.mousePosition.y-(Screen.height-800*s)/2)/s);
    }
    void Panel(Rect r,Color color){GUI.color=color;GUI.DrawTexture(r,Texture2D.whiteTexture);GUI.color=Color.white;}
    void OnGUI()
    {
        if(text==null) {
            var font=Font.CreateDynamicFontFromOSFont(new[]{"Malgun Gothic","Arial"},18);
            text=new GUIStyle(GUI.skin.label){font=font,fontSize=18,wordWrap=true};
            small=new GUIStyle(text){fontSize=14};title=new GUIStyle(text){fontSize=26,fontStyle=FontStyle.Bold};
            button=new GUIStyle(GUI.skin.button){font=font,fontSize=17};
        }
        float scale=Mathf.Min(Screen.width/1280f,Screen.height/800f);
        GUI.matrix=Matrix4x4.TRS(new Vector3((Screen.width-1280*scale)/2,(Screen.height-800*scale)/2),Quaternion.identity,Vector3.one*scale);
        Color panel=new Color(.055f,.065f,.065f,.96f);
        Panel(new Rect(20,18,420,87),panel);GUI.Label(new Rect(38,26,390,36),"돌빛 마을  /  STONEHAVEN",title);
        GUI.Label(new Rect(38,65,390,28),"중세의 작은 쉼터 · 대장간 / 교회 / 잡화점",small);
        Panel(new Rect(810,18,450,87),panel);
        GUI.Label(new Rect(830,31,425,28),"골드 "+gold+"     체력 "+health+" / "+maxHealth+"     검 +"+sword,text);
        GUI.Label(new Rect(830,65,425,24),"물약 "+potions+"개    ·    "+(blessed?"교회의 축복을 받음":"축복 없음"),small);
        foreach(var npc in npcs) {
            Vector3 screen=townCamera.WorldToScreenPoint(npc.transform.position+Vector3.up*1.1f);
            Vector2 point=new Vector2((screen.x-(Screen.width-1280*scale)/2)/scale,(Screen.height-screen.y-(Screen.height-800*scale)/2)/scale);
            if(point.x<70||point.x>1210||point.y<130||point.y>675)continue;
            Panel(new Rect(point.x-72,point.y-10,144,29),panel);
            GUI.Label(new Rect(point.x-70,point.y-8,140,26),npc.displayName,new GUIStyle(small){alignment=TextAnchor.MiddleCenter});
        }
        var map=MapRect;Panel(map,new Color(.10f,.17f,.14f,.96f));
        foreach(var obstacle in obstacles) {
            Vector2 p=MosslightGame.Unproject(obstacle.transform.position);
            float x=map.x+(p.x+Limit)/(2*Limit)*map.width,y=map.y+(Limit-p.y)/(2*Limit)*map.height;
            float w=Mathf.Max(2,obstacle.groundSize.x/(2*Limit)*map.width),h=Mathf.Max(2,obstacle.groundSize.y/(2*Limit)*map.height);
            Panel(new Rect(x-w/2,y-h/2,w,h),new Color(.48f,.46f,.33f));
        }
        foreach(var npc in npcs) {
            Vector2 p=MosslightGame.Unproject(npc.transform.position);
            float x=map.x+(p.x+Limit)/(2*Limit)*map.width,y=map.y+(Limit-p.y)/(2*Limit)*map.height;
            Panel(new Rect(x-4,y-4,8,8),new Color(1,.77f,.36f));
            if(mapOpen)GUI.Label(new Rect(x+8,y-12,170,25),npc.displayName,small);
        }
        Panel(new Rect(map.x+(position.x+Limit)/(2*Limit)*map.width-3,map.y+(Limit-position.y)/(2*Limit)*map.height-3,6,6),Color.white);
        GUI.Label(new Rect(map.x,map.yMax+4,map.width,25),"M 지도 · 지도 우클릭 이동",small);
        if(portal) {
            Vector2 p=MosslightGame.Unproject(portal.transform.position);
            float x=map.x+(p.x+Limit)/(2*Limit)*map.width,y=map.y+(Limit-p.y)/(2*Limit)*map.height;
            Panel(new Rect(x-4,y-4,8,8),Color.cyan);
            if(mapOpen)GUI.Label(new Rect(x+8,y-12,170,25),"던전 포탈",small);
        }
        Panel(new Rect(20,698,1240,82),panel);
        GUI.Label(new Rect(38,709,960,25),notice,text);
        GUI.Label(new Rect(38,748,950,25),"우클릭 이동 · NPC 좌클릭 · 휠 확대/축소 · M 전체 지도 · Esc 닫기 | 진행은 실행 중 유지",small);
        if(GUI.Button(new Rect(1040,715,195,46),"물약 사용 ("+potions+")",button))Drink();
        if(talking) {
            Panel(new Rect(280,420,720,255),panel);
            GUI.Label(new Rect(305,440,600,36),talking.displayName,title);
            GUI.Label(new Rect(305,486,665,63),talking.greeting,text);
            string action=talking.service==TownNpc.Service.Blacksmith?"검 강화 · "+talking.price+" G":talking.service==TownNpc.Service.Shop?"회복 물약 · "+talking.price+" G":"무료 치료";
            if(GUI.Button(new Rect(305,571,270,45),action,button))UseService(talking);
            if(talking.service==TownNpc.Service.Church&&GUI.Button(new Rect(590,571,270,45),blessed?"축복 완료":"축복 · 20 G",button)) {
                if(!Bless())notice=blessed?"이미 축복을 받았습니다.":"골드가 부족합니다.";
            }
            if(GUI.Button(new Rect(870,435,100,35),"닫기",button))talking=null;
            GUI.Label(new Rect(305,628,670,28),"마을에서 준비하고 쉬어 가세요. 검 강화는 이 마을 시제품의 장비 수치입니다.",small);
        }
        if(portalDialog) {
            Panel(new Rect(280,420,720,255),panel);
            GUI.Label(new Rect(310,440,650,40),"포탈 광장 · 숲 던전",title);
            GUI.Label(new Rect(310,490,650,65),"슬라임 3개 웨이브를 처치하고 제단을 복구하세요.\n던전은 입장할 때마다 새로 시작합니다.",text);
            if(GUI.Button(new Rect(310,580,310,48),"던전 입장",button))EnterDungeon();
            if(GUI.Button(new Rect(640,580,310,48),"마을에 머무르기",button))portalDialog=false;
        }
    }
    void SmokeTest()
    {
        try {
            if(npcs.Length!=3)throw new Exception("Missing NPCs");
            var timer=System.Diagnostics.Stopwatch.StartNew();
            if(Limit>=36)foreach(var target in new[]{new Vector2(-31,30),new Vector2(31,0),new Vector2(30,30)}) {
                Go(target);Vector2 end=position;
                foreach(var p in path){if(!Segment(end,p))throw new Exception("Expanded route crosses obstacle");end=p;}
                if(Vector2.Distance(end,target)>1.15f)throw new Exception("Expanded district unreachable: "+target);
            }
            foreach(var npc in npcs) {
                Go(MosslightGame.Unproject(npc.transform.position));
                Vector2 end=position;foreach(var p in path){if(!Segment(end,p))throw new Exception("Blocked path");end=p;}
                if(Vector2.Distance(end,MosslightGame.Unproject(npc.transform.position))>1.15f)throw new Exception("Unreachable NPC "+npc.name);
            }
            var smith=Array.Find(npcs,n=>n.service==TownNpc.Service.Blacksmith);
            Debug.Log("TOWN_NAVIGATION_OK: expanded districts, bridges and NPC paths in "+timer.ElapsedMilliseconds+" ms");
            var shop=Array.Find(npcs,n=>n.service==TownNpc.Service.Shop);
            var church=Array.Find(npcs,n=>n.service==TownNpc.Service.Church);
            gold=120;health=2;if(!UseService(smith)||sword!=1||gold!=80)throw new Exception("Smith");
            if(!UseService(shop)||potions!=1||gold!=65||!Drink()||health!=5)throw new Exception("Shop/potion");
            UseService(church);if(health!=maxHealth||!Bless()||Bless())throw new Exception("Church");
            gold=0;int old=sword;if(UseService(smith)||sword!=old)throw new Exception("Insufficient funds");
            Debug.Log("TOWN_SMOKE_OK: all NPC routes, smith, shop, potion, healing, blessing, insufficient gold");Application.Quit(0);
        }catch(Exception e){Debug.LogException(e);Application.Quit(1);}
    }
}
