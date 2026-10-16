using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class BossRecord
{
    public string bossId;
    public bool defeated;
    public float bestTime;
    public int bestGrade;
}

[Serializable]
public class SaveData
{
    public int version = 1;
    public int coins;
    public int gems;
    public int pilotXp;
    public int bestFlappy;
    public string selectedPlane = "boeing";
    public List<string> unlockedPlanes = new List<string> { "boeing" };
    public List<string> unlockedGames = new List<string> { "flappy", "bossrush" };
    public List<BossRecord> bossRecords = new List<BossRecord>();
    public bool musicOn = true;
    public float sfxVolume = 1f;
}

public static class SaveSystem
{
    private static SaveData _data;
    public static SaveData Data
    {
        get
        {
            if (_data == null) Load();
            return _data;
        }
    }

    private static string SavePath => Path.Combine(Application.persistentDataPath, "save.json");
    private static string BackupPath => SavePath + ".bak";
    private static string TempPath => SavePath + ".tmp";

    public static SaveData Load()
    {
        SaveData loaded = TryReadFile(SavePath) ?? TryReadFile(BackupPath);
        if (loaded == null)
        {
            loaded = new SaveData();
            if (PlayerPrefs.HasKey("best"))
                loaded.bestFlappy = PlayerPrefs.GetInt("best");
        }
        Migrate(loaded);
        _data = loaded;
        return _data;
    }

    private static SaveData TryReadFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                string json = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(json))
                    return JsonUtility.FromJson<SaveData>(json);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed reading {path}: {e.Message}");
        }
        return null;
    }

    public static void Save()
    {
        try
        {
            string json = JsonUtility.ToJson(Data, true);
            File.WriteAllText(TempPath, json);

            if (File.Exists(SavePath))
                File.Replace(TempPath, SavePath, BackupPath);
            else
                File.Move(TempPath, SavePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Failed to save: {e.Message}");
        }
    }

    public static void Migrate(SaveData d)
    {
        if (d == null) return;
        if (d.version < 1) d.version = 1;
        if (d.unlockedPlanes == null) d.unlockedPlanes = new List<string> { "boeing" };
        if (d.unlockedGames == null) d.unlockedGames = new List<string> { "flappy", "bossrush" };
        if (d.bossRecords == null) d.bossRecords = new List<BossRecord>();
    }

    public static bool SpendCoins(int amount)
    {
        if (amount < 0 || Data.coins < amount) return false;
        Data.coins -= amount;
        Save();
        return true;
    }

    public static void AddCoins(int amount)
    {
        Data.coins += Mathf.Max(0, amount);
        Save();
    }

    public static void ResetAll()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(BackupPath)) File.Delete(BackupPath);
            if (File.Exists(TempPath)) File.Delete(TempPath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[SaveSystem] Reset file error: {e.Message}");
        }

        _data = new SaveData();
        Save();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _data = null;
    }
}
