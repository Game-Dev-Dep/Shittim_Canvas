using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Volume_Services : MonoBehaviour
{
    public static Volume_Services Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Volume Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }



    [Header("UI Elements")]
    [SerializeField]
    public Button Volume_Toggle_Button;
    [SerializeField]
    public RawImage Volume_On_Image;
    [SerializeField]
    public RawImage Volume_Off_Image;
    [SerializeField]
    public Volume Volume_Component;

    [Header("Core Variables")]
    public bool is_Volume_On = true;

    private void Get_Config()
    {
        is_Volume_On = Config_Services.Instance.Global_Function_Config.is_Volume_On;
        Update_Button_UI();
    }

    public void Set_Config()
    {
        Config_Services.Instance.Global_Function_Config.is_Volume_On = is_Volume_On;
    }

    void Start()
    {
        Get_Config();

        Volume_Toggle_Button.onClick.AddListener(Toggle_Volume);
    }

    public void Toggle_Volume()
    {
        is_Volume_On = !is_Volume_On;
        Volume_Component.enabled = is_Volume_On;
        Update_Button_UI();
    }

    public void Update_Button_UI()
    {
        Volume_On_Image.enabled = is_Volume_On;
        Volume_Off_Image.enabled = !is_Volume_On;
    }
}
