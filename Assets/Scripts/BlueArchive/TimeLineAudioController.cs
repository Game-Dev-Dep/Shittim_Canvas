using MX.Audio;
using System;
using UnityEngine;
using UnityEngine.Playables;

public enum AudioType
{
    SFX = 0,
    Voice = 1
}
public class AudioController : PlayableBehaviour
{
    public AudioSourceData AudioData { get; set; }
    public Action PlayAction { get; set; }
    public override void OnBehaviourPlay(Playable playable, FrameData info)
    {
        base.OnBehaviourPlay(playable, info);
        Audio_Services.Instance.Play_AudioClip(Audio_Services.AudioClip_Type.SFX, AudioData);
    }
}
public class TimeLineAudioController : PlayableAsset
{
    public bool PlaySoundManager;
    public AudioType Audio;
    [SerializeField]
    private string VoiceId;
    public AudioSourceData AudioData;
    private GameObject owner;
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        this.owner = owner;
        var playable = ScriptPlayable<AudioController>.Create(graph);
        var behaviour = playable.GetBehaviour();

        if (behaviour != null)
        {
            behaviour.AudioData = this.AudioData;
            behaviour.PlayAction = GetPlayAction();
        }

        return playable;
    }
    private Action GetPlayAction() 
    {
        return () => { };
    }
    private void PlayBySoundManager() { }
    private void PlayByAudioPlayer() { }
    private void PlayByVoicePlayer() { }
}