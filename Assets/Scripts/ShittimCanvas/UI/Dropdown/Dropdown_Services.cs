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
            Debug.Log("[Awake] Dropdown Services �����������");
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
    public float optionHeight = 123f; //����д���ģ��Ĵ�㡣

    private List<string> folderNames = new List<string>();
    private bool isDropdownOpen = false;
    private VerticalLayoutGroup contentLayoutGroup;

    public TMP_InputField searchInputField;
    private string searchKeyword = "";
    private List<string> filteredFolderNames = new List<string>();

    private List<GameObject> optionPool = new List<GameObject>();
    private List<GameObject> activeOptions = new List<GameObject>();

    private float debounceTime = 0.15f; // 150ms����
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

        //�����ϴ�ѡ���ѧ��
        string lastSelected = PlayerPrefs.GetString("Dropdown_SelectedCharacter", "");
        if (!string.IsNullOrEmpty(lastSelected))
        {
            StartCoroutine(DelaySetPreview(lastSelected));
        }
        else
        {
            SetDefaultCharacterThumbnail();
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
                Debug.Log($"[Dropdown_Services] ��ģ���ȡѡ��߶�: {optionHeight}");
            }
        }
    }

    void SetDefaultCharacterThumbnail()
    {
        if (mainButtonIcon == null) return;

        // ��ȡĬ�Ͻ�ɫ����
        string defaultCharacterName = Config_Services.Instance.MemoryLobby_Camera_Config.Defalut_Character_Name;
        
        // ����Ĭ�ϱ�ǩ�ı�
        if (mainButtonLabel != null)
        {
            mainButtonLabel.text = defaultCharacterName;
        }

        // ����Ĭ�Ͻ�ɫ������ͼ
        if (defaultCharacterName != "Textures" && Texture_Services.Lobbyillust.ContainsKey(defaultCharacterName))
        {
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[defaultCharacterName] + ".png");
            Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
            if (thumbnail != null)
            {
                mainButtonIcon.texture = thumbnail;
                Debug.Log($"[Dropdown_Services] �ɹ�����Ĭ�Ͻ�ɫ����ͼ: {defaultCharacterName}");
                return;
            }
            else
            {
                Debug.LogWarning($"[Dropdown_Services] �޷�����Ĭ�Ͻ�ɫ����ͼ: {thumbnailPath}");
            }
        }
        else
        {
            Debug.LogWarning($"[Dropdown_Services] Ĭ�Ͻ�ɫ {defaultCharacterName} �� Lobbyillust ��δ�ҵ�");
        }

        // �������ʧ�ܣ�ʹ��Ĭ��ͼ��
        if (defaultIcon != null)
        {
            mainButtonIcon.texture = defaultIcon;
            Debug.Log("[Dropdown_Services] ʹ��Ĭ��ͼ��");
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
            folderNames.Sort();
            Debug.Log($"[Dropdown_Services] ������ {folderNames.Count} ���ļ��У�������");
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
            //��������
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
        //Ԥ��������
        int poolSize = Mathf.Min(folderNames.Count, 20); //�ȴ�20��
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
            //��֤������
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
        //���յ�ǰ��ʾ��ѡ��
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
        int batchSize = 5; //��������ѡ��
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

        // ����ѡ���ı����Լ�һ��Debug Log
        TMP_Text optionText = option.GetComponentInChildren<TMP_Text>();
        if (optionText != null)
        {
            optionText.text = optionName;
        }
        else
        {
            Debug.LogError($"[Dropdown_Services] ѡ�� {optionName} û���ҵ�TMP_Text���");
        }
    }

    void UpdateScrollSystem()
    {
        // ǿ������VerticalLayoutGroupΪ�������룬��ֹContent Anchor�Գ�
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
        mainButtonLabel.text = selectedCharacter;
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
        //����ѡ���ѧ��
        PlayerPrefs.SetString("Dropdown_SelectedCharacter", selectedCharacter);
        PlayerPrefs.Save();
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
            // Filter�߼�����������һ����׼��һ��ǰ׺��һ��ģ��
            filteredFolderNames = new List<string>();
            
            //��ȷ
            var exactMatch = folderNames.FirstOrDefault(name => name.ToLower() == searchKeyword);
            if (exactMatch != null)
            {
                filteredFolderNames.Add(exactMatch);
            }
            else
            {
                //ǰ׺
                var startsWithMatches = folderNames.Where(name => name.ToLower().StartsWith(searchKeyword)).ToList();
                filteredFolderNames.AddRange(startsWithMatches);
                
                //ģ��
                if (filteredFolderNames.Count == 0)
                {
                    var containsMatches = folderNames.Where(name => name.ToLower().Contains(searchKeyword)).ToList();
                    filteredFolderNames.AddRange(containsMatches);
                }
            }
        }
        
        //��Sortһ��
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
        yield return null; // ��һ֡
        if (contentLayoutGroup != null)
        {
            contentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
            Canvas.ForceUpdateCanvases();
        }
    }

    IEnumerator DelaySetPreview(string lastSelected)
    {
        yield return null; //���ǵ�һ֡��ͻȻ����ò��ѡ�������ͼ�������
        //Debug��ؾ��������ˣ������⿴log�ܲ�

        mainButtonLabel.text = lastSelected;
        if (lastSelected != "Textures" && Texture_Services.Lobbyillust.ContainsKey(lastSelected))
        {
            string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[lastSelected] + ".png");
            Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
            Debug.Log($"[Dropdown_Services] ���Լ�������ͼ: {thumbnailPath}");
            if (thumbnail != null)
            {
                mainButtonIcon.texture = thumbnail;
            }
            else
            {
                Debug.LogWarning("[Dropdown_Services] ��������ͼʧ�ܣ�ʹ��Ĭ��ͼ��");
                mainButtonIcon.texture = defaultIcon;
            }
        }
        else
        {
            Debug.LogWarning($"[Dropdown_Services] Lobbyillust ������ key: {lastSelected}��ʹ��Ĭ��ͼ��");
            mainButtonIcon.texture = defaultIcon;
        }
    }
}