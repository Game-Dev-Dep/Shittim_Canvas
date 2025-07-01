using Spine;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static UIBasicSprite;


public class SpineClip : AnimationReferenceAsset
{
    public BehaviorType PlayMode;
    public FinishType FinishMode;
    public string ClipName;
    public bool IsTrackMainIdle;
    private float defaultMix;
    public float DefaultMix => defaultMix;
    private float clipDuration;
    public float ClipDuration => clipDuration;
    public bool UseDefaultIntroMix;
    public float IntroMix;
    public bool Loop;
    public int Track;
    public bool UseDefaultOutroMix;
    public float OutroMix;
    public float OutroStartOffset;
    private float timeToOutroStart;
    public float TimeToOutroStart => timeToOutroStart;
    private bool isInitialized;
    public bool IsInitialized => isInitialized;
    private bool canReceiveQueue;
    public bool CanReceiveQueue 
    {
        get => canReceiveQueue;
        set => canReceiveQueue = value; 
    }
    public PreDelayType RandomTimingIntroDelayMode;
    public float IntroDelayDuration;
    public float RandomDelayMin;
    public float RandomDelayMax;
    public int AddRandomLoopMin;
    public int AddRandomLoopMax; 
    private float randomTimingPlayStartTime;
    public float RandomTimingPlayStartTime
    {
        get => randomTimingPlayStartTime;
        set => randomTimingPlayStartTime = value;
    }
    private float randomizedDelayDuration;
    public float RandomizedDelayDuration
    {
        get => randomizedDelayDuration;
        set => randomizedDelayDuration = value;
    }
    public ScriptableObject NextClipObject;
    public ScriptableObject[] SyncPlayClipObjects;
    public SoundKey[] SoundKeys;
    private SpineClip nextClip;
    public SpineClip NextClip => nextClip;
    public Spine.Animation Clip {
        get => animation;
        set => animation = value;
    }

    public new void Initialize()
    {
        if (!isInitialized)
        {
            if (skeletonDataAsset != null)
            {
                SkeletonData skeletonData = skeletonDataAsset.GetSkeletonData(true);
                animationName = ClipName;
                Clip = skeletonData.FindAnimation(ClipName);

                if (Clip != null)
                {
                    clipDuration = Clip.Duration;
                    timeToOutroStart = clipDuration + OutroStartOffset;
                    canReceiveQueue = true;
                }
            }

            isInitialized = true;
        }
    }

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
}
