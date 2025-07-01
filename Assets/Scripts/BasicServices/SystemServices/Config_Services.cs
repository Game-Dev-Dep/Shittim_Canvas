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
            Debug.Log("[Awake] Config Services �����������");
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
        Console_Log($"��ʼ��ʼ�� Config Services");

        Save_Function_Settings_Button.onClick.AddListener(Save_Global_Function_Config_Listener);

        MemoryLobby_Camera_Config = File_Services.Load_Specific_Type_From_File<Camera_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "MemoryLobby Camera Config.json"));
        Console_Log($"��ȡ�������������:\n" +
                    $"��ɫ��: {MemoryLobby_Camera_Config.Defalut_Character_Name}\n" +
                    $"λ��X: {MemoryLobby_Camera_Config.Camera_Position_X}\n" +
                    $"λ��Y: {MemoryLobby_Camera_Config.Camera_Position_Y}\n" +
                    $"��תZ: {MemoryLobby_Camera_Config.Camera_Rotation_Z}\n" +
                    $"����: {MemoryLobby_Camera_Config.Camera_Size}"
                    );

        Global_Function_Config = File_Services.Load_Specific_Type_From_File<Function_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
        Console_Log($"��ȡ���Ĺ�������:\n" +
                    $"��ק: {Global_Function_Config.is_IK_On}\n" +
                    $"�Ի�: {Global_Function_Config.is_Talk_On}\n" +
                    $"����Ի���Ļ: {Global_Function_Config.is_Subtitle_JP_On}\n" +
                    $"�Զ���Ի���Ļ: {Global_Function_Config.is_Subtitle_Custom_On}\n" +
                    $"ȫ������: {Global_Function_Config.is_Global_Sound_On}\n" +
                    $"����: {Global_Function_Config.is_Talk_Sound_On}\n" +
                    $"��������: {Global_Function_Config.Talk_Sound}\n" +
                    $"��Ч: {Global_Function_Config.is_SFX_Sound_On}\n" +
                    $"��Ч������{Global_Function_Config.SFX_Sound}\n" +
                    $"BGM: {Global_Function_Config.is_BGM_Sound_On}\n" +
                    $"BGM����: {Global_Function_Config.BGM_Sound}\n" +
                    $"���ڴ���: {Global_Function_Config.is_Volume_On}\n" +
                    $"����������: {Global_Function_Config.is_AutoStartup_On}\n" +
                    $"Ĭ�ϱ�ֽģʽ����: {Global_Function_Config.is_Auto_Wallpaper_Mode_On}"
                    );

        Gloabal_WindowFilter_Config = File_Services.Load_Specific_Type_From_File<WindowFilter_Config>(Path.Combine(File_Services.Config_Files_Folder_Path, "WindowFilter Config.json"));
        Console_Log($"��ȡ�����Զ��帲�Ǵ�������:\n" +
                    $"���ڱ������: {Gloabal_WindowFilter_Config.Title_Names.Count}\n" +
                    $"������������: {Gloabal_WindowFilter_Config.Class_Names.Count}"
                    );

        Console_Log($"������ʼ�� Config Services");
    }

    public void Save_Camera_Config(Camera_Config camera_config, string file_path)
    {
        Console_Log($"��������������:\n" +
                    $"��ɫ��: {camera_config.Defalut_Character_Name}\n" +
                    $"λ��X: {camera_config.Camera_Position_X}\n" +
                    $"λ��Y: {camera_config.Camera_Position_Y}\n" +
                    $"��תZ: {camera_config.Camera_Rotation_Z}\n" +
                    $"����: {camera_config.Camera_Size}");
        File_Services.Save_Specific_Type_To_File<Camera_Config>(camera_config, file_path);
    }

    public void Save_Global_Function_Config_Listener()
    {
        Spine_Services.Instance.Set_Config();
        Subtitle_Services.Instance.Set_Config();
        Audio_Services.Instance.Set_Config();
        Volume_Services.Instance.Set_Config();
        Wallpaper_Services.Instance.Set_Config();
        Save_Function_Config(Global_Function_Config, Path.Combine(File_Services.Config_Files_Folder_Path, "Function Config.json"));
    }

    public void Save_Function_Config(Function_Config function_config, string file_path)
    {
        Console_Log($"����Ĺ�������:\n" +
                    $"��ק: {function_config.is_IK_On}\n" +
                    $"�Ի�: {function_config.is_Talk_On}\n" +
                    $"����Ի���Ļ: {Global_Function_Config.is_Subtitle_JP_On}\n" +
                    $"�Զ���Ի���Ļ: {Global_Function_Config.is_Subtitle_Custom_On}\n" +
                    $"ȫ������: {function_config.is_Global_Sound_On}\n" +
                    $"����: {function_config.is_Talk_Sound_On}\n" +
                    $"��������: {function_config.Talk_Sound}\n" +
                    $"��Ч: {function_config.is_SFX_Sound_On}\n" +
                    $"��Ч������{function_config.SFX_Sound}\n" +
                    $"BGM: {function_config.is_BGM_Sound_On}\n" +
                    $"BGM����: {function_config.BGM_Sound}\n" +
                    $"���ڴ���: {function_config.is_Volume_On}\n" +
                    $"����������: {function_config.is_AutoStartup_On}\n" +
                    $"Ĭ�ϱ�ֽģʽ����: {function_config.is_Auto_Wallpaper_Mode_On}");
        File_Services.Save_Specific_Type_To_File<Function_Config>(function_config, file_path);
    }


    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Config_Services", message, loglevel, logtype); }
}
