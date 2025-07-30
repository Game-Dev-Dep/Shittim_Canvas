using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterList_Services : MonoBehaviour
{
    [SerializeField]
    public GameObject Character_List_Root_GameObject;
    [SerializeField]
    public GameObject Character_List_Search_Result_Content_GameObject;
    [SerializeField]
    public TMP_InputField Character_List_Search_BarInputField;
    [SerializeField]
    public GameObject Character_Portrait_Template;
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

    public float Character_Portrait_Width;
    public float Character_Portrait_Height;
    public float Character_Portrait_Spacing_X;
    public float Character_Portrait_Spacing_Y;

    public Dictionary<long, List<Character>> Character_List = new Dictionary<long, List<Character>>();

    public bool is_Character_List_On = false;

    public void Get_Charcter_List()
    {
        string json = File.ReadAllText(Path.Combine(File_Services.Student_Lists_Folder_Path, "CharacterList.json"));
        Character_List = JsonConvert.DeserializeObject<Dictionary<long, List<Character>>>(json);
    }

    private void Start()
    {
        Console_Log("开始初始化 Character_Services");

        Character_List_Toggle_Button.onClick.AddListener(Toggle_Character_List_Panel);
        Character_List_Exit_Button.onClick.AddListener(Hide_Character_List_Panel);

        Get_Charcter_List();
        Get_Detail_Option_UI_Parameters();

        Console_Log("结束初始化 Character_Services");
    }

    void Toggle_Character_List_Panel()
    {
        is_Character_List_On = !is_Character_List_On;
        if (is_Character_List_On)
        {
            Display_Character_List_Panel();
        }
        else
        {
            Hide_Character_List_Panel();
        }
    }

    void Display_Character_List_Panel()
    {
        is_Character_List_On = true;
        Character_List_Root_GameObject.SetActive(is_Character_List_On);
        Create_Character_List_UI();
    }

    void Hide_Character_List_Panel()
    {
        is_Character_List_On = false;
        Character_List_Root_GameObject.SetActive(is_Character_List_On);
        Destroy_Setting_Content_UI();
    }

    void Get_Detail_Option_UI_Parameters()
    {
        Character_Portrait_Width = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().cellSize.x;
        Character_Portrait_Height = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().cellSize.y;
        Character_Portrait_Spacing_X = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().spacing.x;
        Character_Portrait_Spacing_Y = Character_List_Search_Result_Content_GameObject.GetComponent<GridLayoutGroup>().spacing.y;
    }

    public void Create_Character_List_UI()
    {
        Console_Log("开始创建角色列表UI");

        Character_List_Search_Result_Content_GameObject.GetComponent<RectTransform>().sizeDelta = new Vector2(0, (Character_Portrait_Height + Character_Portrait_Spacing_X) * ((Character_List.Count / 5) + 1 ) + Character_Portrait_Spacing_X);

        foreach (var character in Character_List)
        {
            GameObject character_portrait_gameobject = Instantiate(Character_Portrait_Template, Character_List_Search_Result_Content_GameObject.transform);
            character_portrait_gameobject.SetActive(true);

            RawImage character_portrait_rawimage_component = character_portrait_gameobject.GetComponent<RawImage>();
            character_portrait_rawimage_component.texture = Texture_Services.Get_Texture_By_Path(Path.Combine(File_Services.Student_Lists_Folder_Path,$"Student_Portrait_{character.Value.First().Name}_Collection.png"));

            Button character_portrait_button = character_portrait_gameobject.GetComponent<Button>();
            character_portrait_button.onClick.AddListener(() =>
            {
                Character_Select_Handler(character.Key, character.Value.First().Name, character.Value.Count);
            });

            TextMeshProUGUI character_portrait_text_component = character_portrait_gameobject.transform.Find("[Character List] Character Name Text").GetComponent<TextMeshProUGUI>();
            character_portrait_text_component.text = character.Value.First().Name;
        }

        Console_Log("结束创建角色列表UI");
    }

    void Destroy_Setting_Content_UI()
    {
        foreach (Transform child in Character_List_Search_Result_Content_GameObject.transform)
        {
            if (child != Character_Portrait_Template.transform) Destroy(child.gameObject);
        }
    }

    private void Character_Select_Handler(long character_id, string character_name, int character_num)
    {
        Console_Log($"已选择角色: {character_id} {character_name} 角色含有: {character_num} 个变体");
        if (character_num == 1)
        {
            Character_Services.Instance.Switch_Character(character_name);
            Character_List_Root_GameObject.SetActive(false);
            Destroy_Setting_Content_UI();
        }
        else
        {
            Multi_Lobby_Root_GameObject.SetActive(true);
            Multi_Lobby_Dropdown.ClearOptions();
            List<string> character_names = new List<string>();
            foreach (Character character in Character_List[character_id])
            {
                character_names.Add(character.Name);
            }
            Multi_Lobby_Dropdown.AddOptions(character_names);
            Multi_Lobby_Confirm_Button.onClick.AddListener(() =>
            {
                int index = Multi_Lobby_Dropdown.value;
                Multi_Lobby_Select_Handler(index, character_id);
            });
            Multi_Lobby_Quit_Button.onClick.AddListener(() =>
            {
                Multi_Lobby_Root_GameObject.SetActive(false);
            });
        }
        
    }

    private void Multi_Lobby_Select_Handler(int index, long character_id)
    {
        Console_Log($"已选择角色的变体: {character_id} {(Character_List[character_id].ToArray())[index].Name}");
        Character_Services.Instance.Switch_Character((Character_List[character_id].ToArray())[index].Name);
        Multi_Lobby_Root_GameObject.SetActive(false);

        Character_List_Root_GameObject.SetActive(false);
        Destroy_Setting_Content_UI();
    }

    public class Character
    {
        public string Name = string.Empty;
        public List<string> Nicknames = new List<string>();
        public School Shcool = new School();
        public Club Club = new Club();
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
        Sakugawa = 15
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
    }



    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
