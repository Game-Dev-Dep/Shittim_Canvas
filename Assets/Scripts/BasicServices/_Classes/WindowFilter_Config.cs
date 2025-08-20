using System.Collections.Generic;

public class WindowFilter_Config
{
    // 最新最热
    // 壁纸模式交互白名单 - 这些窗口不会触发壁纸模式的交互
    public List<string> Wallpaper_Interaction_Whitelist_Title_Names = new List<string>();
    public List<string> Wallpaper_Interaction_Whitelist_Class_Names = new List<string>();
    
    // 全屏检测静音白名单 - 这些窗口会被识别为全屏并触发静音
    public List<string> Fullscreen_Mute_Whitelist_Title_Names = new List<string>();
    public List<string> Fullscreen_Mute_Whitelist_Class_Names = new List<string>();
    
    // 以前的
    public List<string> Title_Names => Wallpaper_Interaction_Whitelist_Title_Names;
    public List<string> Class_Names => Wallpaper_Interaction_Whitelist_Class_Names;
}
