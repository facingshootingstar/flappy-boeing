using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Firebase Auth over its REST API (no SDK). A name becomes the email name@flappyboeing.app. Only
/// {current, uid, refreshToken} is kept in accounts.json; the password is never stored. The API key comes from
/// Resources/firebase.json (git-ignored, see firebase.example.json). All calls report back through a callback
/// with an error line (short, uppercase, pixel-font safe) or null on success.
/// </summary>
public static class Accounts
{
    public const int NameMin = 3, NameMax = 12, PassMin = 6, PassMax = 24;
    const string Domain = "@flappyboeing.app";
    const string AuthUrl = "https://identitytoolkit.googleapis.com/v1/accounts:";
    const string TokenUrl = "https://securetoken.googleapis.com/v1/token";

    [Serializable] class Store { public string current = "", uid = "", refreshToken = ""; }
    [Serializable] class KeyFile { public string apiKey = ""; }
    [Serializable] class Req { public string email, password; public bool returnSecureToken = true; }
    [Serializable] class Resp { public string localId, refreshToken, refresh_token, user_id; }
    [Serializable] class ErrBody { public ErrInner error; }
    [Serializable] class ErrInner { public string message; }

    static Store store;
    static string apiKey;
    static bool keyLoaded;

    static string FilePath => Path.Combine(Application.persistentDataPath, "accounts.json");
    static string TempPath => FilePath + ".tmp";

    /// <summary>True once Firebase accepted a sign-in or a token refresh during this run. The menu waits for it.</summary>
    public static bool Verified { get; private set; }
    public static bool Busy { get; private set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { store = null; apiKey = null; keyLoaded = false; Verified = false; Busy = false; }

    /// <summary>Signed-in name, or empty.</summary>
    public static string Current { get { Load(); return Verified ? store.current : ""; } }
    public static string Uid { get { Load(); return store.uid; } }
    public static bool SignedIn => Verified && Current.Length > 0;
    /// <summary>A refresh token from an earlier run is stored (not yet checked with Firebase).</summary>
    public static bool HasSession { get { Load(); return store.refreshToken.Length > 0 && store.current.Length > 0; } }
    public static string SavedName { get { Load(); return store.current; } }

    public static bool KeyMissing { get { LoadKey(); return string.IsNullOrEmpty(apiKey); } }

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

    public static string EmailFor(string name) => name.ToLowerInvariant() + Domain;

    public static void Register(string name, string pass, Action<string> done) => Auth("signUp", name, pass, done);
    public static void SignIn(string name, string pass, Action<string> done) => Auth("signInWithPassword", name, pass, done);

    public static void SignOut()
    {
        Load();
        Verified = false;
        store.current = store.uid = store.refreshToken = "";
        Save();
    }

    /// <summary>Checks the stored refresh token with Firebase. An expired or revoked token is dropped quietly.</summary>
    public static void Restore(Action<string> done)
    {
        Load();
        if (!HasSession) { done?.Invoke("NO SESSION"); return; }
        if (KeyMissing) { done?.Invoke("FIREBASE KEY MISSING"); return; }
        string body = "grant_type=refresh_token&refresh_token=" + UnityWebRequest.EscapeURL(store.refreshToken);
        Send(TokenUrl + "?key=" + apiKey, Encoding.UTF8.GetBytes(body), "application/x-www-form-urlencoded", (resp, err) =>
        {
            if (err != null)
            {
                if (err == "SESSION EXPIRED") { store.refreshToken = store.uid = store.current = ""; Save(); }
                done?.Invoke(err);
                return;
            }
            store.refreshToken = string.IsNullOrEmpty(resp.refresh_token) ? store.refreshToken : resp.refresh_token;
            if (!string.IsNullOrEmpty(resp.user_id)) store.uid = resp.user_id;
            Verified = true;
            Save();
            done?.Invoke(null);
        });
    }

    static void Auth(string endpoint, string name, string pass, Action<string> done)
    {
        string err = CheckName(name) ?? CheckPass(pass);
        if (err != null) { done?.Invoke(err); return; }
        if (KeyMissing) { done?.Invoke("FIREBASE KEY MISSING"); return; }
        if (Busy) return;
        var json = JsonUtility.ToJson(new Req { email = EmailFor(name), password = pass });
        Send(AuthUrl + endpoint + "?key=" + apiKey, Encoding.UTF8.GetBytes(json), "application/json", (resp, e) =>
        {
            if (e != null) { done?.Invoke(e); return; }
            Load();
            store.current = name;
            store.uid = resp.localId ?? "";
            store.refreshToken = resp.refreshToken ?? "";
            Verified = true;
            Save();
            done?.Invoke(null);
        });
    }

    static void Send(string url, byte[] body, string contentType, Action<Resp, string> cb)
    {
        Busy = true;
        var req = new UnityWebRequest(url, "POST")
        {
            uploadHandler = new UploadHandlerRaw(body) { contentType = contentType },
            downloadHandler = new DownloadHandlerBuffer(),
            timeout = 15
        };
        req.SendWebRequest().completed += _ =>
        {
            Busy = false;
            string text = req.downloadHandler != null ? req.downloadHandler.text : "";
            var result = req.result;
            req.Dispose();
            if (result == UnityWebRequest.Result.Success)
            {
                Resp r = null;
                try { r = JsonUtility.FromJson<Resp>(text); } catch (Exception) { }
                cb(r ?? new Resp(), null);
            }
            else cb(null, result == UnityWebRequest.Result.ProtocolError ? MapError(text) : "NO CONNECTION");
        };
    }

    /// <summary>Firebase error text ("WEAK_PASSWORD : ...") to a short line for the pixel font.</summary>
    public static string MapError(string body)
    {
        string code = "";
        try { code = JsonUtility.FromJson<ErrBody>(body)?.error?.message ?? ""; } catch (Exception) { }
        int cut = code.IndexOf(' ');
        if (cut > 0) code = code.Substring(0, cut);
        switch (code)
        {
            case "EMAIL_EXISTS": return "NAME ALREADY TAKEN";
            case "INVALID_LOGIN_CREDENTIALS":
            case "EMAIL_NOT_FOUND":
            case "INVALID_PASSWORD": return "WRONG NAME OR PASSWORD";
            case "WEAK_PASSWORD": return "PASSWORD TOO WEAK";
            case "TOO_MANY_ATTEMPTS_TRY_LATER": return "TOO MANY TRIES";
            case "USER_DISABLED": return "ACCOUNT DISABLED";
            case "OPERATION_NOT_ALLOWED":
            case "CONFIGURATION_NOT_FOUND": return "EMAIL LOGIN IS OFF";
            case "TOKEN_EXPIRED":
            case "INVALID_REFRESH_TOKEN":
            case "USER_NOT_FOUND": return "SESSION EXPIRED";
        }
        if (code.StartsWith("API_KEY") || code.StartsWith("INVALID_API_KEY") || body.Contains("API key not valid")) return "BAD FIREBASE KEY";
        return "LOGIN FAILED";
    }

    static void LoadKey()
    {
        if (keyLoaded) return;
        keyLoaded = true;
        try
        {
            // Resources works on every platform (WebGL/Android can't File-read StreamingAssets); the
            // StreamingAssets copy is kept as a desktop fallback for older setups.
            var res = Resources.Load<TextAsset>("firebase");
            string json = res != null ? res.text : null;
            string p = Path.Combine(Application.streamingAssetsPath, "firebase.json");
            if (json == null && File.Exists(p)) json = File.ReadAllText(p);
            if (json != null) apiKey = (JsonUtility.FromJson<KeyFile>(json)?.apiKey ?? "").Trim();
        }
        catch (Exception e) { Debug.LogWarning("[Accounts] could not read firebase.json: " + e.Message); }
    }

    static void Load()
    {
        if (store != null) return;
        try { store = File.Exists(FilePath) ? JsonUtility.FromJson<Store>(File.ReadAllText(FilePath)) : null; }
        catch (Exception e) { Debug.LogWarning("[Accounts] could not read accounts: " + e.Message); }
        store ??= new Store();
        store.current ??= ""; store.uid ??= ""; store.refreshToken ??= "";
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
            && CheckName("ABCDEFGHIJKLM") != null && CheckPass("abc") != null && CheckPass("abcdef") == null
            && EmailFor("PoE") == "poe@flappyboeing.app"
            && MapError("{\"error\":{\"message\":\"EMAIL_EXISTS\"}}") == "NAME ALREADY TAKEN"
            && MapError("{\"error\":{\"message\":\"WEAK_PASSWORD : x\"}}") == "PASSWORD TOO WEAK"
            && MapError("{\"error\":{\"message\":\"INVALID_LOGIN_CREDENTIALS\"}}") == "WRONG NAME OR PASSWORD";
        if (!ok) Debug.LogError("[Accounts] self check failed");
    }
}
