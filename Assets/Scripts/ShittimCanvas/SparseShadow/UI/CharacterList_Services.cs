using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class CharacterList_Services : MonoBehaviour
{
    [SerializeField]
    public GameObject Character_List_Root_GameObject;
    [SerializeField]
    public GameObject Character_List_Search_Result_Content_GameObject;
    [SerializeField]
    public TMP_InputField Character_List_Search_BarInputField;
    [SerializeField]
    public GameObject Character_Card_Template;
    [SerializeField]
    public Button Character_List_Toggle_Button;
    [SerializeField]
    public Button Character_List_Exit_Button;
    [SerializeField]
    public GameObject Multi_Lobby_Root_GameObject;
    [SerializeField]
    public TMP_Dropdown Multi_Lobby_Dropdown;
    [SerializeField]
    public Button Multi_Lobby_Confirm_Button;
    [SerializeField]
    public Button Multi_Lobby_Quit_Button;
    [SerializeField]
    public TMP_Dropdown Search_Result_Sort_Dropdown;
    [SerializeField]
    public TMP_Dropdown School_Dropdown;
    [SerializeField]
    public Button Previous_Page_Button;
    [SerializeField]
    public Button Next_Page_Button;
    [SerializeField]
    public TextMeshProUGUI Page_Info_Text;
    [SerializeField]
    public ToggleGroup Character_Filter_ToggleGroup;
    [SerializeField]
    public Toggle All_Characters_Toggle;
    [SerializeField]
    public Toggle Favorite_Characters_Toggle;
    [SerializeField]
    public GameObject Empty_Result_GameObject;

    public float Character_Portrait_Width;
    public float Character_Portrait_Height;
    public float Character_Portrait_Spacing_X;
    public float Character_Portrait_Spacing_Y;

    private Dictionary<long, List<Character>> Character_List = new Dictionary<long, List<Character>>();
    private Dictionary<long, List<Character>> Filtered_Character_List = new Dictionary<long, List<Character>>();
    private Dictionary<long, List<Character>> Original_Character_List = new Dictionary<long, List<Character>>(); // 保存原始顺序
    private string searchKeyword = "";
    private float debounceTime = 0.3f; // 增加到300ms防抖，减少搜索频率
    private Coroutine debounceCoroutine;
    private List<GameObject> activeCharacterCards = new List<GameObject>(); // 缓存活跃的卡片对象
    private List<GameObject> cardPool = new List<GameObject>(); // 对象池
    private int maxPoolSize = 50; // 最大池大小

    // 搜索服务
    private CharacterSearchService searchService = new CharacterSearchService();

    // 性能优化相关
    private bool isDataLoaded = false;
    private bool isUIInitialized = false;
    private const int VISIBLE_ITEMS_COUNT = 24; // 一次显示的最大数量
    private int currentPage = 0; // 当前页码
    private List<long> currentDisplayKeys = new List<long>(); // 当前显示的键值列表

    /// <summary>
    /// 获取应用所有过滤后的角色列表
    /// </summary>
    private Dictionary<long, List<Character>> GetFilteredCharacterList()
    {
        var characterListToUse = string.IsNullOrEmpty(searchKeyword) ? Character_List : Filtered_Character_List;
        
        if (is_Favorite_Filter_On) 
            characterListToUse = Favorite_Filter(characterListToUse);
        
        if (is_School_Filter_On) 
            characterListToUse = School_Filter(characterListToUse, currentSelectedSchool);
            
        return characterListToUse;
    }

    // 排序dropdown列表相关
    private enum SortMode
    {
        Default,    // 默认排序（按CharacterList.json中的顺序）
        Ascending,  // A-Z升序
        Descending  // Z-A降序
    }

    private SortMode currentSortMode = SortMode.Default;

    private Favorite_Config favorite_config;
    private bool is_Favorite_Filter_On = false;
    
    // 学院过滤相关
    private School currentSelectedSchool = School.None; // 当前选中的学院
    private bool is_School_Filter_On = false; // 是否启用学院过滤

    private bool is_Character_List_On = false;

    public void Get_Charcter_List()
    {
        if (isDataLoaded) return; // 如果已加载，直接return

        string json = File.ReadAllText(Path.Combine(File_Services.Student_Lists_Folder_Path, "CharacterList.json"));
        Character_List = JsonConvert.DeserializeObject<Dictionary<long, List<Character>>>(json);
        // 保存原始顺序
        Original_Character_List = new Dictionary<long, List<Character>>(Character_List);

        // 构建搜索索引
        searchService.BuildSearchIndex(Character_List);

        isDataLoaded = true;
    }

    public void Get_Favorite_Config()
    {
        favorite_config = Config_Services.Instance.Global_Favorite_Config;
    }

    public void Save_Favorite_Config()
    {
        Config_Services.Instance.Save_Favorite_Config(favorite_config, Path.Combine(File_Services.Config_Files_Folder_Path, "Favorite Config.json"));
    }

    private void Start()
    {
        Console_Log("开始初始化 Character_Services");

        Character_List_Toggle_Button.onClick.AddListener(Toggle_Character_List_Panel);
        Character_List_Exit_Button.onClick.AddListener(Hide_Character_List_Panel);

        // 绑定Toggle事件
        if (All_Characters_Toggle != null)
        {
            All_Characters_Toggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(All_Characters_Toggle, isOn));
        }
        if (Favorite_Characters_Toggle != null)
        {
            Favorite_Characters_Toggle.onValueChanged.AddListener((isOn) => OnToggleValueChanged(Favorite_Characters_Toggle, isOn));
        }

        // 绑定搜索输入框事件
        if (Character_List_Search_BarInputField != null)
        {
            Character_List_Search_BarInputField.onValueChanged.AddListener(OnSearchValueChanged);
        }

        // 排序Dropdown
        if (Search_Result_Sort_Dropdown != null)
        {
            Search_Result_Sort_Dropdown.onValueChanged.AddListener(OnSortDropdownChanged);
            InitializeSortDropdown();
        }

        if (School_Dropdown != null)
        {
            School_Dropdown.onValueChanged.AddListener(OnSchoolDropdownChanged);
            Initialize_School_Dropdown();
        }

        // 分页按钮
        if (Previous_Page_Button != null)
        {
            Previous_Page_Button.onClick.AddListener(OnPreviousPage);
        }

        if (Next_Page_Button != null)
        {
            Next_Page_Button.onClick.AddListener(OnNextPage);
        }

        Get_Favorite_Config();
        Get_Charcter_List();
        Get_Detail_Option_UI_Parameters();

        // 初始化Toggle Group状态
        if (Character_Filter_ToggleGroup != null)
        {
            // 默认选择"全部"选项
            if (All_Characters_Toggle != null)
            {
                All_Characters_Toggle.isOn = true;
            }
            is_Favorite_Filter_On = false;
        }

        // Onchange调用
        LocalizationSettings.SelectedLocaleChanged += OnLanguageChanged;

        Console_Log("结束初始化 Character_Services");
    }

    private void Toggle_Character_List_Panel()
    {
        if (is_Character_List_On)
        {
            Hide_Character_List_Panel();
        }
        else
        {
            Display_Character_List_Panel();
        }
    }

    private void Display_Character_List_Panel()
    {
        is_Character_List_On = true;
        Character_List_Root_GameObject.SetActive(true);

        // 确保Toggle Group状态与当前过滤状态同步
        if (Character_Filter_ToggleGroup != null)
        {
            if (is_Favorite_Filter_On)
            {
                if (Favorite_Characters_Toggle != null) Favorite_Characters_Toggle.isOn = true;
            }
            else
            {
                if (All_Characters_Toggle != null) All_Characters_Toggle.isOn = true;
            }
        }

        Create_Character_List_UI();
    }

    private void Hide_Character_List_Panel()
    {
        is_Character_List_On = false;
        Character_List_Root_GameObject.SetActive(is_Character_List_On);
        Destroy_Chracter_List_UI();
    }

    void OnToggleValueChanged(Toggle toggle, bool isOn)
    {
        if (toggle == null || !isOn) return; // 只处理被选中的Toggle

        // 根据选中的Toggle设置收藏过滤状态
        if (toggle == All_Characters_Toggle)
        {
            is_Favorite_Filter_On = false;
        }
        else if (toggle == Favorite_Characters_Toggle)
        {
            is_Favorite_Filter_On = true;
        }

        Console_Log($"Toggle 切换: {toggle.name}, 收藏过滤状态: {is_Favorite_Filter_On}");

        // 重置页码，因为过滤条件改变后当前页码可能超出新的最大页数
        currentPage = 0;

        // 重新创建UI以应用过滤
        Destroy_Chracter_List_UI();
        Create_Character_List_UI();
    }

    private void Get_Detail_Option_UI_Parameters()
    {
        Character_Portrait_Width = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().cellSize.x;
        Character_Portrait_Height = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().cellSize.y;
        Character_Portrait_Spacing_X = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().spacing.x;
        Character_Portrait_Spacing_Y = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().spacing.y;
    }

    public void Create_Character_List_UI()
    {
        Console_Log("开始创建角色列表UI");

        if (!isDataLoaded)
        {
            Get_Charcter_List();
        }

        if (!isUIInitialized)
        {
            InitializeUI();
            isUIInitialized = true;
        }

        UpdateCharacterListDisplay();
        Console_Log("结束创建角色列表UI");
    }

    private void OnPreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            UpdateCharacterListDisplay();
            UpdatePageInfo();
        }
    }

    private void OnNextPage()
    {
        var characterListToUse = GetFilteredCharacterList();
        int maxPage = (characterListToUse.Count - 1) / VISIBLE_ITEMS_COUNT;

        if (currentPage < maxPage)
        {
            currentPage++;
            UpdateCharacterListDisplay();
            UpdatePageInfo();
        }
    }

    private void UpdatePageInfo()
    {
        if (Page_Info_Text != null)
        {
            var characterListToUse = GetFilteredCharacterList();
            int maxPage = (characterListToUse.Count - 1) / VISIBLE_ITEMS_COUNT;
            string pageInfoKey = "character_list_panel.page_info";
            string localizedPageInfo = GetLocalizedText(pageInfoKey);

            // 如果本地化文本为空或与key相同，使用默认格式
            if (string.IsNullOrEmpty(localizedPageInfo) || localizedPageInfo == pageInfoKey)
            {
                Page_Info_Text.text = $" {currentPage + 1} / {maxPage + 1} ";
            }
            else
            {
                // 使用本地化格式，支持参数替换
                Page_Info_Text.text = string.Format(localizedPageInfo, currentPage + 1, maxPage + 1);
            }
        }

        // 更新按钮状态
        if (Previous_Page_Button != null)
        {
            Previous_Page_Button.interactable = currentPage > 0;
        }

        if (Next_Page_Button != null)
        {
            var characterListToUse = GetFilteredCharacterList();
            int maxPage = (characterListToUse.Count - 1) / VISIBLE_ITEMS_COUNT;
            Next_Page_Button.interactable = currentPage < maxPage;
        }
    }

    private void InitializeUI()
    {
        // 预创建一些卡片对象到池中
        for (int i = 0; i < VISIBLE_ITEMS_COUNT; i++)
        {
            GameObject card = Instantiate(Character_Card_Template);
            card.SetActive(false);
            cardPool.Add(card);
        }
    }
    // 把搜索搬过来了
    private void OnSearchValueChanged(string keyword)
    {
        searchKeyword = keyword.ToLower();
        if (debounceCoroutine != null)
            StopCoroutine(debounceCoroutine);
        debounceCoroutine = StartCoroutine(DebounceSearch());
    }

    private IEnumerator DebounceSearch()
    {
        yield return new WaitForSeconds(debounceTime);
        FilterCharacters();
        SortCharacters(); // 保持排序
        UpdateCharacterListDisplay();
    }

    private void FilterCharacters()
    {
        if (string.IsNullOrEmpty(searchKeyword))
        {
            Filtered_Character_List = new Dictionary<long, List<Character>>(Character_List);
        }
        else
        {
            // 使用搜索服务进行搜索
            var searchResults = searchService.SearchCharacters(searchKeyword);
            Filtered_Character_List = new Dictionary<long, List<Character>>();

            foreach (var characterId in searchResults)
            {
                if (Character_List.ContainsKey(characterId))
                {
                    Filtered_Character_List.Add(characterId, Character_List[characterId]);
                }
            }
        }

        // 重置到第一页
        currentPage = 0;

        Console_Log($"搜索关键词: '{searchKeyword}', 找到 {Filtered_Character_List.Count} 个角色");
    }

    /// <summary>
    /// 获取本地化的角色名称
    /// </summary>
    private string GetLocalizedCharacterName(Character character)
    {
        // 获取当前语言设置
        var currentLocale = LocalizationSettings.SelectedLocale;
        if (currentLocale == null) return character.DevName;

        string localeCode = currentLocale.Identifier.Code;

        // 根据当前语言返回对应的名称
        switch (localeCode)
        {
            case "zh":
                return !string.IsNullOrEmpty(character.FullNameSC) ? character.FullNameSC : character.DevName;
            case "zh-TW":
                return !string.IsNullOrEmpty(character.FullNameTC) ? character.FullNameTC : character.DevName;
            case "en":
                return !string.IsNullOrEmpty(character.FullNameEn) ? character.FullNameEn : character.DevName;
            case "ja":
                return !string.IsNullOrEmpty(character.FullNameJp) ? character.FullNameJp : character.DevName;
            default:
                return character.DevName;
        }
    }

    /// <summary>
    /// 获取本地化的学校名称
    /// </summary>
    private string GetLocalizedSchoolName(School school)
    {
        string schoolKey = $"character.school.{school.ToString().ToLower()}";
        string localizedName = GetLocalizedText(schoolKey);

        // 如果本地化文本为空或与key相同，返回null
        if (string.IsNullOrEmpty(localizedName) || localizedName == schoolKey)
        {
            return null;
        }

        return localizedName;
    }

    /// <summary>
    /// 获取本地化的俱乐部名称
    /// </summary>
    private string GetLocalizedClubName(Club club)
    {
        string clubKey = $"character.club.{club.ToString().ToLower()}";
        string localizedName = GetLocalizedText(clubKey);

        // 如果本地化文本为空或与key相同，返回null
        if (string.IsNullOrEmpty(localizedName) || localizedName == clubKey)
        {
            return null;
        }

        return localizedName;
    }

    // 排序相关方法
    private void InitializeSortDropdown()
    {
        if (Search_Result_Sort_Dropdown != null)
        {
            UpdateSortDropdownOptions();
            Search_Result_Sort_Dropdown.value = 0; // 默认选择第一个选项
        }
    }

    private void Initialize_School_Dropdown()
    {
        School_Dropdown.ClearOptions();
        List<string> options = new List<string>();
        options.Add(GetLocalizedText("character.school.all"));
        for(int i = 1; i <= 16 ; i++)
        {
            string school_name = ((School)i).ToString();
            options.Add(GetLocalizedText($"character.school.{school_name.ToLower()}"));
        }
        School_Dropdown.AddOptions(options);
    }

    private void UpdateSortDropdownOptions()
    {
        if (Search_Result_Sort_Dropdown != null)
        {
            Search_Result_Sort_Dropdown.ClearOptions();

            // 添加排序选项
            List<string> sortOptions = new List<string>
            {
                GetLocalizedText("character_list_panel.sort_button.default_sort"),
                GetLocalizedText("character_list_panel.sort_button.asc_sort"),
                GetLocalizedText("character_list_panel.sort_button.desc_sort")
            };

            Search_Result_Sort_Dropdown.AddOptions(sortOptions);
        }
    }

    private string GetLocalizedText(string key)
    {
        // Localization_Utils
        return Localization_Utils.Get_Localized_Text(key);
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLanguageChanged;

        // 清理Toggle事件监听
        if (All_Characters_Toggle != null)
        {
            All_Characters_Toggle.onValueChanged.RemoveAllListeners();
        }
        if (Favorite_Characters_Toggle != null)
        {
            Favorite_Characters_Toggle.onValueChanged.RemoveAllListeners();
        }
    }

    // 语言切换相关
    private void OnLanguageChanged(Locale locale)
    {
        // 重新构建搜索索引以支持新语言
        if (isDataLoaded)
        {
            searchService.BuildSearchIndex(Character_List);
        }

        if (Search_Result_Sort_Dropdown != null)
        {
            int currentValue = Search_Result_Sort_Dropdown.value;
            UpdateSortDropdownOptions();
            Search_Result_Sort_Dropdown.value = currentValue;
        }
        
        if (School_Dropdown != null)
        {
            int currentValue = School_Dropdown.value;
            Initialize_School_Dropdown();
            School_Dropdown.value = currentValue;
        }

        // 如果当前有搜索结果，重新搜索以更新显示
        if (!string.IsNullOrEmpty(searchKeyword))
        {
            FilterCharacters();
            UpdateCharacterListDisplay();
        }
    }

    private void OnSortDropdownChanged(int index)
    {
        // 根据下拉框索引设置排序模式
        switch (index)
        {
            case 0:
                currentSortMode = SortMode.Default;
                break;
            case 1:
                currentSortMode = SortMode.Ascending;
                break;
            case 2:
                currentSortMode = SortMode.Descending;
                break;
        }

        Console_Log($"排序下拉框选择改变，当前排序模式: {currentSortMode}");
        SortCharacters();
        UpdateCharacterListDisplay();
    }

    void SortCharacters()
    {
        var characterListToSort = GetFilteredCharacterList();

        if (characterListToSort.Count == 0) return;

        // 根据排序模式进行排序
        switch (currentSortMode)
        {
            case SortMode.Default:
                // 恢复原始顺序
                if (string.IsNullOrEmpty(searchKeyword))
                {
                    Character_List = new Dictionary<long, List<Character>>(Original_Character_List);
                }
                else
                {
                    // 对于搜索结果，先按原始顺序过滤，然后保持原始顺序
                    var filteredOriginal = new Dictionary<long, List<Character>>();
                    foreach (var item in Original_Character_List)
                    {
                        if (Filtered_Character_List.ContainsKey(item.Key))
                        {
                            filteredOriginal.Add(item.Key, item.Value);
                        }
                    }

                    Filtered_Character_List = filteredOriginal;
                }

                break;

            case SortMode.Ascending:
                // A-Z升序排序
                SortByField(characterListToSort, true);
                break;

            case SortMode.Descending:
                // Z-A降序排序
                SortByField(characterListToSort, false);
                break;
        }
    }


    private void OnSchoolDropdownChanged(int index)
    {
        if (index == 0)
        {
            currentSelectedSchool = School.None;
            is_School_Filter_On = false;
        }
        else
        {
            currentSelectedSchool = (School)index;
            is_School_Filter_On = true;
        }
        
        // 重置页码
        currentPage = 0;
        
        // 重新创建UI以应用过滤
        Destroy_Chracter_List_UI();
        Create_Character_List_UI();
    }

    private void SortByField(Dictionary<long, List<Character>> characterListToSort, bool ascending)
    {
        // 将Dictionary转换为List进行排序
        var sortedList = characterListToSort.ToList();

        // 按名称排序
        if (ascending)
        {
            sortedList.Sort((a, b) => a.Value.First().DevName.CompareTo(b.Value.First().DevName));
        }
        else
        {
            sortedList.Sort((a, b) => b.Value.First().DevName.CompareTo(a.Value.First().DevName));
        }

        // 将排序后的结果重新转换为Dictionary
        var sortedDict = new Dictionary<long, List<Character>>();
        foreach (var item in sortedList)
        {
            sortedDict.Add(item.Key, item.Value);
        }

        // 更新对应的列表
        if (string.IsNullOrEmpty(searchKeyword))
        {
            Character_List = sortedDict;
        }
        else
        {
            Filtered_Character_List = sortedDict;
        }
    }

    /// <summary>
    /// 疏: 杰先生的石山发力了
    /// 杰: 你再骂！？
    /// </summary>
    /// <param name="characterListToUse">要过滤的角色列表</param>
    /// <returns>过滤后的角色列表</returns>
    private Dictionary<long, List<Character>> Favorite_Filter(Dictionary<long, List<Character>> characterListToUse)
    {
        Dictionary<long, List<Character>> characterListToUse_New = new Dictionary<long, List<Character>>();
        foreach (var character in characterListToUse)
        {
            if (favorite_config.Character_Names.Contains(character.Value.First().DevName))
            {
                characterListToUse_New.Add(character.Key, character.Value);
            }
        }

        if (characterListToUse_New != new Dictionary<long, List<Character>>())
        {
            return characterListToUse_New;
        }
        else
        {
            return characterListToUse;
        }
    }

    private Dictionary<long, List<Character>> School_Filter(Dictionary<long, List<Character>> characterListToUse, School school)
    {
        // 如果选择None，返回所有角色
        if (school == School.None)
        {
            return characterListToUse;
        }
        
        Dictionary<long, List<Character>> characterListToUse_New = new Dictionary<long, List<Character>>();
        foreach (var character in characterListToUse)
        {
            if (character.Value.First().School == school)
            {
                characterListToUse_New.Add(character.Key, character.Value);
            }
        }
        return characterListToUse_New;
    }

    private void UpdateCharacterListDisplay()
    {
        // 使用过滤后的角色列表
        var characterListToUse = GetFilteredCharacterList();

        // 计算当前需要显示的项目
        currentDisplayKeys.Clear();
        var keysToDisplay = characterListToUse.Keys.ToList();
        int startIndex = currentPage * VISIBLE_ITEMS_COUNT;
        int endIndex = Mathf.Min(startIndex + VISIBLE_ITEMS_COUNT, keysToDisplay.Count);

        for (int i = startIndex; i < endIndex; i++)
        {
            currentDisplayKeys.Add(keysToDisplay[i]);
        }

        // 回收现有的卡片到对象池
        ReturnCardsToPool();

        // 强制布局更新，确保GridLayoutGroup重置
        StartCoroutine(ForceLayoutUpdate());

        // 只创建当前页面的卡片
        foreach (var characterKey in currentDisplayKeys)
        {
            var character = characterListToUse[characterKey];
            GameObject character_card_gameobject = GetCardFromPool();
            character_card_gameobject.transform.SetParent(Character_List_Search_Result_Content_GameObject.transform);
            character_card_gameobject.SetActive(true);

            // 重置RectTransform属性，防止对象池中的对象保留之前的尺寸
            RectTransform cardRect = character_card_gameobject.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(Character_Portrait_Width, Character_Portrait_Height);
                cardRect.anchoredPosition = Vector2.zero;
            }

            activeCharacterCards.Add(character_card_gameobject);

            // 获取卡片内的图片组件
            RawImage character_portrait_rawimage_component = character_card_gameobject.GetComponentInChildren<RawImage>();
            if (character_portrait_rawimage_component != null)
            {
                character_portrait_rawimage_component.texture = Texture_Services.Get_Texture_By_Path(Path.Combine(File_Services.Student_Lists_Folder_Path,$"Student_Portrait_{character.First().DevName}_Collection.png"));
            }

            // 获取卡片的按钮组件
            Button character_card_button = character_card_gameobject.GetComponent<Button>();
            if (character_card_button != null)
            {
                character_card_button.onClick.RemoveAllListeners(); // 清除之前的监听器
                character_card_button.onClick.AddListener(() =>
                {
                    Character_Select_Handler(characterKey, character.First().DevName, character.Count);
                });
            }

            // 获取卡片内的角色名称文本组件
            TextMeshProUGUI character_name_text_component = character_card_gameobject.GetComponentInChildren<TextMeshProUGUI>();
            if (character_name_text_component != null)
            {
                // 使用本地化的角色名称
                string localizedCharacterName = GetLocalizedCharacterName(character.First());
                character_name_text_component.text = localizedCharacterName;
            }

            // 收藏按钮
            Button character_favorite_button = character_card_gameobject.transform.Find("[Character List] Favorite Button").GetComponent<Button>();
            if (favorite_config.Character_Names.Contains(character.First().DevName))
            {
                Update_Favorite_Button_UI(true, character_favorite_button.gameObject);
            }
            else
            {
                Update_Favorite_Button_UI(false, character_favorite_button.gameObject);
            }
            character_favorite_button.onClick.RemoveAllListeners();
            character_favorite_button.onClick.AddListener(() =>
            {
                Favorite_Toggle_Handler(character.First().DevName, character_favorite_button.gameObject);
            });
        }

        // 调整内容区域大小 - 基于当前页面实际显示的数量计算
        int itemsPerRow = 6; // 每行5个，以后卡片池炸了可以改改这个 疏影：五个在哪？？？
        int currentPageItemCount = currentDisplayKeys.Count;
        int rowsNeeded = Mathf.CeilToInt((float)currentPageItemCount / itemsPerRow);

        float contentHeight = (Character_Portrait_Height + Character_Portrait_Spacing_Y) * rowsNeeded + Character_Portrait_Spacing_Y;
        Character_List_Search_Result_Content_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, contentHeight);

        // 更新分页信息
        UpdatePageInfo();

        // 如果没搜索结果，显示我搞的阿罗娜空结果界面
        if (Empty_Result_GameObject != null)
        {
            bool shouldShowEmptyResult = characterListToUse.Count == 0;
            Empty_Result_GameObject.SetActive(shouldShowEmptyResult);
        }
    }

    GameObject GetCardFromPool()
    {
        if (cardPool.Count > 0)
        {
            GameObject card = cardPool[cardPool.Count - 1];
            cardPool.RemoveAt(cardPool.Count - 1);

            RectTransform cardRect = card.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(Character_Portrait_Width, Character_Portrait_Height);
                cardRect.anchoredPosition = Vector2.zero;
            }

            return card;
        }
        else
        {
            GameObject newCard = Instantiate(Character_Card_Template);

            RectTransform cardRect = newCard.GetComponent<RectTransform>();
            if (cardRect != null)
            {
                cardRect.localScale = Vector3.one;
                cardRect.sizeDelta = new Vector2(Character_Portrait_Width, Character_Portrait_Height);
                cardRect.anchoredPosition = Vector2.zero;
            }

            return newCard;
        }
    }

    private void ReturnCardsToPool()
    {
        foreach (var card in activeCharacterCards)
        {
            if (card != null)
            {
                card.SetActive(false);
                card.transform.SetParent(null);

                RectTransform cardRect = card.GetComponent<RectTransform>();
                if (cardRect != null)
                {
                    cardRect.localScale = Vector3.one;
                    cardRect.sizeDelta = new Vector2(Character_Portrait_Width, Character_Portrait_Height);
                    cardRect.anchoredPosition = Vector2.zero;
                }

                if (cardPool.Count < maxPoolSize)
                {
                    cardPool.Add(card);
                }
                else
                {
                    Destroy(card);
                }
            }
        }

        activeCharacterCards.Clear();
    }

    IEnumerator ForceLayoutUpdate()
    {
        yield return null;

        Canvas.ForceUpdateCanvases();

        GridLayoutGroup gridLayout = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            gridLayout.enabled = false;
            yield return null;
            gridLayout.enabled = true;
        }

        Canvas.ForceUpdateCanvases();
    }

    private void Destroy_Chracter_List_UI()
    {
        ReturnCardsToPool();
    }

    private void Character_Select_Handler(long character_id, string character_name, int character_num)
    {
        Console_Log($"已选择角色: {character_id} {character_name} 角色含有: {character_num} 个变体");
        if (character_num == 1)
        {
            Character_Services.Instance.Switch_Character(character_name);
            Character_List_Root_GameObject.SetActive(false);
            Destroy_Chracter_List_UI();
        }
        else
        {
            Multi_Lobby_Root_GameObject.SetActive(true);
            Multi_Lobby_Dropdown.ClearOptions();
            List<string> character_names = new List<string>();
            foreach (Character character in Character_List[character_id])
            {
                character_names.Add(character.DevName);
            }

            Multi_Lobby_Dropdown.AddOptions(character_names);
            Multi_Lobby_Confirm_Button.onClick.RemoveAllListeners();
            Multi_Lobby_Confirm_Button.onClick.AddListener(() =>
            {
                int index = Multi_Lobby_Dropdown.value;
                Multi_Lobby_Select_Handler(index, character_id);
            });
            Multi_Lobby_Quit_Button.onClick.RemoveAllListeners();
            Multi_Lobby_Quit_Button.onClick.AddListener(() =>
            {
                Multi_Lobby_Root_GameObject.SetActive(false);
            });
        }
    }

    private void Multi_Lobby_Select_Handler(int index, long character_id)
    {
        // 处理多人大厅选择
        Console_Log($"选择多人大厅: {index}, 角色ID: {character_id}");
    }

    private void Favorite_Toggle_Handler(string character_name, GameObject favorite_button)
    {
        if (!favorite_config.Character_Names.Contains(character_name))
        {
            favorite_config.Character_Names.Add(character_name);
            Update_Favorite_Button_UI(true, favorite_button);
        }
        else
        {
            favorite_config.Character_Names.Remove(character_name);
            Update_Favorite_Button_UI(false, favorite_button);
        }

        Save_Favorite_Config();

        // 如果当前在收藏页面，刷新显示
        if (is_Favorite_Filter_On && is_Character_List_On)
        {
            Destroy_Chracter_List_UI();
            Create_Character_List_UI();
        }
    }

    public void Update_Favorite_Button_UI(bool is_On, GameObject favorite_button)
    {
        favorite_button.transform.Find("[Character List] Favorite On Icon").GetComponent<Image>().enabled = is_On;
        favorite_button.transform.Find("[Character List] Favorite Off Icon").GetComponent<Image>().enabled = !is_On;
    }

    public class Character
    {
        public string DevName = string.Empty;
        public string FullNameSC = string.Empty;
        public string FullNameTC = string.Empty;
        public string FullNameEn = string.Empty;
        public string FullNameJp = string.Empty;
        public List<string> Nicknames = new List<string>();
        public School School = new School();
        public Club Club = new Club();
    }

    /// <summary>
    /// 角色搜索服务类
    /// </summary>
    public class CharacterSearchService
    {
        private Dictionary<string, List<long>> searchIndex = new Dictionary<string, List<long>>();
        private Dictionary<long, Character> characterCache = new Dictionary<long, Character>();
        private bool isIndexBuilt = false;

        /// <summary>
        /// 构建搜索索引
        /// </summary>
        public void BuildSearchIndex(Dictionary<long, List<Character>> characterList)
        {
            searchIndex.Clear();
            characterCache.Clear();

            foreach (var kvp in characterList)
            {
                var character = kvp.Value.First();
                characterCache[kvp.Key] = character;

                // 索引角色名称（多语言）
                IndexCharacterName(kvp.Key, character);

                // 索引昵称
                IndexNicknames(kvp.Key, character);

                // 索引学校名称
                IndexSchoolName(kvp.Key, character);

                // 索引俱乐部名称
                IndexClubName(kvp.Key, character);
            }

            isIndexBuilt = true;
        }

        private void IndexCharacterName(long characterId, Character character)
        {
            // 索引DevName
            AddToIndex(character.DevName.ToLower(), characterId);

            // 索引多语言名称
            AddToIndex(character.FullNameSC.ToLower(), characterId);
            AddToIndex(character.FullNameTC.ToLower(), characterId);
            AddToIndex(character.FullNameEn.ToLower(), characterId);
            AddToIndex(character.FullNameJp.ToLower(), characterId);
        }

        private void IndexNicknames(long characterId, Character character)
        {
            foreach (var nickname in character.Nicknames)
            {
                AddToIndex(nickname.ToLower(), characterId);
            }
        }

        private void IndexSchoolName(long characterId, Character character)
        {
            string schoolKey = $"character.school.{character.School.ToString().ToLower()}";
            string schoolName = Localization_Utils.Get_Localized_Text(schoolKey);

            if (!string.IsNullOrEmpty(schoolName) && schoolName != schoolKey)
            {
                AddToIndex(schoolName.ToLower(), characterId);
            }
        }

        private void IndexClubName(long characterId, Character character)
        {
            string clubKey = $"character.club.{character.Club.ToString().ToLower()}";
            string clubName = Localization_Utils.Get_Localized_Text(clubKey);

            if (!string.IsNullOrEmpty(clubName) && clubName != clubKey)
            {
                AddToIndex(clubName.ToLower(), characterId);
            }
        }

        private void AddToIndex(string term, long characterId)
        {
            if (string.IsNullOrEmpty(term)) return;

            // 添加完整术语
            if (!searchIndex.ContainsKey(term))
                searchIndex[term] = new List<long>();

            if (!searchIndex[term].Contains(characterId))
                searchIndex[term].Add(characterId);

            // 添加前缀索引（用于前缀搜索）
            for (int i = 1; i < term.Length; i++)
            {
                string prefix = term.Substring(0, i);
                if (!searchIndex.ContainsKey(prefix))
                    searchIndex[prefix] = new List<long>();

                if (!searchIndex[prefix].Contains(characterId))
                    searchIndex[prefix].Add(characterId);
            }
        }

        /// <summary>
        /// 搜索角色
        /// </summary>
        public List<long> SearchCharacters(string searchTerm)
        {
            if (!isIndexBuilt || string.IsNullOrEmpty(searchTerm))
                return new List<long>();

            string searchTermLower = searchTerm.ToLower();
            var results = new HashSet<long>();

            // 精确匹配
            if (searchIndex.ContainsKey(searchTermLower))
            {
                foreach (var characterId in searchIndex[searchTermLower])
                {
                    results.Add(characterId);
                }
            }

            // 模糊匹配（包含搜索）
            foreach (var kvp in searchIndex)
            {
                if (kvp.Key.Contains(searchTermLower))
                {
                    foreach (var characterId in kvp.Value)
                    {
                        results.Add(characterId);
                    }
                }
            }

            return results.ToList();
        }

        /// <summary>
        /// 获取搜索建议
        /// </summary>
        public List<string> GetSearchSuggestions(string partialTerm, int maxSuggestions = 5)
        {
            if (!isIndexBuilt || string.IsNullOrEmpty(partialTerm))
                return new List<string>();

            string partialTermLower = partialTerm.ToLower();
            var suggestions = new List<string>();

            foreach (var kvp in searchIndex)
            {
                if (kvp.Key.StartsWith(partialTermLower) && kvp.Key != partialTermLower)
                {
                    suggestions.Add(kvp.Key);
                    if (suggestions.Count >= maxSuggestions)
                        break;
                }
            }

            return suggestions;
        }

        /// <summary>
        /// 清除索引
        /// </summary>
        public void ClearIndex()
        {
            searchIndex.Clear();
            characterCache.Clear();
            isIndexBuilt = false;
        }
    }

    public enum School
    {
        None = 0,
        Hyakkiyako = 1,
        RedWinter = 2,
        Trinity = 3,
        Gehenna = 4,
        Abydos = 5,
        Millennium = 6,
        Arius = 7,
        Shanhaijing = 8,
        Valkyrie = 9,
        WildHunt = 10,
        SRT = 11,
        SCHALE = 12,
        ETC = 13,
        Tokiwadai = 14,
        Sakugawa = 15,
        Highlander = 16
    }

    public enum Club
    {
        None = 0,
        Engineer = 1,
        CleanNClearing = 2,
        KnightsHospitaller = 3,
        IndeGEHENNA = 4,
        IndeMILLENNIUM = 5,
        IndeHyakkiyako = 6,
        IndeShanhaijing = 7,
        IndeTrinity = 8,
        FoodService = 9,
        Countermeasure = 10,
        BookClub = 11,
        MatsuriOffice = 12,
        GourmetClub = 13,
        HoukagoDessert = 14,
        RedwinterSecretary = 15,
        Schale = 16,
        TheSeminar = 17,
        AriusSqud = 18,
        Justice = 19,
        Fuuki = 20,
        Kohshinjo68 = 21,
        Meihuayuan = 22,
        SisterHood = 23,
        GameDev = 24,
        anzenkyoku = 25,
        RemedialClass = 26,
        SPTF = 27,
        TrinityVigilance = 28,
        Veritas = 29,
        TrainingClub = 30,
        Onmyobu = 31,
        Shugyobu = 32,
        Endanbou = 33,
        NinpoKenkyubu = 34,
        Class227 = 35,
        EmptyClub = 36,
        Emergentology = 37,
        RabbitPlatoon = 38,
        PandemoniumSociety = 39,
        HotSpringsDepartment = 40,
        TeaParty = 41,
        PublicPeaceBureau = 42,
        Genryumon = 43,
        BlackTortoisePromenade = 44,
        LaborParty = 45,
        KnowledgeLiberationFront = 46,
        Hyakkayouran = 47,
        ShinySparkleSociety = 48,
        AbydosStudentCouncil = 49,
        CentralControlCenter = 50,
        FreightLogisticsDepartment = 51,
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
