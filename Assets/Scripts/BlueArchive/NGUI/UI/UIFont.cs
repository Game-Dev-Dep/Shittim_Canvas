//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// UIFont contains everything needed to be able to print text. This is the legacy component that stores its data in a prefab.
/// It's best to use NGUIFont now as it saves its data in Scriptable Objects, which plays better with Unity 2018+.
/// </summary>

[ExecuteInEditMode]
//[AddComponentMenu("NGUI/UI/NGUI Font")] // Replaced by NGUIFont, a Scriptable Object
public class UIFont : MonoBehaviour, INGUIFont
{
	[HideInInspector] [SerializeField] Material mMat;
	[HideInInspector] [SerializeField] Rect mUVRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector] [SerializeField] BMFont mFont = new BMFont();
	[HideInInspector] [SerializeField] Object mAtlas;
	[HideInInspector] [SerializeField] Object mReplacement;

	// List of symbols, such as emoticons like ":)", ":(", etc
	[HideInInspector] [SerializeField] List<BMSymbol> mSymbols = new List<BMSymbol>();

	// Used for dynamic fonts
	[HideInInspector] [SerializeField] Font mDynamicFont;
	[HideInInspector] [SerializeField] int mDynamicFontSize = 16;
	[HideInInspector] [SerializeField] FontStyle mDynamicFontStyle = FontStyle.Normal;

	// Cached value
	[System.NonSerialized] UISpriteData mSprite = null;
	[System.NonSerialized] int mPMA = -1;
	[System.NonSerialized] int mPacked = -1;

	/// <summary>
	/// Explicitly specified font type. Legacy behaviour would always determine this automatically in the past.
	/// </summary>

	public NGUIFontType type
	{
		get
		{
			if (replacement != null) return NGUIFontType.Reference;
			if (dynamicFont != null) return NGUIFontType.Dynamic;
			return NGUIFontType.Bitmap;
		}
		set { }
	}

	/// <summary>
	/// Access to the BMFont class directly.
	/// </summary>

	public BMFont bmFont
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.bmFont : mFont;
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.bmFont = value;
			else mFont = value;
		}
	}

	/// <summary>
	/// Original width of the font's texture in pixels.
	/// </summary>

	public int texWidth
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.texWidth : ((mFont != null) ? mFont.texWidth : 1);
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.texWidth = value;
			else if (mFont != null) mFont.texWidth = value;
		}
	}

	/// <summary>
	/// Original height of the font's texture in pixels.
	/// </summary>

	public int texHeight
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.texHeight : ((mFont != null) ? mFont.texHeight : 1);
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.texHeight = value;
			else if (mFont != null) mFont.texHeight = value;
		}
	}

	/// <summary>
	/// Whether the font has any symbols defined.
	/// </summary>

	public bool hasSymbols
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.hasSymbols : (mSymbols != null && mSymbols.Count != 0);
		}
	}

	/// <summary>
	/// List of symbols within the font.
	/// </summary>

	public List<BMSymbol> symbols
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.symbols : mSymbols;
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.symbols = value;
			else mSymbols = value;
		}
	}

	/// <summary>
	/// Atlas used by the font, if any.
	/// </summary>

	public INGUIAtlas atlas
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.atlas;
			return mAtlas as INGUIAtlas;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.atlas = value;
			}
			else if (mAtlas as INGUIAtlas != value)
			{
				mPMA = -1;
				mAtlas = value as UnityEngine.Object;

				if (value != null)
				{
					mMat = value.spriteMaterial;
					if (sprite != null) mUVRect = uvRect;
				}
				else
				{
					mAtlas = null;
					mMat = null;
				}

				MarkAsChanged();
			}
		}
	}

	public INGUIAtlas symbolAtlas { get { return atlas; } }

	/// <summary>
	/// Convenience method that returns the chosen sprite inside the atlas.
	/// </summary>

	public UISpriteData GetSprite (string spriteName)
	{
		var ia = atlas;
		if (ia != null) return ia.GetSprite(spriteName);
		return null;
	}

	/// <summary>
	/// Get or set the material used by this font.
	/// </summary>

	public Material material
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.material;

			var ia = mAtlas as INGUIAtlas;
			if (ia != null) return ia.spriteMaterial;

			if (mMat != null)
			{
				if (mDynamicFont != null && mMat != mDynamicFont.material)
				{
					mMat.mainTexture = mDynamicFont.material.mainTexture;
				}
				return mMat;
			}

			if (mDynamicFont != null)
			{
				return mDynamicFont.material;
			}
			return null;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.material = value;
			}
			else if (mMat != value)
			{
				mPMA = -1;
				mMat = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether the font is using a premultiplied alpha material.
	/// </summary>

	[System.Obsolete("Use premultipliedAlphaShader instead")]
	public bool premultipliedAlpha { get { return premultipliedAlphaShader; } }

	/// <summary>
	/// Whether the font is using a premultiplied alpha material.
	/// </summary>

	public bool premultipliedAlphaShader
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.premultipliedAlphaShader;

			var ia = mAtlas as INGUIAtlas;
			if (ia != null) return ia.premultipliedAlpha;

			if (mPMA == -1)
			{
				Material mat = material;
				mPMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
			}
			return (mPMA == 1);
		}
	}

	/// <summary>
	/// Whether the font is a packed font.
	/// </summary>

	public bool packedFontShader
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.packedFontShader;
			if (mAtlas != null) return false;

			if (mPacked == -1)
			{
				Material mat = material;
				mPacked = (mat != null && mat.shader != null && mat.shader.name.Contains("Packed")) ? 1 : 0;
			}
			return (mPacked == 1);
		}
	}

	/// <summary>
	/// Convenience function that returns the texture used by the font.
	/// </summary>

	public Texture2D texture
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.texture;
			Material mat = material;
			return (mat != null) ? mat.mainTexture as Texture2D : null;
		}
	}

	/// <summary>
	/// Offset and scale applied to all UV coordinates.
	/// </summary>

	public Rect uvRect
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.uvRect;
			return (mAtlas != null && sprite != null) ? mUVRect : new Rect(0f, 0f, 1f, 1f);
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.uvRect = value;
			}
			else if (sprite == null && mUVRect != value)
			{
				mUVRect = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Sprite used by the font, if any.
	/// </summary>

	public string spriteName
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.spriteName : mFont.spriteName;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.spriteName = value;
			}
			else if (mFont.spriteName != value)
			{
				mFont.spriteName = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether this is a valid font.
	/// </summary>

	public bool isValid { get { return mDynamicFont != null || mFont.isValid; } }

	[System.Obsolete("Use defaultSize instead")]
	public int size
	{
		get { return defaultSize; }
		set { defaultSize = value; }
	}

	/// <summary>
	/// Pixel-perfect size of this font.
	/// </summary>

	public int defaultSize
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.defaultSize;
			if (isDynamic || mFont == null) return mDynamicFontSize;
			return mFont.charSize;
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.defaultSize = value;
			else mDynamicFontSize = value;
		}
	}

	/// <summary>
	/// This feature was added after deprecating this class, so it's not actually used here.
	/// </summary>

	public int spaceWidth
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.spaceWidth;
			return 0;
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.spaceWidth = value;
		}
	}

	/// <summary>
	/// Retrieves the sprite used by the font, if any.
	/// </summary>

	public UISpriteData sprite
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.sprite;

			var ia = mAtlas as INGUIAtlas;

			if (mSprite == null && ia != null && mFont != null && !string.IsNullOrEmpty(mFont.spriteName))
			{
				mSprite = ia.GetSprite(mFont.spriteName);
				if (mSprite == null) mSprite = ia.GetSprite(name);
				if (mSprite == null) mFont.spriteName = null;
				else UpdateUVRect();

				var sym = symbols;
				for (int i = 0, imax = sym.Count; i < imax; ++i) sym[i].MarkAsChanged();
			}
			return mSprite;
		}
	}

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this font to use the replacement font instead.
	/// Suggested use: set up all your widgets to use a dummy font that points to the real font. Switching that font to
	/// another one (for example an eastern language one) is then a simple matter of setting this field on your dummy font.
	/// </summary>

	public INGUIFont replacement
	{
		get
		{
			if (mReplacement == null) return null;
			return mReplacement as INGUIFont;
		}
		set
		{
			INGUIFont rep = value;
			if (rep == this as INGUIFont) rep = null;

			if (mReplacement as INGUIFont != rep)
			{
				if (rep != null && rep.replacement == this as INGUIFont) rep.replacement = null;
				if (mReplacement != null) MarkAsChanged();
				mReplacement = rep as UnityEngine.Object;

				if (rep != null)
				{
					mPMA = -1;
					mMat = null;
					mFont = null;
					mDynamicFont = null;
				}
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Checks the replacement references, returning the deepest-most font.
	/// </summary>

	public INGUIFont finalFont
	{
		get
		{
			INGUIFont fnt = this;

			for (int i = 0; i < 10; ++i)
			{
				var rep = fnt.replacement;
				if (rep != null) fnt = rep;
			}
			return fnt;
		}
	}

	/// <summary>
	/// Whether the font is dynamic.
	/// </summary>

	public bool isDynamic
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.isDynamic : (mDynamicFont != null);
		}
	}

	/// <summary>
	/// Get or set the dynamic font source.
	/// </summary>

	public Font dynamicFont
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.dynamicFont : mDynamicFont;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.dynamicFont = value;
			}
			else if (mDynamicFont != value)
			{
				if (mDynamicFont != null) material = null;
				mDynamicFont = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Get or set the dynamic font's style.
	/// </summary>

	public FontStyle dynamicFontStyle
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.dynamicFontStyle : mDynamicFontStyle;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.dynamicFontStyle = value;
			}
			else if (mDynamicFontStyle != value)
			{
				mDynamicFontStyle = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Trim the glyphs, making sure they never go past the trimmed texture bounds.
	/// </summary>

	void Trim ()
	{
		Texture tex = null;
		var ia = mAtlas as INGUIAtlas;
		if (ia != null) tex = ia.texture;

		if (tex != null && mSprite != null)
		{
			Rect full = NGUIMath.ConvertToPixels(mUVRect, texture.width, texture.height, true);
			Rect trimmed = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);

			int xMin = Mathf.RoundToInt(trimmed.xMin - full.xMin);
			int yMin = Mathf.RoundToInt(trimmed.yMin - full.yMin);
			int xMax = Mathf.RoundToInt(trimmed.xMax - full.xMin);
			int yMax = Mathf.RoundToInt(trimmed.yMax - full.yMin);

			mFont.Trim(xMin, yMin, xMax, yMax);
		}
	}

	/// <summary>
	/// Helper function that determines whether the font uses the specified one, taking replacements into account.
	/// </summary>

	public bool References (INGUIFont font)
	{
		if (font == null) return false;
		if (font == this as INGUIFont) return true;
		var rep = replacement;
		return (rep != null) ? rep.References(font) : false;
	}

	/// <summary>
	/// Refresh all labels that use this font.
	/// </summary>

	public void MarkAsChanged ()
	{
#if UNITY_EDITOR
		NGUITools.SetDirty(this);
#endif
		var rep = replacement;
		if (rep != null) rep.MarkAsChanged();

		mSprite = null;
		var labels = NGUITools.FindActive<UILabel>();

		for (int i = 0, imax = labels.Length; i < imax; ++i)
		{
			var lbl = labels[i];

			if (lbl.enabled && NGUITools.GetActive(lbl.gameObject) && NGUITools.CheckIfRelated(this, lbl.font as INGUIFont))
			{
				var fnt = lbl.font;
				lbl.font = null;
				lbl.font = fnt;
			}
		}

		// Clear all symbols
		var sym = symbols;
		for (int i = 0, imax = sym.Count; i < imax; ++i) sym[i].MarkAsChanged();
	}

	/// <summary>
	/// Forcefully update the font's sprite reference.
	/// </summary>

	public void UpdateUVRect ()
	{
		if (mAtlas == null) return;

		Texture tex = null;
		var ia = mAtlas as INGUIAtlas;
		if (ia != null) tex = ia.texture;

		if (tex != null)
		{
			mUVRect = new Rect(
				mSprite.x - mSprite.paddingLeft,
				mSprite.y - mSprite.paddingTop,
				mSprite.width + mSprite.paddingLeft + mSprite.paddingRight,
				mSprite.height + mSprite.paddingTop + mSprite.paddingBottom);

			mUVRect = NGUIMath.ConvertToTexCoords(mUVRect, tex.width, tex.height);
#if UNITY_EDITOR
			// The font should always use the original texture size
			if (mFont != null)
			{
				float tw = (float)mFont.texWidth / tex.width;
				float th = (float)mFont.texHeight / tex.height;

				if (tw != mUVRect.width || th != mUVRect.height)
				{
					//Debug.LogWarning("Font sprite size doesn't match the expected font texture size.\n" +
					//	"Did you use the 'inner padding' setting on the Texture Packer? It must remain at '0'.", this);
					mUVRect.width = tw;
					mUVRect.height = th;
				}
			}
#endif
			// Trimmed sprite? Trim the glyphs
			if (mSprite.hasPadding) Trim();
		}
	}

	/// <summary>
	/// Retrieve the specified symbol, optionally creating it if it's missing.
	/// </summary>

	BMSymbol GetSymbol (string sequence, bool createIfMissing)
	{
		for (int i = 0, imax = mSymbols.Count; i < imax; ++i)
		{
			var sym = mSymbols[i];
			if (sym.sequence == sequence) return sym;
		}

		if (createIfMissing)
		{
			var sym = new BMSymbol();
			sym.sequence = sequence;
			mSymbols.Add(sym);
			return sym;
		}
		return null;
	}

	/// <summary>
	/// Retrieve the symbol at the beginning of the specified sequence, if a match is found.
	/// </summary>

	public BMSymbol MatchSymbol (ref string text, int offset, int textLength)
	{
		// No symbols present
		int count = mSymbols.Count;
		if (count == 0) return null;
		textLength -= offset;

		// Run through all symbols
		for (int i = 0; i < count; ++i)
		{
			var sym = mSymbols[i];

			// If the symbol's length is longer, move on
			int symbolLength = sym.length;
			if (symbolLength == 0 || textLength < symbolLength) continue;

			var match = true;

			// Match the characters
			for (int c = 0; c < symbolLength; ++c)
			{
				if (text[offset + c] != sym.sequence[c])
				{
					match = false;
					break;
				}
			}

			// Match found
			if (match && sym.Validate(atlas)) return sym;
		}
		return null;
	}

	/// <summary>
	/// Add a new symbol to the font.
	/// </summary>

	public BMSymbol AddSymbol (string sequence, string spriteName)
	{
		var symbol = GetSymbol(sequence, true);
		symbol.spriteName = spriteName;
		MarkAsChanged();
		return symbol;
	}

	/// <summary>
	/// Remove the specified symbol from the font.
	/// </summary>

	public void RemoveSymbol (string sequence)
	{
		var symbol = GetSymbol(sequence, false);
		if (symbol != null) symbols.Remove(symbol);
		MarkAsChanged();
	}

	/// <summary>
	/// Change an existing symbol's sequence to the specified value.
	/// </summary>

	public void RenameSymbol (string before, string after)
	{
		var symbol = GetSymbol(before, false);
		if (symbol != null) symbol.sequence = after;
		MarkAsChanged();
	}

	/// <summary>
	/// Whether the specified sprite is being used by the font.
	/// </summary>

	public bool UsesSprite (string s)
	{
		if (!string.IsNullOrEmpty(s))
		{
			if (s.Equals(spriteName))
				return true;

			for (int i = 0, imax = symbols.Count; i < imax; ++i)
			{
				BMSymbol sym = symbols[i];
				if (s.Equals(sym.spriteName))
					return true;
			}
		}
		return false;
	}
}

