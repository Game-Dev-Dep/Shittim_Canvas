using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

[RequireComponent(typeof(TextMeshProUGUI))]
public class Localization_To_TMP : MonoBehaviour
{

    [Tooltip("本地化键值（在String Table中的键）")]
    public string Localization_Key = "key_name";

    [Tooltip("是否在Awake时立即更新文本")]
    public bool is_Awake_Update_On = true;

    private TextMeshProUGUI TMP_Component;
    private LocalizedString Localized_String = new LocalizedString();
    private bool is_Manually_Set = false; // 标记是否被手动设置过
    private string last_Manual_Text = ""; // 记录最后手动设置的文本

    private void Awake()
    {
        // 获取TextMeshPro组件
        TMP_Component = GetComponent<TextMeshProUGUI>();
        if (TMP_Component == null)
        {
            Debug.LogError("TextMeshProUGUI组件未找到！", this);
            return;
        }

        // 设置本地化键值
        Localized_String.TableReference = "UI Text"; // 默认表名，可在Inspector中修改
        Localized_String.TableEntryReference = Localization_Key;

        // 注册本地化变更事件
        Localized_String.StringChanged += Update_Text;

        if (is_Awake_Update_On)
        {
            // 立即更新文本
            Update_Text(Localized_String.GetLocalizedString());
        }
    }
    private void OnEnable()
    {
        // 重新启用时，如果之前被手动设置过，则恢复手动设置的文本
        if (is_Manually_Set && !string.IsNullOrEmpty(last_Manual_Text))
        {
            if (TMP_Component != null)
            {
                TMP_Component.text = last_Manual_Text;
            }
        }
        else
        {
            // 否则使用本地化文本
            Update_Text(Localized_String.GetLocalizedString());
        }
    }

    private void OnDestroy()
    {
        // 注销事件，防止内存泄漏
        Localized_String.StringChanged -= Update_Text;
    }


    /// <summary>
    /// 更新TMP文本内容
    /// </summary>
    private void Update_Text(string translated_value)
    {
        if (TMP_Component != null && !is_Manually_Set)
        {
            TMP_Component.text = translated_value;
        }
    }

    /// <summary>
    /// 动态更改本地化键值
    /// </summary>
    public void Set_Localization_Key(string key_value)
    {
        Localization_Key = key_value;
        Localized_String.TableEntryReference = key_value;
        Update_Text(Localized_String.GetLocalizedString());
    }

    /// <summary>
    /// 刷新文本（当语言变更时调用）
    /// </summary>
    public void Refresh_Text()
    {
        Update_Text(Localized_String.GetLocalizedString());
    }

    /// <summary>
    /// 手动设置文本，这会覆盖本地化文本
    /// </summary>
    public void Set_Manual_Text(string text)
    {
        if (TMP_Component != null)
        {
            TMP_Component.text = text;
            is_Manually_Set = true;
            last_Manual_Text = text;
        }
    }

    /// <summary>
    /// 重置为本地化文本
    /// </summary>
    public void Reset_To_Localized_Text()
    {
        is_Manually_Set = false;
        last_Manual_Text = "";
        Update_Text(Localized_String.GetLocalizedString());
    }
}
