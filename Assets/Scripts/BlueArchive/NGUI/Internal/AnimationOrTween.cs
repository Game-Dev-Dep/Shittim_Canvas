//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

namespace AnimationOrTween
{
	[DoNotObfuscateNGUI] public enum Trigger
	{
		OnClick,
		OnHover,
		OnPress,
		OnHoverTrue,
		OnHoverFalse,
		OnPressTrue,
		OnPressFalse,
		OnActivate,
		OnActivateTrue,
		OnActivateFalse,
		OnDoubleClick,
		OnSelect,
		OnSelectTrue,
		OnSelectFalse,
		Manual,
		OnEnable,
	}

	[DoNotObfuscateNGUI] public enum Direction
	{
		Reverse = -1,
		Toggle = 0,
		Forward = 1,
	}

	[DoNotObfuscateNGUI] public enum EnableCondition
	{
		DoNothing = 0,
		EnableThenPlay,
		IgnoreDisabledState,
	}

	[DoNotObfuscateNGUI] public enum DisableCondition
	{
		DisableAfterReverse = -1,
		DoNotDisable = 0,
		DisableAfterForward = 1,
	}
}
