using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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
    public GameObject Setting_Content_GameObject;
    [SerializeField]
    public GameObject Setting_Detail_Option_Template_GameObject;

    //[Header("UI Settings")]


    [Header("Core Variables")]
    public float Setting_Detail_Option_Width;
    public float Setting_Detail_Option_Height;
    public float Setting_Detail_Option_Spacing;
    public Setting_Option_Type Cur_Setting_Option_Type;
    public Dictionary<Setting_Option_Type, List<Setting_Detail_Option>> Setting_Contents = new Dictionary<Setting_Option_Type, List<Setting_Detail_Option>>();

    void Start()
    {
        Get_Detail_Option_UI_Parameters();

        Setting_General_Option_Toggle.onValueChanged.AddListener(Toggle_General_Option);
        Setting_Audio_Option_Toggle.onValueChanged.AddListener(Toggle_Audio_Option);
        Setting_Graphic_Option_Toggle.onValueChanged.AddListener(Toggle_Graphic_Option);

        Setting_Contents.Add(
            Setting_Option_Type.General,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title = "Title 01",
                    Description = "Description 01",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle
                },
                new Setting_Detail_Option
                {
                    Title = "Title 02",
                    Description = "Description 02",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Input
                },
                new Setting_Detail_Option
                {
                    Title = "Title 03",
                    Description = "Description 03",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider
                },
                new Setting_Detail_Option
                {
                    Title = "Title 04",
                    Description = "Description 04",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Dropdown
                }
            }
        );

        Setting_Contents.Add(
            Setting_Option_Type.Audio,
            new List<Setting_Detail_Option>()
            {
                new Setting_Detail_Option
                {
                    Title = "语音音量",
                    Description = "调节触发对话时的语音音量",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider,
                    Slider_Callback = (value) => {
                        Debug.Log($"语音音量设置为: {value}");
                    }
                },
                new Setting_Detail_Option
                {
                    Title = "音效音量",
                    Description = "调节播放开场动画时的音效音量",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider
                },
                new Setting_Detail_Option
                {
                    Title = "背景音乐音量",
                    Description = "调节记忆大厅的背景音乐音量",
                    Setting_Detail_Option_Type = Setting_Detail_Option_Type.Slider
                },
            }
        );
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
        }
    }

    void Create_Setting_Detail_Option_UI(List<Setting_Detail_Option> setting_detail_option_list)
    {
        Setting_Content_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (Setting_Detail_Option_Height + Setting_Detail_Option_Spacing) * setting_detail_option_list.Count);

        foreach (Setting_Detail_Option setting_detail_option in setting_detail_option_list)
        {
            GameObject new_detail_option = Instantiate(Setting_Detail_Option_Template_GameObject, Setting_Content_GameObject.transform);
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
                    GameObject Slider_Group_GameObject = new_detail_option.transform.Find("[Setting] Detail Option Slider Group").gameObject;
                    Slider_Group_GameObject.SetActive(true);
                    Slider slider = Slider_Group_GameObject.transform.Find("[Setting] Detail Option Slider").GetComponent<Slider>();
                    if (setting_detail_option.Slider_Callback != null)
                    {
                        slider.onValueChanged.AddListener((value) => setting_detail_option.Slider_Callback(value));
                    }
                    break;
                case Setting_Detail_Option_Type.Input:
                    // 设置Input相关UI
                    new_detail_option.transform.Find("[Setting] Detail Option Input Group").gameObject.SetActive(true);
                    break;
                case Setting_Detail_Option_Type.Dropdown:
                    // 设置Dropdown相关UI
                    new_detail_option.transform.Find("[Setting] Detail Option Dropdown Group").gameObject.SetActive(true);
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
        Graphic
    }

    public enum Setting_Detail_Option_Type
    {
        Toggle,
        Slider,
        Input,
        Dropdown
    }

    public class Setting_Detail_Option
    {
        public string Title;
        public string Description;
        public Setting_Detail_Option_Type Setting_Detail_Option_Type;

        // 回调函数容器
        public Action<bool> Toggle_Callback;
        public Action<float> Slider_Callback;
        public Action<string> Input_Callback;
        public Action<int> Dropdown_Callback;

        public float Slider_Min = 0;
        public float Slider_Max = 1;
        public List<string> DropdownOptions;
    }
}

