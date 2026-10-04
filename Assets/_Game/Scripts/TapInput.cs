using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>Single verb of the game: one tap (mouse, touch or space), works with either input backend.</summary>
public static class TapInput
{
    /// <summary>Set by AutoPilot (debug bot) so tests can tap without a device.</summary>
    public static int InjectedFrame = -1;

    public static bool Pressed()
    {
        if (InjectedFrame == Time.frameCount) return true;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current;
        var t = Touchscreen.current;
        var k = Keyboard.current;
        return (m != null && m.leftButton.wasPressedThisFrame)
            || (t != null && t.primaryTouch.press.wasPressedThisFrame)
            || (k != null && k.spaceKey.wasPressedThisFrame);
#else
        return Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)
            || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began);
#endif
    }
}
