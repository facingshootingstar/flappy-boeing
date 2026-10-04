using UnityEngine;

/// <summary>Natural flock motion: each bird bobs on its own sine and drifts slightly faster/slower than the flock.</summary>
public class BirdFlight : MonoBehaviour
{
    public float amp = 0.2f, freq = 1.6f, phase, drift;
    float baseY;

    void Start() { baseY = transform.localPosition.y; }

    void Update()
    {
        var p = transform.localPosition;
        p.y = baseY + Mathf.Sin(Time.time * freq + phase) * amp;
        p.x += drift * Time.deltaTime;
        transform.localPosition = p;
    }
}
