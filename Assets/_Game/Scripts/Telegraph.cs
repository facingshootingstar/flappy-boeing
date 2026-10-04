using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Marks a warning stripe or target ring as "about to be dangerous" so the debug bot can steer clear of it.
/// The zone is a capsule along the object's local x axis: halfLength either side of the centre, halfWidth across.
/// </summary>
public class Telegraph : MonoBehaviour
{
    public static readonly List<Telegraph> All = new List<Telegraph>();
    public float halfLength, halfWidth;

    public static void Mark(GameObject go, float halfLength, float halfWidth)
    {
        var t = go.AddComponent<Telegraph>();
        t.halfLength = halfLength; t.halfWidth = halfWidth;
    }

    void OnEnable() { All.Add(this); }
    void OnDisable() { All.Remove(this); }

    /// <summary>Distance from q to the zone (negative inside).</summary>
    public float Gap(Vector2 q)
    {
        Vector2 c = transform.position, axis = transform.right;
        float along = Mathf.Clamp(Vector2.Dot(q - c, axis), -halfLength, halfLength);
        return Vector2.Distance(q, c + axis * along) - halfWidth;
    }
}
