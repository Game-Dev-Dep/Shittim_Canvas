using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MX.Audio
{
    public class AmbientAudioEvent : ScriptableObject // TypeDefIndex: 14691
    {
        public enum CommandType
        {
            Play = 0,
            Stop = 1
        }

        // Fields
        [Header("Ambience")]
        public CommandType Command; // 0x18
        public float CrossFadeDuration; // 0x1C
        public AudioClip Clip; // 0x20
        public bool Loop; // 0x28
        [Range(0, 1)]
        public float Volume; // 0x2C
        [Range(-3, 3)]
        public float Pitch; // 0x30
    }
}
