using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;

public class File_Services : MonoBehaviour
{
    public static string Root_Folder_Path
    {
        get
        {
            #if UNITY_EDITOR
            return Path.Combine(Application.persistentDataPath, "Core Files");
            #else
            return Path.Combine(Path.GetDirectoryName(Application.dataPath), "Core Files");
            #endif
        }
    }

    // ===== Config Files ===== \\
    public static string Config_Files_Folder_Path => Path.Combine(Root_Folder_Path, "Config Files");

    // ===== MX Files ===== \\
    public static string MX_Files_Folder_Path => Path.Combine(Root_Folder_Path, "MX Files");
    public static string MX_Files_AssetBundles_Folder_Path => Path.Combine(MX_Files_Folder_Path, "AssetBundles");
    public static string MX_Files_Textures_Folder_Path => Path.Combine(MX_Files_AssetBundles_Folder_Path, "Textures");
    public static string MX_Files_MediaResources_Folder_Path => Path.Combine(MX_Files_Folder_Path, "MediaResources");
    public static string MX_Files_TableBundles_Folder_Path => Path.Combine(MX_Files_Folder_Path, "TableBundles");

    // ===== Student Files ===== \\
    public static string Student_Files_Folder_Path => Path.Combine(Root_Folder_Path, "Student Files");
    public static string Student_Textures_Folder_Path => Path.Combine(Student_Files_Folder_Path, "Textures");

    // ===== Student Lists ===== \\
    public static string Student_Lists_Folder_Path => Path.Combine(Root_Folder_Path, "Student Lists");
    
    
    private void Start()
    {
        Console_Log($"开始初始化文件系统");

        Create_Default_Directories();
        Create_Default_Files();

        Check_Student_Files_Folder();

        Console_Log($"结束初始化文件系统");
    }

    /// <summary>
    /// 校验 Student Files 文件夹结构
    /// </summary>
    private static void Check_Student_Files_Folder()
    {
        Console_Log($"开始校验 Student Files 文件夹结构");

        string Bundle_Files_Structure_File_Path = Path.Combine(Student_Files_Folder_Path, "Bundle Files Structure.json");
        string Bundle_Files_Structure_File_JSON = File.ReadAllText(Bundle_Files_Structure_File_Path);
        List<string> Bundle_Files_Structure_List = JsonConvert.DeserializeObject<List<string>>(Bundle_Files_Structure_File_JSON);
        List<string> bundle_file_paths = Directory.GetFiles(Student_Files_Folder_Path, "*.bundle", SearchOption.AllDirectories).ToList();

        Console_Log($"标准结构应该有 {Bundle_Files_Structure_List.Count} 个 Bundle 文件，本地有 {bundle_file_paths.Count} 个 Bundle 文件");

        foreach (string bundle_file_path in bundle_file_paths)
        {
            string bundle_file_path_temp = bundle_file_path.Replace(Student_Files_Folder_Path, "");
            if (!Bundle_Files_Structure_List.Contains(bundle_file_path_temp))
            {
                Console_Log($"文件 {Path.GetFileName(bundle_file_path)} 多余", Debug_Services.LogLevel.Info, LogType.Warning);
                File.Delete(bundle_file_path);
                if(!File.Exists(bundle_file_path)) Console_Log($"文件 {Path.GetFileName(bundle_file_path)} 已删除");
            }
        }
        foreach (string standard_bundle_file_path in Bundle_Files_Structure_List)
        {
            string standard_bundle_file_path_temp = Student_Files_Folder_Path + standard_bundle_file_path;
            if (!bundle_file_paths.Contains(standard_bundle_file_path_temp))
            {
                Console_Log($"文件 {Path.GetFileName(standard_bundle_file_path)} 缺失", Debug_Services.LogLevel.Info, LogType.Warning);
            }
        }

        Console_Log($"结束校验 Student Files 文件夹结构"); 
    }

    /// <summary>
    /// 创建默认目录
    /// </summary>
    private static void Create_Default_Directories()
    {
        Console_Log($"开始创建默认目录");

        Create_Directory(Config_Files_Folder_Path);
        Create_Directory(MX_Files_Folder_Path);
        Create_Directory(MX_Files_AssetBundles_Folder_Path);
        Create_Directory(MX_Files_Textures_Folder_Path);
        Create_Directory(MX_Files_MediaResources_Folder_Path);
        Create_Directory(MX_Files_TableBundles_Folder_Path);
        Create_Directory(Student_Files_Folder_Path);
        Create_Directory(Student_Textures_Folder_Path);
        Create_Directory(Student_Lists_Folder_Path);

        Console_Log($"结束创建默认目录");
    }

    /// <summary>
    /// 创建完整的指定路径
    /// </summary>
    /// <param name="path">路径</param>
    private static void Create_Directory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            Console_Log($"创建目录: {path}");
        }
    }

    /// <summary>
    /// 创建默认文件
    /// </summary>
    private static void Create_Default_Files()
    {
        Console_Log($"开始创建默认文件");

        if (!File.Exists(Path.Combine(Config_Files_Folder_Path, "Function Config.json")))
        {
            Save_Default_Type_To_File<Function_Config>(Path.Combine(Config_Files_Folder_Path, "Function Config.json"));
        }

        if (!File.Exists(Path.Combine(Config_Files_Folder_Path, "MemoryLobby Camera Config.json")))
        {
            Save_Default_Type_To_File<Camera_Config>(Path.Combine(Config_Files_Folder_Path, "MemoryLobby Camera Config.json"));
        }

        if (!File.Exists(Path.Combine(Config_Files_Folder_Path, "WindowFilter Config.json")))
        {
            Save_Default_Type_To_File<WindowFilter_Config>(Path.Combine(Config_Files_Folder_Path, "WindowFilter Config.json"));
        }

        Console_Log($"结束创建默认文件");
    }

    public static T Load_Specific_Type_From_File<T>(string file_path)
    {
        if (!File.Exists(file_path))
        {
            Console_Log($"{typeof(T).Name} 类型的文件: {file_path} 不存在", Debug_Services.LogLevel.Debug, LogType.Error);
            return default;
        }
        string json = File.ReadAllText(file_path);
        return JsonConvert.DeserializeObject<T>(json);
    }

    public static void Save_Default_Type_To_File<T>(string file_path) where T : new()
    {
        T config = new T();
        string json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(file_path, json);

        Debug.Log($"已创建 {typeof(T).Name} 类型的默认文件: {file_path}");
    }

    public static void Save_Specific_Type_To_File<T>(T config, string file_path)
    {
        string json = JsonConvert.SerializeObject(config, Formatting.Indented);
        File.WriteAllText(file_path, json);

        Debug.Log($"已写入 {typeof(T).Name} 类型的文件: {file_path}");
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("File_Services", message, loglevel, logtype); }
}
