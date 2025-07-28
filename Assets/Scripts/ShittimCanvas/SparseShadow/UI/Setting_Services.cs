using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
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

    //[Header("UI Settings")]

    [Header("Core Variables")]
    public bool is_Setting_On = false;
    public float Setting_Detail_Option_Width;
    public float Setting_Detail_Option_Height;
    public float Setting_Detail_Option_Spacing;
    public Setting_Option_Type Cur_Setting_Option_Type;
    public Dictionary<Setting_Option_Type, List<Setting_Detail_Option>> Setting_Contents = new Dictionary<Setting_Option_Type, List<Setting_Detail_Option>>();

    private Setting_Config setting_config;

    private void Get_Setting_Config()
    {
        setting_config = Config_Services.Instance.Global_Setting_Config;

        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[setting_config.General.Language];

        setting_config.About.Version = Version_Services.Instance.Version_String;
        setting_config.About.Build_Date = Version_Services.Instance.Build_Time_String;
    }

    public void Save_Setting_Config()
    {
        Config_Services.Instance.Save_Global_Config(setting_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
    }

    void Start()
    {
        Console_Log("开始初始化 Setting Services");

        Get_Detail_Option_UI_Parameters();

        Setting_Toggle_Button.onClick.AddListener(Toggle_Setting_Panel);
        Setting_Off_Button.onClick.AddListener(Hide_Setting_Panel);
        Setting_Save_Button.onClick.AddListener(Save_Setting_Config);

        Setting_General_Option_Toggle.onValueChanged.AddListener(Toggle_General_Option);
        Setting_Audio_Option_Toggle.onValueChanged.AddListener(Toggle_Audio_Option);
        Setting_Graphic_Option_Toggle.onValueChanged.AddListener(Toggle_Graphic_Option);
        Setting_About_Option_Toggle.onValueChanged.AddListener(Toggle_About_Option);

        Get_Setting_Config();

        Setting_Contents.Add(
            Setting_Option_Type.General,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title = "语言",
                    Description = "选择 Shittim Canvas 的语言",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown,
                    Dropdown_Value = setting_config.General.Language,
                    Dropdown_Options = setting_config.General.Language_List,
                    Dropdown_Callback = (value) => {
                        setting_config.General.Language = value;
                        LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
                        Setting_Contents[Setting_Option_Type.General][0].Dropdown_Value = value;
                    }
                },
                new Setting_Detail_Option
                {
                    Title = "开机自启动",
                    Description = "设置 Shittim Canvas 是否在开机时自动启动",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Auto_Startup,
                    ToggleGroup_Options = setting_config.General.Auto_Startup_List,
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
                    Title = "启动进入壁纸模式",
                    Description = "设置 Shittim Canvas 是否在启动时自动进入壁纸模式",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.General.Auto_Wallpaper_Mode,
                    ToggleGroup_Options = setting_config.General.Auto_Wallpaper_Mode_List,
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.General.Auto_Wallpaper_Mode = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
                            Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Value = int.Parse(Setting_Contents[Setting_Option_Type.General][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
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
                    Title = "总音量",
                    Description = "调节总的音量",
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
                    Title = "语音音量",
                    Description = "调节触发对话时的语音音量",
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
                    Title = "音效音量",
                    Description = "调节播放开场动画时的音效音量",
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
                    Title = "背景音乐音量",
                    Description = "调节记忆大厅的背景音乐音量",
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
                    Title = "编辑模式分辨率",
                    Description = "在编辑模式下的窗口分辨率 (请使用 '宽度x高度' 的格式，例如 '1920x1080')",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Input,
                    Input_Value = setting_config.Graphic.Editor_Mode_Resolution_Width + "x" + setting_config.Graphic.Editor_Mode_Resolution_Height,
                    Input_Callback = (value) => {
                        string[] resolution = value.Split('x');
                        if (resolution.Length == 2 && int.TryParse(resolution[0], out int width) && int.TryParse(resolution[1], out int height))
                        {
                            setting_config.Graphic.Editor_Mode_Resolution_Width = width;
                            setting_config.Graphic.Editor_Mode_Resolution_Height = height;
                            Setting_Contents[Setting_Option_Type.Graphic][0].Input_Value = $"{width}x{height}";
                            Setting_Contents[Setting_Option_Type.Graphic][0].InputField_Component.text = $"";
                            (Setting_Contents[Setting_Option_Type.Graphic][0].InputField_Component.placeholder as TMP_Text).text = $"{width}x{height}";

                            Window_Services.Instance.Edit_Mode_Height = height;
                            Window_Services.Instance.Edit_Mode_Width = width;

                            Screen.SetResolution(width, height, FullScreenMode.Windowed);
                        }
                        else
                        {
                            Console_Log("无效的分辨率格式");
                        }
                    }
                },
                new Setting_Detail_Option
                {
                    Title = "编辑模式 UI 缩放比例",
                    Description = "在壁纸模式下的窗口分辨率",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Value = setting_config.Graphic.Editor_Mode_UI_Scale,
                    Slider_Min_Value = 0.5f,
                    Slider_Max_Value = 2.5f,
                    Slider_Callback = (value) => {
                        setting_config.Graphic.Editor_Mode_UI_Scale = value;
                        GameObject.Find("Canvas").GetComponent<CanvasScaler>().scaleFactor = value;
                        Setting_Contents[Setting_Option_Type.Graphic][1].Text_Component.text = $"{Value_Map(value, Setting_Contents[Setting_Option_Type.Graphic][1].Slider_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][1].Slider_Max_Value, Setting_Contents[Setting_Option_Type.Graphic][1].Slider_Text_Min_Value, Setting_Contents[Setting_Option_Type.Graphic][1].Slider_Text_Max_Value):F0} %";
                    }
                },
                new Setting_Detail_Option
                {
                    Title = "壁纸模式显示策略",
                    Description = "在壁纸模式下的显示策略",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle,
                    ToggleGroup_Value = setting_config.Graphic.Wallpaper_Mode_Refresh_Type,
                    ToggleGroup_Options = setting_config.Graphic.Wallpaper_Mode_Refresh_Type_List,
                    Toggle_Callback = (value) => {
                        if (value)
                        {
                            setting_config.Graphic.Wallpaper_Mode_Refresh_Type = int.Parse(Setting_Contents[Setting_Option_Type.Graphic][2].ToggleGroup_Component.ActiveToggles().FirstOrDefault().name);
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
                    Title = "壁纸模式锁帧帧率",
                    Description = "壁纸模式下锁帧时的最大帧率",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Input,
                    Input_Value = setting_config.Graphic.Wallpaper_Mode_Framerate.ToString(),
                    Input_Callback = (value) => {
                        setting_config.Graphic.Wallpaper_Mode_Framerate = int.Parse(value);
                        Setting_Contents[Setting_Option_Type.Graphic][3].Input_Value = value;
                        Setting_Contents[Setting_Option_Type.Graphic][3].InputField_Component.text = "";
                        (Setting_Contents[Setting_Option_Type.Graphic][3].InputField_Component.placeholder as TMP_Text).text = value;

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
                    Title = "Shittim Canvas",
                    Description = "Developed By GDD.",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = ""
                },
                new Setting_Detail_Option
                {
                    Title = "构建版本",
                    Description = "Shittim Canvas 的构建版本",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = setting_config.About.Version
                },
                new Setting_Detail_Option
                {
                    Title = "构建日期",
                    Description = "Shittim Canvas 的构建版本",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Text,
                    Text_Value = setting_config.About.Build_Date
                }
            }
        );

        Update_Setting_Content_UI();

        Console_Log("结束初始化 Setting Services");
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
        Save_Setting_Config();
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

            // 设置标题和描述
            TextMeshProUGUI title_text = new_detail_option.transform.Find("[Setting] Detail Option Title Text").GetComponent<TextMeshProUGUI>();
            title_text.text = setting_detail_option.Title;
            TextMeshProUGUI description_text = new_detail_option.transform.Find("[Setting] Detail Option Description Text").GetComponent<TextMeshProUGUI>();
            description_text.text = setting_detail_option.Description;

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
                    Console_Log(Toggle_Index.ToString());

                    for (int i = 0; i < Toggle_Num; i++)
                    {
                        GameObject toggle_option = Instantiate(setting_detail_option.Setting_Detail_Option_GameObject.transform.Find($"[Setting] Detail Option Toggle Template").gameObject, setting_detail_option.Setting_Detail_Option_GameObject.transform);
                        toggle_option.SetActive(true);

                        toggle_option.gameObject.name = i.ToString();

                        Toggle toggle_component = toggle_option.GetComponent<Toggle>();

                        TextMeshProUGUI toggle_text = toggle_option.transform.Find("[Setting] Detail Option Text").GetComponent<TextMeshProUGUI>();
                        toggle_text.text = setting_detail_option.ToggleGroup_Options[i];

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

                    setting_detail_option.Dropdown_Component.value = setting_detail_option.Dropdown_Value;

                    setting_detail_option.Dropdown_Component.ClearOptions();
                    setting_detail_option.Dropdown_Component.AddOptions(setting_detail_option.Dropdown_Options);
                    
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
        public string Title;
        public string Description;
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

        public List<string> ToggleGroup_Options;
        public float Slider_Min_Value = 0;
        public float Slider_Max_Value = 1;
        public float Slider_Text_Min_Value = 0;
        public float Slider_Text_Max_Value = 100;
        public List<string> Dropdown_Options;
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Setting_Services", message, loglevel, logtype); }
}

