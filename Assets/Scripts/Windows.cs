using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class HideWindow : MonoBehaviour
{
    [DllImport("user32.dll")]
    static extern IntPtr GetActiveWindow();
    [DllImport("User32.dll")]
    public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    [DllImport("User32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    private const int GWL_EXSTYLE = -0x14;
    private const int WS_EX_TOOLWINDOW = 0x0080;
    private const int SW_MINIMIZE = 2;

    void Start()
    {
        #if !UNITY_EDITOR
        IntPtr pMainWindow = GetActiveWindow();
        SetWindowLong(pMainWindow, GWL_EXSTYLE, GetWindowLong(pMainWindow, GWL_EXSTYLE) | WS_EX_TOOLWINDOW);
        ShowWindow(pMainWindow, SW_MINIMIZE);
        #endif
    }
}