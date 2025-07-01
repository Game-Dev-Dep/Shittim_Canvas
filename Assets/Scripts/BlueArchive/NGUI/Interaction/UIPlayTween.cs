//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using AnimationOrTween;
using System.Collections.Generic;

/// <summary>
/// Play the specified tween on click.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/Interaction/Play Tween")]
public class UIPlayTween : MonoBehaviour
{
	static public UIPlayTween current;

	/// <summary>
	/// Target on which there is one or more tween.
	/// </summary>

	public GameObject tweenTarget;

	/// <summary>
	/// If there are multiple tweens, you can choose which ones get activated by changing their group.
	/// </summary>

	public int tweenGroup = 0;

	/// <summary>
	/// Which event will trigger the tween.
	/// </summary>

	public Trigger trigger = Trigger.OnClick;

	/// <summary>
	/// Direction to tween in.
	/// </summary>

	public Direction playDirection = Direction.Forward;

	/// <summary>
	/// Whether the tween will be reset to the start or end when activated. If not, it will continue from where it currently is.
	/// </summary>

	public bool resetOnPlay = false;

	/// <summary>
	/// Whether the tween will be reset to the start if it's disabled when activated.
	/// </summary>

	public bool resetIfDisabled = false;

	[Tooltip("If true, Play Tween will reset all associated tweens to their starting state at the very start, before activation triggers")]
	public bool setState = false;

	[Tooltip("Starting factor to assume, 0 being the start and 1 being the end"), Range(0f, 1f)]
	public float startState = 0f;

	/// <summary>
	/// What to do if the tweenTarget game object is currently disabled.
	/// </summary>

	public EnableCondition ifDisabledOnPlay = EnableCondition.DoNothing;

	/// <summary>
	/// What to do with the tweenTarget after the tween finishes.
	/// </summary>

	public DisableCondition disableWhenFinished = DisableCondition.DoNotDisable;

	/// <summary>
	/// Whether the tweens on the child game objects will be considered.
	/// </summary>

	public bool includeChildren = false;

	/// <summary>
	/// Event delegates called when the animation finishes.
	/// </summary>

	public List<EventDelegate> onFinished = new List<EventDelegate>();

	// Deprecated functionality, kept for backwards compatibility
	[HideInInspector][SerializeField] GameObject eventReceiver;
	[HideInInspector][SerializeField] string callWhenFinished;

	[System.NonSerialized] UITweener[] mTweens;
	[System.NonSerialized] bool mStarted = false;
	[System.NonSerialized] bool mIsActive = false;
	[System.NonSerialized] bool mActivated = false;
	
	/// <summary>
	/// Whether the tween is currently playing.
	/// </summary>

	public bool isActive { get { return mIsActive; } }

	void Awake ()
	{
		// Remove deprecated functionality if new one is used
		if (eventReceiver != null && EventDelegate.IsValid(onFinished))
		{
			eventReceiver = null;
			callWhenFinished = null;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}
	}

	void Start()
	{
		mStarted = true;

		if (tweenTarget == null)
		{
			tweenTarget = gameObject;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}

#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (setState)
		{
			var go = (tweenTarget == null) ? gameObject : tweenTarget;
			mTweens = includeChildren ? go.GetComponentsInChildren<UITweener>() : go.GetComponents<UITweener>();

			if (mTweens.Length == 0)
			{
				// No tweeners found -- should we disable the object?
				if (disableWhenFinished != DisableCondition.DoNotDisable)
					NGUITools.SetActive(tweenTarget, false);
			}
			else
			{
				var forward = (playDirection != Direction.Reverse);

				for (int i = 0, imax = mTweens.Length; i < imax; ++i)
				{
					var tw = mTweens[i];

					if (tw.tweenGroup == tweenGroup)
					{
						tw.Play(forward ? startState == 1f : startState != 1f);
						tw.Sample(forward ? startState : 1f - startState, true);
						tw.enabled = false;
					}
				}
			}
		}

		if (trigger == Trigger.OnEnable) Play(playDirection != Direction.Reverse);
	}

	void OnEnable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (mStarted) OnHover(UICamera.IsHighlighted(gameObject));

		if (mStarted && trigger == Trigger.OnEnable)
		{
			Play(playDirection != Direction.Reverse);
		}
		else if (UICamera.currentTouch != null)
		{
			if (trigger == Trigger.OnPress || trigger == Trigger.OnPressTrue)
				mActivated = (UICamera.currentTouch.pressed == gameObject);

			if (trigger == Trigger.OnHover || trigger == Trigger.OnHoverTrue)
				mActivated = (UICamera.currentTouch.current == gameObject);
		}

		var toggle = GetComponent<UIToggle>();
		if (toggle != null) EventDelegate.Add(toggle.onChange, OnToggle);
	}

	void OnDisable ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		var toggle = GetComponent<UIToggle>();
		if (toggle) EventDelegate.Remove(toggle.onChange, OnToggle);
	}

	void OnDragOver () { if (trigger == Trigger.OnHover) OnHover(true); }

	void OnHover (bool isOver)
	{
		if (enabled)
		{
			if (trigger == Trigger.OnHover ||
				(trigger == Trigger.OnHoverTrue && isOver) ||
				(trigger == Trigger.OnHoverFalse && !isOver))
			{
				if (isOver == mActivated) return;

				// Hover out action happened on a child object -- we want to maintain the hovered state
				if (!isOver && UICamera.hoveredObject != null && UICamera.hoveredObject.transform.IsChildOf(transform))
				{
					// Subscribe to a global hover listener so we can keep receiving hover notifications
					UICamera.onHover += CustomHoverListener;
					isOver = true;
					if (mActivated) return;
				}

				mActivated = isOver && (trigger == Trigger.OnHover);
				Play(isOver);
			}
		}
	}

	/// <summary>
	/// Wait for the hover event to happen outside the object's hierarchy before removing the hovered state.
	/// </summary>

	void CustomHoverListener (GameObject go, bool isOver)
	{
		if (!this) return;
		var myGo = gameObject;
		var hover = myGo && go && (go == myGo || go.transform.IsChildOf(transform));

		if (!hover)
		{
			OnHover(false);
			UICamera.onHover -= CustomHoverListener;
		}
	}

	void OnDragOut ()
	{
		if (enabled && mActivated)
		{
			mActivated = false;
			Play(false);
		}
	}

	void OnPress (bool isPressed)
	{
		if (enabled)
		{
			if (trigger == Trigger.OnPress ||
				(trigger == Trigger.OnPressTrue && isPressed) ||
				(trigger == Trigger.OnPressFalse && !isPressed))
			{
				mActivated = isPressed && (trigger == Trigger.OnPress);
				Play(isPressed);
			}
		}
	}

	void OnClick ()
	{
		if (enabled && trigger == Trigger.OnClick)
		{
			Play(true);
		}
	}

	void OnDoubleClick ()
	{
		if (enabled && trigger == Trigger.OnDoubleClick)
		{
			Play(true);
		}
	}

	void OnSelect (bool isSelected)
	{
		if (enabled)
		{
			if (trigger == Trigger.OnSelect ||
				(trigger == Trigger.OnSelectTrue && isSelected) ||
				(trigger == Trigger.OnSelectFalse && !isSelected))
			{
				mActivated = isSelected && (trigger == Trigger.OnSelect);
				Play(isSelected);
			}
		}
	}

	void OnToggle ()
	{
		if (!enabled || UIToggle.current == null) return;
		if (trigger == Trigger.OnActivate ||
			(trigger == Trigger.OnActivateTrue && UIToggle.current.value) ||
			(trigger == Trigger.OnActivateFalse && !UIToggle.current.value))
			Play(UIToggle.current.value);
	}

	void Update ()
	{
#if UNITY_EDITOR
		if (!Application.isPlaying) return;
#endif
		if (mTweens == null) return;
		
		var isFinished = true;
		
		for (int i = 0, imax = mTweens.Length; i < imax; ++i)
		{
			var tw = mTweens[i];
			if (tw.tweenGroup != tweenGroup) continue;

			if (tw.enabled)
			{
				isFinished = false;
				break;
			}
		}

		if (isFinished && disableWhenFinished != DisableCondition.DoNotDisable)
		{
			var properDirection = true;

			for (int i = 0, imax = mTweens.Length; i < imax; ++i)
			{
				var tw = mTweens[i];
				if (tw.tweenGroup != tweenGroup) continue;

				if ((int)tw.direction != (int)disableWhenFinished)
				{
					properDirection = false;
					break;
				}
			}

			if (isFinished)
			{
				if (properDirection) NGUITools.SetActive(tweenTarget, false);
				OnFinished();
				mTweens = null;
			}
		}
	}

	[ContextMenu("Stop")]
	public void Stop ()
	{
		if (mTweens != null) foreach(var tw in mTweens) tw.Finish();

		if (mIsActive)
		{
			mIsActive = false;
			OnFinished();
		}
	}

	/// <summary>
	/// Activate the tweeners.
	/// </summary>

	[ContextMenu("Play Forward")]
	public void PlayForward () { Play(true); }

	[ContextMenu("Play in reverse")]
	public void PlayReverse () { Play(false); }

	/// <summary>
	/// Activate the tweeners.
	/// </summary>

	public void Play (bool forward = true)
	{
		mIsActive = false;
		GameObject go = (tweenTarget == null) ? gameObject : tweenTarget;

		if (!NGUITools.GetActive(go))
		{
			// If the object is disabled, don't do anything
			if (ifDisabledOnPlay != EnableCondition.EnableThenPlay) return;

			// Enable the game object before tweening it
			NGUITools.SetActive(go, true);
		}

		// Gather the tweening components
		mTweens = includeChildren ? go.GetComponentsInChildren<UITweener>() : go.GetComponents<UITweener>();

		if (mTweens.Length == 0)
		{
			// No tweeners found -- should we disable the object?
			if (disableWhenFinished != DisableCondition.DoNotDisable)
				NGUITools.SetActive(tweenTarget, false);
		}
		else
		{
			bool activated = false;
			if (playDirection == Direction.Reverse) forward = !forward;

			// Run through all located tween components
			for (int i = 0, imax = mTweens.Length; i < imax; ++i)
			{
				var tw = mTweens[i];

				// If the tweener's group matches, we can work with it
				if (tw.tweenGroup == tweenGroup)
				{
					// Ensure that the game objects are enabled
					if (!activated && !NGUITools.GetActive(go))
					{
						activated = true;
						NGUITools.SetActive(go, true);
					}

					mIsActive = true;

					if (playDirection == Direction.Toggle)
					{
						tw.Toggle();
					}
					else if (resetOnPlay || (resetIfDisabled && !tw.enabled))
					{
						tw.Play(forward);
						tw.ResetToBeginning();
					}
					else tw.Play(forward);
				}
			}
		}

		// Can't have a start state after calling Play()
		setState = false;
	}

	/// <summary>
	/// Callback triggered when each tween executed by this script finishes.
	/// </summary>

	void OnFinished ()
	{
		if (current == null)
		{
			current = this;
			EventDelegate.Execute(onFinished);

			// Legacy functionality
			if (eventReceiver != null && !string.IsNullOrEmpty(callWhenFinished))
				eventReceiver.SendMessage(callWhenFinished, SendMessageOptions.DontRequireReceiver);

			eventReceiver = null;
			current = null;
		}
	}
}
