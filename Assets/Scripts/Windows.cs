using System;
using System.Runtime.InteropServices;
using UnityEngine;

public class HideWindow : MonoBehaviour {
    #region Win32 API
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    
    [DllImport("User32.dll")]
    private static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
    
    [DllImport("User32.dll")]
    private static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    #endregion

    #region Constants
    private const int GWL_EXSTYLE = -0x14;
    private const int WS_EX_TOOLWINDOW = 0x0080;
    private const int SW_MINIMIZE = 2;
    #endregion

    #region Unity Lifecycle 
    private void Start() {
        HideApplicationWindow();
    }
    #endregion

    #region Window Management
    private void HideApplicationWindow() {
        #if !UNITY_EDITOR
        IntPtr mainWindow = GetActiveWindow();
        SetWindowLong(
            mainWindow, 
            GWL_EXSTYLE, 
            GetWindowLong(mainWindow, GWL_EXSTYLE) | WS_EX_TOOLWINDOW
        );
        ShowWindow(mainWindow, SW_MINIMIZE);
        #endif
    }
    #endregion
}