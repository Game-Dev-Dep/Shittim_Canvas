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
    public SpineAnimationStateTrack original_track;
    public SpineAnimationStateTrack talk_m_track;
    public SpineAnimationStateTrack talk_a_track;

#if UNITY_EDITOR
    public bool is_StoryMode_Started = false;
    public bool is_StoryMode_Camera_Fixed = false;
    public bool is_Screenshot_Took = false;
#endif

    public GameObject EyeIK_GameObject;

    public bool is_Idle_Sync_SpineClip_Inited = false;
    public SpineClip Idle_SpineClip;
    public List<SpineAnimationStateTrack> Idle_Sync_SpineClip_Track_List = new List<SpineAnimationStateTrack>();



    /// <summary>
    /// 更新方法
    /// </summary>
    private void Update()
    {

#if UNITY_EDITOR

        // ===== 编辑模式下，如果正在录制，则调整摄像机位置和缩放 ===== \\
        if (Recorder_Services.Instance.is_Record && Camera_Services.Instance.MemoryLobby_Camera != null && !is_StoryMode_Camera_Fixed)
        {
            Camera_Services.Instance.MemoryLobby_Camera.transform.position = new Vector3(0f, 0f, 0f);
            Camera_Services.Instance.MemoryLobby_Camera.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            Camera_Services.Instance.MemoryLobby_Camera.orthographicSize = Recorder_Services.Instance.Recorder_Camera_Size;
            is_StoryMode_Camera_Fixed = true;
            Recorder_Services.Console_Log($"已调整摄像机缩放: {Recorder_Services.Instance.Recorder_Camera_Size}");
        }

#endif

        // ===== 根据摄像机缩放调整眼动追踪的触发范围 EyeIK ===== \\
        if (EyeIK_GameObject != null)
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

        // ===== 处理 Idle_01 动画的同步播放动画 ===== \\
        if (!is_Idle_Sync_SpineClip_Inited)
        {
            for (int i = 0; i < Idle_SpineClip.SyncPlayClipObjects.Count(); i++)
            {
                if (Idle_SpineClip.SyncPlayClipObjects[i] != null)
                {
                    Idle_Sync_SpineClip_Track_List.Add(timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, $"Idle_Sync_Clip_0{i}"));
                    Idle_Sync_SpineClip_Track_List[i].trackIndex = i + 1;
                    player_director.SetGenericBinding(Idle_Sync_SpineClip_Track_List[i], skeleton_animation);
                    skeleton_animation.AnimationState.SetAnimation(Idle_Sync_SpineClip_Track_List[i].trackIndex, ((SpineClip)Idle_SpineClip.SyncPlayClipObjects[i]).ClipName, ((SpineClip)Idle_SpineClip.SyncPlayClipObjects[i]).Loop);
                    Console_Log($"轨道 {Idle_Sync_SpineClip_Track_List[i].trackIndex} 上播放同步动画 {((SpineClip)Idle_SpineClip.SyncPlayClipObjects[i]).ClipName}", Debug_Services.LogLevel.Core);
                }
            }
            is_Idle_Sync_SpineClip_Inited = true;
        }

        // ===== 判断是否处于播放待机动画状态 ===== \\
        if (!is_Character_Idle_Mode)
        {
            if (skeleton_animation.AnimationState.GetCurrent(0)?.Animation.Name == "Idle_01") is_Character_Idle_Mode = true;
            else is_Character_Idle_Mode = false; 
            Index_Services.Instance.is_Idle_Mode = is_Character_Idle_Mode;
        }
        else
        {

#if UNITY_EDITOR

            // ===== 编辑模式下，录制故事模式视频 ===== \\
            if (Recorder_Services.Instance.is_Record && !is_StoryMode_Started)
            {
                StartCoroutine(Recorder_Services.Instance.Play_Talk_Clips_StoryMode(skeleton_animation));
                is_StoryMode_Started = true;
                Recorder_Services.Console_Log("开始录制故事模式");
            }

            // ===== 编辑模式下，获取截图 ===== \\
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



    /// <summary>
    /// 加载角色的基本信息和资源。
    /// </summary>
    /// <param name="character_name">角色名</param>
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



    /// <summary>
    /// 加载角色的基本信息，包括记忆大厅信息、音频信息、字幕信息等。
    /// </summary>
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

            // ===== 更新能否显示字幕的相关UI =====
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

        // ===== 获取背景音乐信息 =====
        float loop_start_time = 0f;
        float loop_end_time = 0f;
        foreach (BGMExcel_DB bgm_excel_db in Audio_Services.Instance.BGMExcel_DB_list)
        {
            if (bgm_excel_db.Id == memory_lobby_info.BGMId)
            {
                BGM_Path = bgm_excel_db.Path;
                loop_start_time = bgm_excel_db.LoopStartTime;
                loop_end_time = bgm_excel_db.LoopEndTime;
                Console_Log($"背景音乐: {BGM_Path} 循环起始: {bgm_excel_db.LoopStartTime} 循环结束: {bgm_excel_db.LoopEndTime}");
            }
        }
        StartCoroutine(Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.BGM, Path.Combine(File_Services.MX_Files_MediaResources_Folder_Path, $"{BGM_Path}.ogg"), null, true, loop_start_time, loop_end_time)); // 播放背景音乐

        Console_Log($"台词音频数: {memory_lobby_info.Audio_Files.Count} 开场语音: {has_Start_Idle_Audio} 台词文本数: {memory_lobby_info.Subtitles.Count} Talk动画数: {Talk_Animaiton_Num}");
    }



    private AssetBundle Core_Bundle;
    private List<string> dependencies_bundles_paths = new List<string>();
    private Dictionary<string, AssetBundle> Dependencies_Bundles = new Dictionary<string, AssetBundle>();
    private Dictionary<string, AssetBundle> Character_Bundles = new Dictionary<string, AssetBundle>();
    /// <summary>
    /// 加载角色的 AssetBundle 包，包括核心包、依赖包和角色包。
    /// </summary>
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
            Console_Log($"核心AB包 {Path.GetFileName(dependencies_bundles_paths.Last())} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
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
                Console_Log($"角色AB {file_name} 加载异常", Debug_Services.LogLevel.Debug, LogType.Warning);
            }
        }
        Console_Log($"加载了 {Character_Bundles.Count} 个角色AB包");



        // ===== 初始化 SpineClip 和 VolumeProfile ===== \\
        foreach (AssetBundle character_bundle in Character_Bundles.Values)
        {
            // ===== 初始化 SpineClip ===== \\
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

            // ===== 初始化 VolumeProfile ===== \\
            VolumeProfile[] volume_profile_array = character_bundle.LoadAllAssets<VolumeProfile>();
            if (volume_profile_array.Length != 0)
            {
                Console_Log($"开始初始化 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
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
                    Console_Log($"已禁用 PaniniProjection 特效");
                }
                Console_Log($"结束初始化 {volume_profile_array.Length} 个 Volume Profile 中的第一个", Debug_Services.LogLevel.Core);
            }
        }
    }



    /// <summary>
    /// 实例化角色的 Prefab，并获取实例化后必要的 GameObject 信息。
    /// </summary>
    public void Instantiate_Character_GameObject()
    {
        Console_Log("开始实例化角色 Prefab");

        lobby_gameobject_prefab = (GameObject)Core_Bundle.LoadAsset($"Assets/_MX/AddressableAsset/UI/UILobbyElement/{memory_lobby_info.PrefabName}.prefab");
        if (lobby_gameobject_prefab != null)
        {
            lobby_gameobject_instantiated = Instantiate(lobby_gameobject_prefab, transform.position, Quaternion.identity);
            Console_Log("成功实例化角色 Prefab");

            EyeIK_GameObject = lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().SpineCharacter.gameObject.transform.Find("EyeIK").gameObject;
            if(EyeIK_GameObject != null)
            {
                Console_Log("成功获取到 EyeIK 的 GameObject");
            }
            else
            {
                Console_Log("EyeIK 的 GameObject 为空", Debug_Services.LogLevel.Debug, LogType.Error);
            }
        }
        else
        {
            Console_Log("角色 Prefab 为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("结束实例化角色 Prefab");
    }



    /// <summary>
    /// 初始化 SkeletonAnimation 组件，并注册 Spine 事件监听器。
    /// </summary>
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



    /// <summary>
    /// 初始化 PlayableDirector 组件，并获取 TimelineAsset 资源。
    /// </summary>
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



    /// <summary>
    /// 初始化 TimelineAsset 资源，并检查 Idle_01 动画的同步播放动画。
    /// </summary>
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

            Console_Log("开始检查 Idle_01 动画的同步播放动画", Debug_Services.LogLevel.Core);
            foreach (TrackAsset timeline_track in timeline_asset.GetOutputTracks())
            {
                if (timeline_track is SpineAnimationStateTrack spine_animation_state_track)
                {
                    original_track = spine_animation_state_track;
                    foreach (TimelineClip timeline_clip in original_track.GetClips())
                    {
                        SpineAnimationStateClip spine_animation_state_clip = timeline_clip.asset as SpineAnimationStateClip;
                        SpineClip spine_clip = spine_animation_state_clip.template.animationReference as SpineClip;
                        if (spine_clip != null)
                        {
                            if (spine_clip.ClipName == "Idle_01")
                            {
                                Console_Log("找到了 Idle_01 动画", Debug_Services.LogLevel.Core);
                                Idle_SpineClip = spine_clip;
                                Console_Log($"Idle_01 动画有 {spine_clip.SyncPlayClipObjects.Count()} 个同步动画", Debug_Services.LogLevel.Core);
                            }
                        }
                    }
                }
            }
            Console_Log("结束检查 Idle_01 动画的同步播放动画", Debug_Services.LogLevel.Core);


            Console_Log($"开始在轨道 {Idle_SpineClip.SyncPlayClipObjects.Count() + 1} 上创建 Talk_M 动画轨道", Debug_Services.LogLevel.Core);
            Index_Services.Instance.M_Track_Num = Idle_SpineClip.SyncPlayClipObjects.Count() + 1;
            talk_m_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_M");
            talk_m_track.trackIndex = Idle_SpineClip.SyncPlayClipObjects.Count() + 1;
            player_director.SetGenericBinding(talk_m_track, skeleton_animation);
            Console_Log($"结束在轨道 {Idle_SpineClip.SyncPlayClipObjects.Count() + 1} 上创建 Talk_M 动画轨道", Debug_Services.LogLevel.Core);

            Console_Log($"开始在轨道 {Idle_SpineClip.SyncPlayClipObjects.Count() + 2} 上创建 Talk_A 动画轨道", Debug_Services.LogLevel.Core);
            Index_Services.Instance.A_Track_Num = Idle_SpineClip.SyncPlayClipObjects.Count() + 2;
            talk_a_track = timeline_asset.CreateTrack<SpineAnimationStateTrack>(null, "Talk_A");
            talk_a_track.trackIndex = Idle_SpineClip.SyncPlayClipObjects.Count() + 2;
            player_director.SetGenericBinding(talk_a_track, skeleton_animation);
            Console_Log($"结束在轨道 {Idle_SpineClip.SyncPlayClipObjects.Count() + 2} 上创建 Talk_A 动画轨道", Debug_Services.LogLevel.Core);
        }
        else
        {
            Console_Log($"PlayableDirector 组件为空", Debug_Services.LogLevel.Debug, LogType.Error);
        }

        Console_Log("结束初始化 TimelineAsset 资源");
    }



    /// <summary>
    /// 禁用 ChatDialog 物体，以避免在加载角色时显示聊天对话框。
    /// </summary>
    public void Disable_ChatDialog_GameObject()
    {
        lobby_gameobject_instantiated.GetComponent<UILobbyContainer>().ChatDialog.gameObject.SetActive(false);
        Console_Log("已禁用 ChatDialog 物体");
    }



    private int Event_Index = 0;
    private int Audio_File_Index = 0;
    /// <summary>
    /// Spine 事件监听器，当 Spine 动画触发事件时调用。
    /// </summary>
    /// <param name="track_entry">轨道实例</param>
    /// <param name="spine_event">Spine 事件</param>
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



    /// <summary>
    /// 卸载角色的所有资源。
    /// </summary>
    public void Unload_Character()
    {
        Console_Log($"开始卸载角色: {Character_Name}");

        Index_Services.Instance.is_Idle_Mode = false;
        Index_Services.Instance.is_Talking = false;
        if (Subtitle_Services.Instance != null) StartCoroutine(Stop_Subtitle_Delay());  // 只有在字幕服务存在且正在显示时才停止字幕
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.Talk_GameObject);
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.SFX_GameObject);
        Destroy(Audio_Services.Instance.BGM_GameObject.GetComponent<Audio_Loop_Controller>());
        Audio_Services.Remove_All_AudioSources(Audio_Services.Instance.BGM_GameObject);
        Destroy(GameObject.Find("UI Root"));
        Destroy(GetComponent<Volume>());
        Destroy(GetComponent<Character>());
        Resources.UnloadUnusedAssets();
        System.GC.Collect();

        Console_Log($"结束卸载角色: {Character_Name}");
        Console_Log($"· ======================================== ·");
    }



    /// <summary>
    /// 延迟停止字幕显示的协程。
    /// </summary>
    /// <returns></returns>
    private IEnumerator Stop_Subtitle_Delay()
    {
        yield return null;  // 等好帧！—— By Japerz
        Subtitle_Services.Instance.is_Stopping_Display = true;
    }



    /// <summary>
    /// 强制卸载角色的所有 AssetBundle 包
    /// </summary>
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



    /// <summary>
    /// 卸载角色的所有 AssetBundle 包，但不释放内存。
    /// </summary>
    public void Unload_Character_Bundles_False()
    {
        Core_Bundle.Unload(false);
        foreach (AssetBundle asset_bundle in Dependencies_Bundles.Values) asset_bundle.Unload(false);
        foreach (AssetBundle asset_bundle in Character_Bundles.Values) asset_bundle.Unload(false);
        Resources.UnloadUnusedAssets();
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("Character_Services", message, loglevel, logtype); }
}
