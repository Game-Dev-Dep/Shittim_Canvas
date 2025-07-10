using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

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

    public TMP_InputField searchInputField;
    private string searchKeyword = "";
    private List<string> filteredFolderNames = new List<string>();

    private List<GameObject> optionPool = new List<GameObject>();
    private List<GameObject> activeOptions = new List<GameObject>();

    private float debounceTime = 0.15f; // 150ms防抖
    private Coroutine debounceCoroutine;
    private Coroutine populateCoroutine;

    void Start()
    {
        Wallpaper_Mode_Toggle_Button.onClick.AddListener(Destroy_Options);
        mainButton.onClick.AddListener(ToggleDropdown);
        folderPath = File_Services.Student_Files_Folder_Path;
        LoadFolderNames();
        optionTemplate.SetActive(false);
        dropdownPanel.SetActive(false);
        contentLayoutGroup = content.GetComponent<VerticalLayoutGroup>();
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;
        InitializeOptionHeight();
        if (searchInputField != null)
            searchInputField.onValueChanged.AddListener(OnSearchValueChanged);
        FilterOptions();
        InitializeOptionPool();

        //加载上次选择的学生
        string lastSelected = PlayerPrefs.GetString("Dropdown_SelectedCharacter", "");
        if (!string.IsNullOrEmpty(lastSelected))
        {
            StartCoroutine(DelaySetPreview(lastSelected));
        }
        else
        {
            SetDefaultCharacterThumbnail();
        }

        // 绑到相机保存按钮，疏影的锅。
        var cameraServices = Camera_Services.Instance;
        if (cameraServices != null && cameraServices.Save_Camera_Settings_Button != null)
        {
            cameraServices.Save_Camera_Settings_Button.onClick.AddListener(SaveDropdownSelection);
        }
    }

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
            SetTextWithLocalization(mainButtonLabel, defaultCharacterName);
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

    // 辅助方法：设置文本，优先使用Localization_To_TMP组件（主要修Localization的bug）
    private void SetTextWithLocalization(TMPro.TMP_Text textComponent, string text)
    {
        var localizationComponentType = System.Type.GetType("Localization_To_TMP");
        if (localizationComponentType != null)
        {
            var localizationComponent = textComponent.GetComponent(localizationComponentType);
            if (localizationComponent != null)
            {
                var setManualTextMethod = localizationComponentType.GetMethod("Set_Manual_Text", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (setManualTextMethod != null)
                {
                    setManualTextMethod.Invoke(localizationComponent, new object[] { text });
                    return;
                }
            }
        }
        // 如果没有找到Localization_To_TMP组件，直接设置文本
        textComponent.text = text;
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
            folderNames.Sort();
            Debug.Log($"[Dropdown_Services] 加载了 {folderNames.Count} 个文件夹，已排序");
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
            //重置搜索
            searchKeyword = "";
            if (searchInputField != null)
                searchInputField.text = "";
            FilterOptions();
            UpdateOptionsDisplay();
        }
        else
        {
            dropdownPanel.SetActive(false);
        }
    }

    void InitializeOptionPool()
    {
        //预创建对象
        int poolSize = Mathf.Min(folderNames.Count, 20); //先创20个
        for (int i = 0; i < poolSize; i++)
        {
            GameObject option = Instantiate(optionTemplate, content);
            option.SetActive(false);
            optionPool.Add(option);
        }
    }

    GameObject GetOptionFromPool()
    {
        GameObject option;
        if (optionPool.Count > 0)
        {
            //保证排序不乱
            option = optionPool[0];
            optionPool.RemoveAt(0);
        }
        else
        {
            option = Instantiate(optionTemplate, content);
        }

        option.SetActive(true);

        return option;
    }

    void ReturnOptionToPool(GameObject option)
    {
        option.SetActive(false);
        optionPool.Add(option);
    }

    void OnSearchValueChanged(string keyword)
    {
        searchKeyword = keyword.ToLower();
        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(DebounceSearch());
    }

    IEnumerator DebounceSearch()
    {
        yield return new WaitForSeconds(debounceTime);
        FilterOptions();
        UpdateOptionsDisplay();
    }

    void UpdateOptionsDisplay()
    {
        //回收当前显示的选项
        foreach (var option in activeOptions)
        {
            ReturnOptionToPool(option);
        }
        activeOptions.Clear();

        if (populateCoroutine != null)
            StopCoroutine(populateCoroutine);
        populateCoroutine = StartCoroutine(PopulateOptionsBatch());
    }

    IEnumerator PopulateOptionsBatch()
    {
        int batchSize = 5; //分批生成选项
        for (int i = 0; i < filteredFolderNames.Count; i++)
        {
            GameObject option = GetOptionFromPool();
            SetupOption(option, filteredFolderNames[i]);
            activeOptions.Add(option);
            if ((i + 1) % batchSize == 0)
            {
                yield return null;
            }
        }
        UpdateScrollSystem();
    }

    void SetupOption(GameObject option, string optionName)
    {
        RawImage icon = option.transform.Find("Icon")?.GetComponent<RawImage>();
        TMP_Text label = option.transform.Find("Label")?.GetComponent<TMP_Text>();

        if (optionName != "Textures")
        {
            if (Texture_Services.Lobbyillust.ContainsKey(optionName))
            {
                string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[optionName] + ".png");
                Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
                icon.texture = thumbnail != null ? thumbnail : defaultIcon;
            }
            else
            {
                icon.texture = defaultIcon;
            }
        }
        else
        {
            icon.texture = defaultIcon;
        }

        if (label != null) label.text = optionName;

        Button btn = option.GetComponent<Button>();
        btn.onClick.RemoveAllListeners();
        string capturedName = optionName;
        btn.onClick.AddListener(() => OnOptionSelectedByName(capturedName));

        // 设置选项文本，以及一个Debug Log
        TMP_Text optionText = option.GetComponentInChildren<TMP_Text>();
        if (optionText != null)
        {
            optionText.text = optionName;
        }
        else
        {
            Debug.LogError($"[Dropdown_Services] 选项 {optionName} 没有找到TMP_Text组件");
        }
    }

    void UpdateScrollSystem()
    {
        // 强制设置VerticalLayoutGroup为顶部对齐，防止Content Anchor脑抽
        if (contentLayoutGroup != null)
        {
            contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            contentLayoutGroup.childControlHeight = true;
            contentLayoutGroup.childControlWidth = true;
            contentLayoutGroup.childForceExpandHeight = false;
            contentLayoutGroup.childForceExpandWidth = false;
        }

        float totalContentHeight = CalculateTotalContentHeight();
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalContentHeight);

        float maxVisibleHeight = maxVisibleOptions * optionHeight
            + contentLayoutGroup.padding.top
            + contentLayoutGroup.padding.bottom
            + Mathf.Max(0, maxVisibleOptions - 1) * contentLayoutGroup.spacing;

        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxVisibleHeight);

        bool needsScroll = totalContentHeight > maxVisibleHeight;
        scrollRect.vertical = needsScroll;
        scrollRect.verticalScrollbar.gameObject.SetActive(needsScroll);

        content.anchoredPosition = Vector2.zero;
    }

    float CalculateTotalContentHeight()
    {
        if (activeOptions.Count == 0)
        {
            return 0;
        }

        float totalHeight = contentLayoutGroup.padding.top
            + contentLayoutGroup.padding.bottom
            + activeOptions.Count * optionHeight
            + Mathf.Max(0, activeOptions.Count - 1) * contentLayoutGroup.spacing;

        return totalHeight;
    }

    void OnOptionSelectedByName(string selectedCharacter)
    {
        // 使用辅助方法设置文本
        SetTextWithLocalization(mainButtonLabel, selectedCharacter);
        
        if (selectedCharacter != "Textures")
        {
            if (Texture_Services.Lobbyillust.ContainsKey(selectedCharacter))
            {
                string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[selectedCharacter] + ".png");
                Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
                mainButtonIcon.texture = thumbnail != null ? thumbnail : defaultIcon;
            }
            else
            {
                mainButtonIcon.texture = defaultIcon;
            }
        }
        else
        {
            mainButtonIcon.texture = defaultIcon;
        }
        Character_Services.Instance.Switch_Character(selectedCharacter);
        dropdownPanel.SetActive(false);
        isDropdownOpen = false;
    }

    void FilterOptions()
    {
        if (string.IsNullOrEmpty(searchKeyword))
        {
            filteredFolderNames = new List<string>(folderNames);
        }
        else
        {
            // Filter逻辑，分三个，一个精准，一个前缀和一个模糊
            filteredFolderNames = new List<string>();
            
            //精确
            var exactMatch = folderNames.FirstOrDefault(name => name.ToLower() == searchKeyword);
            if (exactMatch != null)
            {
                filteredFolderNames.Add(exactMatch);
            }
            else
            {
                //前缀
                var startsWithMatches = folderNames.Where(name => name.ToLower().StartsWith(searchKeyword)).ToList();
                filteredFolderNames.AddRange(startsWithMatches);
                
                //模糊
                if (filteredFolderNames.Count == 0)
                {
                    var containsMatches = folderNames.Where(name => name.ToLower().Contains(searchKeyword)).ToList();
                    filteredFolderNames.AddRange(containsMatches);
                }
            }
        }
        
        //再Sort一遍
        filteredFolderNames.Sort();
    }

    public void Destroy_Options()
    {
        foreach (var option in activeOptions)
        {
            ReturnOptionToPool(option);
        }
        activeOptions.Clear();
    }

    IEnumerator ForceLayoutUpdateNextFrame()
    {
        yield return null; // 等一帧
        if (contentLayoutGroup != null)
        {
            contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            Canvas.ForceUpdateCanvases();
        }
    }
    IEnumerator DelaySetPreview(string lastSelected)
    {
        yield return null; //还是等一帧，突然加载貌似选择的缩略图不会加载
        //Debug相关就留在这了，出问题看log能查

        SetTextWithLocalization(mainButtonLabel, lastSelected);
        if (lastSelected != "Textures" && Texture_Services.Lobbyillust.ContainsKey(lastSelected))
        {
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[lastSelected] + ".png");
            Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
            Debug.Log($"[Dropdown_Services] 尝试加载缩略图: {thumbnailPath}");
            if (thumbnail != null)
            {
                mainButtonIcon.texture = thumbnail;
            }
            else
            {
                Debug.LogWarning("[Dropdown_Services] 加载缩略图失败，使用默认图标");
                mainButtonIcon.texture = defaultIcon;
            }
        }
        else
        {
            Debug.LogWarning($"[Dropdown_Services] Lobbyillust 不包含 key: {lastSelected}，使用默认图标");
            mainButtonIcon.texture = defaultIcon;
        }
    }

    //保存当前下拉菜单选择，往上看可以找到绑着相机保存按钮的code
    public void SaveDropdownSelection()
    {
        if (mainButtonLabel != null)
        {
            PlayerPrefs.SetString("Dropdown_SelectedCharacter", mainButtonLabel.text);
            PlayerPrefs.Save();
        }
    }
}