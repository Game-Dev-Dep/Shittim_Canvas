using System;
using UnityEngine;
using Utils;

public class Notification_Services : MonoBehaviour
{
    public static Notification_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Notification Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool is_Notification_On = true;

    public void Send_Notifiction(string notifiction_message, string notificiton_title = "Shittim_Canvas")
    {

#if !UNITY_EDITOR

        try
        {
            TrayIcon.ShowBalloonTip(
                notificiton_title,
                notifiction_message,
                TrayIcon.ToolTipIcon.Info,
                true
            );
            Console_Log("$已发送通知 {notifiction_message}");
        }
        catch (Exception ex)
        {
            Console_Log($"发送通知失败: {ex.Message}", Debug_Services.LogLevel.Debug, LogType.Warning);
        }

#endif

    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Notification_Services", message, loglevel, logtype); }
}
