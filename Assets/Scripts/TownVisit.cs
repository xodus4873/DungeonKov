using UnityEngine;

// In-memory travel state. Domain reload and exiting the application clear it.
public static class TownVisit
{
    public static bool saved;
    public static int gold,health,maxHealth,potions,sword;
    public static bool blessed;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reset(){saved=false;}
}
