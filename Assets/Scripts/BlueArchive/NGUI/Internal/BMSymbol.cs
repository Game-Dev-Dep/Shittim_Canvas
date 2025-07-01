//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

/// <summary>
/// Symbols are a sequence of characters such as ":)" that get replaced with a sprite, such as the smiley face.
/// </summary>

[System.Serializable]
public class BMSymbol
{
	public string sequence;		// Character sequence, such as ":)"
	public string spriteName;	// Name of the sprite in the atlas
	public bool colored;		// Whether the label's color should apply to the symbol
	public bool pixelPerfect;	// If set, the symbol won't be affected by label's scaling, and will always be printed pixel-perfect. Useful for things like line separators.

	[System.NonSerialized] UISpriteData mSprite = null;
	[System.NonSerialized] bool mIsValid = false;
	[System.NonSerialized] int mLength = 0;
	[System.NonSerialized] int mOffsetX = 0;	// (outer - inner) in pixels
	[System.NonSerialized] int mOffsetY = 0;	// (outer - inner) in pixels
	[System.NonSerialized] int mWidth = 0;		// Symbol's width in pixels (sprite.outer.width)
	[System.NonSerialized] int mHeight = 0;		// Symbol's height in pixels (sprite.outer.height)
	[System.NonSerialized] int mAdvance = 0;	// Symbol's inner width in pixels (sprite.inner.width)
	[System.NonSerialized] Rect mUV;

	public int length { get { if (mLength == 0) mLength = sequence.Length; return mLength; } }
	public int offsetX { get { return mOffsetX; } }
	public int offsetY { get { return mOffsetY; } }
	public int width { get { return mWidth; } }
	public int height { get { return mHeight; } }
	public int paddedHeight { get { return mSprite != null ? mSprite.paddingTop + mSprite.paddingBottom + mSprite.height : mHeight; } }
	public int advance { get { return mAdvance; } }
	public Rect uvRect { get { return mUV; } }

	/// <summary>
	/// Mark this symbol as dirty, clearing the sprite reference.
	/// </summary>

	public void MarkAsChanged () { mIsValid = false; }

	/// <summary>
	/// Validate this symbol, given the specified atlas.
	/// </summary>

	public bool Validate (INGUIAtlas atlas)
	{
		if (atlas == null) return false;

#if UNITY_EDITOR
		if (!Application.isPlaying || !mIsValid)
#else
		if (!mIsValid)
#endif
		{
			if (string.IsNullOrEmpty(spriteName)) return false;

			Texture tex = null;
			mSprite = atlas.GetSprite(spriteName);

			if (mSprite != null)
			{
				tex = atlas.texture;

				if (tex == null)
				{
					mSprite = null;
				}
				else
				{
					mUV = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
					mUV = NGUIMath.ConvertToTexCoords(mUV, tex.width, tex.height);
					mOffsetX = mSprite.paddingLeft;
					mOffsetY = mSprite.paddingTop;
					mWidth = mSprite.width;
					mHeight = mSprite.height;
					mAdvance = mSprite.width + (mSprite.paddingLeft + mSprite.paddingRight);
					mIsValid = true;
				}
			}
		}
		return (mSprite != null);
	}
}
