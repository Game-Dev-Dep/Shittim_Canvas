#if UNITY_EDITOR

using System.IO;
using UnityEngine;

public class Screenshot_Services : MonoBehaviour
{
    public static Screenshot_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Screenshot Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool is_Screenshot = false;

    public void Take_Screenshot(string character_name)
    {
        string file_name = $"Memorial Lobby {character_name}.png";
        string file_path = Path.Combine("Screenshots", file_name);
        string screenshot_folder_path = Path.GetDirectoryName(file_path);
        if (!Directory.Exists(screenshot_folder_path)) Directory.CreateDirectory(screenshot_folder_path);
        if (!File.Exists(file_path))
        {
            ScreenCapture.CaptureScreenshot(file_path);
            Console_Log($"截图已保存: {file_path}");
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Screenshot_Services", message, loglevel, logtype); }
}

#endif