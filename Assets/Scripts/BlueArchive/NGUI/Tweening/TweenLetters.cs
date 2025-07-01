//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Attaching this script to a label will make the label's letters animate.
/// </summary>

public class TweenLetters : UITweener
{
	[DoNotObfuscateNGUI] public enum AnimationLetterOrder { Forward, Reverse, Random }

	struct LetterProperties
	{
		public float start;
		public float duration; // if RandomDurations is set, these will all be different.
		public Vector3 pos;
		public Quaternion rot;
		public Vector3 scale;
	}

	[System.Serializable]
	public class AnimationProperties
	{
		[Tooltip("If set, overrides the tween's animation duration")]
		public float duration = 0f;
		public AnimationLetterOrder animationOrder = AnimationLetterOrder.Random;
		[Range(0f, 1f)]
		public float overlap = 0.5f;

		[Tooltip("If set, each letter will animate with a random duration")]
		public bool randomDurations = false;

		[MinMaxRange(0f, 1f)] public Vector2 randomness = new Vector2(0.25f, 0.75f);

		[HideInInspector] public bool upgraded = false;

		// Deprecated
		[HideInInspector] public Vector2 offsetRange = Vector2.zero;
		[HideInInspector] public Vector3 pos = Vector3.zero;
		[HideInInspector] public Vector3 rot = Vector3.zero;
		[HideInInspector] public Vector3 scale = Vector3.one;

		public Vector3 pos1 = Vector3.zero;
		public Vector3 pos2 = Vector3.zero;

		public Vector3 rot1 = Vector3.zero;
		public Vector3 rot2 = Vector3.zero;

		public Vector3 scale1 = Vector3.one;
		public Vector3 scale2 = Vector3.one;

		[Range(0f, 1f), Tooltip("Starting or finishing alpha")]
		public float alpha = 0f;

		public void Upgrade ()
		{
			upgraded = true;

			pos1 = pos - new Vector3(offsetRange.x, offsetRange.y, 0f);
			pos2 = pos + new Vector3(offsetRange.x, offsetRange.y, 0f);

			rot1 = rot;
			rot2 = rot;

			scale1 = scale;
			scale2 = scale;
		}
	}

	public AnimationProperties hoverOver = new AnimationProperties();
	public AnimationProperties hoverOut = new AnimationProperties();

	UILabel mLabel;
	int mVertexCount = -1;
	int[] mLetterOrder;
	LetterProperties[] mLetter;
	AnimationProperties mCurrent;

	protected void OnValidate ()
	{
		if (hoverOver != null && !hoverOver.upgraded) { hoverOver.Upgrade(); NGUITools.SetDirty(this, "Upgraded TweenLetters"); }
		if (hoverOut != null && !hoverOut.upgraded) { hoverOut.Upgrade(); NGUITools.SetDirty(this, "Upgraded TweenLetters"); }
	}

	void OnEnable ()
	{
		mVertexCount = -1;
		mLabel = GetComponent<UILabel>();
		mLabel.onPostFill += OnPostFill;
		mCurrent = hoverOver;
	}

	protected override void OnDisable ()
	{
		base.OnDisable();
		mLabel.onPostFill -= OnPostFill;
	}

	public override void Play (bool forward)
	{
		enabled = true;
		mCurrent = forward ? hoverOver : hoverOut;
		if (mCurrent.duration != 0f) duration = mCurrent.duration;
		base.Play(forward);
	}

	void OnPostFill (UIWidget widget, int bufferOffset, List<Vector3> verts, List<Vector2> uvs, List<Color> cols)
	{
		if (verts == null) return;
		var vertexCount = verts.Count;
		if (verts == null || vertexCount == 0) return;
		if (mLabel == null) return;

#if !UNITY_EDITOR
		try {
#endif
		var quads = mLabel.quadsPerCharacter;
		const int quadVerts = 4;
		var characterCount = vertexCount / quads / quadVerts;
		var pt = mLabel.printedText;

		if (mVertexCount != vertexCount)
		{
			mVertexCount = vertexCount;
			SetLetterOrder(characterCount);
			GetLetterDuration(characterCount);
		}

		var mtx = Matrix4x4.identity;
		var lerpPos = Vector3.zero;
		var lerpRot = Quaternion.identity;
		var lerpScale = Vector3.one;
		var lerpAlpha = 1f;
		int firstVert, letter;
		float letterStart, t; // The individual letters tweenFactor
		var letterCenter = Vector3.zero;
		var vert = Vector3.zero;
		var c = Color.clear;
		var timeIntoAnimation = tweenFactor * duration;

		for (int q = 0; q < quads; ++q)
		{
			for (int i = 0; i < characterCount; ++i)
			{
				letter = mLetterOrder[i]; // Choose which letter to animate.
				firstVert = q * characterCount * quadVerts + letter * quadVerts;

				if (firstVert >= vertexCount)
				{
#if UNITY_EDITOR
					Debug.LogError("TweenLetters encountered an unhandled case trying to modify a vertex " + firstVert + ". Vertex Count: " + vertexCount + " Pass: " + q + "\nText: " + pt);
#endif
					continue;
				}

				letterStart = mLetter[letter].start;
				t = Mathf.Clamp(timeIntoAnimation - letterStart, 0f, mLetter[letter].duration) / mLetter[letter].duration;
				t = animationCurve.Evaluate(t);

				letterCenter = GetCenter(verts, firstVert, quadVerts);

				lerpPos = Vector3.LerpUnclamped(mLetter[letter].pos, Vector3.zero, t);
				lerpRot = Quaternion.SlerpUnclamped(mLetter[letter].rot, Quaternion.identity, t);
				lerpScale = Vector3.LerpUnclamped(mLetter[letter].scale, Vector3.one, t);
				lerpAlpha = Mathf.LerpUnclamped(mCurrent.alpha, 1f, t);

				mtx.SetTRS(lerpPos, lerpRot, lerpScale);

				for (int iv = firstVert; iv < firstVert + quadVerts; ++iv)
				{
					vert = verts[iv];
					vert -= letterCenter;
					vert = mtx.MultiplyPoint3x4(vert);
					vert += letterCenter;
					verts[iv] = vert;

					c = cols[iv];
					c.a *= lerpAlpha;
					cols[iv] = c;
				}
			}
		}
#if !UNITY_EDITOR
		} catch (System.Exception) { enabled = false; }
#endif
	}

	/// <summary>
	/// Check every frame to see if the text has changed and mark the label as having been updated.
	/// </summary>
	
	protected override void OnUpdate (float factor, bool isFinished)
	{
		if (mLabel)
		{
			mLabel.enabled = !(isFinished && mCurrent != null && mCurrent == hoverOut && mCurrent.alpha == 0f);
			mLabel.MarkAsChanged();
		}
	}

	/// <summary>
	/// Sets the sequence that the letters are animated in.
	/// </summary>
	
	void SetLetterOrder (int letterCount)
	{
		if (letterCount == 0)
		{
			mLetter = null;
			mLetterOrder = null;
			return;
		}

		mLetterOrder = new int[letterCount];
		mLetter = new LetterProperties[letterCount];

		for (int i = 0; i < letterCount; ++i)
		{
			mLetterOrder[i] = (mCurrent.animationOrder == AnimationLetterOrder.Reverse) ? letterCount - 1 - i : i;

			var prop = new LetterProperties();

			prop.pos.x = Random.Range(mCurrent.pos1.x, mCurrent.pos2.x);
			prop.pos.y = Random.Range(mCurrent.pos1.y, mCurrent.pos2.y);
			prop.pos.z = Random.Range(mCurrent.pos1.z, mCurrent.pos2.z);

			prop.rot = Quaternion.Euler(
				Random.Range(mCurrent.rot1.x, mCurrent.rot2.x),
				Random.Range(mCurrent.rot1.y, mCurrent.rot2.y),
				Random.Range(mCurrent.rot1.z, mCurrent.rot2.z));

			prop.scale.x = Random.Range(mCurrent.scale1.x, mCurrent.scale2.x);
			prop.scale.y = Random.Range(mCurrent.scale1.y, mCurrent.scale2.y);
			prop.scale.z = Random.Range(mCurrent.scale1.z, mCurrent.scale2.z);

			mLetter[mLetterOrder[i]] = prop;
		}

		if (mCurrent.animationOrder == AnimationLetterOrder.Random)
		{
			// Shuffle the numbers in the array.
			var rng = new System.Random();
			int n = letterCount;

			while (n > 1)
			{
				int k = rng.Next(--n + 1);
				int tmp = mLetterOrder[k];
				mLetterOrder[k] = mLetterOrder[n];
				mLetterOrder[n] = tmp;
			}
		}
	}

	/// <summary>
	/// Returns how long each letter has to animate based on the overall duration requested and how much they overlap.
	/// </summary>
	
	void GetLetterDuration (int letterCount)
	{
		if (mCurrent.randomDurations)
		{
			for (int i = 0; i < mLetter.Length; ++i)
			{
				mLetter[i].start = Random.Range(0f, mCurrent.randomness.x * duration);
				float end = Random.Range(mCurrent.randomness.y * duration, duration);
				mLetter[i].duration = end - mLetter[i].start;
			}
		}
		else
		{
			// Calculate how long each letter will take to fade in.
			float lengthPerLetter = duration / letterCount;
			float flippedOverlap = 1f - mCurrent.overlap;

			// Figure out how long the animation will be taking into account overlapping letters.
			float totalDuration = lengthPerLetter * letterCount * flippedOverlap;

			// Scale the smaller total running time back up to the requested animation time.
			float letterDuration = ScaleRange(lengthPerLetter, totalDuration + lengthPerLetter * mCurrent.overlap, duration);

			float offset = 0;
			for (int i = 0; i < mLetter.Length; ++i)
			{
				int letter = mLetterOrder[i];
				mLetter[letter].start = offset;
				mLetter[letter].duration = letterDuration;
				offset += mLetter[letter].duration * flippedOverlap;
			}
		}
	}

	/// <summary>
	/// Simplified Scale range function that assumes a minimum of 0 for both ranges.
	/// </summary>

	float ScaleRange (float value, float baseMax, float limitMax) { return (limitMax * value / baseMax); }

	/// <summary>
	/// Finds the center point of a series of verts.
	/// </summary>

	static Vector3 GetCenter (List<Vector3> verts, int firstVert, int length)
	{
		Vector3 center = verts[firstVert];
		for (int v = firstVert + 1; v < firstVert + length; ++v) center += verts[v];
		return center / length;
	}
}
