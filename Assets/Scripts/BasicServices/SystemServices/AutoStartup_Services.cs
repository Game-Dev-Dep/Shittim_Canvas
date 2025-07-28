using Microsoft.Win32;
using System;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.UI;

public class AutoStartup_Services : MonoBehaviour
{
    public static AutoStartup_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            UnityEngine.Debug.Log("[Awake] AutoStartup Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private bool is_AutoStartup_On = false;

    private const string REGISTRY_KEY = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string APP_NAME = "ShittimCanvas";

    private void Get_Config()
    {
        is_AutoStartup_On = Config_Services.Instance.Global_Setting_Config.General.Auto_Startup == 0 ? true : false;
    }

    void Start()
    {
        Console_Log("开始初始化 AutoStartup Services");

        Get_Config();

        Console_Log("结束初始化 AutoStartup Services");
    }

    public void Enable_AutoStartup()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
            {
                string appPath = Process.GetCurrentProcess().MainModule.FileName;
                key.SetValue(APP_NAME, "\"" + appPath + "\"");
                is_AutoStartup_On = true;
                Console_Log("已启用注册表自启动");
            }
        }
        catch (Exception e)
        {
            is_AutoStartup_On = false;
            Console_Log("注册表操作失败: " + e.Message);
        }
    }

    public void Disable_AutoStartup()
    {
        try
        {
            using (RegistryKey key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY, true))
            {
                key.DeleteValue(APP_NAME, false);
                is_AutoStartup_On = false;
                Console_Log("已禁用注册表自启动");
            }
        }
        catch (Exception e)
        {
            is_AutoStartup_On = false;
            Console_Log("注册表操作失败: " + e.Message);
        }
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("AutoStartup_Services", message, loglevel, logtype); }
}
