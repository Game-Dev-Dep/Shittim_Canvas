using UnityEngine;
using UnityEngine.Localization;
using TMPro;
using System;
using System.Collections.Generic;

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

    /// <summary>
    /// 增强的搜索功能，支持多语言搜索
    /// </summary>
    /// <param name="searchTerm">搜索关键词</param>
    /// <param name="searchableItems">可搜索的项目列表</param>
    /// <param name="getLocalizedText">获取本地化文本的委托</param>
    /// <returns>匹配的项目列表</returns>
    public static List<T> SearchWithLocalization<T>(
        string searchTerm, 
        List<T> searchableItems, 
        Func<T, string> getLocalizedText,
        Func<T, string> getOriginalText = null)
    {
        if (string.IsNullOrEmpty(searchTerm) || searchableItems == null)
            return searchableItems;

        var results = new List<T>();
        string searchTermLower = searchTerm.ToLower();

        foreach (var item in searchableItems)
        {
            string localizedText = getLocalizedText(item);
            string originalText = getOriginalText?.Invoke(item) ?? localizedText;

            // 转换为小写进行比较
            string localizedLower = localizedText.ToLower();
            string originalLower = originalText.ToLower();

            // 精确匹配
            if (localizedLower == searchTermLower || originalLower == searchTermLower)
            {
                results.Add(item);
                continue;
            }

            // 前缀匹配
            if (localizedLower.StartsWith(searchTermLower) || originalLower.StartsWith(searchTermLower))
            {
                results.Add(item);
                continue;
            }

            // 模糊匹配
            if (localizedLower.Contains(searchTermLower) || originalLower.Contains(searchTermLower))
            {
                results.Add(item);
                continue;
            }
        }

        return results;
    }

    /// <summary>
    /// 获取所有可用的本地化键值
    /// </summary>
    /// <returns>本地化键值列表</returns>
    public static List<string> GetAllLocalizationKeys()
    {
        var keys = new List<string>();
        
        // 这里可以根据你的本地化系统获取所有键值
        // 由于Unity的本地化系统比较复杂，这里提供一个基础框架
        // 你可以根据实际需要扩展这个方法
        
        return keys;
    }

    /// <summary>
    /// 搜索本地化键值
    /// </summary>
    /// <param name="searchTerm">搜索关键词</param>
    /// <returns>匹配的键值列表</returns>
    public static List<string> SearchLocalizationKeys(string searchTerm)
    {
        if (string.IsNullOrEmpty(searchTerm))
            return new List<string>();

        var allKeys = GetAllLocalizationKeys();
        var results = new List<string>();
        string searchTermLower = searchTerm.ToLower();

        foreach (var key in allKeys)
        {
            string keyLower = key.ToLower();
            string localizedValue = Get_Localized_Text(key).ToLower();

            // 搜索键值本身
            if (keyLower.Contains(searchTermLower))
            {
                results.Add(key);
                continue;
            }

            // 搜索本地化内容
            if (localizedValue.Contains(searchTermLower))
            {
                results.Add(key);
                continue;
            }
        }

        return results;
    }

    /// <summary>
    /// 获取本地化文本的搜索建议
    /// </summary>
    /// <param name="partialKey">部分键值</param>
    /// <param name="maxSuggestions">最大建议数量</param>
    /// <returns>搜索建议列表</returns>
    public static List<string> GetLocalizationSuggestions(string partialKey, int maxSuggestions = 10)
    {
        if (string.IsNullOrEmpty(partialKey))
            return new List<string>();

        var allKeys = GetAllLocalizationKeys();
        var suggestions = new List<string>();
        string partialKeyLower = partialKey.ToLower();

        foreach (var key in allKeys)
        {
            if (suggestions.Count >= maxSuggestions)
                break;

            string keyLower = key.ToLower();
            if (keyLower.StartsWith(partialKeyLower) || keyLower.Contains(partialKeyLower))
            {
                suggestions.Add(key);
            }
        }

        return suggestions;
    }
} 