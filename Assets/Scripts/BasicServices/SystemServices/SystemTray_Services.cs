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
            ("切换通知开关", Toggle_Notification),
            (TrayIcon.SEPARATOR, null),
            ("退出", Quit_Program)
        };

        TrayIcon.Init("ShittimCanvas", "Shittim Canvas", SystemTray_Icon, SystemTray_Menu);

        Debug.Log($"结束初始化系统托盘");
#endif
    }

    public void Toggle_Notification()
    {
        Notification_Services.Instance.is_Notification_On = !Notification_Services.Instance.is_Notification_On;
        Console_Log($"系统托盘触发: 切换通知状态为 {Notification_Services.Instance.is_Notification_On}");

        Notification_Services.Instance.Send_Notifiction(Notification_Services.Instance.is_Notification_On ? "通知已开启" : "通知已关闭");
    }

    private void Enter_Wallpaper_Mode()
    {
        Console_Log($"系统托盘触发: 进入壁纸模式");

        if (!Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Wallpaper_Mode()
    {
        Console_Log($"系统托盘触发: 返回正常模式");
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Program()
    {
        Console_Log("系统托盘触发: 退出");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("SystemTray Services", message, loglevel, logtype); }
}
