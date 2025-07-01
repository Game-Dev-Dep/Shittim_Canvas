using System;

[Serializable]
public class Camera_Config
{
    public string Defalut_Character_Name { get; set; } = "CH0239";
    public float Camera_Position_X { get; set; } = 0.0f;
    public float Camera_Position_Y { get; set; } = 0.0f;
    public float Camera_Rotation_Z { get; set; } = 0.0f;
    public float Camera_Size { get; set; } = 1.0f;
}
