using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Gate shown before the front menu while nobody is signed in. Name + password go to Firebase through
/// <see cref="Accounts"/>; success opens <see cref="FrontMenu"/>. A saved session is refreshed silently first.
/// Without StreamingAssets/firebase.json the screen says FIREBASE KEY MISSING and stays blocked.
/// </summary>
public class LoginScreen : MonoBehaviour
{
    const int Order = 180;

    Transform root;
    Ui.Button nameBox, passBox, loginBtn, signupBtn;
    PixelText msgTxt;
    string nameIn = "", passIn = "";
    int field;
    bool waiting, blocked;
    float lockUntil;
    TouchScreenKeyboard kb;
    MenuIntro intro;
    readonly List<BtnFx> fx = new List<BtnFx>();

    void Start()
    {
        root = Ui.Root("Login", transform);
        Ui.Box("SkyTop", root, new Vector2(0f, 3.2f), new Vector2(26f, 8f), new Color32(104, 170, 226, 255), Order);
        Ui.Box("SkyMid", root, new Vector2(0f, -1.6f), new Vector2(26f, 2.4f), new Color32(158, 206, 238, 255), Order);
        Ui.Box("SkyLow", root, new Vector2(0f, -4f), new Vector2(26f, 4f), new Color32(212, 230, 240, 255), Order);
        MenuSky.Build(root, Order + 1, 0.85f, 1.1f, 3.3f);

        intro = new MenuIntro();
        var title = Ui.Txt(root, CutsceneDB.Title, new Vector2(0f, 3.4f), 1.25f, Announcer.Yellow, Order + 8,
            PixelText.Align.Center, PixelText.Effect.Boil);
        intro.Add(title.transform, new Vector2(0f, 6f), 0f, 0.9f, true);

        var plaque = Ui.Plaque(root, new Vector2(0f, -0.55f), new Vector2(8.6f, 5.4f), Order + 21);
        intro.Add(plaque.transform, new Vector2(0f, -9f), 0.35f, 0.6f);
        var head = Ui.Txt(root, "LOG IN", new Vector2(0f, 1.6f), 1.2f, Announcer.Yellow, Order + 22,
            PixelText.Align.Center, PixelText.Effect.Boil);
        var nameLbl = Ui.Txt(root, "NAME", new Vector2(-3.65f, 0.65f), 0.7f, Announcer.Cream, Order + 22, PixelText.Align.Left);
        var passLbl = Ui.Txt(root, "PASS", new Vector2(-3.65f, -0.3f), 0.7f, Announcer.Cream, Order + 22, PixelText.Align.Left);
        nameBox = Ui.Btn(root, "", new Vector2(0.75f, 0.65f), new Vector2(5.2f, 0.72f), Announcer.Cream, Order + 22);
        passBox = Ui.Btn(root, "", new Vector2(0.75f, -0.3f), new Vector2(5.2f, 0.72f), Announcer.Cream, Order + 22);
        nameBox.label.transform.localScale = passBox.label.transform.localScale = Vector3.one * 0.8f;
        msgTxt = Ui.Txt(root, "", new Vector2(0f, -1.1f), 0.62f, Announcer.Red, Order + 22);
        loginBtn = Ui.Btn(root, "LOG IN", new Vector2(-1.9f, -2.0f), new Vector2(3.4f, 0.75f), Announcer.Cream, Order + 22);
        signupBtn = Ui.Btn(root, "SIGN UP", new Vector2(1.9f, -2.0f), new Vector2(3.4f, 0.75f), Announcer.Cream, Order + 22);

        intro.Add(head.transform, new Vector2(0f, -9f), 0.35f, 0.6f);
        intro.Add(nameLbl.transform, new Vector2(0f, -9f), 0.35f, 0.6f);
        intro.Add(passLbl.transform, new Vector2(0f, -9f), 0.35f, 0.6f);
        intro.Add(nameBox, new Vector2(0f, -9f), 0.4f);
        intro.Add(passBox, new Vector2(0f, -9f), 0.5f);
        intro.Add(msgTxt.transform, new Vector2(0f, -9f), 0.5f, 0.5f);
        intro.Add(loginBtn, new Vector2(-9f, 0f), 0.65f);
        intro.Add(signupBtn, new Vector2(9f, 0f), 0.75f);
        lockUntil = Time.unscaledTime + intro.EndTime;
        foreach (var b in new[] { nameBox, passBox, loginBtn, signupBtn }) fx.Add(new BtnFx(b));

        AudioManager.Music("music_flappy", 0.6f);
        UiInput.Typed();

        if (Accounts.KeyMissing)
        {
            blocked = true;
            msgTxt.Text = "FIREBASE KEY MISSING";
        }
        else if (Accounts.HasSession)
        {
            waiting = true;
            msgTxt.SetColor(Announcer.Cream);
            msgTxt.Text = "SIGNING IN...";
            string saved = Accounts.SavedName;
            Accounts.Restore(err =>
            {
                waiting = false;
                if (this == null) return;
                if (err == null) { ToMenu(); return; }
                if (saved.Length > 0) nameIn = saved;
                if (err != "SESSION EXPIRED") Fail(err); else msgTxt.Text = "";
            });
        }
        PaintFields();
    }

    void Update()
    {
        if (root == null) return;
        intro.Tick();
        BtnFx.TickAll(fx);
        PaintFields();
        if (Time.unscaledTime < lockUntil) { UiInput.Typed(); return; }
        if (blocked || waiting) { UiInput.Typed(); return; }

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
        if (!UiInput.Up(out var p)) return;
        if (nameBox.Hit(p)) Focus(0);
        else if (passBox.Hit(p)) Focus(1);
        else if (loginBtn.Hit(p)) Submit(false);
        else if (signupBtn.Hit(p)) Submit(true);
    }

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
        string err = Accounts.CheckName(nameIn) ?? Accounts.CheckPass(passIn);
        if (err != null) { Fail(err); return; }
        waiting = true;
        msgTxt.SetColor(Announcer.Cream);
        msgTxt.Text = "PLEASE WAIT...";
        AudioManager.Sfx("ready", 0.4f);
        System.Action<string> done = e =>
        {
            waiting = false;
            if (this == null) return;
            if (e == null) { AudioManager.Sfx("ready", 0.6f); ToMenu(); }
            else { passIn = ""; Fail(e); }
        };
        if (create) Accounts.Register(nameIn, passIn, done); else Accounts.SignIn(nameIn, passIn, done);
    }

    void Fail(string line)
    {
        AudioManager.Sfx("card", 0.5f);
        msgTxt.SetColor(Announcer.Red);
        msgTxt.Text = line;
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

    void ToMenu()
    {
        if (kb != null) { kb.active = false; kb = null; }
        if (root) Destroy(root.gameObject);
        root = null;
        gameObject.AddComponent<FrontMenu>();
        Destroy(this);
    }
}
