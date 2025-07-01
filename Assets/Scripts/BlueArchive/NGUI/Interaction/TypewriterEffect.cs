//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Text;
using System.Collections.Generic;

/// <summary>
/// This script is able to fill in the label's text gradually, giving the effect of someone typing or fading in the content over time.
/// </summary>

[RequireComponent(typeof(UILabel))]
[AddComponentMenu("NGUI/Interaction/Typewriter Effect")]
public class TypewriterEffect : MonoBehaviour
{
	static public TypewriterEffect current;

	struct FadeEntry
	{
		public int index;
		public string text;
		public float alpha;
	}

	[Tooltip("How many characters will be printed per second.")]
	public int charsPerSecond = 20;

	[Tooltip("How long it takes for each character to fade in.")]
	public float fadeInTime = 0f;

	[Tooltip("How long to pause when a period is encountered (in seconds).")]
	public float delayOnPeriod = 0f;

	[Tooltip("How long to pause when a new line character is encountered (in seconds).")]
	public float delayOnNewLine = 0f;

	[Tooltip("If a scroll view is specified, its UpdatePosition() function will be called every time the text is updated.")]
	public UIScrollView scrollView;

	[Tooltip("If set to 'true', the label's dimensions will be that of a fully faded-in content.")]
	public bool keepFullDimensions = false;

	/// <summary>
	/// Event delegate triggered when the typewriter effect finishes.
	/// </summary>

	public List<EventDelegate> onFinished = new List<EventDelegate>();

	UILabel mLabel;
	string mFullText;
	string mMyText;
	int mCurrentOffset = 0;
	float mNextChar = 0f;
	bool mReset = true;
	bool mActive = false;

	BetterList<FadeEntry> mFade = new BetterList<FadeEntry>();

	/// <summary>
	/// Whether the typewriter effect is currently active or not.
	/// </summary>

	public bool isActive { get { return mActive; } }

	/// <summary>
	/// Reset the typewriter effect to the beginning of the label.
	/// </summary>

	public void ResetToBeginning ()
	{
		if (mActive && mLabel != null && !string.IsNullOrEmpty(mFullText) && mMyText == mLabel.text)
		{
			mMyText = mFullText;
			mLabel.text = mMyText;
		}

		mCurrentOffset = 0;
		mReset = true;
		mActive = true;
		mFade.Clear();

		Update();
	}

	/// <summary>
	/// Finish the typewriter operation and show all the text right away.
	/// </summary>

	public void Finish ()
	{
		if (mActive)
		{
			mActive = false;

			if (!string.IsNullOrEmpty(mFullText))
			{
				if (!mReset && mLabel != null && mMyText == mLabel.text) mLabel.text = mFullText;

				mMyText = mFullText;
				mCurrentOffset = mFullText.Length;
			}

			mFade.Clear();

			if (keepFullDimensions && scrollView != null)
				scrollView.UpdatePosition();

			current = this;
			EventDelegate.Execute(onFinished);
			current = null;
		}
	}

	void OnEnable () { mReset = true; mActive = true; }

	void OnDisable () { Finish(); }

	void OnApplicationQuit () { onFinished = null; }

	void Update ()
	{
		if (!mActive) return;

		if (mLabel != null && mLabel.text != mMyText) mReset = true;

		if (mReset)
		{
			mReset = false;
			mNextChar = 0f;
			mCurrentOffset = 0;
			mLabel = GetComponent<UILabel>();
			mFullText = mLabel.processedText;
			mMyText = mFullText;
			mFade.Clear();

			if (keepFullDimensions && scrollView != null) scrollView.UpdatePosition();
		}

		if (string.IsNullOrEmpty(mFullText)) return;

		var len = mFullText.Length;

		while (mCurrentOffset < len && mNextChar <= RealTime.time)
		{
			int lastOffset = mCurrentOffset;
			charsPerSecond = Mathf.Max(1, charsPerSecond);

			// Automatically skip all symbols
			if (mLabel.supportEncoding)
			{
				NGUIText.nguiFont = mLabel.font;

				for (; ; )
				{
					if (NGUIText.ParseSymbol(mFullText, ref mCurrentOffset)) continue;

					var sym = NGUIText.GetSymbol(ref mFullText, mCurrentOffset, len);

					if (sym != null)
					{
						mCurrentOffset += sym.length - 1;
						continue;
					}
					break;
				}
			}

			++mCurrentOffset;

			// Reached the end? We're done.
			if (mCurrentOffset > len) break;

			// Periods and end-of-line characters should pause for a longer time.
			float delay = 1f / charsPerSecond;
			char c = (lastOffset < len) ? mFullText[lastOffset] : '\n';
			var next = (lastOffset + 1 < len) ? mFullText[lastOffset + 1] : '\n';

			if (c == '\n')
			{
				if (lastOffset > 0)
				{
					var prev = mFullText[lastOffset - 1];

					if (prev == '\n')
					{
						delay += delayOnNewLine;
					}
					else if (prev == '.' || prev == ']') delay += Mathf.Max(0f, delayOnNewLine - delayOnPeriod);
				}
			}
			else if (lastOffset + 1 == len || next <= ' ' || next == '[')
			{
				if (c == '.')
				{
					if (lastOffset + 2 < len && next == '.' && mFullText[lastOffset + 2] == '.')
					{
						delay += delayOnPeriod * 3f;
						lastOffset += 2;
					}
					else delay += delayOnPeriod;
				}
				else if (c == '!' || c == '?')
				{
					delay += delayOnPeriod;
				}
			}

			if (mNextChar == 0f)
			{
				mNextChar = RealTime.time + delay;
			}
			else mNextChar += delay;

			if (fadeInTime != 0f)
			{
				// There is smooth fading involved
				FadeEntry fe = new FadeEntry();
				fe.index = lastOffset;
				fe.alpha = 0f;
				fe.text = mFullText.Substring(lastOffset, mCurrentOffset - lastOffset);
				mFade.Add(fe);
			}
			else
			{
				// No smooth fading necessary
				mMyText = keepFullDimensions ?
					mFullText.Substring(0, mCurrentOffset) + "[00]" + mFullText.Substring(mCurrentOffset) :
					mFullText.Substring(0, mCurrentOffset);
				mLabel.text = mMyText;

				// If a scroll view was specified, update its position
				if (!keepFullDimensions && scrollView != null) scrollView.UpdatePosition();
			}
		}

		// Alpha-based fading
		if (mCurrentOffset >= len && mFade.size == 0)
		{
			mMyText = mFullText;
			mLabel.text = mMyText;
			current = this;
			EventDelegate.Execute(onFinished);
			current = null;
			mActive = false;
		}
		else if (mFade.size != 0)
		{
			for (int i = 0; i < mFade.size; )
			{
				var fe = mFade.buffer[i];
				fe.alpha += RealTime.deltaTime / fadeInTime;

				if (fe.alpha < 1f)
				{
					mFade.buffer[i] = fe;
					++i;
				}
				else mFade.RemoveAt(i);
			}

			if (mFade.size == 0)
			{
				if (mCurrentOffset < len)
				{
					if (keepFullDimensions)
					{
						mMyText = mFullText.Substring(0, mCurrentOffset) + "[00]" + mFullText.Substring(mCurrentOffset);
					}
					else mMyText = mFullText.Substring(0, mCurrentOffset);

					mLabel.text = mMyText;
				}
				else return;
			}
			else
			{
				var sb = new StringBuilder();

				for (int i = 0; i < mFade.size; ++i)
				{
					var fe = mFade.buffer[i];

					if (i == 0)
					{
						sb.Append(mFullText.Substring(0, fe.index));
					}

					sb.Append('[');
					sb.Append(NGUIText.EncodeAlpha(fe.alpha));
					sb.Append(']');
					sb.Append(fe.text);
				}

				if (keepFullDimensions && mCurrentOffset < len)
				{
					sb.Append("[00]");
					sb.Append(mFullText.Substring(mCurrentOffset));
				}

				mMyText = sb.ToString();
				mLabel.text = mMyText;
			}
		}
	}
}
