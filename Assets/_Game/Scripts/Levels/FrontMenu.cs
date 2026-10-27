using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Boot screen. PLAY opens the mail map, RANKING reads saved level records, OPTIONS is music and sfx, EXIT quits.
/// Labels stay in the pixel font. Art is Imagine sprites when those files exist.
/// The corner button opens local sign-in (name + password, see <see cref="Accounts"/>) to greet the player by name.
/// </summary>
public class FrontMenu : MonoBehaviour
{
    const int Order = 180;

    Transform root, panel;
    Ui.Button[] btns;
    Ui.Button backBtn, optMusic, optSfx, accountBtn;
    Ui.Button nameBox, passBox, loginBtn, signupBtn, logoutBtn;
    PixelText msgTxt;
    string nameIn = "", passIn = "";
    int field;
    TouchScreenKeyboard kb;
    int sel;
    float lockUntil;
    Transform poeT;
    Vector3 poeHome;

    static readonly string[] Labels = { "PLAY", "RANKING", "OPTIONS", "EXIT" };

    void Start()
    {
        root = Ui.Root("Front", transform);
        var hangar = PropArt.Imagen("hangar", 15f, true);
        if (hangar) Ui.Spr("Hangar", root, hangar, Vector2.zero, Order);
        else Ui.Box("Hangar", root, Vector2.zero, new Vector2(16f, 10f), new Color32(42, 36, 48, 255), Order);

        var poe = PropArt.Imagen("poe", 2.8f);
        if (poe)
        {
            poeT = Ui.Spr("Poe", root, poe, new Vector2(-4.15f, -0.35f), Order + 4).transform;
            poeHome = poeT.localPosition;
        }
        var pilot = PropArt.Imagen("pilot_idle", 2.35f);
        if (pilot) Ui.Spr("Pilot", root, pilot, new Vector2(-1.55f, -1.05f), Order + 5);

        Ui.Txt(root, CutsceneDB.Title, new Vector2(2.15f, 2.85f), 0.72f, Announcer.Yellow, Order + 8,
            PixelText.Align.Center, PixelText.Effect.Boil);
        Ui.Txt(root, "THE MAIL STILL FLIES", new Vector2(2.15f, 2.15f), 0.62f, Announcer.Cream, Order + 8);

        btns = new Ui.Button[Labels.Length];
        for (int i = 0; i < Labels.Length; i++)
            btns[i] = Ui.Btn(root, Labels[i], new Vector2(2.15f, 1.15f - i * 0.95f), new Vector2(4.4f, 0.78f),
                Announcer.Cream, Order + 8);
        Ui.Txt(root, "W/S: MOVE   SPACE: OK", new Vector2(2.15f, -3.55f), 0.58f, Announcer.Cream, Order + 8);
        accountBtn = Ui.Btn(root, AccountLabel(), new Vector2(-4.9f, 3.6f), new Vector2(4.6f, 0.62f),
            Announcer.Cream, Order + 8);
        accountBtn.label.transform.localScale = Vector3.one * 0.62f;
        AudioManager.Music("music_flappy", 0.6f);
        Paint();
    }

    static string AccountLabel() => Accounts.SignedIn ? "HI " + Accounts.Current : "LOG IN";

    void Update()
    {
        if (poeT) poeT.localPosition = poeHome + new Vector3(0f, Mathf.Sin(Time.unscaledTime * 2.2f) * 0.08f, 0f);
        if (panel) { PanelInput(); return; }
        if (UiInput.UpDown()) Move(-1);
        if (UiInput.DownDown()) Move(1);
        if (UiInput.Confirm()) Activate(sel);
        else if (UiInput.Up(out var p))
        {
            if (accountBtn.Hit(p)) { AudioManager.Sfx("card", 0.5f); OpenAccount(); }
            else for (int i = 0; i < btns.Length; i++)
                if (btns[i].Hit(p)) { sel = i; Activate(i); break; }
        }
        Paint();
    }

    void Move(int d)
    {
        sel = (sel + d + btns.Length) % btns.Length;
        AudioManager.Sfx("score", 0.28f, 0f, 0.04f);
        Paint();
    }

    void Paint()
    {
        if (btns == null) return;
        for (int i = 0; i < btns.Length; i++)
            btns[i].label.SetColor(i == sel ? Announcer.Yellow : Announcer.Cream);
    }

    void Activate(int i)
    {
        AudioManager.Sfx("ready", 0.55f);
        if (i == 0) Play();
        else if (i == 1) OpenRank();
        else if (i == 2) OpenOptions();
        else Quit();
    }

    void Play()
    {
        LevelFlow.AtMenu = false;
        if (root) Destroy(root.gameObject);
        GetComponent<LevelRuntime>().OpenMap();
        Destroy(this);
    }

    void OpenRank()
    {
        ClosePanel();
        lockUntil = Time.unscaledTime + 0.18f;
        panel = Ui.Root("Rank", root);
        var dim = Ui.Ink; dim.a = 0.62f;
        Ui.Box("Dim", panel, Vector2.zero, new Vector2(20f, 12f), dim, Order + 20);
        Ui.Plaque(panel, new Vector2(0f, -0.15f), new Vector2(10.2f, 6.6f), Order + 21);
        int stars = LevelFlow.TotalStars();
        Ui.Txt(panel, "RANKING", new Vector2(0f, 2.55f), 1.3f, Announcer.Yellow, Order + 22, PixelText.Align.Center, PixelText.Effect.Boil);
        Ui.Txt(panel, "STARS " + stars + "/" + LevelDB.MaxStars, new Vector2(0f, 1.9f), 0.8f, Announcer.Cream, Order + 22);
        Ui.Txt(panel, "RANK: " + RankName(stars), new Vector2(0f, 1.4f), 0.9f, Announcer.Teal, Order + 22);

        var rows = new List<LevelRecord>(SaveSystem.Data.levels);
        rows.Sort((a, b) =>
        {
            int s = b.stars.CompareTo(a.stars);
            if (s != 0) return s;
            s = b.best.CompareTo(a.best);
            return s != 0 ? s : a.id.CompareTo(b.id);
        });
        if (rows.Count == 0)
            Ui.Txt(panel, "NO RUNS YET", new Vector2(0f, 0.3f), 0.9f, Color.white, Order + 22);
        else
        {
            int n = Mathf.Min(8, rows.Count);
            for (int i = 0; i < n; i++)
            {
                var r = rows[i];
                Ui.Txt(panel, "L" + r.id + "   STARS " + r.stars + "   BEST " + r.best,
                    new Vector2(-4.3f, 0.75f - i * 0.38f), 0.7f, i == 0 ? Announcer.Yellow : Color.white,
                    Order + 22, PixelText.Align.Left);
            }
        }
        backBtn = Ui.Btn(panel, "BACK", new Vector2(0f, -2.7f), new Vector2(2.6f, 0.7f), Announcer.Cream, Order + 22);
    }

    void OpenOptions()
    {
        ClosePanel();
        lockUntil = Time.unscaledTime + 0.18f;
        panel = Ui.Root("Options", root);
        var dim = Ui.Ink; dim.a = 0.62f;
        Ui.Box("Dim", panel, Vector2.zero, new Vector2(20f, 12f), dim, Order + 20);
        Ui.Plaque(panel, new Vector2(0f, -0.1f), new Vector2(8f, 5.2f), Order + 21);
        Ui.Txt(panel, "OPTIONS", new Vector2(0f, 1.85f), 1.3f, Announcer.Yellow, Order + 22, PixelText.Align.Center, PixelText.Effect.Boil);
        string music = SaveSystem.Data.musicOn ? "MUSIC ON" : "MUSIC OFF";
        int pct = Mathf.RoundToInt(Mathf.Clamp01(SaveSystem.Data.sfxVolume) * 100f);
        optMusic = Ui.Btn(panel, music, new Vector2(0f, 0.55f), new Vector2(4.6f, 0.78f), Announcer.Cream, Order + 22);
        optSfx = Ui.Btn(panel, "SFX " + pct, new Vector2(0f, -0.45f), new Vector2(4.6f, 0.78f), Announcer.Cream, Order + 22);
        backBtn = Ui.Btn(panel, "BACK", new Vector2(0f, -1.7f), new Vector2(2.6f, 0.7f), Announcer.Cream, Order + 22);
    }

    /// <summary>Signed out: name and password fields with LOG IN / SIGN UP. Signed in: the name and LOG OUT.</summary>
    void OpenAccount()
    {
        ClosePanel();
        lockUntil = Time.unscaledTime + 0.18f;
        panel = Ui.Root("Account", root);
        var dim = Ui.Ink; dim.a = 0.62f;
        Ui.Box("Dim", panel, Vector2.zero, new Vector2(20f, 12f), dim, Order + 20);
        Ui.Plaque(panel, new Vector2(0f, -0.15f), new Vector2(8.6f, 5.8f), Order + 21);
        if (Accounts.SignedIn)
        {
            Ui.Txt(panel, "ACCOUNT", new Vector2(0f, 2.05f), 1.3f, Announcer.Yellow, Order + 22, PixelText.Align.Center, PixelText.Effect.Boil);
            Ui.Txt(panel, "SIGNED IN AS", new Vector2(0f, 1.05f), 0.7f, Announcer.Cream, Order + 22);
            Ui.Txt(panel, Accounts.Current, new Vector2(0f, 0.4f), 1.1f, Announcer.Teal, Order + 22);
            logoutBtn = Ui.Btn(panel, "LOG OUT", new Vector2(0f, -0.85f), new Vector2(3.6f, 0.75f), Announcer.Cream, Order + 22);
        }
        else
        {
            Ui.Txt(panel, "LOG IN", new Vector2(0f, 2.05f), 1.3f, Announcer.Yellow, Order + 22, PixelText.Align.Center, PixelText.Effect.Boil);
            Ui.Txt(panel, "NAME", new Vector2(-3.65f, 1.0f), 0.7f, Announcer.Cream, Order + 22, PixelText.Align.Left);
            Ui.Txt(panel, "PASS", new Vector2(-3.65f, 0.05f), 0.7f, Announcer.Cream, Order + 22, PixelText.Align.Left);
            nameBox = Ui.Btn(panel, "", new Vector2(0.75f, 1.0f), new Vector2(5.2f, 0.72f), Announcer.Cream, Order + 22);
            passBox = Ui.Btn(panel, "", new Vector2(0.75f, 0.05f), new Vector2(5.2f, 0.72f), Announcer.Cream, Order + 22);
            nameBox.label.transform.localScale = passBox.label.transform.localScale = Vector3.one * 0.8f;
            msgTxt = Ui.Txt(panel, "", new Vector2(0f, -0.75f), 0.62f, Announcer.Red, Order + 22);
            loginBtn = Ui.Btn(panel, "LOG IN", new Vector2(-1.9f, -1.6f), new Vector2(3.4f, 0.75f), Announcer.Cream, Order + 22);
            signupBtn = Ui.Btn(panel, "SIGN UP", new Vector2(1.9f, -1.6f), new Vector2(3.4f, 0.75f), Announcer.Cream, Order + 22);
            field = 0;
            UiInput.Typed();
            PaintFields();
        }
        backBtn = Ui.Btn(panel, "BACK", new Vector2(0f, -2.5f), new Vector2(2.6f, 0.65f), Announcer.Cream, Order + 22);
    }

    void AccountInput()
    {
        if (UiInput.Escape()) { AudioManager.Sfx("card", 0.4f); ClosePanel(); return; }
        if (nameBox != null)
        {
            foreach (char c in UiInput.Typed()) AddChar(c);
            if (kb != null)
            {
                if (field == 0) { nameIn = ""; foreach (char c in kb.text) AddChar(c); }
                else passIn = kb.text.Length > Accounts.PassMax ? kb.text.Substring(0, Accounts.PassMax) : kb.text;
                if (kb.status != TouchScreenKeyboard.Status.Visible) kb = null;
            }
            if (UiInput.Erase())
            {
                if (field == 0 && nameIn.Length > 0) nameIn = nameIn.Substring(0, nameIn.Length - 1);
                if (field == 1 && passIn.Length > 0) passIn = passIn.Substring(0, passIn.Length - 1);
            }
            if (UiInput.Tab()) field = 1 - field;
            if (UiInput.Enter()) { if (field == 0) field = 1; else Submit(false); }
            if (nameBox != null) PaintFields();
        }
        if (!UiInput.Up(out var p)) return;
        if (nameBox != null && nameBox.Hit(p)) Focus(0);
        else if (passBox != null && passBox.Hit(p)) Focus(1);
        else if (loginBtn != null && loginBtn.Hit(p)) Submit(false);
        else if (signupBtn != null && signupBtn.Hit(p)) Submit(true);
        else if (logoutBtn != null && logoutBtn.Hit(p))
        {
            Accounts.SignOut();
            AudioManager.Sfx("card", 0.5f);
            accountBtn.label.Text = AccountLabel();
            OpenAccount();
        }
        else if (backBtn != null && backBtn.Hit(p)) { AudioManager.Sfx("card", 0.4f); ClosePanel(); }
    }

    /// <summary>Names keep to A-Z and 0-9; passwords take any printable character.</summary>
    void AddChar(char c)
    {
        if (c < ' ' || c == (char)127) return;
        if (field == 0)
        {
            c = char.ToUpperInvariant(c);
            if (nameIn.Length < Accounts.NameMax && ((c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))) nameIn += c;
        }
        else if (passIn.Length < Accounts.PassMax) passIn += c;
    }

    void Focus(int f)
    {
        field = f;
        if (TouchScreenKeyboard.isSupported)
            kb = TouchScreenKeyboard.Open(f == 0 ? nameIn : passIn, TouchScreenKeyboardType.Default, false, false, f == 1);
    }

    void Submit(bool create)
    {
        string err = create ? Accounts.Register(nameIn, passIn) : Accounts.SignIn(nameIn, passIn);
        if (err != null)
        {
            AudioManager.Sfx("card", 0.5f);
            msgTxt.Text = err;
            if (!create) passIn = "";
            return;
        }
        AudioManager.Sfx("ready", 0.55f);
        accountBtn.label.Text = AccountLabel();
        ClosePanel();
    }

    /// <summary>Password shows as + marks (the pixel font has no asterisk); the active field gets a blinking caret.</summary>
    void PaintFields()
    {
        bool blink = Mathf.Repeat(Time.unscaledTime, 1f) < 0.5f;
        nameBox.label.Text = nameIn + (field == 0 && blink ? "-" : "");
        passBox.label.Text = new string('+', passIn.Length) + (field == 1 && blink ? "-" : "");
        nameBox.label.SetColor(field == 0 ? Announcer.Yellow : Announcer.Cream);
        passBox.label.SetColor(field == 1 ? Announcer.Yellow : Announcer.Cream);
    }

    void ClosePanel()
    {
        if (panel) Destroy(panel.gameObject);
        panel = null;
        backBtn = optMusic = optSfx = null;
        nameBox = passBox = loginBtn = signupBtn = logoutBtn = null;
        msgTxt = null;
        nameIn = passIn = "";
        if (kb != null) { kb.active = false; kb = null; }
    }

    void PanelInput()
    {
        if (Time.unscaledTime < lockUntil) return;
        if (panel.name == "Account") { AccountInput(); return; }
        if (UiInput.Back() || (UiInput.Confirm() && optMusic == null))
        {
            AudioManager.Sfx("card", 0.4f);
            ClosePanel();
            return;
        }
        if (!UiInput.Up(out var p)) return;
        if (optMusic != null && optMusic.Hit(p)) ToggleMusic();
        else if (optSfx != null && optSfx.Hit(p)) CycleSfx();
        else if (backBtn != null && backBtn.Hit(p)) { AudioManager.Sfx("card", 0.4f); ClosePanel(); }
    }

    void ToggleMusic()
    {
        SaveSystem.Data.musicOn = !SaveSystem.Data.musicOn;
        SaveSystem.Save();
        AudioManager.ApplyMix();
        AudioManager.Sfx("card", 0.55f);
        OpenOptions();
    }

    void CycleSfx()
    {
        float v = SaveSystem.Data.sfxVolume;
        SaveSystem.Data.sfxVolume = v > 0.75f ? 0.5f : v > 0.25f ? 0f : 1f;
        SaveSystem.Save();
        AudioManager.Sfx("score", 0.9f);
        OpenOptions();
    }

    static void Quit()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    /// <summary>Pilot title from letters already delivered. Gold ending stays at 60.</summary>
    public static string RankName(int stars)
    {
        if (stars >= 60) return "LEGEND";
        if (stars >= 40) return "VETERAN";
        if (stars >= 26) return "ACE";
        if (stars >= 15) return "RUNNER";
        if (stars >= 6) return "PILOT";
        return "CADET";
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void SelfCheck()
    {
        bool ok = RankName(0) == "CADET" && RankName(6) == "PILOT" && RankName(15) == "RUNNER"
            && RankName(26) == "ACE" && RankName(40) == "VETERAN" && RankName(60) == "LEGEND" && RankName(75) == "LEGEND";
        if (!ok) Debug.LogError("[FrontMenu] rank names failed");
    }
}
