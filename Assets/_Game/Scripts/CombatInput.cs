using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Combat phase controls: WASD / arrows move, Space / J / left mouse (hold) shoot, K / right mouse EX, Shift / L dash.
/// Also the menu keys (M mute, J retry, R restart). Works with either input backend.
/// </summary>
public static class CombatInput
{
    /// <summary>Debug bot overrides (AutoPilot).</summary>
    public static Vector2? MoveOverride;
    public static bool FireOverride, DashOverride, ExOverride;

    public static Vector2 Move()
    {
        if (MoveOverride.HasValue) return MoveOverride.Value;
        Vector2 v = Vector2.zero;
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        if (k != null)
        {
            if (k.wKey.isPressed || k.upArrowKey.isPressed) v.y += 1;
            if (k.sKey.isPressed || k.downArrowKey.isPressed) v.y -= 1;
            if (k.aKey.isPressed || k.leftArrowKey.isPressed) v.x -= 1;
            if (k.dKey.isPressed || k.rightArrowKey.isPressed) v.x += 1;
        }
#else
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) v.x -= 1;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) v.x += 1;
#endif
        return v.sqrMagnitude > 1f ? v.normalized : v;
    }

    public static bool Fire()
    {
        if (FireOverride) return true;
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; var k = Keyboard.current; var t = Touchscreen.current;
        return (k != null && (k.spaceKey.isPressed || k.jKey.isPressed))
            || (m != null && m.leftButton.isPressed)
            || (t != null && t.primaryTouch.press.isPressed);
#else
        return Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || Input.GetMouseButton(0);
#endif
    }

    /// <summary>Dash, edge-triggered.</summary>
    public static bool Dash()
    {
        if (DashOverride) { DashOverride = false; return true; }
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current;
        return k != null && (k.leftShiftKey.wasPressedThisFrame || k.rightShiftKey.wasPressedThisFrame || k.lKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift) || Input.GetKeyDown(KeyCode.L);
#endif
    }

    /// <summary>EX shot / Super, edge-triggered.</summary>
    public static bool Ex()
    {
        if (ExOverride) { ExOverride = false; return true; }
#if ENABLE_INPUT_SYSTEM
        var m = Mouse.current; var k = Keyboard.current;
        return (k != null && k.kKey.wasPressedThisFrame) || (m != null && m.rightButton.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.K) || Input.GetMouseButtonDown(1);
#endif
    }

    public static bool MutePressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; return k != null && k.mKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.M);
#endif
    }

    public static bool RetryPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; return k != null && (k.jKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame);
#else
        return Input.GetKeyDown(KeyCode.J) || Input.GetKeyDown(KeyCode.Return);
#endif
    }

    /// <summary>Temporary mode select on the title screen: 1 Classic, 2 Flappy, 3 Boss Rush. 0 = none.</summary>
    public static int ModeKey()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; if (k == null) return 0;
        return k.digit1Key.wasPressedThisFrame ? 1 : k.digit2Key.wasPressedThisFrame ? 2 : k.digit3Key.wasPressedThisFrame ? 3 : 0;
#else
        return Input.GetKeyDown(KeyCode.Alpha1) ? 1 : Input.GetKeyDown(KeyCode.Alpha2) ? 2 : Input.GetKeyDown(KeyCode.Alpha3) ? 3 : 0;
#endif
    }

    public static bool RestartPressed()
    {
#if ENABLE_INPUT_SYSTEM
        var k = Keyboard.current; return k != null && k.rKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.R);
#endif
    }
}
