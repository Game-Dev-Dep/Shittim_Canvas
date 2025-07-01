using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class Dropdown_Services : MonoBehaviour
{
    public static Dropdown_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Dropdown Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    [Header("UI References")]
    public Button mainButton;
    public RawImage mainButtonIcon;
    public TMP_Text mainButtonLabel;
    public GameObject dropdownPanel;
    public ScrollRect scrollRect;
    public RectTransform viewport;
    public RectTransform content;
    public GameObject optionTemplate;

    public Button Wallpaper_Mode_Toggle_Button;

    [Header("Settings")]
    public string folderPath = "";
    public Texture2D defaultIcon;
    public int maxVisibleOptions = 5;
    public float optionHeight = 40f;

    private List<string> folderNames = new List<string>();
    private bool isDropdownOpen = false;
    private VerticalLayoutGroup contentLayoutGroup;

    bool is_First_Get = true;
    void Start()
    {
        Wallpaper_Mode_Toggle_Button.onClick.AddListener(Destroy_Options);

        mainButton.onClick.AddListener(ToggleDropdown);
        folderPath = File_Services.Student_Files_Folder_Path;
        LoadFolderNames();

        optionTemplate.SetActive(false);
        dropdownPanel.SetActive(false);

        // 获取关键组件
        contentLayoutGroup = content.GetComponent<VerticalLayoutGroup>();

        // 确保滚动视图配置正确
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
    }

    void LoadFolderNames()
    {
        folderNames.Clear();
        if (Directory.Exists(folderPath))
        {
            DirectoryInfo dir = new DirectoryInfo(folderPath);
            foreach (DirectoryInfo subDir in dir.GetDirectories())
            {
                folderNames.Add(subDir.Name);
            }
        }
        else
        {
            Debug.LogError("Folder not found: " + folderPath);
        }
    }

    void ToggleDropdown()
    {
        isDropdownOpen = !isDropdownOpen;
        dropdownPanel.SetActive(isDropdownOpen);

        if (isDropdownOpen)
        {
            if (is_First_Get)
            {
                is_First_Get = false;
                StartCoroutine(PopulateOptionsAfterFrame());
            }
            else
            {
                dropdownPanel.SetActive(true);
            }
        }
        else
        {
            dropdownPanel.SetActive(false);
        }
    }

    IEnumerator PopulateOptionsAfterFrame()
    {
        yield return null; // 等待UI布局计算完成
        PopulateOptions();
    }

    void PopulateOptions()
    {
        // 动态生成选项
        for (int i = 0; i < folderNames.Count; i++)
        {
            GameObject option = Instantiate(optionTemplate, content);
            option.SetActive(true);

            RawImage icon = option.transform.Find("Icon")?.GetComponent<RawImage>();
            TMP_Text label = option.transform.Find("Label")?.GetComponent<TMP_Text>();

            if (folderNames[i] != "Textures")
            {
                //StartCoroutine(Texture_Services.Get_Texture_By_Path_Async(Path.Combine(File_Services.Student_Lists_Folder_Path, Process_Handler.Instance.Lobbyillust[folderNames[i]] + ".png"), local_texture2d =>
                //{
                //    if (local_texture2d != null)
                //    {
                //        icon.texture = local_texture2d;
                //    }
                //}));

                icon.texture = Texture_Services.Get_Texture_By_Path(Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[folderNames[i]] + ".png"));
            }
            else icon.texture = defaultIcon;



            if (label != null) label.text = folderNames[i];

            // 设置选项高度
            RectTransform rt = option.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, optionHeight);

            Button btn = option.GetComponent<Button>();
            int index = i;
            btn.onClick.AddListener(() => OnOptionSelected(index));
        }

        // 更新滚动系统
        UpdateScrollSystem();
    }

    public void Destroy_Options()
    {
        foreach (Transform child in content.transform)
        {
            GameObject childObject = child.gameObject;
            if (childObject != optionTemplate)
            {
                Destroy(childObject);
            }
        }
        is_First_Get = true;
    }


    void UpdateScrollSystem()
    {
        // 计算总内容高度
        float totalContentHeight = CalculateTotalContentHeight();

        // 设置内容区域尺寸
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalContentHeight);

        // 计算最大可见高度
        float maxVisibleHeight = maxVisibleOptions * optionHeight
            + contentLayoutGroup.padding.top
            + contentLayoutGroup.padding.bottom
            + Mathf.Max(0, maxVisibleOptions - 1) * contentLayoutGroup.spacing;

        // 设置视口高度
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxVisibleHeight);

        // 启用/禁用滚动
        bool needsScroll = totalContentHeight > maxVisibleHeight;
        scrollRect.vertical = needsScroll;
        scrollRect.verticalScrollbar.gameObject.SetActive(needsScroll);

        // 重置滚动位置
        content.anchoredPosition = Vector2.zero;
    }

    float CalculateTotalContentHeight()
    {
        if (contentLayoutGroup == null) return 0;

        float height = contentLayoutGroup.padding.top + contentLayoutGroup.padding.bottom;

        int activeChildCount = 0;
        foreach (Transform child in content)
        {
            if (child.gameObject.activeSelf && child != optionTemplate.transform)
            {
                height += child.GetComponent<RectTransform>().rect.height;
                
                activeChildCount++;
            }
        }

        // 添加间距
        if (activeChildCount > 0)
        {
            height += (activeChildCount - 1) * contentLayoutGroup.spacing;
        }

        return height;
    }

    void OnOptionSelected(int index)
    {
        if (index < 0 || index >= folderNames.Count) return;

        mainButtonLabel.text = folderNames[index];

        //StartCoroutine(Texture_Services.Get_Texture_By_Path_Async(Path.Combine(File_Services.Student_Lists_Folder_Path, Process_Handler.Instance.Lobbyillust[folderNames[index]] + ".png"), local_texture2d =>
        //{
        //    if (local_texture2d != null)
        //    {
        //        mainButtonIcon.texture = local_texture2d;
        //    }
        //}));

        Character_Services.Instance.Switch_Character(folderNames[index]);

        dropdownPanel.SetActive(false);
        isDropdownOpen = false;
    }
}