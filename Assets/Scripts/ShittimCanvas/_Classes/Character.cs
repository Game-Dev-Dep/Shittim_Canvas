
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
            Recorder_Services.Console_Log($"已调整摄像机缩放: {Recorder_Services.Instance.Recorder_Camera_Size}");
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
                Recorder_Services.Console_Log("已开始自动播放剧情");
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
        Console_Log($"· ======================================== ·");
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

        Console_Log($"结束加载角色: {character_name}");
    }

    public void Load_Character_Base_Info()
    {
        // ===== 获取学生的记忆大厅基本信息 =====
        memory_lobby_info = File_Services.Load_Specific_Type_From_File<MemoryLobby_Info>(Path.Combine(Character_Folder_Path, "MemoryLobby_Info.json"));

        // ===== 依照字幕信息获取对话功能必要信息 =====
        if (memory_lobby_info.Subtitles.Count != 0)
        {
            // ===== 判断是否有开场语音 =====
            Match match = Regex.Match(memory_lobby_info.Audio_Files.First(), @"memoriallobby_(\d+)");
            if (match.Success && match.Groups.Count > 1)
            {
                if (int.TryParse(match.Groups[1].Value, out int first_audio_file_index))
                {
                    Console_Log($"音频文件名从 {first_audio_file_index} 开始索引");
                    if (first_audio_file_index == 0)
                    {
                        Console_Log($"角色 {Character_Name} 有开场语音");
                        has_Start_Idle_Audio = true;
                    }
                }
            }
            else
            {
                Console_Log($"开场语音文件正则无匹配");
            }



            // ===== 获取总对话动画数 =====
            string[] talk_animation_end_index_parts = memory_lobby_info.Subtitles.Last().AnimationName.Split("_");
            Talk_Animaiton_Num = int.Parse(talk_animation_end_index_parts[1].Replace("0", ""));
            Index_Services.Instance.Talk_Animaiton_Num = Talk_Animaiton_Num;
            
            

            // ===== 判断是否能显示字幕 =====
            is_Can_Show_Subtitle = memory_lobby_info.Audio_Files.Count == memory_lobby_info.Subtitles.Count ? true : false;

            Subtitle_Services.Instance.Subtitle_JP_Toggle_Button.enabled = is_Can_Show_Subtitle;
            Subtitle_Services.Instance.Subtitle_JP_Toggle_Button.gameObject.GetComponent<Image>().color = is_Can_Show_Subtitle ? new Color(1, 1, 1, 1) : new Color(0.7f, 0.7f, 0.7f, 1);

            Subtitle_Services.Instance.Subtitle_Custom_Toggle_Button.enabled = is_Can_Show_Subtitle;
            Subtitle_Services.Instance.Subtitle_Custom_Toggle_Button.gameObject.GetComponent<Image>().color = is_Can_Show_Subtitle ? new Color(1, 1, 1, 1) : new Color(0.7f, 0.7f, 0.7f, 1);
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
                Console_Log($"背景音乐: {BGM_Path}");
            }
        }
        StartCoroutine(Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.BGM, Path.Combine(File_Services.MX_Files_MediaResources_Folder_Path, $"{BGM_Path}.ogg")));

        Console_Log($"台词音频数: {memory_lobby_info.Audio_Files.Count} 开场语音: {has_Start_Idle_Audio} 台词文本数: {memory_lobby_info.Subtitles.Count} Talk动画数: {Talk_Animaiton_Num}");
    }

    private AssetBundle Core_Bundle;
    private List<string> dependencies_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Dependencies_Bundles = new Dictionary<string, AssetBundle>();
    private List<string> character_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Character_Bundles = new Dictionary<string, AssetBundle>();
    public void Load_Character_Bundles()
    {
        dependencies_bundles_paths = File_Services.Load_Specific_Type_From_File<List<string>>(Path.Combine(File_Services.Student_Files_Folder_Path, Character_Name, "Bundles", "Dependencies.json"));

        // ===== 加载核心AB包 ===== \\
        Core_Bundle = AssetBundle.LoadFromFile(File_Services.Root_Folder_Path + dependencies_bundles_paths.Last());
        if (Core_Bundle != null)
        {
            Console_Log($"加载了核心AB包 {Path.GetFileName(dependencies_bundles_paths.Last())}", Debug_Services.LogLevel.Core);
            dependencies_bundles_paths.RemoveAt(dependencies_bundles_paths.Count - 1);
        }
        else
        {
            Console_Log($"AB包 {Path.GetFileName(dependencies_bundles_paths.Last())} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
            return;
        }

        // ===== 加载依赖AB包 ===== \\
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
                Console_Log($"依赖AB包 {file_name} 加载异常", Debug_Services.LogLevel.Debug, LogType.Error);
            }
        }
        Console_Log($"加载了 {Dependencies_Bundles.Count} 个依赖AB包");

        // ===== 加载角色AB包 ===== \\
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
                Console_Log($"角色AB包 {file_name} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
            }
        }
        Console_Log($"加载了 {Character_Bundles.Count} 个角色AB包");

        foreach (AssetBundle character_bundle in Character_Bundles.Values)
        {
            SpineClip[] spine_clip_array = character_bundle.LoadAllAssets<SpineClip>();
            if (spine_clip_array.Length != 0)
            {
                Console_Log($"开始初始化 {spine_clip_array.Length} 个 SpineClip", Debug_Services.LogLevel.Core);
                foreach (SpineClip spine_clip in spine_clip_array)
                {
                    spine_clip.Initialize();
                    Console_Log($"初始化了 {spine_clip.ClipName}", Debug_Services.LogLevel.Core);
                }
                Console_Log($"结束初始化 {spine_clip_array.Length} 个 SpineClip", Debug_Services.LogLevel.Core);
            }

            VolumeProfile[] volume_profile_array = character_bundle.LoadAllAssets<VolumeProfile>();
            if (volume_profile_array.Length != 0)
            {
                Console_Log($"开始初始化 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
                volume_component = gameObject.AddComponent<Volume>();
                Volume_Services.Instance.Volume_Component = volume_component;
                volume_component.sharedProfile = volume_profile_array[0];
                Console_Log($"第一个 Volume Profile 为: {volume_component.sharedProfile.name}", Debug_Services.LogLevel.Core);
                PaniniProjection panini_projection;
                if (volume_component.profile.TryGet(out panini_projection))
                {
                    panini_projection.active = false;
                    Console_Log($"已禁用 PaniniProjection 特效");
                }
                Console_Log($"结束初始化 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
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

        Console_Log("结束实例化角色 Prefab");
    }
    
    public void Init_SkeletonAnimation()
    {
        Console_Log("开始初始化 SkeletonAnimation 组件");

        skeleton_animation = lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().SpineCharacter.SkeletonAnimation;
        if (skeleton_animation != null)
        {
            Console_Log($"在 {skeleton_animation.gameObject.name} 上找到了 SkeletonAnimation 组件", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"获取到的 SkeletonAnimation 组件为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        skeleton_animation.AnimationState.Event += OnSpineEvent;

        Console_Log("结束初始化 SkeletonAnimation 组件");
    }

    public void Init_PlayableDirector()
    {
        Console_Log("开始初始化 PlayerDirector 组件");

        Console_Log($"存在 {lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().Length} 个 PlayableDirector 组件");
        player_director = lobby_gameobject_instantiated.GetComponentsInChildren<PlayableDirector>().First();
        if (player_director != null)
        {
            Console_Log($"在 {player_director.gameObject.name} 上找到了 PlayableDirector 组件", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"没有找到 PlayableDirector 组件", Debug_Services.LogLevel.Debug, LogType.Error);
        }
        
        Console_Log("结束初始化 PlayerDirector 组件");
    }

    public void Init_TimelineAsset()
    {
        Console_Log("开始初始化 TimelineAsset 资源");

        if (player_director != null)
        {
            timeline_asset = player_director.playableAsset as TimelineAsset;
            if (timeline_asset != null)
            {
                Console_Log("获取到了 PlayerDirector 组件的 playableAsset");
            }
            else
            {
                Console_Log("获取到的 PlayerDirector 组件的 playableAsset 为空", Debug_Services.LogLevel.Debug, LogType.Error);
                return;
            }

            Console_Log("开始在轨道1上创建 Talk_M 动画轨道", Debug_Services.LogLevel.Core);
            talk_m_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_M");
            talk_m_track.trackIndex = 1;
            player_director.SetGenericBinding(talk_m_track, skeleton_animation);
            Console_Log("结束在轨道1上创建 Talk_M 动画轨道", Debug_Services.LogLevel.Core);

            Console_Log("开始在轨道2上创建 Talk_A 动画轨道", Debug_Services.LogLevel.Core);
            talk_a_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_A");
            talk_a_track.trackIndex = 2;
            player_director.SetGenericBinding(talk_a_track, skeleton_animation);
            Console_Log("开始在轨道2上创建 Talk_A 动画轨道", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"PlayableDirector 组件为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("结束初始化 TimelineAsset 资源");
    }

    public void Disable_ChatDialog_GameObject()
    {
        lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().ChatDialog.gameObject.SetActive(false);
        Console_Log("已禁用 ChatDialog 物体");
    }

    private int Event_Index = 0;
    private int Audio_File_Index = 0;
    public void OnSpineEvent(TrackEntry track_entry, Spine.Event spine_event)
    {
        Console_Log($"[{Event_Index++}] 事件名称: {spine_event.Data.Name} 动画名称: {track_entry.Animation.Name} 轨道: {track_entry.TrackIndex}");

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
        Subtitle_Services.Instance.is_Stopping_Display = true;
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.Talk_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.SFX_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.BGM_GameObject);
        Destroy(GameObject.Find("UI Root"));
        Destroy(GetComponent<Volume>());
        Destroy(GetComponent<Character>());
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Console_Log($"结束卸载角色: {Character_Name}");
        Console_Log($"· ======================================== ·");
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
