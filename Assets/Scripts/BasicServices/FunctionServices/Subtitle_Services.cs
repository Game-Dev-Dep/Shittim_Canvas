using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Subtitle_Services : MonoBehaviour
{
    public static Subtitle_Services Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Subtitle Services 初始化");
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }



    [Header("UI Elements")]
    [SerializeField]
    public TextMeshProUGUI Subtitle_JP_Text;
    [SerializeField]
    public Button Subtitle_JP_Toggle_Button;
    [SerializeField]
    public RawImage Subtitle_JP_On_Image;
    [SerializeField]
    public RawImage Subtitle_JP_Off_Image;

    [SerializeField]
    public TextMeshProUGUI Subtitle_Custom_Text;
    [SerializeField]
    public Button Subtitle_Custom_Toggle_Button;
    [SerializeField]
    public RawImage Subtitle_Custom_On_Image;
    [SerializeField]
    public RawImage Subtitle_Custom_Off_Image;


    [Header("Subtitle Settings")]
    public float Text_Fade_Duration = 0.25f;

    [Header("Core Variables")]

    public bool is_Stopping_Display = false;
    public bool is_Subtitle_JP_On = true;
    public bool is_Subtitle_Custom_On = true;
    private Coroutine show_subtitle_coroutine;
    private Queue<Subtitle_Request> Subtitle_JP_Request_Queue = new Queue<Subtitle_Request>();
    private Queue<Subtitle_Request> Subtitle_Custom_Request_Queue = new Queue<Subtitle_Request>();
    public struct Subtitle_Request
    {
        public string Text_JP;
        public string Text_Custom;
        public float Text_Duration;
    }

    private void Get_Config()
    {
        is_Subtitle_JP_On = Config_Services.Instance.Global_Function_Config.is_Subtitle_JP_On;
        Update_JP_Button_UI();

        is_Subtitle_Custom_On = Config_Services.Instance.Global_Function_Config.is_Subtitle_Custom_On;
        Update_Custom_Button_UI();
    }

    public void Set_Config()
    {
        Config_Services.Instance.Global_Function_Config.is_Subtitle_JP_On = is_Subtitle_JP_On;
        Config_Services.Instance.Global_Function_Config.is_Subtitle_Custom_On = is_Subtitle_Custom_On;
    }

    public void Start()
    {
        Console_Log("开始初始化 Subtitle Services");

        Get_Config();

        Subtitle_JP_Toggle_Button.onClick.AddListener(Toggle_Subtitle_JP);
        Subtitle_Custom_Toggle_Button.onClick.AddListener(Toggle_Subtitle_Custom);

        Subtitle_JP_Text.text = "";
        Subtitle_JP_Text.alpha = 0;
        Subtitle_Custom_Text.text = "";
        Subtitle_Custom_Text.alpha = 0;

        Update_JP_Button_UI();
        Update_Custom_Button_UI();

        Console_Log("初始化 Subtitle Services");
    }

    public void Update()
    {
        // 显示JP字幕
        if (Subtitle_JP_Request_Queue.Count > 0)
        {
            if (show_subtitle_coroutine == null)
            {
                show_subtitle_coroutine = StartCoroutine(Show_Subtitle_Coroutine(Subtitle_JP_Request_Queue.Dequeue()));
            }
        }

        // 显示自定义字幕 - 如果JP字幕已经显示完毕，则显示自定义字幕
        if (Subtitle_JP_Request_Queue.Count == 0 && Subtitle_Custom_Request_Queue.Count > 0)
        {
            if (show_subtitle_coroutine == null)
            {
                show_subtitle_coroutine = StartCoroutine(Show_Subtitle_Coroutine(Subtitle_Custom_Request_Queue.Dequeue()));
            }
        }
    }

    public void Toggle_Subtitle_JP()
    {
        is_Subtitle_JP_On = !is_Subtitle_JP_On;
        if (!is_Subtitle_JP_On) StartCoroutine(Text_Fade_Out(Subtitle_JP_Text, Text_Fade_Duration));
        else StartCoroutine(Text_Fade_In(Subtitle_JP_Text, Text_Fade_Duration));
        Update_JP_Button_UI();
    }

    public void Update_JP_Button_UI()
    {
        Subtitle_JP_On_Image.enabled = is_Subtitle_JP_On;
        Subtitle_JP_Off_Image.enabled = !is_Subtitle_JP_On;
    }

    public void Toggle_Subtitle_Custom()
    {
        is_Subtitle_Custom_On = !is_Subtitle_Custom_On;
        if (!is_Subtitle_Custom_On) StartCoroutine(Text_Fade_Out(Subtitle_Custom_Text, Text_Fade_Duration));
        else StartCoroutine(Text_Fade_In(Subtitle_Custom_Text, Text_Fade_Duration));
        Update_Custom_Button_UI();
    }

    public void Update_Custom_Button_UI()
    {
        Subtitle_Custom_On_Image.enabled = is_Subtitle_Custom_On;
        Subtitle_Custom_Off_Image.enabled = !is_Subtitle_Custom_On;
    }

    public void Show_Subtitle(Subtitle_Request subtitle_request)
    {
        Console_Log($"显示字幕: {subtitle_request.Text_JP} 自定义字幕: {subtitle_request.Text_Custom} 持续时间: {subtitle_request.Text_Duration}s");
        
        // 如果正在显示字幕，则停止显示
        if (show_subtitle_coroutine != null)
        {
            is_Stopping_Display = true;
            // 等待一帧后重置停止标志
            StartCoroutine(ResetStoppingFlagAfterFrame());
        }
        
        Subtitle_JP_Request_Queue.Enqueue(subtitle_request);
    }

    private IEnumerator ResetStoppingFlagAfterFrame()
    {
        yield return null; // 等待一帧
        is_Stopping_Display = false;
    }

    public IEnumerator Show_Subtitle_Coroutine(Subtitle_Request subtitle_request)
    {
        Console_Log("Show_Subtitle_Coroutine 开始", Debug_Services.LogLevel.Ignore);

        // 如果正在显示字幕，则停止显示
        is_Stopping_Display = false;

        if (Subtitle_JP_Text.alpha != 0) StartCoroutine(Text_Fade_Out(Subtitle_JP_Text, Text_Fade_Duration));
        if (Subtitle_Custom_Text.alpha != 0) StartCoroutine(Text_Fade_Out(Subtitle_Custom_Text, Text_Fade_Duration));

        Subtitle_JP_Text.text = subtitle_request.Text_JP;
        Subtitle_Custom_Text.text = subtitle_request.Text_Custom;

        if (is_Subtitle_JP_On) StartCoroutine(Text_Fade_In(Subtitle_JP_Text, Text_Fade_Duration));
        if (is_Subtitle_Custom_On) StartCoroutine(Text_Fade_In(Subtitle_Custom_Text, Text_Fade_Duration));
        yield return new WaitForSeconds(Text_Fade_Duration);

        // 如果正在停止显示，则停止显示
        if (is_Stopping_Display)
        {
            Console_Log("字幕停止显示", Debug_Services.LogLevel.Ignore);
            show_subtitle_coroutine = null;
            yield break;
        }

        yield return StartCoroutine(Subtitle_Display(subtitle_request.Text_Duration));

        if (is_Subtitle_JP_On) StartCoroutine(Text_Fade_Out(Subtitle_JP_Text, Text_Fade_Duration));
        if (is_Subtitle_Custom_On) StartCoroutine(Text_Fade_Out(Subtitle_Custom_Text, Text_Fade_Duration));
        yield return new WaitForSeconds(Text_Fade_Duration);

        if (is_Stopping_Display) is_Stopping_Display = false;
        show_subtitle_coroutine = null;
        Console_Log("Show_Subtitle_Coroutine 完成", Debug_Services.LogLevel.Ignore);
    }

    private IEnumerator Subtitle_Display(float text_display_duration)
    {
        Console_Log("开始显示字幕", Debug_Services.LogLevel.Ignore);

        float elapsed = 0f;
        while (elapsed < text_display_duration)
        {
            if (is_Stopping_Display)
            {
                Console_Log("字幕停止显示");
                yield break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        Console_Log("字幕显示完成", Debug_Services.LogLevel.Ignore);
    }

    private IEnumerator Text_Fade_In(TextMeshProUGUI text, float text_fade_duration)
    {
        if (text.alpha == 1) yield break;

        Console_Log("开始淡入字幕", Debug_Services.LogLevel.Ignore);

        float elapsed_time = 0f;
        float text_start_alpha = text.alpha;

        while (elapsed_time < text_fade_duration)
        {
            float t = elapsed_time / text_fade_duration;
            text.alpha = Mathf.Lerp(text_start_alpha, 1, t);
            elapsed_time += Time.deltaTime;
            yield return null;
        }

        text.alpha = 1;

        Console_Log("淡入字幕完成", Debug_Services.LogLevel.Ignore);
    }

    private IEnumerator Text_Fade_Out(TextMeshProUGUI text, float text_fade_duration)
    {
        if (text.alpha == 0) yield break;

        Console_Log("开始淡出字幕", Debug_Services.LogLevel.Ignore);

        float elapsed_time = 0f;
        float text_start_alpha = text.alpha;

        while (elapsed_time < text_fade_duration)
        {
            float t = elapsed_time / text_fade_duration;
            text.alpha = Mathf.Lerp(text_start_alpha, 0, t);
            elapsed_time += Time.deltaTime;
            yield return null;
        }

        text.alpha = 0;

        Console_Log("淡出字幕完成", Debug_Services.LogLevel.Ignore);
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Subtitle Services", message, loglevel, logtype); }
}