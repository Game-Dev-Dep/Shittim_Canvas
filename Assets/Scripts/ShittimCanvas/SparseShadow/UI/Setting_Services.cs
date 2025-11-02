using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization;
using UnityEngine.UI;
using static Setting_Services;

public class Setting_Services : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    public GameObject Setting_Root_GameObject;
    [SerializeField]
    public Toggle Setting_General_Option_Toggle;
    [SerializeField]
    public Toggle Setting_Audio_Option_Toggle;
    [SerializeField]
    public Toggle Setting_Graphic_Option_Toggle;
    [SerializeField]
    public Toggle Setting_OverlayConfig_Option_Toggle;
    [SerializeField]
    public Toggle Setting_About_Option_Toggle;
    [SerializeField]
    public GameObject Setting_Content_GameObject;
    [SerializeField]
    public GameObject Setting_Detail_Option_Template_GameObject;
    [SerializeField]
    public Button Setting_Toggle_Button;
    [SerializeField]
    public Button Setting_Off_Button;
    [SerializeField]
    public Button Setting_Save_Button;
    [SerializeField]
    public Button Setting_Reset_Button;
    [SerializeField]
    public GameObject Setting_Save_Button_Unsaved_Icon;
    [SerializeField]
    public GameObject Setting_Toggle_Button_Unsaved_Icon;

    //[Header("UI Settings")]

    [Header("Core Variables")]
    public bool is_Setting_On = false;
    public float Setting_Detail_Option_Width;
    public float Setting_Detail_Option_Height;
    public float Setting_Detail_Option_Spacing;
    public Setting_Option_Type Cur_Setting_Option_Type;
    public Dictionary<Setting_Option_Type, List<Setting_Detail_Option>> Setting_Contents = new Dictionary<Setting_Option_Type, List<Setting_Detail_Option>>();

    private Setting_Config setting_config;
    private Setting_Config saved_setting_config;
    private WindowFilter_Config saved_windowFilter_config;
    private LocalizedString localizedString = new LocalizedString();
    private int buildVersionClickCount = 0; // 构建版本点击计数器

    private string GetGraphicsAPIString()
    {
        UnityEngine.Rendering.GraphicsDeviceType deviceType = SystemInfo.graphicsDeviceType;
        switch (deviceType)
        {
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
            case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                return "DirectX";
            case UnityEngine.Rendering.GraphicsDeviceType.Vulkan:
                return "Vulkan";
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLCore:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES2:
            case UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3:
                return "OpenGL";
            case UnityEngine.Rendering.GraphicsDeviceType.Metal:
                return "Metal";
            default:
                return deviceType.ToString();
        }
    }

    private void Get_Setting_Config()
    {
        setting_config = Config_Services.Instance.Global_Setting_Config;

        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[setting_config.General.Language];

        GameObject.Find("Canvas").GetComponent<CanvasScaler>().scaleFactor = setting_config.Graphic.Editor_Mode_UI_Scale;

        setting_config.About.Version = Version_Services.Instance.Version_String;
        setting_config.About.Build_Date = Version_Services.Instance.Build_Time_String;

        // 同步通知服务状态
        if (Notification_Services.Instance != null)
        {
            Notification_Services.Instance.is_Notification_On = (setting_config.General.Notification_Enabled == 0);
        }
    }

    public void Save_Setting_Config()
    {
        Config_Services.Instance.Save_Setting_Config(setting_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
        Config_Services.Instance.Save_WindowFilter_Config(Config_Services.Instance.Gloabal_WindowFilter_Config, Path.Combine(File_Services.Config_Files_Folder_Path, "WindowFilter Config.json"));
        saved_setting_config = CloneSettingConfig(setting_config);
        saved_windowFilter_config = CloneWindowFilterConfig(Config_Services.Instance.Gloabal_WindowFilter_Config);
        if (Setting_Save_Button_Unsaved_Icon != null) Setting_Save_Button_Unsaved_Icon.SetActive(false);
        if (Setting_Toggle_Button_Unsaved_Icon != null) Setting_Toggle_Button_Unsaved_Icon.SetActive(false);
        Toast_Wrapper_Services.ShowToast("toast.settings_saved", 3f);
    }

    private void Save_Setting_Config_Silent()
    {
        Config_Services.Instance.Save_Setting_Config(setting_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
    }

    public void Reset_Setting_Config()
    {
        setting_config = new Setting_Config();
        Config_Services.Instance.Gloabal_WindowFilter_Config = new WindowFilter_Config();
        GameObject.Find("Canvas").GetComponent<CanvasScaler>().scaleFactor = setting_config.Graphic.Editor_Mode_UI_Scale;
        Refresh_Display_Options();
        Save_Setting_Config();
        Setting_Contents = new Dictionary<Setting_Option_Type, List<Setting_Detail_Option>>();
        Init_Setting_Contents();
        Update_Setting_Content_UI();
    }

    public void Init_Setting_Contents()
    {
        Setting_Contents.Add(
            Setting_Option_Type.General,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.languages",
                    Description_Key = "settings_panel.general.languages.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = setting_config.General.Language,
                    Dropdown_Options = setting_config.General.Language_List,
                    Dropdown_Callback = (value) => {
                        // 验证语言索引是否有效
                        if (value >= 0 && value < LocalizationSettings.AvailableLocales.Locales.Count)
                        {
                            setting_config.General.Language = value;
                            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
                            Setting_Contents[Setting_Option_Type.General][0].Dropdown_Value = value;
                            // dropdown校验
                            if (Setting_Contents[Setting_Option_Type.General][0].Dropdown_Component != null)
                            {
                                Setting_Contents[Setting_Option_Type.General][0].Dropdown_Component.value = value;
                            }
                        }
                        else
                        {
                            UnityEngine.Debug.LogWarning($"无效的语言索引: {value}, 可用语言数量: {LocalizationSettings.AvailableLocales.Locales.Count}");
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.auto_start",
                    Description_Key = "settings_panel.general.auto_start.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Auto_Startup,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Auto_Startup = int.Parse(Setting_Contents[Setting_Option_Type.General][1].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][1].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][1].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            if(Setting_Contents[Setting_Option_Type.General][1].ToggleGroup_Value == 0)
                            {
                                AutoStartup_Services.Instance.Enable_AutoStartup();
                            }
                            else
                            {
                                AutoStartup_Services.Instance.Disable_AutoStartup();
                            }
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.random_character_on_startup",
                    Description_Key = "settings_panel.general.random_character_on_startup.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Random_Character_On_Startup,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Random_Character_On_Startup = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.auto_random_character_interval",
                    Description_Key = "settings_panel.general.auto_random_character_interval.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = setting_config.General.Auto_Random_Character_Interval,
                    Dropdown_Options = setting_config.General.Auto_Random_Character_Interval_List,
                    Dropdown_Callback = (value) => {
                        setting_config.General.Auto_Random_Character_Interval = value;
                        Setting_Contents[Setting_Option_Type.General][3].Dropdown_Value = value;
                        if (Setting_Contents[Setting_Option_Type.General][3].Dropdown_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.General][3].Dropdown_Component.value = value;
                        }
                        if (Dropdown_Services.Instance != null)
                        {
                            Dropdown_Services.Instance.UpdateAutoRandomInterval(value);
                        }
                        Update_Setting_Content_UI();
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.auto_random_character_range",
                    Description_Key = "settings_panel.general.auto_random_character_range.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = setting_config.General.Auto_Random_Character_Range,
                    Dropdown_Options = setting_config.General.Auto_Random_Character_Range_List,
                    Dropdown_Callback = (value) => {
                        setting_config.General.Auto_Random_Character_Range = value;
                        Setting_Contents[Setting_Option_Type.General][4].Dropdown_Value = value;
                        if (Setting_Contents[Setting_Option_Type.General][4].Dropdown_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.General][4].Dropdown_Component.value = value;
                        }
                        if (Dropdown_Services.Instance != null)
                        {
                            Dropdown_Services.Instance.UpdateAutoRandomRange(value);
                        }
                    },
                    isNew = true,
                    IsVisible = () => setting_config.General.Auto_Random_Character_Interval > 0
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.pseudo_random_mode",
                    Description_Key = "settings_panel.general.pseudo_random_mode.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Pseudo_Random_Mode,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Pseudo_Random_Mode = int.Parse(Setting_Contents[Setting_Option_Type.General][5].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][5].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][5].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                        }
                    },
                    isNew = true
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.auto_wallpaper",
                    Description_Key = "settings_panel.general.auto_wallpaper.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Auto_Wallpaper_Mode,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Auto_Wallpaper_Mode = int.Parse(Setting_Contents[Setting_Option_Type.General][6].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][6].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][6].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.notification_enabled",
                    Description_Key = "settings_panel.general.notification_enabled.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Notification_Enabled,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Notification_Enabled = int.Parse(Setting_Contents[Setting_Option_Type.General][7].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][7].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][7].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);

                            // 更新通知服务的状态
                            if (Notification_Services.Instance != null)
                            {
                                Notification_Services.Instance.is_Notification_On = (setting_config.General.Notification_Enabled == 0);
                            }
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.general.wallpaper_mode_status_area_enabled",
                    Description_Key = "settings_panel.general.wallpaper_mode_status_area_enabled.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Wallpaper_Mode_Status_Area_Enabled,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Wallpaper_Mode_Status_Area_Enabled = int.Parse(Setting_Contents[Setting_Option_Type.General][8].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][8].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][8].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                        }
                    }
                }
            }
        );

        Setting_Contents.Add(
            Setting_Option_Type.Audio,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.main_volume",
                    Description_Key = "settings_panel.audio.main_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.Global_Sound,
                    Slider_Min_Value = 0f,
                    Slider_Max_Value = 1f,
                    Slider_Text_Min_Value = 0f,   // 0%
                    Slider_Text_Max_Value = 100f, // 100%
                    Slider_Callback = (value) => {
                        setting_config.Audio.Global_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][0].Slider_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Audio][0].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Audio][0].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Text_Max_Value):F0}";
                        }
                        Audio_Services.Instance.Global_Sound_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.voice_volume",
                    Description_Key = "settings_panel.audio.voice_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.Talk_Sound,
                    Slider_Min_Value = 0f,
                    Slider_Max_Value = 1f,
                    Slider_Text_Min_Value = 0f,   // 0%
                    Slider_Text_Max_Value = 100f, // 100%
                    Slider_Callback = (value) => {
                        setting_config.Audio.Talk_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][1].Slider_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Audio][1].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Audio][1].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Text_Max_Value):F0}";
                        }
                        Audio_Services.Instance.Talk_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.se_volume",
                    Description_Key = "settings_panel.audio.se_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.SFX_Sound,
                    Slider_Min_Value = 0f,
                    Slider_Max_Value = 1f,
                    Slider_Text_Min_Value = 0f,   // 0%
                    Slider_Text_Max_Value = 100f, // 100%
                    Slider_Callback = (value) => {
                        setting_config.Audio.SFX_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][2].Slider_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Audio][2].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Audio][2].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Text_Max_Value):F0}";
                        }
                        Audio_Services.Instance.SFX_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.bgm_volume",
                    Description_Key = "settings_panel.audio.bgm_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.BGM_Sound,
                    Slider_Min_Value = 0f,
                    Slider_Max_Value = 1f,
                    Slider_Text_Min_Value = 0f,   // 0%
                    Slider_Text_Max_Value = 100f, // 100%
                    Slider_Callback = (value) => {
                        setting_config.Audio.BGM_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][3].Slider_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Audio][3].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Audio][3].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Text_Max_Value):F0}";
                        }
                        Audio_Services.Instance.BGM_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.ui_sfx_volume",
                    Description_Key = "settings_panel.audio.ui_sfx_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.UI_SFX_Sound,
                    Slider_Min_Value = 0f,
                    Slider_Max_Value = 1f,
                    Slider_Text_Min_Value = 0f,
                    Slider_Text_Max_Value = 100f,
                    Slider_Callback = (value) => {
                        setting_config.Audio.UI_SFX_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][4].Slider_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Audio][4].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Audio][4].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][4].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][4].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][4].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][4].Slider_Text_Max_Value):F0}";
                        }
                        Audio_Services.Instance.UI_SFX_Slider_Handler(value);
                    }
                },
            }
        );

        Setting_Contents.Add(
            Setting_Option_Type.Graphic,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.graphic.window_size",
                    Description_Key = "settings_panel.graphic.window_size.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = Get_Resolution_Dropdown_Index(),
                    Dropdown_Options = Get_Available_Resolutions(),
                    Dropdown_Callback = (value) => {
                        var resolutions = Get_Available_Resolutions();
                        if (value < resolutions.Count)
                        {
                            string selectedResolution = resolutions[value];
                            string[] resolution = selectedResolution.Split('x');
                            if (resolution.Length == 2 && int.TryParse(resolution[0], out int width) && int.TryParse(resolution[1], out int height))
                            {
                                setting_config.Graphic.Editor_Mode_Resolution_Width = width;
                                setting_config.Graphic.Editor_Mode_Resolution_Height = height;
                                
                                Window_Services.Instance.Edit_Mode_Height = height;
                                Window_Services.Instance.Edit_Mode_Width = width;

                                Screen.SetResolution(width, height, FullScreenMode.Windowed);
                                
                                Console_Log($"设置分辨率: {width}x{height}");
                            }
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.graphic.display_monitor",
                    Description_Key = "settings_panel.graphic.display_monitor.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = setting_config.Graphic.Selected_Display_Monitor_Index,
                    Dropdown_Options = setting_config.Graphic.Display_Monitor_Options,
                    Dropdown_Callback = (value) => {
                        setting_config.Graphic.Selected_Display_Monitor_Index = value;
                        Setting_Contents[Setting_Option_Type.Graphic][1].Dropdown_Value = value;
                        if (Setting_Contents[Setting_Option_Type.Graphic][1].Dropdown_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Graphic][1].Dropdown_Component.value = value;
                        }

                        var displays = Display.displays;
                        if (value < displays.Length)
                        {
                            var selectedDisplay = displays[value];
                            Console_Log($"选择显示器: 显示器 {value + 1} ({selectedDisplay.systemWidth}x{selectedDisplay.systemHeight})");
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.graphic.ui_scale",
                    Description_Key = "settings_panel.graphic.ui_scale.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Graphic.Editor_Mode_UI_Scale,
                    Slider_Min_Value = 0.5f,
                    Slider_Max_Value = 1.5f,
                    Slider_Text_Min_Value = 0f,   // 0%
                    Slider_Text_Max_Value = 100f, // 100%
                    Slider_Callback = (value) => {
                        setting_config.Graphic.Editor_Mode_UI_Scale = value;
                        GameObject.Find("Canvas").GetComponent<CanvasScaler>().scaleFactor = value;
                        if (Setting_Contents[Setting_Option_Type.Graphic][2].Slider_InputField_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.Graphic][2].Slider_InputField_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Text_Max_Value):F0}";
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.graphic.wallpaper_mode_policy",
                    Description_Key = "settings_panel.graphic.wallpaper_mode_policy.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.Graphic.Wallpaper_Mode_Refresh_Type,
                    ToggleGroup_Options = new List<string> { "settings_panel.graphic.vsync", "settings_panel.elements.frame_lock_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.Graphic.Wallpaper_Mode_Refresh_Type = int.Parse(Setting_Contents[Setting_Option_Type.Graphic][3].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            if(setting_config.Graphic.Wallpaper_Mode_Refresh_Type == 0)
                            {
                                Framerate_Services.Instance.is_VSync_Mode = true;
                            }
                            else
                            {
                                Framerate_Services.Instance.is_VSync_Mode = false;
                            }
                            Framerate_Services.Instance.Apply_VSync_Settings();
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.graphic.vsync",
                    Description_Key = "settings_panel.graphic.vsync.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Input,
                    Input_Value = setting_config.Graphic.Wallpaper_Mode_Framerate.ToString(),
                    Input_Callback = (value) => {
                        if (int.TryParse(value, out int framerate))
                        {
                            setting_config.Graphic.Wallpaper_Mode_Framerate = framerate;
                            Setting_Contents[Setting_Option_Type.Graphic][4].Input_Value = value;
                            Framerate_Services.Instance.Target_Framerate = framerate;

                            if (!Framerate_Services.Instance.is_VSync_Mode)
                            {
                                Framerate_Services.Instance.Apply_VSync_Settings();
                            }
                        }
                    }
                }
            }
        );

        Setting_Contents.Add(
            Setting_Option_Type.OverlayConfig,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.overlayconfig.wallpaper_interaction_whitelist_titles",
                    Description_Key = "settings_panel.overlayconfig.wallpaper_interaction_whitelist_titles.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.TextField,
                    TextField_Value = string.Join(",", Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Title_Names),
                    TextField_Callback = (value) => {
                        var titles = value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToList();
                        Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Title_Names = titles;
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.overlayconfig.wallpaper_interaction_whitelist_classes",
                    Description_Key = "settings_panel.overlayconfig.wallpaper_interaction_whitelist_classes.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.TextField,
                    TextField_Value = string.Join(",", Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Class_Names),
                    TextField_Callback = (value) => {
                        var classNames = value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                                              .Select(s => s.Trim())
                                              .Where(s => !string.IsNullOrEmpty(s))
                                              .ToList();
                        Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Class_Names = classNames;
                    },
                    isNew = true
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.overlayconfig.fullscreen_mute_whitelist_titles",
                    Description_Key = "settings_panel.overlayconfig.fullscreen_mute_whitelist_titles.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.TextField,
                    TextField_Value = string.Join(",", Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Title_Names),
                    TextField_Callback = (value) => {
                        var titles = value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                                          .Select(s => s.Trim())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToList();
                        Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Title_Names = titles;
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.overlayconfig.fullscreen_mute_whitelist_classes",
                    Description_Key = "settings_panel.overlayconfig.fullscreen_mute_whitelist_classes.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.TextField,
                    TextField_Value = string.Join(",", Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Class_Names),
                    TextField_Callback = (value) => {
                        var classNames = value.Split(new[] { ',' }, System.StringSplitOptions.RemoveEmptyEntries)
                                              .Select(s => s.Trim())
                                              .Where(s => !string.IsNullOrEmpty(s))
                                              .ToList();
                        Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Class_Names = classNames;
                    }
                }
            }
        );

        Setting_Contents.Add(
            Setting_Option_Type.About,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.about.shittim_canvas",
                    Description_Key = "settings_panel.about.shittim_canvas.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = "https://sc.japerz.com/",
                    Text_Click_Callback = () => {
                        try
                        {
                            Toast_Wrapper_Services.ShowToast("toast.open_webpage", 3f);
                            Application.OpenURL("https://sc.japerz.com/");
                        }
                        catch (System.Exception)
                        {
                            Toast_Wrapper_Services.ShowToast("toast.open_webpage_failed", 3f);
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.about.build_ver",
                    Description_Key = "settings_panel.about.build_ver.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = setting_config.About.Version,
                    Text_Click_Callback = () => {
                        buildVersionClickCount++;              
                        if (buildVersionClickCount >= 10)
                        {
                            Toast_Wrapper_Services.ShowToast("https://shittimcanvas114514.japerz.com", 5f);
                            buildVersionClickCount = 0; // Reset计数器
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.about.build_date",
                    Description_Key = "settings_panel.about.build_date.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = setting_config.About.Build_Date,
                    Text_Click_Callback = () => {
                        if (Changelog_Services.Instance != null)
                        {
                            Changelog_Services.Instance.Show_Changelog();
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.about.graphics_api",
                    Description_Key = "settings_panel.about.graphics_api.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = GetGraphicsAPIString(),
                    Text_Click_Callback = null
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.about.open_player_log",
                    Description_Key = "settings_panel.about.open_player_log.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Button,
                    Button_Text_Key = "settings_panel.about.open_player_log.button",
                    Button_Click_Callback = () => {
                        try
                        {
                            string playerLogPath = Path.Combine(Application.persistentDataPath, "Player.log");
                            string playerLogFolder = Path.GetDirectoryName(playerLogPath);
                            
                            if (Directory.Exists(playerLogFolder))
                            {
                                #if UNITY_EDITOR
                                UnityEngine.Debug.Log($"Player Log文件夹路径: {playerLogFolder}");
                                Toast_Wrapper_Services.ShowToast($"Player Log文件夹路径: {playerLogFolder}", 5f);
                                #else
                                Process.Start("explorer.exe", playerLogFolder);
                                Toast_Wrapper_Services.ShowToast("toast.player_log_folder_opened", 3f);
                                #endif
                            }
                            else
                            {
                                Toast_Wrapper_Services.ShowToast("toast.player_log_folder_not_found", 3f);
                            }
                        }
                        catch (System.Exception ex)
                        {
                            UnityEngine.Debug.LogError($"打开Player Log文件夹失败: {ex.Message}");
                            Toast_Wrapper_Services.ShowToast("toast.player_log_folder_open_failed", 3f);
                        }
                    }
                }
            }
        );
    }

    void Start()
    {
        Console_Log("开始初始化 Setting Services");

        Get_Detail_Option_UI_Parameters();

        Setting_Toggle_Button.onClick.AddListener(Toggle_Setting_Panel);
        Setting_Off_Button.onClick.AddListener(Hide_Setting_Panel);
        Setting_Save_Button.onClick.AddListener(Save_Setting_Config);
        Setting_Reset_Button.onClick.AddListener(Reset_Setting_Config);

        Setting_General_Option_Toggle.onValueChanged.AddListener(Toggle_General_Option);
        Setting_Audio_Option_Toggle.onValueChanged.AddListener(Toggle_Audio_Option);
        Setting_Graphic_Option_Toggle.onValueChanged.AddListener(Toggle_Graphic_Option);
        Setting_OverlayConfig_Option_Toggle.onValueChanged.AddListener(Toggle_OverlayConfig_Option);
        Setting_About_Option_Toggle.onValueChanged.AddListener(Toggle_About_Option);

        Get_Setting_Config();

        // 初始化显示器选项 - 延迟初始化
        StartCoroutine(Initialize_Display_Options_Coroutine());

        // 注册语言变更事件
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

        // 确保当前语言设置正确
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale != null)
        {
            var currentLanguageIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(currentLocale);
            if (currentLanguageIndex >= 0 && currentLanguageIndex != setting_config.General.Language)
            {
                setting_config.General.Language = currentLanguageIndex;
            }
        }

        // 同步语言列表与本地化设置
        SyncLanguageListWithLocalizationSettings();

        Audio_Services.Instance.Global_Sound_Slider_Handler(setting_config.Audio.Global_Sound);
        Audio_Services.Instance.Talk_Slider_Handler(setting_config.Audio.Talk_Sound);
        Audio_Services.Instance.SFX_Slider_Handler(setting_config.Audio.SFX_Sound);
        Audio_Services.Instance.BGM_Slider_Handler(setting_config.Audio.BGM_Sound);
        Audio_Services.Instance.UI_SFX_Slider_Handler(setting_config.Audio.UI_SFX_Sound);

        Init_Setting_Contents();

        Update_Setting_Content_UI();

        saved_setting_config = CloneSettingConfig(setting_config);
        saved_windowFilter_config = CloneWindowFilterConfig(Config_Services.Instance.Gloabal_WindowFilter_Config);
        if (Setting_Save_Button_Unsaved_Icon != null) Setting_Save_Button_Unsaved_Icon.SetActive(false);
        if (Setting_Toggle_Button_Unsaved_Icon != null) Setting_Toggle_Button_Unsaved_Icon.SetActive(false);

        Console_Log("结束初始化 Setting Services");
    }

    private void Update()
    {
        UpdateUnsavedIcon();
    }

    private void OnDestroy()
    {
        // 注销语言变更事件
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;
    }

    private void OnLanguageChanged(Locale locale)
    {
        // 语言变更时更新UI
        if (is_Setting_On)
        {
            // 更新当前语言设置
            var currentLanguageIndex = LocalizationSettings.AvailableLocales.Locales.IndexOf(locale);
            if (currentLanguageIndex >= 0)
            {
                setting_config.General.Language = currentLanguageIndex;
                if (Setting_Contents.ContainsKey(Setting_Option_Type.General) && Setting_Contents[Setting_Option_Type.General].Count > 0)
                {
                    Setting_Contents[Setting_Option_Type.General][0].Dropdown_Value = currentLanguageIndex;
                }
                Console_Log($"语言已切换到: {locale.LocaleName} (索引: {currentLanguageIndex})");
            }
            Update_Setting_Content_UI();
        }
    }

    private System.Collections.IEnumerator Initialize_Display_Options_Coroutine()
    {
        // 直接使用Unity的Screen API获取显示器信息
        setting_config.Graphic.Display_Monitor_Options = Get_Display_Options_From_Screen();

        // 确保选中的显示器索引有效
        if (setting_config.Graphic.Selected_Display_Monitor_Index >= setting_config.Graphic.Display_Monitor_Options.Count)
        {
            setting_config.Graphic.Selected_Display_Monitor_Index = 0;
        }

        yield break;
    }

    private List<string> Get_Display_Options_From_Screen()
    {
        var options = new List<string>();

        // 获取所有显示器
        var displays = Display.displays;

        for (int i = 0; i < displays.Length; i++)
        {
            var display = displays[i];
            string option = $"Display {i + 1} ({display.systemWidth}x{display.systemHeight})";
            if (i == 0)
            {
                option += " [Main]";
            }
            options.Add(option);
        }

        // 如果没有检测到显示器，使用默认选项
        if (options.Count == 0)
        {
            options.Add("Main Display");
        }

        return options;
    }

    public void Refresh_Display_Options()
    {
        setting_config.Graphic.Display_Monitor_Options = Get_Display_Options_From_Screen();

        // 确保选中的显示器索引有效
        if (setting_config.Graphic.Selected_Display_Monitor_Index >= setting_config.Graphic.Display_Monitor_Options.Count)
        {
            setting_config.Graphic.Selected_Display_Monitor_Index = 0;
        }

        // 如果当前正在显示图形设置，则更新UI
        if (is_Setting_On && Cur_Setting_Option_Type == Setting_Option_Type.Graphic)
        {
            Update_Setting_Content_UI();
        }
    }

    void Toggle_Setting_Panel()
    {
        is_Setting_On = !is_Setting_On;
        if(is_Setting_On)
        {
            Display_Setting_Panel();
        }
        else
        {
            Hide_Setting_Panel();
        }
    }

    void Display_Setting_Panel()
    {
        is_Setting_On = true;
        Setting_Root_GameObject.SetActive(is_Setting_On);
        
        // 重新获取最新的设置配置，确保OOBE中的语言选择能同步到设置面板
        Get_Setting_Config();
        
        // 清除现有的设置内容，重新初始化
        Setting_Contents.Clear();
        Init_Setting_Contents();
        
        // 更新UI显示
        Update_Setting_Content_UI();
    }

    void Hide_Setting_Panel()
    {
        is_Setting_On = false;
        Setting_Root_GameObject.SetActive(is_Setting_On);
    }

    void Toggle_General_Option(bool value)
    {
        if (!value)
        {
            return;
        }
        else
        {
            Cur_Setting_Option_Type = Setting_Option_Type.General;
            // 重新获取最新配置，确保显示正确的值
            Get_Setting_Config();
            Setting_Contents.Clear();
            Init_Setting_Contents();
            Update_Setting_Content_UI();
        }
    }

    void Toggle_Audio_Option(bool value)
    {
        if (!value)
        {
            return;
        }
        else
        {
            Cur_Setting_Option_Type = Setting_Option_Type.Audio;
            // 重新获取最新配置，确保显示正确的值
            Get_Setting_Config();
            Setting_Contents.Clear();
            Init_Setting_Contents();
            Update_Setting_Content_UI();
        }
    }

    void Toggle_Graphic_Option(bool value)
    {
        if (!value)
        {
            return;
        }
        else
        {
            Cur_Setting_Option_Type = Setting_Option_Type.Graphic;
            // 重新获取最新配置，确保显示正确的值
            Get_Setting_Config();
            Setting_Contents.Clear();
            Init_Setting_Contents();
            Update_Setting_Content_UI();
        }
    }

    void Toggle_OverlayConfig_Option(bool value)
    {
        if (!value)
        {
            return;
        }
        else
        {
            Cur_Setting_Option_Type = Setting_Option_Type.OverlayConfig;
            Get_Setting_Config();
            Setting_Contents.Clear();
            Init_Setting_Contents();
            Update_Setting_Content_UI();
        }
    }

    void Toggle_About_Option(bool value)
    {
        if (!value)
        {
            return;
        }
        else
        {
            Cur_Setting_Option_Type = Setting_Option_Type.About;
            // 重新获取最新配置，确保显示正确的值
            Get_Setting_Config();
            Setting_Contents.Clear();
            Init_Setting_Contents();
            Update_Setting_Content_UI();
        }
    }

    void Get_Detail_Option_UI_Parameters()
    {
        Setting_Detail_Option_Width = Setting_Detail_Option_Template_GameObject.GetComponent<RectTransform>().sizeDelta.x;
        Setting_Detail_Option_Height = Setting_Detail_Option_Template_GameObject.GetComponent<RectTransform>().sizeDelta.y;
        Setting_Detail_Option_Spacing = Setting_Content_GameObject.GetComponent<VerticalLayoutGroup>().spacing;
    }

    void Update_Setting_Content_UI()
    {
        Destroy_Setting_Content_UI();
        
        // 确保设置内容存在且使用最新配置
        if (!Setting_Contents.ContainsKey(Cur_Setting_Option_Type))
        {
            // 如果设置内容不存在，重新初始化
            Init_Setting_Contents();
        }
        
        switch (Cur_Setting_Option_Type)
        {
            case Setting_Option_Type.General:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.General]);
                break;
            case Setting_Option_Type.Audio:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.Audio]);
                break;
            case Setting_Option_Type.Graphic:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.Graphic]);
                break;
            case Setting_Option_Type.OverlayConfig:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.OverlayConfig]);
                break;
            case Setting_Option_Type.About:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.About]);
                break;
        }
    }

    void Create_Setting_Detail_Option_UI(List<Setting_Detail_Option> setting_detail_option_list)
    {
        int visibleCount = setting_detail_option_list.Count(opt => opt.IsVisible == null || opt.IsVisible());
        Setting_Content_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (Setting_Detail_Option_Height + Setting_Detail_Option_Spacing) * visibleCount);

        foreach (Setting_Detail_Option setting_detail_option in setting_detail_option_list)
        {
            bool isVisible = setting_detail_option.IsVisible == null || setting_detail_option.IsVisible();
            if (!isVisible) continue;

            GameObject new_detail_option = Instantiate(Setting_Detail_Option_Template_GameObject, Setting_Content_GameObject.transform);
            setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option;
            new_detail_option.SetActive(true);

            TextMeshProUGUI title_text = new_detail_option.transform.Find("[Setting] Detail Option Title Text").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description_text = new_detail_option.transform.Find("[Setting] Detail Option Description Text").GetComponent<TextMeshProUGUI>();
            Localization_Utils.Apply_Localization_To_Text(title_text, setting_detail_option.Title_Key);
            Localization_Utils.Apply_Localization_To_Text(description_text, setting_detail_option.Description_Key);

            Transform newTag = new_detail_option.transform.Find("[Setting] NEW");
            if (newTag != null) newTag.gameObject.SetActive(setting_detail_option.isNew);

            // 根据类型设置UI
            switch (setting_detail_option.Setting_Detail_Option_Type)
            {
                case Setting_Detail_Option_Type.Toggle:
                    // 设置Toggle相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Toggle Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.ToggleGroup_Component = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<ToggleGroup>();

                    int Toggle_Num = setting_detail_option.ToggleGroup_Options.Count;

                    int Toggle_Index = setting_detail_option.ToggleGroup_Value;

                    for (int i = 0; i < Toggle_Num; i++)
                    {
                        GameObject toggle_option = Instantiate(setting_detail_option.Setting_Detail_Option_GameObject.transform.Find($"[Setting] Detail Option Toggle Template").gameObject, setting_detail_option.Setting_Detail_Option_GameObject.transform);
                        toggle_option.SetActive(true);

                        toggle_option.gameObject.name = i.ToString();

                        Toggle toggle_component = toggle_option.GetComponent<Toggle>();

                        TextMeshProUGUI toggle_text = toggle_option.transform.Find("[Setting] Detail Option Text").GetComponent<TextMeshProUGUI>();

                        if (Localization_Utils.Is_Localization_Key(setting_detail_option.ToggleGroup_Options[i]))
                        {
                            Localization_Utils.Apply_Localization_To_Text(toggle_text, setting_detail_option.ToggleGroup_Options[i]);
                        }
                        else
                        {
                            toggle_text.text = setting_detail_option.ToggleGroup_Options[i];
                        }

                        if (setting_detail_option.Toggle_Callback != null)
                        {
                            toggle_component.onValueChanged.AddListener((value) => setting_detail_option.Toggle_Callback(value));
                        }
                    }

                    setting_detail_option.ToggleGroup_Component.SetAllTogglesOff();
                    for (int i = 0; i < Toggle_Num; i++)
                    {
                        if(i == Toggle_Index) setting_detail_option.Setting_Detail_Option_GameObject.transform.Find($"{i}").GetComponent<Toggle>().isOn = true;
                        else setting_detail_option.Setting_Detail_Option_GameObject.transform.Find($"{i}").GetComponent<Toggle>().isOn = false;
                    }
                    break;

                case Setting_Detail_Option_Type.Slider:
                    // 设置Slider相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Slider Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Slider_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Slider").GetComponent<Slider>();
                    
                    setting_detail_option.Slider_InputField_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Slider Text").GetComponent<TMP_InputField>();
                    
                    if (setting_detail_option.Slider_InputField_Component == null)
                    {
                        setting_detail_option.Text_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Slider Text").GetComponent<TextMeshProUGUI>();
                    }

                    setting_detail_option.Slider_Component.value = setting_detail_option.Slider_Value;
                    setting_detail_option.Slider_Component.minValue = setting_detail_option.Slider_Min_Value;
                    setting_detail_option.Slider_Component.maxValue = setting_detail_option.Slider_Max_Value;

                    if (setting_detail_option.Slider_InputField_Component != null)
                    {
                        setting_detail_option.Slider_InputField_Component.text = $"{Value_Map(setting_detail_option.Slider_Component.value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value):F0}";
                        setting_detail_option.Slider_InputField_Component.contentType = TMP_InputField.ContentType.IntegerNumber;
                        setting_detail_option.Slider_InputField_Component.characterLimit = 3;
                        
                        setting_detail_option.Slider_InputField_Component.onEndEdit.AddListener((inputValue) => {
                            if (int.TryParse(inputValue, out int newValue))
                            {
                                float mappedValue = Value_Map(newValue, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value);
                                mappedValue = Mathf.Clamp(mappedValue, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value);
                                
                                setting_detail_option.Slider_Component.value = mappedValue;
                                
                                if (setting_detail_option.Slider_Callback != null)
                                {
                                    setting_detail_option.Slider_Callback(mappedValue);
                                }
                            }
                            else
                            {
                                setting_detail_option.Slider_InputField_Component.text = $"{Value_Map(setting_detail_option.Slider_Component.value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value):F0}";
                            }
                        });
                    }
                    else
                    {
                        setting_detail_option.Text_Component.text = $"{Value_Map(setting_detail_option.Slider_Component.value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value):F0} %";
                    }

                    if (setting_detail_option.Slider_Callback != null)
                    {
                        setting_detail_option.Slider_Component.onValueChanged.AddListener((value) => {
                            if (setting_detail_option.Slider_InputField_Component != null)
                            {
                                setting_detail_option.Slider_InputField_Component.text = $"{Value_Map(value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value):F0}";
                            }
                            
                            setting_detail_option.Slider_Callback(value);
                        });
                    }
                    break;

                case Setting_Detail_Option_Type.Input:
                    // 设置Input相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Input Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.InputField_Component = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<TMP_InputField>();

                    setting_detail_option.InputField_Component.text = "";
                    (setting_detail_option.InputField_Component.placeholder as TMP_Text).text = setting_detail_option.Input_Value;

                    if (setting_detail_option.Input_Callback != null)
                    {
                        setting_detail_option.InputField_Component.onEndEdit.AddListener((value) => {
                            if (string.IsNullOrEmpty(value))
                            {
                                value = setting_detail_option.Input_Value;
                            }
                            setting_detail_option.Input_Callback(value);
                            setting_detail_option.Input_Value = value;
                            setting_detail_option.InputField_Component.text = "";
                            (setting_detail_option.InputField_Component.placeholder as TMP_Text).text = value;
                        });
                    }

                    break;

                case Setting_Detail_Option_Type.Dropdown:
                    // 设置Dropdown相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Dropdown Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Dropdown_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Dropdown").GetComponent<TMP_Dropdown>();

                    setting_detail_option.Dropdown_Component.ClearOptions();
                    // dropdown现在狠狠的用key                    
                    List<string> localizedOptions = new List<string>();
                    foreach (string option in setting_detail_option.Dropdown_Options)
                    {
                        if (Localization_Utils.Is_Localization_Key(option))
                        {
                            localizedOptions.Add(Localization_Utils.Get_Localized_Text(option));
                        }
                        else
                        {
                            localizedOptions.Add(option);
                        }
                    }
                    setting_detail_option.Dropdown_Component.AddOptions(localizedOptions);

                    // dropdown适配
                    setting_detail_option.Dropdown_Component.value = setting_detail_option.Dropdown_Value;

                    if (setting_detail_option.Dropdown_Callback != null)
                    {
                        setting_detail_option.Dropdown_Component.onValueChanged.AddListener((value) => setting_detail_option.Dropdown_Callback(value));
                    }
                    break;

                case Setting_Detail_Option_Type.Text:
                    // 设置Text相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Text Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Text_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Text").GetComponent<TextMeshProUGUI>();
                    setting_detail_option.Text_Component.text = setting_detail_option.Text_Value;
                    
                    // 为Text添加点击事件
                    if (setting_detail_option.Text_Click_Callback != null)
                    {
                        var button = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<Button>();
                        if (button == null)
                        {
                            button = setting_detail_option.Setting_Detail_Option_GameObject.AddComponent<Button>();
                        }
                        button.onClick.AddListener(() => setting_detail_option.Text_Click_Callback());
                    }
                    break;

                case Setting_Detail_Option_Type.Button:
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Text Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Text_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Text").GetComponent<TextMeshProUGUI>();

                    if (!string.IsNullOrEmpty(setting_detail_option.Button_Text_Key))
                    {
                        Localization_Utils.Apply_Localization_To_Text(setting_detail_option.Text_Component, setting_detail_option.Button_Text_Key);
                    }
                    else
                    {
                        setting_detail_option.Text_Component.text = "Button";
                    }
                    
                    if (setting_detail_option.Button_Click_Callback != null)
                    {
                        var button = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<Button>();
                        if (button == null)
                        {
                            button = setting_detail_option.Setting_Detail_Option_GameObject.AddComponent<Button>();
                        }
                        button.onClick.AddListener(() => setting_detail_option.Button_Click_Callback());
                    }
                    break;

                case Setting_Detail_Option_Type.TextField:
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option TextField Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.TextField_Component = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<TMP_InputField>();

                    setting_detail_option.TextField_Component.text = setting_detail_option.TextField_Value;

                    if (setting_detail_option.TextField_Callback != null)
                    {
                        setting_detail_option.TextField_Component.onValueChanged.AddListener((value) => {
                            setting_detail_option.TextField_Value = value;
                            setting_detail_option.TextField_Callback(value);
                        });
                    }
                    break;
            }
        }
    }

    void Destroy_Setting_Content_UI()
    {
        foreach (Transform child in Setting_Content_GameObject.transform)
        {
            if (child != Setting_Detail_Option_Template_GameObject.transform) Destroy(child.gameObject);
        }
    }

    public float Value_Map(float value, float input_min, float input_max, float output_min, float output_max)
    {
        value = Math.Clamp(value, input_min, input_max);
        float input_range = input_max - input_min;
        float output_range = output_max - output_min;
        return ((value - input_min) / input_range) * output_range + output_min;
    }




    public enum Setting_Option_Type
    {
        General,
        Audio,
        Graphic,
        OverlayConfig,
        About
    }

    public enum Setting_Detail_Option_Type
    {
        Toggle,
        Slider,
        Input,
        Dropdown,
        Text,
        Button,
        TextField
    }

    public class Setting_Detail_Option
    {
        public string Title_Key;
        public string Description_Key;
        public Setting_Detail_Option_Type Setting_Detail_Option_Type;
        public GameObject Setting_Detail_Option_GameObject;
        public bool isNew = false;
        public Func<bool> IsVisible = null;

        public ToggleGroup ToggleGroup_Component;
        public Slider Slider_Component;
        public TMP_InputField InputField_Component;
        public TMP_Dropdown Dropdown_Component;
        public TextMeshProUGUI Text_Component;
        public Button Button_Component;
        public TMP_InputField Slider_InputField_Component;
        public TMP_InputField TextField_Component;

        public int ToggleGroup_Value;
        public float Slider_Value;
        public string Input_Value;
        public int Dropdown_Value;
        public string Text_Value;
        public string Button_Text_Key;
        public string TextField_Value;

        public Action<bool> Toggle_Callback;
        public Action<float> Slider_Callback;
        public Action<string> Input_Callback;
        public Action<int> Dropdown_Callback;
        public Action Text_Click_Callback;
        public Action Button_Click_Callback;
        public Action<string> TextField_Callback;

        public List<string> ToggleGroup_Options;
        public float Slider_Min_Value = 0;
        public float Slider_Max_Value = 1;
        public float Slider_Text_Min_Value = 0;
        public float Slider_Text_Max_Value = 100;
        public List<string> Dropdown_Options;
    }



    private List<string> Get_Available_Resolutions()
    {
        var resolutions = new List<string>();
        
        // 获取所有显示器的最大分辨率
        int maxWidth = 0;
        int maxHeight = 0;
        
        var displays = Display.displays;
        if (displays.Length > 0)
        {
            foreach (var display in displays)
            {
                maxWidth = Math.Max(maxWidth, display.systemWidth);
                maxHeight = Math.Max(maxHeight, display.systemHeight);
            }
        }
        else
        {
            maxWidth = Screen.currentResolution.width;
            maxHeight = Screen.currentResolution.height;
        }
        
        // 分辨率列表
        var commonResolutions = new List<(int width, int height)>
        {
            (5120, 2880),  // 5K
            (4320, 2160),  // 4K - 变体2 
            (4096, 2304),  // 4K - 变体3
            (3840, 2160),  // 4K - 变体4
            (2560, 1440),  // 2K
            (2560, 1080),  // 2K
            (1920, 1080),  // Full HD
            (1600, 900),   // HD+
            (1366, 768),   // HD
            (1280, 720),   // HD
            (1024, 768),   // XGA
            (800, 600),    // SVGA
            (640, 480),    // VGA
        };
        
        // 过滤出不超过最大分辨率的选项
        foreach (var resolution in commonResolutions)
        {
            if (resolution.width <= maxWidth && resolution.height <= maxHeight)
            {
                resolutions.Add($"{resolution.width}x{resolution.height}");
            }
        }
        
        // 如果没有合适的选项，添加当前分辨率(避免刁钻的显示器分辨率hhh)
        if (resolutions.Count == 0)
        {
            resolutions.Add($"{maxWidth}x{maxHeight}");
        }
        
        return resolutions;
    }
    
    private int Get_Resolution_Dropdown_Index()
    {
        var resolutions = Get_Available_Resolutions();
        string currentResolution = $"{setting_config.Graphic.Editor_Mode_Resolution_Width}x{setting_config.Graphic.Editor_Mode_Resolution_Height}";
        
        for (int i = 0; i < resolutions.Count; i++)
        {
            if (resolutions[i] == currentResolution)
            {
                return i;
            }
        }
        
        // 如果当前分辨率不在列表中，返回第一个选项
        return 0;
    }

    private void SyncLanguageListWithLocalizationSettings()
    {
        // 检查语言列表数量是否与可用语言数量一致
        if (setting_config.General.Language_List.Count != LocalizationSettings.AvailableLocales.Locales.Count)
        {
            Console_Log($"语言列表数量 ({setting_config.General.Language_List.Count}) 与可用语言数量 ({LocalizationSettings.AvailableLocales.Locales.Count}) 不一致，正在同步...");
            
            // 根据本地化设置重新生成语言列表
            setting_config.General.Language_List.Clear();
            for (int i = 0; i < LocalizationSettings.AvailableLocales.Locales.Count; i++)
            {
                var locale = LocalizationSettings.AvailableLocales.Locales[i];
                string languageName = GetLanguageDisplayName(locale);
                setting_config.General.Language_List.Add(languageName);
            }
            
            Console_Log($"语言列表已同步: {string.Join(", ", setting_config.General.Language_List)}");
        }
        
        // 确保语言索引有效
        if (setting_config.General.Language >= setting_config.General.Language_List.Count)
        {
            setting_config.General.Language = 0;
            Console_Log($"语言索引超出范围，已重置为0");
        }
    }

    private string GetLanguageDisplayName(Locale locale)
    {
        // 根据语言代码返回对应的显示名称
        switch (locale.Identifier.Code)
        {
            case "en":
                return "English";
            case "ja":
                return "日本語";
            case "zh":
                return "简体中文";
            case "zh-TW":
                return "繁體中文";
            default:
                return locale.LocaleName;
        }
    }

    // 更新小红点相关
    private void UpdateUnsavedIcon()
    {
        if (saved_setting_config == null || saved_windowFilter_config == null) return;
        
        bool hasSettingChanges = 
            setting_config.General.Language != saved_setting_config.General.Language ||
            setting_config.General.Auto_Startup != saved_setting_config.General.Auto_Startup ||
            setting_config.General.Auto_Wallpaper_Mode != saved_setting_config.General.Auto_Wallpaper_Mode ||
            setting_config.General.Notification_Enabled != saved_setting_config.General.Notification_Enabled ||
            setting_config.General.Wallpaper_Mode_Status_Area_Enabled != saved_setting_config.General.Wallpaper_Mode_Status_Area_Enabled ||
             setting_config.General.Random_Character_On_Startup != saved_setting_config.General.Random_Character_On_Startup ||
             setting_config.General.Auto_Random_Character_Interval != saved_setting_config.General.Auto_Random_Character_Interval ||
             setting_config.General.Auto_Random_Character_Range != saved_setting_config.General.Auto_Random_Character_Range ||
             setting_config.General.Pseudo_Random_Mode != saved_setting_config.General.Pseudo_Random_Mode ||
            Mathf.Abs(setting_config.Audio.Global_Sound - saved_setting_config.Audio.Global_Sound) > 0.001f ||
            Mathf.Abs(setting_config.Audio.Talk_Sound - saved_setting_config.Audio.Talk_Sound) > 0.001f ||
            Mathf.Abs(setting_config.Audio.SFX_Sound - saved_setting_config.Audio.SFX_Sound) > 0.001f ||
            Mathf.Abs(setting_config.Audio.BGM_Sound - saved_setting_config.Audio.BGM_Sound) > 0.001f ||
            Mathf.Abs(setting_config.Audio.UI_SFX_Sound - saved_setting_config.Audio.UI_SFX_Sound) > 0.001f ||
            setting_config.Graphic.Editor_Mode_Resolution_Width != saved_setting_config.Graphic.Editor_Mode_Resolution_Width ||
            setting_config.Graphic.Editor_Mode_Resolution_Height != saved_setting_config.Graphic.Editor_Mode_Resolution_Height ||
            Mathf.Abs(setting_config.Graphic.Editor_Mode_UI_Scale - saved_setting_config.Graphic.Editor_Mode_UI_Scale) > 0.001f ||
            setting_config.Graphic.Wallpaper_Mode_Refresh_Type != saved_setting_config.Graphic.Wallpaper_Mode_Refresh_Type ||
            setting_config.Graphic.Wallpaper_Mode_Framerate != saved_setting_config.Graphic.Wallpaper_Mode_Framerate ||
            setting_config.Graphic.Selected_Display_Monitor_Index != saved_setting_config.Graphic.Selected_Display_Monitor_Index;

        bool hasWindowFilterChanges = !ListEquals(Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Title_Names, saved_windowFilter_config.Wallpaper_Interaction_Whitelist_Title_Names) ||
            !ListEquals(Config_Services.Instance.Gloabal_WindowFilter_Config.Wallpaper_Interaction_Whitelist_Class_Names, saved_windowFilter_config.Wallpaper_Interaction_Whitelist_Class_Names) ||
            !ListEquals(Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Title_Names, saved_windowFilter_config.Fullscreen_Mute_Whitelist_Title_Names) ||
            !ListEquals(Config_Services.Instance.Gloabal_WindowFilter_Config.Fullscreen_Mute_Whitelist_Class_Names, saved_windowFilter_config.Fullscreen_Mute_Whitelist_Class_Names);
        
        bool hasChanges = hasSettingChanges || hasWindowFilterChanges;
        
        if (Setting_Save_Button_Unsaved_Icon != null) Setting_Save_Button_Unsaved_Icon.SetActive(hasChanges);
        if (Setting_Toggle_Button_Unsaved_Icon != null) Setting_Toggle_Button_Unsaved_Icon.SetActive(hasChanges);
    }

    private bool ListEquals(List<string> list1, List<string> list2)
    {
        if (list1 == null && list2 == null) return true;
        if (list1 == null || list2 == null) return false;
        if (list1.Count != list2.Count) return false;
        for (int i = 0; i < list1.Count; i++)
        {
            if (list1[i] != list2[i]) return false;
        }
        return true;
    }

    // 复制一份设置配置来比对设置是否发生变化
    private Setting_Config CloneSettingConfig(Setting_Config config)
    {
        var clone = new Setting_Config();
         clone.General.Language = config.General.Language;
         clone.General.Auto_Startup = config.General.Auto_Startup;
         clone.General.Random_Character_On_Startup = config.General.Random_Character_On_Startup;
         clone.General.Auto_Random_Character_Interval = config.General.Auto_Random_Character_Interval;
         clone.General.Auto_Random_Character_Range = config.General.Auto_Random_Character_Range;
         clone.General.Pseudo_Random_Mode = config.General.Pseudo_Random_Mode;
         clone.General.Auto_Wallpaper_Mode = config.General.Auto_Wallpaper_Mode;
         clone.General.Notification_Enabled = config.General.Notification_Enabled;
        clone.General.Wallpaper_Mode_Status_Area_Enabled = config.General.Wallpaper_Mode_Status_Area_Enabled;
        clone.General.OOBE_Completed = config.General.OOBE_Completed;
        clone.Audio.Global_Sound = config.Audio.Global_Sound;
        clone.Audio.Talk_Sound = config.Audio.Talk_Sound;
        clone.Audio.SFX_Sound = config.Audio.SFX_Sound;
        clone.Audio.BGM_Sound = config.Audio.BGM_Sound;
        clone.Audio.UI_SFX_Sound = config.Audio.UI_SFX_Sound;
        clone.Graphic.Editor_Mode_Resolution_Width = config.Graphic.Editor_Mode_Resolution_Width;
        clone.Graphic.Editor_Mode_Resolution_Height = config.Graphic.Editor_Mode_Resolution_Height;
        clone.Graphic.Editor_Mode_UI_Scale = config.Graphic.Editor_Mode_UI_Scale;
        clone.Graphic.Wallpaper_Mode_Refresh_Type = config.Graphic.Wallpaper_Mode_Refresh_Type;
        clone.Graphic.Wallpaper_Mode_Framerate = config.Graphic.Wallpaper_Mode_Framerate;
        clone.Graphic.Selected_Display_Monitor_Index = config.Graphic.Selected_Display_Monitor_Index;
        return clone;
    }

    private WindowFilter_Config CloneWindowFilterConfig(WindowFilter_Config config)
    {
        var clone = new WindowFilter_Config();
        clone.Wallpaper_Interaction_Whitelist_Title_Names = new List<string>(config.Wallpaper_Interaction_Whitelist_Title_Names);
        clone.Wallpaper_Interaction_Whitelist_Class_Names = new List<string>(config.Wallpaper_Interaction_Whitelist_Class_Names);
        clone.Fullscreen_Mute_Whitelist_Title_Names = new List<string>(config.Fullscreen_Mute_Whitelist_Title_Names);
        clone.Fullscreen_Mute_Whitelist_Class_Names = new List<string>(config.Fullscreen_Mute_Whitelist_Class_Names);
        return clone;
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Setting_Services", message, loglevel, logtype); }
}

