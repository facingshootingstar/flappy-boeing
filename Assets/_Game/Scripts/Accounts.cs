using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

/// <summary>
/// Local accounts: name + password, kept in accounts.json on this device only. Used to greet the player
/// by name. Passwords are never stored, only a salted PBKDF2 hash. The last signed-in name is remembered.
/// </summary>
public static class Accounts
{
    public const int NameMin = 3, NameMax = 12, PassMin = 4, PassMax = 24;
    const int Iterations = 20000, HashBytes = 32, SaltBytes = 16;

    [Serializable] class Entry { public string name, salt, hash; }
    [Serializable] class Store { public string current = ""; public List<Entry> users = new List<Entry>(); }

    static Store store;

    static string FilePath => Path.Combine(Application.persistentDataPath, "accounts.json");
    static string TempPath => FilePath + ".tmp";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { store = null; }

    /// <summary>Signed-in name, or empty.</summary>
    public static string Current { get { Load(); return store.current; } }
    public static bool SignedIn => Current.Length > 0;

    /// <summary>Name rule: letters and digits only, so it always draws in the pixel font.</summary>
    public static string CheckName(string name)
    {
        if (name.Length < NameMin) return "NAME TOO SHORT";
        if (name.Length > NameMax) return "NAME TOO LONG";
        foreach (char c in name)
            if (!((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) return "NAME: A-Z 0-9 ONLY";
        return null;
    }

    public static string CheckPass(string pass)
    {
        if (pass.Length < PassMin) return "PASSWORD TOO SHORT";
        if (pass.Length > PassMax) return "PASSWORD TOO LONG";
        return null;
    }

    /// <summary>Creates the account and signs in. Returns an error line, or null on success.</summary>
    public static string Register(string name, string pass)
    {
        string err = CheckName(name) ?? CheckPass(pass);
        if (err != null) return err;
        Load();
        if (Find(name) != null) return "NAME ALREADY TAKEN";
        var salt = new byte[SaltBytes];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
        store.users.Add(new Entry { name = name, salt = Convert.ToBase64String(salt), hash = Convert.ToBase64String(Hash(pass, salt)) });
        store.current = name;
        Save();
        return null;
    }

    /// <summary>Signs in. Returns an error line, or null on success.</summary>
    public static string SignIn(string name, string pass)
    {
        Load();
        var e = Find(name);
        if (e == null || !Same(Hash(pass, Convert.FromBase64String(e.salt)), Convert.FromBase64String(e.hash)))
            return "WRONG NAME OR PASSWORD";
        store.current = name;
        Save();
        return null;
    }

    public static void SignOut()
    {
        Load();
        store.current = "";
        Save();
    }

    static Entry Find(string name) => store.users.Find(u => u.name == name);

    static byte[] Hash(string pass, byte[] salt)
    {
        using (var kdf = new Rfc2898DeriveBytes(pass, salt, Iterations, HashAlgorithmName.SHA256))
            return kdf.GetBytes(HashBytes);
    }

    static bool Same(byte[] a, byte[] b)
    {
        if (a.Length != b.Length) return false;
        int diff = 0;
        for (int i = 0; i < a.Length; i++) diff |= a[i] ^ b[i];
        return diff == 0;
    }

    static void Load()
    {
        if (store != null) return;
        try { store = File.Exists(FilePath) ? JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) : null; }
        catch (Exception e) { Debug.LogWarning("[Accounts] could not read accounts: " + e.Message); }
        store ??= new Store();
        store.users ??= new List<Entry>();
        store.current ??= "";
        if (store.current.Length > 0 && Find(store.current) == null) store.current = "";
    }

    static void Save()
    {
        try
        {
            File.WriteAllText(TempPath, JsonUtility.ToJson(store, true));
            if (File.Exists(FilePath)) File.Delete(FilePath);
            File.Move(TempPath, FilePath);
        }
        catch (Exception e) { Debug.LogWarning("[Accounts] could not save accounts: " + e.Message); }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void SelfCheck()
    {
        bool ok = CheckName("POE") == null && CheckName("AB") != null && CheckName("POE 1") != null
            && CheckName("ABCDEFGHIJKLM") != null && CheckPass("abc") != null && CheckPass("abcd") == null;
        if (!ok) Debug.LogError("[Accounts] name/password rules failed");
    }
}
