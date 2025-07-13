#if UNITY_EDITOR

using System.IO;
using System.Collections;
using UnityEngine;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
using TMPro;
using Spine.Unity;

public class Recorder_Services : MonoBehaviour
{
    public static Recorder_Services Instance { get; set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[Awake] Recorder Services 单例创建完成");
        }
        else
        {
            Destroy(gameObject);
        }
    }



    public bool is_Record = false;

    [Header("UI Elements")]
    [SerializeField]
    public TMP_InputField Recorder_InputField;

    [Header("Record Settings")]
    public float Recorder_Camera_Size = 1.7f;
    public int Recorder_Width = 1920;
    public int Recorder_Height = 1080;
    public float Recorder_Framrate = 60.0f;
    public float Talk_Record_Start_Delay = 2.0f;
    public float Talk_Record_End_Delay = 4.0f;
    public float Talk_Record_Gap_Delay = 2.0f;
    public float Record_End_Delay = 5f;

    [Header("Core Variables")]
    public RecorderController recorder_controller;

    private void Start()
    {
        if (is_Record)
        {
            Recorder_InputField.gameObject.SetActive(true);
            Recorder_InputField.onEndEdit.AddListener(Start_Recording);
        }
    }

    public void Start_Recording(string value)
    {
        Init(value);

        Character_Services.Instance.Switch_Character(value);

        Spine_Services.Instance.is_IK_On = false;
        Spine_Services.Instance.Update_IK_Button();

        Spine_Services.Instance.is_Talk_On = false;
        Spine_Services.Instance.Update_Talk_Button();

        Audio_Services.Instance.Talk_Slider_Handler(100);

        Audio_Services.Instance.SFX_Slider_Handler(100);

        Audio_Services.Instance.BGM_Slider_Handler(100);

        Wallpaper_Services.Instance.Clear_HUD();

        Recorder_InputField.GetComponent<CanvasGroup>().alpha = 0f;
    }

    public void Init(string character_name)
    {
        MovieRecorderSettings movie_recorder_settings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movie_recorder_settings.name = "Video Recorder";
        movie_recorder_settings.Enabled = true;
        movie_recorder_settings.VideoBitRateMode = UnityEditor.VideoBitrateMode.High;
        movie_recorder_settings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = Recorder_Width,
            OutputHeight = Recorder_Height
        };
        movie_recorder_settings.AudioInputSettings.PreserveAudio = true;
        movie_recorder_settings.OutputFile = Path.Combine("RecordVideos", character_name);

        RecorderControllerSettings recorder_controller_settings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        recorder_controller = new RecorderController(recorder_controller_settings);
        recorder_controller_settings.AddRecorderSettings(movie_recorder_settings);
        recorder_controller.Settings.SetRecordModeToManual();
        recorder_controller_settings.FrameRate = Recorder_Framrate;
        recorder_controller.PrepareRecording();
    }

    public IEnumerator Play_Talk_Clips_StoryMode(SkeletonAnimation skeleton_animation)
    {
        yield return new WaitForSeconds(Talk_Record_Start_Delay);

        for (int i = 1; i <= Index_Services.Instance.Talk_Animaiton_Num; i++)
        {
            yield return StartCoroutine(Spine_Services.Instance.Play_Talk_Clips(i, skeleton_animation));
            yield return new WaitForSeconds(Talk_Record_Gap_Delay);
        }

        yield return new WaitForSeconds(Talk_Record_End_Delay);

        recorder_controller.StopRecording();
        Console_Log("停止录制");
    }

    public static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Recorder_Services", message, loglevel, logtype); }
}
#endif
