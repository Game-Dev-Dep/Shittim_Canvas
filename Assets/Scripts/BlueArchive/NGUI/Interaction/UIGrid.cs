//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// All children added to the game object with this script will be repositioned to be on a grid of specified dimensions.
/// If you want the cells to automatically set their scale based on the dimensions of their content, take a look at UITable.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Grid")]
public class UIGrid : UIWidgetContainer
{
	public delegate void OnReposition ();

	[DoNotObfuscateNGUI] public enum Arrangement
	{
		Horizontal,
		Vertical,
		CellSnap,
	}

	[DoNotObfuscateNGUI] public enum Sorting
	{
		None,
		Alphabetic,
		Horizontal,
		Vertical,
		Custom,
	}

	/// <summary>
	/// Type of arrangement -- vertical, horizontal or cell snap.
	/// </summary>

	public Arrangement arrangement = Arrangement.Horizontal;

	/// <summary>
	/// How to sort the grid's elements.
	/// </summary>

	public Sorting sorting = Sorting.None;

	[Tooltip("Whether the sort order will be inverted")]
	public bool inverted = false;

	[Tooltip("Final pivot point for the grid's content.")]
	public UIWidget.Pivot pivot = UIWidget.Pivot.TopLeft;

	[DoNotObfuscateNGUI]
	public enum Expansion
	{
		Legacy,
		BasedOnPivot,
	}

	[Tooltip("Legacy style expansion positions children to the right and down from the first one. Pivot-based expansion positions children moving away from the pivot instead, and centered if necessary.")]
	public Expansion expansionStyle = Expansion.Legacy;

	/// <summary>
	/// Maximum children per line.
	/// If the arrangement is horizontal, this denotes the number of columns.
	/// If the arrangement is vertical, this stands for the number of rows.
	/// </summary>

	public int maxPerLine = 0;

	/// <summary>
	/// The width of each of the cells.
	/// </summary>

	public float cellWidth = 200f;

	/// <summary>
	/// The height of each of the cells.
	/// </summary>

	public float cellHeight = 200f;

	[Tooltip("Whether the grid will smoothly animate its children into the correct place.")]
	public bool animateSmoothly = false;

	[Tooltip("If 'true' and Animate Smoothly is also 'true', will check to see if elements have a TweenAlpha on them. If so, elements will appear in their target position instead of animating from the current position.")]
	public bool animateFadeIn = false;

	/// <summary>
	/// Whether to ignore the disabled children or to treat them as being present.
	/// </summary>

	public bool hideInactive = false;

	/// <summary>
	/// Whether the parent container will be notified of the grid's changes.
	/// </summary>

	public bool keepWithinPanel = false;

	/// <summary>
	/// Callback triggered when the grid repositions its contents.
	/// </summary>

	public OnReposition onReposition;

	/// <summary>
	/// Custom sort delegate, used when the sorting method is set to 'custom'.
	/// </summary>

	public System.Comparison<Transform> onCustomSort;

	// Use the 'sorting' property instead
	[HideInInspector][SerializeField] bool sorted = false;

	protected UIPanel mPanel;
	protected bool mInitDone = false;

	/// <summary>
	/// Reposition the children on the next Update().
	/// </summary>

	public bool repositionNow { set { if (value) { mSprings = null; enabled = true; } } }

	/// <summary>
	/// Get the current list of the grid's children.
	/// </summary>

	public List<Transform> GetChildList ()
	{
		var myTrans = transform;
		var list = new List<Transform>();

		for (int i = 0; i < myTrans.childCount; ++i)
		{
			var t = myTrans.GetChild(i);
			var go = t.gameObject;

			if (go && (!hideInactive || go.activeSelf))
			{
				if (!UIDragDropItem.IsDragged(go)) list.Add(t);
			}
		}

		// Sort the list using the desired sorting logic
		if (sorting != Sorting.None && arrangement != Arrangement.CellSnap)
		{
			if (sorting == Sorting.Alphabetic) { if (inverted) list.Sort(SortByNameInv); else list.Sort(SortByName); }
			else if (sorting == Sorting.Horizontal) { if (inverted) list.Sort(SortHorizontalInv); else list.Sort(SortHorizontal); }
			else if (sorting == Sorting.Vertical) { if (inverted) list.Sort(SortVerticalInv); else list.Sort(SortVertical); }
			else if (onCustomSort != null) list.Sort(onCustomSort);
			else Sort(list);
		}
		return list;
	}

	/// <summary>
	/// Convenience method: get the child at the specified index.
	/// Note that if you plan on calling this function more than once, it's faster to get the entire list using GetChildList() instead.
	/// </summary>

	public Transform GetChild (int index)
	{
		List<Transform> list = GetChildList();
		return (index < list.Count) ? list[index] : null;
	}

	/// <summary>
	/// Get the index of the specified item.
	/// </summary>

	public int GetIndex (Transform trans) { return GetChildList().IndexOf(trans); }

	/// <summary>
	/// Convenience method -- add a new child.
	/// </summary>

	[System.Obsolete("Use gameObject.AddChild or transform.parent = gridTransform")]
	public void AddChild (Transform trans)
	{
		if (trans != null)
		{
			trans.parent = transform;
			ResetPosition(GetChildList());
		}
	}

	/// <summary>
	/// Convenience method -- add a new child.
	/// Note that if you plan on adding multiple objects, it's faster to GetChildList() and modify that instead.
	/// </summary>

	[System.Obsolete("Use gameObject.AddChild or transform.parent = gridTransform")]
	public void AddChild (Transform trans, bool sort)
	{
		if (trans != null)
		{
			trans.parent = transform;
			ResetPosition(GetChildList());
		}
	}

	/// <summary>
	/// Remove the specified child from the list.
	/// Note that if you plan on removing multiple objects, it's faster to GetChildList() and modify that instead.
	/// </summary>

	public bool RemoveChild (Transform t)
	{
		List<Transform> list = GetChildList();

		if (list.Remove(t))
		{
			ResetPosition(list);
			return true;
		}
		return false;
	}

	/// <summary>
	/// Initialize the grid. Executed only once.
	/// </summary>

	protected virtual void Init ()
	{
		mInitDone = true;
		mPanel = NGUITools.FindInParents<UIPanel>(gameObject);
	}

	/// <summary>
	/// Cache everything and reset the initial position of all children.
	/// </summary>

	protected virtual void Start ()
	{
		if (!mInitDone) Init();
		bool smooth = animateSmoothly;
		animateSmoothly = false;
		Reposition();
		animateSmoothly = smooth;
		enabled = false;
	}

	/// <summary>
	/// Reset the position if necessary, then disable the component.
	/// </summary>

	protected virtual void Update ()
	{
		if (mSprings != null && mSprings.Count != 0)
		{
			var springing = false;

			foreach (var sp in mSprings)
			{
				if (sp && sp.enabled)
				{
					springing = true;
					break;
				}
			}

			if (!springing)
			{
				mSprings.Clear();
				enabled = false;
			}

			// Constrain everything to be within the panel's bounds
			if (keepWithinPanel) ConstrainWithinPanel();

			// Notify the listener
			if (onReposition != null) onReposition();
		}
		else
		{
			Reposition();
			if (mSprings == null || mSprings.Count == 0) enabled = false;
		}
	}

	/// <summary>
	/// Reposition the content on inspector validation.
	/// </summary>

	void OnValidate () { if (!Application.isPlaying && NGUITools.GetActive(this)) Reposition(); }

	// Various generic sorting functions
	static public int SortByName (Transform a, Transform b) { return string.Compare(a.name, b.name); }
	static public int SortByNameInv (Transform a, Transform b) { return string.Compare(b.name, a.name); }
	static public int SortHorizontal (Transform a, Transform b) { return a.localPosition.x.CompareTo(b.localPosition.x); }
	static public int SortHorizontalInv (Transform a, Transform b) { return b.localPosition.x.CompareTo(a.localPosition.x); }
	static public int SortVertical (Transform a, Transform b) { return b.localPosition.y.CompareTo(a.localPosition.y); }
	static public int SortVerticalInv (Transform a, Transform b) { return a.localPosition.y.CompareTo(b.localPosition.y); }

	/// <summary>
	/// You can override this function, but in most cases it's easier to just set the onCustomSort delegate instead.
	/// </summary>

	protected virtual void Sort (List<Transform> list) { }

	[System.NonSerialized] List<SpringPosition> mSprings = null;

	/// <summary>
	/// Recalculate the position of all elements within the grid, sorting them alphabetically if necessary.
	/// </summary>

	[ContextMenu("Execute")]
	public virtual void Reposition ()
	{
		if (Application.isPlaying && !mInitDone && NGUITools.GetActive(gameObject)) Init();
		else if (mPanel == null) mPanel = NGUITools.FindInParents<UIPanel>(gameObject);

		// Legacy functionality
		if (sorted)
		{
			sorted = false;
			if (sorting == Sorting.None) sorting = Sorting.Alphabetic;
			NGUITools.SetDirty(this);
		}

		// Get the list of children in their current order
		var list = GetChildList();

		// Reset the position and order of all objects in the list
		ResetPosition(list);

		// Constrain everything to be within the panel's bounds
		if (keepWithinPanel) ConstrainWithinPanel();

		// Stay enabled until springs finish
		if (mSprings != null && mSprings.Count != 0) enabled = true;

		// Notify the listener
		if (onReposition != null) onReposition();
	}

	/// <summary>
	/// Constrain the grid's content to be within the panel's bounds.
	/// </summary>

	public void ConstrainWithinPanel ()
	{
		if (mPanel != null)
		{
			mPanel.ConstrainTargetToBounds(transform, true);
			var sv = mPanel.GetComponent<UIScrollView>();
			if (sv != null) sv.UpdateScrollbars(true);
		}
	}

	/// <summary>
	/// Reset the position of all child objects based on the order of items in the list.
	/// </summary>

	protected virtual void ResetPosition (List<Transform> list)
	{
		if (mSprings != null) mSprings.Clear();

		int x = 0;
		int y = 0;
		int maxX = 0;
		int maxY = 0;
		var smoothAnim = animateSmoothly && gameObject.activeInHierarchy && Application.isPlaying;
		var offset = 0f;

		// Re-add the children in the same order we have them in and position them accordingly
		for (int i = 0, imax = list.Count; i < imax; ++i)
		{
			var t = list[i];
			var pos = t.localPosition;
			var depth = pos.z;

			if (arrangement == Arrangement.CellSnap)
			{
				if (cellWidth > 0) pos.x = Mathf.Round(pos.x / cellWidth) * cellWidth;
				if (cellHeight > 0) pos.y = Mathf.Round(pos.y / cellHeight) * cellHeight;
			}
			else
			{
				pos = (arrangement == Arrangement.Horizontal) ? new Vector3(cellWidth * x, -cellHeight * y, depth) : new Vector3(cellWidth * y, -cellHeight * x, depth);

				// Pivot-based expansion flips which way the new rows/columns go, keeping them moving away from the pivot
				if (expansionStyle == Expansion.BasedOnPivot)
				{
					if (pivot == UIWidget.Pivot.Bottom || pivot == UIWidget.Pivot.BottomLeft || pivot == UIWidget.Pivot.BottomRight) pos.y = -pos.y;
					if (pivot == UIWidget.Pivot.Right || pivot == UIWidget.Pivot.BottomRight || pivot == UIWidget.Pivot.TopRight) pos.x = -pos.x;

					if (offset != 0f)
					{
						if (arrangement == Arrangement.Horizontal)
						{
							if (pivot == UIWidget.Pivot.Top || pivot == UIWidget.Pivot.Bottom) pos.x += offset;
						}
						else if (arrangement == Arrangement.Vertical)
						{
							if (pivot == UIWidget.Pivot.Left || pivot == UIWidget.Pivot.Right) pos.y -= offset;
						}
					}
				}
			}

			// Special case: if the element is currently invisible and is fading in, we want to position it in the right place right away
			if (smoothAnim && animateFadeIn)
			{
				var tw = t.GetComponent<TweenAlpha>();
				if (tw != null && tw.enabled && tw.value == 0f && tw.to == 1f) smoothAnim = false;
			}

			if (smoothAnim)
			{
				var sp = t.gameObject.GetComponent<SpringPosition>();
				if (sp != null && sp.enabled) sp.Finish();
			}

			if (smoothAnim)
			{
				if (t.localPosition != pos)
				{
					var sp = SpringPosition.Begin(t.gameObject, pos, 15f);
					sp.ignoreTimeScale = true;
					if (mSprings == null) mSprings = new List<SpringPosition>();
					mSprings.Add(sp);
				}
			}
			else t.localPosition = pos;

			maxX = Mathf.Max(maxX, x);
			maxY = Mathf.Max(maxY, y);

			if (++x >= maxPerLine && maxPerLine > 0)
			{
				x = 0;
				++y;

				var expected = list.Count - i;
				if (expected < maxPerLine) offset = Mathf.Round((maxPerLine - expected + 1) * 0.5f * (arrangement == Arrangement.Horizontal ? cellWidth : cellHeight));
			}
		}

		// Apply the origin offset
		if (pivot != UIWidget.Pivot.TopLeft)
		{
			var po = NGUIMath.GetPivotOffset(pivot);

			float fx, fy;

			if (arrangement == Arrangement.Horizontal)
			{
				fx = Mathf.Lerp(0f, maxX * cellWidth, po.x);
				fy = Mathf.Lerp(-maxY * cellHeight, 0f, po.y);
			}
			else
			{
				fx = Mathf.Lerp(0f, maxY * cellWidth, po.x);
				fy = Mathf.Lerp(-maxX * cellHeight, 0f, po.y);
			}

			if (expansionStyle == Expansion.BasedOnPivot && arrangement != Arrangement.CellSnap)
			{
				if (pivot == UIWidget.Pivot.Bottom || pivot == UIWidget.Pivot.BottomLeft || pivot == UIWidget.Pivot.BottomRight) fy = 0f;
				if (pivot == UIWidget.Pivot.Right || pivot == UIWidget.Pivot.BottomRight || pivot == UIWidget.Pivot.TopRight) fx = 0f;
			}

			foreach (var t in list)
			{
				var sp = smoothAnim ? t.GetComponent<SpringPosition>() : null;

				if (sp != null && sp.enabled)
				{
					sp.target.x -= fx;
					sp.target.y -= fy;
				}
				else
				{
					Vector3 pos = t.localPosition;
					pos.x -= fx;
					pos.y -= fy;
					t.localPosition = pos;
				}
			}
		}
	}
}
