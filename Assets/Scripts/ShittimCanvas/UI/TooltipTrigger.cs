using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Tooltip触发器，用于在UI元素上触发Tooltip显示
/// </summary>
public class TooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Tooltip Content")]
    [Tooltip("Tooltip标题的本地化键")]
    public string titleKey = "";
    
    [Tooltip("Tooltip描述的本地化键")]
    public string descriptionKey = "";
    
    [Header("Tooltip Settings")]
    [Tooltip("是否启用Tooltip")]
    public bool enableTooltip = true;
    
    [Tooltip("自定义显示位置（可选）")]
    public Vector3 customPosition = Vector3.zero;
    
    [Tooltip("是否使用自定义位置")]
    public bool useCustomPosition = false;
    
    private void Start()
    {
        // 验证Tooltip服务是否存在
        if (Tooltips_Services.Instance == null)
        {
            Debug.LogWarning("[Tooltip Trigger] Tooltips_Services未找到！请确保有用Tooltips_Services组件。");
        }
    }
    
    /// <summary>
    /// 鼠标进入事件
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!enableTooltip || Tooltips_Services.Instance == null) return;
        
        // 确定显示位置
        Vector3 position = useCustomPosition ? customPosition : eventData.position;
        
        // 显示Tooltip
        Tooltips_Services.Instance.ShowTooltip(titleKey, descriptionKey, position);
    }
    
    /// <summary>
    /// 鼠标离开事件
    /// </summary>
    /// <param name="eventData">指针事件数据</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!enableTooltip || Tooltips_Services.Instance == null) return;
        
        // 隐藏Tooltip
        Tooltips_Services.Instance.HideTooltip();
    }
    
    /// <summary>
    /// 动态设置Tooltip内容
    /// </summary>
    /// <param name="newTitleKey">新的标题键</param>
    /// <param name="newDescriptionKey">新的描述键</param>
    public void SetTooltipContent(string newTitleKey, string newDescriptionKey)
    {
        titleKey = newTitleKey;
        descriptionKey = newDescriptionKey;
    }
    
    /// <summary>
    /// 启用/禁用Tooltip
    /// </summary>
    /// <param name="enabled">是否启用</param>
    public void SetTooltipEnabled(bool enabled)
    {
        enableTooltip = enabled;
        
        // 如果禁用且当前正在显示，则隐藏Tooltip
        if (!enabled && Tooltips_Services.Instance != null)
        {
            Tooltips_Services.Instance.HideTooltip();
        }
    }
    
    /// <summary>
    /// 设置自定义位置
    /// </summary>
    /// <param name="position">自定义位置</param>
    /// <param name="useCustom">是否使用自定义位置</param>
    public void SetCustomPosition(Vector3 position, bool useCustom = true)
    {
        customPosition = position;
        useCustomPosition = useCustom;
    }
} 