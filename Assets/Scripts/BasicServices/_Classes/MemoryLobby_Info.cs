using System.Collections.Generic;

public class MemoryLobby_Info
{
    public long CharacterId { get; set; }
    public string CharacterName { get; set; } = "";
    public string PrefabName { get; set; } = "";
    public long BGMId { get; set; }
    public List<string> Audio_Files { get; set; } = new List<string>();
    public List<Subtitle> Subtitles { get; set; } = new List<Subtitle>();
    public class Subtitle
    {
        public long Duration { get; set; }
        public string AnimationName { get; set; } = "";
        public string LocalizeJP { get; set; } = "";
        public string LocalizeKR { get; set; } = "";
        public string LocalizeCustom { get; set; } = "";
        public Subtitle(long duration, string animation_name, string localize_jp, string localize_kr)
        {
            Duration = duration;
            AnimationName = animation_name;
            LocalizeJP = localize_jp;
            LocalizeKR = localize_kr;
        }
    }
}