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

    bool is_First_Get = true;
    void Start()
    {
        Wallpaper_Mode_Toggle_Button.onClick.AddListener(Destroy_Options);

        mainButton.onClick.AddListener(ToggleDropdown);
        folderPath = File_Services.Student_Files_Folder_Path;
        LoadFolderNames();

        optionTemplate.SetActive(false);
        dropdownPanel.SetActive(false);

        // ��ȡ�ؼ����
        contentLayoutGroup = content.GetComponent<VerticalLayoutGroup>();

        // ȷ��������ͼ������ȷ
        scrollRect.viewport = viewport;
        scrollRect.content = content;
        scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // ��ģ���ȡѡ��߶�
        InitializeOptionHeight();

        // ����Ĭ�Ͻ�ɫ������ͼ
        SetDefaultCharacterThumbnail();
    }

    //д����ȡѡ��߶�
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
        yield return null; // �ȴ�UI���ּ������
        PopulateOptions();
    }

    void PopulateOptions()
    {
        // ��̬����ѡ��
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
                        // �������ʧ�ܣ�ʹ��Ĭ��ͼ��
                        icon.texture = defaultIcon;
                        Debug.LogWarning($"[Dropdown_Services] �޷�����ѡ������ͼ: {thumbnailPath}");
                    }
                }
                else
                {
                    icon.texture = defaultIcon;
                    Debug.LogWarning($"[Dropdown_Services] ��ɫ {folderNames[i]} �� Lobbyillust ��δ�ҵ�");
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

        // ���¹���ϵͳ
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
        // ���������ݸ߶�
        float totalContentHeight = CalculateTotalContentHeight();

        // ������������ߴ�
        content.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, totalContentHeight);

        // �������ɼ��߶�
        float maxVisibleHeight = maxVisibleOptions * optionHeight
            + contentLayoutGroup.padding.top
            + contentLayoutGroup.padding.bottom
            + Mathf.Max(0, maxVisibleOptions - 1) * contentLayoutGroup.spacing;

        // �����ӿڸ߶�
        viewport.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, maxVisibleHeight);

        // ����/���ù���
        bool needsScroll = totalContentHeight > maxVisibleHeight;
        scrollRect.vertical = needsScroll;
        scrollRect.verticalScrollbar.gameObject.SetActive(needsScroll);

        // ���ù���λ��
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

        // ��Ӽ��
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
        Debug.Log($"[Dropdown_Services] ѡ���ɫ: {selectedCharacter}");

        mainButtonLabel.text = selectedCharacter;

        // ���ز���ʾѧ������ͼ
        if (selectedCharacter != "Textures")
        {
            if (Texture_Services.Lobbyillust.ContainsKey(selectedCharacter))
            {
                string thumbnailPath = Path.Combine(File_Services.Student_Lists_Folder_Path, Texture_Services.Lobbyillust[selectedCharacter] + ".png");
                Texture2D thumbnail = Texture_Services.Get_Texture_By_Path(thumbnailPath);
                if (thumbnail != null)
                {
                    mainButtonIcon.texture = thumbnail;
                    Debug.Log($"[Dropdown_Services] �ɹ����ؽ�ɫ����ͼ: {selectedCharacter}");
                }
                else
                {
                    // �������ʧ�ܣ�ʹ��Ĭ��ͼ��
                    mainButtonIcon.texture = defaultIcon;
                    Debug.LogWarning($"[Dropdown_Services] �޷����ؽ�ɫ����ͼ: {thumbnailPath}");
                }
            }
            else
            {
                mainButtonIcon.texture = defaultIcon;
                Debug.LogWarning($"[Dropdown_Services] ��ɫ {selectedCharacter} �� Lobbyillust ��δ�ҵ�");
            }
        }
        else
        {
            mainButtonIcon.texture = defaultIcon;
            Debug.Log("[Dropdown_Services] ѡ�� Textures �ļ��У�ʹ��Ĭ��ͼ��");
        }

        Character_Services.Instance.Switch_Character(selectedCharacter);

        dropdownPanel.SetActive(false);
        isDropdownOpen = false;
    }
}