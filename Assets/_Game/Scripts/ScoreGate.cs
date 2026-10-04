using UnityEngine;

/// <summary>Invisible trigger in the gap of a bird column; passing through scores one point.</summary>
public class ScoreGate : MonoBehaviour
{
    bool used;
    void OnTriggerEnter2D(Collider2D other)
    {
        if (used || other.GetComponent<PlayerController>() == null) return;
        used = true;
        GameState.I?.Add(1);
    }
}
