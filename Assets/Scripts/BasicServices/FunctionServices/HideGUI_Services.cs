using UnityEngine;
using UnityEngine.UI;

public class HideGUI_Services : MonoBehaviour
{
    public static HideGUI_Services Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] HideGUI Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button Hide_GUI_Button;
    [SerializeField]
    public GameObject Hide_GUI_Button_Container; // 用于保留隐藏按钮的容器。其实也可以用在其他的Area上，来实现保留一部分UI区域的功能
    [SerializeField]
    public UnityEngine.UI.RawImage GUI_On_Image;
    [SerializeField]
    public UnityEngine.UI.RawImage GUI_Off_Image;

    [Header("Core Variables")]
    public bool is_GUI_Hidden = false;

    private bool lastWallpaperMode = false;

    private void Start()
    {
        Console_Log("开始初始化 HideGUI Services");
        
        if (Hide_GUI_Button != null)
        {
            Hide_GUI_Button.onClick.AddListener(Toggle_GUI_Visibility);
        }
        
        Console_Log("结束初始化 HideGUI Services");
        Update_GUI_Image();
        lastWallpaperMode = IsWallpaperMode();
    }

    // 壁纸模式下，隐藏按钮跟着一块隐藏的功能
    private void Update()
    {
        bool currentWallpaperMode = IsWallpaperMode();
        if (currentWallpaperMode != lastWallpaperMode)
        {
            lastWallpaperMode = currentWallpaperMode;
            Update_Hide_Button_Visibility();
        }
    }

    private bool IsWallpaperMode()
    {
#if UNITY_EDITOR
        return Wallpaper_Services.Instance != null && Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor;
#else
        return Wallpaper_Services.Instance != null && Wallpaper_Services.Instance.is_Wallpaper_Mode;
#endif
    }

    private void Update_Hide_Button_Visibility()
    {
        if (Hide_GUI_Button_Container == null) return;
        if (IsWallpaperMode())
        {
            // 壁纸模式下，隐藏按钮跟着一块隐藏
            Hide_GUI_Button_Container.SetActive(false);
        }
        else
        {
            // 非壁纸模式下，根据GUI隐藏状态决定
            Hide_GUI_Button_Container.SetActive(true);
        }
    }

    public void Toggle_GUI_Visibility()
    {
        is_GUI_Hidden = !is_GUI_Hidden;
        
        if (is_GUI_Hidden)
        {
            Console_Log("隐藏GUI");
            Hide_GUI();
        }
        else
        {
            Console_Log("显示GUI");
            Show_GUI();
        }
        Update_GUI_Image();
    }

    public void Hide_GUI()
    {
        is_GUI_Hidden = true;
        
        if (Wallpaper_Services.Instance != null)
        {
            // 临时保存当前状态
            bool originalWallpaperMode = Wallpaper_Services.Instance.is_Wallpaper_Mode;
#if UNITY_EDITOR
            bool originalWallpaperModeEditor = Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor;
#endif

            // 设置壁纸模式状态以触发Clear_HUD的隐藏逻辑
#if UNITY_EDITOR
            Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor = true;
#else
            Wallpaper_Services.Instance.is_Wallpaper_Mode = true;
#endif

            // 调用Clear_HUD隐藏其他GUI
            Wallpaper_Services.Instance.Clear_HUD();

            // 恢复原始状态
            Wallpaper_Services.Instance.is_Wallpaper_Mode = originalWallpaperMode;
#if UNITY_EDITOR
            Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor = originalWallpaperModeEditor;
#endif
        }

        // 保持选择的容器保持可见
        if (Hide_GUI_Button_Container != null)
        {
            Hide_GUI_Button_Container.SetActive(true);
        }
        Update_GUI_Image();
        Update_Hide_Button_Visibility();
    }

    public void Show_GUI()
    {
        is_GUI_Hidden = false;
        
        if (Wallpaper_Services.Instance != null)
        {
            bool originalWallpaperMode = Wallpaper_Services.Instance.is_Wallpaper_Mode;
#if UNITY_EDITOR
            bool originalWallpaperModeEditor = Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor;
#endif

#if UNITY_EDITOR
            Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor = false;
#else
            Wallpaper_Services.Instance.is_Wallpaper_Mode = false;
#endif

            Wallpaper_Services.Instance.Clear_HUD();

            //恢复原始状态
            Wallpaper_Services.Instance.is_Wallpaper_Mode = originalWallpaperMode;
#if UNITY_EDITOR
            Wallpaper_Services.Instance.is_Wallpaper_Mode_Editor = originalWallpaperModeEditor;
#endif
        }
        Update_GUI_Image();
        Update_Hide_Button_Visibility();
    }

    private void Update_GUI_Image()
    {
        if (GUI_On_Image != null) GUI_On_Image.enabled = !is_GUI_Hidden;
        if (GUI_Off_Image != null) GUI_Off_Image.enabled = is_GUI_Hidden;
    }

    // 留个接口，以后方便外部调用
    public static void HideAllGUI()
    {
        if (Instance != null)
        {
            Instance.Hide_GUI();
        }
    }

    public static void ShowAllGUI()
    {
        if (Instance != null)
        {
            Instance.Show_GUI();
        }
    }

    public static void ToggleGUI()
    {
        if (Instance != null)
        {
            Instance.Toggle_GUI_Visibility();
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) 
    { 
        Debug_Services.Instance.Console_Log("HideGUI_Services", message, loglevel, logtype); 
    }
} 