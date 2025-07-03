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
    public float optionHeight = 123f; //唉，写死的，改大点。

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

        // 从模板获取选项高度
        InitializeOptionHeight();

        // 设置默认角色的缩略图
        SetDefaultCharacterThumbnail();
    }

    //写个获取选项高度
    void InitializeOptionHeight()
    {
        if (optionTemplate != null)
        {
            RectTransform templateRect = optionTemplate.GetComponent<RectTransform>();
            if (templateRect != null)
            {
                optionHeight = templateRect.sizeDelta.y;
                Debug.Log($"[Dropdown_Services] 从模板获取选项高度: {optionHeight}");
            }
        }
    }

    void SetDefaultCharacterThumbnail()
    {
        if (mainButtonIcon == null) return;

        // 获取默认角色名称
        string defaultCharacterName = Config_Services.Instance.MemoryLobby_Camera_Config.Defalut_Character_Name;
        
        // 设置默认标签文本
        if (mainButtonLabel != null)
        {
            mainButtonLabel.text = defaultCharacterName;
        }

        // 加载默认角色的缩略图
        if (defaultCharacterName != "Textures" && Texture_Services.Lobbyillust.ContainsKey(defaultCharacterName))
        {
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[defaultCharacterName] + ".png");
            Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
            if (thumbnail != null)
            {
                mainButtonIcon.texture = thumbnail;
                Debug.Log($"[Dropdown_Services] 成功加载默认角色缩略图: {defaultCharacterName}");
                return;
            }
            else
            {
                Debug.LogWarning($"[Dropdown_Services] 无法加载默认角色缩略图: {thumbnailPath}");
            }
        }
        else
        {
            Debug.LogWarning($"[Dropdown_Services] 默认角色 {defaultCharacterName} 在 Lobbyillust 中未找到");
        }

        // 如果加载失败，使用默认图标
        if (defaultIcon != null)
        {
            mainButtonIcon.texture = defaultIcon;
            Debug.Log("[Dropdown_Services] 使用默认图标");
        }
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
                if (Texture_Services.Lobbyillust.ContainsKey(folderNames[i]))
                {
                    string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[folderNames[i]] + ".png");
                    Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
                    if (thumbnail != null)
                    {
                        icon.texture = thumbnail;
                    }
                    else
                    {
                        // 如果加载失败，使用默认图标
                        icon.texture = defaultIcon;
                        Debug.LogWarning($"[Dropdown_Services] 无法加载选项缩略图: {thumbnailPath}");
                    }
                }
                else
                {
                    icon.texture = defaultIcon;
                    Debug.LogWarning($"[Dropdown_Services] 角色 {folderNames[i]} 在 Lobbyillust 中未找到");
                }
            }
            else 
            {
                icon.texture = defaultIcon;
            }

            if (label != null) label.text = folderNames[i];

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

        string selectedCharacter = folderNames[index];
        Debug.Log($"[Dropdown_Services] 选择角色: {selectedCharacter}");

        mainButtonLabel.text = selectedCharacter;

        // 加载并显示学生缩略图
        if (selectedCharacter != "Textures")
        {
            if (Texture_Services.Lobbyillust.ContainsKey(selectedCharacter))
            {
                string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[selectedCharacter] + ".png");
                Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
                if (thumbnail != null)
                {
                    mainButtonIcon.texture = thumbnail;
                    Debug.Log($"[Dropdown_Services] 成功加载角色缩略图: {selectedCharacter}");
                }
                else
                {
                    // 如果加载失败，使用默认图标
                    mainButtonIcon.texture = defaultIcon;
                    Debug.LogWarning($"[Dropdown_Services] 无法加载角色缩略图: {thumbnailPath}");
                }
            }
            else
            {
                mainButtonIcon.texture = defaultIcon;
                Debug.LogWarning($"[Dropdown_Services] 角色 {selectedCharacter} 在 Lobbyillust 中未找到");
            }
        }
        else
        {
            mainButtonIcon.texture = defaultIcon;
            Debug.Log("[Dropdown_Services] 选择 Textures 文件夹，使用默认图标");
        }

        Character_Services.Instance.Switch_Character(selectedCharacter);

        dropdownPanel.SetActive(false);
        isDropdownOpen = false;
    }
}