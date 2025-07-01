//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This class makes it possible to activate or select something by pressing a key (such as space bar for example).
/// </summary>

[AddComponentMenu("NGUI/Interaction/Key Binding")]
#if TNET
public class UIKeyBinding : MonoBehaviour, TNet.IStartable
#else
public class UIKeyBinding : MonoBehaviour
#endif
{
	static public List<UIKeyBinding> list = new List<UIKeyBinding>();

	[DoNotObfuscateNGUI] public enum Action
	{
		PressAndClick,
		Select,
		All,
	}

	[DoNotObfuscateNGUI] public enum Modifier
	{
		Any,
		Shift,
		Ctrl,
		Alt,
		None,
	}

	/// <summary>
	/// Key that will trigger the binding.
	/// </summary>

	public KeyCode keyCode = KeyCode.None;

	/// <summary>
	/// Modifier key that must be active in order for the binding to trigger.
	/// </summary>

	public Modifier modifier = Modifier.Any;

	/// <summary>
	/// Action to take with the specified key.
	/// </summary>

	public Action action = Action.PressAndClick;

	[System.NonSerialized] bool mIgnoreUp = false;
	[System.NonSerialized] bool mIsInput = false;
	[System.NonSerialized] bool mPress = false;

	/// <summary>
	/// Key binding's descriptive caption.
	/// </summary>

	public string captionText
	{
		get
		{
			string s = NGUITools.KeyToCaption(keyCode);
			if (modifier == Modifier.None || modifier == Modifier.Any) return s;
			return modifier + "+" + s;
		}
	}

	/// <summary>
	/// Check to see if the specified key happens to be bound to some element.
	/// </summary>

	static public bool IsBound (KeyCode key)
	{
		for (int i = 0, imax = list.Count; i < imax; ++i)
		{
			var kb = list[i];
			if (kb != null && kb.keyCode == key) return true;
		}
		return false;
	}

	/// <summary>
	/// Find the specified key binding by its game object's name.
	/// </summary>

	static public UIKeyBinding Find (string name)
	{
		for (int i = 0, imax = list.Count; i < imax; ++i)
		{
			if (list[i].name == name) return list[i];
		}
		return null;
	}

#if TNET
	protected virtual void Awake () { TNet.TNUpdater.AddStart(this); }
#endif
	protected virtual void OnEnable () { list.Add(this); }
	protected virtual void OnDisable () { list.Remove(this); }

	/// <summary>
	/// If we're bound to an input field, subscribe to its Submit notification.
	/// </summary>

#if TNET
	public virtual void OnStart ()
#else
	protected virtual void Start ()
#endif
	{
		var input = GetComponent<UIInput>();
		mIsInput = (input != null);
		if (input != null) EventDelegate.Add(input.onSubmit, OnSubmit);
	}

	/// <summary>
	/// Ignore the KeyUp message if the input field "ate" it.
	/// </summary>

	protected virtual void OnSubmit () { if (UICamera.currentKey == keyCode && IsModifierActive()) mIgnoreUp = true; }

	/// <summary>
	/// Convenience function that checks whether the required modifier key is active.
	/// </summary>

	protected virtual bool IsModifierActive () { return IsModifierActive(modifier); }

	/// <summary>
	/// Convenience function that checks whether the required modifier key is active.
	/// </summary>

	static public bool IsModifierActive (Modifier modifier)
	{
		if (modifier == Modifier.Any) return true;

		if (modifier == Modifier.Alt)
		{
			if (UICamera.GetKey(KeyCode.LeftAlt) ||
				UICamera.GetKey(KeyCode.RightAlt)) return true;
		}
		else if (modifier == Modifier.Ctrl)
		{
			if (UICamera.GetKey(KeyCode.LeftControl) ||
				UICamera.GetKey(KeyCode.RightControl)) return true;
		}
		else if (modifier == Modifier.Shift)
		{
			if (UICamera.GetKey(KeyCode.LeftShift) ||
				UICamera.GetKey(KeyCode.RightShift)) return true;
		}
		else if (modifier == Modifier.None)
			return
				!UICamera.GetKey(KeyCode.LeftAlt) &&
				!UICamera.GetKey(KeyCode.RightAlt) &&
				!UICamera.GetKey(KeyCode.LeftControl) &&
				!UICamera.GetKey(KeyCode.RightControl) &&
				!UICamera.GetKey(KeyCode.LeftShift) &&
				!UICamera.GetKey(KeyCode.RightShift);
		return false;
	}

	/// <summary>
	/// Process the key binding.
	/// </summary>

	protected virtual void Update ()
	{
		if (keyCode != KeyCode.Numlock && UICamera.inputHasFocus) return;
		if (keyCode == KeyCode.None || !IsModifierActive()) return;
		if (UIDragDropItem.IsDragged(gameObject)) return;
#if WINDWARD && UNITY_ANDROID
		// NVIDIA Shield controller has an odd bug where it can open the on-screen keyboard via a KeyCode.Return binding,
		// and then it can never be closed. I am disabling it here until I can track down the cause.
		if (keyCode == KeyCode.Return && PlayerPrefs.GetInt("Start Chat") == 0) return;
#endif

#if UNITY_FLASH
		bool keyDown = Input.GetKeyDown(keyCode);
		bool keyUp = Input.GetKeyUp(keyCode);
#else
		bool keyDown = UICamera.GetKeyDown(keyCode);
		bool keyUp = UICamera.GetKeyUp(keyCode);
#endif

		if (keyDown) mPress = true;

		if (action == Action.PressAndClick || action == Action.All)
		{
			if (keyDown)
			{
				UICamera.currentTouchID = -1;
				UICamera.currentKey = keyCode;
				OnBindingPress(true);
			}

			if (mPress && keyUp)
			{
				UICamera.currentTouchID = -1;
				UICamera.currentKey = keyCode;
				OnBindingPress(false);
				OnBindingClick();
			}
		}

		if (action == Action.Select || action == Action.All)
		{
			if (keyUp)
			{
				if (mIsInput)
				{
					if (!mIgnoreUp && !(keyCode != KeyCode.Numlock && UICamera.inputHasFocus))
					{
						if (mPress) UICamera.selectedObject = gameObject;
					}
					mIgnoreUp = false;
				}
				else if (mPress)
				{
					UICamera.hoveredObject = gameObject;
				}
			}
		}

		if (keyUp) mPress = false;
	}

	protected virtual void OnBindingPress (bool pressed) { UICamera.Notify(gameObject, "OnPress", pressed); }
	protected virtual void OnBindingClick () { UICamera.Notify(gameObject, "OnClick", null); }

	/// <summary>
	/// Convert the key binding to its text format.
	/// </summary>

	public override string ToString () { return GetString(keyCode, modifier); }

	/// <summary>
	/// Convert the key binding to its text format.
	/// </summary>

	static public string GetString (KeyCode keyCode, Modifier modifier)
	{
		return (modifier != Modifier.None) ? modifier + "+" + NGUITools.KeyToCaption(keyCode) : NGUITools.KeyToCaption(keyCode);
	}

	/// <summary>
	/// Given the ToString() text, parse it for key and modifier information.
	/// </summary>

	static public bool GetKeyCode (string text, out KeyCode key, out Modifier modifier)
	{
		key = KeyCode.None;
		modifier = Modifier.None;
		if (string.IsNullOrEmpty(text)) return true;

		if (text.Length > 2 && text.Contains("+") && text[text.Length - 1] != '+')
		{
			var parts = text.Split(new char[] { '+' }, 2);
			key = NGUITools.CaptionToKey(parts[1]);
			try { modifier = (Modifier)System.Enum.Parse(typeof(Modifier), parts[0]); }
			catch (System.Exception) { return false; }
		}
		else
		{
			modifier = Modifier.None;
			key = NGUITools.CaptionToKey(text);
		}
		return true;
	}

	/// <summary>
	/// Get the currently active key modifier, if any.
	/// </summary>

	static public Modifier GetActiveModifier ()
	{
		var mod = Modifier.None;
		if (UICamera.GetKey(KeyCode.LeftAlt) || UICamera.GetKey(KeyCode.RightAlt)) mod = Modifier.Alt;
		else if (UICamera.GetKey(KeyCode.LeftShift) || UICamera.GetKey(KeyCode.RightShift)) mod = Modifier.Shift;
		else if (UICamera.GetKey(KeyCode.LeftControl) || UICamera.GetKey(KeyCode.RightControl)) mod = Modifier.Ctrl;
		return mod;
	}
}
