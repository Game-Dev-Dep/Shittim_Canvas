using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Xml.Serialization;
using Utils;

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

    // VSync设置备份
    private bool saved_VSync_Mode = true;
    private int saved_Target_Framerate = 120;

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

        // 保存当前VSync设置
        if (Framerate_Services.Instance != null)
        {
            saved_VSync_Mode = Framerate_Services.Instance.is_VSync_Mode;
            saved_Target_Framerate = Framerate_Services.Instance.Target_Framerate;
            Console_Log($"保存VSync设置: VSync={saved_VSync_Mode}, 目标帧率={saved_Target_Framerate}");
        }

        // 1. 重新获取Unity窗口句柄
        IntPtr unityHandle = Win32Wrapper.FindUnityWindow();
        if (unityHandle == IntPtr.Zero)
        {
            Console_Log("未能获取Unity窗口句柄", Debug_Services.LogLevel.Debug, LogType.Error);
            return;
        }
        Window_Services.Unity_Handle = unityHandle;

        // 2. 如果窗口被最小化，先还原
        Win32Wrapper.ShowWindow(unityHandle, Win32Wrapper.SW_RESTORE);

        // 3. 先置顶再沉底
        Win32Wrapper.SetWindowPos(
            unityHandle,
            Win32Wrapper.HWND_TOPMOST,
            0, 0, 0, 0,
            Win32Wrapper.SetWindowPosFlags.NoMove |
            Win32Wrapper.SetWindowPosFlags.NoSize |
            Win32Wrapper.SetWindowPosFlags.NoActivate |
            Win32Wrapper.SetWindowPosFlags.ShowWindow
        );
        Win32Wrapper.SetWindowPos(
            unityHandle,
            Win32Wrapper.HWND_BOTTOM,
            0, 0, 0, 0,
            Win32Wrapper.SetWindowPosFlags.NoMove |
            Win32Wrapper.SetWindowPosFlags.NoSize |
            Win32Wrapper.SetWindowPosFlags.NoActivate |
            Win32Wrapper.SetWindowPosFlags.ShowWindow
        );

        // 4. 设置父窗口
        Console_Log($"设置窗口父子关系: Unity窗口 {unityHandle} -> WorkerW {Window_Services.WorkerW_Handle}");
        Win32Wrapper.SetParent(unityHandle, Window_Services.WorkerW_Handle);

        // 5. 再次强制显示
        Win32Wrapper.ShowWindow(unityHandle, Win32Wrapper.SW_SHOW);

        Console_Log($"设置壁纸模式分辨率: {Window_Services.Instance.Device_Screen_Width}x{Window_Services.Instance.Device_Screen_Height}");
        Screen.SetResolution(Window_Services.Instance.Device_Screen_Width, Window_Services.Instance.Device_Screen_Height, FullScreenMode.FullScreenWindow);

        is_Wallpaper_Mode = true;

        Console_Log($"壁纸模式最终屏幕分辨率: {Screen.width}x{Screen.height}");
        Console_Log($"壁纸模式最终屏幕模式: {Screen.fullScreenMode}");

        Console_Log("成功进入壁纸模式");

        Notification_Services.Instance.Send_Notifiction("已进入壁纸模式，右键托盘图标可返回正常模式或退出软件");
    }

    public void Quit_Wallpaper_Mode()
    {
        Console_Log("触发退回编辑模式");

        Console_Log($"重置窗口父子关系: Unity窗口 {Window_Services.Unity_Handle} -> 桌面");
        Win32Wrapper.SetParent(Window_Services.Unity_Handle, IntPtr.Zero);

        // 兼容桌面美化软件：强制显示
        Win32Wrapper.ShowWindow(Window_Services.Unity_Handle, Win32Wrapper.SW_SHOW);

        Console_Log($"设置编辑模式分辨率: {Window_Services.Instance.Edit_Mode_Width}x{Window_Services.Instance.Edit_Mode_Height}");
        Screen.SetResolution(Window_Services.Instance.Edit_Mode_Width, Window_Services.Instance.Edit_Mode_Height, FullScreenMode.Windowed);

        is_Wallpaper_Mode = false;

        Console_Log($"编辑模式最终屏幕分辨率: {Screen.width} × {Screen.height}");
        Console_Log($"编辑模式最终屏幕模式: {Screen.fullScreenMode}");

        // 从壁纸模式回正常模式恢复VSync设置
        if (Framerate_Services.Instance != null)
        {
            Console_Log($"恢复VSync设置: VSync={saved_VSync_Mode}, 目标帧率={saved_Target_Framerate}");
            Framerate_Services.Instance.is_VSync_Mode = saved_VSync_Mode;
            Framerate_Services.Instance.Target_Framerate = saved_Target_Framerate;
            Framerate_Services.Instance.Apply_VSync_Settings();
            Framerate_Services.Instance.Update_Button_UI();
        }

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
        Wallpaper_Area.SetActive(!is_Wallpaper_Mode_Editor);
        Function_Area.SetActive(!is_Wallpaper_Mode_Editor);
        Status_Area.SetActive(!is_Wallpaper_Mode_Editor);
#else
        Wallpaper_Area.SetActive(!is_Wallpaper_Mode);
        Function_Area.SetActive(!is_Wallpaper_Mode);
        Status_Area.SetActive(!is_Wallpaper_Mode);
#endif
        
        // 壁纸模式下隐藏Tooltip，但是保留了窗口模式隐藏GUI的Tooltip
        var tooltipService = GameObject.Find("Tool Tip Service");
        if (tooltipService != null)
        {
            tooltipService.SendMessage("HideTooltip", SendMessageOptions.DontRequireReceiver);
        }
        
        // 直接控制Tooltip区域的显示状态（之前有些情况还是隐藏不了，所以暴力一点）
        var tooltipArea = GameObject.Find("Tool Tips Area");
        if (tooltipArea != null)
        {
#if UNITY_EDITOR
            tooltipArea.SetActive(!is_Wallpaper_Mode_Editor);
#else
            tooltipArea.SetActive(!is_Wallpaper_Mode);
#endif
        }
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Wallpaper_Services", message, loglevel, logtype); }
}
