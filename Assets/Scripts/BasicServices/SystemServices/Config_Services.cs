using System.IO;
using UnityEngine;
using Newtonsoft.Json;
using Spine;
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
    public Setting_Config Global_Setting_Config = new Setting_Config();
    public Favorite_Config Global_Favorite_Config = new Favorite_Config();
    public CharacterTimer_Config Global_CharacterTimer_Config = new CharacterTimer_Config();


    // Fix for CS0120 and CS0572 errors
    // The issue arises because `Setting_Config.Audio` is a nested class, not an instance.
    // To fix this, we need to ensure that `Global_Setting_Config.Audio` is properly instantiated and accessed.

    private void Start()
    {
        Console_Log($"开始初始化 Config Services");

        Save_Function_Settings_Button.onClick.AddListener(Save_Global_Function_Config_Listener);

        Global_Setting_Config = File_Services.Load_Specific_Type_From_File<Setting_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
        Console_Log(
            $"读取到的全局设置:\n" +
            $"常规设置:\n" +
            $"  语言选中项: {Global_Setting_Config.General.Language}\n" +
            $"  语言选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Language_List)}\n" +
            $"  开机自启动选中项: {Global_Setting_Config.General.Auto_Startup}\n" +
            $"  开机启动选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Auto_Startup_List)}\n" +
            $"  默认壁纸模式选中项: {Global_Setting_Config.General.Auto_Wallpaper_Mode}\n" +
            $"  默认壁纸模式选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Auto_Wallpaper_Mode_List)}\n" +
            $"  通知启用选中项: {Global_Setting_Config.General.Notification_Enabled}\n" +
            $"  壁纸模式状态区域启用选中项: {Global_Setting_Config.General.Wallpaper_Mode_Status_Area_Enabled}\n" +
            $"  OOBE完成状态: {Global_Setting_Config.General.OOBE_Completed}\n" +
            $"音频设置:\n" +
            $"  全局音量: {Global_Setting_Config.Audio.Global_Sound}\n" +
            $"  语音音量: {Global_Setting_Config.Audio.Talk_Sound}\n" +
            $"  音效音量: {Global_Setting_Config.Audio.SFX_Sound}\n" +
            $"  背景音乐音量: {Global_Setting_Config.Audio.BGM_Sound}\n" +
            $"视频设置:\n" +
            $"  编辑模式分辨率: {Global_Setting_Config.Graphic.Editor_Mode_Resolution_Width} x {Global_Setting_Config.Graphic.Editor_Mode_Resolution_Height}\n" +
            $"  编辑模式 UI 缩放: {Global_Setting_Config.Graphic.Editor_Mode_UI_Scale}\n" +
            $"  壁纸模式显示策略选中项: {Global_Setting_Config.Graphic.Wallpaper_Mode_Refresh_Type}\n" +
            $"  壁纸模式显示策略选项: {JsonConvert.SerializeObject(Global_Setting_Config.Graphic.Wallpaper_Mode_Refresh_Type_List)}\n" +
            $"  壁纸模式锁帧帧率: {Global_Setting_Config.Graphic.Wallpaper_Mode_Framerate}");

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
                    $"后期处理: {Global_Function_Config.is_Volume_On}\n"
                    );

        Gloabal_WindowFilter_Config = File_Services.Load_Specific_Type_From_File<WindowFilter_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "WindowFilter Config.json"));
        Console_Log($"读取到的自定义覆盖窗口设置:\n" +
                    $"壁纸模式交互白名单 - 窗口标题个数: {Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Title_Names.Count}\n" +
                    $"壁纸模式交互白名单 - 窗口类名个数: {Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Class_Names.Count}\n" +
                    $"全屏检测静音白名单 - 窗口标题个数: {Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Title_Names.Count}\n" +
                    $"全屏检测静音白名单 - 窗口类名个数: {Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Class_Names.Count}"
                    );

        Global_Favorite_Config = File_Services.Load_Specific_Type_From_File<Favorite_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "Favorite Config.json"));
        Console_Log(
            "读取到的收藏设置:\n" +
            $"收藏的个数: {Global_Favorite_Config.Character_Names.Count}\n" +
            $"收藏的内容: {JsonConvert.SerializeObject(Global_Favorite_Config.Character_Names.ToArray())}\n"
        );

        Global_CharacterTimer_Config = File_Services.Load_Specific_Type_From_File<CharacterTimer_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "CharacterTimer Config.json"));

        Console_Log($"结束初始化 Config Services");
    }

    public void Save_Setting_Config(Setting_Config setting_config, string file_path)
    {
        Console_Log
        (
            $"保存的全局设置:\n" +
            $"常规设置:\n" +
            $"  语言选中项: {Global_Setting_Config.General.Language}\n" +
            $"  语言选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Language_List)}\n" +
            $"  开机自启动选中项: {Global_Setting_Config.General.Auto_Startup}\n" +
            $"  开机启动选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Auto_Startup_List)}\n" +
            $"  默认壁纸模式选中项: {Global_Setting_Config.General.Auto_Wallpaper_Mode}\n" +
            $"  默认壁纸模式选项: {JsonConvert.SerializeObject(Global_Setting_Config.General.Auto_Wallpaper_Mode_List)}\n" +
            $"  通知启用选中项: {Global_Setting_Config.General.Notification_Enabled}\n" +
            $"  壁纸模式状态区域启用选中项: {Global_Setting_Config.General.Wallpaper_Mode_Status_Area_Enabled}\n" +
            $"  OOBE完成状态: {Global_Setting_Config.General.OOBE_Completed}\n" +
            $"音频设置:\n" +
            $"  全局音量: {Global_Setting_Config.Audio.Global_Sound}\n" +
            $"  语音音量: {Global_Setting_Config.Audio.Talk_Sound}\n" +
            $"  音效音量: {Global_Setting_Config.Audio.SFX_Sound}\n" +
            $"  背景音乐音量: {Global_Setting_Config.Audio.BGM_Sound}\n" +
            $"视频设置:\n" +
            $"  编辑模式分辨率: {Global_Setting_Config.Graphic.Editor_Mode_Resolution_Width} x {Global_Setting_Config.Graphic.Editor_Mode_Resolution_Height}\n" +
            $"  编辑模式 UI 缩放: {Global_Setting_Config.Graphic.Editor_Mode_UI_Scale}\n" +
            $"  壁纸模式显示策略选中项: {Global_Setting_Config.Graphic.Wallpaper_Mode_Refresh_Type}\n" +
            $"  壁纸模式显示策略选项: {JsonConvert.SerializeObject(Global_Setting_Config.Graphic.Wallpaper_Mode_Refresh_Type_List)}\n" +
            $"  壁纸模式锁帧帧率: {Global_Setting_Config.Graphic.Wallpaper_Mode_Framerate}"
        );
        File_Services.Save_Specific_Type_To_File<Setting_Config>(setting_config, file_path);
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
        Volume_Services.Instance.Set_Config();
        Save_Function_Config(Global_Function_Config, Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
        Toast_Wrapper_Services.ShowToast("toast.save_function_settings", 3f, "success");
    }

    public void Auto_Save_Function_Config()
    {
        Spine_Services.Instance.Set_Config();
        Subtitle_Services.Instance.Set_Config();
        Volume_Services.Instance.Set_Config();
        Save_Function_Config(Global_Function_Config, Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
    }

    public void Save_Function_Config(Function_Config function_config, string file_path)
    {
        Console_Log($"保存的功能设置:\n" +
                    $"拖拽: {function_config.is_IK_On}\n" +
                    $"对话: {function_config.is_Talk_On}\n" +
                    $"日语对话字幕: {function_config.is_Subtitle_JP_On}\n" +
                    $"自定义对话字幕: {function_config.is_Subtitle_Custom_On}\n" +
                    $"后期处理: {function_config.is_Volume_On}\n");
        File_Services.Save_Specific_Type_To_File<Function_Config>(function_config, file_path);
    }

    public void Save_Favorite_Config(Favorite_Config favorite_config, string file_path)
    {
        Console_Log
        (
            "保存的收藏设置:\n" +
            $"收藏的个数: {favorite_config.Character_Names.Count}\n" +
            $"收藏的内容: {JsonConvert.SerializeObject(favorite_config.Character_Names.ToArray())}\n"
        );
        File_Services.Save_Specific_Type_To_File<Favorite_Config>(favorite_config, file_path);
    }

    public void Save_WindowFilter_Config(WindowFilter_Config windowFilter_config, string file_path)
    {
        Console_Log($"保存的窗口过滤设置:\n" +
                    $"壁纸模式交互白名单 - 窗口标题个数: {windowFilter_config.Wallpaper_Interaction_Whitelist_Title_Names.Count}\n" +
                    $"壁纸模式交互白名单 - 窗口类名个数: {windowFilter_config.Wallpaper_Interaction_Whitelist_Class_Names.Count}\n" +
                    $"全屏检测静音白名单 - 窗口标题个数: {windowFilter_config.Fullscreen_Mute_Whitelist_Title_Names.Count}\n" +
                    $"全屏检测静音白名单 - 窗口类名个数: {windowFilter_config.Fullscreen_Mute_Whitelist_Class_Names.Count}");
        File_Services.Save_Specific_Type_To_File<WindowFilter_Config>(windowFilter_config, file_path);
    }

    public void Save_CharacterTimer_Config(CharacterTimer_Config timer_config, string file_path)
    {
        File_Services.Save_Specific_Type_To_File<CharacterTimer_Config>(timer_config, file_path);
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Config_Services", message, loglevel, logtype); }
}
