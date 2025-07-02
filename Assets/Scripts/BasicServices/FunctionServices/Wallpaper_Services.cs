using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Serialization;

public class Wallpaper_Services : MonoBehaviour
{
    public static Wallpaper_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Wallpaper Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button Wallpaper_Mode_Toggle_Button;
    [SerializeField]
    public Button Auto_Wallpaper_Mode_Toggle_Button;
    [SerializeField]
    public GameObject Wallpaper_Area;
    [SerializeField]
    public GameObject Function_Area;
    [SerializeField]
    public GameObject Status_Area;
    [SerializeField]
    public RawImage Auto_Wallpaper_Mode_On_Icon;
    [SerializeField]
    public RawImage Auto_Wallpaper_Mode_Off_Icon;



    [Header("Core Variables")]
    public bool is_Wallpaper_Mode = false;
    private bool is_Auto_Wallpaper_Mode_On = false;
#if UNITY_EDITOR
    public bool is_Wallpaper_Mode_Editor = false;
#endif

    private void Get_Config()
    {
        is_Auto_Wallpaper_Mode_On = Config_Services.Instance.Global_Function_Config.is_Auto_Wallpaper_Mode_On;
        Update_Auto_Wallpaper_Mode_Button_UI();
    }

    public void Set_Config()
    {
        Config_Services.Instance.Global_Function_Config.is_Auto_Wallpaper_Mode_On = is_Auto_Wallpaper_Mode_On;
    }

    private void Start()
    {
        Console_Log("开始初始化 Wallpaper Services");

        Get_Config();
        Wallpaper_Mode_Toggle_Button.onClick.AddListener(Toggle_Wallpaper_Mode);
        Auto_Wallpaper_Mode_Toggle_Button.onClick.AddListener(Toggle_Auto_Wallpaper_Mode);

#if !UNITY_EDITOR
        if (is_Auto_Wallpaper_Mode_On)
        {
            is_Wallpaper_Mode = false;
            Toggle_Wallpaper_Mode();
        }
#endif

        Console_Log("结束初始化 Wallpaper Services");
    }

    public void Toggle_Wallpaper_Mode()
    {

#if !UNITY_EDITOR
        if (is_Wallpaper_Mode)
        {
            if (Window_Services.Unity_Handle == IntPtr.Zero)
            {
                Console_Log("Unity 窗口句柄无效", Debug_Services.LogLevel.Debug, LogType.Warning);
                return;
            }

            Window_Services.Instance.Cover_Status_Image.gameObject.SetActive(false);
            Window_Services.Instance.Cover_Status_Text.gameObject.SetActive(false);
            Window_Services.Instance.Cur_Cover_Window_Type = Window_Services.Cover_Window_Type.No_Window;

            Quit_Wallpaper_Mode();
        }
        else
        {
            if (Window_Services.WorkerW_Handle == IntPtr.Zero)
            {
                Console_Log("WorkerW 窗口句柄无效，无法进入壁纸模式", Debug_Services.LogLevel.Debug, LogType.Error);
                return;
            }
            else if (Window_Services.Unity_Handle == IntPtr.Zero)
            {
                Console_Log("Unity 窗口句柄无效，无法进入壁纸模式", Debug_Services.LogLevel.Debug, LogType.Error);
                return;
            }

            Window_Services.Instance.Cover_Status_Image.gameObject.SetActive(true);
            Window_Services.Instance.Cover_Status_Text.gameObject.SetActive(true);

            Enter_Wallpaper_Mode();
        }
        Clear_HUD();
#else
        is_Wallpaper_Mode = !is_Wallpaper_Mode;
        is_Wallpaper_Mode_Editor = !is_Wallpaper_Mode_Editor;

        if (is_Wallpaper_Mode_Editor) Console_Log("触发进入壁纸模式");
        else Console_Log("触发退回编辑模式");

        Clear_HUD();
#endif

    }

    public void Enter_Wallpaper_Mode()
    {
        Console_Log("触发进入壁纸模式");

        Console_Log($"设置窗口父子关系: Unity窗口 {Window_Services.Unity_Handle} -> WorkerW {Window_Services.WorkerW_Handle}");
        Win32Wrapper.SetParent(Window_Services.Unity_Handle, Window_Services.WorkerW_Handle);

        Console_Log($"设置壁纸模式分辨率: {Window_Services.Instance.Device_Screen_Width}x{Window_Services.Instance.Device_Screen_Height}");
        Screen.SetResolution(Window_Services.Instance.Device_Screen_Width, Window_Services.Instance.Device_Screen_Height, FullScreenMode.FullScreenWindow);

        is_Wallpaper_Mode = true;

        Console_Log($"壁纸模式最终屏幕分辨率: {Screen.width}x{Screen.height}");
        Console_Log($"壁纸模式最终屏幕模式: {Screen.fullScreenMode}");

        Console_Log("成功进入壁纸模式");
    }

    public void Quit_Wallpaper_Mode()
    {
        Console_Log("触发退回编辑模式");

        Console_Log($"重置窗口父子关系: Unity窗口 {Window_Services.Unity_Handle} -> 桌面");
        Win32Wrapper.SetParent(Window_Services.Unity_Handle, IntPtr.Zero);

        Console_Log($"设置编辑模式分辨率: {Window_Services.Instance.Edit_Mode_Width}x{Window_Services.Instance.Edit_Mode_Height}");
        Screen.SetResolution(Window_Services.Instance.Edit_Mode_Width, Window_Services.Instance.Edit_Mode_Height, FullScreenMode.Windowed);

        is_Wallpaper_Mode = false;

        Console_Log($"编辑模式最终屏幕分辨率: {Screen.width} × {Screen.height}");
        Console_Log($"编辑模式最终屏幕模式: {Screen.fullScreenMode}");

        Console_Log("成功退回编辑模式");
    }

    private void Toggle_Auto_Wallpaper_Mode()
    {
        is_Auto_Wallpaper_Mode_On = !is_Auto_Wallpaper_Mode_On;
        Update_Auto_Wallpaper_Mode_Button_UI();
    }

    public void Update_Auto_Wallpaper_Mode_Button_UI()
    {
        Auto_Wallpaper_Mode_On_Icon.enabled = is_Auto_Wallpaper_Mode_On;
        Auto_Wallpaper_Mode_Off_Icon.enabled = !is_Auto_Wallpaper_Mode_On;
    }

    public void Clear_HUD()
    {
#if UNITY_EDITOR
        bool isEditor = is_Wallpaper_Mode_Editor;
#else
        bool isEditor = false;
#endif
        Wallpaper_Area.SetActive(!isEditor);
        Function_Area.SetActive(!isEditor);
        Status_Area.SetActive(!isEditor);
    }
    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Wallpaper_Services", message, loglevel, logtype); }
}