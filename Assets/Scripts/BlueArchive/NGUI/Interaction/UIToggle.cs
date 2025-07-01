//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using AnimationOrTween;
using System.Collections.Generic;

/// <summary>
/// Simple toggle functionality.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Toggle")]
public class UIToggle : UIWidgetContainer
{
	/// <summary>
	/// List of all the active toggles currently in the scene.
	/// </summary>

	static public BetterList<UIToggle> list = new BetterList<UIToggle>();

	/// <summary>
	/// Current toggle that sent a state change notification.
	/// </summary>

	static public UIToggle current;

	/// <summary>
	/// If set to anything other than '0', all active toggles in this group will behave as radio buttons.
	/// </summary>

	public int group = 0;

	/// <summary>
	/// Sprite that's visible when the 'isActive' status is 'true'.
	/// </summary>

	public UIWidget activeSprite;

	/// <summary>
	/// If 'true', when checked the sprite will be hidden when the toggle is checked instead of when it's not.
	/// </summary>

	public bool invertSpriteState = false;

	/// <summary>
	/// Animation to play on the active sprite, if any.
	/// </summary>

	public Animation activeAnimation;

	/// <summary>
	/// Animation to play on the active sprite, if any.
	/// </summary>

	public Animator animator;

	/// <summary>
	/// Tween to use, if any.
	/// </summary>

	public UITweener tween;

	/// <summary>
	/// Whether the toggle starts checked.
	/// </summary>

	public bool startsActive = false;

	/// <summary>
	/// If checked, tween-based transition will be instant instead.
	/// </summary>

	public bool instantTween = false;

	/// <summary>
	/// Can the radio button option be 'none'?
	/// </summary>

	public bool optionCanBeNone = false;

	/// <summary>
	/// Callbacks triggered when the toggle's state changes.
	/// </summary>

	public List<EventDelegate> onChange = new List<EventDelegate>();

	public delegate bool Validate (bool choice);

	/// <summary>
	/// Want to validate the choice before committing the changes? Set this delegate.
	/// </summary>

	public Validate validator;

	/// <summary>
	/// Deprecated functionality. Use the 'group' option instead.
	/// </summary>

	[HideInInspector][SerializeField] UISprite checkSprite = null;
	[HideInInspector][SerializeField] Animation checkAnimation;
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string functionName = "OnActivate";
	[HideInInspector][SerializeField] bool startsChecked = false; // Use 'startsActive' instead

	[System.NonSerialized] int mIgnoreFrame = 0;

	bool mIsActive = true;
	bool mStarted = false;

	/// <summary>
	/// Whether the toggle is checked.
	/// </summary>

	public bool value
	{
		get
		{
			return mStarted ? mIsActive : startsActive;
		}
		set
		{
			if (!mStarted) startsActive = value;
			else if (group == 0 || value || optionCanBeNone || !mStarted) Set(value);
		}
	}

	/// <summary>
	/// Whether the collider is enabled and the widget can be interacted with.
	/// </summary>

	public bool isColliderEnabled
	{
		get
		{
			var c = GetComponent<Collider>();
			if (c != null) return c.enabled;
			var b = GetComponent<Collider2D>();
			return (b != null && b.enabled);
		}
	}

	[System.Obsolete("Use 'value' instead")]
	public bool isChecked { get { return value; } set { this.value = value; } }

	/// <summary>
	/// Return the first active toggle within the specified group.
	/// </summary>

	static public UIToggle GetActiveToggle (int group)
	{
		for (int i = 0; i < list.size; ++i)
		{
			var toggle = list.buffer[i];
			if (toggle != null && toggle.group == group && toggle.mIsActive)
				return toggle;
		}
		return null;
	}

	void OnEnable () { mIgnoreFrame = Time.frameCount; list.Add(this); }
	void OnDisable () { list.Remove(this); }

	/// <summary>
	/// Activate the initial state.
	/// </summary>

	public void Start ()
	{
		if (mStarted) return;

		if (startsChecked)
		{
			startsChecked = false;
			startsActive = true;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}

		// Auto-upgrade
		if (!Application.isPlaying)
		{
			if (checkSprite != null && activeSprite == null)
			{
				activeSprite = checkSprite;
				checkSprite = null;
			}

			if (checkAnimation != null && activeAnimation == null)
			{
				activeAnimation = checkAnimation;
				checkAnimation = null;
			}

			if (Application.isPlaying && activeSprite != null)
				activeSprite.alpha = invertSpriteState ? (startsActive ? 0f : 1f) : (startsActive ? 1f : 0f);

			if (EventDelegate.IsValid(onChange))
			{
				eventReceiver = null;
				functionName = null;
			}
		}
		else
		{
			mIsActive = !startsActive;
			mStarted = true;
			bool instant = instantTween;
			instantTween = true;
			Set(startsActive);
			instantTween = instant;
		}
	}

	/// <summary>
	/// Check or uncheck on click.
	/// </summary>

	public void OnClick ()
	{
		if (mIgnoreFrame == Time.frameCount) return;

		if (enabled && isColliderEnabled && UICamera.currentTouchID != -2)
		{
			mIgnoreFrame = Time.frameCount;
			value = !value;
		}
	}

	/// <summary>
	/// Fade out or fade in the active sprite and notify the OnChange event listener.
	/// If setting the initial value, call Start() first.
	/// </summary>

	public void Set (bool state, bool notify = true)
	{
		if (validator != null && !validator(state)) return;

		if (!mStarted)
		{
			mIsActive = state;
			startsActive = state;
			if (activeSprite != null)
				activeSprite.alpha = invertSpriteState ? (state ? 0f : 1f) : (state ? 1f : 0f);
		}
		else if (mIsActive != state)
		{
			// Uncheck all other toggles
			if (group != 0 && state)
			{
				for (int i = 0, imax = list.size; i < imax; )
				{
					var cb = list.buffer[i];
					if (cb != this && cb.group == group) cb.Set(false);

					if (list.size != imax)
					{
						imax = list.size;
						i = 0;
					}
					else ++i;
				}
			}

			// Remember the state
			mIsActive = state;

			// Tween the color of the active sprite
			if (activeSprite != null)
			{
				if (instantTween || !NGUITools.GetActive(this))
				{
					activeSprite.alpha = invertSpriteState ? (mIsActive ? 0f : 1f) : (mIsActive ? 1f : 0f);
				}
				else
				{
					TweenAlpha.Begin(activeSprite.gameObject, 0.15f, invertSpriteState ? (mIsActive ? 0f : 1f) : (mIsActive ? 1f : 0f));
				}
			}

			if (notify && current == null)
			{
				var tog = current;
				current = this;

				if (EventDelegate.IsValid(onChange))
				{
					EventDelegate.Execute(onChange);
				}
				else if (eventReceiver != null && !string.IsNullOrEmpty(functionName))
				{
					// Legacy functionality support (for backwards compatibility)
					eventReceiver.SendMessage(functionName, mIsActive, SendMessageOptions.DontRequireReceiver);
				}
				current = tog;
			}

			// Play the checkmark animation
			if (animator != null)
			{
				var aa = ActiveAnimation.Play(animator, null,
					state ? Direction.Forward : Direction.Reverse,
					EnableCondition.IgnoreDisabledState,
					DisableCondition.DoNotDisable);
				if (aa != null && (instantTween || !NGUITools.GetActive(this))) aa.Finish();
			}

			if (activeAnimation != null)
			{
				var aa = ActiveAnimation.Play(activeAnimation, null,
					state ? Direction.Forward : Direction.Reverse,
					EnableCondition.IgnoreDisabledState,
					DisableCondition.DoNotDisable);
				if (aa != null && (instantTween || !NGUITools.GetActive(this))) aa.Finish();
			}

			if (tween != null)
			{
				var isActive = NGUITools.GetActive(this);

				tween.Play(state);
				if (instantTween || !isActive) tween.tweenFactor = state ? 1f : 0f;

				if (tween.tweenGroup != 0)
				{
					var tws = gameObject.GetComponentsInChildren<UITweener>(true);

					for (int i = 0, imax = tws.Length; i < imax; ++i)
					{
						var t = tws[i];

						if (t != tween && t.tweenGroup == tween.tweenGroup)
						{
							t.Play(state);
							if (instantTween || !isActive) t.tweenFactor = state ? 1f : 0f;
						}
					}
				}
			}
		}
	}
}
