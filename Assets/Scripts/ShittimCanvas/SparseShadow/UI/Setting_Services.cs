using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
    }

    public void Save_Setting_Config()
    {

        setting_config.Audio.Global_Sound = Audio_Services.Instance.Global_Sound;
        setting_config.Audio.Talk_Sound = Audio_Services.Instance.Talk_Sound;
        setting_config.Audio.SFX_Sound = Audio_Services.Instance.SFX_Sound;
        setting_config.Audio.BGM_Sound = Audio_Services.Instance.BGM_Sound;

        Config_Services.Instance.Save_Global_Config(setting_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
    }

    void Start()
    {
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
                        Save_Setting_Config();
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
                        Audio_Services.Instance.BGM_Slider_Handler(value);
                    }
                },
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

    void Init_Setting_Content()
    {

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
                    new_detail_option.transform.Find("[Setting] Detail Option Toggle Group").gameObject.SetActive(true);
                    break;
                case Setting_Detail_Option_Type.Slider:
                    // 设置Slider相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Slider Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Slider_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Slider").GetComponent<Slider>();
                    setting_detail_option.Slider_Component.value = setting_detail_option.Slider_Value;
                    setting_detail_option.Slider_Component.minValue = setting_detail_option.Slider_Min_Value;
                    setting_detail_option.Slider_Component.maxValue = setting_detail_option.Slider_Max_Value;
                    if (setting_detail_option.Slider_Callback != null)
                    {
                        setting_detail_option.Slider_Component.onValueChanged.AddListener((value) => setting_detail_option.Slider_Callback(value));
                    }
                    break;
                case Setting_Detail_Option_Type.Input:
                    // 设置Input相关UI
                    new_detail_option.transform.Find("[Setting] Detail Option Input Group").gameObject.SetActive(true);
                    break;
                case Setting_Detail_Option_Type.Dropdown:
                    // 设置Dropdown相关UI
                    setting_detail_option.Setting_Detail_Option_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Dropdown Group").gameObject;
                    setting_detail_option.Setting_Detail_Option_GameObject.SetActive(true);
                    setting_detail_option.Dropdown_Component = setting_detail_option.Setting_Detail_Option_GameObject.transform.Find("[Setting] Detail Option Dropdown").GetComponent<TMP_Dropdown>();
                    setting_detail_option.Dropdown_Component.ClearOptions();
                    setting_detail_option.Dropdown_Component.AddOptions(setting_detail_option.Dropdown_Options);
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
        public InputField InputField_Component;
        public TMP_Dropdown Dropdown_Component;
        public TextMeshProUGUI Text_Component;

        public int ToggleGroup_Value;
        public float Slider_Value;
        public string Input_Value;
        public int Dropdown_Value;
        public string Text_Value;

        public Action<bool> ToggleGroup_Callback;
        public Action<float> Slider_Callback;
        public Action<string> Input_Callback;
        public Action<int> Dropdown_Callback;

        public float Slider_Min_Value = 0;
        public float Slider_Max_Value = 1;
        public List<string> Dropdown_Options;
    }
}

