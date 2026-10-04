using UnityEngine;

/// <summary>Plays a sliced sprite sheet on a SpriteRenderer at a fixed fps (no Animator controller needed).</summary>
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 8f;
    public bool loop = true;
    public float phase;   // start offset in frames, so a flock does not flap in lockstep
    SpriteRenderer sr;
    float t;

    void Awake() { sr = GetComponent<SpriteRenderer>(); }
    void OnEnable() { t = phase; Apply(); }

    public void Play(Sprite[] newFrames, float newFps)
    {
        if (frames == newFrames) return;
        frames = newFrames; fps = newFps; t = 0f; Apply();
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        t += Time.deltaTime * fps;
        Apply();
    }

    void Apply()
    {
        if (frames == null || frames.Length == 0 || sr == null) return;
        int i = loop ? (int)t % frames.Length : Mathf.Min((int)t, frames.Length - 1);
        sr.sprite = frames[i];
    }
}
