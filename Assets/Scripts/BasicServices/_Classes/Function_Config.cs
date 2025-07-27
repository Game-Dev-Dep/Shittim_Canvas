using System;

[Serializable]
public class Function_Config
{
    public bool is_IK_On = true;
    public bool is_Talk_On = true;
    public bool is_Subtitle_JP_On = true;
    public bool is_Subtitle_Custom_On = true;
    public bool is_Volume_On = true;
    public bool is_AutoStartup_On = false;
    public bool is_Auto_Wallpaper_Mode_On = false;
    
    // VSync相关配置
    public bool is_VSync_Mode = true;
    public int Target_Framerate = 120;
}
