using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    //[Header("UI Settings")]

    [Header("Core Variables")]
    public bool is_Setting_On = false;
    public float Setting_Detail_Option_Width;
    public float Setting_Detail_Option_Height;
    public float Setting_Detail_Option_Spacing;
    public Setting_Option_Type Cur_Setting_Option_Type;
    public Dictionary<Setting_Option_Type, List<Setting_Detail_Option>> Setting_Contents = new Dictionary<Setting_Option_Type, List<Setting_Detail_Option>>();

    private Setting_Config setting_config;
    private LocalizedString localizedString = new LocalizedString();
    private int buildVersionClickCount = 0; // 构建版本点击计数器

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
        Toast_Wrapper_Services.ShowToast("toast.settings_saved", 3f);
    }

    private void Save_Setting_Config_Silent()
    {
        Config_Services.Instance.Save_Setting_Config(setting_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
    }

    public void Reset_Setting_Config()
    {
        setting_config = new Setting_Config();
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
                        setting_config.General.Language = value;
                        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
                        Setting_Contents[Setting_Option_Type.General][0].Dropdown_Value = value;
                        // dropdown校验
                        if (Setting_Contents[Setting_Option_Type.General][0].Dropdown_Component != null)
                        {
                            Setting_Contents[Setting_Option_Type.General][0].Dropdown_Component.value = value;
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
                    Title_Key = "settings_panel.general.auto_wallpaper",
                    Description_Key = "settings_panel.general.auto_wallpaper.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Auto_Wallpaper_Mode,
                    ToggleGroup_Options = new List<string> { "settings_panel.elements.yes_radio", "settings_panel.elements.no_radio" },
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Auto_Wallpaper_Mode = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
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
                            setting_config.General.Notification_Enabled = int.Parse(Setting_Contents[Setting_Option_Type.General][3].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][3].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][3].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);

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
                            setting_config.General.Wallpaper_Mode_Status_Area_Enabled = int.Parse(Setting_Contents[Setting_Option_Type.General][4].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][4].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][4].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
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
                    Slider_Callback = (value) => {
                        setting_config.Audio.Global_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][0].Slider_Value = value;
                        Setting_Contents[Setting_Option_Type.Audio][0].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][0].Slider_Text_Max_Value):F0} %";
                        Audio_Services.Instance.Global_Sound_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.voice_volume",
                    Description_Key = "settings_panel.audio.voice_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.Talk_Sound,
                    Slider_Callback = (value) => {
                        setting_config.Audio.Talk_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][1].Slider_Value = value;
                        Setting_Contents[Setting_Option_Type.Audio][1].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][1].Slider_Text_Max_Value):F0} %";
                        Audio_Services.Instance.Talk_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.se_volume",
                    Description_Key = "settings_panel.audio.se_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.SFX_Sound,
                    Slider_Callback = (value) => {
                        setting_config.Audio.SFX_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][2].Slider_Value = value;
                        Setting_Contents[Setting_Option_Type.Audio][2].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][2].Slider_Text_Max_Value):F0} %";
                        Audio_Services.Instance.SFX_Slider_Handler(value);
                    }
                },
                new Setting_Detail_Option
                {
                    Title_Key = "settings_panel.audio.bgm_volume",
                    Description_Key = "settings_panel.audio.bgm_volume.desc",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Audio.BGM_Sound,
                    Slider_Callback = (value) => {
                        setting_config.Audio.BGM_Sound = value;
                        Setting_Contents[Setting_Option_Type.Audio][3].Slider_Value = value;
                        Setting_Contents[Setting_Option_Type.Audio][3].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Audio][3].Slider_Text_Max_Value):F0} %";
                        Audio_Services.Instance.BGM_Slider_Handler(value);
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
                    Slider_Callback = (value) => {
                        setting_config.Graphic.Editor_Mode_UI_Scale = value;
                        GameObject.Find("Canvas").GetComponent<CanvasScaler>().scaleFactor = value;
                        Setting_Contents[Setting_Option_Type.Graphic][2].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][2].Slider_Text_Max_Value):F0} %";
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
                        setting_config.Graphic.Wallpaper_Mode_Framerate = int.Parse(value);
                        Setting_Contents[Setting_Option_Type.Graphic][4].Input_Value = value;
                        Setting_Contents[Setting_Option_Type.Graphic][4].InputField_Component.text = "";
                        (Setting_Contents[Setting_Option_Type.Graphic][4].InputField_Component.placeholder as TMP_Text).text = value;

                        Framerate_Services.Instance.Target_Framerate = int.Parse(value);

                        if (!Framerate_Services.Instance.is_VSync_Mode)
                        {
                            Framerate_Services.Instance.Apply_VSync_Settings();
                        }
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
                    Text_Value = "GitHub: https://github.com/Game-Dev-Dep/Shittim_Canvas"
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
                    Text_Value = setting_config.About.Build_Date
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

        Audio_Services.Instance.Global_Sound_Slider_Handler(setting_config.Audio.Global_Sound);
        Audio_Services.Instance.Talk_Slider_Handler(setting_config.Audio.Talk_Sound);
        Audio_Services.Instance.SFX_Slider_Handler(setting_config.Audio.SFX_Sound);
        Audio_Services.Instance.BGM_Slider_Handler(setting_config.Audio.BGM_Sound);

        Init_Setting_Contents();

        Update_Setting_Content_UI();

        Console_Log("结束初始化 Setting Services");
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
        Save_Setting_Config_Silent();
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
            case Setting_Option_Type.About:
                Create_Setting_Detail_Option_UI(Setting_Contents[Setting_Option_Type.About]);
                break;
        }
    }

    void Create_Setting_Detail_Option_UI(List<Setting_Detail_Option> setting_detail_option_list)
    {
        Setting_Content_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (Setting_Detail_Option_Height + Setting_Detail_Option_Spacing) * setting_detail_option_list.Count);

        foreach (Setting_Detail_Option setting_detail_option in setting_detail_option_list)
        {
            GameObject new_detail_option = Instantiate(Setting_Detail_Option_Template_GameObject, Setting_Content_GameObject.transform);
            setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option;
            new_detail_option.SetActive(true);

            TextMeshProUGUI title_text = new_detail_option.transform.Find("[Setting] Detail Option Title Text").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI description_text = new_detail_option.transform.Find("[Setting] Detail Option Description Text").GetComponent<TextMeshProUGUI>();
            Localization_Utils.Apply_Localization_To_Text(title_text, setting_detail_option.Title_Key);
            Localization_Utils.Apply_Localization_To_Text(description_text, setting_detail_option.Description_Key);

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
                    setting_detail_option.Text_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Slider Text").GetComponent<TextMeshProUGUI>();

                    setting_detail_option.Slider_Component.value = setting_detail_option.Slider_Value;
                    setting_detail_option.Slider_Component.minValue = setting_detail_option.Slider_Min_Value;
                    setting_detail_option.Slider_Component.maxValue = setting_detail_option.Slider_Max_Value;

                    setting_detail_option.Text_Component.text = $"{Value_Map(setting_detail_option.Slider_Component.value, setting_detail_option.Slider_Min_Value, setting_detail_option.Slider_Max_Value, setting_detail_option.Slider_Text_Min_Value, setting_detail_option.Slider_Text_Max_Value):F0} %";

                    if (setting_detail_option.Slider_Callback != null)
                    {
                        setting_detail_option.Slider_Component.onValueChanged.AddListener((value) => setting_detail_option.Slider_Callback(value));
                    }
                    break;

                case Setting_Detail_Option_Type.Input:
                    // 设置Input相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Input Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.InputField_Component = setting_detail_option.Setting_Detail_Option_GameObject.GetComponent<TMP_InputField>();

                    (setting_detail_option.InputField_Component.placeholder as TMP_Text).text = setting_detail_option.Input_Value;

                    if (setting_detail_option.Input_Callback != null)
                    {
                        setting_detail_option.InputField_Component.onEndEdit.AddListener((value) => setting_detail_option.Input_Callback(value));
                    }

                    break;

                case Setting_Detail_Option_Type.Dropdown:
                    // 设置Dropdown相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Dropdown Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Dropdown_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Dropdown").GetComponent<TMP_Dropdown>();

                    setting_detail_option.Dropdown_Component.ClearOptions();
                    // dropdown不用key
                    setting_detail_option.Dropdown_Component.AddOptions(setting_detail_option.Dropdown_Options);

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
        About
    }

    public enum Setting_Detail_Option_Type
    {
        Toggle,
        Slider,
        Input,
        Dropdown,
        Text
    }

    public class Setting_Detail_Option
    {
        public string Title_Key;
        public string Description_Key;
        public Setting_Detail_Option_Type Setting_Detail_Option_Type;
        public GameObject Setting_Detail_Option_GameObject;

        public ToggleGroup ToggleGroup_Component;
        public Slider Slider_Component;
        public TMP_InputField InputField_Component;
        public TMP_Dropdown Dropdown_Component;
        public TextMeshProUGUI Text_Component;

        public int ToggleGroup_Value;
        public float Slider_Value;
        public string Input_Value;
        public int Dropdown_Value;
        public string Text_Value;

        public Action<bool> Toggle_Callback;
        public Action<float> Slider_Callback;
        public Action<string> Input_Callback;
        public Action<int> Dropdown_Callback;
        public Action Text_Click_Callback;

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

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Setting_Services", message, loglevel, logtype); }
}

