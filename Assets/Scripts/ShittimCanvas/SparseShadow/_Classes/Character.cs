using System.IO;
using System.Linq;
using System.Collections;
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
            Recorder_Services.Console_Log($"相机已固定: {Recorder_Services.Instance.Recorder_Camera_Size}");
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
                Recorder_Services.Console_Log("开始录制故事模式");
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
        Console_Log($" ======================================== ");
        Console_Log($"开始加载角色: {character_name}");
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
            Recorder_Services.Console_Log("开始录制");
        }

#endif

        Unload_Character_Bundles_False();

        Console_Log($"加载角色: {character_name}");
    }

    public void Load_Character_Base_Info()
    {
        // ===== 获取学生文件夹中的所有信息 =====
        memory_lobby_info = File_Services.Load_Specific_Type_From_File<MemoryLobby_Info>(Path.Combine(Character_Folder_Path, "MemoryLobby_Info.json"));

        // ===== 获取屏幕信息以获取要使用的信息 =====
        if (memory_lobby_info.Subtitles.Count != 0)
        {
            // ===== 判断是否有必要显示屏幕 =====
            Match match = Regex.Match(memory_lobby_info.Audio_Files.First(), @"memoriallobby_(\d+)");
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out int first_audio_file_index))
                {
                    Console_Log($"音频文件从 {first_audio_file_index} 开始");
                    if (first_audio_file_index == 0)
                    {
                        Console_Log($"角色 {Character_Name} 有必要的音频文件");
                        has_Start_Idle_Audio = true;
                    }
                }
            }
            else
            {
                Console_Log($"音频文件没有匹配");
            }



            // ===== 获取所有音频文件 =====
            string[] talk_animation_end_index_parts = memory_lobby_info.Subtitles.Last().AnimationName.Split("_");
            Talk_Animaiton_Num = int.Parse(talk_animation_end_index_parts[1].Replace("0", ""));
            Index_Services.Instance.Talk_Animaiton_Num = Talk_Animaiton_Num;
            
            

            // ===== 判断是否显示屏幕 =====
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
                Console_Log($"播放音乐: {BGM_Path}");
            }
        }
        StartCoroutine(Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.BGM, Path.Combine(File_Services.MX_Files_MediaResources_Folder_Path, $"{BGM_Path}.ogg")));

        Console_Log($"音频文件数量: {memory_lobby_info.Audio_Files.Count} 有必要的音频文件: {has_Start_Idle_Audio} 角色音频文件: {memory_lobby_info.Subtitles.Count} Talk动画: {Talk_Animaiton_Num}");
    }

    private AssetBundle Core_Bundle;
    private List<string> dependencies_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Dependencies_Bundles = new Dictionary<string, AssetBundle>();
    private List<string> character_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Character_Bundles = new Dictionary<string, AssetBundle>();
    public void Load_Character_Bundles()
    {
        dependencies_bundles_paths = File_Services.Load_Specific_Type_From_File<List<string>>(Path.Combine(File_Services.Student_Files_Folder_Path, Character_Name, "Bundles", "Dependencies.json"));

        // ===== 加载核心AB ===== \\
        Core_Bundle = AssetBundle.LoadFromFile(File_Services.Root_Folder_Path + dependencies_bundles_paths.Last());
        if (Core_Bundle != null)
        {
            Console_Log($"加载核心AB {Path.GetFileName(dependencies_bundles_paths.Last())}", Debug_Services.LogLevel.Core);
            dependencies_bundles_paths.RemoveAt(dependencies_bundles_paths.Count - 1);
        }
        else
        {
            Console_Log($"AB {Path.GetFileName(dependencies_bundles_paths.Last())} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
            return;
        }

        // ===== 加载所有AB ===== \\
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
                Console_Log($"加载AB {file_name} 加载异常", Debug_Services.LogLevel.Debug, LogType.Error);
            }
        }
        Console_Log($"加载了 {Dependencies_Bundles.Count} 个依赖AB");

        // ===== 加载角色AB ===== \\
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
                Console_Log($"角色AB {file_name} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
            }
        }
        Console_Log($"加载了 {Character_Bundles.Count} 个角色AB");

        foreach (AssetBundle character_bundle in Character_Bundles.Values)
        {
            SpineClip[] spine_clip_array = character_bundle.LoadAllAssets<SpineClip>();
            if (spine_clip_array.Length != 0)
            {
                Console_Log($"开始加载 {spine_clip_array.Length} 个 SpineClip", Debug_Services.LogLevel.Core);
                foreach (SpineClip spine_clip in spine_clip_array)
                {
                    spine_clip.Initialize();
                    Console_Log($"开始加载 {spine_clip.ClipName}", Debug_Services.LogLevel.Core);
                }
                Console_Log($"加载了 {spine_clip_array.Length} 个 SpineClip", Debug_Services.LogLevel.Core);
            }

            VolumeProfile[] volume_profile_array = character_bundle.LoadAllAssets<VolumeProfile>();
            if (volume_profile_array.Length != 0)
            {
                Console_Log($"开始加载 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
                volume_component = gameObject.AddComponent<Volume>();
                if (Volume_Services.Instance != null)
                {
                    Volume_Services.Instance.Volume_Component = volume_component;
                }
                volume_component.sharedProfile = volume_profile_array[0];
                Console_Log($"第一个 Volume Profile 为: {volume_component.sharedProfile.name}", Debug_Services.LogLevel.Core);
                PaniniProjection panini_projection;
                if (volume_component.profile.TryGet(out panini_projection))
                {
                    panini_projection.active = false;
                    Console_Log($"取消 PaniniProjection 效果");
                }
                Console_Log($"加载了 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
            }
        }
    }
    public void Instantiate_Character_GameObject()
    {
        Console_Log("开始实例化角色 Prefab");

        lobby_gameobject_prefab = (GameObject)Core_Bundle.LoadAsset($"Assets/_MX/AddressableAsset/UI/UILobbyElement/{memory_lobby_info.PrefabName}.prefab");
        if (lobby_gameobject_prefab != null)
        {
            lobby_gameobject_instantiated = Instantiate(lobby_gameobject_prefab, transform.position, Quaternion.identity);
            Console_Log("成功实例化角色 Prefab");
        }
        else
        {
            Console_Log("角色 Prefab 为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("完成实例化角色 Prefab");
    }
    
    public void Init_SkeletonAnimation()
    {
        Console_Log("开始初始化 SkeletonAnimation");

        skeleton_animation = lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().SpineCharacter.SkeletonAnimation;
        if (skeleton_animation != null)
        {
            Console_Log($"找到 {skeleton_animation.gameObject.name} 的 SkeletonAnimation", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"获取 SkeletonAnimation 失败为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        skeleton_animation.AnimationState.Event += OnSpineEvent;

        Console_Log("完成初始化 SkeletonAnimation");
    }

    public void Init_PlayableDirector()
    {
        Console_Log("开始初始化 PlayerDirector");

        Console_Log($"找到 {lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().Length} 个 PlayableDirector");
        player_director = lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().First();
        if (player_director != null)
        {
            Console_Log($"找到 {player_director.gameObject.name} 的 PlayableDirector", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"没有找到 PlayableDirector", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        
        Console_Log("完成初始化 PlayerDirector");
    }

    public void Init_TimelineAsset()
    {
        Console_Log("开始初始化 TimelineAsset 资源");

        if (player_director != null)
        {
            timeline_asset = player_director.playableAsset as TimelineAsset;
            if (timeline_asset != null)
            {
                Console_Log("获取到 player_director 的 playableAsset");
            }
            else
            {
                Console_Log("获取到 player_director 的 playableAsset 失败", Debug_Services.LogLevel.Debug, LogType.Error);
                return;
            }

            Console_Log("开始在轨道1上添加 Talk_M 动画", Debug_Services.LogLevel.Core);
            talk_m_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_M");
            talk_m_track.trackIndex = 1;
            player_director.SetGenericBinding(talk_m_track, skeleton_animation);
            Console_Log("完成在轨道1上添加 Talk_M 动画", Debug_Services.LogLevel.Core);

            Console_Log("开始在轨道2上添加 Talk_A 动画", Debug_Services.LogLevel.Core);
            talk_a_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_A");
            talk_a_track.trackIndex = 2;
            player_director.SetGenericBinding(talk_a_track, skeleton_animation);
            Console_Log("完成在轨道2上添加 Talk_A 动画", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"PlayableDirector 失败为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("完成初始化 TimelineAsset 资源");
    }

    public void Disable_ChatDialog_GameObject()
    {
        lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().ChatDialog.gameObject.SetActive(false);
        Console_Log("取消 ChatDialog");
    }

    private int Event_Index = 0;
    private int Audio_File_Index = 0;
    public void OnSpineEvent(TrackEntry track_entry, Spine.Event spine_event)
    {
        Console_Log($"[{Event_Index++}] 事件: {spine_event.Data.Name} 动画: {track_entry.Animation.Name} 轨道: {track_entry.TrackIndex}");

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
        Console_Log($"开始卸载角色: {Character_Name}");

        Index_Services.Instance.is_Idle_Mode = false;
        Index_Services.Instance.is_Talking = false;
        
        //只有在字幕服务存在且正在显示时才停止字幕
        if (Subtitle_Services.Instance != null)
        {
            //等一下来确保当前字幕能够正常显示
            StartCoroutine(DelayedStopSubtitle());
        }
        
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.Talk_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.SFX_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.BGM_GameObject);
        Destroy(GameObject.Find("UI Root"));
        Destroy(GetComponent<Volume>());
        Destroy(GetComponent<Character>());
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Console_Log($"卸载角色: {Character_Name}");
        Console_Log($" ======================================== ");
    }

    private IEnumerator DelayedStopSubtitle()
    {
        //等好帧！
        yield return null;
        Subtitle_Services.Instance.is_Stopping_Display = true;
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
