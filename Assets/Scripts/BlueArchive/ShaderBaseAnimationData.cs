using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class ShaderBaseAnimationData : ScriptableObject // TypeDefIndex: 3164
{
    // Fields
    public float PreDelay; // 0x18
    public string EnableKeyword; // 0x20
    public bool DisableKeywordOnFinish; // 0x28
    public float Duration; // 0x2C
    public bool IsUnscaledTime; // 0x30
    public bool Loop; // 0x31
}
