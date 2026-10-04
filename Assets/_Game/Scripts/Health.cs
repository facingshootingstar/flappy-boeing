using System;
using UnityEngine;

/// <summary>Hit points for bosses and minions. Bosses split into phases at the given fractions (e.g. 0.66, 0.33).</summary>
public class Health : MonoBehaviour
{
    /// <summary>The boss currently on screen (boss bar, bot, death-screen progress).</summary>
    public static Health Boss;

    public float max = 100f;
    public float hp = 100f;
    public bool Vulnerable = true;
    public bool isBoss;
    public float[] phaseThresholds = new float[0];

    public int Phase { get; private set; }
    public bool Dead => hp <= 0f;
    public float Fraction => max > 0f ? Mathf.Clamp01(hp / max) : 0f;
    /// <summary>0..1 progress through the current phase (death screen).</summary>
    public float PhaseFraction
    {
        get
        {
            float top = Phase == 0 ? 1f : phaseThresholds[Phase - 1];
            float bottom = Phase < phaseThresholds.Length ? phaseThresholds[Phase] : 0f;
            return Mathf.Clamp01((top - Fraction) / Mathf.Max(0.0001f, top - bottom));
        }
    }

    public event Action<int> PhaseChanged;
    public event Action Died;
    public event Action<float> Damaged;

    SpriteRenderer[] renderers;
    Color[] baseColors;
    float flashT;

    void Awake() { CacheRenderers(); }
    void OnDestroy() { if (Boss == this) Boss = null; }

    public void CacheRenderers()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
        baseColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++) baseColors[i] = renderers[i].color;
    }

    public void Begin(float maxHp)
    {
        max = hp = maxHp;
        Phase = 0;
        Vulnerable = true;
        if (isBoss) Boss = this;
    }

    public void Hit(float n)
    {
        if (!Vulnerable || Dead) return;
        hp -= n;
        Damaged?.Invoke(n);
        flashT = 0.06f;
        // phase change: clamp so a single big hit cannot skip a phase
        if (Phase < phaseThresholds.Length && Fraction <= phaseThresholds[Phase])
        {
            hp = Mathf.Max(hp, 0.01f);
            Phase++;
            Vulnerable = false;   // the boss re-opens after its transition
            PhaseChanged?.Invoke(Phase);
            return;
        }
        if (hp <= 0f) { hp = 0f; Died?.Invoke(); }
    }

    void LateUpdate()
    {
        if (renderers == null) return;
        if (flashT > 0f)
        {
            flashT -= Time.unscaledDeltaTime;
            bool on = flashT > 0f;
            for (int i = 0; i < renderers.Length; i++)
                if (renderers[i]) renderers[i].color = on ? Color.Lerp(baseColors[i], new Color(1f, 0.55f, 0.45f, baseColors[i].a), 0.7f) : baseColors[i];
        }
    }

    /// <summary>Change a renderer's resting colour (keeps the hit flash working).</summary>
    public void SetBaseColor(SpriteRenderer r, Color c)
    {
        if (renderers == null) return;
        for (int i = 0; i < renderers.Length; i++) if (renderers[i] == r) { baseColors[i] = c; r.color = c; }
    }
}
