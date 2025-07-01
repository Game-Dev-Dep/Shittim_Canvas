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
        // ���ð�ť�¼�
        clearButton.onClick.AddListener(ClearConsole);
        toggleAutoScrollButton.onClick.AddListener(ToggleAutoScroll);
        copyToClipboardButton.onClick.AddListener(CopyToClipboard);

        // �����������¼�
        searchInput.onValueChanged.AddListener(OnSearchFilterChanged);

        // ������־�����л��¼�
        infoToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });
        warningToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });
        errorToggle.onValueChanged.AddListener(delegate { UpdateFilteredLogs(); });

        // ��ʼ״̬
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
        // �������������
        if (!consoleVisible && requireCheatCode)
        {
            DetectKonamiCode();
        }

        // ��ESC���رտ���̨
        if (consoleVisible && Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleConsole();
        }
    }

    private void DetectKonamiCode()
    {
        // ��鰴����ʱ
        if (Time.time - lastKeyTime > keyTimeout)
        {
            konamiIndex = 0;
        }

        // ��鰴������
        if (Input.anyKeyDown)
        {
            if (konamiIndex < konamiCode.Length && Input.GetKeyDown(konamiCode[konamiIndex]))
            {
                konamiIndex++;
                lastKeyTime = Time.time;

                // ���������
                if (konamiIndex == konamiCode.Length)
                {
                    ToggleConsole();
                    konamiIndex = 0;
                }
            }
            else
            {
                // ����İ�������������
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
        // �������̨���ɼ�����������־����ѡ��
        // if (!consoleVisible) return;

        // ������־�������ǰ׺
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

        // ���ʱ���
        string timestamp = System.DateTime.Now.ToString("[HH:mm:ss] ");
        string formattedLog = timestamp + prefix + logString;

        // �����־
        allLogs.Add(formattedLog);

        // ������־����
        if (allLogs.Count > maxLines)
        {
            allLogs.RemoveAt(0);
        }

        // ������ʾ
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
            // Ӧ����������
            if (!string.IsNullOrEmpty(searchFilter)
                && !log.ToLower().Contains(searchFilter.ToLower()))
            {
                continue;
            }

            // Ӧ����־���͹���
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

        // �Զ��������ײ� - ʹ��Э��ȷ����UI���º�ִ��
        if (autoScroll)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        // �ȴ�һ֡ȷ��UI�������
        yield return new WaitForEndOfFrame();

        // ǿ�Ƹ���Canvas
        Canvas.ForceUpdateCanvases();

        // �������ײ�
        scrollRect.verticalNormalizedPosition = 0f;

        // �ٴ�ǿ�Ƹ���Canvas
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

        // ��������Զ������������������ײ�
        if (autoScroll)
        {
            StartCoroutine(ScrollToBottom());
        }
    }

    private void CopyToClipboard()
    {
        GUIUtility.systemCopyBuffer = string.Join("\n", filteredLogs);

        // ��ʾ���Ƴɹ��ķ���
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