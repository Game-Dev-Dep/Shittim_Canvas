using System;
using System.Collections.Generic;

[Serializable]
public class Setting_Config
{
    public General_Class General = new General_Class();
    public Audio_Class Audio = new Audio_Class();
    public Graphic_Class Graphic;
    public About_Class About = new About_Class();

    public class General_Class
    {
        public int Language = 0;
        public List<string> Language_List = new List<string>
        {
            "简体中文",
            "English",
            "日本語"
        };
    }
    public class Audio_Class
    {
        public float Global_Sound = 1.0f;
        public float Talk_Sound = 1.0f;
        public float SFX_Sound = 1.0f;
        public float BGM_Sound = 1.0f;
    }
    public class Graphic_Class
    {

    }
    public class About_Class
    {
        public string Version = "シティム・キャンバス 2nd Edition Ver1.03";
        public string Build_Date = "2010-07-28";
    }
}
