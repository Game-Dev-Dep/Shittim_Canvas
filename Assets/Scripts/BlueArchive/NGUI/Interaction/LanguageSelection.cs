//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Turns the popup list it's attached to into a language selection list.
/// </summary>

[RequireComponent(typeof(UIPopupList))]
[AddComponentMenu("NGUI/Interaction/Language Selection")]
public class LanguageSelection : MonoBehaviour
{
	UIPopupList mList;
	bool mStarted = false;

	void Awake () { mList = GetComponent<UIPopupList>(); }

	void Start ()
	{
		mStarted = true;
		Refresh();
		EventDelegate.Add(mList.onChange, delegate() { Localization.language = UIPopupList.current.value; });
	}

	void OnEnable () { if (mStarted) Refresh(); }

	/// <summary>
	/// Immediately refresh the list of known languages.
	/// </summary>

	public void Refresh ()
	{
		if (mList != null && Localization.knownLanguages != null)
		{
			mList.Clear();

			for (int i = 0, imax = Localization.knownLanguages.Length; i < imax; ++i)
				mList.items.Add(Localization.knownLanguages[i]);

			mList.value = Localization.language;
		}
	}

	void OnLocalize () { Refresh(); } 
}
