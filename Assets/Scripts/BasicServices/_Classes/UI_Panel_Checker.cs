using UnityEngine;

public static class UI_Panel_Checker
{
    public static bool IsAnyUIPanelOpen()
    {
        var settingPanel = GameObject.Find("[Setting] Root");
        if (settingPanel != null && settingPanel.activeSelf) return true;

        if (OOBE_Services.Instance != null && OOBE_Services.Instance.is_OOBE_On) return true;

        var multiLobbyPanel = GameObject.Find("[Multi Lobby] Root");
        if (multiLobbyPanel != null && multiLobbyPanel.activeSelf) return true;

        var characterListPanel = GameObject.Find("[Character List] Root");
        if (characterListPanel != null && characterListPanel.activeSelf) return true;

        return false;
    }
}
