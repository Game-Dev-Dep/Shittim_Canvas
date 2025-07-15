using UnityEngine;

public class Index_Services : MonoBehaviour
{
    public static Index_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Index Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("Character Related")]
    public string Character_Name;
    public int Talk_Animaiton_Num = 0;
    public int M_Track_Num = 1;
    public int A_Track_Num = 2;
    public GameObject Cur_Responsing_Object = null;
    public bool is_Talking = false;
    public bool is_Idle_Mode = false;

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Index_Services", message, loglevel, logtype); }
}
