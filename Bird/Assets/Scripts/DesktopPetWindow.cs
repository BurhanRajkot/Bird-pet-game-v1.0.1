using UnityEngine;
using System;
using System.Runtime.InteropServices;

public class DesktopPetWindow : MonoBehaviour
{
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR

    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll")]
    static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    static extern bool SetLayeredWindowAttributes(IntPtr hwnd, uint crKey, byte bAlpha, uint dwFlags);

    [DllImport("user32.dll")]
    static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    const int GWL_STYLE = -16;
    const int GWL_EXSTYLE = -20;

    const int WS_POPUP = unchecked((int)0x80000000);
    const int WS_VISIBLE = 0x10000000;

    const int WS_EX_LAYERED = 0x00080000;
    const int WS_EX_TRANSPARENT = 0x00000020;
    const int WS_EX_TOOLWINDOW = 0x00000080;
    const int WS_EX_TOPMOST = 0x00000008;
    const int WS_EX_NOACTIVATE = 0x08000000;

    const uint LWA_COLORKEY = 0x00000001;

    static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);

    const uint SWP_NOMOVE = 0x0002;
    const uint SWP_NOSIZE = 0x0001;
    const uint SWP_SHOWWINDOW = 0x0040;
    const uint SWP_FRAMECHANGED = 0x0020;

    const int SW_SHOW = 5;

#endif

    [Header("Overlay Settings")]
    public bool clickThrough = true;
    public bool topMost = true;
    public bool hideFromTaskbar = true;
    public bool dontStealFocus = true;

    void Start()
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        ApplyWindowSettings();
#endif
    }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
    void ApplyWindowSettings()
    {
        IntPtr hwnd = GetActiveWindow();
        if (hwnd == IntPtr.Zero) return;

        SetWindowLong(hwnd, GWL_STYLE, WS_POPUP | WS_VISIBLE);

        int exStyle = GetWindowLong(hwnd, GWL_EXSTYLE);

        exStyle |= WS_EX_LAYERED;

        if (clickThrough) exStyle |= WS_EX_TRANSPARENT;
        if (topMost) exStyle |= WS_EX_TOPMOST;
        if (hideFromTaskbar) exStyle |= WS_EX_TOOLWINDOW;
        if (dontStealFocus) exStyle |= WS_EX_NOACTIVATE;

        SetWindowLong(hwnd, GWL_EXSTYLE, exStyle);

        // Black becomes transparent
        SetLayeredWindowAttributes(hwnd, 0x000000, 0, LWA_COLORKEY);

        if (topMost)
        {
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0,
                SWP_NOMOVE | SWP_NOSIZE | SWP_SHOWWINDOW | SWP_FRAMECHANGED);
        }

        ShowWindow(hwnd, SW_SHOW);
    }
#endif
}