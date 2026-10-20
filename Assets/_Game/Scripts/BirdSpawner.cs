using System.Collections.Generic;
using UnityEngine;

/// <summary>Birds are the living pipes: a loose flock with one open lane. Passing the lane scores.</summary>
public class BirdSpawner : MonoBehaviour
{
    public Sprite[] cruise;
    public float speed = 3.2f;
    public float interval = 1.9f;
    public float gap = 3.0f;
    public float spawnX = 9f;

    float timer;
    readonly List<Transform> columns = new List<Transform>();

    void Start()
    {
        Spawn(gap);
        if (columns.Count > 0) columns[0].position = new Vector3(4.6f, 0f, 0f);
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs == null || !gs.Alive || !gs.Started) return;

        // difficulty ramps with score, never spikes
        float k = Mathf.Clamp01(gs.Score / 120f);
        float curSpeed = Mathf.Lerp(speed, speed * 1.45f, k);
        float curInterval = Mathf.Lerp(interval, interval * 0.7f, k);
        float curGap = Mathf.Lerp(gap, gap * 0.82f, k);

        timer -= Time.deltaTime;
        if (timer <= 0f) { Spawn(curGap); timer = curInterval; }

        for (int i = columns.Count - 1; i >= 0; i--)
        {
            if (columns[i] == null) { columns.RemoveAt(i); continue; }
            columns[i].Translate(Vector3.left * curSpeed * Time.deltaTime, Space.World);
            if (columns[i].position.x < -spawnX - 1f) { Destroy(columns[i].gameObject); columns.RemoveAt(i); }
        }
    }

    // A flock crossing the screen. Birds are scattered (random spacing, x spread, gaps in the flock) rather than lined up;
    // the only guarantee is an open lane around the gate, wide enough for any bob/drift.
    void Spawn(float curGap)
    {
        var col = new GameObject("Flock").transform;
        col.SetParent(transform, false);
        col.position = new Vector3(spawnX, 0, 0);
        float centerY = Random.Range(-2.1f, 2.1f);
        const float top = 4.6f, bottom = -4.6f;
        float lane = curGap / 2f + 0.55f;   // keeps bobbing birds out of the corridor
        for (float y = centerY + lane; y < top; y += Random.Range(0.75f, 1.35f))
            if (Random.value > 0.18f) MakeBird(col, y);
        for (float y = centerY - lane; y > bottom; y -= Random.Range(0.75f, 1.35f))
            if (Random.value > 0.18f) MakeBird(col, y);

        var gate = new GameObject("Gate");
        gate.transform.SetParent(col, false);
        gate.transform.localPosition = new Vector3(0.1f, centerY, 0);
        var bc = gate.AddComponent<BoxCollider2D>(); bc.isTrigger = true; bc.size = new Vector2(0.3f, curGap);
        gate.AddComponent<ScoreGate>();
        columns.Add(col);
    }

    void MakeBird(Transform parent, float y)
    {
        var go = new GameObject("Bird");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(Random.Range(-1.0f, 1.0f), y, 0);
        float s = Random.Range(1.35f, 1.6f);
        go.transform.localScale = new Vector3(s, s, 1);
        var sr = go.AddComponent<SpriteRenderer>(); sr.sortingOrder = 16;
        var a = go.AddComponent<SpriteAnimator>(); a.frames = cruise; a.fps = Random.Range(5f, 8f); a.phase = Random.Range(0f, 4f);
        var f = go.AddComponent<BirdFlight>();
        f.amp = Random.Range(0.1f, 0.28f); f.freq = Random.Range(1.2f, 2.2f); f.phase = Random.Range(0f, 6.28f); f.drift = Random.Range(-0.2f, 0.2f);
        var c = go.AddComponent<CircleCollider2D>(); c.isTrigger = true; c.radius = 0.34f;
        go.AddComponent<Hazard>();
    }

    public void ClearAll()
    {
        foreach (var c in columns) if (c) Destroy(c.gameObject);
        columns.Clear();
    }
}
