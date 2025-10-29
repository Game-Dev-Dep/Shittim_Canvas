using System;
using System.Collections.Generic;

[Serializable]
public class Camera_Config
{
    public string Defalut_Character_Name { get; set; } = "CH0239";
    public float Camera_Position_X { get; set; } = 0.0f;
    public float Camera_Position_Y { get; set; } = 0.0f;
    public float Camera_Rotation_Z { get; set; } = 0.0f;
    public float Camera_Size { get; set; } = 1.0f;
    
    // 每个角色的相机参数
    public Dictionary<string, CharacterCameraData> Character_Camera_Settings { get; set; } = new Dictionary<string, CharacterCameraData>();
}

[Serializable]
public class CharacterCameraData
{
    public float Camera_Position_X { get; set; } = 0.0f;
    public float Camera_Position_Y { get; set; } = 0.0f;
    public float Camera_Rotation_Z { get; set; } = 0.0f;
    public float Camera_Size { get; set; } = 1.0f;
}
