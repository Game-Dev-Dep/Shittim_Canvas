using System;
using System.Collections.Generic;

[Serializable]
public class CharacterTimer_Config
{
    public Dictionary<string, CharacterTimerData> Character_Timers = new Dictionary<string, CharacterTimerData>();
    public string Current_Active_Character = "";
    public long Last_Switch_Timestamp = 0;

    public class CharacterTimerData
    {
        public long Accumulated_Seconds = 0;
        public long Last_Start_Timestamp = 0;
        public bool Is_Active = false;
    }
}
