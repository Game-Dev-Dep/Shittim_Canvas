using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Win32Wrapper;

public class Window_Services : MonoBehaviour
{
    public static Window_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Window Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button Fullscreen_Mode_Toggle_Button;
    [SerializeField]
    public RawImage Fullscreen_Mode_On_Image;
    [SerializeField]
    public RawImage Fullscreen_Mode_Off_Image;
    [SerializeField]
    public TextMeshProUGUI Handle_Status_Text;
    [SerializeField]
    public Image Cover_Status_Image;
    [SerializeField]
    public TextMeshProUGUI Cover_Status_Text;

    [Header("Resolution Settings")]
    public int Edit_Mode_Width = 1920;
    public int Edit_Mode_Height = 1080;

    [Header("Core Variables")]
    public bool is_Fullscreen_Mode = false;
    public int Device_Screen_Width = 1920;
    public int Device_Screen_Height = 1080;
    public static IntPtr Program_Manager_Handle = IntPtr.Zero;
    public static IntPtr WorkerW_Handle = IntPtr.Zero;
    public static IntPtr Unity_Handle = IntPtr.Zero;

    // ===== 窗口覆盖相关 =====

    [System.Serializable]
    public class Window_Info
    {
        public IntPtr handle;
        public string title;
        public string className;
        public bool isVisible;
        public bool isMaximized;
        public RECT rect;
    }

    // 系统窗口类名过滤列表
    private readonly HashSet<string> systemWindowClasses = new HashSet<string>
    {
        "Shell_TrayWnd",        // 任务栏
        "DV2ControlHost",       // 系统控制
        "MsgrIMEWindowClass",   // 输入法
        "IME",                  // 输入法相关
        "MSCTFIME UI",          // 输入法UI
        "Windows.UI.Core.CoreWindow", // UWP系统窗口
        "ApplicationFrameWindow",     // UWP应用框架
        "Progman",              // 桌面程序管理器
        "WorkerW",              // 桌面工作区
        "Button",               // 系统按钮
        "Static",               // 静态控件
        "#32770",               // 对话框
        "tooltips_class32",     // 工具提示
        "MSTaskSwWClass",       // 任务切换
        "EdgeUiInputTopWndClass", // Edge UI输入
        "NativeHWNDHost",       // 原生窗口宿主
        "ApplicationManager_DesktopShellWindow", // 应用管理器
        "MultitaskingViewFrame", // 多任务视图
        "ForegroundStaging",     // 前台暂存
        //"Chrome_WidgetWin_0",    // Chrome内部窗口（某些情况）
        //"Chrome_WidgetWin_1"     // Chrome内部窗口（某些情况）
    };

    // 系统窗口标题过滤列表
    private readonly HashSet<string> systemWindowTitles = new HashSet<string>
    {
        "",                     // 空标题
        "Default IME",          // 默认输入法
        "MSCTFIME UI",          // 输入法UI
        "Program Manager",      // 程序管理器
        "Desktop"               // 桌面
    };

    public enum Cover_Window_Type
    {
        Maximized_Window,
        Normal_Window,
        No_Window
    }

    [System.Serializable]
    public class Detection_Result
    {
        public Cover_Window_Type cover_window_type;
        public Window_Info top_window_info;
    }

    // ===== =====
    
    private void Start()
    {
        Console_Log("开始初始化 Window Services");

        Fullscreen_Mode_Toggle_Button.onClick.AddListener(Toggle_Fullscreen_Mode);

        Device_Screen_Width = Screen.currentResolution.width;
        Device_Screen_Height = Screen.currentResolution.height;

        Edit_Mode_Width = Device_Screen_Width - 50;
        Edit_Mode_Height = Device_Screen_Height - 200;

        Console_Log($"当前屏幕分辨率: {Device_Screen_Width} × {Device_Screen_Height}");
        Console_Log($"编辑模式分辨率: {Edit_Mode_Width} × {Edit_Mode_Height}");

        Screen.SetResolution(Edit_Mode_Width, Edit_Mode_Height, FullScreenMode.Windowed);
        Console_Log($"当前屏幕模式: {Screen.fullScreenMode}");

        Update_Button_UI();

        if (Get_Program_Manager_Handle())
        {
            Get_WorkW_Handle();
        }
        Get_Unity_Handle();

        Cover_Status_Image.gameObject.SetActive(false);
        Cover_Status_Text.SetText("");
        Cover_Status_Text.gameObject.SetActive(false);

        Handle_Status_Text.SetText(
            $"Program Manager: {Program_Manager_Handle}\n" +
            $"WorkerW: {WorkerW_Handle}\n" +
            $"Shittim Canvas: {Unity_Handle}"
        );

        StartCoroutine(Get_Cover_Window_Coroutine());

        Console_Log("结束初始化 Window Services");
    }

    public void Toggle_Fullscreen_Mode()
    {
        Console_Log("触发切换全屏模式");

        is_Fullscreen_Mode = !is_Fullscreen_Mode;
        if (is_Fullscreen_Mode)
        {
            Console_Log("切换为全屏模式"); 
            Screen.SetResolution(Device_Screen_Width, Device_Screen_Height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            Console_Log("切换为窗口模式");
            Screen.SetResolution(Edit_Mode_Width, Edit_Mode_Height, FullScreenMode.Windowed);
        }

        Update_Button_UI();
    }

    private void Update_Button_UI()
    {
        Fullscreen_Mode_On_Image.enabled = is_Fullscreen_Mode;
        Fullscreen_Mode_Off_Image.enabled = !is_Fullscreen_Mode;
    }

    public bool Get_Program_Manager_Handle()
    {
        Console_Log("开始查找 Progman 窗口句柄");
        Program_Manager_Handle = Win32Wrapper.FindWindow("Progman", null);

        if (Program_Manager_Handle != IntPtr.Zero)
        {
            Console_Log($"找到了 Progman 窗口句柄: {Program_Manager_Handle}");

            Console_Log($"开始发送消息到 Progman 窗口");
            IntPtr result = IntPtr.Zero;
            Win32Wrapper.SendMessageTimeout(Program_Manager_Handle, 0x052C, new IntPtr(0), IntPtr.Zero, Win32Wrapper.SendMessageTimeoutFlags.SMTO_NORMAL, 1000, out result);
            Console_Log($"结束发送消息到 Progman 窗口");
        }
        else
        {
            Console_Log("未找到 Progman 窗口句柄！", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("结束查找 Progman 窗口句柄");
        return Program_Manager_Handle != IntPtr.Zero ? true : false;
    }

    public bool Get_WorkW_Handle()
    {
        Console_Log("开始查找 WorkerW 窗口句柄");

        Console_Log("开始寻找与 SHELLDLL_DefView 同级的 WorkerW 窗口");
        Win32Wrapper.EnumWindows(new Win32Wrapper.EnumWindowsProc((tophandle, topparamhandle) =>
        {
            IntPtr SHELLDLL_DefView_Handle = Win32Wrapper.FindWindowEx(
                                                tophandle,             // 从该顶级窗口开始查找
                                                IntPtr.Zero,           // 从第一个子窗口开始
                                                "SHELLDLL_DefView",    // 类名
                                                IntPtr.Zero            // 无标题限制
                                              );

            if (SHELLDLL_DefView_Handle != IntPtr.Zero)
            {
                Console_Log($"找到了 SHELLDLL_DefView 窗口句柄: {SHELLDLL_DefView_Handle}");

                WorkerW_Handle = Win32Wrapper.FindWindowEx(
                                    IntPtr.Zero,    // 在所有顶级窗口中查找
                                    tophandle,      // 从当前顶级窗口(tophandle)之后开始枚举
                                    "WorkerW",      // 类名
                                    IntPtr.Zero     // 无标题限制
                                  );

                if (WorkerW_Handle != IntPtr.Zero)
                {
                    Console_Log($"找到了与 SHELLDLL_DefView 同级的 WorkerW 窗口句柄: {WorkerW_Handle}");
                    return false;
                }
            }
            return true;
        }), IntPtr.Zero);

        if (WorkerW_Handle == IntPtr.Zero)
        {
            Console_Log("未找到与 SHELLDLL_DefView 同级的 WorkerW 窗口！", Debug_Services.LogLevel.Debug, LogType.Warning);

            Console_Log("开始查找在 Progman 下的 WorkerW 窗口");
            WorkerW_Handle = Win32Wrapper.FindWindowEx(
                                Program_Manager_Handle,    // 从 Progman 窗口开始查找
                                IntPtr.Zero,               // 从第一个子窗口开始
                                "WorkerW",                 // 类名
                                IntPtr.Zero                // 无标题限制
                              );

            if (WorkerW_Handle != IntPtr.Zero)
            {
                Console_Log($"找到了在 Progman 下的 WorkerW 窗口句柄: {WorkerW_Handle}");
            }
            else
            {
                Console_Log("未找到在 Progman 下的 WorkerW 窗口句柄！", Debug_Services.LogLevel.Debug, LogType.Error);
            }
        }

        Console_Log("结束查找 WorkerW 窗口句柄");
        return WorkerW_Handle != IntPtr.Zero ? true : false;
    }

    public bool Get_Unity_Handle()
    {
        Console_Log("开始查找 Unity 窗口句柄");

        Unity_Handle = Win32Wrapper.FindUnityWindow();

        if (Unity_Handle != IntPtr.Zero)
        {
            Console_Log($"找到了 Unity 窗口句柄: {Unity_Handle}");
        }
        else
        {
            Console_Log($"未找到 Unity 窗口句柄", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("结束查找 Unity 窗口句柄");
        return Unity_Handle != IntPtr.Zero ? true : false;
    }

    
    /// <summary>
    /// 主要检测函数
    /// </summary>
    public Detection_Result Get_Cover_Window()
    {
        Detection_Result detection_result = new Detection_Result();

        // 1. 先检查是否有最大化窗口
        Window_Info window_info = Get_Top_Maximized_Window_Info();
        if (window_info != null)
        {
            detection_result.cover_window_type = Cover_Window_Type.Maximized_Window;
            detection_result.top_window_info = window_info;
            return detection_result;
        }

        // 2. 检查WorkerW是否被遮挡
        window_info = Get_WorkerW_Cover_Window_Info();
        if (window_info != null)
        {
            detection_result.cover_window_type = Cover_Window_Type.Normal_Window;
            detection_result.top_window_info = window_info;
            return detection_result;
        }


        detection_result.cover_window_type = Cover_Window_Type.No_Window;
        return detection_result;
    }

    public Cover_Window_Type Cur_Cover_Window_Type = Cover_Window_Type.No_Window;
    public bool is_Switching = false;
    IEnumerator Get_Cover_Window_Coroutine()
    {
        
        Detection_Result last_detection_result = new Detection_Result();
        bool last_is_VSync_Mode = false;
        while (true)
        {
            if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
            {
                if (is_Switching) is_Switching = false;

                Detection_Result cur_detection_result = Get_Cover_Window();

                Cur_Cover_Window_Type = cur_detection_result.cover_window_type;

                Console_Log($"当前窗口覆盖状态: {Cur_Cover_Window_Type} 上一窗口覆盖状态: {last_detection_result.cover_window_type} 切换指示器: {is_Switching}", Debug_Services.LogLevel.Ignore);

                if (cur_detection_result.cover_window_type != last_detection_result.cover_window_type)
                {
                    //Console_Log($"触发窗口切换", Debug_Services.LogLevel.Ignore);

                    is_Switching  = true;

                    switch (cur_detection_result.cover_window_type)
                    {
                        case Cover_Window_Type.Maximized_Window:
                            if (Framerate_Services.Instance.is_VSync_Mode)
                            {
                                Framerate_Services.Instance.Toggle_VSync_Mode();
                                Framerate_Services.Instance.Update_Button_UI();
                                Framerate_Services.Instance.Update_InputField_UI();

                                last_is_VSync_Mode = true;
                            }
                            Framerate_Services.Instance.Set_Target_Framerate("10", true);

                            AudioListener.volume = 0f;
                            
                            Cover_Status_Image.color = new Color32(208, 64, 56, 255);
                            Cover_Status_Text.SetText($"{cur_detection_result.top_window_info.title} {cur_detection_result.top_window_info.handle.ToString("X8")} {cur_detection_result.top_window_info.className}");
                            Console_Log($"WorkerW 被覆盖，覆盖窗口是最大化窗口: {cur_detection_result.top_window_info.title} {cur_detection_result.top_window_info.handle.ToString("X8")}  {cur_detection_result.top_window_info.className}", Debug_Services.LogLevel.Ignore);
                            break;

                        case Cover_Window_Type.Normal_Window:
                            
                            if (last_detection_result.cover_window_type == Cover_Window_Type.Maximized_Window)
                            {
                                if (last_is_VSync_Mode)
                                {
                                    Framerate_Services.Instance.Toggle_VSync_Mode();
                                    Framerate_Services.Instance.Update_Button_UI();
                                    Framerate_Services.Instance.Update_InputField_UI();

                                    last_is_VSync_Mode = false;
                                }
                                else
                                {
                                    if (!Framerate_Services.Instance.is_VSync_Mode)
                                    {
                                        Framerate_Services.Instance.Set_Target_Framerate(Framerate_Services.Instance.Target_Framerate.ToString());
                                    }
                                }
                            }
                            AudioListener.volume = 1f;

                            Cover_Status_Image.color = new Color32(245, 168, 62, 255);
                            Cover_Status_Text.SetText($"{cur_detection_result.top_window_info.title} {cur_detection_result.top_window_info.handle.ToString("X8")} {cur_detection_result.top_window_info.className}");
                            Console_Log($"WorkerW 被覆盖，覆盖窗口是普通窗口: {cur_detection_result.top_window_info.title} {cur_detection_result.top_window_info.handle.ToString("X8")} {cur_detection_result.top_window_info.className}", Debug_Services.LogLevel.Ignore);
                            break;

                        case Cover_Window_Type.No_Window:
                            if (last_detection_result.cover_window_type == Cover_Window_Type.Maximized_Window)
                            {
                                if (last_is_VSync_Mode)
                                {
                                    Framerate_Services.Instance.Toggle_VSync_Mode();
                                    Framerate_Services.Instance.Update_Button_UI();
                                    Framerate_Services.Instance.Update_InputField_UI();

                                    last_is_VSync_Mode = false;
                                }
                                else
                                {
                                    if (!Framerate_Services.Instance.is_VSync_Mode)
                                    {
                                        Framerate_Services.Instance.Set_Target_Framerate(Framerate_Services.Instance.Target_Framerate.ToString());
                                    }
                                }
                            }
                            AudioListener.volume = 1f;

                            Cover_Status_Image.color = new Color32(80, 153, 63, 255);
                            Cover_Status_Text.SetText("");
                            Console_Log($"WorkerW 无覆盖", Debug_Services.LogLevel.Ignore);
                            break;
                    }
                }
                last_detection_result = cur_detection_result;
            }
            else
            {
                if (last_detection_result.cover_window_type == Cover_Window_Type.Maximized_Window && is_Switching)
                {
                    if (last_is_VSync_Mode)
                    {
                        Framerate_Services.Instance.Toggle_VSync_Mode();
                        Framerate_Services.Instance.Update_Button_UI();
                        Framerate_Services.Instance.Update_InputField_UI();

                        last_is_VSync_Mode = false;
                    }
                    else
                    {
                        Framerate_Services.Instance.Set_Target_Framerate(Framerate_Services.Instance.Target_Framerate.ToString());
                    }
                }
                AudioListener.volume = 1f;
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    /// <summary>
    /// 查找最顶层的最大化窗口
    /// </summary>
    private Window_Info Get_Top_Maximized_Window_Info()
    {
        List<Window_Info> all_window_info = Get_All_Window_Info();

        //Console_Log($"找到窗口 {all_window_info.Count} 个");

        foreach (Window_Info window_info in all_window_info)
        {
            //Console_Log($"{window_info.title} {window_info.isVisible} {window_info.isMaximized} {Is_System_Window(window_info)}");
            if (window_info.isVisible && window_info.isMaximized && !Is_System_Window(window_info))
            {
                //Console_Log($"找到最大化窗口: {window_info}");
                return window_info;
            }
        }

        return null;
    }

    /// <summary>
    /// 检查WorkerW是否被遮挡
    /// </summary>
    private Window_Info Get_WorkerW_Cover_Window_Info()
    {
        // 获取所有可见窗口
        List<Window_Info> all_window_info = Get_All_Window_Info();

        foreach (Window_Info window_info in all_window_info)
        {
            if (window_info.isVisible && !Is_System_Window(window_info) && !is_Custom_Window(window_info))
            {
                if (Is_Window_Above_WorkerW(window_info.handle))
                {
                    //Console_Log($"发现 WorkerW 上方的窗口: {window_info.title}");
                    return window_info;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 检查窗口是否在WorkerW之上
    /// </summary>
    private bool Is_Window_Above_WorkerW(IntPtr window_handle)
    {
        IntPtr cur_window_handle = GetTopWindow(IntPtr.Zero);

        while (cur_window_handle != IntPtr.Zero)
        {
            if (cur_window_handle == window_handle)
            {
                // 找到了目标窗口，检查它是否在WorkerW之前出现
                return true;
            }

            if (cur_window_handle == WorkerW_Handle)
            {
                // 先遇到了WorkerW，说明目标窗口在WorkerW之下
                return false;
            }

            cur_window_handle = GetWindow(cur_window_handle, GW_HWNDNEXT);
        }

        return false;
    }

    /// <summary>
    /// 获取所有窗口
    /// </summary>
    private List<Window_Info> Get_All_Window_Info()
    {
        List<Window_Info> all_window_info = new List<Window_Info>();

        EnumWindows((hWnd, lParam) =>
        {
            Window_Info window_info = Get_Window_Info(hWnd);
            if (window_info != null)
            {
                all_window_info.Add(window_info);
            }
            return true;
        }, IntPtr.Zero);

        return all_window_info;
    }

    
    public static bool Is_Window_Minimized(IntPtr hWnd)
    {
        WINDOWPLACEMENT window_placement = new WINDOWPLACEMENT();
        window_placement.length = Marshal.SizeOf(window_placement);

        if (GetWindowPlacement(hWnd, ref window_placement))
        {
            return window_placement.showCmd == SW_SHOWMINIMIZED;
        }

        return false;
    }

    /// <summary>
    /// 获取窗口信息
    /// </summary>
    private Window_Info Get_Window_Info(IntPtr hWnd)
    {
        bool is_visible = !Is_Window_Minimized(hWnd) & IsWindowVisible(hWnd);

       // Console_Log(Get_Window_Title(hWnd) + " is_visible:" + is_visible + " Is_Window_Minimized(hWnd):" + Is_Window_Minimized(hWnd) + " IsWindowVisible(hWnd):" + IsWindowVisible(hWnd));

        if (!is_visible) return null;

        Window_Info window_info = new Window_Info
        {
            handle = hWnd,
            title = Get_Window_Title(hWnd),
            className = Get_Window_Class_Name(hWnd),
            isVisible = is_visible,
            isMaximized = IsZoomed(hWnd)
        };

        return window_info;
    }

    /// <summary>
    /// 判断是否为系统窗口
    /// </summary>
    private bool Is_System_Window(Window_Info window_info)
    {
        // 检查类名
        if (systemWindowClasses.Contains(window_info.className)) return true;

        // 检查标题
        if (systemWindowTitles.Contains(window_info.title)) return true;

        //// 检查窗口大小（过滤掉过小的窗口，通常是系统控件）
        //int window_width = window_info.rect.Right - window_info.rect.Left;
        //int window_height = window_info.rect.Bottom - window_info.rect.Top;
        //if (window_width < 50 || window_height < 50) return true;

        //// 检查是否在屏幕外（系统有时会创建屏幕外的窗口）
        //if (window_info.rect.Left < -1000 || window_info.rect.Top < -1000) return true;

        return false;
    }

    private bool is_Custom_Window(Window_Info window_info)
    {
        if (Config_Services.Instance.Gloabal_WindowFilter_Config.Title_Names.Contains(window_info.title)) return true;

        if (Config_Services.Instance.Gloabal_WindowFilter_Config.Class_Names.Contains(window_info.className)) return true;

        return false;
    }

    /// <summary>
    /// 获取窗口标题
    /// </summary>
    private string Get_Window_Title(IntPtr hWnd)
    {
        StringBuilder title = new StringBuilder(256);
        GetWindowText(hWnd, title, title.Capacity);
        return title.ToString();
    }

    /// <summary>
    /// 获取窗口类名
    /// </summary>
    private string Get_Window_Class_Name(IntPtr hWnd)
    {
        StringBuilder class_name = new StringBuilder(256);
        GetClassName(hWnd, class_name, class_name.Capacity);
        return class_name.ToString();
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Window_Services", message, loglevel, logtype); }
}
