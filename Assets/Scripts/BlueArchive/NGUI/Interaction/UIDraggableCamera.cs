//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Works together with UIDragCamera script, allowing you to drag a secondary camera while keeping it constrained to a certain area.
/// </summary>

[RequireComponent(typeof(Camera))]
[AddComponentMenu("NGUI/Interaction/Draggable Camera")]
public class UIDraggableCamera : MonoBehaviour
{
	[Tooltip("Root object that will be used for drag-limiting bounds.")]
	public Transform rootForBounds;

	[Tooltip("Scale value applied to the drag delta. Set X or Y to 0 to disallow dragging in that direction.")]
	public Vector2 scale = Vector2.one;

	[Tooltip("Effect the scroll wheel will have on the momentum.")]
	public float scrollWheelFactor = 0f;

	[Tooltip("If specified to a non-zero range, the scroll wheel's functionality will be changed to altering the camera's orthographic size instead")]
	public Vector2 scrollZoomRange;

	[Tooltip("Effect to apply when dragging.")]
	public UIDragObject.DragEffect dragEffect = UIDragObject.DragEffect.MomentumAndSpring;

	[Tooltip("Whether the drag operation will be started smoothly, or if if it will be precise (but may have a noticeable jump).")]
	public bool smoothDragStart = true;

	[Tooltip("How much momentum gets applied when the press is released after dragging.")]
	public float momentumAmount = 35f;

	[Tooltip("Additional padding to apply to the calculated bounds, in case you want to limit or expand the region")]
	public Vector2 padding = Vector2.zero;

	[Tooltip("If set, padding will be multiplied by the camera's orthographic size")]
	public bool paddingIsRelative = true;

	[System.NonSerialized] Camera mCam;
	[System.NonSerialized] Transform mTrans;
	[System.NonSerialized] bool mPressed = false;
	[System.NonSerialized] Vector2 mMomentum = Vector2.zero;
	[System.NonSerialized] Bounds mBounds;
	[System.NonSerialized] float mScroll = 0f;
	[System.NonSerialized] bool mDragStarted = false;

	/// <summary>
	/// Camera this script is working with.
	/// </summary>

	public Camera cachedCamera { get { return mCam; } }

	/// <summary>
	/// Current momentum, exposed just in case it's needed.
	/// </summary>

	public Vector2 currentMomentum { get { return mMomentum; } set { mMomentum = value; } }

	/// <summary>
	/// Zoom level if scroll zoom range is used.
	/// </summary>

	public float zoom
	{
		get
		{
			if (mCam == null || scrollZoomRange.x == 0f || scrollZoomRange.y == 0f) return 1f;
			return Mathf.InverseLerp(scrollZoomRange.x, scrollZoomRange.y, mCam.orthographicSize);
		}
	}

	/// <summary>
	/// Cache the root.
	/// </summary>

	void Start ()
	{
		mCam = GetComponent<Camera>();
		mTrans = transform;

		if (rootForBounds == null)
		{
			Debug.LogError(NGUITools.GetHierarchy(gameObject) + " needs the 'Root For Bounds' parameter to be set", this);
			enabled = false;
		}
	}

	/// <summary>
	/// Calculate the offset needed to be constrained within the panel's bounds.
	/// </summary>

	Vector3 CalculateConstrainOffset ()
	{
		if (rootForBounds == null) return Vector3.zero;

		var rect = mCam.rect;
		var sw = Screen.width;
		var sh = Screen.height;
		var bottomLeft = new Vector3(rect.xMin * sw, rect.yMin * sh, 0f);
		var topRight = new Vector3(rect.xMax * sw, rect.yMax * sh, 0f);

		bottomLeft = mCam.ScreenToWorldPoint(bottomLeft);
		topRight = mCam.ScreenToWorldPoint(topRight);

		var padding = this.padding;

		if (paddingIsRelative)
		{
			var os = mCam.orthographicSize * 2f;
			padding.x *= os * mCam.aspect;
			padding.y *= os;
		}

		var minRect = new Vector2(mBounds.min.x - padding.x, mBounds.min.y - padding.y);
		var maxRect = new Vector2(mBounds.max.x + padding.x, mBounds.max.y + padding.y);

		return NGUIMath.ConstrainRect(minRect, maxRect, bottomLeft, topRight);
	}

	/// <summary>
	/// Constrain the current camera's position to be within the viewable area's bounds.
	/// </summary>

	public bool ConstrainToBounds (bool immediate)
	{
		if (mTrans != null && rootForBounds != null)
		{
			var offset = CalculateConstrainOffset();

			if (offset.sqrMagnitude > 0f)
			{
				if (immediate)
				{
					mTrans.position -= offset;
				}
				else
				{
					var sp = SpringPosition.Begin(gameObject, mTrans.position - offset, 13f);
					sp.ignoreTimeScale = true;
					sp.worldSpace = true;
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Calculate the bounds of all widgets under this game object.
	/// </summary>

	public void Press (bool isPressed)
	{
		if (isPressed) mDragStarted = false;

		if (rootForBounds != null)
		{
			mPressed = isPressed;

			if (isPressed)
			{
				// Update the bounds
				mBounds = NGUIMath.CalculateAbsoluteWidgetBounds(rootForBounds);

				// Remove all momentum on press
				mMomentum = Vector2.zero;
				if (scrollZoomRange.x == 0f) mScroll = 0f;

				// Disable the spring movement
				var sp = GetComponent<SpringPosition>();
				if (sp != null) sp.enabled = false;
			}
			else if (dragEffect == UIDragObject.DragEffect.MomentumAndSpring)
			{
				ConstrainToBounds(false);
			}
		}
	}

	/// <summary>
	/// Drag event receiver.
	/// </summary>

	public void Drag (Vector2 delta)
	{
		// Prevents the initial jump when the drag threshold gets passed
		if (smoothDragStart && !mDragStarted)
		{
			mDragStarted = true;
			return;
		}

		UICamera.currentTouch.clickNotification = UICamera.ClickNotification.BasedOnDelta;

		// I think this is no longer needed? Needs to be double-checked...
		//if (mRoot != null) delta *= mRoot.pixelSizeAdjustment;

		// Dragging should be relative to the orthographic size. If the size doesn't match the root's expected scale,
		// meaning the camera is not pixel-perfect, then additional adjustments must be made in order for dragging to match the mouse movement.
		var offset = Vector2.Scale(delta, -scale);
		var scaleY = mTrans.lossyScale.y * Screen.height;
		var camSize = mCam.orthographicSize * 2f;
		offset *= camSize / scaleY;

		// Move the camera
		mTrans.localPosition += (Vector3)offset;

		// Adjust the momentum
		mMomentum = Vector2.Lerp(mMomentum, mMomentum + offset * (0.01f * momentumAmount), 0.67f);

		// Constrain the UI to the bounds, and if done so, eliminate the momentum
		if (dragEffect != UIDragObject.DragEffect.MomentumAndSpring && ConstrainToBounds(true))
		{
			mMomentum = Vector2.zero;
			if (scrollZoomRange.x == 0f) mScroll = 0f;
		}
	}

	/// <summary>
	/// If the object should support the scroll wheel, do it.
	/// </summary>

	public void Scroll (float delta)
	{
		if (enabled && NGUITools.GetActive(gameObject))
		{
			if (Mathf.Sign(mScroll) != Mathf.Sign(delta)) mScroll = 0f;
			mScroll += delta * scrollWheelFactor;
		}
	}

	/// <summary>
	/// Apply the dragging momentum.
	/// </summary>

	void Update ()
	{
		float delta = RealTime.deltaTime;

		if (mPressed)
		{
			// Disable the spring movement
			var sp = GetComponent<SpringPosition>();
			if (sp != null) sp.enabled = false;
			if (scrollZoomRange.x == 0f) mScroll = 0f;
		}
		else
		{
			if (scrollZoomRange.x == 0f)
			{
				mMomentum += scale * (mScroll * 20f);
				mScroll = NGUIMath.SpringLerp(mScroll, 0f, 20f, delta);
			}
			else if (mCam.orthographic)
			{
				mCam.orthographicSize = Mathf.Clamp(NGUIMath.SpringLerp(mCam.orthographicSize, mCam.orthographicSize - mScroll, 1f, delta), scrollZoomRange.x, scrollZoomRange.y);
				mScroll = NGUIMath.SpringLerp(mScroll, 0f, 5f, delta);
				if (Mathf.Abs(mScroll) < 0.001f) mScroll = 0f;
			}

			if (mMomentum.magnitude > 0.01f || mScroll != 0f)
			{
				// Apply the momentum
				mTrans.localPosition += (Vector3)NGUIMath.SpringDampen(ref mMomentum, 9f, delta);
				mBounds = NGUIMath.CalculateAbsoluteWidgetBounds(rootForBounds);

				if (!ConstrainToBounds(dragEffect == UIDragObject.DragEffect.None))
				{
					var sp = GetComponent<SpringPosition>();
					if (sp != null) sp.enabled = false;
				}
				return;
			}
			else if (scrollZoomRange.x == 0f) mScroll = 0f;
		}

		// Dampen the momentum
		NGUIMath.SpringDampen(ref mMomentum, 9f, delta);
	}
}
