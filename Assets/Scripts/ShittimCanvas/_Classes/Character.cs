using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Spine;
using Spine.Unity;
using Spine.Unity.Playables;

#if UNITY_EDITOR
using UnityEditor.Recorder;
#endif

public class Character : MonoBehaviour
{
    public static string Character_Name;
    public string Character_Folder_Path;
    public MemoryLobby_Info memory_lobby_info;
    public string BGM_Path;
    public Coroutine BGM_Corotutine;

    bool is_Can_Show_Subtitle = true;
    bool has_Start_Idle_Audio = false;

    public GameObject lobby_gameobject_prefab;
    public GameObject lobby_gameobject_instantiated;

    public Volume volume_component;

    public SkeletonAnimation skeleton_animation;
    public int Talk_Animaiton_Num = 0;
    public bool is_Character_Idle_Mode = false;

    public PlayableDirector player_director;
    public TimelineAsset timeline_asset;
    public SpineAnimationStateTrack talk_m_track;
    public SpineAnimationStateTrack talk_a_track;

#if UNITY_EDITOR
    public bool is_StoryMode_Started = false;
    public bool is_StoryMode_Camera_Fixed = false;
    public bool is_Screenshot_Took = false;
#endif

    public GameObject EyeIK_GameObject;
    private bool is_EyeIK_GameObject_Founded = false;

    private void Update()
    {

#if UNITY_EDITOR
        if(Recorder_Services.Instance.is_Record && Camera_Services.Instance.MemoryLobby_Camera != null && !is_StoryMode_Camera_Fixed)
        {
            Camera_Services.Instance.MemoryLobby_Camera.transform.position = new Vector3(0f, 0f, 0f);
            Camera_Services.Instance.MemoryLobby_Camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            Camera_Services.Instance.MemoryLobby_Camera.orthographicSize = Recorder_Services.Instance.Recorder_Camera_Size;
            is_StoryMode_Camera_Fixed = true;
            Recorder_Services.Console_Log($"�ѵ������������: {Recorder_Services.Instance.Recorder_Camera_Size}");
        }
#endif

        if (!is_EyeIK_GameObject_Founded)
        {
            if (GameObject.Find("EyeIK") != null)
            {
                EyeIK_GameObject = GameObject.Find("EyeIK");
                is_EyeIK_GameObject_Founded = true;
            }
        }
        else
        {
            if (Camera_Services.Instance.MemoryLobby_Camera.orthographicSize > 1)
            {
                EyeIK_GameObject.transform.localScale = new Vector3(Camera_Services.Instance.MemoryLobby_Camera.orthographicSize, Camera_Services.Instance.MemoryLobby_Camera.orthographicSize, 1);
            }
            else
            {
                if (EyeIK_GameObject.transform.localScale != new Vector3(1, 1, 1))
                {
                    EyeIK_GameObject.transform.localScale = new Vector3(1, 1, 1);
                }
            }
        }

        if (!is_Character_Idle_Mode)
        {
            if (skeleton_animation.AnimationState.GetCurrent(0)?.Animation.Name == "Idle_01") is_Character_Idle_Mode = true;
            else is_Character_Idle_Mode = false; 
            Index_Services.Instance.is_Idle_Mode = is_Character_Idle_Mode;
        }
        else
        {
#if UNITY_EDITOR

            if (Recorder_Services.Instance.is_Record && !is_StoryMode_Started)
            {
                StartCoroutine(Recorder_Services.Instance.Play_Talk_Clips_StoryMode(skeleton_animation));
                is_StoryMode_Started = true;
                Recorder_Services.Console_Log("�ѿ�ʼ�Զ����ž���");
            }

            if (Screenshot_Services.Instance.is_Screenshot)
            {
                if (!is_Screenshot_Took)
                {
                    Screenshot_Services.Instance.Take_Screenshot(Character_Name);
                    is_Screenshot_Took = true;
                }
            }
#endif
        }
    }

    
    public void Load_Charachter(string character_name)
    {
        Console_Log($"�� ======================================== ��");
        Console_Log($"��ʼ���ؽ�ɫ: {character_name}");
        Index_Services.Instance.Character_Name = character_name;
        Character_Name = character_name;
        Character_Folder_Path = Path.Combine(File_Services.Student_Files_Folder_Path, character_name);
        Load_Character_Base_Info();
        Load_Character_Bundles();
        Instantiate_Character_GameObject();
        Init_SkeletonAnimation();
        Init_PlayableDirector();
        Init_TimelineAsset();
        Disable_ChatDialog_GameObject();
        
        Shader_Services.Instance.Replace_Render_Shader(lobby_gameobject_instantiated);
        Shader_Services.Instance.Replace_All_Spine_Shader();

        player_director.RebindPlayableGraphOutputs();
        player_director.Play();

#if UNITY_EDITOR

        if (Recorder_Services.Instance.is_Record)
        {
            Recorder_Services.Instance.recorder_controller.StartRecording();
            Recorder_Services.Console_Log("��ʼ¼��");
        }

#endif

        Unload_Character_Bundles_False();

        Console_Log($"�������ؽ�ɫ: {character_name}");
    }

    public void Load_Character_Base_Info()
    {
        // ===== ��ȡѧ���ļ������������Ϣ =====
        memory_lobby_info = File_Services.Load_Specific_Type_From_File<MemoryLobby_Info>(Path.Combine(Character_Folder_Path, "MemoryLobby_Info.json"));

        // ===== ������Ļ��Ϣ��ȡ�Ի����ܱ�Ҫ��Ϣ =====
        if (memory_lobby_info.Subtitles.Count != 0)
        {
            // ===== �ж��Ƿ��п������� =====
            Match match = Regex.Match(memory_lobby_info.Audio_Files.First(), @"memoriallobby_(\d+)");
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out int first_audio_file_index))
                {
                    Console_Log($"��Ƶ�ļ����� {first_audio_file_index} ��ʼ����");
                    if (first_audio_file_index == 0)
                    {
                        Console_Log($"��ɫ {Character_Name} �п�������");
                        has_Start_Idle_Audio = true;
                    }
                }
            }
            else
            {
                Console_Log($"���������ļ�������ƥ��");
            }



            // ===== ��ȡ�ܶԻ������� =====
            string[] talk_animation_end_index_parts = memory_lobby_info.Subtitles.Last().AnimationName.Split("_");
            Talk_Animaiton_Num = int.Parse(talk_animation_end_index_parts[1].Replace("0", ""));
            Index_Services.Instance.Talk_Animaiton_Num = Talk_Animaiton_Num;
            
            

            // ===== �ж��Ƿ�����ʾ��Ļ =====
            is_Can_Show_Subtitle = memory_lobby_info.Audio_Files.Count == memory_lobby_info.Subtitles.Count ? true : false;

            if (Subtitle_Services.Instance != null)
            {
                if (Subtitle_Services.Instance.Subtitle_JP_Toggle_Button != null)
                {
                    Subtitle_Services.Instance.Subtitle_JP_Toggle_Button.enabled = is_Can_Show_Subtitle;
                    Image jpButtonImage = Subtitle_Services.Instance.Subtitle_JP_Toggle_Button.gameObject.GetComponent<Image>();
                    if (jpButtonImage != null)
                    {
                        jpButtonImage.color = is_Can_Show_Subtitle ? new Color(1, 1, 1, 1) : new Color(0.7f, 0.7f, 0.7f, 1);
                    }
                }

                if (Subtitle_Services.Instance.Subtitle_Custom_Toggle_Button != null)
                {
                    Subtitle_Services.Instance.Subtitle_Custom_Toggle_Button.enabled = is_Can_Show_Subtitle;
                    Image customButtonImage = Subtitle_Services.Instance.Subtitle_Custom_Toggle_Button.gameObject.GetComponent<Image>();
                    if (customButtonImage != null)
                    {
                        customButtonImage.color = is_Can_Show_Subtitle ? new Color(1, 1, 1, 1) : new Color(0.7f, 0.7f, 0.7f, 1);
                    }
                }
            }
        }
        else
        {
            Talk_Animaiton_Num = 0;
            Index_Services.Instance.Talk_Animaiton_Num = 0;
        }


        foreach (BGMExcel_DB bgm_excel_db in Audio_Services.Instance.BGMExcel_DB_list)
        {
            if (bgm_excel_db.Id == memory_lobby_info.BGMId)
            {
                BGM_Path = bgm_excel_db.Path;
                Console_Log($"��������: {BGM_Path}");
            }
        }
        StartCoroutine(Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.BGM, Path.Combine(File_Services.MX_Files_MediaResources_Folder_Path, $"{BGM_Path}.ogg")));

        Console_Log($"̨����Ƶ��: {memory_lobby_info.Audio_Files.Count} ��������: {has_Start_Idle_Audio} ̨���ı���: {memory_lobby_info.Subtitles.Count} Talk������: {Talk_Animaiton_Num}");
    }

    private AssetBundle Core_Bundle;
    private List<string> dependencies_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Dependencies_Bundles = new Dictionary<string, AssetBundle>();
    private List<string> character_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Character_Bundles = new Dictionary<string, AssetBundle>();
    public void Load_Character_Bundles()
    {
        dependencies_bundles_paths = File_Services.Load_Specific_Type_From_File<List<string>>(Path.Combine(File_Services.Student_Files_Folder_Path, Character_Name, "Bundles", "Dependencies.json"));

        // ===== ���غ���AB�� ===== \\
        Core_Bundle = AssetBundle.LoadFromFile(File_Services.Root_Folder_Path + dependencies_bundles_paths.Last());
        if (Core_Bundle != null)
        {
            Console_Log($"�����˺���AB�� {Path.GetFileName(dependencies_bundles_paths.Last())}", Debug_Services.LogLevel.Core);
            dependencies_bundles_paths.RemoveAt(dependencies_bundles_paths.Count - 1);
        }
        else
        {
            Console_Log($"AB�� {Path.GetFileName(dependencies_bundles_paths.Last())} �����쳣", Debug_Services.LogLevel.Debug, LogType.Warning);
            return;
        }

        // ===== ��������AB�� ===== \\
        foreach (string dependencies_bundles_path in dependencies_bundles_paths)
        {
            string file_name = Path.GetFileName(dependencies_bundles_path);
            if (file_name == "assets-_mx-effectsfreezed-texture-_mxdependency-2021-04-01_tga_assets_all_1563291350.bundle") continue;

            AssetBundle asset_bundle = AssetBundle.LoadFromFile(File_Services.Root_Folder_Path + dependencies_bundles_path);
            if (asset_bundle != null)
            {
                Dependencies_Bundles.Add(file_name, asset_bundle);
            }
            else
            {
                Console_Log($"����AB�� {file_name} �����쳣", Debug_Services.LogLevel.Debug, LogType.Error);
            }
        }
        Console_Log($"������ {Dependencies_Bundles.Count} ������AB��");

        // ===== ���ؽ�ɫAB�� ===== \\
        foreach (string file_path in Directory.GetFiles(Path.Combine(File_Services.Student_Files_Folder_Path, Character_Name, "Bundles"), "*.bundle", SearchOption.AllDirectories))
        {
            string file_name = Path.GetFileName(file_path); 
            AssetBundle asset_bundle = AssetBundle.LoadFromFile(file_path);
            if (asset_bundle != null)
            {
                Character_Bundles.Add(file_name, asset_bundle);
            }
            else
            {
                Console_Log($"��ɫAB�� {file_name} �����쳣", Debug_Services.LogLevel.Debug, LogType.Warning);
            }
        }
        Console_Log($"������ {Character_Bundles.Count} ����ɫAB��");

        foreach (AssetBundle character_bundle in Character_Bundles.Values)
        {
            SpineClip[] spine_clip_array = character_bundle.LoadAllAssets<SpineClip>();
            if (spine_clip_array.Length != 0)
            {
                Console_Log($"��ʼ��ʼ�� {spine_clip_array.Length} �� SpineClip", Debug_Services.LogLevel.Core);
                foreach (SpineClip spine_clip in spine_clip_array)
                {
                    spine_clip.Initialize();
                    Console_Log($"��ʼ���� {spine_clip.ClipName}", Debug_Services.LogLevel.Core);
                }
                Console_Log($"������ʼ�� {spine_clip_array.Length} �� SpineClip", Debug_Services.LogLevel.Core);
            }

            VolumeProfile[] volume_profile_array = character_bundle.LoadAllAssets<VolumeProfile>();
            if (volume_profile_array.Length != 0)
            {
                Console_Log($"��ʼ��ʼ�� {volume_profile_array.Length} �� Volume Profile �еĵ�һ��", Debug_Services.LogLevel.Core);
                volume_component = gameObject.AddComponent<Volume>();
                Volume_Services.Instance.Volume_Component = volume_component;
                volume_component.sharedProfile = volume_profile_array[0];
                Console_Log($"��һ�� Volume Profile Ϊ: {volume_component.sharedProfile.name}", Debug_Services.LogLevel.Core);
                PaniniProjection panini_projection;
                if (volume_component.profile.TryGet(out panini_projection))
                {
                    panini_projection.active = false;
                    Console_Log($"�ѽ��� PaniniProjection ��Ч");
                }
                Console_Log($"������ʼ�� {volume_profile_array.Length} �� Volume Profile �еĵ�һ��", Debug_Services.LogLevel.Core);
            }
        }
    }
    public void Instantiate_Character_GameObject()
    {
        Console_Log("��ʼʵ������ɫ Prefab");

        lobby_gameobject_prefab = (GameObject)Core_Bundle.LoadAsset($"Assets/_MX/AddressableAsset/UI/UILobbyElement/{memory_lobby_info.PrefabName}.prefab");
        if (lobby_gameobject_prefab != null)
        {
            lobby_gameobject_instantiated = Instantiate(lobby_gameobject_prefab, transform.position, Quaternion.identity);
            Console_Log("�ɹ�ʵ������ɫ Prefab");
        }
        else
        {
            Console_Log("��ɫ Prefab Ϊ��", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("����ʵ������ɫ Prefab");
    }
    
    public void Init_SkeletonAnimation()
    {
        Console_Log("��ʼ��ʼ�� SkeletonAnimation ���");

        skeleton_animation = lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().SpineCharacter.SkeletonAnimation;
        if (skeleton_animation != null)
        {
            Console_Log($"�� {skeleton_animation.gameObject.name} ���ҵ��� SkeletonAnimation ���", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"��ȡ���� SkeletonAnimation ���Ϊ��", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        skeleton_animation.AnimationState.Event += OnSpineEvent;

        Console_Log("������ʼ�� SkeletonAnimation ���");
    }

    public void Init_PlayableDirector()
    {
        Console_Log("��ʼ��ʼ�� PlayerDirector ���");

        Console_Log($"���� {lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().Length} �� PlayableDirector ���");
        player_director = lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().First();
        if (player_director != null)
        {
            Console_Log($"�� {player_director.gameObject.name} ���ҵ��� PlayableDirector ���", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"û���ҵ� PlayableDirector ���", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        
        Console_Log("������ʼ�� PlayerDirector ���");
    }

    public void Init_TimelineAsset()
    {
        Console_Log("��ʼ��ʼ�� TimelineAsset ��Դ");

        if (player_director != null)
        {
            timeline_asset = player_director.playableAsset as TimelineAsset;
            if (timeline_asset != null)
            {
                Console_Log("��ȡ���� PlayerDirector ����� playableAsset");
            }
            else
            {
                Console_Log("��ȡ���� PlayerDirector ����� playableAsset Ϊ��", Debug_Services.LogLevel.Debug, LogType.Error);
                return;
            }

            Console_Log("��ʼ�ڹ��1�ϴ��� Talk_M �������", Debug_Services.LogLevel.Core);
            talk_m_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_M");
            talk_m_track.trackIndex = 1;
            player_director.SetGenericBinding(talk_m_track, skeleton_animation);
            Console_Log("�����ڹ��1�ϴ��� Talk_M �������", Debug_Services.LogLevel.Core);

            Console_Log("��ʼ�ڹ��2�ϴ��� Talk_A �������", Debug_Services.LogLevel.Core);
            talk_a_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_A");
            talk_a_track.trackIndex = 2;
            player_director.SetGenericBinding(talk_a_track, skeleton_animation);
            Console_Log("��ʼ�ڹ��2�ϴ��� Talk_A �������", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"PlayableDirector ���Ϊ��", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("������ʼ�� TimelineAsset ��Դ");
    }

    public void Disable_ChatDialog_GameObject()
    {
        lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().ChatDialog.gameObject.SetActive(false);
        Console_Log("�ѽ��� ChatDialog ����");
    }

    private int Event_Index = 0;
    private int Audio_File_Index = 0;
    public void OnSpineEvent(TrackEntry track_entry, Spine.Event spine_event)
    {
        Console_Log($"[{Event_Index++}] �¼�����: {spine_event.Data.Name} ��������: {track_entry.Animation.Name} ���: {track_entry.TrackIndex}");

        if (spine_event.Data.Name.ToLower() == "talk")
        {
            string audio_file_path = Path.Combine(File_Services.Student_Files_Folder_Path, Character_Name, "Audios", memory_lobby_info.Audio_Files[Audio_File_Index]);

            if (is_Can_Show_Subtitle)
            {
                Subtitle_Services.Subtitle_Request subtitle_request = new Subtitle_Services.Subtitle_Request()
                {
                    Text_JP = memory_lobby_info.Subtitles[Audio_File_Index].LocalizeJP,
                    Text_Custom = memory_lobby_info.Subtitles[Audio_File_Index].LocalizeCustom,
                    Text_Duration = memory_lobby_info.Subtitles[Audio_File_Index].Duration / 1000
                };
                Subtitle_Services.Instance.Show_Subtitle(subtitle_request);
            }

            StartCoroutine(Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.Talk, audio_file_path));

            Audio_File_Index++;

            if (Audio_File_Index == memory_lobby_info.Audio_Files.Count)
            {
                Audio_File_Index = 0;
                Event_Index = 1;
            }
        }
    }

    public void Unload_Character()
    {
        Console_Log($"��ʼж�ؽ�ɫ: {Character_Name}");

        Index_Services.Instance.is_Idle_Mode = false;
        Index_Services.Instance.is_Talking = false;
        Subtitle_Services.Instance.is_Stopping_Display = true;
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.Talk_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.SFX_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.BGM_GameObject);
        Destroy(GameObject.Find("UI Root"));
        Destroy(GetComponent<Volume>());
        Destroy(GetComponent<Character>());
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Console_Log($"����ж�ؽ�ɫ: {Character_Name}");
        Console_Log($"�� ======================================== ��");
    }

    public void Unload_Character_Bundles_True()
    {
        if (Core_Bundle != null)
        {
            Core_Bundle.Unload(true);
        }
        foreach (AssetBundle asset_bundle in Dependencies_Bundles.Values)
        {
            if (asset_bundle != null)
            {
                asset_bundle.Unload(true);
            }
        }
        foreach (AssetBundle asset_bundle in Character_Bundles.Values)
        {
            if (asset_bundle != null)
            {
                asset_bundle.Unload(true);
            }
        }
    }

    public void Unload_Character_Bundles_False()
    {
        Core_Bundle.Unload(false);
        foreach (AssetBundle asset_bundle in Dependencies_Bundles.Values) asset_bundle.Unload(false);
        foreach (AssetBundle asset_bundle in Character_Bundles.Values) asset_bundle.Unload(false);
        Resources.UnloadUnusedAssets();
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
