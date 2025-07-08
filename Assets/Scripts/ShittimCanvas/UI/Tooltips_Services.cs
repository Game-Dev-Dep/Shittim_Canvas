using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

/// <summary>
/// Tooltip Services，用于管理多语言Tooltip的显示和隐藏
/// </summary>
public class Tooltips_Services : MonoBehaviour
{
    [Header("Tooltip UI Components")]
    [Tooltip("Tooltip区域GameObject")]
    public GameObject tooltipArea;
    
    [Tooltip("Tooltip标题TextMeshPro组件")]
    public TextMeshProUGUI titleText;
    
    [Tooltip("Tooltip描述TextMeshPro组件")]
    public TextMeshProUGUI descriptionText;
    
    [Header("Tooltip Settings")]
    [Tooltip("显示延迟时间（秒）")]
    public float showDelay = 0.5f;
    
    [Tooltip("Tooltip偏移量")]
    public Vector2 offset = new Vector2(10f, 10f);
    
    [Tooltip("是否跟随鼠标")]
    public bool followMouse = true;
    
    [Header("Localization Settings")]
    [Tooltip("本地化表名")]
    public string tableName = "UI Text"; // 现在就这一个，以后如果有很多个Table的话就到时候再说（
    
    private LocalizedString titleLocalizedString;
    private LocalizedString descriptionLocalizedString;
    private Coroutine showCoroutine;
    private bool isVisible = false;
    private Camera uiCamera;
    private Canvas canvas;
    private RectTransform canvasRectTransform;
    
    // 单例模式
    public static Tooltips_Services Instance { get; private set; }
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeTooltip();
    }
    
    // 初始化时隐藏Tooltip
    private void Start()
    {
        HideTooltip();
    }
    
    private void InitializeTooltip()
    {
        // 获取UI相机和Canvas
        uiCamera = Camera.main;
        canvas = GetComponentInParent<Canvas>();
        if (canvas != null)
        {
            canvasRectTransform = canvas.GetComponent<RectTransform>();
        }
        
        // 初始化本地化字符串
        titleLocalizedString = new LocalizedString(tableName, "");
        descriptionLocalizedString = new LocalizedString(tableName, "");
        
        // 注册本地化变更事件
        titleLocalizedString.StringChanged += OnTitleChanged;
        descriptionLocalizedString.StringChanged += OnDescriptionChanged;
    }
    
    /// <summary>
    /// 显示Tooltip
    /// </summary>
    /// <param name="titleKey">标题本地化键</param>
    /// <param name="descriptionKey">描述本地化键</param>
    /// <param name="position">显示位置</param>
    public void ShowTooltip(string titleKey, string descriptionKey, Vector3 position = default)
    {
        if (tooltipArea == null || titleText == null || descriptionText == null)
        {
            Debug.LogWarning("[Tooltip Services] Tooltip组件未正确设置！");
            return;
        }
        
        // 停止之前的显示协程
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
        }
        
        // 设置本地化key
        titleLocalizedString.TableEntryReference = titleKey;
        descriptionLocalizedString.TableEntryReference = descriptionKey;
        
        if (position != default)
        {
            SetTooltipPosition(position);
        }
        
        // 开始显示延迟
        showCoroutine = StartCoroutine(ShowTooltipDelayed());
    }
    
    /// <summary>
    /// 隐藏Tooltip
    /// </summary>
    public void HideTooltip()
    {
        if (showCoroutine != null)
        {
            StopCoroutine(showCoroutine);
            showCoroutine = null;
        }
        
        if (tooltipArea != null)
        {
            tooltipArea.SetActive(false);
            isVisible = false;
        }
    }
    
    /// <summary>
    /// 延迟显示Tooltip
    /// </summary>
    private IEnumerator ShowTooltipDelayed()
    {
        yield return new WaitForSeconds(showDelay);
        
        if (tooltipArea != null)
        {
            tooltipArea.SetActive(true);
            isVisible = true;
        }
    }
    
    /// <summary>
    /// 设置Tooltip位置
    /// </summary>
    /// <param name="worldPosition">世界坐标位置</param>
    public void SetTooltipPosition(Vector3 worldPosition)
    {
        if (tooltipArea == null || canvas == null) return;
        
        RectTransform tooltipRect = tooltipArea.GetComponent<RectTransform>();
        if (tooltipRect == null) return;
        
        Vector2 screenPoint;
        
        if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
        {
            // 屏幕空间覆盖模式
            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
        }
        else
        {
            // 屏幕空间相机模式
            screenPoint = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPosition);
        }
        
        // 转换为Canvas本地坐标
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRectTransform, screenPoint, uiCamera, out localPoint);
        
        // 应用偏移量
        localPoint += offset;
        
        // 设置位置
        tooltipRect.anchoredPosition = localPoint;
    }
    
    /// <summary>
    /// 更新Tooltip位置（跟随鼠标）
    /// </summary>
    private void Update()
    {
        if (isVisible && followMouse)
        {
            Vector3 mousePosition = Input.mousePosition;
            SetTooltipPosition(mousePosition);
        }
    }
    
    /// <summary>
    /// 标题本地化变更回调
    /// </summary>
    private void OnTitleChanged(string value)
    {
        if (titleText != null)
        {
            titleText.text = value;
        }
    }
    
    /// <summary>
    /// 描述本地化变更回调
    /// </summary>
    private void OnDescriptionChanged(string value)
    {
        if (descriptionText != null)
        {
            descriptionText.text = value;
        }
    }
    
    /// <summary>
    /// 动态设置显示延迟
    /// </summary>
    /// <param name="delay">延迟时间</param>
    public void SetShowDelay(float delay)
    {
        showDelay = delay;
    }
    
    /// <summary>
    /// 动态设置偏移量
    /// </summary>
    /// <param name="newOffset">新的偏移量</param>
    public void SetOffset(Vector2 newOffset)
    {
        offset = newOffset;
    }
    
    /// <summary>
    /// 设置是否跟随鼠标
    /// </summary>
    /// <param name="follow">是否跟随</param>
    public void SetFollowMouse(bool follow)
    {
        followMouse = follow;
    }
    
    private void OnDestroy()
    {
        // 注销事件
        if (titleLocalizedString != null)
        {
            titleLocalizedString.StringChanged -= OnTitleChanged;
        }
        if (descriptionLocalizedString != null)
        {
            descriptionLocalizedString.StringChanged -= OnDescriptionChanged;
        }
    }
} 