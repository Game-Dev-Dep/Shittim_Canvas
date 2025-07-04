using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SystemTray_Services : MonoBehaviour
{
    [SerializeField]
    public Texture2D SystemTray_Icon;

    void Awake()
    {
#if !UNITY_EDITOR
        Debug.Log($"开始初始化系统托盘");

        List<(string, Action)> SystemTray_Menu = new List<(string, Action)>()
        {
            ("进入壁纸模式", Enter_Wallpaper_Mode),
            ("返回正常模式", Quit_Wallpaper_Mode),
            (TrayIcon.SEPARATOR, null),
            ("退出", Quit_Program)
        };

        TrayIcon.Init("ShittimCanvas", "Shittim Canvas", SystemTray_Icon, SystemTray_Menu);

        Debug.Log($"结束初始化系统托盘");
#endif
    }
    private void Enter_Wallpaper_Mode()
    {
        Debug.Log($"系统托盘触发: 进入壁纸模式");

        if (!Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Wallpaper_Mode()
    {
        Debug.Log($"系统托盘触发: 返回正常模式");
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Program()
    {
        Debug.Log("系统托盘触发: 退出");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
