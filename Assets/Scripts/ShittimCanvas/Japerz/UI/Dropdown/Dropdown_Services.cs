using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

/// <summary>
/// 欢迎来到大改过的Dropdown_Services！
/// 虽然叫Dropdown_Services,但已经NTR给快捷收藏面板用了喵。
/// </summary>
public class Dropdown_Services : MonoBehaviour
{
    public static Dropdown_Services Instance { get; set; }
    
    [Header("UI References")]
    [Header("Starred List Button")]
    public GameObject starredListButton;
    
    [Header("Starred List Panel")]
    public GameObject starredListPanel; 
    public ScrollRect starredListScrollView;
    public Transform starredListContentGrid;
    public GameObject characterButtonPrefab;

    [Header("Favorite List Button")]
    public GameObject favoriteListButton;

    [Header("Character List Services Reference")]
    public CharacterList_Services characterListServices;

    [Header("Settings")]
    public Texture2D defaultIcon;

    private List<string> starredCharacterNames = new List<string>();
    private Dictionary<long, List<CharacterData>> characterList = new Dictionary<long, List<CharacterData>>();
    private bool isDataLoaded, isUpdatingFromCharacterServices;
    private string currentSelectedCharacter = "";
    private List<GameObject> activeButtons = new List<GameObject>();

    [System.Serializable]
    public class CharacterData
    {
        public string DevName, FullNameSC, FullNameTC, FullNameEn, FullNameJp, School, Club;
        public List<string> Nicknames = new List<string>();
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (starredListButton != null) StartCoroutine(DelayedInitialization());
        
        if (favoriteListButton != null) StartCoroutine(DelayedFavoriteButtonInitialization());
        
        if (starredListPanel != null)
            starredListPanel.SetActive(false);
    }

    IEnumerator DelayedInitialization()
{
    yield return null;
    if (starredListButton == null) yield break;
    
    LoadCharacterData();
    LoadStarredCharacters();
    SetupCharacterButtons();
    BindButtonEvents();
}

IEnumerator DelayedFavoriteButtonInitialization()
{
    yield return null;
    if (favoriteListButton == null) yield break;
    
    BindFavoriteButtonEvents();
}
    
    void BindButtonEvents()
    {
        if (starredListButton != null)
        {
            var button = starredListButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(ToggleStarredListPanel);
            }
        }
    }

    void BindFavoriteButtonEvents()
    {
        if (favoriteListButton != null)
        {
            var button = favoriteListButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.AddListener(OpenFavoriteListInCharacterList);
            }
        }
    }
    
    void ToggleStarredListPanel()
    {
        if (starredListPanel != null)
        {
            bool isActive = starredListPanel.activeSelf;
            starredListPanel.SetActive(!isActive);
            
            // 如果面板打开，刷新按钮状态
            if (!isActive)
            {
                SetupCharacterButtons();
                ResetScrollPosition();
            }
        }
    }

    //收藏面板逻辑
    void OpenFavoriteListInCharacterList()
    {
        if (starredListPanel != null)
        {
            starredListPanel.SetActive(false);
        }
        
        if (characterListServices != null)
        {
            if (characterListServices.Character_List_Root_GameObject != null)
            {
                characterListServices.Character_List_Root_GameObject.SetActive(true);
                characterListServices.Create_Character_List_UI();
                StartCoroutine(SwitchToFavoriteMode());
            }
            else
            {
                Debug.LogWarning("[Dropdown_Services] Character_List_Root_GameObject 未找到");
            }
        }
        else
        {
            Debug.LogWarning("[Dropdown_Services] characterListServices 未找到");
        }
    }

IEnumerator SwitchToFavoriteMode()
{
    yield return null;
    
    // 直接操作收藏夹Toggle来切换到收藏夹
    if (characterListServices != null && characterListServices.Favorite_Characters_Toggle != null)
    {
        // 切换到收藏夹
        characterListServices.Favorite_Characters_Toggle.isOn = true;
    }
    else
    {
        Debug.LogWarning("[Dropdown_Services] 无法找到收藏夹Toggle组件");
    }
}
    
    void LoadCharacterData()
    {
        if (isDataLoaded) return;
        try
        {
            string json = File.ReadAllText(Path.Combine(File_Services.Student_Lists_Folder_Path, "CharacterList.json"));
            characterList = JsonConvert.DeserializeObject<Dictionary<long, List<CharacterData>>>(json);
            isDataLoaded = true;
        }
        catch (System.Exception e) { Debug.LogError($"[Starred List Dropdown] 加载角色数据失败: {e.Message}"); }
    }

    void SetupCharacterButtons()
    {
        if (starredListPanel == null || starredListContentGrid == null) return;
        
        // 清理现有按钮
        ClearActiveButtons();
        
        if (starredCharacterNames.Count < 2)
        {
            if (starredListButton != null)
                starredListButton.SetActive(false);
            starredListPanel.SetActive(false);
            return;
        }
        
        if (starredListButton != null)
            starredListButton.SetActive(true);
        
        foreach (string characterName in starredCharacterNames)
        {
            CreateCharacterButton(characterName);
        }
        
        AdjustContentSize();
        ResetScrollPosition();
    }
    
    void CreateCharacterButton(string characterName)
    {
        if (characterButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(characterButtonPrefab, starredListContentGrid);
        buttonObj.SetActive(true);
        activeButtons.Add(buttonObj);
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        if (buttonRect != null)
        {
            buttonRect.localScale = Vector3.one;
            buttonRect.anchoredPosition = Vector2.zero;
        }
        
        var imageComponent = buttonObj.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.sprite = GetCharacterSprite(characterName);
        }
        
        // 设置按钮点击事件
        var buttonComponent = buttonObj.GetComponent<Button>();
        if (buttonComponent != null)
        {
            string capturedName = characterName;
            buttonComponent.onClick.AddListener(() => OnCharacterButtonClicked(capturedName));
        }
    }
    
    void ClearActiveButtons()
    {
        foreach (var button in activeButtons)
        {
            if (button != null)
            {
                Destroy(button);
            }
        }
        activeButtons.Clear();
    }
    
    void OnCharacterButtonClicked(string characterName)
    {
        OnOptionSelected(characterName);
    }

    CharacterData FindCharacterByDevName(string devName) => 
        characterList?.Values.FirstOrDefault(list => list?.FirstOrDefault()?.DevName == devName)?.FirstOrDefault();

    void LoadStarredCharacters()
    {
        starredCharacterNames.Clear();
        if (Config_Services.Instance?.Global_Favorite_Config?.Character_Names != null)
        {
            starredCharacterNames = Config_Services.Instance.Global_Favorite_Config.Character_Names.ToList();
            starredCharacterNames.Sort();
        }
    }

    Sprite GetCharacterSprite(string characterName)
    {
        if (characterName != "Textures" && Texture_Services.Lobbyillust.ContainsKey(characterName))
        {
            string baseFileName = Texture_Services.Lobbyillust[characterName];
            string smallFileName = baseFileName + "_Small";
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, smallFileName + ".png");
            var thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
            if (thumbnail != null) return Sprite.Create(thumbnail, new Rect(0, 0, thumbnail.width, thumbnail.height), Vector2.one * 0.5f);
        }
        return defaultIcon != null ? Sprite.Create(defaultIcon, new Rect(0, 0, defaultIcon.width, defaultIcon.height), Vector2.one * 0.5f) : null;
    }

    void OnOptionSelected(string selectedCharacter)
    {
        if (selectedCharacter == currentSelectedCharacter) return;

        currentSelectedCharacter = selectedCharacter;

        isUpdatingFromCharacterServices = true;
        if (Character_Services.Instance != null) Character_Services.Instance.Switch_Character(selectedCharacter);
        StartCoroutine(ResetUpdateFlag());
    }
    
    private IEnumerator ResetUpdateFlag()
    {
        yield return new WaitForEndOfFrame();
        isUpdatingFromCharacterServices = false;
    }

    // Public methods
    public void RefreshStarredList()
    {
        LoadStarredCharacters();
        SetupCharacterButtons();
    }

    public void AddStarredCharacter(string characterName)
    {
        if (!starredCharacterNames.Contains(characterName))
        {
            starredCharacterNames.Add(characterName);
            starredCharacterNames.Sort();
            RefreshStarredList();
        }
    }

    public void RemoveStarredCharacter(string characterName)
    {
        if (starredCharacterNames.Contains(characterName))
        {
            starredCharacterNames.Remove(characterName);
            RefreshStarredList();
        }
    }

    public bool IsCharacterStarred(string characterName) => starredCharacterNames.Contains(characterName);
    
    public void SetCurrentSelectedCharacter(string characterName)
    {
        if (isUpdatingFromCharacterServices || currentSelectedCharacter == characterName) return;
        currentSelectedCharacter = characterName;
        SetupCharacterButtons();
    }
    
    public string GetCurrentSelectedCharacter() => currentSelectedCharacter;

    void AdjustContentSize()
    {
        if (starredListContentGrid == null) return;
        
        var gridLayout = starredListContentGrid.GetComponent<GridLayoutGroup>();
        if (gridLayout != null)
        {
            int itemsPerRow = Mathf.Max(1, Mathf.FloorToInt(starredListContentGrid.GetComponent<RectTransform>().rect.width / (gridLayout.cellSize.x + gridLayout.spacing.x)));
            int rowsNeeded = Mathf.CeilToInt((float)starredCharacterNames.Count / itemsPerRow);
            
            float contentHeight = (gridLayout.cellSize.y + gridLayout.spacing.y) * rowsNeeded + gridLayout.spacing.y;
            
            var contentRect = starredListContentGrid.GetComponent<RectTransform>();
            contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);
        }
    }
    
    void ResetScrollPosition()
    {
        if (starredListScrollView != null)
        {
            starredListScrollView.normalizedPosition = new Vector2(0, 1);
        }
    }

    void OnDestroy()
    {
        // 清理按钮事件监听器
        if (starredListButton != null)
        {
            var button = starredListButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
        
        // 清理收藏夹按钮事件监听器
        if (favoriteListButton != null)
        {
            var button = favoriteListButton.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
            }
        }
    }
}