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
    
    [Header("Settings")]
    public Texture2D defaultIcon;

    private List<string> starredCharacterNames = new List<string>();
    private Dictionary<long, List<CharacterData>> characterList = new Dictionary<long, List<CharacterData>>();
    private bool isDataLoaded, isUpdatingFromCharacterServices;
    private string currentSelectedCharacter = "";
    private List<GameObject> activeButtons = new List<GameObject>(); // 活跃的按钮列表

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
    }

    IEnumerator DelayedInitialization()
    {
        yield return null;
        if (starredListButton == null) yield break;
        
        LoadCharacterData();
        LoadStarredCharacters();
        SetupCharacterButtons();
        
        // 绑定按钮点击事件
        BindButtonEvents();
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
            }
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
        
        // 如果收藏数量小于2，隐藏面板
        if (starredCharacterNames.Count < 2)
        {
            starredListPanel.SetActive(false);
            return;
        }
        
        // 显示面板
        starredListPanel.SetActive(true);
        
        // 为每个收藏学生创建按钮
        foreach (string characterName in starredCharacterNames)
        {
            CreateCharacterButton(characterName);
        }
    }
    
    void CreateCharacterButton(string characterName)
    {
        if (characterButtonPrefab == null) return;
        
        GameObject buttonObj = Instantiate(characterButtonPrefab, starredListContentGrid);
        activeButtons.Add(buttonObj);
        
        // 设置按钮图标
        var imageComponent = buttonObj.GetComponent<Image>();
        if (imageComponent != null)
        {
            imageComponent.sprite = GetCharacterSprite(characterName);
        }
        
        // 设置按钮点击事件
        var buttonComponent = buttonObj.GetComponent<Button>();
        if (buttonComponent != null)
        {
            string capturedName = characterName; // 捕获变量
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
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[characterName] + ".png");
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
    }
}