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
    public float Global_Sound = 0f;
    public float Talk_Sound = 0f;
    public float SFX_Sound = 0f;
    public float BGM_Sound = 0f;
    public List<BGMExcel_DB> BGMExcel_DB_list;

    public void Get_Config()
    {
        Console_Log("开始加载所有 Audio Services 配置文件");

        BGMExcel_DB_list = File_Services.Load_Specific_Type_From_File<List<BGMExcel_DB>>(Path.Combine(File_Services.MX_Files_TableBundles_Folder_Path, "BGMExcel.json"));
        Console_Log($"获取到 {BGMExcel_DB_list.Count} 首 BGM 信息");

        Console_Log("结束加载所有 Audio Services 配置文件");
    }

    private void Start()
    {
        Get_Config();
    }

    public void Global_Sound_Slider_Handler(float value)
    {
        Global_Sound = value;
        Master_Audio_Mixer_Group.audioMixer.SetFloat("Global_Volume", Get_Decibels(value, -80, 15));
    }

    public void Talk_Slider_Handler(float value)
    {
        Talk_Sound = value;
        Talk_Audio_Mixer_Group.audioMixer.SetFloat("Talk_Volume", Get_Decibels(value, -80, 0));
    }

    public void SFX_Slider_Handler(float value)
    {
        SFX_Sound = value;
        SFX_Audio_Mixer_Group.audioMixer.SetFloat("SFX_Volume", Get_Decibels(value, -80, -5));
    }

    public void BGM_Slider_Handler(float value)
    {
        BGM_Sound = value;
        BGM_Audio_Mixer_Group.audioMixer.SetFloat("BGM_Volume", Get_Decibels(value, -80, -25));
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

    public IEnumerator Play_AudioClip(
        AudioClip_Type audioclip_type,
        string audio_file_path,
        AudioClip audio_clip = null,
        bool is_loop = false,
        float loop_start_time = 0f,
        float loop_end_time = 0f
    )
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

        bool is_use_custom_loop = is_loop && loop_start_time != 0f && loop_end_time != 0f;

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
                if (is_use_custom_loop)
                {
                    audio_source.loop = false;
                    Audio_Loop_Controller audio_loop_controller = audio_source.gameObject.AddComponent<Audio_Loop_Controller>();
                    audio_loop_controller.Initialize(loop_start_time, loop_end_time);
                }
                else
                {
                    audio_source.loop = true;
                }
                break;
        }

        audio_source.Play();

        if(!audio_source.loop && !is_use_custom_loop)
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
