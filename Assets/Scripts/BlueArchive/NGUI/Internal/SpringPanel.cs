//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Similar to SpringPosition, but also moves the panel's clipping. Works in local coordinates.
/// </summary>

[RequireComponent(typeof(UIPanel))]
[AddComponentMenu("NGUI/Internal/Spring Panel")]
public class SpringPanel : MonoBehaviour
{
	static public SpringPanel current;

	/// <summary>
	/// Target position to spring the panel to.
	/// </summary>

	public Vector3 target = Vector3.zero;

	/// <summary>
	/// Strength of the spring. The higher the value, the faster the movement.
	/// </summary>

	public float strength = 10f;

	public delegate void OnFinished ();

	/// <summary>
	/// Delegate function to call when the operation finishes.
	/// </summary>

	public OnFinished onFinished;

	[System.NonSerialized] UIPanel mPanel;
	[System.NonSerialized] Transform mTrans;
	[System.NonSerialized] UIScrollView mDrag;
	[System.NonSerialized] float mDelta = 0f;

	/// <summary>
	/// Cache the transform.
	/// </summary>

	void Start ()
	{
		mPanel = GetComponent<UIPanel>();
		mDrag = GetComponent<UIScrollView>();
		mTrans = transform;
	}

	/// <summary>
	/// Advance toward the target position.
	/// </summary>

	void Update () { AdvanceTowardsPosition(); }

	/// <summary>
	/// Advance toward the target position.
	/// </summary>

	protected virtual void AdvanceTowardsPosition ()
	{
		mDelta += RealTime.deltaTime;

		var trigger = false;
		var before = mTrans.localPosition;
		var after = NGUIMath.SpringLerp(before, target, strength, mDelta);

		if ((before - target).sqrMagnitude < 0.01f)
		{
			after = target;
			enabled = false;
			trigger = true;
			mDelta = 0f;
		}
		else
		{
			after.x = Mathf.Round(after.x);
			after.y = Mathf.Round(after.y);
			after.z = Mathf.Round(after.z);

			if ((after - before).sqrMagnitude < 0.01f) return;
			else mDelta = 0f;
		}

		mTrans.localPosition = after;

		var offset = after - before;
		var cr = mPanel.clipOffset;
		cr.x -= offset.x;
		cr.y -= offset.y;
		mPanel.clipOffset = cr;

		if (mDrag != null) mDrag.UpdateScrollbars(false);

		if (trigger && onFinished != null)
		{
			current = this;
			onFinished();
			current = null;
		}
	}

	/// <summary>
	/// Start the tweening process.
	/// </summary>

	static public SpringPanel Begin (GameObject go, Vector3 pos, float strength)
	{
		var sp = go.GetComponent<SpringPanel>();
		if (sp == null) sp = go.AddComponent<SpringPanel>();
		sp.target = pos;
		sp.strength = strength;
		sp.onFinished = null;
		sp.enabled = true;
		return sp;
	}

	/// <summary>
	/// Stop the tweening process.
	/// </summary>

	static public SpringPanel Stop (GameObject go)
	{
		var sp = go.GetComponent<SpringPanel>();

		if (sp != null && sp.enabled)
		{
			if (sp.onFinished != null) sp.onFinished();
			sp.enabled = false;
		}
		return sp;
	}
}
