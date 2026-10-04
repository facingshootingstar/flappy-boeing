using System.Collections;
using UnityEngine;

/// <summary>
/// HUD. Flappy phase: score + tap prompt. Combat: HP plaque and five super cards (bottom left).
/// End screens are built at runtime: GAME OVER (Flappy), YOU DIED! with gauntlet progress and retry (combat), YOU WIN! with stats and a grade.
/// </summary>
public class HudController : MonoBehaviour
{
    public DigitDisplay score;
    public GameObject tapPrompt;
    public BossDirector director;

    static readonly Color Ink = new Color32(42, 33, 28, 255);
    const int Order = 100;

    static Sprite pixel;
    Transform combatRoot;
    SpriteRenderer hpPlaque;
    PixelText hpText;
    SpriteRenderer[] cardBack, cardFill, cardFace;
    readonly bool[] full = new bool[PlayerWeapons.MaxCards];
    readonly float[] flipT = new float[PlayerWeapons.MaxCards];
    int lastHp = -1;

    Transform screen;
    PixelText prompt;
    float inputAt = float.MaxValue;
    enum Screen { None, Flappy, Died, Won }
    Screen mode;

    void OnEnable() { GameState.ScoreChanged += OnScore; GameState.Died += OnDied; GameState.Won += OnWon; }
    void OnDisable() { GameState.ScoreChanged -= OnScore; GameState.Died -= OnDied; GameState.Won -= OnWon; }

    void Start()
    {
        score.Show(GameState.I ? GameState.I.Score : 0);
        tapPrompt.SetActive(true);
        BuildCombatHud();
    }

    void OnScore(int s) { score.Show(s); }

    // ---------------------------------------------------------------- building blocks
    static Sprite Pixel
    {
        get
        {
            if (pixel) return pixel;
            var tex = new Texture2D(1, 1) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white); tex.Apply();
            pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
            return pixel;
        }
    }

    static SpriteRenderer Box(string name, Transform parent, Vector2 pos, Vector2 size, Color c, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = new Vector3(size.x, size.y, 1f);
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = Pixel; r.color = c; r.sortingOrder = order;
        return r;
    }

    static SpriteRenderer Spr(string name, Transform parent, Sprite s, Vector2 pos, int order, float scale = 1f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var r = go.AddComponent<SpriteRenderer>();
        r.sprite = s; r.sortingOrder = order;
        return r;
    }

    static SpriteRenderer Plaque(Transform parent, Vector2 pos, Vector2 size, int order)
    {
        var r = Spr("Plaque", parent, GameKit.I ? GameKit.I.plaque : null, pos, order);
        r.drawMode = SpriteDrawMode.Sliced;
        r.size = size;
        return r;
    }

    static PixelText Txt(Transform parent, string s, Vector2 pos, float scale, Color c, int order,
        PixelText.Align align = PixelText.Align.Center, PixelText.Effect fx = PixelText.Effect.None)
    {
        var go = new GameObject("Text");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = Vector3.one * scale;
        var t = go.AddComponent<PixelText>();
        t.sortingOrder = order; t.align = align; t.effect = fx; t.color = c;
        t.Text = s;
        t.Rebuild();
        return t;
    }

    // ---------------------------------------------------------------- combat HUD
    void BuildCombatHud()
    {
        combatRoot = new GameObject("CombatHud").transform;
        combatRoot.SetParent(transform, false);
        hpPlaque = Plaque(combatRoot, new Vector2(-6.55f, -3.78f), new Vector2(1.5f, 0.55f), Order);
        hpText = Txt(combatRoot, "HP 3", new Vector2(-6.55f, -3.78f), 1f, Color.white, Order + 1);
        var kit = GameKit.I;
        int n = PlayerWeapons.MaxCards;
        cardBack = new SpriteRenderer[n]; cardFill = new SpriteRenderer[n]; cardFace = new SpriteRenderer[n];
        for (int i = 0; i < n; i++)
        {
            var at = new Vector2(-5.4f + i * 0.45f, -3.78f);
            cardBack[i] = Spr("CardBack" + i, combatRoot, kit ? kit.cards[0] : null, at, Order);
            cardBack[i].color = new Color(0.35f, 0.28f, 0.25f, 0.85f);
            cardFill[i] = Spr("CardFill" + i, combatRoot, kit ? kit.cards[0] : null, at, Order + 1);
            cardFace[i] = Spr("CardFace" + i, combatRoot, kit ? kit.cards[1] : null, at, Order + 2);
            cardFace[i].gameObject.SetActive(false);
        }
        combatRoot.gameObject.SetActive(false);
    }

    void UpdateCombatHud()
    {
        var ph = PlayerHealth.I;
        int hp = ph ? ph.Hp : 3;
        if (hp != lastHp) { lastHp = hp; hpText.Text = "HP " + Mathf.Max(0, hp); }
        bool danger = hp <= 1 && Mathf.Repeat(Time.unscaledTime * 3f, 1f) < 0.5f;
        hpPlaque.color = danger ? Announcer.Red : Color.white;

        float meter = PlayerWeapons.Meter;
        for (int i = 0; i < cardBack.Length; i++)
        {
            float f = Mathf.Clamp01(meter - i);
            bool isFull = f >= 0.999f;
            if (isFull && !full[i]) { flipT[i] = 0f; AudioManager.Sfx("card", 0.5f, 0.05f, 0.05f, 1.3f); }
            full[i] = isFull;
            cardFace[i].gameObject.SetActive(isFull);
            cardFill[i].gameObject.SetActive(!isFull && f > 0f);
            if (isFull)
            {
                flipT[i] += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(flipT[i] / 0.18f);
                float bounce = PlayerWeapons.Cards >= PlayerWeapons.MaxCards ? 1f + 0.08f * Mathf.Sin(Time.unscaledTime * 10f + i) : 1f;
                cardFace[i].transform.localScale = new Vector3(Mathf.Abs(Mathf.Cos((1f - k) * Mathf.PI / 2f)) * bounce, bounce, 1f);
            }
            else if (f > 0f)
            {
                const float h = 0.5f;
                cardFill[i].transform.localScale = new Vector3(1f, f, 1f);
                cardFill[i].transform.localPosition = new Vector3(-5.4f + i * 0.45f, -3.78f - h / 2f * (1f - f), 0f);
            }
        }
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null) return;
        if (gs.Started && tapPrompt.activeSelf) tapPrompt.SetActive(false);
        bool showScore = !gs.Combat && mode == Screen.None;
        if (score.gameObject.activeSelf != showScore) score.gameObject.SetActive(showScore);
        bool combat = gs.Combat && gs.Alive;
        if (combatRoot.gameObject.activeSelf != combat) combatRoot.gameObject.SetActive(combat);
        if (combat) UpdateCombatHud();

        if (mode == Screen.None) return;
        if (prompt) prompt.gameObject.SetActive(Time.unscaledTime >= inputAt && Mathf.Repeat(Time.unscaledTime * 1.5f, 1f) < 0.7f);
        if (Time.unscaledTime < inputAt) return;
        if (mode == Screen.Died)
        {
            if (CombatInput.RetryPressed()) gs.Retry();
            else if (CombatInput.RestartPressed()) gs.Restart();
        }
        else if (TapInput.Pressed() || CombatInput.RetryPressed() || CombatInput.RestartPressed()) gs.Restart();
    }

    // ---------------------------------------------------------------- end screens
    void OnDied()
    {
        var gs = GameState.I;
        StartCoroutine(gs.Combat ? DiedScreen() : FlappyScreen());
    }

    void OnWon() { StartCoroutine(WinScreen()); }

    Transform NewScreen(float dim)
    {
        if (screen) Destroy(screen.gameObject);
        screen = new GameObject("EndScreen").transform;
        screen.SetParent(transform, false);
        var c = Ink; c.a = dim;
        Box("Dim", screen, Vector2.zero, new Vector2(20f, 12f), c, 140);
        return screen;
    }

    IEnumerator FlappyScreen()
    {
        mode = Screen.Flappy;
        yield return new WaitForSecondsRealtime(0.5f);
        var gs = GameState.I;
        var s = NewScreen(0.35f);
        Txt(s, "GAME OVER", new Vector2(0f, 1.7f), 3f, Announcer.Red, 142, PixelText.Align.Center, PixelText.Effect.Boil);
        Plaque(s, new Vector2(0f, -0.35f), new Vector2(5f, 2.3f), 141);
        Txt(s, "SCORE  " + gs.Score, new Vector2(0f, 0.35f), 1.5f, Color.white, 142);
        Txt(s, "BEST  " + gs.Best, new Vector2(0f, -0.3f), 1.5f, Announcer.Yellow, 142);
        prompt = Txt(s, "J/SPACE: RETRY", new Vector2(0f, -1.05f), 1f, Announcer.Teal, 142);
        inputAt = Time.unscaledTime + 0.4f;
    }

    IEnumerator DiedScreen()
    {
        mode = Screen.Died;
        yield return new WaitForSecondsRealtime(0.9f);
        var gs = GameState.I;
        var s = NewScreen(0.55f);
        AudioManager.Duck(0.35f, 0.4f);
        var title = Txt(s, "YOU DIED!", new Vector2(0f, 1.9f), 3f, Announcer.Red, 142, PixelText.Align.Center, PixelText.Effect.Boil);
        StartCoroutine(PopIn(title.transform, 3f));
        Plaque(s, new Vector2(0f, -0.55f), new Vector2(9f, 2.6f), 141);

        // gauntlet progress: 3 bosses x 3 phases
        const float x0 = -3.8f, x1 = 3.8f, by = -0.45f;
        float boss = Health.Boss ? Health.Boss.PhaseFraction : 0f;
        float progress = gs.Progress(boss);
        Box("Line", s, new Vector2(0f, by), new Vector2(x1 - x0, 0.08f), new Color(0.35f, 0.28f, 0.25f), 142);
        int ticks = GameState.BossCount * GameState.PhasesPerBoss;
        for (int k = 0; k <= ticks; k++)
            Box("Tick", s, new Vector2(Mathf.Lerp(x0, x1, k / (float)ticks), by), new Vector2(0.06f, k % 3 == 0 ? 0.3f : 0.16f), Announcer.Cream, 143);
        if (director != null)
            for (int i = 0; i < director.bosses.Length && i < GameState.BossCount; i++)
            {
                var bsr = director.bosses[i] ? director.bosses[i].GetComponent<SpriteRenderer>() : null;
                if (bsr == null || bsr.sprite == null) continue;
                float sc = 0.6f / bsr.sprite.bounds.size.y;
                var icon = Spr("BossIcon", s, bsr.sprite, new Vector2(Mathf.Lerp(x0, x1, (i + 1f) / GameState.BossCount), by + 0.5f), 143, sc);
                if (i < gs.BossIndex) icon.color = new Color(0.45f, 0.4f, 0.38f);
            }
        var fill = Box("Fill", s, new Vector2(x0, by), new Vector2(0.01f, 0.08f), Announcer.Teal, 143);
        var pc = PlayerController.I;
        var marker = Spr("Marker", s, pc && pc.fly != null && pc.fly.Length > 0 ? pc.fly[0] : null, new Vector2(x0, by - 0.42f), 144, 0.55f);
        prompt = Txt(s, "J: RETRY BOSS    R: RESTART", new Vector2(0f, -1.4f), 1f, Announcer.Teal, 142);
        prompt.gameObject.SetActive(false);
        // slide the marker to the furthest point reached
        for (float t = 0f; t < 1.1f; t += Time.unscaledDeltaTime)
        {
            float x = Mathf.Lerp(x0, x1, progress * Mathf.SmoothStep(0f, 1f, t / 1.1f));
            marker.transform.localPosition = new Vector3(x, by - 0.42f, 0f);
            fill.transform.localScale = new Vector3(Mathf.Max(0.01f, x - x0), 0.08f, 1f);
            fill.transform.localPosition = new Vector3((x0 + x) / 2f, by, 0f);
            yield return null;
        }
        Txt(s, Mathf.RoundToInt(progress * 100f) + "%", new Vector2(Mathf.Lerp(x0, x1, progress), by - 0.95f), 1f, Announcer.Yellow, 144);
        inputAt = Time.unscaledTime + 0.2f;
    }

    IEnumerator WinScreen()
    {
        mode = Screen.Won;
        yield return new WaitForSecondsRealtime(0.4f);
        var gs = GameState.I;
        var s = NewScreen(0.6f);
        var title = Txt(s, "YOU WIN!", new Vector2(0f, 2.9f), 3f, Announcer.Yellow, 142, PixelText.Align.Center, PixelText.Effect.Boil);
        StartCoroutine(PopIn(title.transform, 3f));
        Plaque(s, new Vector2(0f, -0.35f), new Vector2(8f, 4.5f), 141);

        int hp = PlayerHealth.I ? PlayerHealth.I.Hp : 3;
        int secs = Mathf.RoundToInt(gs.FightTime);
        string[,] rows =
        {
            { "TIME", (secs / 60) + ":" + (secs % 60).ToString("00") },
            { "HP BONUS", Mathf.Max(0, hp) + "/3" },
            { "PARRY", gs.Parries.ToString() },
            { "SUPER", gs.Supers.ToString() },
            { "HITS", gs.HitsTaken.ToString() },
            { "SCORE", gs.Score.ToString() },
        };
        for (int i = 0; i < rows.GetLength(0); i++)
        {
            yield return new WaitForSecondsRealtime(0.28f);
            float y = 1.3f - i * 0.5f;
            Txt(s, rows[i, 0], new Vector2(-3.4f, y), 1f, Announcer.Cream, 142, PixelText.Align.Left);
            Txt(s, rows[i, 1], new Vector2(0.9f, y), 1f, Color.white, 142, PixelText.Align.Right);
            AudioManager.Sfx("card", 0.6f, 0.05f);
        }
        yield return new WaitForSecondsRealtime(0.45f);
        Txt(s, "GRADE", new Vector2(2.4f, 1.2f), 1f, Announcer.Cream, 142);
        var grade = Txt(s, Grade(gs), new Vector2(2.4f, 0.15f), 4f, Announcer.Pink, 142, PixelText.Align.Center, PixelText.Effect.Boil);
        StartCoroutine(PopIn(grade.transform, 4f));
        AudioManager.Sfx("wallop", 1f, 0f);
        CameraFx.Shake(0.1f, 0.25f);
        Debug.Log("[Win] time=" + secs + " hits=" + gs.HitsTaken + " parries=" + gs.Parries + " supers=" + gs.Supers + " grade=" + grade.Text);
        prompt = Txt(s, "J/SPACE: PLAY AGAIN", new Vector2(0f, -2.15f), 1f, Announcer.Teal, 142);
        inputAt = Time.unscaledTime + 0.8f;
    }

    public static string Grade(GameState gs)
    {
        float pts = 30f * Mathf.Clamp01((420f - gs.FightTime) / 240f)
                  + Mathf.Max(0, 30 - 10 * gs.HitsTaken)
                  + 8 * Mathf.Min(gs.Parries, 3)
                  + 8 * Mathf.Min(gs.Supers, 2);
        if (pts >= 90f) return "A+";
        if (pts >= 80f) return "A";
        if (pts >= 70f) return "A-";
        if (pts >= 60f) return "B+";
        if (pts >= 50f) return "B";
        if (pts >= 40f) return "B-";
        if (pts >= 30f) return "C";
        return "D";
    }

    static IEnumerator PopIn(Transform t, float scale)
    {
        for (float k = 0f; k < 1f; k += Time.unscaledDeltaTime / 0.16f)
        {
            t.localScale = Vector3.one * Mathf.Round(Mathf.Lerp(scale + 2f, scale, 1f - (1f - k) * (1f - k)) * 4f) / 4f;
            yield return null;
        }
        t.localScale = Vector3.one * scale;
    }
}
