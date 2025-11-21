using Spine;
using System.Collections;
using System.Linq;
using UnityEngine;
using System.Reflection;
using System;

public class SpineDragIK : MonoBehaviour
{
    public SpineCharacter SpineController;
    public SpineClip IngClip;
    public SpineClip EndClip;
    public Transform Bone;
    public Vector3 OrigLocalPos;
    public Vector3 BoneCenterOffset;
    public Vector3 MinLocalPos;
    public Vector3 MaxLocalPos;
    [Range(0, 1)] public float FollowDragSpeed01;
    [Range(0, 1)] public float FollowReleaseSpeed01;
    public float TriggerDelay;

    public Vector3 screenPos;
    public float screenZDistance;
    public Vector3 destLocalPos;
    public bool isPressing;
    public bool isUpdating;
    public Coroutine coroutine;
    public Coroutine triggerCoroutine;
    public PortraitSpineCharacter portraitSpineCharacter;
    public Camera mCachedCamera;

    [Header("平滑设置")]
    public float smoothTime = 0.15f;          // 新增：平滑时间
    public float maxSpeed = Mathf.Infinity;   // 新增：最大速度
    private Vector3 currentVelocity;          // 新增：当前速度

    private Component boneFollowerComponent;
    private bool originalFollowMode = true;
    private Vector3 pressStartLocalPos;

    private Camera CachedCamera
    {
        get
        {
            if (mCachedCamera == null)
            {
                mCachedCamera = Camera_Services.Instance.MemoryLobby_Camera;
            }
            return mCachedCamera;
        }
    }

    private SpineClip initialIngClip;
    void OnEnable()
    {
        initialIngClip = IngClip;
        destLocalPos = OrigLocalPos;

        if (Bone != null)
        {
            boneFollowerComponent = FindBoneFollowerComponent(Bone.gameObject);
            if (boneFollowerComponent != null)
            {
                originalFollowMode = GetBoneMode(boneFollowerComponent);
            }
        }

        if (Debug_Services.Instance.is_Debug)
        {
            Bone.gameObject.AddComponent<SpriteRenderer>();
            Bone.gameObject.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>("Sprites/Circle");
            Bone.gameObject.GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0.5f);
            Bone.gameObject.GetComponent<SpriteRenderer>().sortingOrder = 1;
        }

        if (gameObject.name == "HairPatIK") smoothTime = 0.1f;
    }

    void OnDisable()
    {
        if (Bone != null)
            Bone.localPosition = OrigLocalPos;

        destLocalPos = OrigLocalPos;
        SetBoneMode(originalFollowMode);
        StopAllCoroutines();
    }

    private void Update()
    {
        if (UI_Panel_Checker.IsAnyUIPanelOpen())
        {
            if (coroutine != null)
            {
                force_trigger_OnPress = true;
                OnPress(false);
                if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
                {
                    Wallpaper_Mode_Handler.Instance.is_Released = false;
                    Index_Services.Instance.Cur_Responsing_Object = null;
                }
                coroutine = null;
            }
            return;
        }

        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window)
            {
                if (coroutine != null)
                {
                    Console_Log("协程不为空，释放一次");
                    force_trigger_OnPress = true;
                    OnPress(false);
                    Wallpaper_Mode_Handler.Instance.is_Released = false;
                    Index_Services.Instance.Cur_Responsing_Object = null;
                    coroutine = null;
                }
                
                return;
            }

            if (Index_Services.Instance.Cur_Responsing_Object != gameObject) return;

            if (Wallpaper_Mode_Handler.Instance.is_Released)
            {
                OnPress(false);
                Wallpaper_Mode_Handler.Instance.is_Released = false;
                Index_Services.Instance.Cur_Responsing_Object = null;
            }

            if (Wallpaper_Mode_Handler.Instance.is_Pressed)
            {
                OnPress(true);
                Wallpaper_Mode_Handler.Instance.is_Pressed = false;
            }
            
            if (Wallpaper_Mode_Handler.Instance.is_Draging)
            {
                OnDrag();
            }
        }
        else
        {
            if (!Spine_Services.Instance.is_IK_On)
            {
                if (triggerCoroutine != null) StopCoroutine(triggerCoroutine);
                if (coroutine != null) StopCoroutine(coroutine);
                isUpdating = false;
            }
        }

        if (Debug_Services.Instance.is_Debug) Debug_Services.Instance.Debug_Info_Text_05.SetText(
            $"{gameObject.name}\n" +
            $"isPressing: {isPressing}\n" +
            $"isUpdating: {isUpdating}\n" +
            $"screenPos: {screenPos}\n" +
            $"destLocalPos: {destLocalPos}\n"
            );

    }



    private bool force_trigger_OnPress = false;
    public void OnPress(bool state)
    {
        if (UI_Panel_Checker.IsAnyUIPanelOpen()) return;

        if (!force_trigger_OnPress)
        {
            if (SpineController.SkeletonAnimation.AnimationState.GetCurrent(0)?.Animation.Name != "Idle_01") return;
            if (Index_Services.Instance.is_Talking) return;
            if (!Spine_Services.Instance.is_IK_On) return;

            if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
            {
                if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window) return;
                if (gameObject != Index_Services.Instance.Cur_Responsing_Object) return;
                if (!Wallpaper_Mode_Handler.Instance.is_Pressed && !Wallpaper_Mode_Handler.Instance.is_Released) return;
            }
        }
        else force_trigger_OnPress = false;


        Console_Log($"{gameObject.name} OnPress 合法");


        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            isPressing = Wallpaper_Mode_Handler.Instance.is_Pressing;
        }
        else isPressing = state;

        

        if (state)
        {
            Console_Log($"{gameObject.name} OnPress 按下");

            if (boneFollowerComponent == null && Bone != null)
            {
                boneFollowerComponent = FindBoneFollowerComponent(Bone.gameObject);
                if (boneFollowerComponent != null)
                {
                    originalFollowMode = GetBoneMode(boneFollowerComponent);
                }
            }
            SetBoneMode(false);

            if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
            {
                screenPos = Input_Services.Instance.Mouse_Info.Position;
            }
            else
            {
                screenPos = Input.mousePosition;
            }
            
            screenPos.z = CachedCamera.WorldToScreenPoint(Bone.position).z;

            //UpdateDestLocalPos();
            pressStartLocalPos = Bone.parent.InverseTransformPoint(CachedCamera.ScreenToWorldPoint(screenPos));
            destLocalPos = Bone.localPosition;

            IngClip = initialIngClip;

            if (triggerCoroutine != null) StopCoroutine(triggerCoroutine);
            triggerCoroutine = StartCoroutine(CoTrigger());

            if (coroutine != null)
            {
                StopCoroutine(coroutine);
                
            }
            coroutine = StartCoroutine(CoMoveBone());
        }
        else
        {
            Console_Log($"{gameObject.name} OnPress 松开");

            if (EndClip != null)
            {
                Console_Log($"{gameObject.name} 播放结束动画 01: {EndClip.ClipName}");
                TrackEntry track_entry_01 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.M_Track_Num, EndClip.ClipName, false, 0f);
                SpineController.SkeletonAnimation.AnimationState.AddEmptyAnimation(Index_Services.Instance.M_Track_Num, 0f, 0f);

                if (EndClip.SyncPlayClipObjects.Count() != 0 && EndClip.SyncPlayClipObjects.First() != null)
                {
                    SpineClip sync_clip = EndClip.SyncPlayClipObjects[0] as SpineClip;
                    Console_Log($"{gameObject.name} 播放结束动画 02: {sync_clip.ClipName}");
                    TrackEntry track_entry_02 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.A_Track_Num, sync_clip.ClipName, false, 0f);
                    SpineController.SkeletonAnimation.AnimationState.AddEmptyAnimation(Index_Services.Instance.A_Track_Num, 0f, 0f);
                }
            }

            screenPos = new Vector3(0, 0, 0);
            destLocalPos = new Vector3(0, 0, 0);
        }
    }

    public void OnDrag()
    {
        if (UI_Panel_Checker.IsAnyUIPanelOpen()) return;

        if (SpineController.SkeletonAnimation.AnimationState.GetCurrent(0)?.Animation.Name != "Idle_01") return;
        if (Index_Services.Instance.is_Talking) return;
        if (!Spine_Services.Instance.is_IK_On) return;

        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window) return;
            if (gameObject != Index_Services.Instance.Cur_Responsing_Object) return;
            if (!Wallpaper_Mode_Handler.Instance.is_Draging) return;
        }

        //Console_Log($"{gameObject.name} OnDrag 合法");

        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            screenPos = Input_Services.Instance.Mouse_Info.Position;
        }
        else
        {
            screenPos = Input.mousePosition;
        }
        screenPos.z = screenZDistance;

        UpdateDestLocalPos();
    }

    private void UpdateDestLocalPos()
    {
        if (UI_Panel_Checker.IsAnyUIPanelOpen()) return;

        if (!isUpdating) return;
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Index_Services.Instance.Cur_Responsing_Object != gameObject) return;
        }

        //Console_Log($"{gameObject.name} 坐标更新 合法");

        // 坐标转换流程保持不变
        Vector3 worldPos = CachedCamera.ScreenToWorldPoint(screenPos);
        Transform parent = Bone.parent;
        Vector3 currentLocalPos = parent.InverseTransformPoint(worldPos);
        
        Vector3 offset = currentLocalPos - pressStartLocalPos;
        Vector3 targetLocalPos = OrigLocalPos + offset + BoneCenterOffset;

        destLocalPos = new Vector3(
            Mathf.Clamp(targetLocalPos.x, OrigLocalPos.x + MinLocalPos.x, OrigLocalPos.x + MaxLocalPos.x),
            Mathf.Clamp(targetLocalPos.y, OrigLocalPos.y + MinLocalPos.y, OrigLocalPos.y + MaxLocalPos.y),
            OrigLocalPos.z  // 保持原始Z值
        );
    }

    private IEnumerator CoTrigger()
    {
        yield return new WaitForSeconds(TriggerDelay);
        if (isPressing && IngClip != null)
        {
            if (IngClip.Loop)
            {
                Console_Log($"{gameObject.name} 播放进行动画 01: {IngClip.ClipName}");
                SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.M_Track_Num, 0);
                TrackEntry track_entry_01 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.M_Track_Num, IngClip.ClipName, true, 0);
                //track_entry_01.MixDuration = 1f;


                if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
                {
                    SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                    Console_Log($"{gameObject.name} 播放进行动画 02: {sync_clip.ClipName}");
                    SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.A_Track_Num, 0);
                    TrackEntry track_entry_02 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.A_Track_Num, sync_clip.ClipName, true, 0);
                    //track_entry_02.MixDuration = 1f;
                }
            }
            else
            {
                Console_Log($"{gameObject.name} 播放起始动画 01: {IngClip.ClipName}");
                SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.M_Track_Num, 0f);
                TrackEntry track_entry_01 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.M_Track_Num, IngClip.ClipName, false, 0f);
                //track_entry_01.MixDuration = 1f;
                track_entry_01.Complete += OnClipComplete;

                if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
                {
                    SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                    Console_Log($"{gameObject.name} 播放起始动画 02: {sync_clip.ClipName}");
                    SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.A_Track_Num, 0f);
                    TrackEntry track_entry_02 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.A_Track_Num, sync_clip.ClipName, false, 0f);
                    //track_entry_02.MixDuration = 1f;
                }
            }
            
        }
    }

    private void OnClipComplete(Spine.TrackEntry trackEntry)
    {
        //if (trackEntry.TrackIndex != 1) return;
        Console_Log($"{gameObject.name} 触发结束");

        trackEntry.Complete -= OnClipComplete;

        Console_Log($"{((SpineClip)IngClip.NextClipObject).ClipName}");
        if (IngClip.NextClipObject != null)
        {
            IngClip = (SpineClip)IngClip.NextClipObject;
            Console_Log($"{gameObject.name} 播放后续循环动画 01: {IngClip.ClipName}");
            //SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.M_Track_Num, 0f);
            TrackEntry track_entry_01 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.M_Track_Num, IngClip.ClipName, true, 0f);
            //track_entry_01.MixDuration = 1f;
            
            if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
            {
                SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                Console_Log($"{gameObject.name} 播放后续循环动画 02: {sync_clip.ClipName}");
                //SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(Index_Services.Instance.A_Track_Num, 0f);
                TrackEntry track_entry_02 = SpineController.SkeletonAnimation.AnimationState.AddAnimation(Index_Services.Instance.A_Track_Num, sync_clip.ClipName, true, 0f);
                //track_entry_02.MixDuration = 1f;
            }
        }
    }

    private IEnumerator CoMoveBone()
    {
        Console_Log($"{gameObject.name} 协程 CoMoveBone 启动");

        isUpdating = true;
        //Console_Log($"{gameObject.name} 的 isUpdating 被置为 {isUpdating}");

        while (true)
        {
            if (isPressing)
            {
                if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
                {
                    screenPos = Input_Services.Instance.Mouse_Info.Position;
                }
                else
                {
                    screenPos = Input.mousePosition;
                }
                screenPos.z = screenZDistance;
                UpdateDestLocalPos();
            }

            Vector3 targetPos = isPressing ? destLocalPos : OrigLocalPos;
            float currentSmoothTime = isPressing ? smoothTime : smoothTime * 0.3f;
            Bone.localPosition = Vector3.SmoothDamp(
                Bone.localPosition,
                targetPos,
                ref currentVelocity,
                currentSmoothTime,
                maxSpeed,
                Time.deltaTime
            );

            if (!isPressing && Vector3.Distance(Bone.localPosition, OrigLocalPos) < 0.001f)
            {
                Bone.localPosition = OrigLocalPos;
                isUpdating = false;
                SetBoneMode(originalFollowMode);
                yield break;
            }

            yield return null;
        }
    }

    private Component FindBoneFollowerComponent(GameObject boneObject)
    {
        Component[] components = boneObject.GetComponents<Component>();
        foreach (Component comp in components)
        {
            Type compType = comp.GetType();
            if (compType.Name == "SkeletonUtilityBone")
            {
                return comp;
            }
            if (compType.Name.Contains("BoneFollower") || compType.Name.Contains("Follower"))
            {
                return comp;
            }
            FieldInfo followField = compType.GetField("followBoneRotation") ?? compType.GetField("followBonePosition");
            PropertyInfo followProp = compType.GetProperty("followBoneRotation") ?? compType.GetProperty("followBonePosition");
            if (followField != null || followProp != null)
            {
                return comp;
            }
            FieldInfo modeField = compType.GetField("overrideMode") ?? compType.GetField("mode");
            PropertyInfo modeProp = compType.GetProperty("overrideMode") ?? compType.GetProperty("mode");
            if (modeField != null || modeProp != null)
            {
                return comp;
            }
        }
        return null;
    }

    private bool GetBoneMode(Component follower)
    {
        if (follower == null) return true;

        Type compType = follower.GetType();
        
        if (compType.Name == "SkeletonUtilityBone")
        {
            PropertyInfo skelModeProp = compType.GetProperty("mode");
            if (skelModeProp != null)
            {
                object modeValue = skelModeProp.GetValue(follower);
                if (modeValue != null)
                {
                    int modeInt = Convert.ToInt32(modeValue);
                    return modeInt == 0;
                }
            }
            FieldInfo skelModeField = compType.GetField("mode");
            if (skelModeField != null)
            {
                object modeValue = skelModeField.GetValue(follower);
                if (modeValue != null)
                {
                    int modeInt = Convert.ToInt32(modeValue);
                    return modeInt == 0;
                }
            }
            return true;
        }

        FieldInfo followRotField = compType.GetField("followBoneRotation");
        if (followRotField != null && followRotField.FieldType == typeof(bool))
        {
            return (bool)followRotField.GetValue(follower);
        }

        PropertyInfo followRotProp = compType.GetProperty("followBoneRotation");
        if (followRotProp != null && followRotProp.PropertyType == typeof(bool))
        {
            return (bool)followRotProp.GetValue(follower);
        }

        FieldInfo modeField = compType.GetField("overrideMode") ?? compType.GetField("mode");
        if (modeField != null)
        {
            object modeValue = modeField.GetValue(follower);
            if (modeValue != null)
            {
                int modeInt = Convert.ToInt32(modeValue);
                return modeInt == 0;
            }
        }

        PropertyInfo modeProp = compType.GetProperty("overrideMode") ?? compType.GetProperty("mode");
        if (modeProp != null)
        {
            object modeValue = modeProp.GetValue(follower);
            if (modeValue != null)
            {
                int modeInt = Convert.ToInt32(modeValue);
                return modeInt == 0;
            }
        }

        return true;
    }

    private void SetBoneMode(bool followMode)
    {
        if (boneFollowerComponent == null) return;

        Type compType = boneFollowerComponent.GetType();
        
        if (compType.Name == "SkeletonUtilityBone")
        {
            PropertyInfo skelModeProp = compType.GetProperty("mode");
            if (skelModeProp != null && skelModeProp.CanWrite)
            {
                Type propType = skelModeProp.PropertyType;
                object modeValue;
                
                if (propType.IsEnum)
                {
                    Array enumValues = Enum.GetValues(propType);
                    int targetIndex = followMode ? 0 : 1;
                    if (targetIndex < enumValues.Length)
                    {
                        modeValue = enumValues.GetValue(targetIndex);
                    }
                    else
                    {
                        modeValue = followMode ? 0 : 1;
                    }
                }
                else
                {
                    modeValue = followMode ? 0 : 1;
                }
                
                skelModeProp.SetValue(boneFollowerComponent, modeValue);
                return;
            }
            
            FieldInfo skelModeField = compType.GetField("mode");
            if (skelModeField != null)
            {
                Type fieldType = skelModeField.FieldType;
                object modeValue;
                
                if (fieldType.IsEnum)
                {
                    Array enumValues = Enum.GetValues(fieldType);
                    int targetIndex = followMode ? 0 : 1;
                    if (targetIndex < enumValues.Length)
                    {
                        modeValue = enumValues.GetValue(targetIndex);
                    }
                    else
                    {
                        modeValue = followMode ? 0 : 1;
                    }
                }
                else
                {
                    modeValue = followMode ? 0 : 1;
                }
                
                skelModeField.SetValue(boneFollowerComponent, modeValue);
                return;
            }
            
            return;
        }
        
        int modeValueInt = followMode ? 0 : 1;

        FieldInfo followRotField = compType.GetField("followBoneRotation");
        if (followRotField != null && followRotField.FieldType == typeof(bool))
        {
            followRotField.SetValue(boneFollowerComponent, followMode);
            return;
        }

        PropertyInfo followRotProp = compType.GetProperty("followBoneRotation");
        if (followRotProp != null && followRotProp.PropertyType == typeof(bool) && followRotProp.CanWrite)
        {
            followRotProp.SetValue(boneFollowerComponent, followMode);
            return;
        }

        FieldInfo modeField = compType.GetField("overrideMode") ?? compType.GetField("mode");
        if (modeField != null)
        {
            modeField.SetValue(boneFollowerComponent, modeValueInt);
            return;
        }

        PropertyInfo modeProp = compType.GetProperty("overrideMode") ?? compType.GetProperty("mode");
        if (modeProp != null && modeProp.CanWrite)
        {
            modeProp.SetValue(boneFollowerComponent, modeValueInt);
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("SpineDragIK", message, loglevel, logtype); }
}
