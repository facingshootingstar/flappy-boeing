using System;
using System.Collections.Generic;

[Serializable]
public class RunResult
{
    public string gameId;
    public int score;
    public bool won;
    public float duration;
    public int coinsEarned;
    public List<string> materials = new List<string>();

    public static RunResult Create(string gameId, int score, bool won, float duration)
    {
        return new RunResult
        {
            gameId = gameId,
            score = score,
            won = won,
            duration = duration,
            coinsEarned = 0,
            materials = new List<string>()
        };
    }
}
