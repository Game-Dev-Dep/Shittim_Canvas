using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class Audio_Loop_Controller : MonoBehaviour
{
    private AudioSource audio_source;
    private int loop_start_samples;
    private int loop_end_samples;

    bool is_first_time = true;

    public void Initialize(float loopStartTime, float loopEndTime)
    {
        audio_source = GetComponent<AudioSource>();

        // 转换为采样点保证精度
        int sampleRate = audio_source.clip.frequency;
        loop_start_samples = Mathf.Clamp(
            (int)(loopStartTime * sampleRate),
            0,
            audio_source.clip.samples - 1
        );

        loop_end_samples = Mathf.Clamp(
            (int)(loopEndTime * sampleRate),
            loop_start_samples + 1,
            audio_source.clip.samples
        );

        // 设置初始播放位置
        audio_source.timeSamples = 0;
    }

    void Update()
    {
        if (audio_source.isPlaying && audio_source.timeSamples >= loop_end_samples)
        {
            if (is_first_time) is_first_time = false;
            audio_source.timeSamples = loop_start_samples;
        }
        if (audio_source.isPlaying && audio_source.timeSamples < loop_start_samples && !is_first_time)
        {
            audio_source.timeSamples = loop_start_samples;
        }
    }
}
