using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 按钮特效/音效Services
/// 拉的另外重量级的一坨，虽然看起来代码量较少，但每一坨都是重量级的。
/// It just works.
/// </summary>

[DisallowMultipleComponent]
public class Button_VFX_Services : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [Header("Target")]
    [HideInInspector] private RectTransform target;
    [Range(0.6f, 1f)] public float pressedScale = 0.92f;
    [Min(0f)] public float tweenDuration = 0.08f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    public AudioClip clickClip;
    public AudioClip downClip;
    [Range(0f, 1f)] public float volumeMultiplier = 1f;

    [Header("Options")]
    public bool disableAnimation = false;

    Vector3 _origScale;
    Coroutine _anim;
    bool _pressed;
    Button _button;

    void Awake()
    {
        if (!target) target = transform as RectTransform;
        _origScale = target.localScale;
        _button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (downClip && Audio_Services.Instance != null)
        {
            Audio_Services.Instance.PlayUI_SFX(downClip, volumeMultiplier);
        }
        
        if (!disableAnimation)
        {
            _pressed = true;
            StartScaleTween(_origScale, Vector3.one * pressedScale);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!disableAnimation)
        {
            _pressed = false;
            StartScaleTween(target.localScale, _origScale);
        }
        
        if (clickClip && Audio_Services.Instance != null)
        {
            Audio_Services.Instance.PlayUI_SFX(clickClip, volumeMultiplier);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!disableAnimation && _pressed)
        {
            _pressed = false;
            StartScaleTween(target.localScale, _origScale);
        }
    }

    void StartScaleTween(Vector3 from, Vector3 to)
    {
        if (gameObject.activeInHierarchy == false) return;
        if (_anim != null) StopCoroutine(_anim);
        _anim = StartCoroutine(TweenScale(from, to, tweenDuration));
    }

    System.Collections.IEnumerator TweenScale(Vector3 from, Vector3 to, float dur)
    {
        if (dur <= 0f) { target.localScale = to; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / dur;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            target.localScale = Vector3.LerpUnclamped(from, to, k);
            yield return null;
        }
        target.localScale = to;
        _anim = null;
    }
    
    public void PlayClickSfx()
    {
        if (clickClip && Audio_Services.Instance != null)
        {
            Audio_Services.Instance.PlayUI_SFX(clickClip, volumeMultiplier);
        }
    }

    void OnDestroy()
    {
        if (_anim != null)
        {
            StopCoroutine(_anim);
            _anim = null;
        }
    }
}
