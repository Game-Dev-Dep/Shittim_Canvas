using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Framerate_Services : MonoBehaviour
{
    public static Framerate_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Framerate Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Core Variables")]
    public bool is_VSync_Mode = true;
    public int Target_Framerate = 120;

    private void Start()
    {
        Console_Log("开始初始化 Framerate Services");

        Get_Config();

        Console_Log("结束初始化 Framerate Services");
    }

    private void Get_Config()
    {
        is_VSync_Mode = Config_Services.Instance.Global_Setting_Config.Graphic.Wallpaper_Mode_Refresh_Type == 0 ? true : false;
        Target_Framerate = Config_Services.Instance.Global_Setting_Config.Graphic.Wallpaper_Mode_Framerate;
        
        // 应用加载的设置
        Apply_VSync_Settings();
    }

    public void Toggle_VSync_Mode()
    {
        Console_Log("触发切换 VSync 模式");

        is_VSync_Mode = !is_VSync_Mode;

        Apply_VSync_Settings();
    }

    public void Apply_VSync_Settings()
    {
        if (is_VSync_Mode)
        {
            Console_Log("切换为 VSync 模式");
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
        }
        else
        {
            Console_Log("切换为指定帧率模式");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Target_Framerate;
        }
    }

    public void Set_Target_Framerate_Listener(string value) { Set_Target_Framerate(value); }

    public void Set_Target_Framerate(string value, bool is_temp_change = false)
    {
        if (!is_VSync_Mode)
        {
            Console_Log("开始设置目标帧率");

            if (int.TryParse(value, out int input_framerate) && input_framerate > 0 && input_framerate < 360)
            {
                Console_Log($"目标帧率 {input_framerate} 为合法的非负整数");
                if (!is_temp_change) Target_Framerate = input_framerate;
                Application.targetFrameRate = input_framerate;
            }
            else
            {
                Console_Log($"目标帧率 {input_framerate} 不为合法的非负整数");
            }

            Console_Log("结束设置目标帧率");
        }
        else
        {
            Console_Log("当前为VSync模式，不能指定帧率");
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Framerate_Services", message, loglevel, logtype); }
}
