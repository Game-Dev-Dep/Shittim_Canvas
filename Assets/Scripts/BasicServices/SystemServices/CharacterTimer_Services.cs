using System;
using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
//欢迎来到学生陪伴时长服务，这是一个用于记录与学生陪伴时长的系统！
//实现方法比较拉，但具体思路就是设置一个字典，键为学生名字，值为陪伴时长，然后每次切换学生时，更新字典中的值，并保存到配置文件中。
//用timestamp来记录上次切换学生的时间，然后每次切换学生时，更新timestamp，并计算出陪伴时长，大概就是这样.jpg
/// </summary>


public class CharacterTimer_Services : MonoBehaviour
{
    public static CharacterTimer_Services Instance { get; set; }
    private Coroutine saveTimerCoroutine;

    void Awake()
    {
        if (Instance == null) { Instance = this; Debug.Log("[Awake] CharacterTimer Services 单例创建完成"); }
        else Destroy(gameObject);
    }

    void Start() 
    { 
        saveTimerCoroutine = StartCoroutine(AutoSaveTimer());
        StartCoroutine(RestoreTimerOnStartupDelayed());
    }
    
    private IEnumerator RestoreTimerOnStartupDelayed()
    {
        yield return new WaitForSeconds(1f);
        RestoreTimerOnStartup();
    }
    
    private void RestoreTimerOnStartup()
    {
        var timerConfig = Config_Services.Instance.Global_CharacterTimer_Config;
        
        if (!string.IsNullOrEmpty(timerConfig.Current_Active_Character))
        {
            string characterName = timerConfig.Current_Active_Character;
            
            var expectedName = Config_Services.Instance.MemoryLobby_Camera_Config.Defalut_Character_Name;
            
            if (characterName == expectedName)
            {
                if (!timerConfig.Character_Timers.ContainsKey(characterName))
                {
                    timerConfig.Character_Timers[characterName] = new CharacterTimer_Config.CharacterTimerData();
                }
                
                var timerData = timerConfig.Character_Timers[characterName];
                timerData.Last_Start_Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                timerData.Is_Active = true;
                
                Console_Log($"恢复计时器: {characterName}");
            }
        }
    }

     // 统一停表入口
     void StopActiveTimer()
     {
         var cfg = Config_Services.Instance.Global_CharacterTimer_Config;
         var id = cfg.Current_Active_Character;
         if (string.IsNullOrEmpty(id) || !cfg.Character_Timers.ContainsKey(id)) return;
         var t = cfg.Character_Timers[id];
         if (!t.Is_Active) return;
         long now = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
         t.Accumulated_Seconds += now - t.Last_Start_Timestamp;
         t.Is_Active = false;
     }

    public void HandleCharacterSwitch(string new_character_name)
    {
        var timerConfig = Config_Services.Instance.Global_CharacterTimer_Config;
        long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        if (!string.IsNullOrEmpty(timerConfig.Current_Active_Character) && timerConfig.Current_Active_Character != new_character_name)
        {
            if (timerConfig.Character_Timers.ContainsKey(timerConfig.Current_Active_Character))
            {
                var timerData = timerConfig.Character_Timers[timerConfig.Current_Active_Character];
                if (timerData.Is_Active)
                {
                    long elapsed = currentTimestamp - timerData.Last_Start_Timestamp;
                    timerData.Accumulated_Seconds += elapsed;
                    timerData.Is_Active = false;
                }
            }
        }

        timerConfig.Last_Switch_Timestamp = currentTimestamp;

        if (!timerConfig.Character_Timers.ContainsKey(new_character_name))
            timerConfig.Character_Timers[new_character_name] = new CharacterTimer_Config.CharacterTimerData();

        var newTimerData = timerConfig.Character_Timers[new_character_name];
        newTimerData.Last_Start_Timestamp = currentTimestamp;
        newTimerData.Is_Active = true;
        timerConfig.Current_Active_Character = new_character_name;
    }

    public long GetTotalTimerSeconds(string characterName)
    {
        var timerConfig = Config_Services.Instance.Global_CharacterTimer_Config;
        if (timerConfig.Character_Timers.ContainsKey(characterName))
        {
            var timerData = timerConfig.Character_Timers[characterName];
            long total = timerData.Accumulated_Seconds;
            if (timerData.Is_Active && timerConfig.Current_Active_Character == characterName)
            {
                long currentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                total += currentTimestamp - timerData.Last_Start_Timestamp;
            }
            return total;
        }
        return 0;
    }

    IEnumerator AutoSaveTimer()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); //为了保证性能，设置了五秒保存一次，影响应该不大.jpg
            Config_Services.Instance.Save_CharacterTimer_Config(
                Config_Services.Instance.Global_CharacterTimer_Config,
                Path.Combine(File_Services.Config_Files_Folder_Path, "CharacterTimer Config.json"));
        }
    }
    
    //一堆监听
     void OnApplicationPause(bool paused)
     {
         if (paused)
         {
             StopActiveTimer();
             Config_Services.Instance.Save_CharacterTimer_Config(
                 Config_Services.Instance.Global_CharacterTimer_Config,
                 Path.Combine(File_Services.Config_Files_Folder_Path, "CharacterTimer Config.json"));
         }
         else
         {
             RestoreTimerOnStartup();
         }
     }

     void OnApplicationQuit()
     {
         StopActiveTimer();
         Config_Services.Instance.Save_CharacterTimer_Config(
             Config_Services.Instance.Global_CharacterTimer_Config,
             Path.Combine(File_Services.Config_Files_Folder_Path, "CharacterTimer Config.json"));
     }

    void OnDestroy()
    {
        if (saveTimerCoroutine != null) StopCoroutine(saveTimerCoroutine);
        StopActiveTimer();
        if (Config_Services.Instance != null)
            Config_Services.Instance.Save_CharacterTimer_Config(
                Config_Services.Instance.Global_CharacterTimer_Config,
                Path.Combine(File_Services.Config_Files_Folder_Path, "CharacterTimer Config.json"));
    }

    static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log)
    { Debug_Services.Instance.Console_Log("CharacterTimer_Services", message, loglevel, logtype); }
}
