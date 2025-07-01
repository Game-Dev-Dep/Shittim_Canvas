using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class TrayDemo : MonoBehaviour
{
    [SerializeField]
    private Texture2D icon;
    [SerializeField]
    private string iconName;
    [SerializeField]
    private Image demoImage;

    private void Greet() { Debug.Log("Icon Left Clicked!"); }
    private void TurnImageRed() { demoImage.color = Color.red; }
    private void TurnImageBlue() { demoImage.color = Color.blue; }

    private void Stop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Awake()
    {
        var context = new List<(string, Action)>() {
            (TrayIcon.LEFT_CLICK, Greet),
            ("Turn Image Red", TurnImageRed),
            ("Turn Image Blue", TurnImageBlue),
            (TrayIcon.SEPARATOR, null),
            ("Quit", Stop)
        };

        TrayIcon.Init("Demo", iconName, icon, context);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            TrayIcon.ShowBalloonTip("Pop Up!", "You pressed Space!", TrayIcon.ToolTipIcon.Info);
    }
}
