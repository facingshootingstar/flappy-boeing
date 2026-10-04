using System.Collections;
using UnityEngine;

/// <summary>Storm weather: every few seconds a distant bolt cracks inside the clouds, the whole sky flickers white
/// and thunder rolls in a beat later.</summary>
public class SkyFlash : MonoBehaviour
{
    public SpriteRenderer flash;                     // full-screen white quad just above the sky
    public Sprite bolt;
    public int boltOrder = -9;
    public Vector2 interval = new Vector2(3.5f, 7f);
    float timer;
    GameObject liveBolt;

    void OnEnable()
    {
        timer = Random.Range(1f, 2.5f);
        SetFlash(0f);
    }

    void OnDisable()
    {
        if (liveBolt) Destroy(liveBolt);
        SetFlash(0f);
    }

    void SetFlash(float a)
    {
        if (!flash) return;
        var c = flash.color; c.a = a; flash.color = c;
    }

    void Update()
    {
        var gs = GameState.I;
        if (gs != null && !gs.Alive) return;
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = Random.Range(interval.x, interval.y);
        StartCoroutine(Strike());
    }

    IEnumerator Strike()
    {
        if (bolt)
        {
            liveBolt = new GameObject("FarBolt");
            liveBolt.transform.SetParent(transform, false);
            liveBolt.transform.localPosition = new Vector3(Random.Range(-6.5f, 6.5f), Random.Range(1.2f, 2.2f), 0f);
            float s = Random.Range(0.6f, 1f);
            liveBolt.transform.localScale = new Vector3(Random.value < 0.5f ? -s : s, s, 1f);
            var r = liveBolt.AddComponent<SpriteRenderer>();
            r.sprite = bolt; r.sortingOrder = boltOrder; r.color = new Color(1f, 1f, 1f, 0.85f);
        }
        float[] seq = { 0.45f, 0.08f, 0.32f };
        foreach (var a in seq) { SetFlash(a); yield return new WaitForSeconds(0.05f); }
        if (liveBolt) Destroy(liveBolt);
        for (float t = 0f; t < 0.3f; t += Time.deltaTime) { SetFlash(0.32f * (1f - t / 0.3f)); yield return null; }
        SetFlash(0f);
        yield return new WaitForSeconds(Random.Range(0.15f, 0.5f));
        AudioManager.Sfx("thunder", Random.Range(0.25f, 0.4f), 0.1f, 0.5f, Random.Range(0.7f, 0.85f));
    }
}
