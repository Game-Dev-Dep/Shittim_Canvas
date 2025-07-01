using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;

public class Debug_Services : MonoBehaviour
{
    public static Debug_Services Instance { get; set; }
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug_Area.SetActive(is_Debug);
            Debug.Log("[Awake] Debug Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool is_Debug = false;

    public bool is_Show_Level_Ignore = false;
    public bool is_Show_Level_Info = true;
    public bool is_Show_Level_Debug = true;
    public bool is_Show_Level_Core = false;

    public GameObject Debug_Area;
    public TextMeshProUGUI Debug_Info_Text_01;
    public TextMeshProUGUI Debug_Info_Text_02;
    public TextMeshProUGUI Debug_Info_Text_03;
    public TextMeshProUGUI Debug_Info_Text_04;
    public TextMeshProUGUI Debug_Info_Text_05;
    public TextMeshProUGUI Debug_Info_Text_06;

    public enum LogLevel
    {
        Ignore,
        Info,
        Debug,
        Core,
    }

    public void Update()
    {
        if (is_Debug)
        {
            GPU_Info();
            Memory_Info();
        }
    }

    public void Console_Log(string title, string message, LogLevel loglevel = LogLevel.Info, LogType logtype = LogType.Log)
    {
        if (
            (loglevel == LogLevel.Ignore && is_Show_Level_Ignore) ||
            (loglevel == LogLevel.Info && is_Show_Level_Info) ||
            (loglevel == LogLevel.Debug && is_Show_Level_Debug) ||
            (loglevel == LogLevel.Core && is_Show_Level_Core)
           )
        {
            string time_stamp = DateTime.Now.ToString("HH:mm:ss.fff");
            string formatted_message = $"[{time_stamp}][{loglevel.ToString()}][{title}] {message}";
            switch (logtype)
            {
                case LogType.Error:
                    Debug.LogError(formatted_message);
                    break;

                case LogType.Warning:
                    Debug.LogWarning(formatted_message);
                    break;

                case LogType.Log:
                default:
                    Debug.Log(formatted_message);
                    break;
            }
        }
    }

    public void GPU_Info()
    {
        Debug_Info_Text_02.SetText(
        $"GPU Info\n" +
        $"FPS: {1.0f / Time.deltaTime:F1}\n"+
        $"GPU: {SystemInfo.graphicsDeviceName}\n" +
        $"API: {SystemInfo.graphicsDeviceType}\n" +
        $"VRAM: {SystemInfo.graphicsMemorySize} MB\n"
#if UNITY_EDITOR
         + 
        $"Draw Calls: {UnityStats.drawCalls}\n" +
        $"Batches: {UnityStats.batches}\n" +
        $"SetPass Calls: {UnityStats.setPassCalls}\n" +
        $"Tris: {UnityStats.triangles}\n" +
        $"Verts: {UnityStats.vertices}\n"
#endif
        );
    }

    public void Memory_Info()
    {
        Debug_Info_Text_03.SetText(
        $"Memory Info\n" +
        $"Total Memory: {System.GC.GetTotalMemory(false) / 1024 / 1024} MB\n" +
        $"Used Heap: {Profiler.GetTotalAllocatedMemoryLong() / 1024 / 1024} MB\n" +
        $"Reserved Heap: {Profiler.GetTotalReservedMemoryLong() / 1024 / 1024} MB\n" +
        $"Texture Memory: {Profiler.GetAllocatedMemoryForGraphicsDriver() / 1024 / 1024} MB\n" +
        $"Mono Heap: {Profiler.GetMonoHeapSizeLong() / 1024} KB\n" +
        $"Mono Used: {Profiler.GetMonoUsedSizeLong() / 1024} KB"
        );
    }
}
