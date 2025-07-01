using System;
using System.Collections.Generic;
using UnityEngine;
using Utils;

public class SystemTray_Services : MonoBehaviour
{
    [SerializeField]
    public Texture2D SystemTray_Icon;

    void Awake()
    {
#if !UNITY_EDITOR
        Debug.Log($"��ʼ��ʼ��ϵͳ����");

        List<(string, Action)> SystemTray_Menu = new List<(string, Action)>()
        {
            ("�����ֽģʽ", Enter_Wallpaper_Mode),
            ("��������ģʽ", Quit_Wallpaper_Mode),
            (TrayIcon.SEPARATOR, null),
            ("�˳�", Quit_Program)
        };

        TrayIcon.Init("ShittimCanvas", "Shittim Canvas", SystemTray_Icon, SystemTray_Menu);

        Debug.Log($"������ʼ��ϵͳ����");
#endif
    }
    private void Enter_Wallpaper_Mode()
    {
        Debug.Log($"ϵͳ���̴���: �����ֽģʽ");

        if (!Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Wallpaper_Mode()
    {
        Debug.Log($"ϵͳ���̴���: ��������ģʽ");
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            Wallpaper_Services.Instance.Toggle_Wallpaper_Mode();
        }
        else return;
    }
    private void Quit_Program()
    {
        Debug.Log("ϵͳ���̴���: �˳�");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
