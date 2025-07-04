using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Networking;
using TMPro;
using MX.Audio;


public class Audio_Services : MonoBehaviour
{
    public static Audio_Services Instance { get; set; }
    public void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Audio Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public enum AudioClip_Type
    {
        Talk,
        SFX,
        BGM
    }

    [Header("UI Elements")]
    [SerializeField]
    public Button Sound_Toggle_Button;
    [SerializeField]
    public RawImage Sound_On_Image;
    [SerializeField]
    public RawImage Sound_Off_Image;
    [SerializeField]
    public TextMeshProUGUI Talk_Text;
    [SerializeField]
    public Slider Talk_Slider;
    [SerializeField]
    public Button Talk_Toggle_Button;
    [SerializeField]
    public RawImage Talk_On_Image;
    [SerializeField]
    public RawImage Talk_Off_Image;
    [SerializeField]
    public TextMeshProUGUI SFX_Text;
    [SerializeField]
    public Slider SFX_Slider;
    [SerializeField]
    public Button SFX_Toggle_Button;
    [SerializeField]
    public RawImage SFX_On_Image;
    [SerializeField]
    public RawImage SFX_Off_Image;
    [SerializeField]
    public TextMeshProUGUI BGM_Text;
    [SerializeField]
    public Slider BGM_Slider;
    [SerializeField]
    public Button BGM_Toggle_Button;
    [SerializeField]
    public RawImage BGM_On_Image;
    [SerializeField]
    public RawImage BGM_Off_Image;

    [SerializeField]
    public AudioMixerGroup Master_Audio_Mixer_Group;
    [SerializeField]
    public AudioMixerGroup Talk_Audio_Mixer_Group;
    [SerializeField]
    public AudioMixerGroup SFX_Audio_Mixer_Group;
    [SerializeField]
    public AudioMixerGroup BGM_Audio_Mixer_Group;

    [SerializeField]
    public GameObject Talk_GameObject;
    [SerializeField]
    public GameObject SFX_GameObject;
    [SerializeField]
    public GameObject BGM_GameObject;

    [Header("Core Variables")]
    public bool is_Global_Sound_On = true;
    public bool is_Talk_Sound_On = true;
    public float Talk_Sound = 0f;
    public bool is_SFX_Sound_On = true;
    public float SFX_Sound = 0f;
    public bool is_BGM_Sound_On = true;
    public float BGM_Sound = 0f;
    public List<BGMExcel_DB> BGMExcel_DB_list;

    public void Get_Config()
    {
        Console_Log("开始加载所有 Audio Services 配置文件");

        is_Global_Sound_On = Config_Services.Instance.Global_Function_Config.is_Global_Sound_On;
        Update_Global_Sound_Button_UI();

        is_Talk_Sound_On = Config_Services.Instance.Global_Function_Config.is_Talk_Sound_On;
        Update_Talk_Sound_Button_UI();
        Talk_Sound = Config_Services.Instance.Global_Function_Config.Talk_Sound;
        Talk_Slider_Handler(Talk_Sound);

        is_SFX_Sound_On = Config_Services.Instance.Global_Function_Config.is_SFX_Sound_On;
        Update_SFX_Sound_Button_UI();
        SFX_Sound = Config_Services.Instance.Global_Function_Config.SFX_Sound;
        SFX_Slider_Handler(SFX_Sound);

        is_BGM_Sound_On = Config_Services.Instance.Global_Function_Config.is_BGM_Sound_On;
        Update_BGM_Sound_Button_UI();
        BGM_Sound = Config_Services.Instance.Global_Function_Config.BGM_Sound;
        BGM_Slider_Handler(BGM_Sound);

        BGMExcel_DB_list = File_Services.Load_Specific_Type_From_File<List<BGMExcel_DB>>(Path.Combine(File_Services.MX_Files_TableBundles_Folder_Path, "BGMExcel.json"));
        Console_Log($"获取到 {BGMExcel_DB_list.Count} 首 BGM 信息");

        Console_Log("结束加载所有 Audio Services 配置文件");
    }

    public void Set_Config()
    {
        Config_Services.Instance.Global_Function_Config.is_Global_Sound_On = is_Global_Sound_On;
        Config_Services.Instance.Global_Function_Config.is_Talk_Sound_On = is_Talk_Sound_On;
        Config_Services.Instance.Global_Function_Config.Talk_Sound = Talk_Sound;
        Config_Services.Instance.Global_Function_Config.is_SFX_Sound_On = is_SFX_Sound_On;
        Config_Services.Instance.Global_Function_Config.SFX_Sound = SFX_Sound;
        Config_Services.Instance.Global_Function_Config.is_BGM_Sound_On = is_BGM_Sound_On;
        Config_Services.Instance.Global_Function_Config.BGM_Sound = BGM_Sound;
    }

    private void Start()
    {
        Get_Config();
        Sound_Toggle_Button.onClick.AddListener(Toggle_Global_Sound);

        Talk_Toggle_Button.onClick.AddListener(Toggle_Talk_Sound);
        SFX_Toggle_Button.onClick.AddListener(Toggle_SFX_Sound);
        BGM_Toggle_Button.onClick.AddListener(Toggle_BGM_Sound);

        Talk_Slider.onValueChanged.AddListener(Talk_Slider_Handler);
        SFX_Slider.onValueChanged.AddListener(SFX_Slider_Handler);
        BGM_Slider.onValueChanged.AddListener(BGM_Slider_Handler);
    }

    public void Toggle_Global_Sound()
    {
        is_Global_Sound_On = !is_Global_Sound_On;
        if (is_Global_Sound_On)
        {
            Master_Audio_Mixer_Group.audioMixer.SetFloat("Global_Volume", 0f);
        }
        else
        {
            Master_Audio_Mixer_Group.audioMixer.SetFloat("Global_Volume", -80f);
        }
        Update_Global_Sound_Button_UI();
    }

    private void Update_Global_Sound_Button_UI()
    {
        Sound_On_Image.enabled = is_Global_Sound_On;
        Sound_Off_Image.enabled = !is_Global_Sound_On;
    }

    public void Toggle_Talk_Sound()
    {
        is_Talk_Sound_On = !is_Talk_Sound_On;
        if (is_Talk_Sound_On)
        {
            Talk_Slider_Handler(Talk_Sound);
        }
        else
        {
            Talk_Slider_Handler(0f);
        }
        Update_Talk_Sound_Button_UI();
    }

    private void Update_Talk_Sound_Button_UI()
    {
        Talk_On_Image.enabled = is_Talk_Sound_On;
        Talk_Off_Image.enabled = !is_Talk_Sound_On;
    }


    public void Toggle_SFX_Sound()
    {
        is_SFX_Sound_On = !is_SFX_Sound_On;
        if (is_SFX_Sound_On)
        {
            SFX_Slider_Handler(SFX_Sound);
        }
        else
        {
            SFX_Slider_Handler(0f);
        }
        Update_SFX_Sound_Button_UI();
    }

    private void Update_SFX_Sound_Button_UI()
    {
        SFX_On_Image.enabled = is_SFX_Sound_On;
        SFX_Off_Image.enabled = !is_SFX_Sound_On;
    }

    public void Toggle_BGM_Sound()
    {
        is_BGM_Sound_On = !is_BGM_Sound_On;
        if (is_BGM_Sound_On)
        {
            BGM_Slider_Handler(BGM_Sound);
        }
        else
        {
            BGM_Slider_Handler(0f);
        }
        Update_BGM_Sound_Button_UI();
    }

    private void Update_BGM_Sound_Button_UI()
    {
        BGM_On_Image.enabled = is_BGM_Sound_On;
        BGM_Off_Image.enabled = !is_BGM_Sound_On;
    }

    public void Talk_Slider_Handler(float value)
    {
        if (is_Talk_Sound_On)
        {
            Talk_Slider.interactable = true;
            Talk_Sound = value;
            Talk_Slider.value = value;
        }
        else
        {
            Talk_Slider.interactable = false;
            Talk_Slider.value = 0;
        }
        
        Talk_Audio_Mixer_Group.audioMixer.SetFloat("Talk_Volume", Get_Decibels(Talk_Slider.value, -80, -5));
        Update_Talk_Text();
    }

    private void Update_Talk_Text()
    {
        if(is_Talk_Sound_On) Talk_Text.SetText((Talk_Sound * 100).ToString("0"));
        else Talk_Text.SetText("0");
    }

    public void SFX_Slider_Handler(float value)
    {
        if (is_SFX_Sound_On)
        {
            SFX_Slider.interactable = true;
            SFX_Sound = value;
            SFX_Slider.value = value;
        }
        else
        {
            SFX_Slider.interactable = false;
            SFX_Slider.value = 0;
        }
        
        SFX_Audio_Mixer_Group.audioMixer.SetFloat("SFX_Volume", Get_Decibels(SFX_Slider.value, -80, -7));
        Update_SFX_Text();
    }

    private void Update_SFX_Text()
    {
        if (is_SFX_Sound_On) SFX_Text.SetText((SFX_Sound * 100).ToString("0"));
        else SFX_Text.SetText("0");
    }

    public void BGM_Slider_Handler(float value)
    {
        if (is_BGM_Sound_On)
        {
            BGM_Slider.interactable = true;
            BGM_Sound = value;
            BGM_Slider.value = value;
        }
        else
        {
            BGM_Slider.interactable = false;
            BGM_Slider.value = 0;
        }
        
        BGM_Audio_Mixer_Group.audioMixer.SetFloat("BGM_Volume", Get_Decibels(BGM_Slider.value, -80, -25));
        Update_BGM_Text();
    }
    private void Update_BGM_Text()
    {
        if (is_BGM_Sound_On) BGM_Text.SetText((BGM_Sound * 100).ToString("0"));
        else BGM_Text.SetText("0");
    }

    public static float Get_Decibels(float value, float minDecibel, float maxDecibel)
    {
        value = Mathf.Clamp01(value);
        float minLinear = Mathf.Pow(10, minDecibel / 20f);
        float maxLinear = Mathf.Pow(10, maxDecibel / 20f);
        float linearValue = Mathf.Lerp(minLinear, maxLinear, value);
        if (linearValue <= Mathf.Pow(10, minDecibel / 20f) * 1.0001f)
        {
            return minDecibel;
        }
        return 20.0f * Mathf.Log10(linearValue);
    }

    public IEnumerator Play_AudioClip(AudioClip_Type audioclip_type, string audio_file_path, AudioClip audio_clip = null, bool is_loop = false)
    {
        AudioSource audio_source = null;

        if (audio_clip == null)
        {
            yield return StartCoroutine(Get_AudioClip_By_Path_Async(audio_file_path, local_audio_clip =>
            {
                if (local_audio_clip != null)
                {
                    audio_clip = local_audio_clip;
                }
            }));
        }

        switch (audioclip_type)
        {
            case AudioClip_Type.Talk:
                audio_source = Talk_GameObject.AddComponent<AudioSource>();
                audio_source.outputAudioMixerGroup = Talk_Audio_Mixer_Group;
                audio_source.clip = audio_clip;
                audio_source.loop = false;
                break;

            case AudioClip_Type.SFX:
                break;

            case AudioClip_Type.BGM:
                audio_source = BGM_GameObject.AddComponent<AudioSource>();
                audio_source.outputAudioMixerGroup = BGM_Audio_Mixer_Group;
                audio_source.clip = audio_clip;
                audio_source.loop = true;
                break;
        }

        audio_source.Play();

        if(!audio_source.loop)
        {
            yield return new WaitForSeconds(audio_clip.length);
            Destroy(audio_source);
        }
    }

    public void Play_AudioClip(AudioClip_Type audioclip_type, AudioSourceData audio_source_data) { StartCoroutine(Play_AudioClip_Coroutine(audioclip_type, audio_source_data)); }

    public IEnumerator Play_AudioClip_Coroutine(AudioClip_Type audioclip_type, AudioSourceData audio_source_data)
    {
        AudioSource audio_source = null;

        switch (audioclip_type)
        {
            case AudioClip_Type.Talk:
                break;

            case AudioClip_Type.SFX:
                audio_source = SFX_GameObject.AddComponent<AudioSource>();
                audio_source.outputAudioMixerGroup = SFX_Audio_Mixer_Group;
                audio_source.clip = audio_source_data.AudioClips[0];
                audio_source.loop = audio_source_data.Loop;
                
                break;

            case AudioClip_Type.BGM:
                break;
        }

        audio_source.Play();
        yield return new WaitForSeconds(audio_source.clip.length);
        Destroy(audio_source);
    }

    public static void Remove_All_AudioSources(GameObject gameobject)
    {
        if (gameobject == null)
        {
            Console_Log("目标 GameObject 为空", Debug_Services.LogLevel.Debug, LogType.Error);
            return;
        }

        List<AudioSource> audio_sources = gameobject.GetComponentsInChildren<AudioSource>(true).ToList();

        foreach (AudioSource audio_source in audio_sources)
        {
            if (audio_source != null)
            {
                Destroy(audio_source);
                Console_Log($"已移除在 {gameobject} 上的 AudioSource 组件: {audio_source.name}");
            }
        }
    }

    public IEnumerator Get_AudioClip_By_Path_Async(string audio_file_path, System.Action<AudioClip> onLoaded)
    {
        string full_path = Path.Combine("file:///", audio_file_path);
        string local_audioclip_name = Path.GetFileName(audio_file_path);
        Console_Log($"本地 AudioClip {local_audioclip_name} 加载路径: {full_path}");
        using (UnityWebRequest unity_web_request = UnityWebRequestMultimedia.GetAudioClip(full_path, UnityEngine.AudioType.OGGVORBIS))
        {
            yield return unity_web_request.SendWebRequest();
            if (unity_web_request.result == UnityWebRequest.Result.Success)
            {
                AudioClip audioclip = DownloadHandlerAudioClip.GetContent(unity_web_request);
                audioclip.name = local_audioclip_name;
                Console_Log($"本地 AudioClip {local_audioclip_name} 加载成功");
                onLoaded?.Invoke(audioclip);
            }
            else
            {
                Console_Log($"本地 AudioClip {local_audioclip_name} 加载失败: {unity_web_request.error}", Debug_Services.LogLevel.Debug, LogType.Error);
                onLoaded?.Invoke(null);
            }
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Audio_Services", message, loglevel, logtype); }
}