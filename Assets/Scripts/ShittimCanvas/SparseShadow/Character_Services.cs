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
#if !UNITY_EDITOR
        character = gameObject.AddComponent<Character>();
        character.Load_Charachter(Character_Name);
#endif
    }

    public void Switch_Character(string character_name)
    {
        if(character != null) character.Unload_Character();
        
        character = gameObject.AddComponent<Character>();
        character.Load_Charachter(character_name);
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
