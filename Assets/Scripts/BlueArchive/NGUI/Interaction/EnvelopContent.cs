//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// This script is capable of resizing the widget it's attached to in order to
/// completely envelop targeted UI content.
/// </summary>

[RequireComponent(typeof(UIWidget))]
[AddComponentMenu("NGUI/Interaction/Envelop Content")]
public class EnvelopContent : MonoBehaviour
{
	[Tooltip("The widgets used to determine the content bounds should reside underneath this root object")]
	public Transform targetRoot;

	[Tooltip("Value added to the left border (usually negative)")]
	public int padLeft = 0;

	[Tooltip("Value added to the right border (usually positive)")]
	public int padRight = 0;

	[Tooltip("Value added to the bottom border (usually negative)")]
	public int padBottom = 0;

	[Tooltip("Value added to the top border (usually positive)")]
	public int padTop = 0;

	[Tooltip("Minimum desired width, used only if the value is above 0")]
	public int minWidth = 0;

	[Tooltip("Minimum desired height, used only if the value is above 0")]
	public int minHeight = 0;

	[Tooltip("If true, disabled widgets will be ignored and won't be used for bounds calculations")]
	public bool ignoreDisabled = true;

	[System.NonSerialized] bool mStarted = false;

	void Start ()
	{
		mStarted = true;
		Execute();
	}

	void OnEnable () { if (mStarted) Execute(); }

	[ContextMenu("Execute")]
	public void Execute ()
	{
		if (targetRoot == transform)
		{
			Debug.LogError("Target Root object cannot be the same object that has Envelop Content. Make it a sibling instead.", this);
		}
		else if (NGUITools.IsChild(targetRoot, transform))
		{
			Debug.LogError("Target Root object should not be a parent of Envelop Content. Make it a sibling instead.", this);
		}
		else
		{
			var b = NGUIMath.CalculateRelativeWidgetBounds(transform.parent, targetRoot, !ignoreDisabled);
			var x0 = b.min.x + padLeft;
			var y0 = b.min.y + padBottom;
			var x1 = b.max.x + padRight;
			var y1 = b.max.y + padTop;

			if (minWidth > 0) x1 = Mathf.Max(x1, x0 + minWidth);
			if (minHeight > 0) y0 = Mathf.Min(y0, y1 - minHeight);

			var w = Mathf.RoundToInt(x1 - x0);
			var h = Mathf.RoundToInt(y1 - y0);

			if ((w & 1) == 1) ++w;
			if ((h & 1) == 1) ++h;

			GetComponent<UIWidget>().SetRect(x0, y0, w, h);
			BroadcastMessage("UpdateAnchors", SendMessageOptions.DontRequireReceiver);
			NGUITools.UpdateWidgetCollider(gameObject);
		}
	}
}
