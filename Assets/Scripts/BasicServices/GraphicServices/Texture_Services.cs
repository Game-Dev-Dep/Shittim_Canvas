using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class Texture_Services : MonoBehaviour
{
    private void Start()
    {
        Load_Lobbyillust();
    }

    public static Dictionary<string, string> Lobbyillust = new Dictionary<string, string>();
    /// <summary>
    /// �������еĴ����廭
    /// </summary>
    public static void Load_Lobbyillust()
    {
        Debug.Log("��ʼ�������� Lobbyillust ��Ϣ");

        Lobbyillust = File_Services.Load_Specific_Type_From_File<Dictionary<string, string>>(Path.Combine(File_Services.Student_Lists_Folder_Path, "Lobbyillust.json"));

        Debug.Log("������������ Lobbyillust ��Ϣ");
    }

    public static Texture2D Get_Texture_By_Path(string path)
    {
        if (File.Exists(path))
        {
            byte[] texture_bytes = File.ReadAllBytes(path);
            Texture2D texture2d = new Texture2D(2, 2); // ��ʱ�ߴ�
            if (texture2d.LoadImage(texture_bytes)) // �Զ�ʶ��PNG/JPG�ȸ�ʽ
            {
                Console_Log("������سɹ�: " + path, Debug_Services.LogLevel.Ignore);
                return texture2d;
            }
        }
        Console_Log($"�ļ������ڻ�����ЧͼƬ: {path}", Debug_Services.LogLevel.Debug, LogType.Error);
        return null;
    }
    public static IEnumerator Get_Texture_By_Path_Async(string texture_file_path, System.Action<Texture2D> onLoaded)
    {
        string fullPath = Path.Combine("file:///", texture_file_path);
        string local_texture2d_name = Path.GetFileName(texture_file_path);
        Console_Log($"���� Texture {local_texture2d_name} ����·��: {fullPath}");
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(fullPath))
        {
            yield return request.SendWebRequest();
            if (request.result == UnityWebRequest.Result.Success)
            {
                Texture2D texture2d = DownloadHandlerTexture.GetContent(request);
                Console_Log($"���� Texture {local_texture2d_name} ���سɹ�");
                onLoaded?.Invoke(texture2d);
            }
            else
            {
                Console_Log($"����Texture {local_texture2d_name} ����ʧ��: {request.error}", Debug_Services.LogLevel.Debug, LogType.Error);
                onLoaded?.Invoke(null);
            }
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Texture_Services", message, loglevel, logtype); }
}
