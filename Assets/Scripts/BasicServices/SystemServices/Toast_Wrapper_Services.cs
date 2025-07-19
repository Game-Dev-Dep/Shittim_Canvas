using UnityEngine;

/// <summary>
/// 这个主要是用来给Toast Notification Message用的Wrapper，防止编译错误，Shittim Canvas和MX还不吃直接import那一套！
/// </summary>
public static class Toast_Wrapper_Services
{
    public static void ShowToast(string message, float duration = 3f, string icon = "success")
    {
        try
        {
            // 插件提供的 ToastNotification.Show
            var toastType = System.Type.GetType("ToastNotification");
            if (toastType != null)
            {
                var showMethod = toastType.GetMethod("Show", new System.Type[] { typeof(string), typeof(float), typeof(string) });
                if (showMethod != null)
                {
                    showMethod.Invoke(null, new object[] { message, duration, icon });
                    return;
                }
            }
            
            // 如果炸了就直接GameObject（Hierarchy里名字记得别改，要不然这玩意就没用了）
            var toastObject = GameObject.Find("ToastNotificationMessage");
            if (toastObject != null)
            {
                var toastComponent = toastObject.GetComponent<MonoBehaviour>();
                if (toastComponent != null)
                {
                    var showMethod = toastComponent.GetType().GetMethod("Show", new System.Type[] { typeof(string), typeof(float), typeof(string) });
                    if (showMethod != null)
                    {
                        showMethod.Invoke(toastComponent, new object[] { message, duration, icon });
                        return;
                    }
                }
            }
            
            // 如果全炸了就看log罢
            Debug.Log($"[Toast_Wrapper_Services] {message}");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"Toast failed: {ex.Message}");
            Debug.Log($"[Toast_Wrapper_Services] {message}");
        }
    }
} 