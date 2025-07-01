using UnityEngine;
using TMPro;

public class Input_Services : MonoBehaviour
{
    public static Input_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Input Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [SerializeField]
    public Mouse_Info_Class Mouse_Info = new Mouse_Info_Class();
    void Update()
    {
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Get_Mouse_Info();
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    Camera_Services.Instance.Toggle_Drag_Mode();
                }
                if (Input.GetKeyDown(KeyCode.R))
                {
                    Camera_Services.Instance.Toggle_Rotate_Mode();
                }
            }
        }
    }
    public void Get_Mouse_Info()
    {
        Mouse_Info.Position = Win32InputWrapper.GetMousePosVector2();
        Mouse_Info.Position.y = Screen.currentResolution.height - Mouse_Info.Position.y;
        Mouse_Info.is_LMB_Pressed = Win32InputWrapper.GetKeyDown(win32keycode.LMB);
        Mouse_Info.is_RMB_Pressed = Win32InputWrapper.GetKeyDown(win32keycode.RMB);
        if (Debug_Services.Instance.is_Debug) 
            Debug_Services.Instance.Debug_Info_Text_01.SetText(
                $"{Wallpaper_Services.Instance.is_Wallpaper_Mode}\n" +
                $"Mouse Info\n" +
                $"Position: ( {Mouse_Info.Position.x} , {Mouse_Info.Position.y} ) | LMB: {Mouse_Info.is_LMB_Pressed} {((short)Win32InputWrapper.GetAsyncKeyState(((int)win32keycode.LMB))).ToString()} | RMB: {Mouse_Info.is_RMB_Pressed}");
    }
    public class Mouse_Info_Class
    {
        public Vector2 Position = Vector2.zero;
        public bool is_LMB_Pressed;
        public bool is_RMB_Pressed;
    }
}
