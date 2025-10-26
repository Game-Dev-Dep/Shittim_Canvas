using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character_Services : MonoBehaviour
{
    public static Character_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Character Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public string Character_Name = "";
    public Character character;

    private void Get_Config()
    {
        Character_Name = Config_Services.Instance.MemoryLobby_Camera_Config.Defalut_Character_Name;
    }

    private void Start()
    {
        Get_Config();
        
        // 检查是否启用启动随机大厅
        bool isRandomStartup = Config_Services.Instance.Global_Setting_Config.General.Random_Character_On_Startup == 0;
        
        if (isRandomStartup)
        {
            StartCoroutine(LoadRandomCharacter());
        }
        else
        {
//#if !UNITY_EDITOR
            character = gameObject.AddComponent<Character>();
            character.Load_Charachter(Character_Name);
//#endif
            
            //延迟通知Dropdown_Services设置初始lobby
            StartCoroutine(DelayedSetInitialCharacter());
        }
    }
    
    private IEnumerator LoadRandomCharacter()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (Dropdown_Services.Instance != null)
        {
            Dropdown_Services.Instance.SwitchToRandomCharacter();
        }
    }
    
    private IEnumerator DelayedSetInitialCharacter()
    {
        yield return new WaitForSeconds(0.1f);
        
        // 通知Dropdown_Services设置初始lobby
        if (Dropdown_Services.Instance != null)
        {
            Dropdown_Services.Instance.SetCurrentSelectedCharacter(Character_Name);
        }
    }

    public void Switch_Character(string character_name)
    {
        StartCoroutine(Switch_Character_Coroutine(character_name));
    }

    private IEnumerator Switch_Character_Coroutine(string character_name)
    {
        Debug.Log($"[Character_Services] 开始切换角色到: {character_name}");
        
        EnsureFadeServices();

        if (Fade_Services.Instance != null)
        {
            Debug.Log($"[Character_Services] 执行淡出效果");
            yield return StartCoroutine(Fade_Services.Instance.FadeOut(0.2f));
        }

        if (Camera_Services.Instance != null && Character_Name != character_name && !string.IsNullOrEmpty(Character_Name))
        {
            Camera_Services.Instance.Auto_Save_Camera_Settings();
        }
        
        if(character != null) character.Unload_Character();
        
        yield return null;
        
        Debug.Log($"[Character_Services] 加载角色: {character_name}");
        character = gameObject.AddComponent<Character>();
        character.Load_Charachter(character_name);
        
        if (CharacterTimer_Services.Instance != null)
        {
            CharacterTimer_Services.Instance.HandleCharacterSwitch(character_name);
        }
        
        yield return new WaitForSeconds(0.1f);
        
        //通知Dropdown_Services更新当前选择的lobby
        if (Dropdown_Services.Instance != null)
        {
            Dropdown_Services.Instance.SetCurrentSelectedCharacter(character_name);
        }

        if (Camera_Services.Instance != null)
        {
            Camera_Services.Instance.Save_Character_Selection();
        }

        if (Fade_Services.Instance != null)
        {
            Debug.Log($"[Character_Services] 执行淡入效果");
            yield return StartCoroutine(Fade_Services.Instance.FadeIn(0.2f));
        }
        
        Debug.Log($"[Character_Services] 角色切换完成: {character_name}");
    }

    private void EnsureFadeServices()
    {
        if (Fade_Services.Instance == null)
        {
            GameObject fadeObj = new GameObject("Fade_Services");
            fadeObj.AddComponent<Fade_Services>();
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
