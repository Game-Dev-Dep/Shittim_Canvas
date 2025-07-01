using Spine.Unity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpineCharacter : SpineBase // TypeDefIndex: 3199
{
    // Fields
    private bool Awaken; // 0x90
    public Action BodyTouchCB; // 0x98
    public Action<string> PlayVoiceCB; // 0xA0
    public bool MuteVoice; // 0xA8
    [Space]
    public List<Material> InstMaterials; // 0xB0
    private readonly List<object> blockInteractionRequesters; // 0xB8

    // Properties
    public Material InstMaterial { get; }
}
