using System;
using System.Globalization;
using System.Text;
using UnityEngine;
#if UNITY_ANDROID && GPGS
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

/// <summary>
/// Google sign-in, used only to show the player's name on the menu. On Android it goes through
/// Google Play Games (plugin installed and the GPGS scripting define set). The editor fakes a name
/// so the menu can be tested. Other platforms report that sign-in is not available.
/// </summary>
public static class GoogleLogin
{
    public static bool SignedIn { get; private set; }

    /// <summary>Name in pixel-font form: no accents, uppercase, at most 14 characters.</summary>
    public static string Name { get; private set; } = "";

    public static bool Available
    {
        get
        {
#if UNITY_EDITOR || (UNITY_ANDROID && GPGS)
            return true;
#else
            return false;
#endif
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { SignedIn = false; Name = ""; }

    /// <summary>Silent sign-in at boot. Play Games signs a returning player in without a prompt.</summary>
    public static void TrySilent(Action<bool> done)
    {
#if UNITY_ANDROID && GPGS && !UNITY_EDITOR
        Activate();
        PlayGamesPlatform.Instance.Authenticate(s => Finish(s == SignInStatus.Success, done));
#else
        done?.Invoke(false);
#endif
    }

    /// <summary>Sign-in started by the player pressing the button.</summary>
    public static void SignIn(Action<bool> done)
    {
#if UNITY_EDITOR
        SignedIn = true;
        Name = Clean("Editor Pilot");
        done?.Invoke(true);
#elif UNITY_ANDROID && GPGS
        Activate();
        PlayGamesPlatform.Instance.ManuallyAuthenticate(s => Finish(s == SignInStatus.Success, done));
#else
        done?.Invoke(false);
#endif
    }

#if UNITY_ANDROID && GPGS && !UNITY_EDITOR
    static bool activated;

    static void Activate()
    {
        if (activated) return;
        PlayGamesPlatform.Activate();
        activated = true;
    }

    static void Finish(bool ok, Action<bool> done)
    {
        SignedIn = ok;
        Name = ok ? Clean(PlayGamesPlatform.Instance.GetUserDisplayName()) : "";
        done?.Invoke(ok);
    }
#endif

    /// <summary>Fits a display name to the pixel font: Vietnamese accents dropped, unknown characters removed.</summary>
    public static string Clean(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "PILOT";
        var sb = new StringBuilder();
        foreach (char ch in raw.Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormD))
        {
            if (CharUnicodeInfo.GetUnicodeCategory(ch) == UnicodeCategory.NonSpacingMark) continue;
            char u = char.ToUpperInvariant(ch);
            if (u == ' ' ? sb.Length > 0 && sb[sb.Length - 1] != ' ' : PixelText.Charset.IndexOf(u) >= 0)
                sb.Append(u);
        }
        string s = sb.ToString().Trim();
        if (s.Length > 14) s = s.Substring(0, 14).TrimEnd();
        return s.Length > 0 ? s : "PILOT";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void SelfCheck()
    {
        bool ok = Clean("Nguyễn Văn Đức") == "NGUYEN VAN DUC" && Clean("") == "PILOT" && Clean("@@") == "PILOT"
            && Clean("A Very Long Player Name") == "A VERY LONG PL";
        if (!ok) Debug.LogError("[GoogleLogin] name cleaning failed");
    }
}
