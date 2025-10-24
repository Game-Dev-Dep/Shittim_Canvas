using System;
using System.Collections.Generic;

[Serializable]
public class Setting_Config
{
    public General_Class General = new General_Class();
    public Audio_Class Audio = new Audio_Class();
    public Graphic_Class Graphic = new Graphic_Class();
    public About_Class About = new About_Class();

    public class General_Class
    {
        public int Language = 2;
        public List<string> Language_List = new List<string>
        {
            "简体中文",
            "繁體中文",
            "English",
            "日本語"
        };
        public int Auto_Startup = 1;
        public List<string> Auto_Startup_List = new List<string>
        {
            "是",
            "否"
        };
        public int Auto_Wallpaper_Mode = 1;
        public List<string> Auto_Wallpaper_Mode_List = new List<string>
        {
            "是",
            "否"
        };
        public int Notification_Enabled = 0;
        public List<string> Notification_Enabled_List = new List<string>
        {
            "开",
            "关"
        };
        public int Wallpaper_Mode_Status_Area_Enabled = 0;
        public List<string> Wallpaper_Mode_Status_Area_Enabled_List = new List<string>
        {
            "开",
            "关"
        };
        public int Random_Character_On_Startup = 1;
        public List<string> Random_Character_On_Startup_List = new List<string>
        {
            "开",
            "关"
        };
        public int Auto_Random_Character_Interval = 0;
        public List<string> Auto_Random_Character_Interval_List = new List<string>
        {
            "OFF",
            "5M",
            "10M",
            "30M",
            "1H",
            "2H",
            "5H",
            "12H"
        };
        public int Pseudo_Random_Mode = 0;
        public List<string> Pseudo_Random_Mode_List = new List<string>
        {
            "开",
            "关"
        };
        public bool OOBE_Completed = false;
    }
    public class Audio_Class
    {
        public float Global_Sound = 1.0f;
        public float Talk_Sound = 0.8f;
        public float SFX_Sound = 0.3f;
        public float BGM_Sound = 0.7f;
        public float UI_SFX_Sound = 1.0f;
    }
    public class Graphic_Class
    {
        public int Editor_Mode_Resolution_Width = 1920;
        public int Editor_Mode_Resolution_Height = 1080;
        public float Editor_Mode_UI_Scale = 0.5f;
        public int Wallpaper_Mode_Refresh_Type = 0;
        public List<string> Wallpaper_Mode_Refresh_Type_List = new List<string>
        {
            "VSync",
            "锁帧"
        };
        public int Wallpaper_Mode_Framerate = 120;
        public int Selected_Display_Monitor_Index = 0;
        public List<string> Display_Monitor_Options = new List<string>();
    }
    public class About_Class
    {
        public string Version = "シティム・キャンバス 2nd Edition Ver1.03";
        public string Build_Date = "2010-07-28";
    }
}
