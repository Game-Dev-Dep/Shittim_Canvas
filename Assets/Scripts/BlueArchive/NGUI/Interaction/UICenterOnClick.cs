//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Attaching this script to an element of a scroll view will make it possible to center on it by clicking on it.
/// </summary>

[AddComponentMenu("NGUI/Interaction/Center Scroll View on Click")]
public class UICenterOnClick : MonoBehaviour
{
	[Range(1f, 32f), Tooltip("The higher the value, the faster the spring animation will be")]
	public float springStrength = 6f;

	void OnClick ()
	{
		UICenterOnChild center = NGUITools.FindInParents<UICenterOnChild>(gameObject);
		UIPanel panel = NGUITools.FindInParents<UIPanel>(gameObject);

		if (center != null)
		{
			if (center.enabled)
				center.CenterOn(transform);
		}
		else if (panel != null && panel.clipping != UIDrawCall.Clipping.None)
		{
			var sv = panel.GetComponentInParent<UIScrollView>();

			if (!sv)
			{
				Debug.LogWarning("No scroll view found", this);
				return;
			}

			var offset = -panel.cachedTransform.InverseTransformPoint(transform.position);
			if (!sv.canMoveHorizontally) offset.x = panel.cachedTransform.localPosition.x;
			if (!sv.canMoveVertically) offset.y = panel.cachedTransform.localPosition.y;
			SpringPanel.Begin(panel.cachedGameObject, offset, springStrength);
		}
	}
}
