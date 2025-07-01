//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// UIDragDropItem is a base script for your own Drag & Drop operations.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Drag and Drop Item")]
public class UIDragDropItem : MonoBehaviour
{
	[DoNotObfuscateNGUI] public enum Restriction
	{
		None,
		Horizontal,
		Vertical,
		PressAndHold,
	}

	[Tooltip("What kind of restriction is applied to the drag & drop logic before dragging is made possible.")]
	public Restriction restriction = Restriction.None;

	[Tooltip("By default, dragging only happens while holding the mouse button / touch. If desired, you can opt to use a click-based approach instead. Note that this only works with a mouse.")]
	public bool clickToDrag = false;

	[Tooltip("Whether a copy of the item will be dragged instead of the item itself.")]
	public bool cloneOnDrag = false;

	[Tooltip("Whether this drag and drop item can be interacted with. If not, only tooltips will work.")]
	public bool interactable = true;

	[Tooltip("How long the user has to press on an item before the drag action activates.")]
	[HideInInspector] public float pressAndHoldDelay = 1f;

	#region Common functionality

	[NonSerialized] protected Transform mTrans;
	[NonSerialized] protected Transform mParent;
	[NonSerialized] protected Collider mCollider;
	[NonSerialized] protected Collider2D mCollider2D;
	[NonSerialized] protected UIButton mButton;
	[NonSerialized] protected UIRoot mRoot;
	[NonSerialized] protected UIGrid mGrid;
	[NonSerialized] protected UITable mTable;
	[NonSerialized] protected float mDragStartTime = 0f;
	[NonSerialized] protected UIDragScrollView mDragScrollView = null;
	[NonSerialized] protected bool mPressed = false;
	[NonSerialized] protected bool mDragging = false;
	[NonSerialized] protected UICamera.MouseOrTouch mTouch;

	/// <summary>
	/// List of items that are currently being dragged.
	/// </summary>

	[NonSerialized] static public List<UIDragDropItem> draggedItems = new List<UIDragDropItem>();

	/// <summary>
	/// Whether this object is currently being dragged.
	/// </summary>

	static public bool IsDragged (GameObject go)
	{
		for (int i = 0; i < draggedItems.Count; ++i)
		{
			var drag = draggedItems[i];

			if (!drag)
			{
				Debug.LogWarning("UIDragDropItem has been destroyed without calling its OnDragDropEnd() function");
				draggedItems.RemoveAt(i--);
			}
			else if (drag.gameObject == go) return true;
		}
		return false;
	}

	protected virtual void Awake ()
	{
		mTrans = transform;
#if UNITY_4_3 || UNITY_4_5 || UNITY_4_6 || UNITY_4_7
		mCollider = collider;
		mCollider2D = collider2D;
#else
		mCollider = gameObject.GetComponent<Collider>();
		mCollider2D = gameObject.GetComponent<Collider2D>();
#endif
	}

	protected virtual void OnEnable () { }

	protected virtual void OnDisable ()
	{
		if (mDragging)
		{
			StopDragging(null);
			UICamera.onPress -= OnGlobalPress;
			UICamera.onClick -= OnGlobalClick;
			UICamera.onMouseMove -= OnDrag;
		}
	}

	/// <summary>
	/// Cache the transform.
	/// </summary>

	protected virtual void Start ()
	{
		mButton = GetComponent<UIButton>();
		mDragScrollView = GetComponent<UIDragScrollView>();
	}

	[NonSerialized] static int mIgnoreClick = 0;

	/// <summary>
	/// Record the time the item was pressed on.
	/// </summary>

	protected virtual void OnPress (bool isPressed)
	{
		if (!interactable || UICamera.currentTouchID == -2 || UICamera.currentTouchID == -3) return;

		if (isPressed)
		{
			if (!mPressed)
			{
				mTouch = UICamera.currentTouch;
				mDragStartTime = RealTime.time + pressAndHoldDelay;
				mPressed = true;
			}
		}
		else if (mPressed && mTouch == UICamera.currentTouch)
		{
			mPressed = false;
			if (!mDragging || !clickToDrag) mTouch = null;
		}
	}

	protected virtual void OnClick ()
	{
		if (!interactable || mIgnoreClick == Time.frameCount) return;

		if (clickToDrag && !mDragging && UICamera.currentTouchID == -1 && draggedItems.Count == 0)
		{
			mTouch = UICamera.currentTouch;

			var item = StartDragging();

			if (clickToDrag && item != null)
			{
				UICamera.onMouseMove += item.OnDrag;
				UICamera.onPress += item.OnGlobalPress;
				UICamera.onClick += item.OnGlobalClick;
			}
		}
	}

	protected void OnGlobalPress (GameObject go, bool state)
	{
		if (state && UICamera.currentTouchID != -1)
		{
			mIgnoreClick = Time.frameCount;
			StopDragging(null);
			UICamera.onPress -= OnGlobalPress;
			UICamera.onClick -= OnGlobalClick;
			UICamera.onMouseMove -= OnDrag;
		}
	}

	protected void OnGlobalClick (GameObject go)
	{
		mIgnoreClick = Time.frameCount;

		if (UICamera.currentTouchID == -1) StopDragging(go);
		else StopDragging(null);

		UICamera.onPress -= OnGlobalPress;
		UICamera.onClick -= OnGlobalClick;
		UICamera.onMouseMove -= OnDrag;
	}

	/// <summary>
	/// Start the dragging operation after the item was held for a while.
	/// </summary>

	protected virtual void Update ()
	{
		if (restriction == Restriction.PressAndHold)
		{
			if (mPressed && !mDragging && mDragStartTime < RealTime.time)
				StartDragging();
		}
	}

	/// <summary>
	/// Start the dragging operation.
	/// </summary>

	protected virtual void OnDragStart ()
	{
		if (!interactable) return;
		if (!enabled || mTouch != UICamera.currentTouch) return;

		// If we have a restriction, check to see if its condition has been met first
		if (restriction != Restriction.None)
		{
			if (restriction == Restriction.Horizontal)
			{
				Vector2 delta = mTouch.totalDelta;
				if (Mathf.Abs(delta.x) < Mathf.Abs(delta.y)) return;
			}
			else if (restriction == Restriction.Vertical)
			{
				Vector2 delta = mTouch.totalDelta;
				if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) return;
			}
			else if (restriction == Restriction.PressAndHold)
			{
				// Checked in Update instead
				return;
			}
		}
		StartDragging();
	}

	/// <summary>
	/// Start the dragging operation.
	/// </summary>

	public virtual UIDragDropItem StartDragging ()
	{
		if (!interactable || !transform || !transform.parent) return null;

		if (!mDragging)
		{
			if (cloneOnDrag)
			{
				mPressed = false;
				var clone = transform.parent.gameObject.AddChild(gameObject);
				clone.transform.localPosition = transform.localPosition;
				clone.transform.localRotation = transform.localRotation;
				clone.transform.localScale = transform.localScale;

				var bc = clone.GetComponent<UIButtonColor>();
				if (bc != null) bc.defaultColor = GetComponent<UIButtonColor>().defaultColor;

				if (mTouch != null && mTouch.pressed == gameObject)
				{
					mTouch.current = clone;
					mTouch.pressed = clone;
					mTouch.dragged = clone;
					mTouch.last = clone;
				}

				var item = clone.GetComponent<UIDragDropItem>();
				item.mTouch = mTouch;
				item.mPressed = true;
				item.mDragging = true;
				item.Start();
				item.OnClone(gameObject);
				item.OnDragDropStart();

				if (UICamera.currentTouch == null)
					UICamera.currentTouch = mTouch;

				mTouch = null;

				UICamera.Notify(gameObject, "OnPress", false);
				UICamera.Notify(gameObject, "OnHover", false);
				return item;
			}
			else
			{
				mDragging = true;
				OnDragDropStart();
				return this;
			}
		}
		return null;
	}

	/// <summary>
	/// Called on the cloned object when it was duplicated.
	/// </summary>

	protected virtual void OnClone (GameObject original)
	{
		var o = original.GetComponent<UIDragDropItem>();
		mParent = o.mParent;
		mGrid = o.mGrid;
		mTable = o.mTable;
	}

	/// <summary>
	/// Perform the dragging.
	/// </summary>

	protected virtual void OnDrag (Vector2 delta)
	{
		if (!interactable) return;
		if (!mDragging || !enabled || mTouch != UICamera.currentTouch) return;
		if (mRoot != null) OnDragDropMove(delta * mRoot.pixelSizeAdjustment);
		else OnDragDropMove(delta);
	}

	/// <summary>
	/// Notification sent when the drag event has ended.
	/// </summary>

	protected virtual void OnDragEnd ()
	{
		if (!interactable) return;
		if (!enabled || mTouch != UICamera.currentTouch) return;
		StopDragging(UICamera.lastHit.collider != null ? UICamera.lastHit.collider.gameObject : null);
	}

	/// <summary>
	/// Drop the dragged item.
	/// </summary>

	public void StopDragging (GameObject go = null)
	{
		if (mDragging)
		{
			mDragging = false;
			OnDragDropRelease(go);
		}
	}

	#endregion

	/// <summary>
	/// Perform any logic related to starting the drag & drop operation.
	/// </summary>

	protected virtual void OnDragDropStart ()
	{
		if (!draggedItems.Contains(this)) draggedItems.Add(this);

		// Automatically disable the scroll view
		if (mDragScrollView != null) mDragScrollView.enabled = false;

		// Disable the collider so that it doesn't intercept events
		if (mButton != null) mButton.isEnabled = false;
		else if (mCollider != null) mCollider.enabled = false;
		else if (mCollider2D != null) mCollider2D.enabled = false;

		mParent = mTrans.parent;
		mRoot = NGUITools.FindInParents<UIRoot>(mParent);
		mGrid = NGUITools.FindInParents<UIGrid>(mParent);
		mTable = NGUITools.FindInParents<UITable>(mParent);

		// Re-parent the item
		if (UIDragDropRoot.root != null)
			mTrans.parent = UIDragDropRoot.root;

		var pos = mTrans.localPosition;
		pos.z = 0f;
		mTrans.localPosition = pos;

		var tp = GetComponent<TweenPosition>();
		if (tp != null) tp.enabled = false;

		var sp = GetComponent<SpringPosition>();
		if (sp != null) sp.enabled = false;

		// Notify the widgets that the parent has changed
		NGUITools.MarkParentAsChanged(gameObject);

		if (mTable != null) mTable.repositionNow = true;
		if (mGrid != null) mGrid.repositionNow = true;
	}

	/// <summary>
	/// Adjust the dragged object's position.
	/// </summary>

	protected virtual void OnDragDropMove (Vector2 delta)
	{
		if (mParent != null)
		{
			var v3 = mTrans.InverseTransformDirection((Vector3)delta);
			if (mRoot != null) v3 = mRoot.transform.TransformDirection(v3);
			mTrans.localPosition += v3;
		}
	}

	/// <summary>
	/// Drop the item onto the specified object.
	/// </summary>

	protected virtual void OnDragDropRelease (GameObject surface)
	{
		if (!cloneOnDrag)
		{
			// Clear the reference to the scroll view since it might be in another scroll view now
			var drags = GetComponentsInChildren<UIDragScrollView>();
			foreach (var d in drags) d.scrollView = null;

			// Re-enable the collider
			if (mButton != null) mButton.isEnabled = true;
			else if (mCollider != null) mCollider.enabled = true;
			else if (mCollider2D != null) mCollider2D.enabled = true;

			// Is there a droppable container?
			var container = surface ? NGUITools.FindInParents<UIDragDropContainer>(surface) : null;

			if (container != null)
			{
				// Container found -- parent this object to the container
				mTrans.parent = (container.reparentTarget != null) ? container.reparentTarget : container.transform;

				Vector3 pos = mTrans.localPosition;
				pos.z = 0f;
				mTrans.localPosition = pos;
				mParent = mTrans.parent;
			}
			else
			{
				// No valid container under the mouse -- revert the item's parent
				mTrans.parent = mParent;
			}

			// Update the grid and table references
			mGrid = NGUITools.FindInParents<UIGrid>(mParent);
			mTable = NGUITools.FindInParents<UITable>(mParent);

			// Re-enable the drag scroll view script
			if (mDragScrollView != null) Invoke("EnableDragScrollView", 0.001f);

			// Notify the widgets that the parent has changed
			NGUITools.MarkParentAsChanged(gameObject);

			if (mTable != null) mTable.repositionNow = true;
			if (mGrid != null) mGrid.repositionNow = true;
		}

		// We're now done
		OnDragDropEnd(surface);

		if (cloneOnDrag) DestroySelf();
	}

	/// <summary>
	/// Called at the end of OnDragDropRelease, indicating that the cloned object should now be destroyed.
	/// </summary>

	protected virtual void DestroySelf () { NGUITools.Destroy(gameObject); }

	/// <summary>
	/// Function called when the object gets reparented after the drop operation finishes.
	/// </summary>

	protected virtual void OnDragDropEnd (GameObject surface) { draggedItems.Remove(this); mParent = null; }

	/// <summary>
	/// Re-enable the drag scroll view script at the end of the frame.
	/// Reason: http://www.tasharen.com/forum/index.php?topic=10203.0
	/// </summary>

	protected void EnableDragScrollView ()
	{
		if (mDragScrollView != null)
			mDragScrollView.enabled = true;
	}

	/// <summary>
	/// Application losing focus should cancel the dragging operation.
	/// </summary>

	protected void OnApplicationFocus (bool focus) { if (!focus) StopDragging(null); }
}
