//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright Â© 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Makes it possible to animate alpha of the widget or a panel.
/// </summary>

[ExecuteInEditMode]
public class AnimatedAlpha : MonoBehaviour
{
	[Range(0f, 1f)]
	public float alpha = 1f;

	UIWidget mWidget;
	UIPanel mPanel;

	void OnEnable ()
	{
		mWidget = GetComponent<UIWidget>();
		mPanel = GetComponent<UIPanel>();
		LateUpdate();
	}

	void LateUpdate ()
	{
		if (mWidget != null) mWidget.alpha = alpha;
		if (mPanel != null) mPanel.alpha = alpha;
	}
}
