using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public sealed class PortalTravelSmoke : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void StartTest()
    {
        if(Array.IndexOf(Environment.GetCommandLineArgs(),"--portal-smoke-test")<0)return;
        var go=new GameObject("Portal travel test");DontDestroyOnLoad(go);go.AddComponent<PortalTravelSmoke>();
    }
    IEnumerator Start()
    {
        yield return null;
        var town=FindAnyObjectByType<TownGame>();
        if(!town||!FindAnyObjectByType<TownPortal>()||GameObject.Find("Central stone well")){Fail("Portal scene setup");yield break;}
        town.EnterDungeon();yield return null;yield return null;
        var dungeon=FindAnyObjectByType<MosslightGame>();
        if(!dungeon||SceneManager.GetActiveScene().name!="Mosslight"||!TownVisit.saved){Fail("Dungeon entry");yield break;}
        int gold=TownVisit.gold;
        dungeon.ReturnToTown();yield return null;yield return null;
        town=FindAnyObjectByType<TownGame>();var portal=FindAnyObjectByType<TownPortal>();
        if(!town||!portal||TownVisit.gold!=gold||Vector2.Distance(town.player.position,portal.transform.position)>5){Fail("Town return");yield break;}
        town.EnterDungeon();yield return null;yield return null;
        if(!FindAnyObjectByType<MosslightGame>()){Fail("Dungeon re-entry");yield break;}
        Debug.Log("PORTAL_TRAVEL_OK: well removed, town to dungeon, return near portal, state preserved, re-entry");Application.Quit(0);
    }
    static void Fail(string message){Debug.LogError(message);Application.Quit(1);}
}
