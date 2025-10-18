using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;

//注意，该功能依赖网络连接

public class Changelog_Services : MonoBehaviour
{
    public static Changelog_Services Instance { get; set; }

    [Header("UI Elements")]
    [SerializeField] public GameObject Changelog_Panel;
    [SerializeField] public TextMeshProUGUI Changelog_Title_Text;
    [SerializeField] public TextMeshProUGUI Changelog_Content_Text;
    [SerializeField] public Button Changelog_Close_Button;

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

    private void Start()
    {
        if (Changelog_Close_Button != null)
        {
            Changelog_Close_Button.onClick.AddListener(Hide_Changelog);
        }
        if (Changelog_Panel != null)
        {
            Changelog_Panel.SetActive(false);
        }
    }

    public void Show_Changelog()
    {
        if (Version_Services.Instance == null)
        {
            Toast_Wrapper_Services.ShowToast("Version service not available", 3f);
            return;
        }
        
        string version = Version_Services.Instance.Version_String;
        
        if (Changelog_Panel == null)
        {
            Toast_Wrapper_Services.ShowToast($"Fetching changelog for {version}...", 3f);
        }
        
        StartCoroutine(Fetch_Changelog(version));
    }

    private IEnumerator Fetch_Changelog(string version)
    {
        string apiUrl = $"https://sc.japerz.com/api/v{version}";
        
        if (Changelog_Panel != null) Changelog_Panel.SetActive(true);
        if (Changelog_Title_Text != null) Changelog_Title_Text.text = "Loading...";
        if (Changelog_Content_Text != null) Changelog_Content_Text.text = "Fetching changelog...";

        using (UnityWebRequest request = UnityWebRequest.Get(apiUrl))
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonConvert.DeserializeObject<ChangelogResponse>(request.downloadHandler.text);
                    if (response != null && response.success && response.data != null)
                    {
                        Display_Changelog(response.data);
                    }
                    else
                    {
                        Display_Error("Failed to parse changelog data");
                    }
                }
                catch (Exception ex)
                {
                    Display_Error($"Error: {ex.Message}");
                }
            }
            else
            {
                Display_Error($"Failed to fetch changelog\n{request.error}");
            }
        }
    }

    private void Display_Changelog(ChangelogData data)
    {
        if (Changelog_Title_Text != null)
        {
            Changelog_Title_Text.text = $"{data.version} - {data.date}";
        }
        
        if (Changelog_Content_Text != null)
        {
            Changelog_Content_Text.text = data.notes;
        }
        
        if (Changelog_Panel == null)
        {
            Toast_Wrapper_Services.ShowToast($"Changelog loaded for {data.version}", 3f);
        }
    }

    private void Display_Error(string error)
    {
        if (Changelog_Title_Text != null) Changelog_Title_Text.text = "Error";
        if (Changelog_Content_Text != null) Changelog_Content_Text.text = error;
        
        if (Changelog_Panel == null)
        {
            Toast_Wrapper_Services.ShowToast(error, 5f);
        }
    }

    public void Hide_Changelog()
    {
        if (Changelog_Panel != null)
        {
            Changelog_Panel.SetActive(false);
        }
    }

    [Serializable]
    public class ChangelogResponse
    {
        public bool success;
        public ChangelogData data;
    }

    [Serializable]
    public class ChangelogData
    {
        public string version;
        public string branch;
        public string date;
        public string notes;
        public string updated_at;
        public string created_at;
    }
}

