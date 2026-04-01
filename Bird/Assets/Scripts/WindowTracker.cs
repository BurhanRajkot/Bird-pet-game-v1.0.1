using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Text;

public class WindowTracker : MonoBehaviour
{
    [Header("References")]
    public Transform nestTransform;
    public Transform perchTargetTransform;

    [Header("Perch Offset (desktop pixels)")]
    public Vector2 perchOffsetPixels = new Vector2(-80f, 30f);

    [Header("Polling")]
    public float pollInterval = 0.15f;

    [Header("Window Rules")]
    public float minWindowWidth = 120f;
    public float minWindowHeight = 80f;
    public bool debugLogs = true;

    public bool HasValidWindow { get; private set; }
    public Vector3 CurrentPerchWorld { get; private set; }
    public string CurrentWindowTitle { get; private set; }

    float pollTimer;
    IntPtr currentTrackedHwnd = IntPtr.Zero;
    string ownWindowTitle;

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", SetLastError = true)]
    static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

    [DllImport("user32.dll")]
    static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    [DllImport("user32.dll")]
    static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    static extern IntPtr GetShellWindow();

    [DllImport("user32.dll")]
    static extern bool IsIconic(IntPtr hWnd); // minimized

#endif

    void Awake()
    {
        ownWindowTitle = Application.productName.ToLower();
    }

    void Start()
    {
        if (nestTransform != null)
            CurrentPerchWorld = nestTransform.position;

        if (perchTargetTransform != null)
            perchTargetTransform.position = CurrentPerchWorld;
    }

    void Update()
    {
        pollTimer += Time.deltaTime;

        if (pollTimer >= pollInterval)
        {
            pollTimer = 0f;
            TrackForegroundWindow();
        }

        if (perchTargetTransform != null)
            perchTargetTransform.position = CurrentPerchWorld;
    }

    void TrackForegroundWindow()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

        IntPtr hwnd = GetForegroundWindow();

        if (hwnd == IntPtr.Zero)
        {
            ReturnToNest("No foreground window");
            return;
        }

        if (hwnd == GetShellWindow())
        {
            ReturnToNest("Desktop / shell active");
            return;
        }

        if (!IsWindowVisible(hwnd))
        {
            ReturnToNest("Foreground not visible");
            return;
        }

        if (IsIconic(hwnd))
        {
            ReturnToNest("Foreground minimized");
            return;
        }

        string title = GetWindowTitle(hwnd);
        string lo = title.ToLower().Trim();

        if (string.IsNullOrWhiteSpace(title))
        {
            ReturnToNest("No title");
            return;
        }

        if (IsBadWindow(lo))
        {
            ReturnToNest("Ignored window: " + title);
            return;
        }

        if (!GetWindowRect(hwnd, out RECT rect))
        {
            ReturnToNest("Failed GetWindowRect");
            return;
        }

        float width = rect.Right - rect.Left;
        float height = rect.Bottom - rect.Top;

        if (width < minWindowWidth || height < minWindowHeight)
        {
            ReturnToNest("Too small: " + title);
            return;
        }

        currentTrackedHwnd = hwnd;
        CurrentWindowTitle = title;
        HasValidWindow = true;
        CurrentPerchWorld = CalculatePerch(rect);

        if (debugLogs)
        {
            Debug.Log($"TRACKING ACTIVE WINDOW: {title} | Rect: {rect.Left},{rect.Top},{rect.Right},{rect.Bottom}");
        }

#else
        ReturnToNest("Not Windows build");
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    string GetWindowTitle(IntPtr hwnd)
    {
        StringBuilder sb = new StringBuilder(512);
        GetWindowText(hwnd, sb, sb.Capacity);
        return sb.ToString();
    }
#endif

    bool IsBadWindow(string lo)
    {
        if (string.IsNullOrWhiteSpace(lo)) return true;

        // Ignore your own overlay
        if (lo.Contains(ownWindowTitle)) return true;

        // Ignore editor/dev windows
        if (lo.Contains("unity")) return true;
        if (lo.Contains("desktopia")) return true;

        // Ignore obvious overlays/system junk
        if (lo.Contains("xbox game bar")) return true;
        if (lo.Contains("nvidia overlay")) return true;
        if (lo.Contains("geforce overlay")) return true;
        if (lo.Contains("discord overlay")) return true;
        if (lo.Contains("program manager")) return true;
        if (lo.Contains("task switching")) return true;
        if (lo.Contains("action center")) return true;
        if (lo.Contains("notification center")) return true;
        if (lo.Contains("msi afterburner")) return true;
        if (lo.Contains("rtss")) return true;
        if (lo.Contains("amd software")) return true;

        return false;
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    Vector3 CalculatePerch(RECT rect)
    {
        // Sit near top-right of active window
        float screenX = rect.Right + perchOffsetPixels.x;
        float screenY = rect.Top + perchOffsetPixels.y;

        screenX = Mathf.Clamp(screenX, 0f, Screen.width);
        screenY = Mathf.Clamp(screenY, 0f, Screen.height);

        Vector2 world = ScreenToWorld.Convert(screenX, screenY);
        return new Vector3(world.x, world.y, 0f);
    }
#endif

    void ReturnToNest(string reason)
    {
        HasValidWindow = false;
        currentTrackedHwnd = IntPtr.Zero;
        CurrentWindowTitle = "";

        if (nestTransform != null)
            CurrentPerchWorld = nestTransform.position;
        else
            CurrentPerchWorld = Vector3.zero;

        if (debugLogs)
            Debug.Log("RETURN TO NEST: " + reason);
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = HasValidWindow ? Color.green : Color.yellow;
            Gizmos.DrawWireSphere(CurrentPerchWorld, 0.15f);
        }
    }
#endif
}