using System.Collections;
using UnityEngine;

/// <summary>
/// Runs the gauntlet. At score 30 the iris closes on the jet, the birds go away and the WASD fight starts;
/// each boss gets its own biome (swapped while the iris is shut), a READY? / WALLOP! intro and a score reward.
/// A retry from the death screen reloads the scene and jumps straight to the checkpoint boss.
/// </summary>
public class BossDirector : MonoBehaviour
{
    public BossBase[] bosses;
    public BirdSpawner birds;
    public BiomeDirector biomes;
    public int[] biomeFor = { 1, 2, 3 };
    public int[] reward = { 40, 50, 60 };

    static bool controlsShown;
    bool running;

    static readonly string[] Controls =
    {
        "CONTROLS",
        "WASD/ARROWS: MOVE",
        "HOLD J/SPACE: SHOOT",
        "K: EX - 5 CARDS: SUPER",
        "SHIFT/L: DASH",
        "DASH INTO PINK: PARRY!",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { controlsShown = false; }

    void OnEnable() { GameState.ScoreChanged += OnScore; }
    void OnDisable() { GameState.ScoreChanged -= OnScore; }

    void Start()
    {
        foreach (var b in bosses) if (b) b.gameObject.SetActive(false);
        var gs = GameState.I;
        if (gs != null && (gs.FromCheckpoint || GameState.Mode == RunMode.BossRush))
        {
            FilmFilter.IrisSet(0f);
            StopBirds();
            gs.EnterCombat();
            running = true;
            StartCoroutine(Gauntlet(Mathf.Clamp(gs.BossIndex, 0, bosses.Length - 1)));
        }
    }

    void OnScore(int s)
    {
        var gs = GameState.I;
        if (running || gs == null || gs.Combat || s < GameState.CombatScore || GameState.Mode == RunMode.Flappy) return;
        running = true;
        StartCoroutine(ToCombat());
    }

    void StopBirds()
    {
        if (!birds) return;
        birds.ClearAll();
        birds.enabled = false;
    }

    IEnumerator ToCombat()
    {
        var gs = GameState.I;
        AudioManager.StopMusic(0.6f);
        AudioManager.Sfx("iris", 0.8f, 0f);
        var pc = PlayerController.I;
        yield return FilmFilter.IrisClose(0.7f, pc ? pc.transform.position : (Vector3?)null);
        if (!gs.Alive) yield break;
        StopBirds();
        gs.EnterCombat();
        yield return Gauntlet(0);
    }

    IEnumerator Gauntlet(int start)
    {
        var gs = GameState.I;
        for (int i = start; i < bosses.Length; i++)
        {
            var boss = bosses[i];
            gs.SetBoss(i, 0);
            if (!FilmFilter.IrisClosed) { AudioManager.Sfx("iris", 0.7f, 0f); yield return FilmFilter.IrisClose(0.6f); }
            if (biomes) biomes.Set(i < biomeFor.Length ? biomeFor[i] : 1);
            if (BulletPool.I) BulletPool.I.ClearAll();
            if (PlayerController.I) PlayerController.I.Place(new Vector2(-5f, 0f));
            AudioManager.Music("music_boss", 0.3f);
            yield return new WaitForSecondsRealtime(0.3f);
            AudioManager.Sfx("iris", 0.7f, 0f, 0.03f, 1.2f);
            yield return FilmFilter.IrisOpen(0.6f);
            if (!controlsShown && Announcer.I)
            {
                controlsShown = true;
                Announcer.I.ShowCard(Controls, 4.5f, new Vector3(0f, -2.4f, 0f));
            }
            if (Announcer.I) Announcer.I.StartCoroutine(Announcer.I.ReadyWallop(boss.displayName));
            yield return StartCoroutine(boss.Fight());
            gs.Add(i < reward.Length ? reward[i] : 50);
            yield return new WaitForSeconds(0.5f);
        }
        if (Announcer.I) yield return Announcer.I.StartCoroutine(Announcer.I.Banner("VICTORY!", Announcer.Yellow, 1.4f, 3));
        AudioManager.Sfx("victory", 1f, 0f);
        gs.Win();
    }
}
