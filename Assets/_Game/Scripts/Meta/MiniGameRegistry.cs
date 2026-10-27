using System.Collections.Generic;
using UnityEngine;

public static class MiniGameRegistry
{
    private static List<MiniGameDef> _cachedDefs;

    private static void EnsureLoaded()
    {
        if (_cachedDefs != null) return;
        var loaded = Resources.LoadAll<MiniGameDef>("MiniGames");
        _cachedDefs = loaded != null ? new List<MiniGameDef>(loaded) : new List<MiniGameDef>();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _cachedDefs = null;
    }

    public static IReadOnlyList<MiniGameDef> All
    {
        get
        {
            EnsureLoaded();
            return _cachedDefs;
        }
    }

    public static MiniGameDef Get(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;

        for (int i = 0; i < _cachedDefs.Count; i++)
        {
            if (_cachedDefs[i] != null && _cachedDefs[i].id == id)
                return _cachedDefs[i];
        }

        return null;
    }

    public static bool IsUnlocked(string id)
    {
        if (string.IsNullOrEmpty(id) || SaveSystem.Data == null || SaveSystem.Data.unlockedGames == null)
            return false;

        return SaveSystem.Data.unlockedGames.Contains(id);
    }

    public static bool TryUnlock(string id)
    {
        if (string.IsNullOrEmpty(id) || IsUnlocked(id))
            return false;

        var def = Get(id);
        if (def == null) return false;

        if (SaveSystem.SpendCoins(def.unlockCoinCost))
        {
            if (SaveSystem.Data.unlockedGames == null)
                SaveSystem.Data.unlockedGames = new List<string>();

            if (!SaveSystem.Data.unlockedGames.Contains(id))
                SaveSystem.Data.unlockedGames.Add(id);

            SaveSystem.Save();
            return true;
        }

        return false;
    }
}
