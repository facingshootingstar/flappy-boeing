using System.Collections;
using UnityEngine;

/// <summary>Camera image effect (old film look) plus the iris wipe and full-screen flash. All timing is unscaled.</summary>
[RequireComponent(typeof(Camera))]
public class FilmFilter : MonoBehaviour
{
    public static FilmFilter I;

    public Shader shader;
    [Range(0, 0.3f)] public float grain = 0.06f;
    [Range(0, 1)] public float scratch = 0.35f;
    [Range(0, 0.01f)] public float dust = 0.0012f;
    [Range(0, 1)] public float vignette = 0.42f;
    [Range(0, 1)] public float sepia = 0.16f;
    [Range(0, 0.3f)] public float flicker = 0.05f;
    public Color ink = new Color32(42, 33, 28, 255);

    public const float IrisOpenRadius = 1.15f;
    float iris = IrisOpenRadius;
    Vector2 irisCenter = new Vector2(0.5f, 0.5f);
    float flash;
    Color flashColor = Color.white;
    Coroutine irisRoutine, flashRoutine;
    Material mat;

    public static bool IrisClosed => I != null && I.iris <= 0.0001f;

    void Awake()
    {
        I = this;
        if (shader != null && shader.isSupported) mat = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
    }

    void OnDestroy() { if (mat) Destroy(mat); if (I == this) I = null; }

    void OnRenderImage(RenderTexture src, RenderTexture dst)
    {
        if (mat == null) { Graphics.Blit(src, dst); return; }
        mat.SetFloat("_T", Time.unscaledTime);
        mat.SetFloat("_Grain", grain);
        mat.SetFloat("_Scratch", scratch);
        mat.SetFloat("_Dust", dust);
        mat.SetFloat("_Vignette", vignette);
        mat.SetFloat("_Sepia", sepia);
        mat.SetFloat("_Flicker", flicker);
        mat.SetFloat("_Iris", iris);
        mat.SetVector("_IrisCenter", irisCenter);
        mat.SetFloat("_Flash", flash);
        mat.SetColor("_FlashColor", flashColor);
        mat.SetColor("_Ink", ink);
        Graphics.Blit(src, dst, mat);
    }

    // ---------------------------------------------------------------- iris
    /// <summary>Close the iris on a world point (or the screen centre). Yield on the result to wait.</summary>
    public static Coroutine IrisClose(float dur = 0.55f, Vector3? world = null)
    {
        if (I == null) return null;
        if (world.HasValue && Camera.main) I.irisCenter = Camera.main.WorldToViewportPoint(world.Value);
        return I.IrisTo(0f, dur);
    }

    public static Coroutine IrisOpen(float dur = 0.55f)
    {
        if (I == null) return null;
        return I.IrisTo(IrisOpenRadius, dur);
    }

    /// <summary>Snap shut instantly (scene start for a checkpoint retry).</summary>
    public static void IrisSet(float radius) { if (I != null) { I.iris = radius; I.irisCenter = new Vector2(0.5f, 0.5f); } }

    Coroutine IrisTo(float target, float dur)
    {
        if (irisRoutine != null) StopCoroutine(irisRoutine);
        irisRoutine = StartCoroutine(IrisAnim(target, dur));
        return irisRoutine;
    }

    IEnumerator IrisAnim(float target, float dur)
    {
        float from = iris, t = 0f;
        AudioManager.Sfx("iris", 0.6f);
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            k = target < from ? k * k : 1f - (1f - k) * (1f - k);
            iris = Mathf.Lerp(from, target, k);
            yield return null;
        }
        iris = target;
        if (target >= IrisOpenRadius) irisCenter = new Vector2(0.5f, 0.5f);
        irisRoutine = null;
    }

    // ---------------------------------------------------------------- flash
    public static void Flash(float amount = 0.85f, float dur = 0.18f, Color? color = null)
    {
        if (I == null) return;
        I.flashColor = color ?? new Color(1f, 0.97f, 0.9f);
        if (I.flashRoutine != null) I.StopCoroutine(I.flashRoutine);
        I.flashRoutine = I.StartCoroutine(I.FlashAnim(amount, dur));
    }

    IEnumerator FlashAnim(float amount, float dur)
    {
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            flash = amount * (1f - t / dur);
            yield return null;
        }
        flash = 0f;
        flashRoutine = null;
    }
}
