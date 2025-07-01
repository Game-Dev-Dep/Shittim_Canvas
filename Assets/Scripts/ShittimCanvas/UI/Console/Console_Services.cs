using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Console_Services : MonoBehaviour
{
    [Header("UI References")]
    public TMP_Text consoleOutput;
    public TMP_InputField searchInput;
    public ScrollRect scrollRect;
    public Button clearButton;
    public Button toggleAutoScrollButton;
    public Button copyToClipboardButton;

    [Header("Log Type Toggles")]
    public Toggle infoToggle;
    public Toggle warningToggle;
    public Toggle errorToggle;

    [Header("Console Panel")]
    public GameObject consolePanel;

    [Header("Settings")]
    public int maxLines = 200;
    public bool autoScroll = true;
    public bool requireCheatCode = true;

    private readonly List<string> allLogs = new List<string>();
    private readonly List<string> filteredLogs = new List<string>();
    private string searchFilter = "";
    private bool consoleVisible = false;

    private KeyCode[] konamiCode = new KeyCode[] {
        /*
        KeyCode.UpArrow, KeyCode.UpArrow,
        KeyCode.DownArrow, KeyCode.DownArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.LeftArrow, KeyCode.RightArrow,
        KeyCode.B, KeyCode.A, KeyCode.B, KeyCode.A
        */
        KeyCode.O, KeyCode.N, KeyCode.I, KeyCode.K, KeyCode.A, KeyCode.T, KeyCode.A, KeyCode.K, KeyCode.A, KeyCode.Y, KeyCode.O, KeyCode.K, KeyCode.O
    };
    private int konamiIndex = 0;
    private float lastKeyTime = 0f;
    private const float keyTimeout = 2f;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void Start()
    {
        // 设置按钮事件
        clearButton.onClick.AddListener(ClearConsole);
        toggleAutoScrollButton.onClick.AddListener(ToggleAutoScroll);
        copyToClipboardButton.onClick.AddListener(CopyToClipboard);

        // 设置搜索框事件
        searchInput.onValueChanged.AddListener(OnSearchFilterChanged);

        // 设置日志类型切换事件
        infoToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });
        warningToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });
        errorToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });

        // 初始状态
        if (Debug_Services.Instance.is_Debug)
        {
            consolePanel.SetActive(true);
            consoleVisible = true;
        }
        else
        {
            consolePanel.SetActive(false);
            consoleVisible = false;
        }
            
        toggleAutoScrollButton.GetComponentInChildren<TMP_Text>().text =
            autoScroll ? "Auto Scroll: ON" : "Auto Scroll: OFF";
    }

    private void Update()
    {
        // 检测作弊码输入
        if (!consoleVisible && requireCheatCode)
        {
            DetectKonamiCode();
        }

        // 按ESC键关闭控制台
        if (consoleVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleConsole();
        }
    }

    private void DetectKonamiCode()
    {
        // 检查按键超时
        if (Time.time - lastKeyTime > keyTimeout)
        {
            konamiIndex = 0;
        }

        // 检查按键序列
        if (Input.anyKeyDown)
        {
            if (konamiIndex < konamiCode.Length && Input.GetKeyDown(konamiCode[konamiIndex]))
            {
                konamiIndex++;
                lastKeyTime = Time.time;

                // 作弊码完成
                if (konamiIndex == konamiCode.Length)
                {
                    ToggleConsole();
                    konamiIndex = 0;
                }
            }
            else
            {
                // 错误的按键，重置序列
                konamiIndex = 0;
            }
        }
    }

    private void ToggleConsole()
    {
        consoleVisible = !consoleVisible;
        consolePanel.SetActive(consoleVisible);

        if (consoleVisible)
        {
            UpdateFilteredLogs();
        }
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // 如果控制台不可见，不处理日志（可选）
        // if (!consoleVisible) return;

        // 根据日志类型添加前缀
        string prefix = "";
        switch (type)
        {
            case LogType.Log:
                prefix = "<color=#00FF00>[INFO]</color> ";
                break;
            case LogType.Warning:
                prefix = "<color=#FFFF00>[WARN]</color> ";
                break;
            case LogType.Error:
            case LogType.Exception:
            case LogType.Assert:
                prefix = "<color=#FF0000>[ERROR]</color> ";
                break;
        }

        // 添加时间戳
        string timestamp = System.DateTime.Now.ToString("[HH:mm:ss] ");
        string formattedLog = timestamp + prefix + logString;

        // 添加日志
        allLogs.Add(formattedLog);

        // 限制日志数量
        if (allLogs.Count > maxLines)
        {
            allLogs.RemoveAt(0);
        }

        // 更新显示
        if (consoleVisible)
        {
            UpdateFilteredLogs();
        }
    }

    private void UpdateFilteredLogs()
    {
        filteredLogs.Clear();

        foreach (string log in allLogs)
        {
            // 应用搜索过滤
            if (!string.IsNullOrEmpty(searchFilter)
                && !log.ToLower().Contains(searchFilter.ToLower()))
            {
                continue;
            }

            // 应用日志类型过滤
            if (log.Contains("[INFO]") && !infoToggle.isOn) continue;
            if (log.Contains("[WARN]") && !warningToggle.isOn) continue;
            if (log.Contains("[ERROR]") && !errorToggle.isOn) continue;

            filteredLogs.Add(log);
        }

        UpdateConsoleDisplay();
    }

    private void UpdateConsoleDisplay()
    {
        consoleOutput.text = string.Join("\n", filteredLogs);

        // 自动滚动到底部 - 使用协程确保在UI更新后执行
        if (autoScroll)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        // 等待一帧确保UI更新完成
        yield return new WaitForEndOfFrame();

        // 强制更新Canvas
        Canvas.ForceUpdateCanvases();

        // 滚动到底部
        scrollRect.verticalNormalizedPosition = 0f;

        // 再次强制更新Canvas
        Canvas.ForceUpdateCanvases();
    }

    private void OnSearchFilterChanged(string filter)
    {
        searchFilter = filter;
        UpdateFilteredLogs();
    }

    private void ClearConsole()
    {
        allLogs.Clear();
        filteredLogs.Clear();
        consoleOutput.text = "";
    }

    private void ToggleAutoScroll()
    {
        autoScroll = !autoScroll;
        toggleAutoScrollButton.GetComponentInChildren<TMP_Text>().text =
            autoScroll ? "Auto Scroll: ON" : "Auto Scroll: OFF";

        // 如果开启自动滚动，立即滚动到底部
        if (autoScroll)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = string.Join("\n", filteredLogs);

        // 显示复制成功的反馈
        StartCoroutine(ShowCopyFeedback());
    }

    private IEnumerator ShowCopyFeedback()
    {
        TextMeshProUGUI buttonText = copyToClipboardButton.GetComponentInChildren<TextMeshProUGUI>();
        string originalText = buttonText.text;
        buttonText.text = "Copied!";

        yield return new WaitForSeconds(1f);

        buttonText.text = originalText;
    }
}