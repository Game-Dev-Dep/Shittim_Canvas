using UnityEngine;

public class Wallpaper_Mode_Handler : MonoBehaviour
{
    public static Wallpaper_Mode_Handler Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Wallpaper Mode Handler 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool is_Pressed = false;
    public bool is_Pressing = false;
    public bool is_Draging = false;
    public bool is_Released = false;
    private bool Last_is_Pressed = false;
    private bool Cur_is_Pressed;

    public Vector2 Drag_Delta;
    private Vector2 Last_Cursor_Postion = Vector2.zero;
    private Vector2 Cur_Cursor_Postion;

    void Update()
    {
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.is_Switching)
            {
                Console_Log($"检测到发窗口切换", Debug_Services.LogLevel.Ignore);
                is_Pressed = false;
                is_Pressing = false;
                is_Draging = false;
                is_Released = false;
            }

            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window) return;

            Cur_is_Pressed = Input_Services.Instance.Mouse_Info.is_LMB_Pressed;
            Cur_Cursor_Postion = Input_Services.Instance.Mouse_Info.Position;

            if ((Cur_is_Pressed && !Last_is_Pressed) || is_Pressing)
            {
                if (!is_Pressing)
                {
                    Console_Log("按下");

                    is_Pressed = true;
                    is_Pressing = true;
                    Index_Services.Instance.Cur_Responsing_Object = UICamera.hoveredObject;
                }
                else
                {
                    //Console_Log("按住");
                    if (Cur_Cursor_Postion != Last_Cursor_Postion)
                    {
                        if (!is_Draging)
                        {
                            Console_Log("开始拖拽");
                            is_Draging = true;
                        }
                        Drag_Delta = Cur_Cursor_Postion - Last_Cursor_Postion;
                    }
                    else
                    {
                        if (is_Draging)
                        {
                            Console_Log("停止拖拽");
                            is_Draging = false;
                        }
                    }
                }

            }

            if (!Cur_is_Pressed && Last_is_Pressed)
            {
                Console_Log("松开");

                is_Pressed = false;
                is_Pressing = false;
                is_Draging = false;
                is_Released = true;
            }


            if (Debug_Services.Instance.is_Debug)
            {
                string gameobject_name = Index_Services.Instance.Cur_Responsing_Object == null ? "NULL" : Index_Services.Instance.Cur_Responsing_Object.name;
                Debug_Services.Instance.Debug_Info_Text_06.SetText(
                $"{gameobject_name}\n" +
                $"is_Pressing:{is_Pressing}\n" +
                $"is_Draging:{is_Draging}\n" +
                $"is_Pressed:{is_Pressed}\n" +
                $"is_Released: {is_Released}\n" +
                $"Cur_is_Pressed:{Cur_is_Pressed}\n" +
                $"Last_is_Pressed:{Last_is_Pressed}\n" +
                $"Cur_Cursor_Postion:{Cur_Cursor_Postion}\n" +
                $"Last_Cursor_Postion:{Last_Cursor_Postion}\n" +
                $"Drag_Delta: {Drag_Delta}"
                );
            }

            Last_is_Pressed = Cur_is_Pressed;
            Last_Cursor_Postion = Cur_Cursor_Postion;
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Wallpaper_Mode_Handler", message, loglevel, logtype); }
}
