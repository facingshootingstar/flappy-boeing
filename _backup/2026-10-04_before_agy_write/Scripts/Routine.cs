using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Hand-stepped coroutine for boss patterns: nested IEnumerators run inline and the whole stack stops at once
/// (phase changes, knockouts). yield null = next frame, yield a float = wait that many scaled seconds.
/// </summary>
public class Routine
{
    readonly Stack<IEnumerator> stack = new Stack<IEnumerator>();
    float wait;

    public Routine(IEnumerator root) { if (root != null) stack.Push(root); }

    public bool Running => stack.Count > 0;

    /// <summary>Advance by dt; returns false once finished.</summary>
    public bool Step(float dt)
    {
        if (wait > 0f) { wait -= dt; if (wait > 0f) return true; }
        int guard = 0;
        while (stack.Count > 0)
        {
            if (++guard > 256) return true;   // a pattern that never yields must not freeze the game
            var top = stack.Peek();
            if (!top.MoveNext()) { stack.Pop(); continue; }
            var cur = top.Current;
            if (cur is IEnumerator e) { stack.Push(e); continue; }
            if (cur is float f) { wait = f; return true; }
            if (cur is int n) { wait = n; return true; }
            return true;
        }
        return false;
    }

    public void Stop() { stack.Clear(); wait = 0f; }
}
