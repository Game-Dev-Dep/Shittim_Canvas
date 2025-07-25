//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright ├é┬® 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Spring-like motion -- the farther away the object is from the target, the stronger the pull.
/// </summary>

[AddComponentMenu("NGUI/Tween/Spring Position")]
public class SpringPosition : MonoBehaviour
{
	static public SpringPosition current;

	/// <summary>
	/// Target position to tween to.
	/// </summary>

	public Vector3 target = Vector3.zero;

	/// <summary>
	/// Strength of the spring. The higher the value, the faster the movement.
	/// </summary>

	public float strength = 10f;

	/// <summary>
	/// Is the calculation done in world space or local space?
	/// </summary>

	public bool worldSpace = false;

	/// <summary>
	/// Whether the time scale will be ignored. Generally UI components should set it to 'true'.
	/// </summary>

	public bool ignoreTimeScale = false;

	/// <summary>
	/// Whether the parent scroll view will be updated as the object moves.
	/// </summary>

	public bool updateScrollView = false;

	public delegate void OnFinished ();

	/// <summary>
	/// Delegate to trigger when the spring finishes.
	/// </summary>

	public OnFinished onFinished;

	// Deprecated functionality
	[SerializeField][HideInInspector] GameObject eventReceiver = null;
	[SerializeField][HideInInspector] public string callWhenFinished;

	Transform mTrans;
	float mThreshold = 0f;
	UIScrollView mSv;

	/// <summary>
	/// Cache the transform.
	/// </summary>

	void Start ()
	{
		mTrans = transform;
		if (updateScrollView) mSv = NGUITools.FindInParents<UIScrollView>(gameObject);
	}

	/// <summary>
	/// Advance toward the target position.
	/// </summary>

	void Update ()
	{
		float delta = ignoreTimeScale ? RealTime.deltaTime : Time.deltaTime;

		if (worldSpace)
		{
			if (mThreshold == 0f) mThreshold = Mathf.Min((target - mTrans.position).magnitude * 0.01f, 0.01f);
			mTrans.position = NGUIMath.SpringLerp(mTrans.position, target, strength, delta);

			if (mThreshold * mThreshold >= (target - mTrans.position).sqrMagnitude)
			{
				mTrans.position = target;
				NotifyListeners();
				enabled = false;
			}
		}
		else
		{
			if (mThreshold == 0f) mThreshold = Mathf.Min((target - mTrans.localPosition).magnitude * 0.01f, 0.01f);
			mTrans.localPosition = NGUIMath.SpringLerp(mTrans.localPosition, target, strength, delta);

			if (mThreshold * mThreshold >= (target - mTrans.localPosition).sqrMagnitude)
			{
				mTrans.localPosition = target;
				NotifyListeners();
				enabled = false;
			}
		}

		// Ensure that the scroll bars remain in sync
		if (mSv != null) mSv.QueueUpdateScrollbars();
	}

	/// <summary>
	/// Immediately finish the animation.
	/// </summary>

	public void Finish ()
	{
		if (enabled)
		{
			if (worldSpace) transform.position = target;
			else transform.localPosition = target;

			NotifyListeners();
			enabled = false;

			// Ensure that the scroll bars remain in sync
			if (mSv != null) mSv.QueueUpdateScrollbars();
		}
	}

	/// <summary>
	/// Notify all finished event listeners.
	/// </summary>

	void NotifyListeners ()
	{
		current = this;

		if (onFinished != null) onFinished();

		if (eventReceiver != null && !string.IsNullOrEmpty(callWhenFinished))
			eventReceiver.SendMessage(callWhenFinished, this, SendMessageOptions.DontRequireReceiver);

		current = null;
	}

	/// <summary>
	/// Start the tweening process.
	/// </summary>

	static public SpringPosition Begin (GameObject go, Vector3 pos, float strength)
	{
		var sp = go.GetComponent<SpringPosition>();
		if (sp == null) sp = go.AddComponent<SpringPosition>();
		sp.target = pos;
		sp.strength = strength;
		sp.onFinished = null;
		if (!sp.enabled) sp.enabled = true;
		return sp;
	}
}
