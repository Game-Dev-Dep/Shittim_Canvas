using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Framerate_Services : MonoBehaviour
{
    public static Framerate_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Framerate Services �����������");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button VSync_Toggle_Button;
    [SerializeField]
    public RawImage VSync_On_Image;
    [SerializeField]
    public RawImage VSync_Off_Image;
    [SerializeField]
    public TMP_InputField Framerate_InputField;
    private TMP_Text Framerate_InputField_Placeholder_Text;

    [Header("Core Variables")]
    public bool is_VSync_Mode = true;
    public int Target_Framerate = 120;

    private void Start()
    {
        Console_Log("������ʼ�� Framerate Services");

        VSync_Toggle_Button.onClick.AddListener(Toggle_VSync_Mode);

        Framerate_InputField.onEndEdit.AddListener(Set_Target_Framerate_Listener);
        Framerate_InputField.contentType = TMP_InputField.ContentType.IntegerNumber;
        Framerate_InputField_Placeholder_Text = Framerate_InputField.placeholder as TMP_Text;

        Update_Button_UI();

        Console_Log("������ʼ�� Framerate Services");
    }

    public void Toggle_VSync_Mode()
    {
        Console_Log("�����л� VSync ģʽ");

        is_VSync_Mode = !is_VSync_Mode;

        if (is_VSync_Mode)
        {
            Console_Log("�л�Ϊ VSync ģʽ");
            QualitySettings.vSyncCount = 1;
            Application.targetFrameRate = -1;
            
        }
        else
        {
            Console_Log("�л�Ϊָ��֡��ģʽ");
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = Target_Framerate;
        }

        Update_Button_UI();
    }

    public void Set_Target_Framerate_Listener(string value) { Set_Target_Framerate(value); }

    public void Set_Target_Framerate(string value, bool is_temp_change = false)
    {
        if (!is_VSync_Mode)
        {
            Console_Log("��ʼ����Ŀ��֡��");

            if (int.TryParse(value, out int input_framerate) && input_framerate > 0 && input_framerate < 360)
            {
                Console_Log($"Ŀ��֡�� {input_framerate} Ϊ�Ϸ��ķǸ�����");
                if (!is_temp_change) Target_Framerate = input_framerate;
                Application.targetFrameRate = input_framerate;
                Update_InputField_UI();
            }
            else
            {
                Console_Log($"Ŀ��֡�� {input_framerate} ��Ϊ�Ϸ��ķǸ�����");
                Framerate_InputField.text = Target_Framerate.ToString();
            }

            Console_Log("��������Ŀ��֡��");
        }
        else
        {
            Console_Log("��ǰΪVSyncģʽ������ָ��֡��");
        }
    }

    public void Update_Button_UI()
    {
        VSync_On_Image.enabled = is_VSync_Mode;
        VSync_Off_Image.enabled = !is_VSync_Mode;
        Update_InputField_UI();
    }

    public void Update_InputField_UI()
    {
        Framerate_InputField.text = "";
        if (is_VSync_Mode)
        {
            Framerate_InputField_Placeholder_Text.SetText("VSync");
            Framerate_InputField.GetComponent<TMP_InputField>().enabled = false;
        }
        else
        {
            Framerate_InputField_Placeholder_Text.SetText(Target_Framerate.ToString());
            Framerate_InputField.GetComponent<TMP_InputField>().enabled = true;
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Framerate Services", message, loglevel, logtype); }
}
