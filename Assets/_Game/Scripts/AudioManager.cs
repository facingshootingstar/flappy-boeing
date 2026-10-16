using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Pooled SFX voices with pitch variance and rate limiting, plus a two-source music crossfade. M toggles mute.</summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager I;

    public AudioClip[] clips;
    [Range(0, 1)] public float sfxVolume = 0.7f;
    [Range(0, 1)] public float musicVolume = 0.42f;
    public int voices = 12;

    readonly Dictionary<string, AudioClip> map = new Dictionary<string, AudioClip>();
    readonly Dictionary<string, float> last = new Dictionary<string, float>();
    AudioSource[] pool;
    int next;
    AudioSource musicA, musicB;
    string currentMusic;
    Coroutine fade;
    static bool muted;

    void Awake()
    {
        I = this;
        muted = !SaveSystem.Data.musicOn;
        foreach (var c in clips) if (c) map[c.name] = c;
        pool = new AudioSource[voices];
        for (int i = 0; i < voices; i++) pool[i] = MakeSource(false);
        musicA = MakeSource(true); musicB = MakeSource(true);
        AudioListener.volume = muted ? 0f : 1f;
    }

    void OnDestroy() { if (I == this) I = null; }

    AudioSource MakeSource(bool loop)
    {
        var s = gameObject.AddComponent<AudioSource>();
        s.playOnAwake = false; s.loop = loop; s.spatialBlend = 0f;
        return s;
    }

    void Update()
    {
        if (CombatInput.MutePressed())
        {
            muted = !muted;
            SaveSystem.Data.musicOn = !muted; SaveSystem.Save();
            AudioListener.volume = muted ? 0f : 1f;
        }
    }

    /// <summary>Play a one-shot. minGap stops the same clip stacking up within that many (unscaled) seconds.</summary>
    public static void Sfx(string name, float volume = 1f, float pitchVar = 0.05f, float minGap = 0.03f, float pitch = 1f)
    {
        if (I == null || !I.map.TryGetValue(name, out var clip)) return;
        float now = Time.unscaledTime;
        if (I.last.TryGetValue(name, out var t) && now - t < minGap) return;
        I.last[name] = now;
        var s = I.pool[I.next];
        I.next = (I.next + 1) % I.pool.Length;
        s.clip = clip;
        s.volume = volume * I.sfxVolume;
        s.pitch = pitch * (1f + Random.Range(-pitchVar, pitchVar));
        s.Play();
    }

    public static void Music(string name, float fadeTime = 1f)
    {
        if (I == null || I.currentMusic == name) return;
        AudioClip clip = null;
        if (name != null) I.map.TryGetValue(name, out clip);
        I.currentMusic = name;
        if (I.fade != null) I.StopCoroutine(I.fade);
        I.fade = I.StartCoroutine(I.Crossfade(clip, fadeTime));
    }

    public static void StopMusic(float fadeTime = 0.6f) => Music(null, fadeTime);

    /// <summary>Duck the current music (death screen, knockout).</summary>
    public static void Duck(float level, float time = 0.3f)
    {
        if (I == null) return;
        I.StartCoroutine(I.DuckTo(level * I.musicVolume, time));
    }

    IEnumerator DuckTo(float target, float time)
    {
        float from = musicA.volume, t = 0f;
        while (t < time) { t += Time.unscaledDeltaTime; musicA.volume = Mathf.Lerp(from, target, t / time); yield return null; }
        musicA.volume = target;
    }

    IEnumerator Crossfade(AudioClip clip, float time)
    {
        // musicA is always the live track
        var tmp = musicA; musicA = musicB; musicB = tmp;
        if (clip != null) { musicA.clip = clip; musicA.volume = 0f; musicA.Play(); }
        float fromB = musicB.volume, t = 0f;
        while (t < time)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / time);
            if (clip != null) musicA.volume = k * musicVolume;
            musicB.volume = fromB * (1f - k);
            yield return null;
        }
        musicB.Stop();
        if (clip != null) musicA.volume = musicVolume;
        fade = null;
    }
}
