using System;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Run state. Score 0-29 is the Flappy phase; at 30 the BossDirector takes over (WASD + shoot gauntlet of 3 bosses x 3 phases).
/// A checkpoint survives scene reloads so the death screen can retry the current boss.
/// <see cref="Mode"/> picks Classic (Flappy then bosses), endless Flappy, or Boss Rush only.
/// </summary>
public enum RunMode { Classic, Flappy, BossRush }

public class GameState : MonoBehaviour
{
    public static GameState I { get; private set; }
    public static event Action<int> ScoreChanged;
    public static event Action Died;
    public static event Action Won;
    public static event Action CombatStarted;

    public const int CombatScore = 30;
    public const int BossCount = 3, PhasesPerBoss = 3;

    // ---- survives scene reloads (retry from the death screen)
    public static int CheckpointBoss = -1;
    static int checkpointScore;
    public static bool God;   // debug only (AutoPilot)
    public static RunMode Mode = RunMode.Classic;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { CheckpointBoss = -1; checkpointScore = 0; God = false; Mode = RunMode.Classic; ScoreChanged = null; Died = null; Won = null; CombatStarted = null; }

    public int Score { get; private set; }
    public bool Alive { get; private set; } = true;
    public bool Started { get; private set; }
    public int Best { get; private set; }
    /// <summary>False for the Flappy phase, true for the boss gauntlet.</summary>
    public bool Combat { get; private set; }
    public bool Victory { get; private set; }
    public bool FromCheckpoint { get; private set; }

    // ---- gauntlet progress + stats
    public int BossIndex { get; private set; }
    public int BossPhase { get; private set; }
    public int Parries, Supers, ExShots, HitsTaken;
    public float FightTime;

    void Awake()
    {
        I = this;
        Best = SaveSystem.Data.bestFlappy;
        if (CheckpointBoss >= 0) { FromCheckpoint = true; Score = checkpointScore; BossIndex = CheckpointBoss; }
    }

    void Start()
    {
        if (!FromCheckpoint && Mode != RunMode.BossRush) AudioManager.Music("music_flappy", 0.5f);
    }

    void OnDestroy() { if (I == this) I = null; }

    void Update()
    {
        if (Combat && Alive) FightTime += Time.deltaTime;
        if (!Started && !Combat)
        {
            int m = CombatInput.ModeKey();
            if (m > 0) { Mode = (RunMode)(m - 1); Restart(); }
        }
    }

    public void Begin() { Started = true; }

    public void EnterCombat()
    {
        if (Combat) return;
        Started = true;
        Combat = true;
        CombatStarted?.Invoke();
    }

    /// <summary>Called by the BossDirector; phase 0 marks the retry checkpoint.</summary>
    public void SetBoss(int index, int phase)
    {
        if (phase == 0) { CheckpointBoss = index; checkpointScore = Score; }
        BossIndex = index; BossPhase = phase;
    }

    /// <summary>0..1 progress through the whole gauntlet (death screen bar).</summary>
    public float Progress(float phaseFraction)
    {
        if (!Combat) return 0f;
        return (BossIndex * PhasesPerBoss + BossPhase + Mathf.Clamp01(phaseFraction)) / (BossCount * PhasesPerBoss);
    }

    public void Win()
    {
        if (!Alive) return;
        Victory = true;
        Alive = false;
        CheckpointBoss = -1;
        RecordBest();
        GrantRun(true);
        Won?.Invoke();
    }

    /// <summary>Last run's payout (coins/xp already saved). Null until a run ends.</summary>
    public RunResult LastResult { get; private set; }

    void GrantRun(bool won)
    {
        float t = Time.timeSinceLevelLoad;
        if (!Combat)
        {
            LastResult = Economy.Grant(RunResult.Create("flappy", Score, won, t));
        }
        else
        {
            // boss runs score by phases cleared (pipes don't count in combat); Classic also pays its Flappy leg
            int phases = won ? BossCount * PhasesPerBoss : BossIndex * PhasesPerBoss + BossPhase;
            int flappyCoins = Mode == RunMode.Classic ? Economy.Grant(RunResult.Create("flappy", Score, false, t)).coinsEarned : 0;
            LastResult = Economy.Grant(RunResult.Create("bossrush", phases, won, t));
            LastResult.coinsEarned += flappyCoins;
        }
        Debug.Log($"[Economy] {LastResult.gameId} score={LastResult.score} won={won} +{LastResult.coinsEarned} coins (total {SaveSystem.Data.coins})");
    }

    void RecordBest()
    {
        if (Mode == RunMode.BossRush || Score <= Best) return;
        Best = Score;
        SaveSystem.Data.bestFlappy = Best;
        SaveSystem.Save();
    }

    public void Add(int n = 1)
    {
        if (!Alive) return;
        Score += n;
        if (!Combat) AudioManager.Sfx("score", 0.55f, 0f);
        ScoreChanged?.Invoke(Score);
    }

    public void Kill()
    {
        if (!Alive || God) return;
        Alive = false;
        RecordBest();
        GrantRun(false);
        Died?.Invoke();
    }

    /// <summary>Reload; in combat the checkpoint restarts the current boss.</summary>
    public void Retry()
    {
        if (!Combat) CheckpointBoss = -1;
        Reload();
    }

    /// <summary>Reload from the very start (Flappy phase).</summary>
    public void Restart()
    {
        CheckpointBoss = -1;
        Reload();
    }

    static void Reload()
    {
        CameraFx.ClearTime();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
