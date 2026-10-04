using UnityEngine;

/// <summary>Each biome is a root GameObject holding its parallax layers, ambient spawners and weather.
/// Index 0 dawn (Flappy), 1 storm (Eagle), 2 sea (Ironwake), 3 dusk (Citadel). The boss director swaps them while the iris is shut.</summary>
public class BiomeDirector : MonoBehaviour
{
    public GameObject[] roots;
    public int Current { get; private set; } = -1;

    // Awake, not Start: on a checkpoint retry the boss director picks the biome in its own Start
    void Awake() { Set(0); }

    public void Set(int b)
    {
        if (roots == null || roots.Length == 0) return;
        b = Mathf.Clamp(b, 0, roots.Length - 1);
        if (b == Current) return;
        Current = b;
        for (int i = 0; i < roots.Length; i++) if (roots[i]) roots[i].SetActive(i == b);
        Debug.Log("[Biome] " + (roots[b] ? roots[b].name : b.ToString()));
    }
}
