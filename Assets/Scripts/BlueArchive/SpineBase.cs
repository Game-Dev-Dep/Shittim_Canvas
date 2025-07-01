using Spine.Unity;
using Spine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using MX.Audio;

public class SpineBase : MonoBehaviour // TypeDefIndex: 3197
{
    // Fields
    private const int maxTrackEntryCount = 20;
    public const string DummyClipName = "Dummy";
    public SkeletonAnimation SkeletonAnimation; // 0x18
    [Header("커스텀 SpineClip들을 이 안에 채웁니다.")]
    [SerializeField]
    private SpineClip[] spineClipPool; // 0x20
    [SerializeField]
    [Header("Idle용 SpineClip들은 이 안에 채웁니다.")]
    private SpineClip[] idleClipPool; // 0x28
    [SerializeField]
    protected SpineClip startClip; // 0x30
    [SerializeField]
    [Header("SpineClip을 사용 안할 경우, Idle 이름")]
    public string IdleName; // 0x38
    protected readonly Dictionary<int, SpineClip> IdleClipTable; // 0x40
    protected readonly Dictionary<int, SpineClip> IdleClipIncludingSyncsTable; // 0x48
    protected readonly Dictionary<string, SpineClip> SpineClipTable; // 0x50
    protected readonly Dictionary<SpineClip, Coroutine> SpineClipCoroutineTable; // 0x58
    private readonly Dictionary<TrackEntry, Coroutine> soundCoroutineTable; // 0x60
    protected Spine.Animation dummyClip; // 0x68
	protected bool IsInitialized; // 0x7A
    //[SerializeField]
    //private AudioPlayer audioPlayer; // 0x80
    [SerializeField]
    private AmbientAudioEvent ambientEvent; // 0x88

    // Properties
    protected Action OnetimeActionOnEnable { get; set; }
    public bool ReInitializeOnEnable { get; set; }
    public bool ReInitializeOnEnableOnce { get; set; }
    //protected AudioPlayer AudioPlayer { get; }


}