using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Candy-Crush style progression. The single scene boots into the level map; picking a node reloads the scene with
/// <see cref="Current"/> set, which configures Flappy / boss runs or swaps the gameplay roots for a mini game.
/// Free play (keys 1/2/3 on the map) clears both flags and leaves the classic flow untouched.
/// </summary>
public static class LevelFlow
{
    public static LevelDef Current;
    public static bool ShowMap = true;
    /// <summary>Map node to centre on next time the map opens.</summary>
    public static int Focus;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Current = null; ShowMap = true; Focus = 0; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        SceneManager.sceneLoaded -= OnLoaded;
        SceneManager.sceneLoaded += OnLoaded;
        LevelRuntime.Ensure();
    }

    static void OnLoaded(Scene s, LoadSceneMode m) => LevelRuntime.Ensure();

    // ---------------------------------------------------------------- progress
    public static LevelRecord Record(int id, bool create = false)
    {
        var list = SaveSystem.Data.levels;
        foreach (var r in list) if (r.id == id) return r;
        if (!create) return null;
        var n = new LevelRecord { id = id };
        list.Add(n);
        return n;
    }

    public static int Stars(int id) { var r = Record(id); return r != null ? r.stars : 0; }
    public static bool Unlocked(int id) => id == 1 || (Stars(id - 1) > 0 && LevelDB.GateBlock(id) == 0);

    public const int ChestEvery = 10, ChestCoins = 25;
    public static int ChestsEarned => TotalStars() / ChestEvery;
    public static bool ChestOpen(int i) => i >= 0 && (SaveSystem.Data.chestMask & (1 << i)) != 0;
    public static bool ChestReady(int i) => i >= 0 && i < 31 && i < ChestsEarned && !ChestOpen(i);

    public static bool ClaimChest(int i)
    {
        if (!ChestReady(i)) return false;
        SaveSystem.Data.chestMask |= 1 << i;
        SaveSystem.Data.coins += ChestCoins;
        SaveSystem.Save();
        return true;
    }

    public static int TotalStars()
    {
        int n = 0;
        foreach (var r in SaveSystem.Data.levels) n += r.stars;
        return n;
    }

    /// <summary>Highest unlocked level (the map's "you are here").</summary>
    public static int Frontier()
    {
        int f = 1;
        while (f < LevelDB.Count && Unlocked(f + 1)) f++;
        return f;
    }

    // ---------------------------------------------------------------- navigation
    public static void Play(int id)
    {
        var lv = LevelDB.Get(id);
        if (lv == null) return;
        Current = lv;
        Focus = id;
        ShowMap = false;
        Last = null;
        GameState.CheckpointBoss = -1;
        Reload();
    }

    public static void Map()
    {
        if (Current != null) Focus = Current.id;
        Current = null;
        ShowMap = true;
        GameState.CheckpointBoss = -1;
        GameState.Mode = RunMode.Classic;
        Reload();
    }

    /// <summary>Free play from the map: the old mode-key flow.</summary>
    public static void FreePlay(RunMode mode)
    {
        Current = null;
        ShowMap = false;
        GameState.Mode = mode;
        GameState.CheckpointBoss = -1;
        Reload();
    }

    static void Reload()
    {
        CameraFx.ClearTime();
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // ---------------------------------------------------------------- results
    public class Outcome
    {
        public LevelDef level;
        public bool won;
        public int score, stars, newStars, coins;
    }

    public static Outcome Last { get; private set; }

    /// <summary>Score a finished level, save stars + star bonus, pay mini games. Returns null if no level is running.</summary>
    public static Outcome Finish(bool won, int score)
    {
        var lv = Current;
        if (lv == null) return null;
        var o = new Outcome { level = lv, score = score, stars = lv.StarsFor(won, score) };
        o.won = o.stars > 0;

        // flappy / boss runs already paid through GameState; mini games pay here
        if (lv.game == LevelGame.Flappy || lv.game == LevelGame.Boss)
            o.coins = GameState.I && GameState.I.LastResult != null ? GameState.I.LastResult.coinsEarned : 0;
        else
            o.coins = Economy.Grant(RunResult.Create(lv.game.ToString().ToLowerInvariant(), score, o.won, Time.timeSinceLevelLoad)).coinsEarned;

        var rec = Record(lv.id, true);
        o.newStars = Mathf.Max(0, o.stars - rec.stars);
        rec.stars = Mathf.Max(rec.stars, o.stars);
        rec.best = Mathf.Max(rec.best, score);
        int bonus = o.newStars * 15;
        SaveSystem.Data.coins += bonus;
        o.coins += bonus;
        SaveSystem.Save();
        Last = o;
        Debug.Log($"[Level] {lv.id} {lv.game} won={o.won} score={score} stars={o.stars} (+{o.newStars} new) coins+{o.coins}");
        if (LevelRuntime.I) LevelRuntime.I.ShowResult(o);
        return o;
    }
}

/// <summary>Per-scene driver: shows the map or sets up the chosen level, listens for run ends and draws the result screen.</summary>
public class LevelRuntime : MonoBehaviour
{
    public static LevelRuntime I { get; private set; }
    public LevelDef Level { get; private set; }
    bool finished;

    // art pulled from the scene before roots are hidden (the builder doesn't know about levels)
    public static Sprite[] Fly;
    public static Sprite[] BossIcons = new Sprite[3];
    public static Sprite[] Skies = new Sprite[4];
    public static Sprite Rocket, Torpedo;

    static readonly string[] GameplayRoots = { "Poeing", "Birds", "Bullets", "Bosses", "BossDirector", "HUD" };
    static readonly string[] KeepOnMap = { "Main Camera", "Game", "FxPool", "Levels" };

    public static void Ensure()
    {
        if (FindAnyObjectByType<LevelRuntime>()) return;
        new GameObject("Levels").AddComponent<LevelRuntime>();
    }

    void Awake()
    {
        I = this;
        GrabArt();
        Level = LevelFlow.Current;
        if (LevelFlow.ShowMap) { OpenMap(); return; }
        if (Level == null) return;   // free play
        GameState.OnlyBoss = -1;
        var biome = FindAnyObjectByType<BiomeDirector>();
        if (biome) biome.Set(LevelDB.WorldBiome[Level.world]);
        switch (Level.game)
        {
            case LevelGame.Flappy: GameState.Mode = RunMode.Flappy; break;
            case LevelGame.Boss: GameState.Mode = RunMode.BossRush; GameState.OnlyBoss = Level.goal; break;
            default:
                GameState.Mode = RunMode.Flappy;
                SetRoots(GameplayRoots, false);
                switch (Level.game)
                {
                    case LevelGame.Missile: gameObject.AddComponent<MissileDodge>(); break;
                    case LevelGame.Stack: gameObject.AddComponent<CargoStack>(); break;
                    case LevelGame.Landing: gameObject.AddComponent<PerfectLanding>(); break;
                    case LevelGame.Supply: gameObject.AddComponent<SupplyDrop>(); break;
                    case LevelGame.Bolt: gameObject.AddComponent<BoltLane>(); break;
                    case LevelGame.Refuel: gameObject.AddComponent<SkyRefuel>(); break;
                }
                break;
        }
        if (Level.twist == LevelTwist.Night && (Level.game == LevelGame.Flappy || Level.game == LevelGame.Boss))
            Ui.Box("Night", transform, Vector2.zero, new Vector2(20f, 12f), new Color(0.02f, 0.03f, 0.1f, 0.34f), 13);
    }

    void OnEnable() { GameState.Died += OnDied; GameState.Won += OnWon; GameState.ScoreChanged += OnScore; }
    void OnDisable() { GameState.Died -= OnDied; GameState.Won -= OnWon; GameState.ScoreChanged -= OnScore; }
    void OnDestroy() { if (I == this) I = null; }

    void Start()
    {
        if (Level != null && !LevelFlow.ShowMap) StartCoroutine(Intro());
    }

    static void GrabArt()
    {
        var pc = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
        if (pc) Fly = pc.fly;
        var w = FindAnyObjectByType<PlayerWeapons>(FindObjectsInactive.Include);
        if (w) Rocket = w.exSprite;
        var pool = FindAnyObjectByType<BulletPool>(FindObjectsInactive.Include);
        if (pool) Torpedo = pool.torpedo;
        var dir = FindAnyObjectByType<BossDirector>(FindObjectsInactive.Include);
        if (dir && dir.bosses != null)
            for (int i = 0; i < dir.bosses.Length && i < 3; i++)
                if (dir.bosses[i]) BossIcons[i] = dir.bosses[i].GetComponent<SpriteRenderer>().sprite;
        var biome = FindAnyObjectByType<BiomeDirector>(FindObjectsInactive.Include);
        if (biome && biome.roots != null)
            for (int b = 0; b < biome.roots.Length && b < 4; b++)
            {
                if (!biome.roots[b]) continue;
                foreach (var sr in biome.roots[b].GetComponentsInChildren<SpriteRenderer>(true))
                    if (sr.sprite && sr.sprite.name.Contains("sky")) { Skies[b] = sr.sprite; break; }
            }
    }

    static void SetRoots(string[] names, bool on)
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (System.Array.IndexOf(names, go.name) >= 0) go.SetActive(on);
    }

    void OpenMap()
    {
        foreach (var go in SceneManager.GetActiveScene().GetRootGameObjects())
            if (System.Array.IndexOf(KeepOnMap, go.name) < 0) go.SetActive(false);
        FilmFilter.IrisSet(1f);
        gameObject.AddComponent<LevelMap>();
    }

    // ---------------------------------------------------------------- flappy / boss endings
    void OnScore(int s)
    {
        var gs = GameState.I;
        if (Level == null || finished || gs == null) return;
        if (Level.game == LevelGame.Flappy && s >= Level.stars[2]) gs.Win();   // three stars ends the flight
    }

    void OnDied()
    {
        if (Level == null || finished) return;
        if (Level.game == LevelGame.Flappy) LevelFlow.Finish(false, GameState.I.Score);
        else if (Level.game == LevelGame.Boss) LevelFlow.Finish(false, 0);
    }

    void OnWon()
    {
        if (Level == null || finished) return;
        var gs = GameState.I;
        if (Level.game == LevelGame.Flappy) LevelFlow.Finish(true, gs.Score);
        else if (Level.game == LevelGame.Boss) LevelFlow.Finish(true, Mathf.Max(0, 10 - gs.HitsTaken));   // 3 stars: <=1 hit, 2 stars: <=4
    }

    // ---------------------------------------------------------------- intro + result
    IEnumerator Intro()
    {
        var root = Ui.Root("LevelIntro", transform);
        var c = Ui.Ink; c.a = 0.55f;
        Ui.Box("Band", root, new Vector2(0f, 1.6f), new Vector2(20f, 1.7f), c, 180);
        Ui.Txt(root, "LEVEL " + Level.id, new Vector2(0f, 2.05f), 2f, Announcer.Yellow, 181, PixelText.Align.Center, PixelText.Effect.Boil);
        Ui.Txt(root, Level.GoalText, new Vector2(0f, 1.2f), 1.3f, Color.white, 181);
        yield return new WaitForSeconds(2.2f);
        Destroy(root.gameObject);
    }

    Transform result;
    Ui.Button retryBtn, mapBtn, nextBtn;
    float inputAt = float.MaxValue;

    public void ShowResult(LevelFlow.Outcome o)
    {
        if (finished) return;
        finished = true;
        StartCoroutine(Result(o));
    }

    IEnumerator Result(LevelFlow.Outcome o)
    {
        yield return new WaitForSecondsRealtime(o.won ? 0.9f : 1.1f);
        result = Ui.Root("LevelResult", transform);
        var dim = Ui.Ink; dim.a = 0.6f;
        Ui.Box("Dim", result, Vector2.zero, new Vector2(20f, 12f), dim, 190);
        Ui.Plaque(result, new Vector2(0f, -0.2f), new Vector2(9f, 5.6f), 191);
        var title = Ui.Txt(result, o.won ? "LEVEL CLEAR!" : "LEVEL FAILED", new Vector2(0f, 3.1f), 2.6f,
            o.won ? Announcer.Yellow : Announcer.Red, 192, PixelText.Align.Center, PixelText.Effect.Boil);
        Ui.Txt(result, "LEVEL " + o.level.id + "  " + o.level.GameName, new Vector2(0f, 1.9f), 1f, Announcer.Teal, 192);
        if (o.won) AudioManager.Sfx("victory", 0.8f, 0f); else AudioManager.Sfx("death", 0.6f, 0f);

        var star = GameKit.I ? GameKit.I.parryStar : Ui.Disc;
        for (int i = 0; i < 3; i++)
        {
            var pos = new Vector2((i - 1) * 1.55f, 0.85f + (i == 1 ? 0.25f : 0f));
            var dark = Ui.Spr("StarSlot", result, star, pos, 192, 1.2f);
            dark.color = new Color(0.2f, 0.16f, 0.14f, 0.85f);
            if (i >= o.stars) continue;
            yield return new WaitForSecondsRealtime(0.3f);
            var s = Ui.Spr("Star", result, star, pos, 193, 1.2f);
            s.color = Announcer.Yellow;
            StartCoroutine(Pop(s.transform, 1.2f));
            AudioManager.Sfx("card", 0.8f, 0f, 0.03f, 1f + i * 0.15f);
            CameraFx.Shake(0.05f, 0.12f);
        }
        Ui.Txt(result, o.level.ScoreName + "  " + o.score, new Vector2(0f, -0.35f), 1.3f, Color.white, 192);
        Ui.Txt(result, "+" + o.coins + " COINS" + (o.newStars > 0 ? "   NEW STARS +" + o.newStars : ""), new Vector2(0f, -0.95f), 1f, Announcer.Yellow, 192);

        bool hasNext = o.won && LevelDB.Get(o.level.id + 1) != null && LevelFlow.Unlocked(o.level.id + 1);
        float bx = hasNext ? -2.7f : -1.5f;
        retryBtn = Ui.Btn(result, "RETRY", new Vector2(bx, -2.1f), new Vector2(2.4f, 0.9f), Color.white, 192);
        mapBtn = Ui.Btn(result, "MAP", new Vector2(bx + (hasNext ? 2.7f : 3f), -2.1f), new Vector2(2.4f, 0.9f), Announcer.Teal, 192);
        nextBtn = hasNext ? Ui.Btn(result, "NEXT", new Vector2(2.7f, -2.1f), new Vector2(2.4f, 0.9f), Announcer.Yellow, 192) : null;
        Ui.Txt(result, hasNext ? "SPACE: NEXT   R: RETRY   ESC: MAP" : "SPACE/R: RETRY   ESC: MAP", new Vector2(0f, -3.4f), 0.8f, Announcer.Cream, 192);
        inputAt = Time.unscaledTime + 0.3f;
    }

    void Update()
    {
        if (!result || Time.unscaledTime < inputAt) return;
        if (UiInput.Up(out var p))
        {
            if (retryBtn != null && retryBtn.Hit(p)) { LevelFlow.Play(Level.id); return; }
            if (mapBtn != null && mapBtn.Hit(p)) { LevelFlow.Map(); return; }
            if (nextBtn != null && nextBtn.Hit(p)) { LevelFlow.Play(Level.id + 1); return; }
        }
        if (UiInput.Back()) { LevelFlow.Map(); return; }
        if (UiInput.RetryKey()) { LevelFlow.Play(Level.id); return; }
        if (UiInput.Confirm() || UiInput.NextKey()) LevelFlow.Play(nextBtn != null ? Level.id + 1 : Level.id);
    }

    public static IEnumerator Pop(Transform t, float scale)
    {
        for (float k = 0f; k < 0.25f && t; k += Time.unscaledDeltaTime)
        {
            float e = k / 0.25f;
            t.localScale = Vector3.one * scale * (1f + 0.6f * Mathf.Sin(e * Mathf.PI) * (1f - e) + (e < 0.15f ? -0.5f : 0f));
            yield return null;
        }
        if (t) t.localScale = Vector3.one * scale;
    }
}
