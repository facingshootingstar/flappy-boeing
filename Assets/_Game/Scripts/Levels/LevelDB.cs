using UnityEngine;

public enum LevelGame { Flappy, Boss, Missile, Stack, Landing, Supply, Bolt, Refuel }

/// <summary>Per-level spice so a repeated sport is not the same node twice. Quota is a bonus, never a fail.</summary>
public enum LevelTwist { None, Wind, Night, Quota }

/// <summary>
/// One node on the level map. <see cref="stars"/> are score thresholds for 1/2/3 stars; mini games pass by reaching their goal,
/// then earn stars 2 and 3 from score. Flappy passes at stars[0] pipes and ends itself at stars[2].
/// </summary>
public class LevelDef
{
    public int id;            // 1-based, map order
    public int world;         // 0..4
    public LevelGame game;
    public int goal;          // missile: seconds, stack: crates, landing: landings, boss: boss index (-1 = whole gauntlet)
    public int[] stars;
    public float hard;        // 0..1 difficulty knob for the mini games
    public LevelTwist twist;

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
                case LevelGame.Landing: return "PERFECT LANDING";
                case LevelGame.Supply: return "SUPPLY DROP";
                case LevelGame.Bolt: return "BOLT LANE";
                default: return "SKY REFUEL";
            }
        }
    }

    string TwistLine => twist == LevelTwist.Wind ? " + WIND" : twist == LevelTwist.Night ? " AT NIGHT" : twist == LevelTwist.Quota ? " + BONUS" : "";

    public string GoalText
    {
        get
        {
            string core;
            switch (game)
            {
                case LevelGame.Flappy: core = "PASS " + stars[0] + " PIPES"; break;
                case LevelGame.Boss:
                    if (id == 25) core = "DEFEAT THE BLACKOUT";
                    else if (id == 20) core = "BEAT THE IRON FLOCK";
                    else core = goal < 0 ? "BEAT ALL 3 BOSSES" : "DEFEAT " + LevelDB.BossNames[goal];
                    break;
                case LevelGame.Missile: core = "SURVIVE " + goal + " SECONDS"; break;
                case LevelGame.Stack: core = "STACK " + goal + " CRATES"; break;
                case LevelGame.Landing: core = "LAND " + goal + " TIMES"; break;
                case LevelGame.Supply: core = "CATCH " + goal + " CRATES"; break;
                case LevelGame.Bolt: core = "DODGE " + goal + " BOLTS"; break;
                default: core = "FILL FOR " + goal + " SECONDS"; break;
            }
            return core + TwistLine;
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
                case LevelGame.Bolt: return "CLOSE";
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
    public static readonly string[] Worlds = { "DAWN VALLEY", "SUNNY SEA", "STORM PEAKS", "SUNDOWN", "STARLIGHT" };
    /// <summary>BiomeDirector index per world (0 dawn, 1 storm, 2 sea, 3 dusk). Starlight reuses dusk.</summary>
    public static readonly int[] WorldBiome = { 0, 2, 1, 3, 3 };
    public static readonly string[] BossNames = { "TEMPEST EAGLE", "IRONWAKE", "SUNDOWN CITADEL" };
    public static readonly Color[] WorldColor =
    {
        new Color32(246, 160, 92, 255), new Color32(84, 178, 214, 255), new Color32(140, 120, 190, 255),
        new Color32(228, 98, 112, 255), new Color32(186, 156, 255, 255),
    };

    /// <summary>Stars required to open the first level of each world. World 2 asks for 6 so two 3-star clears can pay the gate.</summary>
    public static readonly int[] GateStars = { 0, 6, 15, 26, 40 };

    static int next = 1;   // must stay above All: static initializers run in textual order
    public static readonly LevelDef[] All =
    {
        // ---- world 1: dawn valley. Types of levels 1, 2, 4, 5 stay as play-tested.
        L(0, LevelGame.Flappy, 0, 0f, 5, 8, 12),
        L(0, LevelGame.Stack, 8, 0f, 0, 2, 4),
        L(0, LevelGame.Flappy, 0, 0f, 10, 15, 20, LevelTwist.Night),
        L(0, LevelGame.Missile, 20, 0f, 0, 3, 6),
        L(0, LevelGame.Boss, 0, 0f, 0, 6, 9),
        // ---- world 2: sunny sea. Landing bars from a measured 167-point glide.
        L(1, LevelGame.Landing, 3, 0f, 0, 110, 165),
        L(1, LevelGame.Supply, 6, 0.15f, 0, 30, 50),
        L(1, LevelGame.Stack, 12, 0.3f, 0, 3, 6, LevelTwist.Wind),
        L(1, LevelGame.Flappy, 0, 0f, 15, 22, 30, LevelTwist.Night),
        L(1, LevelGame.Boss, 1, 0f, 0, 6, 9),
        // ---- world 3: storm peaks
        L(2, LevelGame.Landing, 3, 0.5f, 0, 100, 145, LevelTwist.Wind),
        L(2, LevelGame.Bolt, 10, 0.25f, 0, 5, 9),
        L(2, LevelGame.Stack, 15, 0.55f, 0, 4, 8, LevelTwist.Quota),
        L(2, LevelGame.Missile, 28, 0.55f, 0, 5, 9, LevelTwist.Night),
        L(2, LevelGame.Boss, 2, 0f, 0, 6, 9),
        // ---- world 4: sundown
        L(3, LevelGame.Refuel, 8, 0.35f, 0, 70, 96),
        L(3, LevelGame.Landing, 4, 0.7f, 0, 120, 170, LevelTwist.Night),
        L(3, LevelGame.Stack, 18, 0.75f, 0, 5, 9, LevelTwist.Wind),
        L(3, LevelGame.Bolt, 14, 0.6f, 0, 6, 12, LevelTwist.Quota),
        L(3, LevelGame.Boss, -1, 0f, 0, 6, 9),
        // ---- world 5: starlight
        L(4, LevelGame.Supply, 8, 0.45f, 0, 40, 70, LevelTwist.Night),
        L(4, LevelGame.Bolt, 16, 0.7f, 0, 7, 14, LevelTwist.Night),
        L(4, LevelGame.Refuel, 8, 0.65f, 0, 64, 92, LevelTwist.Wind),
        L(4, LevelGame.Flappy, 0, 0f, 22, 30, 40, LevelTwist.Night),
        L(4, LevelGame.Boss, 2, 0f, 0, 6, 9, LevelTwist.Night),
    };

    static LevelDef L(int world, LevelGame game, int goal, float hard, int s1, int s2, int s3, LevelTwist twist = LevelTwist.None) =>
        new LevelDef { id = next++, world = world, game = game, goal = goal, hard = hard, stars = new[] { s1, s2, s3 }, twist = twist };

    public static int Count => All.Length;
    public static LevelDef Get(int id) => id >= 1 && id <= All.Length ? All[id - 1] : null;
    public static int MaxStars => All.Length * 3;

    /// <summary>Stars still needed to open a world gate, or 0 when the gate is open or this level is not a gate.</summary>
    public static int GateBlock(int id)
    {
        if (id <= 1 || (id - 1) % PerWorld != 0) return 0;
        int w = (id - 1) / PerWorld;
        if (w >= GateStars.Length) return 0;
        int need = GateStars[w];
        return LevelFlow.TotalStars() < need ? need : 0;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
    static void SelfCheck()
    {
        var flap = All[0];
        bool ok = flap.StarsFor(false, 4) == 0 && flap.StarsFor(false, 5) == 1 && flap.StarsFor(true, 12) == 3
            && All.Length == 25 && Get(6).stars[2] == 165 && Get(7).game == LevelGame.Supply
            && Get(12).game == LevelGame.Bolt && Get(16).game == LevelGame.Refuel
            && Get(21).world == 4 && GateStars[1] == 6 && WorldBiome.Length == Worlds.Length;
        if (!ok) Debug.LogError("[LevelDB] self-check failed");
    }
}
