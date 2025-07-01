//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Ever wanted to be able to auto-center on an object within a draggable panel?
/// Attach this script to the container that has the objects to center on as its children.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Center Scroll View on Child")]
public class UICenterOnChild : MonoBehaviour
{
	public delegate void OnCenterCallback (GameObject centeredObject);

	/// <summary>
	/// The strength of the spring.
	/// </summary>

	public float springStrength = 8f;

	/// <summary>
	/// If set to something above zero, it will be possible to move to the next page after dragging past the specified threshold.
	/// </summary>

	public float nextPageThreshold = 0f;

	/// <summary>
	/// Callback to be triggered when the centering operation completes.
	/// </summary>

	public SpringPanel.OnFinished onFinished;

	/// <summary>
	/// Callback triggered whenever the script begins centering on a new child object.
	/// </summary>

	public OnCenterCallback onCenter;

	UIScrollView mScrollView;
	GameObject mCenteredObject;
	int mCenteredIndex = 0;
	int mTotalCount = 0;

	/// <summary>
	/// Game object that the draggable panel is currently centered on.
	/// </summary>

	public GameObject centeredObject { get { return mCenteredObject; } }

	/// <summary>
	/// Index of the centered object.
	/// </summary>

	public int centeredIndex { get { return mCenteredIndex; } }

	/// <summary>
	/// Number of children that can be centered on.
	/// </summary>

	public int totalChildren { get { return mTotalCount; } }

	void Start () { Recenter(); }
	
	void OnEnable () { if (mScrollView) { mScrollView.centerOnChild = this; Recenter(); } }
	
	void OnDisable () { if (mScrollView) mScrollView.centerOnChild = null; }
	
	void OnDragFinished () { if (enabled) Recenter(); }

	/// <summary>
	/// Ensure that the threshold is always positive.
	/// </summary>

	void OnValidate () { nextPageThreshold = Mathf.Abs(nextPageThreshold); }

	/// <summary>
	/// Recenter the draggable list on the center-most child.
	/// </summary>

	[ContextMenu("Execute")]
	public void Recenter () { Recenter(0, 0); }

	/// <summary>
	/// Recenter the scroll view on the closest child, with the desired offset.
	/// </summary>

	public void Recenter (int offsetX, int offsetY)
	{
		if (mScrollView == null)
		{
			mScrollView = NGUITools.FindInParents<UIScrollView>(gameObject);

			if (mScrollView == null)
			{
				Debug.LogWarning(GetType() + " requires " + typeof(UIScrollView) + " on a parent object in order to work", this);
				enabled = false;
				return;
			}
			else
			{
				if (mScrollView)
				{
					mScrollView.centerOnChild = this;
					//mScrollView.onDragFinished += OnDragFinished;
				}

				if (mScrollView.horizontalScrollBar != null)
					mScrollView.horizontalScrollBar.onDragFinished += OnDragFinished;

				if (mScrollView.verticalScrollBar != null)
					mScrollView.verticalScrollBar.onDragFinished += OnDragFinished;
			}
		}
		if (mScrollView.panel == null) return;

		var trans = transform;
		if (trans.childCount == 0) return;

		// Calculate the panel's center in world coordinates
		var corners = mScrollView.panel.worldCorners;
		var panelCenter = (corners[2] + corners[0]) * 0.5f;

		// Offset this value by the momentum
		var momentum = mScrollView.currentMomentum;
		var moveDelta = NGUIMath.SpringDampen(ref momentum, mScrollView.dampenStrength, 0.02f); // Magic number based on what "feels right"
		var pickingPoint = panelCenter - moveDelta;

		var min = float.MaxValue;
		Transform closest = null;
		var index = 0;
		mCenteredIndex = 0;
		mTotalCount = 0;

		var grid = GetComponent<UIGrid>();
		List<Transform> list = null;

		// Determine the closest child
		if (grid != null)
		{
			list = grid.GetChildList();

			if (offsetX != 0 || offsetY != 0) pickingPoint += trans.TransformDirection(offsetX * grid.cellWidth, offsetY * grid.cellHeight, 0f);

			for (int i = 0, imax = list.Count; i < imax; ++i)
			{
				var t = list[i];
				if (!t.gameObject.activeInHierarchy) continue;

				var sqrDist = Vector3.SqrMagnitude(t.position - pickingPoint);

				if (sqrDist < min)
				{
					min = sqrDist;
					closest = t;
					index = i;
					mCenteredIndex = mTotalCount;
				}

				++mTotalCount;
			}
		}
		else
		{
			if ((offsetX != 0 || offsetY != 0) && trans.childCount > 1)
			{
				var diff = trans.GetChild(1).position - trans.GetChild(0).position;
				pickingPoint += trans.TransformDirection(offsetX * Mathf.Abs(diff.x), offsetY * Mathf.Abs(diff.y), 0f);
			}

			// Note that the children should be in order (left to right for horizontal movement).
			// If they aren't in order, centering won't work properly, and children may get skipped while moving around.
			// Using a grid forces this kind of positioning, but without it you have to make sure that the order is proper yourself.

			for (int i = 0, imax = trans.childCount; i < imax; ++i)
			{
				var t = trans.GetChild(i);
				if (!t.gameObject.activeInHierarchy) continue;

				var sqrDist = Vector3.SqrMagnitude(t.position - pickingPoint);

				if (sqrDist < min)
				{
					min = sqrDist;
					closest = t;
					index = i;
					mCenteredIndex = mTotalCount;
				}

				++mTotalCount;
			}
		}

		// If we have a touch in progress and the next page threshold set
		if (nextPageThreshold > 0f && UICamera.currentTouch != null)
		{
			// If we're still on the same object
			if (mCenteredObject != null && mCenteredObject.transform == (list != null ? list[index] : trans.GetChild(index)))
			{
				var totalDelta = UICamera.currentTouch.totalDelta;
				totalDelta = transform.rotation * totalDelta;

				float delta = 0f;

				switch (mScrollView.movement)
				{
					case UIScrollView.Movement.Horizontal:
					{
						delta = totalDelta.x;
						break;
					}
					case UIScrollView.Movement.Vertical:
					{
						delta = -totalDelta.y;
						break;
					}
					default:
					{
						delta = totalDelta.magnitude;
						break;
					}
				}

				if (Mathf.Abs(delta) > nextPageThreshold)
				{
					if (delta > nextPageThreshold)
					{
						// Next page
						if (list != null)
						{
							if (mCenteredIndex > 0)
							{
								closest = list[--mCenteredIndex];
							}
							else closest = (GetComponent<UIWrapContent>() == null) ? list[mCenteredIndex = 0] : list[mCenteredIndex = list.Count - 1];
						}
						else if (mCenteredIndex > 0)
						{
							closest = trans.GetChild(--mCenteredIndex);
						}
						else closest = (GetComponent<UIWrapContent>() == null) ? trans.GetChild(mCenteredIndex = 0) : trans.GetChild(mCenteredIndex = trans.childCount - 1);
					}
					else if (delta < -nextPageThreshold)
					{
						// Previous page
						if (list != null)
						{
							if (mCenteredIndex < list.Count - 1)
							{
								closest = list[++mCenteredIndex];
							}
							else closest = (GetComponent<UIWrapContent>() == null) ? list[list.Count - 1] : list[0];
						}
						else if (mCenteredIndex < trans.childCount - 1)
						{
							closest = trans.GetChild(++mCenteredIndex);
						}
						else closest = (GetComponent<UIWrapContent>() == null) ? trans.GetChild(mCenteredIndex = trans.childCount - 1) : trans.GetChild(mCenteredIndex = 0);
					}
				}
			}
		}

		CenterOn(closest, panelCenter);
	}

	/// <summary>
	/// Center the panel on the specified target.
	/// </summary>

	void CenterOn (Transform target, Vector3 panelCenter)
	{
		if (target != null && mScrollView != null && mScrollView.panel != null)
		{
			var panelTrans = mScrollView.panel.cachedTransform;
			mCenteredObject = target.gameObject;

			// Figure out the difference between the chosen child and the panel's center in local coordinates
			var cp = panelTrans.InverseTransformPoint(target.position);
			var cc = panelTrans.InverseTransformPoint(panelCenter);
			var localOffset = cp - cc;

			// Offset shouldn't occur if blocked
			if (!mScrollView.canMoveHorizontally) localOffset.x = 0f;
			if (!mScrollView.canMoveVertically) localOffset.y = 0f;
			localOffset.z = 0f;

			// Spring the panel to this calculated position
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				panelTrans.localPosition = panelTrans.localPosition - localOffset;

				var co = mScrollView.panel.clipOffset;
				co.x += localOffset.x;
				co.y += localOffset.y;
				mScrollView.panel.clipOffset = co;
			}
			else
#endif
			{
				var pos = panelTrans.localPosition - localOffset;
				pos.x = Mathf.Round(pos.x);
				pos.y = Mathf.Round(pos.y);
				pos.z = Mathf.Round(pos.z);
				SpringPanel.Begin(mScrollView.panel.cachedGameObject, pos, springStrength).onFinished = onFinished;
			}
		}
		else mCenteredObject = null;

		// Notify the listener
		if (onCenter != null) onCenter(mCenteredObject);
	}

	/// <summary>
	/// Center the panel on the specified target.
	/// </summary>

	public void CenterOn (Transform target)
	{
		if (mScrollView != null && mScrollView.panel != null)
		{
			var corners = mScrollView.panel.worldCorners;
			var panelCenter = (corners[2] + corners[0]) * 0.5f;
			CenterOn(target, panelCenter);
		}
	}

	public void MoveLeft () { Recenter(-1, 0); }
	public void MoveRight () { Recenter(1, 0); }
	public void MoveUp () { Recenter(0, 1); }
	public void MoveDown () { Recenter(0, -1); }
}
