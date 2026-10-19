using UnityEngine;

public enum LevelGame { Flappy, Boss, Missile, Stack, Landing }

/// <summary>
/// One node on the level map. <see cref="stars"/> are score thresholds for 1/2/3 stars; mini games pass by reaching their goal,
/// then earn stars 2 and 3 from score. Flappy passes at stars[0] pipes and ends itself at stars[2].
/// </summary>
public class LevelDef
{
    public int id;            // 1-based, map order
    public int world;         // 0..3
    public LevelGame game;
    public int goal;          // missile: seconds, stack: crates, landing: landings, boss: boss index (-1 = whole gauntlet)
    public int[] stars;
    public float hard;        // 0..1 difficulty knob for the mini games

    public bool IsBoss => game == LevelGame.Boss;

    public string GameName
    {
        get
        {
            switch (game)
            {
                case LevelGame.Flappy: return "FLAPPY FLIGHT";
                case LevelGame.Boss: return goal < 0 ? "BOSS RUSH" : "BOSS FIGHT";
                case LevelGame.Missile: return "MISSILE DODGE";
                case LevelGame.Stack: return "CARGO STACK";
                default: return "PERFECT LANDING";
            }
        }
    }

    public string GoalText
    {
        get
        {
            switch (game)
            {
                case LevelGame.Flappy: return "PASS " + stars[0] + " PIPES";
                case LevelGame.Boss: return goal < 0 ? "BEAT ALL 3 BOSSES" : "DEFEAT " + LevelDB.BossNames[goal];
                case LevelGame.Missile: return "SURVIVE " + goal + " SECONDS";
                case LevelGame.Stack: return "STACK " + goal + " CRATES";
                default: return "LAND " + goal + " TIMES";
            }
        }
    }

    /// <summary>What the score counts, shown next to star thresholds and on the result screen.</summary>
    public string ScoreName
    {
        get
        {
            switch (game)
            {
                case LevelGame.Flappy: return "PIPES";
                case LevelGame.Boss: return "STYLE";
                case LevelGame.Missile: return "POINTS";
                case LevelGame.Stack: return "PERFECT";
                default: return "POINTS";
            }
        }
    }

    public string StarText(int s)
    {
        if (s == 0) return game == LevelGame.Flappy ? stars[0] + " PIPES" : "CLEAR";
        if (game == LevelGame.Boss) return s == 1 ? "4 HITS OR LESS" : "NO MORE THAN 1 HIT";
        return stars[s] + " " + ScoreName;
    }

    /// <summary>0..3 stars for a finished run.</summary>
    public int StarsFor(bool won, int score)
    {
        if (game == LevelGame.Flappy) won = score >= stars[0];
        if (!won) return 0;
        int n = 1;
        if (score >= stars[1]) n++;
        if (score >= stars[2]) n++;
        return n;
    }
}

public static class LevelDB
{
    public const int PerWorld = 5;
    public static readonly string[] Worlds = { "DAWN VALLEY", "SUNNY SEA", "STORM PEAKS", "SUNDOWN" };
    /// <summary>BiomeDirector index per world (0 dawn, 1 storm, 2 sea, 3 dusk).</summary>
    public static readonly int[] WorldBiome = { 0, 2, 1, 3 };
    public static readonly string[] BossNames = { "TEMPEST EAGLE", "IRONWAKE", "SUNDOWN CITADEL" };
    public static readonly Color[] WorldColor =
    {
        new Color32(246, 160, 92, 255), new Color32(84, 178, 214, 255), new Color32(140, 120, 190, 255), new Color32(228, 98, 112, 255),
    };

    static int next = 1;   // must stay above All: static initializers run in textual order
    public static readonly LevelDef[] All =
    {
        // ---- world 1: dawn valley
        L(0, LevelGame.Flappy, 0, 0f, 5, 8, 12),
        L(0, LevelGame.Stack, 8, 0f, 0, 2, 4),
        L(0, LevelGame.Flappy, 0, 0f, 10, 15, 20),
        L(0, LevelGame.Missile, 20, 0f, 0, 3, 6),
        L(0, LevelGame.Boss, 0, 0f, 0, 6, 9),
        // ---- world 2: sunny sea
        L(1, LevelGame.Landing, 3, 0f, 0, 150, 220),
        L(1, LevelGame.Missile, 25, 0.3f, 0, 4, 8),
        L(1, LevelGame.Stack, 12, 0.3f, 0, 3, 6),
        L(1, LevelGame.Flappy, 0, 0f, 15, 22, 30),
        L(1, LevelGame.Boss, 1, 0f, 0, 6, 9),
        // ---- world 3: storm peaks
        L(2, LevelGame.Landing, 3, 0.5f, 0, 160, 230),
        L(2, LevelGame.Flappy, 0, 0f, 20, 28, 36),
        L(2, LevelGame.Stack, 15, 0.6f, 0, 4, 8),
        L(2, LevelGame.Missile, 30, 0.6f, 0, 6, 10),
        L(2, LevelGame.Boss, 2, 0f, 0, 6, 9),
        // ---- world 4: sundown
        L(3, LevelGame.Missile, 40, 0.9f, 0, 8, 14),
        L(3, LevelGame.Landing, 4, 0.8f, 0, 240, 320),
        L(3, LevelGame.Stack, 20, 0.9f, 0, 6, 10),
        L(3, LevelGame.Flappy, 0, 0f, 30, 40, 50),
        L(3, LevelGame.Boss, -1, 0f, 0, 6, 9),
    };

    static LevelDef L(int world, LevelGame game, int goal, float hard, int s1, int s2, int s3) =>
        new LevelDef { id = next++, world = world, game = game, goal = goal, hard = hard, stars = new[] { s1, s2, s3 } };

    public static int Count => All.Length;
    public static LevelDef Get(int id) => id >= 1 && id <= All.Length ? All[id - 1] : null;
    public static int MaxStars => All.Length * 3;
}
