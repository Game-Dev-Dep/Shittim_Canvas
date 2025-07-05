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
        // 重新启用时更新文本
        Update_Text(Localized_String.GetLocalizedString());
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
        if (TMP_Component != null)
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
}
