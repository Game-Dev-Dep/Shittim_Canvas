using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CoreFilesValidation_Services : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject fileCheckBackground;
    [SerializeField] private Button openRootFolderButton;
    [SerializeField] private Button restartButton;
    
    private void Start()
    {
        StartCoroutine(CheckCoreFilesCoroutine());
        
        if (openRootFolderButton != null)
        {
            openRootFolderButton.onClick.AddListener(OpenRootFolder);
        }
        
        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartApplication);
        }
    }
    
    private System.Collections.IEnumerator CheckCoreFilesCoroutine()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (!ValidateCoreFiles())
        {
            ShowCoreFilesMissingUI();
        }
    }
    
    private bool ValidateCoreFiles()
    {
        string rootPath = File_Services.Root_Folder_Path;
        
        if (!Directory.Exists(rootPath))
        {
            Debug.LogError("[CoreFilesValidation_Services]Core Files文件夹不存在");
            return false;
        }

        string[] requiredFolders = {
            File_Services.Config_Files_Folder_Path,
            File_Services.Student_Files_Folder_Path,
            File_Services.Student_Lists_Folder_Path
        };

        foreach (string folder in requiredFolders)
        {
            if (!Directory.Exists(folder))
            {
                Debug.LogError($"[CoreFilesValidation_Services]关键文件夹缺失: {folder}");
                return false;
            }
        }

        string[] requiredFiles = {
            Path.Combine(File_Services.Student_Lists_Folder_Path, "Lobbyillust.json"),
            Path.Combine(File_Services.Config_Files_Folder_Path, "Setting Config.json")
        };

        foreach (string file in requiredFiles)
        {
            if (!File.Exists(file))
            {
                Debug.LogError($"[CoreFilesValidation_Services]关键配置文件缺失: {file}");
                return false;
            }
        }

        Debug.Log("[CoreFilesValidation_Services]Core Files验证通过");
        return true;
    }
    
    private void ShowCoreFilesMissingUI()
    {
        if (fileCheckBackground != null)
        {
            fileCheckBackground.SetActive(true);
            Debug.LogWarning("[CoreFilesValidation_Services]显示Core Files缺失提示UI");
        }
        else
        {
            Debug.LogError("[CoreFilesValidation_Services]File Check Background UI未设置");
        }
    }
    
    public void OpenRootFolder()
    {
        string rootPath = File_Services.Root_Folder_Path;
        
        if (Directory.Exists(rootPath))
        {
            try
            {
                #if UNITY_STANDALONE_WIN
                System.Diagnostics.Process.Start("explorer.exe", rootPath);
                #elif UNITY_STANDALONE_OSX
                System.Diagnostics.Process.Start("open", rootPath);
                #elif UNITY_STANDALONE_LINUX
                System.Diagnostics.Process.Start("xdg-open", rootPath);
                #else
                Debug.Log($"[CoreFilesValidation_Services]Core Files路径: {rootPath}");
                #endif
                
                Debug.Log($"[CoreFilesValidation_Services]已打开Core Files文件夹: {rootPath}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[CoreFilesValidation_Services]打开文件夹失败: {ex.Message}");
                Debug.Log($"[CoreFilesValidation_Services]Core Files路径: {rootPath}");
            }
        }
        else
        {
            Debug.LogWarning("[CoreFilesValidation_Services]Core Files文件夹不存在，无法打开");
        }
    }
    
    public void RestartApplication()
    {
        Debug.Log("开始重启应用程序");
        
        try
        {
            //各个系统下的Shittim Canvas重启的逻辑，万一以后支持了呢（
            #if UNITY_STANDALONE_WIN
            string[] possiblePaths = {
                Application.dataPath.Replace("/Assets", ".exe"),
                Application.dataPath.Replace("\\Assets", ".exe"),
                Path.Combine(Path.GetDirectoryName(Application.dataPath), Path.GetFileNameWithoutExtension(Application.dataPath) + ".exe"),
                GetExecutablePath()
            };
            
            string exePath = null;
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    exePath = path;
                    Debug.Log($"[CoreFilesValidation_Services]找到可执行文件: {exePath}");
                    break;
                }
            }
            
            if (exePath != null)
            {
                Debug.Log($"[CoreFilesValidation_Services]正在启动: {exePath}");
                System.Diagnostics.Process.Start(exePath);
                Debug.Log("[CoreFilesValidation_Services]新进程已启动，准备退出当前应用");
                Application.Quit();
            }
            else
            {
                Debug.LogError("[CoreFilesValidation_Services]未找到可执行文件，尝试的路径:");
                foreach (string path in possiblePaths)
                {
                    Debug.LogError($"  {path}");
                }
                TryAlternativeRestart();
            }
            #elif UNITY_STANDALONE_OSX
            string appPath = Application.dataPath.Replace("/Contents", "");
            if (Directory.Exists(appPath))
            {
                Debug.Log($"[CoreFilesValidation_Services]正在启动macOS应用: {appPath}");
                System.Diagnostics.Process.Start("open", appPath);
                Debug.Log("[CoreFilesValidation_Services]新进程已启动，准备退出当前应用");
                Application.Quit();
            }
            else
            {
                Debug.LogError($"[CoreFilesValidation_Services]未找到应用程序包: {appPath}");
            }
            #elif UNITY_STANDALONE_LINUX
            string exePath = Application.dataPath.Replace("/Assets", "");
            if (File.Exists(exePath))
            {
                Debug.Log($"[CoreFilesValidation_Services]正在启动Linux应用: {exePath}");
                System.Diagnostics.Process.Start(exePath);
                Debug.Log("[CoreFilesValidation_Services]新进程已启动，准备退出当前应用");
                Application.Quit();
            }
            else
            {
                Debug.LogError($"[CoreFilesValidation_Services]未找到可执行文件: {exePath}");
            }
            #else
            Debug.Log("[CoreFilesValidation_Services]当前平台不支持重启功能");
            #endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CoreFilesValidation_Services]重启失败: {ex.Message}");
            Debug.LogError($"[CoreFilesValidation_Services]异常堆栈: {ex.StackTrace}");
        }
    }
    
    private string GetExecutablePath()
    {
        try
        {
            var currentProcess = System.Diagnostics.Process.GetCurrentProcess();
            if (currentProcess != null && !string.IsNullOrEmpty(currentProcess.MainModule?.FileName))
            {
                return currentProcess.MainModule.FileName;
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[CoreFilesValidation_Services]无法获取当前进程路径: {ex.Message}");
        }
        
        string dataPath = Application.dataPath;
        string projectName = Path.GetFileNameWithoutExtension(dataPath);
        if (string.IsNullOrEmpty(projectName) || projectName == "Assets")
        {
            projectName = "Shittim Canvas";
        }
        
        string exePath = Path.Combine(Path.GetDirectoryName(dataPath), projectName + ".exe");
        return exePath;
    }
    
    // 部分电脑尤其是Windows电脑，在重启时会遇到一些问题，这玩意备用，不是很优雅，但能用嘻嘻
    private void TryAlternativeRestart()
    {
        Debug.Log("[CoreFilesValidation_Services]尝试备选重启方案");
        
        try
        {
            #if UNITY_STANDALONE_WIN
            string batchPath = Path.Combine(Path.GetTempPath(), "restart_shittim.bat");
            string exeName = Path.GetFileName(GetExecutablePath());
            if (string.IsNullOrEmpty(exeName))
            {
                exeName = "Shittim Canvas.exe";
            }
            
            string batchContent = $@"@echo off
timeout /t 1 /nobreak >nul
start """" ""{exeName}""
del ""%~f0""";
            
            File.WriteAllText(batchPath, batchContent);
            Debug.Log($"[CoreFilesValidation_Services]创建批处理文件: {batchPath}");
            
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = $"/c {batchPath}";
            startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
            
            System.Diagnostics.Process.Start(startInfo);
            Debug.Log("[CoreFilesValidation_Services]批处理文件已启动，准备退出当前应用");
            Application.Quit();
            #endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[CoreFilesValidation_Services]备选重启方案失败: {ex.Message}");
        }
    }
}
