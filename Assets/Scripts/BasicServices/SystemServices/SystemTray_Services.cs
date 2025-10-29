using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using System.IO;
using Newtonsoft.Json;
using System.Linq;

public class SystemTray_Services : MonoBehaviour
{
    public static SystemTray_Services Instance { get; set; }
    
    [SerializeField]
    public Texture2D SystemTray_Icon;

    private float savedVolume = 1.0f;
    private const string WALLPAPER_MENU_ENTER = "进入壁纸模式";
    private const string WALLPAPER_MENU_EXIT = "返回正常模式";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
#if !UNITY_EDITOR
        Debug.Log($"开始初始化系统托盘");

        List<(string, Action)> SystemTray_Menu = new List<(string, Action)>()
        {
            (WALLPAPER_MENU_ENTER, Toggle_Wallpaper_Mode_From_Tray),
            (TrayIcon.SEPARATOR, null),
            ("静音", Toggle_Mute),
            (TrayIcon.SEPARATOR, null),
            ("复制覆盖窗口类名", Copy_Cover_Window_ClassName),
            (TrayIcon.SEPARATOR, null),
            ("收藏学生", null),
            (TrayIcon.SEPARATOR, null),
            ("退出", Quit_Program)
        };

        TrayIcon.Init("ShittimCanvas", "Shittim Canvas", SystemTray_Icon, SystemTray_Menu);
        StartCoroutine(DelayedCreateFavoriteStudentsSubMenu());

        Debug.Log($"结束初始化系统托盘");
#endif
    }

    private IEnumerator DelayedCreateFavoriteStudentsSubMenu()
    {
        while (Config_Services.Instance == null || Config_Services.Instance.Global_Favorite_Config == null)
        {
            yield return new WaitForSeconds(0.1f);
        }
        
        yield return null;
        CreateFavoriteStudentsSubMenu();
        Console_Log("收藏学生子菜单创建完成");

        bool isMuted = Config_Services.Instance.Global_Setting_Config.Audio.Global_Sound == 0f;
        if (isMuted)
        {
            TrayIcon.SetMenuItemChecked("静音", true);
        }

        while (Wallpaper_Services.Instance == null)
        {
            yield return new WaitForSeconds(0.1f);
        }

        yield return new WaitForSeconds(0.2f);

        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            TrayIcon.UpdateMenuItemText(WALLPAPER_MENU_ENTER, WALLPAPER_MENU_EXIT);
        }
    }

    private void Toggle_Wallpaper_Mode_From_Tray()
    {
        bool isWallpaperMode = Wallpaper_Services.Instance.is_Wallpaper_Mode;
        Console_Log($"系统托盘触发: {(isWallpaperMode ? "返回正常模式" : "进入壁纸模式")}");
        
        Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        
        string oldLabel = isWallpaperMode ? WALLPAPER_MENU_EXIT : WALLPAPER_MENU_ENTER;
        string newLabel = isWallpaperMode ? WALLPAPER_MENU_ENTER : WALLPAPER_MENU_EXIT;
        TrayIcon.UpdateMenuItemText(oldLabel, newLabel);
    }
    
    /// <summary>
    /// 更新壁纸模式菜单文本（供外部调用）
    /// </summary>
    /// <param name="isEnteringWallpaperMode">true表示进入壁纸模式，false表示退出壁纸模式</param>
    public void UpdateWallpaperMenuText(bool isEnteringWallpaperMode)
    {
    #if !UNITY_EDITOR
            if (isEnteringWallpaperMode)
            {
                TrayIcon.UpdateMenuItemText(WALLPAPER_MENU_ENTER, WALLPAPER_MENU_EXIT);
            }
            else
            {
                TrayIcon.UpdateMenuItemText(WALLPAPER_MENU_EXIT, WALLPAPER_MENU_ENTER);
            }
    #endif
    }
    
    // 托盘静音toggle，Menu Label在Utils.cs中定义，在TrayIcon.cs中处理，别忘了Constants.cs中的静音菜单项名称（
    private void Toggle_Mute()
    {
        bool isMuted = Config_Services.Instance.Global_Setting_Config.Audio.Global_Sound == 0f;
        
        if (isMuted)
        {
            Config_Services.Instance.Global_Setting_Config.Audio.Global_Sound = savedVolume;
            Audio_Services.Instance.Global_Sound_Slider_Handler(savedVolume);
            TrayIcon.SetMenuItemChecked("静音", false);
            Console_Log("系统托盘触发: 取消静音");
        }
        else
        {
            savedVolume = Config_Services.Instance.Global_Setting_Config.Audio.Global_Sound;
            if (savedVolume == 0f) savedVolume = 1.0f;
            Config_Services.Instance.Global_Setting_Config.Audio.Global_Sound = 0f;
            Audio_Services.Instance.Global_Sound_Slider_Handler(0f);
            TrayIcon.SetMenuItemChecked("静音", true);
            Console_Log("系统托盘触发: 静音");
        }
    }

    private void Copy_Cover_Window_ClassName()
    {
        Console_Log("系统托盘触发: 复制覆盖窗口类名");
        if (Window_Services.Instance != null)
        {
            string className = Window_Services.Instance.Get_Current_Cover_Window_ClassName();
            if (!string.IsNullOrEmpty(className))
            {
                GUIUtility.systemCopyBuffer = className;
                Console_Log($"已复制窗口类名: {className}");
            }
            else
            {
                Console_Log("当前无覆盖窗口");
            }
        }
    }

    private void Quit_Program()
    {
        Console_Log("系统托盘触发: 退出");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public void RefreshFavoriteStudentsSubMenu()
    {
#if !UNITY_EDITOR
        CreateFavoriteStudentsSubMenu();
#endif
    }

    private void CreateFavoriteStudentsSubMenu()
    {
        var favoriteStudents = GetFavoriteStudentsList();
        if (favoriteStudents.Count > 0)
        {
            TrayIcon.CreateSubMenu("收藏学生", favoriteStudents);
        }
    }

    private List<(string, Action)> GetFavoriteStudentsList()
    {
        var subMenuItems = new List<(string, Action)>();
        
        try
        {
            if (Config_Services.Instance?.Global_Favorite_Config?.Character_Names != null)
            {
                var favoriteNames = Config_Services.Instance.Global_Favorite_Config.Character_Names;
                var characterData = LoadCharacterData();
                
                foreach (var characterName in favoriteNames)
                {
                    string localizedName = GetLocalizedCharacterName(characterName, characterData);
                    subMenuItems.Add((localizedName, () => SwitchToCharacter(characterName)));
                }
            }
        }
        catch (Exception e)
        {
            Console_Log($"获取收藏学生列表失败: {e.Message}", Debug_Services.LogLevel.Core);
        }

        if (subMenuItems.Count == 0)
        {
            subMenuItems.Add(("暂无收藏", null));
        }

        return subMenuItems;
    }

    private Dictionary<long, List<CharacterData>> LoadCharacterData()
    {
        try
        {
            string json = File.ReadAllText(Path.Combine(File_Services.Student_Lists_Folder_Path, "CharacterList.json"));
            return JsonConvert.DeserializeObject<Dictionary<long, List<CharacterData>>>(json);
        }
        catch (Exception e)
        {
            Console_Log($"加载角色数据失败: {e.Message}", Debug_Services.LogLevel.Core);
            return new Dictionary<long, List<CharacterData>>();
        }
    }

    private string GetLocalizedCharacterName(string devName, Dictionary<long, List<CharacterData>> characterData)
    {
        try
        {
            // 查找对应的角色数据
            CharacterData character = null;
            foreach (var kvp in characterData)
            {
                var charList = kvp.Value;
                character = charList.FirstOrDefault(c => c.DevName == devName);
                if (character != null) break;
            }

            if (character == null)
            {
                return devName; // 如果找不到角色数据，返回DevName
            }

            // 获取当前语言设置
            var currentLocale = LocalizationSettings.SelectedLocale;
            if (currentLocale == null) return character.DevName;

            string localeCode = currentLocale.Identifier.Code;

            // 根据当前语言返回对应的名称
            switch (localeCode)
            {
                case "zh":
                    return !string.IsNullOrEmpty(character.FullNameSC) ? character.FullNameSC : character.DevName;
                case "zh-TW":
                    return !string.IsNullOrEmpty(character.FullNameTC) ? character.FullNameTC : character.DevName;
                case "en":
                    return !string.IsNullOrEmpty(character.FullNameEn) ? character.FullNameEn : character.DevName;
                case "ja":
                    return !string.IsNullOrEmpty(character.FullNameJp) ? character.FullNameJp : character.DevName;
                default:
                    return character.DevName;
            }
        }
        catch (Exception e)
        {
            Console_Log($"获取本地化角色名称失败: {e.Message}", Debug_Services.LogLevel.Core);
            return devName;
        }
    }

    // 角色数据结构类
    [System.Serializable]
    public class CharacterData
    {
        public string DevName = string.Empty;
        public string FullNameSC = string.Empty;
        public string FullNameTC = string.Empty;
        public string FullNameEn = string.Empty;
        public string FullNameJp = string.Empty;
        public List<string> Nicknames = new List<string>();
        public string School = string.Empty;
        public string Club = string.Empty;
    }

    private void SwitchToCharacter(string characterName)
    {
        Console_Log($"系统托盘触发: 切换到角色 {characterName}");
        StartCoroutine(DelayedSwitchToCharacter(characterName));
    }

    private IEnumerator DelayedSwitchToCharacter(string characterName)
    {
        yield return null;
        
        var allMonoBehaviours = FindObjectsOfType<MonoBehaviour>();
        MonoBehaviour characterServices = null;
        
        foreach (var mb in allMonoBehaviours)
        {
            if (mb.GetType().Name == "Character_Services")
            {
                characterServices = mb;
                break;
            }
        }
        
        if (characterServices != null)
        {
            var method = characterServices.GetType().GetMethod("Switch_Character");
            if (method != null)
            {
                try
                {
                    method.Invoke(characterServices, new object[] { characterName });
                    Console_Log($"成功切换到角色 {characterName}");
                }
                catch (Exception e)
                {
                    Console_Log($"调用Switch_Character方法失败: {e.Message}", Debug_Services.LogLevel.Core);
                }
            }
            else
            {
                Console_Log($"Character_Services中未找到Switch_Character方法", Debug_Services.LogLevel.Debug);
            }
        }
        else
        {
            Console_Log($"Character_Services实例未找到", Debug_Services.LogLevel.Debug);
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) 
    { 
        Debug_Services.Instance.Console_Log("SystemTray Services", message, loglevel, logtype); 
    }
}
