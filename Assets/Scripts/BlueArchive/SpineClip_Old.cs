using Spine;
using Spine.Unity;
using System;
using System.Reflection;
using UnityEngine;

public enum BehaviorType
{
    BaseTrack,
    Masked,
    MaskedRandomTiming
}

public enum FinishType
{
    DoNothing,
    PlayIdle,
    FinishMask,
    PlayNext
}

public enum PreDelayType
{
    Immediate,
    Random,
    DelayedRandom
}

public struct SoundKey
{
    public float Time;
    public ScriptableObject Event;
}

public class SpineClip_Old : AnimationReferenceAsset
{
    public BehaviorType PlayMode;
    public FinishType FinishMode;
    public string ClipName;
    public bool IsTrackMainIdle;
    private float defaultMix;
    private float clipDuration;
    public bool UseDefaultIntroMix;
    public float IntroMix;
    public bool Loop;
    public int Track;
    public bool UseDefaultOutroMix;
    public float OutroMix;
    public float OutroStartOffset;
    private float timeToOutroStart;
    private bool isInitialized;
    private bool canReceiveQueue;
    public PreDelayType RandomTimingIntroDelayMode;
    public float IntroDelayDuration;
    public float RandomDelayMin;
    public float RandomDelayMax;
    public int AddRandomLoopMin;
    public int AddRandomLoopMax;
    private float randomTimingPlayStartTime;
    private float randomizedDelayDuration;
    public ScriptableObject NextClipObject;
    public ScriptableObject[] SyncPlayClipObjects;
    public SoundKey[] SoundKeys;
    private SpineClip nextClip;

    private Spine.Animation Clip;
    public float DefaultMix => defaultMix;
    public float ClipDuration => clipDuration;
    public float TimeToOutroStart => timeToOutroStart;
    public bool IsInitialized = false;
    public bool CanReceiveQueue
    {
        get => canReceiveQueue;
        set => canReceiveQueue = value;
    }
    public float RandomTimingPlayStartTime
    {
        get => randomTimingPlayStartTime;
        set => randomTimingPlayStartTime = value;
    }
    public float RandomizedDelayDuration
    {
        get => randomizedDelayDuration;
        set => randomizedDelayDuration = value;
    }
    public SpineClip NextClip => nextClip;
    public new Spine.Animation Animation => Clip;
    public new void Initialize()
    {
        if (IsInitialized) return;

        base.Initialize();

        if (skeletonDataAsset != null)
        {
            SkeletonData skeleton_data_asset = skeletonDataAsset.GetSkeletonData(true);
            Debug.Log(ClipName);
            Clip = skeleton_data_asset.FindAnimation(ClipName);
            if (Clip != null)
            {
                Type baseType = typeof(AnimationReferenceAsset);
                FieldInfo animationField = baseType.GetField("animation", BindingFlags.NonPublic | BindingFlags.Instance);
                animationField?.SetValue(this, Clip);

                base.animation = Clip;

                clipDuration = Clip.Duration;
                //IsInitialized = clipDuration + OutroStartOffset;
            }

            canReceiveQueue = true;

            if (NextClipObject != null)
            {
                nextClip = NextClipObject as SpineClip;
            }

            IsInitialized = true;

        }
    }
}