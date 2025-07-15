using System;
using System.Collections;
using UnityEngine;
using TMPro;

public class Version_Services : MonoBehaviour
{
    public static Version_Services Instance { get; set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Version Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public TextMeshProUGUI Version_Text;
    
    // 可以手动在Unity的Service里面设置Version_Text，比如Insider Preview啥的，如果不填就直接从Unity读了
    private TextMeshProUGUI Display_Text
    {
        get
        {
            if (Version_Text != null)
                return Version_Text;
            
            if (Window_Services.Instance != null && Window_Services.Instance.Handle_Status_Text != null)
                return Window_Services.Instance.Handle_Status_Text;
                
            return null;
        }
    }

    [Header("Version Info")]
    [SerializeField]
    private string version_String;
    [SerializeField]
    private string build_Time_String;
    
    public string Version_String { get { return version_String; } private set { version_String = value; } }
    public string Build_Time_String { get { return build_Time_String; } private set { build_Time_String = value; } }

    private void Start()
    {
        Console_Log("开始初始化 Version Services");
        
        Initialize_Version_Info();
        
        // 延迟更新UI，让Window_Services先初始化
        StartCoroutine(Update_Version_UI_Delayed());
        
        Console_Log("结束初始化 Version Services");
    }
    
    private System.Collections.IEnumerator Update_Version_UI_Delayed()
    {
        // 等好帧
        yield return null;
    
        while (Window_Services.Instance == null)
        {
            yield return null;
        }
        
        Update_Version_UI();
    }

    private void Initialize_Version_Info()
    {
        // 读Unity设置的版本号
        Version_String = Application.version;
        
        // 读构建时间
        Build_Time_String = Get_Build_Time();
        
        Console_Log($"版本信息初始化完成: {Version_String}, 构建时间: {Build_Time_String}");
    }

    private string Get_Build_Time()
    {
        try
        {
            // 获取可执行文件的创建时间（在真的做Release的时候，记得Clean Build，要不然时间会一直不变）
            string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
            if (!string.IsNullOrEmpty(exePath))
            {
                System.IO.FileInfo fileInfo = new System.IO.FileInfo(exePath);
                return fileInfo.CreationTime.ToString("yyyy-MM-dd HH:mm");
            }
        }
        catch (Exception ex)
        {
            Console_Log($"获取构建时间失败: {ex.Message}", Debug_Services.LogLevel.Debug, LogType.Warning);
        }

        // 如果无法获取，返回当前时间
        return DateTime.Now.ToString("yyyy-MM-dd HH:mm");
    }

    public void Update_Version_UI()
    {
        if (Display_Text != null)
        {
            // 如果使用的是Handle_Status_Text，则保留原有的窗口句柄信息，并在顶部添加版本信息
            if (Display_Text == Window_Services.Instance?.Handle_Status_Text)
            {
                string originalText = Display_Text.text;
                string versionInfo = $"v{Version_String} ({Build_Time_String})\n";
                string displayText = versionInfo + originalText;
                Display_Text.SetText(displayText);
            }
            else
            {
                // 使用独立的版本文本组件
                string displayText = $"v{Version_String}\n{Build_Time_String}";
                Display_Text.SetText(displayText);
            }
        }
        else
        {
            Console_Log("Display_Text UI元素未设置", Debug_Services.LogLevel.Debug, LogType.Warning);
        }
    }

    public string Get_Full_Version_Info()
    {
        return $"Shittim Canvas v{Version_String} ({Build_Time_String})";
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) 
    { 
        Debug_Services.Instance.Console_Log("Version_Services", message, loglevel, logtype); 
    }
} 