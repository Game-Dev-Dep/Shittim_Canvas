using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using UnityEngine.UI;

public class Config_Services : MonoBehaviour
{
    public static Config_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Config Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField]
    public Button Save_Function_Settings_Button;

    public Camera_Config MemoryLobby_Camera_Config = new Camera_Config();
    public Function_Config Global_Function_Config = new Function_Config();
    public WindowFilter_Config Gloabal_WindowFilter_Config = new WindowFilter_Config();
    
    private void Start()
    {
        Console_Log($"开始初始化 Config Services");

        Save_Function_Settings_Button.onClick.AddListener(Save_Global_Function_Config_Listener);

        MemoryLobby_Camera_Config = File_Services.Load_Specific_Type_From_File<Camera_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "MemoryLobby Camera Config.json"));
        Console_Log($"读取到的摄像机设置:\n" +
                    $"角色名: {MemoryLobby_Camera_Config.Defalut_Character_Name}\n" +
                    $"位置X: {MemoryLobby_Camera_Config.Camera_Position_X}\n" +
                    $"位置Y: {MemoryLobby_Camera_Config.Camera_Position_Y}\n" +
                    $"旋转Z: {MemoryLobby_Camera_Config.Camera_Rotation_Z}\n" +
                    $"缩放: {MemoryLobby_Camera_Config.Camera_Size}"
                    );

        Global_Function_Config = File_Services.Load_Specific_Type_From_File<Function_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
        Console_Log($"读取到的功能设置:\n" +
                    $"拖拽: {Global_Function_Config.is_IK_On}\n" +
                    $"对话: {Global_Function_Config.is_Talk_On}\n" +
                    $"日语对话字幕: {Global_Function_Config.is_Subtitle_JP_On}\n" +
                    $"自定义对话字幕: {Global_Function_Config.is_Subtitle_Custom_On}\n" +
                    $"全局声音: {Global_Function_Config.is_Global_Sound_On}\n" +
                    $"语音: {Global_Function_Config.is_Talk_Sound_On}\n" +
                    $"语音音量: {Global_Function_Config.Talk_Sound}\n" +
                    $"音效: {Global_Function_Config.is_SFX_Sound_On}\n" +
                    $"音效音量：{Global_Function_Config.SFX_Sound}\n" +
                    $"BGM: {Global_Function_Config.is_BGM_Sound_On}\n" +
                    $"BGM音量: {Global_Function_Config.BGM_Sound}\n" +
                    $"后期处理: {Global_Function_Config.is_Volume_On}\n" +
                    $"开机自启动: {Global_Function_Config.is_AutoStartup_On}\n" +
                    $"默认壁纸模式启动: {Global_Function_Config.is_Auto_Wallpaper_Mode_On}"
                    );

        Gloabal_WindowFilter_Config = File_Services.Load_Specific_Type_From_File<WindowFilter_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "WindowFilter Config.json"));
        Console_Log($"读取到的自定义覆盖窗口设置:\n" +
                    $"窗口标题个数: {Gloabal_WindowFilter_Config.Title_Names.Count}\n" +
                    $"窗口类名个数: {Gloabal_WindowFilter_Config.Class_Names.Count}"
                    );

        Console_Log($"结束初始化 Config Services");
    }

    public void Save_Camera_Config(Camera_Config camera_config, string file_path)
    {
        Console_Log($"保存的摄像机设置:\n" +
                    $"角色名: {camera_config.Defalut_Character_Name}\n" +
                    $"位置X: {camera_config.Camera_Position_X}\n" +
                    $"位置Y: {camera_config.Camera_Position_Y}\n" +
                    $"旋转Z: {camera_config.Camera_Rotation_Z}\n" +
                    $"缩放: {camera_config.Camera_Size}");
        File_Services.Save_Specific_Type_To_File<Camera_Config>(camera_config, file_path);
    }

    public void Save_Global_Function_Config_Listener()
    {
        Spine_Services.Instance.Set_Config();
        Subtitle_Services.Instance.Set_Config();
        Audio_Services.Instance.Set_Config();
        Volume_Services.Instance.Set_Config(); //这玩意报not exist，但不妨碍build，就先不管了
        Wallpaper_Services.Instance.Set_Config();
        Save_Function_Config(Global_Function_Config, Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
    }

    public void Save_Function_Config(Function_Config function_config, string file_path)
    {
        Console_Log($"保存的功能设置:\n" +
                    $"拖拽: {function_config.is_IK_On}\n" +
                    $"对话: {function_config.is_Talk_On}\n" +
                    $"日语对话字幕: {function_config.is_Subtitle_JP_On}\n" +
                    $"自定义对话字幕: {function_config.is_Subtitle_Custom_On}\n" +
                    $"全局声音: {function_config.is_Global_Sound_On}\n" +
                    $"语音: {function_config.is_Talk_Sound_On}\n" +
                    $"语音音量: {function_config.Talk_Sound}\n" +
                    $"音效: {function_config.is_SFX_Sound_On}\n" +
                    $"音效音量：{function_config.SFX_Sound}\n" +
                    $"BGM: {function_config.is_BGM_Sound_On}\n" +
                    $"BGM音量: {function_config.BGM_Sound}\n" +
                    $"后期处理: {function_config.is_Volume_On}\n" +
                    $"开机自启动: {function_config.is_AutoStartup_On}\n" +
                    $"默认壁纸模式启动: {function_config.is_Auto_Wallpaper_Mode_On}");
        File_Services.Save_Specific_Type_To_File<Function_Config>(function_config, file_path);
    }
                    // 修了一下上面的参名，在这留个备份（
                    //$"日语对话字幕: {Global_Function_Config.is_Subtitle_JP_On}\n" +
                    //$"自定义对话字幕: {Global_Function_Config.is_Subtitle_Custom_On}\n" +

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Config_Services", message, loglevel, logtype); }
}
