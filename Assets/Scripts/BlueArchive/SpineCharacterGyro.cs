using UnityEngine;

public class SpineCharacterGyro : MonoBehaviour
{
    public enum InputSourceType
    {
        RealGyro,      // ��ʵ������
        KeyboardSim,   // ����ģ��
    }

    // ��������
    public InputSourceType inputSource = InputSourceType.RealGyro;
    public Transform Bone;
    public Vector3 OrigLocalPos;
    public Vector3 MinLocalPos;
    public Vector3 MaxLocalPos;
    [Range(0, 1)] public float GyroFollowPower01 = 0.5f;
    public float GyroSensitivity = 1.0f;

    // ������תģʽר��
    public Transform rotationSource; // ������Ϊ��ת�ο���������

    // ����ģ�����
    public float keyboardSpeed = 0.5f;
    public KeyCode resetKey = KeyCode.R;

    public Vector3 origWorldPos;
    public Vector3 gyroOffset;
    public bool refreshStartRotation;
    public Quaternion gyroStartRotation;
    public Quaternion refObjectStartRot; // �ο������ʼ��ת

    private void OnEnable()
    {
        InitInputSource();
        gyroOffset = Vector3.zero;
    }

    private void Start()
    {
        if (Bone != null && Bone.parent != null)
        {
            origWorldPos = Bone.parent.TransformPoint(OrigLocalPos);
        }
    }

    private void Update()
    {
        if (Bone == null || Bone.parent == null) return;

        UpdateInputSource();
        ApplyMovement();
    }

    private void InitInputSource()
    {
        switch (inputSource)
        {
            case InputSourceType.RealGyro when SystemInfo.supportsGyroscope:
                Input.gyro.updateInterval = 0.1f;
                Input.gyro.enabled = true;
                break;
        }
    }

    private void UpdateInputSource()
    {
        switch (inputSource)
        {
            case InputSourceType.RealGyro:
                UpdateGyroOffsetByAttitude();
                break;

            case InputSourceType.KeyboardSim:
                UpdateKeyboardSimulation();
                break;
        }
    }

    // �����˶��߼����ֲ���
    private void ApplyMovement()
    {
        Vector3 targetWorldPos = origWorldPos + gyroOffset * GyroSensitivity;
        Vector3 targetLocalPos = Bone.parent.InverseTransformPoint(targetWorldPos);
        targetLocalPos = ClampVector3(targetLocalPos, MinLocalPos, MaxLocalPos);

        Bone.localPosition = Vector3.Lerp(
            Bone.localPosition,
            targetLocalPos,
            Mathf.Clamp01(GyroFollowPower01)
        );
    }

    // ��ʵ�������߼�
    private void UpdateGyroOffsetByAttitude()
    {
        if (Input.gyro == null) return;

        Quaternion currentAttitude = Input.gyro.attitude;
        Quaternion deltaRotation = Quaternion.Inverse(currentAttitude) * gyroStartRotation;
        Vector3 direction = deltaRotation * Vector3.forward;
        gyroOffset = new Vector3(-direction.x, -direction.y, 0);
    }

    // ����ģ���߼�
    private void UpdateKeyboardSimulation()
    {
        Vector3 input = new Vector3(
            Input.GetKey(KeyCode.D) ? 1 : Input.GetKey(KeyCode.A) ? -1 : 0,
            Input.GetKey(KeyCode.W) ? 1 : Input.GetKey(KeyCode.S) ? -1 : 0,
            0
        );

        gyroOffset += input * keyboardSpeed * Time.deltaTime;

        if (Input.GetKey(resetKey))
        {
            gyroOffset = Vector3.Lerp(gyroOffset, Vector3.zero, 5f * Time.deltaTime);
        }
    }

    // ��Ԫ����ֵ���
    private static bool IsQuaternionZero(Quaternion q)
    {
        return Mathf.Abs(q.x) < 0.0001f &&
                Mathf.Abs(q.y) < 0.0001f &&
                Mathf.Abs(q.z) < 0.0001f &&
                Mathf.Abs(q.w) < 0.0001f;
    }

    // ��ά������Χ����
    private Vector3 ClampVector3(Vector3 value, Vector3 min, Vector3 max)
    {
        return new Vector3(
            Mathf.Clamp(value.x, min.x, max.x),
            Mathf.Clamp(value.y, min.y, max.y),
            Mathf.Clamp(value.z, min.z, max.z)
        );
    }
}
