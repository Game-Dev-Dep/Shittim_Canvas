using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PortraitSpineCharacter : SpineCharacter, IPortraitSpineCharacter // TypeDefIndex: 3185
{
    // Fields
    public const string UIDefaultClipName = "00";
    private string clipToPlayOnIdleName; // 0xC0
    private SpineClip scToPlayOnIdle; // 0xC8
    private Coroutine sAnimCoroutine; // 0xD0
    private ShaderBaseAnimationData sAnimInstance; // 0xD8
    private Color dimmedColor; // 0xE0

    // Properties
    public string ClipToPlayOnIdle { get; set; }
    private Animation AniToPlayOnIdle { get; }
    public GameObject GameObject { get; }
}
