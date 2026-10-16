using System;

public interface IMiniGame
{
    string Id { get; }
    void Begin(Loadout loadout);
    event Action<RunResult> Finished;
}
