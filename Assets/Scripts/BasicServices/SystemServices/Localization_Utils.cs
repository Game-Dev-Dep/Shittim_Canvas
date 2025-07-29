using UnityEngine;
using UnityEngine.Localization;
using TMPro;

/// <summary>
/// 欢迎来到给代码用的本地化工具类！
/// 不要跟Unity里面那个Localization_To_TMP搞混力，这是俩不同的玩意！
/// 其他地方用请先调用Apply_Localization_To_Text方法，然后使用Get_Localized_Text方法获取本地化文本。
/// 谢谢喵！
/// </summary>
public static class Localization_Utils
{
    public static void Apply_Localization_To_Text(TextMeshProUGUI textComponent, string localizationKey)
    {
        if (textComponent == null || string.IsNullOrEmpty(localizationKey))
            return;

        // 创建本地化字符串
        var localizedString = new LocalizedString("UI Text", localizationKey);
        
        // 注册事件并立即更新
        localizedString.StringChanged += (value) => {
            if (textComponent != null)
            {
                // 如果翻译为空或与键值相同，使用键值作为后备
                if (string.IsNullOrEmpty(value) || value == localizationKey)
                {
                    textComponent.text = localizationKey;
                }
                else
                {
                    textComponent.text = value;
                }
            }
        };
        
        // 立即获取并设置文本
        string localizedText = localizedString.GetLocalizedString();
        if (string.IsNullOrEmpty(localizedText) || localizedText == localizationKey)
        {
            textComponent.text = localizationKey;
        }
        else
        {
            textComponent.text = localizedText;
        }
    }

    // 获取本地化文本
    public static string Get_Localized_Text(string localizationKey)
    {
        if (string.IsNullOrEmpty(localizationKey))
            return localizationKey;

        var localizedString = new LocalizedString("UI Text", localizationKey);
        string localizedText = localizedString.GetLocalizedString();
        
        if (string.IsNullOrEmpty(localizedText) || localizedText == localizationKey)
        {
            return localizationKey;
        }
        else
        {
            return localizedText;
        }
    }

    // 检查是否是本地化键值
    public static bool Is_Localization_Key(string text)
    {
        return !string.IsNullOrEmpty(text) && (text.StartsWith("settings_panel.") || text.StartsWith("toast.") || text.StartsWith("function_area."));
    }
} 