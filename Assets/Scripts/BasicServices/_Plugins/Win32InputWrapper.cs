using System;
using System.Drawing;
using System.Runtime.InteropServices;
using UnityEngine;


public class Win32InputWrapper
{
    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr GetActiveWindow();

    [DllImport("user32.dll", SetLastError = true)]
    public static extern short GetAsyncKeyState(int vkey);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern void GetCursorPos(out Point lpPoint);

    public static IntPtr FindUnityWindow()
    {
        IntPtr hWnd = GetActiveWindow();
        return hWnd;
    }

    public static bool GetKeyDown(win32keycode vKey)
    {
        short keyState = GetAsyncKeyState((int)vKey);
        return keyState < 0;
    }

    public static Vector3 GetMousePosVector3()
    {
        Point point = new Point();
        GetCursorPos(out point);
        return new Vector3(point.X, point.Y, 0);
    }

    public static Vector2 GetMousePosVector2()
    {
        Point point = new Point();
        GetCursorPos(out point);
        return new Vector2(point.X, point.Y);
    }

}

//KeyCode from https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/inputdev/virtual-key-codes.md
public enum win32keycode
{
    LMB = 1,
    RMB = 2,
    SHIFT = 16,
    CTRL = 17,
    ALT = 18,
    Num0 = 48,
    Num1 = 49,
    Num2 = 50,
    Num3 = 51,
    Num4 = 52,
    Num5 = 53,
    Num6 = 54,
    Num7 = 55,
    Num8 = 56,
    Num9 = 57,
    A = 65,
    B = 66,
    C = 67,
    D = 68,
    E = 69,
    F = 70,
    G = 71,
    H = 72,
    I = 73,
    J = 74,
    K = 75,
    L = 76,
    M = 77,
    N = 78,
    O = 79,
    P = 80,
    Q = 81,
    R = 82,
    S = 83,
    T = 84,
    U = 85,
    V = 86,
    W = 87,
    X = 88,
    Y = 89,
    Z = 90,
}
