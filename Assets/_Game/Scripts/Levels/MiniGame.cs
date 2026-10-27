using System.Collections;
using UnityEngine;

/// <summary>
/// Base for level mini games that replace the Flappy gameplay roots (the biome background keeps scrolling behind).
/// Handles the READY/GO count-in, a top HUD line and reporting to <see cref="LevelFlow.Finish"/>.
/// </summary>
public abstract class MiniGame : MonoBehaviour
{
    /// <summary>Debug: let the game play itself (tests / screenshots).</summary>
    public static bool Bot;
    /// <summary>0..1 through the level, so the world light can shift. Flappy uses the pipe score instead.</summary>
    public static float Progress01;
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { Bot = false; Progress01 = 0f; }

    public const float W = 7.5f, H = 4.22f;
    protected const int Order = 30;

    protected LevelDef level;
    protected Transform root;
    protected bool running, over;
    protected float elapsed;
    protected int score;
    PixelText hudLeft, hudRight, hint;

    protected virtual void Start()
    {
        level = LevelFlow.Current ?? LevelDB.All[0];
        root = Ui.Root(GetType().Name, null);
        hudLeft = Ui.Txt(root, "", new Vector2(-7.1f, 3.8f), 1.1f, Color.white, Order + 50, PixelText.Align.Left);
        hudRight = Ui.Txt(root, "", new Vector2(7.1f, 3.8f), 1.1f, Announcer.Yellow, Order + 50, PixelText.Align.Right);
        hint = Ui.Txt(root, HintText, new Vector2(0f, -3.75f), 0.9f, Announcer.Cream, Order + 50);
        ParallaxWorld.Spawn(level.world, root, false);
        Build();
        if (level.twist == LevelTwist.Night)
            Ui.Box("Night", root, Vector2.zero, new Vector2(18f, 10f), new Color(0.04f, 0.05f, 0.12f, 0.38f), Order + 12);
        StartCoroutine(CountIn());
    }

    /// <summary>Sideways gust for Wind levels. Zero otherwise.</summary>
    protected float Gust => level.twist == LevelTwist.Wind ? Mathf.Sin(elapsed * 1.7f) * (0.7f + level.hard) : 0f;

    bool quotaPaid;

    /// <summary>One-shot side bonus worth the gap between 2 stars and 3. Never fails the level.</summary>
    protected void GrantQuota()
    {
        if (quotaPaid || level.twist != LevelTwist.Quota) return;
        quotaPaid = true;
        score += Mathf.Max(1, level.stars[2] - level.stars[1]);
        PopText("BONUS!", new Vector2(0f, 1.7f), 4);
        AudioManager.Sfx("super", 0.75f);
    }

    /// <summary>Bouncy word, a small shake, and a combo that climbs in pitch.</summary>
    protected void PopText(string word, Vector2 at, int combo = 1)
    {
        var t = Ui.Txt(root, word, at, 1.5f, Announcer.Yellow, Order + 56, PixelText.Align.Center, PixelText.Effect.Boil);
        Destroy(t.gameObject, 0.7f);
        float pitch = Mathf.Clamp(1f + (combo - 1) * 0.07f, 0.9f, 1.7f);
        AudioManager.Sfx("score", 0.5f, 0.02f, 0.02f, pitch);
        CameraFx.Shake(0.04f + 0.012f * combo, 0.1f);
    }

    protected abstract string HintText { get; }
    protected abstract void Build();
    protected abstract void Tick(float dt);

    IEnumerator CountIn()
    {
        yield return new WaitForSeconds(2.3f);   // level intro band
        var t = Ui.Txt(root, "READY?", new Vector2(0f, 0.6f), 3f, Announcer.Yellow, Order + 60, PixelText.Align.Center, PixelText.Effect.Boil);
        AudioManager.Sfx("ready", 0.8f, 0f);
        yield return new WaitForSeconds(0.8f);
        t.Text = "GO!";
        t.SetColor(Announcer.Teal);
        AudioManager.Sfx("wallop", 0.7f, 0f);
        if (GameState.I) GameState.I.Begin();   // background scrollers run while started
        running = true;
        yield return new WaitForSeconds(0.5f);
        Destroy(t.gameObject);
        yield return new WaitForSeconds(3f);
        if (hint) hint.gameObject.SetActive(false);
    }

    void Update()
    {
        if (!running || over) return;
        float dt = Time.deltaTime;
        elapsed += dt;
        if (level.goal > 0) Progress01 = Mathf.Clamp01(elapsed / level.goal);
        Tick(dt);
    }

    protected void Hud(string left, string right)
    {
        hudLeft.Text = left;
        hudRight.Text = right;
    }

    protected void End(bool won)
    {
        if (over) return;
        over = true;
        running = false;
        if (won)
        {
            AudioManager.Sfx("victory", 0.9f, 0f);
            Ui.Txt(root, "COMPLETE!", new Vector2(0f, 0.8f), 3f, Announcer.Yellow, Order + 60, PixelText.Align.Center, PixelText.Effect.Boil);
        }
        LevelFlow.Finish(won, score);
    }

    // ---------------------------------------------------------------- shared input
    /// <summary>-1 left, +1 right: keys, or holding the left / right half of the screen.</summary>
    protected static float Steer()
    {
        float x = CombatInput.Move().x;
        if (x != 0f) return Mathf.Sign(x);
        if (UiInput.Held(out var p)) return p.x < 0f ? -1f : 1f;
        return 0f;
    }

    protected static bool Hold() => CombatInput.Fire();

    protected static void Boom(Vector2 at, float scale = 1f)
    {
        if (GameKit.I) GameKit.Burst(GameKit.I.explosion, at, 18f, scale, Order + 20);
        AudioManager.Sfx("explosion", 0.6f);
        CameraFx.Shake(0.12f * scale, 0.2f);
    }

    protected static void Sparkle(Vector2 at, Color c, float scale = 0.8f)
    {
        if (GameKit.I) GameKit.Pop(GameKit.I.parryStar, at, 0.35f, scale, 1.2f, Order + 25, c);
    }
}
