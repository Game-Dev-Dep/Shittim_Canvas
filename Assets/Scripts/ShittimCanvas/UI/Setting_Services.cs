using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Setting_Services : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField]
    public GameObject Setting_Root_GameObject;
    [SerializeField]
    public Button Setting_General_Option_Button;
    [SerializeField]
    public Button Setting_Audio_Option_Button;
    [SerializeField]
    public Button Setting_Graphic_Option_Button;
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
    public List<Setting_Content> Setting_Content_List;

    void Start()
    {
        Get_Detail_Option_UI_Parameters();

        Setting_General_Option_Button.onClick.AddListener(Toggle_General_Option);
        Setting_Audio_Option_Button.onClick.AddListener(Toggle_Audio_Option);
        Setting_Graphic_Option_Button.onClick.AddListener(Toggle_Graphic_Option);

        Setting_Content_List.Add(new Setting_Content
        {
            Setting_Option_Type = Setting_Option_Type.General,
            Setting_Detail_Option_List = new List<Setting_Detail_Option>
            {
                new Setting_Detail_Option { Name = "Name 01", Description = "Description 01", Setting_Detail_Option_Type = Setting_Detail_Option_Type.Toggle },
                new Setting_Detail_Option { Name = "Name 02", Description = "Description 02", Setting_Detail_Option_Type = Setting_Detail_Option_Type.Input }
            }
        });
    }

    void Toggle_General_Option()
    {
        Cur_Setting_Option_Type = Setting_Option_Type.General;
        Update_Setting_Content_UI();
    }

    void Toggle_Audio_Option()
    {
        Cur_Setting_Option_Type = Setting_Option_Type.Audio;
        Update_Setting_Content_UI();
    }

    void Toggle_Graphic_Option()
    {
        Cur_Setting_Option_Type = Setting_Option_Type.Graphic;
        Update_Setting_Content_UI();
    }

    void Get_Detail_Option_UI_Parameters()
    {
        Setting_Detail_Option_Width = Setting_Detail_Option_Template_GameObject.GetComponent<RectTransform>().sizeDelta.x;
        Setting_Detail_Option_Height = Setting_Detail_Option_Template_GameObject.GetComponent<RectTransform>().sizeDelta.y;
        Setting_Detail_Option_Spacing = Setting_Content_GameObject.GetComponent<VerticalLayoutGroup>().spacing;
    }

    void Update_Setting_Content_UI()
    {
        Clear_Setting_Content_UI();
        foreach (Setting_Content setting_content in Setting_Content_List)
        {
            switch (setting_content.Setting_Option_Type)
            {
                case Setting_Option_Type.General:
                    break;
            }
        }
    }

    void Clear_Setting_Content_UI()
    {
        foreach (Transform child in Setting_Content_GameObject.transform)
        {
            if(child != Setting_Detail_Option_Template_GameObject)  Destroy(child.gameObject);
        }
    }

    public class Setting_Content
    {
        public Setting_Option_Type Setting_Option_Type;
        public List<Setting_Detail_Option> Setting_Detail_Option_List;
    }

    public class Setting_Detail_Option
    {
        public string Name;
        public string Description;
        public Setting_Detail_Option_Type Setting_Detail_Option_Type;
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
}

