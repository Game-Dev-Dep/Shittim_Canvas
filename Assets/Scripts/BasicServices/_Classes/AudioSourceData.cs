using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

namespace MX.Audio
{
    [Serializable]
    public class CustomCurve
    {
        public AnimationCurve Curve;
        public AudioSourceCurveType CurveType;
    }
    [Serializable]
    public class AudioMixerSnapshotInfo
    {
        public AudioMixerSnapshot AudioMixerSnapshot;
        public float TimeToReach;
    }
    [Serializable]
    public class AudioSourceData
    {
        public int GroupId;
        public bool IgnoreInterruptDelay;
        public bool IgnoreInterruptPlay;
        public AudioMixerGroup AudioMixerGroup;
        public AudioMixerSnapshotInfo AudioMixerSnapshot;
        public List<AudioClip> AudioClips;
        public List<CustomCurve> CustomCurves;
        public bool BypassEffects;
        public bool BypassListenerEffects;
        public bool BypassReverbZones;
        public bool Loop;
        [Range(0, 256)] public int Priority;
        [Range(0, 1)] public float Volume;
        [Range(0, 3)] public float Pitch;
        [Range(-12, 12)] public int RandomPitchMin;
        [Range(-12, 12)] public int RandomPitchMax;
        public float Delay;
        [Range(-1, 1)] public float StereoPan;
        [Range(0, 1)] public float SpatialBlend;
        [Range(0, 1.1f)] public float ReverbZoneMix;
        [Range(0, 5)] public float DopplerLevel;
        [Range(0, 360)] public float Spread;
        public AudioRolloffMode VolumeRolloff;
        public float MinDistance;
        public float MaxDistance;
    }
}