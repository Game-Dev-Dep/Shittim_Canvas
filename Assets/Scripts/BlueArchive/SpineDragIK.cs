using Spine;
using System.Collections;
using System.Linq;
using UnityEngine;

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

    [Header("ƽ������")]
    public float smoothTime = 0.15f;          // ������ƽ��ʱ��
    public float maxSpeed = Mathf.Infinity;   // ����������ٶ�
    private Vector3 currentVelocity;          // ��������ǰ�ٶ�

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
        StopAllCoroutines();
    }

    private void Update()
    {
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window)
            {
                if (coroutine != null)
                {
                    Console_Log("Э�̲�Ϊ�գ��ͷ�һ��");
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
        //Console_Log($"{gameObject.name} OnPress ����");

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


        Console_Log($"{gameObject.name} OnPress �Ϸ�");


        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            isPressing = Wallpaper_Mode_Handler.Instance.is_Pressing;
        }
        else isPressing = state;

        

        if (state)
        {
            Console_Log($"{gameObject.name} OnPress ����");

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
            Console_Log($"{gameObject.name} OnPress �ɿ�");

            SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(1, 0.5f);
            SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(2, 0.5f);

            SpineController.SkeletonAnimation.AnimationState.SetAnimation(1, EndClip.ClipName, false);
            Console_Log($"{gameObject.name} ���Ž������� 01: {EndClip.ClipName}");

            if (EndClip.SyncPlayClipObjects.Count() != 0 && EndClip.SyncPlayClipObjects.First() != null)
            {
                SpineClip sync_clip = EndClip.SyncPlayClipObjects[0] as SpineClip;
                Console_Log($"{gameObject.name} ���Ž������� 02: {sync_clip.ClipName}");
                SpineController.SkeletonAnimation.AnimationState.SetAnimation(2, sync_clip.ClipName, false);
            }

            SpineController.SkeletonAnimation.AnimationState.AddEmptyAnimation(1, 0.5f, 0f);
            SpineController.SkeletonAnimation.AnimationState.AddEmptyAnimation(2, 0.5f, 0f);

            //isUpdating = false;
            screenPos = new Vector3(0, 0, 0);
            destLocalPos = new Vector3(0, 0, 0);
        }
    }

    public void OnDrag()
    {
        //Console_Log($"{gameObject.name} OnDrag ����");

        if (SpineController.SkeletonAnimation.AnimationState.GetCurrent(0)?.Animation.Name != "Idle_01") return;
        if (Index_Services.Instance.is_Talking) return;
        if (!Spine_Services.Instance.is_IK_On) return;

        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Window_Services.Instance.Cur_Cover_Window_Type != Window_Services.Cover_Window_Type.No_Window) return;
            if (gameObject != Index_Services.Instance.Cur_Responsing_Object) return;
            if (!Wallpaper_Mode_Handler.Instance.is_Draging) return;
        }

        //Console_Log($"{gameObject.name} OnDrag �Ϸ�");

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
        //Console_Log($"{gameObject.name} ������� ����");

        if (!isUpdating) return;
        if (Wallpaper_Services.Instance.is_Wallpaper_Mode)
        {
            if (Index_Services.Instance.Cur_Responsing_Object != gameObject) return;
        }

        //Console_Log($"{gameObject.name} ������� �Ϸ�");

        // ����ת�����̱��ֲ���
        Vector3 worldPos = CachedCamera.ScreenToWorldPoint(screenPos);
        Transform parent = Bone.parent;
        Vector3 localPos = parent.InverseTransformPoint(worldPos);

        // ������׼��ƫ�Ƽ���
        Vector3 baseOffset = parent.InverseTransformPoint(Bone.parent.TransformPoint(OrigLocalPos));

        destLocalPos = new Vector3(
            Mathf.Clamp(localPos.x + BoneCenterOffset.x - baseOffset.x, MinLocalPos.x, MaxLocalPos.x),
            Mathf.Clamp(localPos.y + BoneCenterOffset.y - baseOffset.y, MinLocalPos.y, MaxLocalPos.y),
            OrigLocalPos.z  // ����ԭʼZֵ
        );
    }

    private IEnumerator CoTrigger()
    {
        yield return new WaitForSeconds(TriggerDelay);
        if (isPressing)
        {
            SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(1, 0.5f);
            SpineController.SkeletonAnimation.AnimationState.SetEmptyAnimation(2, 0.5f);

            if (IngClip.Loop)
            {
                SpineController.SkeletonAnimation.AnimationState.SetAnimation(1, IngClip.ClipName, true);
                Console_Log($"{gameObject.name} ���Ž��ж��� 01: {IngClip.ClipName}");
                if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
                {
                    SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                    Console_Log($"{gameObject.name} ���Ž��ж��� 02: {sync_clip.ClipName}");
                    SpineController.SkeletonAnimation.AnimationState.SetAnimation(2, sync_clip.ClipName, true);
                }
            }
            else
            {
                TrackEntry track_entry = SpineController.SkeletonAnimation.AnimationState.SetAnimation(1, IngClip.ClipName, false);
                track_entry.Complete += OnClipComplete;
                Console_Log($"{gameObject.name} ������ʼ���� 01: {IngClip.ClipName}");
                if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
                {
                    SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                    Console_Log($"{gameObject.name} ������ʼ���� 02: {sync_clip.ClipName}");
                    SpineController.SkeletonAnimation.AnimationState.SetAnimation(2, sync_clip.ClipName, true);
                }
            }
            
        }
    }

    private void OnClipComplete(Spine.TrackEntry trackEntry)
    {
        //if (trackEntry.TrackIndex != 1) return;
        Console_Log($"{gameObject.name} ��������");

        trackEntry.Complete -= OnClipComplete;

        if (IngClip.NextClip != null)
        {
            IngClip = IngClip.NextClip;
            SpineController.SkeletonAnimation.AnimationState.SetAnimation(1, IngClip.ClipName, true);
            Console_Log($"{gameObject.name} ���ź���ѭ������ 01: {IngClip.ClipName}");
            if (IngClip.SyncPlayClipObjects.Count() != 0 && IngClip.SyncPlayClipObjects.First() != null)
            {
                SpineClip sync_clip = IngClip.SyncPlayClipObjects[0] as SpineClip;
                Console_Log($"{gameObject.name} ���ź���ѭ������ 02: {sync_clip.ClipName}");
                SpineController.SkeletonAnimation.AnimationState.SetAnimation(2, sync_clip.ClipName, true);
            }
        }
    }

    private IEnumerator CoMoveBone()
    {
        Console_Log($"{gameObject.name} Э�� CoMoveBone ����");

        isUpdating = true;
        //Console_Log($"{gameObject.name} �� isUpdating ����Ϊ {isUpdating}");

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
            Bone.localPosition = Vector3.SmoothDamp(
                Bone.localPosition,
                targetPos,
                ref currentVelocity,
                smoothTime,
                maxSpeed,
                Time.deltaTime
            );

            if (!isPressing && Vector3.Distance(Bone.localPosition, OrigLocalPos) < 0.001f)
            {
                Bone.localPosition = OrigLocalPos;
                isUpdating = false;
                //Console_Log($"{gameObject.name} �� isUpdating ����Ϊ {isUpdating}");
                Console_Log($"{gameObject.name} Э�� CoMoveBone ��ֹ {isUpdating}");
                yield break;
            }

            yield return null;
        }
    }

    private static void Console_Log(string message, Debug_Services.LogLevel loglevel = Debug_Services.LogLevel.Info, LogType logtype = LogType.Log) { Debug_Services.Instance.Console_Log("SpineDragIK", message, loglevel, logtype); }
}