using System;
using UnityEngine;

public static class Economy
{
    public static event Action<RunResult> Granted;

    public static int CoinsFor(RunResult r)
    {
        if (r == null) return 0;

        int coins;
        if (r.gameId == "flappy")
        {
            coins = r.score / 2;
        }
        else if (r.gameId == "bossrush")
        {
            coins = (r.won ? 20 : 0) + r.score * 3; // score = boss phases cleared (0..9)
        }
        else
        {
            coins = r.score / 5;
        }

        if (r.won && r.gameId != "flappy")
        {
            coins += 10;
        }

        return Mathf.Max(0, coins);
    }

    public static int XpFor(RunResult r)
    {
        if (r == null) return 0;
        int baseXp = r.gameId == "bossrush" ? r.score * 5 : r.score / 3;
        return Mathf.Max(1, baseXp) + (r.won ? 25 : 0);
    }

    public static int PilotLevel(int xp)
    {
        if (xp < 0) xp = 0;
        return 1 + Mathf.FloorToInt(Mathf.Sqrt(xp / 50f));
    }

    public static RunResult Grant(RunResult r)
    {
        if (r == null) return null;

        r.coinsEarned = CoinsFor(r);
        int xp = XpFor(r);

        SaveSystem.Data.coins += r.coinsEarned;
        SaveSystem.Data.pilotXp += xp;

        if (r.gameId == "flappy" && r.score > SaveSystem.Data.bestFlappy)
        {
            SaveSystem.Data.bestFlappy = r.score;
        }

        SaveSystem.Save();

        Granted?.Invoke(r);
        return r;
    }
}
