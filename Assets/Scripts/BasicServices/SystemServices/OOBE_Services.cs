using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class OOBE_Services : MonoBehaviour
{
    public static OOBE_Services Instance { get; set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] OOBE Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public GameObject OOBE_Root_GameObject;
    [SerializeField]
    public TMP_Dropdown Language_Dropdown;
    [SerializeField]
    public Button Discord_Button;
    [SerializeField]
    public Button QQ_Button;
    [SerializeField]
    public Button Confirm_Button;

    [Header("Core Variables")]
    public bool is_OOBE_On = false;

    private Setting_Config setting_config;

    private void Start()
    {
        // 延迟检查OOBE状态，保证Config_Services已经完全初始化
        StartCoroutine(Check_OOBE_Status_Coroutine());
    }

    private System.Collections.IEnumerator Check_OOBE_Status_Coroutine()
    {
        // 等好帧
        yield return null;

        // 配置文件读取的等好帧
        yield return new WaitForSeconds(0.1f);
        setting_config = Config_Services.Instance.Global_Setting_Config;

        // 检查是否完成OOBE
        if (!setting_config.General.OOBE_Completed)
        {
            Show_OOBE();
        }
    }

    public void Show_OOBE()
    {
        is_OOBE_On = true;
        OOBE_Root_GameObject.SetActive(true);

        // 初始化语言选择dropdown
        Initialize_Language_Dropdown();

        // 设置按钮事件
        Setup_Button_Events();
    }

    public void Hide_OOBE()
    {
        is_OOBE_On = false;
        OOBE_Root_GameObject.SetActive(false);
    }

    private void Initialize_Language_Dropdown()
    {
        if (Language_Dropdown == null) return;

        if (setting_config.General.Language_List.Count != LocalizationSettings.AvailableLocales.Locales.Count)
        {
            Debug.LogWarning($"语言列表数量 ({setting_config.General.Language_List.Count}) 与可用语言数量 ({LocalizationSettings.AvailableLocales.Locales.Count}) 不一致");
        }

        Language_Dropdown.ClearOptions();
        Language_Dropdown.AddOptions(setting_config.General.Language_List);

        if (setting_config.General.Language >= 0 && setting_config.General.Language < setting_config.General.Language_List.Count)
        {
            Language_Dropdown.value = setting_config.General.Language;
        }
        else
        {
            Language_Dropdown.value = 0;
            setting_config.General.Language = 0;
        }
        
        Language_Dropdown.onValueChanged.AddListener(On_Language_Changed);
    }

    private void Setup_Button_Events()
    {
        if (Discord_Button != null)
            Discord_Button.onClick.AddListener(Open_Discord);

        if (QQ_Button != null)
            QQ_Button.onClick.AddListener(Open_QQ);

        if (Confirm_Button != null)
            Confirm_Button.onClick.AddListener(Confirm_OOBE);
    }

    private void On_Language_Changed(int value)
    {
        if (value >= 0 && value < LocalizationSettings.AvailableLocales.Locales.Count)
        {
            setting_config.General.Language = value;
            Config_Services.Instance.Global_Setting_Config.General.Language = value;
            LocalizationSettings.SelectedLocale = LocalizationSettings.AvailableLocales.Locales[value];
            
            Config_Services.Instance.Save_Setting_Config(setting_config, 
                System.IO.Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));
        }
        else
        {
            Debug.LogWarning($"无效的语言索引: {value}, 可用语言数量: {LocalizationSettings.AvailableLocales.Locales.Count}");
        }
    }

    private void Open_Discord()
    {
        Application.OpenURL("https://discord.gg/n8ZVQ2asHC");
    }

    private void Open_QQ()
    {
        Application.OpenURL("https://qm.qq.com/q/KfNhBNW00A");
    }

    private void Confirm_OOBE()
    {
        setting_config.General.Language = Language_Dropdown.value;
        Config_Services.Instance.Global_Setting_Config.General.Language = setting_config.General.Language;
        Config_Services.Instance.Global_Setting_Config.General.OOBE_Completed = true;
        setting_config.General.OOBE_Completed = true;

        Config_Services.Instance.Save_Setting_Config(setting_config, 
            System.IO.Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json"));

        Hide_OOBE();
    }


} 