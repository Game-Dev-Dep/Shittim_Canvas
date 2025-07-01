using System;
using UnityEngine;

[RequireComponent(typeof(SafeAreaScaler))]
[AddComponentMenu("MX/Utils/UIManualScaleByAspect")]
[DisallowMultipleComponent]
public class UIManualScaleByAspect : MonoBehaviour
{
    [Serializable]
    public class OffsetInfo
    {
        public Transform Transform;
        public Vector3 MostWideAdd;
        public Vector3 MostNarrowAdd;
        public Vector3 AddFactor;
    }

    // Fields
    [Header("19.5 : 9 일 때의 확대량")]
    [SerializeField]
    [Range(1, (float)1.5)]
    private float mostWideScale;
    [Header("4 : 3 일 때의 확대량")]
    [SerializeField]
    [Range(1, (float)1.5)]
    private float mostNarrowScale;
    [SerializeField]
    [Header("확대할 대상")]
    private Transform[] scaleTransforms;
    [SerializeField]
    [Header("이동할 대상")]
    private UIManualScaleByAspect.OffsetInfo[] offsetSettings;
    private float scaleFactor;

    // Methods
    /*
    private void OnEnable() { }
    private void OnDisable() { }
    private void Refresh() { }
    private void Discard() { }
    private Vector3 IntVector(Vector3 v) { }
    */
}
