using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class Camera_Services : MonoBehaviour
{
    public static Camera_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Camera Services �����������");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Camera MemoryLobby_Camera;

    [Header("UI Elements")]
    [SerializeField]
    public Button Read_Camera_Settings_Button;
    [SerializeField]
    public Button Save_Camera_Settings_Button;
    [SerializeField]
    public Button Reset_Camera_Settings_Button;
    [SerializeField]
    public GameObject Camera_Mode_Panel;
    [SerializeField]
    public TextMeshProUGUI Camera_Mode_Text;

    [Header("Drag Settings")]
    public float Drag_Sensitivity = 0.4f;  // ��ק������

    [Header("Zoom Settings")]
    public float Zoom_Sensitivity = 0.01f;  // ����������
    public float Zoom_Min_Size = 0.01f;  // ��С����ֵ
    public float Zoom_Max_Size = 2f;  // �������ֵ

    [Header("Rotation Settings")]
    public float Rotation_Sensitivity = 0.5f;  // ��ת������
    public float Rotation_Min_Angle = -180f;  // ��С��ת�Ƕ�
    public float Rotation_Max_Angle = 180f;  // �����ת�Ƕ�

    private bool is_Drag_Mode;  // ��קģʽ����
    private bool is_Rotate_Mode;  // ��תģʽ����
    private Vector3 Drag_Start_Position;
    private bool is_Camera_Fixed = false;

    private void Start()
    {
        Console_Log("��ʼ��ʼ�� Camera Services");

        Read_Camera_Settings_Button.onClick.AddListener(
            () => Load_Camera_Config(MemoryLobby_Camera, Config_Services.Instance.MemoryLobby_Camera_Config)
        );
        Save_Camera_Settings_Button.onClick.AddListener(
            () => Save_Camera_Settings(MemoryLobby_Camera, Config_Services.Instance.MemoryLobby_Camera_Config)
        );
        Reset_Camera_Settings_Button.onClick.AddListener(
            () => Reset_Camera_Settings(MemoryLobby_Camera)
        );

        Console_Log("������ʼ�� Camera Services");
    }

    private void Update()
    {
        if (MemoryLobby_Camera != null)
        {
            if (!Wallpaper_Services.Instance.is_Wallpaper_Mode)
            {
                if (is_Camera_Fixed) is_Camera_Fixed = false;

                if (is_Drag_Mode)
                {
                    Camera_Mode_Panel.SetActive(true);
                    Camera_Mode_Text.SetText("����ģʽ");
                    if (Input.mouseScrollDelta.y != 0)
                    {
                        Zoom_Handler(MemoryLobby_Camera);
                    }
                    if (Input.GetMouseButtonDown(2))
                    {
                        Drag_Start(MemoryLobby_Camera);
                    }
                    if (Input.GetMouseButton(2))
                    {
                        Drag_Handler(MemoryLobby_Camera, Drag_Start_Position);
                    }
                }

                if (is_Rotate_Mode)
                {
                    Camera_Mode_Panel.SetActive(true);
                    Camera_Mode_Text.SetText("��תģʽ");
                    if (Input.mouseScrollDelta.y != 0)
                    {
                        Rotation_Handler(MemoryLobby_Camera);
                    }
                }

                if(!is_Drag_Mode && !is_Rotate_Mode)
                {
                    Camera_Mode_Panel.SetActive(false);
                }
            }
            else
            {
                if (!is_Camera_Fixed)
                {
                    Console_Log(
                    $"��ǰ�����������:\n" +
                    $"λ��X: {MemoryLobby_Camera.transform.position}\n" +
                    $"λ��Y: {MemoryLobby_Camera.transform.position.y}\n" +
                    $"��תZ: {MemoryLobby_Camera.transform.eulerAngles.z}\n" +
                    $"����: {MemoryLobby_Camera.orthographicSize}"
                    );

                    if (Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Position_X == 0 &&
                        Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Position_Y == 0 &&
                        Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Rotation_Z == 0 &&
                        Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Size == 1)
                    {
                        Console_Log("��ǰ�������������Ĭ������һ�£��������");
                    }
                    else
                    {
                        Console_Log("��ǰ�������������Ĭ�ϵ����ò�һ�£���ʼ����");
                        MemoryLobby_Camera.transform.position = new Vector2(Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Position_X, Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Position_Y);
                        MemoryLobby_Camera.transform.rotation = Quaternion.Euler(0f, 0f, Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Rotation_Z);
                        MemoryLobby_Camera.orthographicSize = Config_Services.Instance.MemoryLobby_Camera_Config.Camera_Size;

                        Console_Log(
                        $"����������������:\n" +
                        $"λ��X: {MemoryLobby_Camera.transform.position}\n" +
                        $"λ��Y: {MemoryLobby_Camera.transform.position.y}\n" +
                        $"��תZ: {MemoryLobby_Camera.transform.eulerAngles.z}\n" +
                        $"����: {MemoryLobby_Camera.orthographicSize}");
                    }

                    is_Camera_Fixed = true;
                }
            }
        }
    }

    public void Set_MemoryLobby_Camera(Camera camera)
    {
        MemoryLobby_Camera = camera;
        Load_Camera_Config(MemoryLobby_Camera, Config_Services.Instance.MemoryLobby_Camera_Config);
        Console_Log("�ѳ�ʼ��������������");
        MemoryLobby_Camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
        Console_Log("������������� Post Processing ѡ��");
    }

    public void Toggle_Drag_Mode()
    {
        is_Drag_Mode = !is_Drag_Mode;
        Console_Log((is_Drag_Mode ? "����" : "����") + "���������ģʽ");
        is_Rotate_Mode = false;
    }

    public void Zoom_Handler(Camera camera)
    {
        float mouse_scroll_delta = -Input.mouseScrollDelta.y;  // ���ݹ��ַ�������������С
        float zoom_new_size = camera.orthographicSize + mouse_scroll_delta * Zoom_Sensitivity;
        camera.orthographicSize = Mathf.Clamp(zoom_new_size, Zoom_Min_Size, Zoom_Max_Size);  // �������ŷ�Χ
    }

    public void Drag_Start(Camera camera)
    {
        Drag_Start_Position = Get_Mouse_World_Position(camera);
    }

    public void Drag_Handler(Camera camera, Vector3 drag_start_position)
    {
        Vector3 mouse_cur_position = Get_Mouse_World_Position(camera);  // ���㵱ǰ���λ�� (��������)
        Vector3 mouse_delta_postition = drag_start_position - mouse_cur_position;  // ����λ�Ʋ�ֵ (��XY��)
        mouse_delta_postition.z = 0;
        camera.transform.position += mouse_delta_postition * Drag_Sensitivity;  // Ӧ��λ��
        Drag_Start(camera);  // ������ʼλ�ã�ʵ��ƽ����ק
    }

    public void Toggle_Rotate_Mode()
    {
        is_Rotate_Mode = !is_Rotate_Mode;
        Console_Log((is_Drag_Mode ? "����" : "����") + "�������ת����");
        is_Drag_Mode = false;
    }

    public void Rotation_Handler(Camera camera)
    {
        float scroll_delta = Input.mouseScrollDelta.y;  // ��ȡ��������ֵ
        float rotation_cur_angle = camera.transform.eulerAngles.z;  // �����µ���ת�Ƕ�
        float rotation_new_angle = rotation_cur_angle + scroll_delta * Rotation_Sensitivity;

        //if (new_rotation_angle > 180f) new_rotation_angle = 180f;
        //if (new_rotation_angle < -180f) new_rotation_angle = -180;

        //rotation_new_angle = Mathf.Clamp(rotation_new_angle, Rotation_Min_Angle, Rotation_Max_Angle);
        camera.transform.rotation = Quaternion.Euler(0f, 0f, rotation_new_angle);  // Ӧ����ת (��Z��)
    }

    Vector3 Get_Mouse_World_Position(Camera camera)
    {
        Vector3 mouse_positon = Input.mousePosition;
        mouse_positon.z = -transform.position.z;
        return camera.ScreenToWorldPoint(mouse_positon);
    }

    public void Load_Camera_Config(Camera camera, Camera_Config camera_config)
    {
        camera.transform.position = new Vector2(camera_config.Camera_Position_X, camera_config.Camera_Position_Y);
        camera.transform.rotation = Quaternion.Euler(0f, 0f, camera_config.Camera_Rotation_Z);
        if (!camera.orthographic) camera.orthographic = true;
        camera.orthographicSize = camera_config.Camera_Size;
    }

    public void Save_Camera_Settings(Camera camera, Camera_Config camera_config)
    {
        camera_config.Defalut_Character_Name = Index_Services.Instance.Character_Name;
        camera_config.Camera_Position_X = camera.transform.position.x;
        camera_config.Camera_Position_Y = camera.transform.position.y;
        camera_config.Camera_Rotation_Z = camera.transform.eulerAngles.z;
        camera_config.Camera_Size = camera.orthographicSize;
        Config_Services.Instance.Save_Camera_Config(camera_config, Path.Combine(File_Services.Config_Files_Folder_Path, "MemoryLobby Camera Config.json"));
    }

    public void Reset_Camera_Settings(Camera camera)
    {
        camera.transform.position = new Vector2(0, 0);
        camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
        camera.orthographicSize = 1;
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Camera_Services", message, loglevel, logtype); }
}