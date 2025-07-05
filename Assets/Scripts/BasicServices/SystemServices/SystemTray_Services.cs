using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SystemTray_Services : MonoBehaviour
{
    [SerializeField]
    public Texture2D SystemTray_Icon;

    // 通知开关状态
    public static bool IsNotificationEnabled = true;

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

    private void Toggle_Notification()
    {
        IsNotificationEnabled = !IsNotificationEnabled;
        Debug.Log($"系统托盘触发: 切换通知状态 - {IsNotificationEnabled}");
        
        // 显示状态变更通知
        ShowNotificationStatusChange();
    }

    private void ShowNotificationStatusChange()
    {
#if !UNITY_EDITOR
        try
        {
            TrayIcon.ShowBalloonTip(
                "Shittim Canvas", 
                IsNotificationEnabled ? "通知已开启" : "通知已关闭", 
                TrayIcon.ToolTipIcon.Info, 
                true
            );
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"发送通知状态变更通知失败: {ex.Message}");
        }
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
