//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

using UnityEngine;
using System.Collections.Generic;

public enum NGUIFontType
{
	Auto,
	Bitmap,
	Reference,
	Dynamic,
}

/// <summary>
/// Generic interface for the NGUI's font implementations. Added in order to support both
/// old style (prefab-based) and new style (scriptable object-based) fonts.
/// </summary>

public interface INGUIFont
{
	/// <summary>
	/// Explicitly specified font type. Legacy behaviour would always determine this automatically in the past.
	/// </summary>

	NGUIFontType type { get; set; }

	/// <summary>
	/// Access to the BMFont class directly.
	/// </summary>

	BMFont bmFont { get; set; }

	/// <summary>
	/// Original width of the font's texture in pixels.
	/// </summary>

	int texWidth { get; set; }

	/// <summary>
	/// Original height of the font's texture in pixels.
	/// </summary>

	int texHeight { get; set; }

	/// <summary>
	/// Whether the font has any symbols defined.
	/// </summary>

	bool hasSymbols { get; }

	/// <summary>
	/// List of symbols within the font.
	/// </summary>

	List<BMSymbol> symbols { get; set; }

	/// <summary>
	/// Atlas used by the font, if any.
	/// </summary>

	INGUIAtlas atlas { get; set; }

	/// <summary>
	/// Atlas used by the symbols, if any. Can match the 'atlas'.
	/// </summary>

	INGUIAtlas symbolAtlas { get; }

	/// <summary>
	/// Convenience method that returns the chosen sprite inside the atlas.
	/// </summary>

	UISpriteData GetSprite (string spriteName);

	/// <summary>
	/// Get or set the material used by this font.
	/// </summary>

	Material material { get; set; }

	/// <summary>
	/// Whether the font is using a premultiplied alpha material.
	/// </summary>

	bool premultipliedAlphaShader { get; }

	/// <summary>
	/// Whether the font is a packed font.
	/// </summary>

	bool packedFontShader { get; }

	/// <summary>
	/// Convenience function that returns the texture used by the font.
	/// </summary>

	Texture2D texture { get; }

	/// <summary>
	/// Offset and scale applied to all UV coordinates.
	/// </summary>

	Rect uvRect { get; set; }

	/// <summary>
	/// Sprite used by the font, if any.
	/// </summary>

	string spriteName { get; set; }

	/// <summary>
	/// Whether this is a valid font.
	/// </summary>

	bool isValid { get; }

	/// <summary>
	/// Pixel-perfect size of this font.
	/// </summary>

	int defaultSize { get; set; }

	/// <summary>
	/// If set, overwrites the width of the space bar, in pixels. Useful for correcting some fonts.
	/// </summary>

	int spaceWidth { get; set; }

	/// <summary>
	/// Retrieves the sprite used by the font, if any.
	/// </summary>

	UISpriteData sprite { get; }

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this font to use the replacement font instead.
	/// Suggested use: set up all your widgets to use a dummy font that points to the real font. Switching that font to
	/// another one (for example an eastern language one) is then a simple matter of setting this field on your dummy font.
	/// </summary>

	INGUIFont replacement { get; set; }

	/// <summary>
	/// Checks the replacement references, returning the deepest-most font.
	/// </summary>

	INGUIFont finalFont { get; }

	/// <summary>
	/// Whether the font is dynamic.
	/// </summary>

	bool isDynamic { get; }

	/// <summary>
	/// Get or set the dynamic font source.
	/// </summary>

	Font dynamicFont { get; set; }

	/// <summary>
	/// Get or set the dynamic font's style.
	/// </summary>

	FontStyle dynamicFontStyle { get; set; }

	/// <summary>
	/// Helper function that determines whether the font uses the specified one, taking replacements into account.
	/// </summary>

	bool References (INGUIFont font);

	/// <summary>
	/// Refresh all labels that use this font.
	/// </summary>

	void MarkAsChanged ();

	/// <summary>
	/// Forcefully update the font's sprite reference.
	/// </summary>

	void UpdateUVRect ();

	/// <summary>
	/// Retrieve the symbol at the beginning of the specified sequence, if a match is found.
	/// </summary>

	BMSymbol MatchSymbol (ref string text, int offset, int textLength);

	/// <summary>
	/// Add a new symbol to the font.
	/// </summary>

	BMSymbol AddSymbol (string sequence, string spriteName);

	/// <summary>
	/// Remove the specified symbol from the font.
	/// </summary>

	void RemoveSymbol (string sequence);

	/// <summary>
	/// Change an existing symbol's sequence to the specified value.
	/// </summary>

	void RenameSymbol (string before, string after);

	/// <summary>
	/// Whether the specified sprite is being used by the font.
	/// </summary>

	bool UsesSprite (string s);
}

/// <summary>
/// NGUI Font contains everything needed to be able to print text.
/// </summary>

[ExecuteInEditMode]
public class NGUIFont : ScriptableObject, INGUIFont
{
	[HideInInspector, SerializeField] NGUIFontType mType = NGUIFontType.Auto;
	[HideInInspector, SerializeField] Material mMat;
	[HideInInspector, SerializeField] Rect mUVRect = new Rect(0f, 0f, 1f, 1f);
	[HideInInspector, SerializeField] BMFont mFont = new BMFont();
	[HideInInspector, SerializeField] Object mAtlas;
	[HideInInspector, SerializeField] Object mReplacement;
	[HideInInspector, SerializeField] Object mSybolAtlas;
	[HideInInspector, SerializeField] float mSymbolScale = 1f;
	[HideInInspector, SerializeField] int mSymbolOffset = 0;
	[HideInInspector, SerializeField] int mSymbolMaxHeight = 0;
	[HideInInspector, SerializeField] bool mSymbolCentered = false;

	// List of symbols, such as emoticons like ":)", ":(", etc
	[HideInInspector, SerializeField] List<BMSymbol> mSymbols = new List<BMSymbol>();

	// Embedded symbols are created from all of the atlas sprites the first time a sprite request is made.
	// This allows for embedding sprites in text without emoticons by using [sp=X] syntax, where 'X' is the name of the sprite.
	[System.NonSerialized] List<BMSymbol> mEmbeddedSymbols = null;

	// Used for dynamic fonts
	[HideInInspector, SerializeField] Font mDynamicFont;
	[HideInInspector, SerializeField] int mDynamicFontSize = 16;
	[HideInInspector, SerializeField] FontStyle mDynamicFontStyle = FontStyle.Normal;
	[HideInInspector, SerializeField] int mSpaceWidth = 0;

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
			if (mType == NGUIFontType.Auto)
			{
				if (mReplacement != null) return NGUIFontType.Reference;
				if (mDynamicFont != null) return NGUIFontType.Dynamic;
				return NGUIFontType.Bitmap;
			}
			return mType;
		}
		set
		{
			if (mType != value)
			{
				if (mType == NGUIFontType.Bitmap) mMat = null;
				mType = value;
			}
		}
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
			if (symbolAtlas != null) return (mSymbols != null && mSymbols.Count != 0);
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
			if (symbolAtlas != null) return mSymbols;
			var rep = replacement;
			return (rep != null) ? rep.symbols : mSymbols;
		}
		set
		{
			if (symbolAtlas != null)
			{
				mSymbols = value;
			}
			else
			{
				var rep = replacement;
				if (rep != null) rep.symbols = value;
				else mSymbols = value;
			}
		}
	}

	/// <summary>
	/// Atlas used by the font, if any.
	/// </summary>

	public INGUIAtlas atlas
	{
		get
		{
			var type = this.type;

			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.atlas;
			}
			else if (type == NGUIFontType.Bitmap)
			{
				return mAtlas as INGUIAtlas;
			}
			return null;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.atlas = value;
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

	/// <summary>
	/// Sprite atlas used for symbols.
	/// </summary>

	public INGUIAtlas symbolAtlas
	{
		get
		{
			return mSybolAtlas as INGUIAtlas;
		}
		set
		{
			if (mSybolAtlas as INGUIAtlas != value)
			{
				mSybolAtlas = value as UnityEngine.Object;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Convenience method that returns the chosen sprite inside the atlas.
	/// </summary>

	public UISpriteData GetSprite(string spriteName)
	{
		var ia = symbolAtlas;
		if (ia == null) ia = atlas;
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
			var type = this.type;

			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.material;
			}
			else if (type == NGUIFontType.Bitmap)
			{
				var ia = mAtlas as INGUIAtlas;
				if (ia != null) return ia.spriteMaterial;
			}
			else if (type == NGUIFontType.Dynamic)
			{
				if (mDynamicFont != null)
				{
					if (mMat != null)
					{
						if (mMat != mDynamicFont.material)
						{
							mMat.mainTexture = mDynamicFont.material.mainTexture;
						}
						return mMat;
					}
					return mDynamicFont.material;
				}
			}
			return mMat;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.material = value;
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
	/// Material used for symbols.
	/// </summary>

	public Material symbolMaterial
	{
		get
		{
			var atl = symbolAtlas;
			return (atl != null) ? atl.spriteMaterial : null;
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
			var type = this.type;

			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.packedFontShader;
				return false;
			}
			else if (type == NGUIFontType.Dynamic) return false;

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
	/// Convenience property that returns the texture used by the font.
	/// </summary>

	public Texture2D texture
	{
		get
		{
			var mat = material;
			return (mat != null) ? mat.mainTexture as Texture2D : null;
		}
	}

	/// <summary>
	/// Convenience property returning the texture used by the font's symbols.
	/// </summary>

	public Texture2D symbolTexture
	{
		get
		{
			var mat = symbolMaterial;
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
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.uvRect;
			}
			return (mAtlas != null && sprite != null) ? mUVRect : new Rect(0f, 0f, 1f, 1f);
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.uvRect = value;
			}
			else if (sprite == null && mUVRect != value)
			{
				mUVRect = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Symbols (emoticons) will be scaled by this factor.
	/// </summary>

	public float symbolScale
	{
		get
		{
			return mSymbolScale;
		}
		set
		{
			value = Mathf.Clamp(value, 0.05f, 5f);

			if (mSymbolScale != value)
			{
				mSymbolScale = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Symbols (emoticons) will be adjusted vertically by this number of pixels.
	/// </summary>

	public int symbolOffset
	{
		get
		{
			return mSymbolOffset;
		}
		set
		{
			if (mSymbolOffset != value)
			{
				mSymbolOffset = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Symbols (emoticons) will have this maximum height. If a sprite exceeds this height, it will be automatically shrunken down.
	/// </summary>

	public int symbolMaxHeight
	{
		get
		{
			return mSymbolMaxHeight;
		}
		set
		{
			if (mSymbolMaxHeight != value)
			{
				mSymbolMaxHeight = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Symbols (emoticons) will be centered if this is 'true'. The alternative is they will be top-left aligned instead.
	/// </summary>

	public bool symbolCentered
	{
		get
		{
			return mSymbolCentered;
		}
		set
		{
			if (mSymbolCentered != value)
			{
				mSymbolCentered = value;
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
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				return rep != null ? rep.spriteName : "";
			}
			return mFont.spriteName;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.spriteName = value;
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
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.defaultSize;
			}
			else if (isDynamic || mFont == null) return mDynamicFontSize;
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
	/// Replaces the width of the space bar if set to a non-zero value.
	/// </summary>

	public int spaceWidth
	{
		get
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.spaceWidth;
			}
			return mSpaceWidth;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.spaceWidth = value;
			}
			else mSpaceWidth = value;
		}
	}

	/// <summary>
	/// Retrieves the sprite used by the font, if any.
	/// </summary>

	public UISpriteData sprite
	{
		get
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.sprite;
				return null;
			}

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
			var type = this.type;

			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				return (rep != null) && rep.isDynamic;
			}
			else if (type == NGUIFontType.Dynamic)
			{
				return (mDynamicFont != null);
			}
			return false;
		}
	}

	/// <summary>
	/// Get or set the dynamic font source.
	/// </summary>

	public Font dynamicFont
	{
		get
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.dynamicFont;
				return null;
			}
			return mDynamicFont;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.dynamicFont = value;
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
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) return rep.dynamicFontStyle;
			}
			return mDynamicFontStyle;
		}
		set
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement;
				if (rep != null) rep.dynamicFontStyle = value;
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
		mEmbeddedSymbols = null;
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
		var s = symbols;

		for (int i = 0, imax = s.Count; i < imax; ++i)
		{
			var sym = s[i];
			if (sym.sequence == sequence) return sym;
		}

		if (createIfMissing)
		{
			var sym = new BMSymbol();
			sym.sequence = sequence;
			s.Add(sym);
			return sym;
		}
		return null;
	}

	/// <summary>
	/// Retrieve the symbol at the beginning of the specified sequence, if a match is found.
	/// </summary>

	public BMSymbol MatchSymbol (ref string text, int offset, int textLength)
	{
		if (offset < 0 || offset >= textLength) return null;
		var atl = symbolAtlas != null ? symbolAtlas : atlas;
		if (atl == null) return null;

		var s = symbols;
		int count = s.Count;
		var sl = atl.spriteList;
		if (sl == null || sl.Count == 0) return null;
		textLength -= offset;

		// Run through all symbols
		for (int i = 0; i < count; ++i)
		{
			var sym = s[i];

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
			if (match && sym.Validate(atl)) return sym;
		}

		// Support embedding sprites using [sp=X] syntax, where 'X' is the name of the sprite
		if (text[offset] == '[' && offset + 6 < text.Length && text[offset + 1] == 's' && text[offset + 2] == 'p' && text[offset + 3] == '=')
		{
			// Create the embedded symbol list if it hasn't been created already
			if (mEmbeddedSymbols == null)
			{
				mEmbeddedSymbols = new List<BMSymbol>();

				var sprites = atl.spriteList;

				foreach (var sp in sprites)
				{
					var bm = new BMSymbol();
					bm.sequence = "[sp=" + sp.name + "]";
					bm.spriteName = sp.name;
					mEmbeddedSymbols.Add(bm);
				}
			}

			// Run through the embedded symbol list
			s = mEmbeddedSymbols;
			count = s.Count;

			// Run through all symbols
			for (int i = 0; i < count; ++i)
			{
				var sym = s[i];

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
				if (match && sym.Validate(atl)) return sym;
			}
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
			if (s.Equals(spriteName)) return true;

			var symbols = this.symbols;

			for (int i = 0, imax = symbols.Count; i < imax; ++i)
			{
				var sym = symbols[i];
				if (s.Equals(sym.spriteName)) return true;
			}
		}
		return false;
	}

	#region Dynamic font kerning implementation

	/// <summary>
	/// Unity's dynamic font CharacterInfo struct is completely devoid of kerning-related information.
	/// In order to get dynamic fonts to print correctly, kerning information has to be retrieved from FreeType directly, then saved.
	/// This means that a part of the dynamic font is not, in fact, "dynamic", as there is no way of accessing this data outside of edit mode.
	/// </summary>

	[System.Serializable]
	public struct KerningAdjustment
	{
		public int left;
		public int right;
		public int offset;
	}

	[HideInInspector, SerializeField] List<KerningAdjustment> mKerningAdjustments;

	[System.NonSerialized] Dictionary<uint, short> mKerningCache;

	/// <summary>
	/// Returns the number of kerning pairs in this font.
	/// </summary>

	public int kerningCount
	{
		get
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement as NGUIFont;
				if (rep != null) return rep.kerningCount;
			}

			if (type == NGUIFontType.Bitmap && bmFont != null)
			{
				var glyphs = bmFont.glyphs;
				var count = 0;
				if (glyphs != null) foreach(var g in glyphs) if (g.kerning != null) count += g.kerning.Count;
				return count;
			}
			return mKerningAdjustments != null ? mKerningAdjustments.Count : 0;
		}
	}

	/// <summary>
	/// Kerning data for dynamic fonts. Unity is missing kerning information, so NGUI adds it at edit time.
	/// Bitmap kerning data is stored differently (as it was coded that way ages ago), so it can't be retrieved as a single array.
	/// </summary>

	public List<KerningAdjustment> kerningData
	{
		get
		{
			if (type == NGUIFontType.Reference)
			{
				var rep = replacement as NGUIFont;
				if (rep != null) return rep.kerningData;
			}

			if (type == NGUIFontType.Bitmap) return null;
			return mKerningAdjustments;
		}
	}

	/// <summary>
	/// Retrieves the special amount by which to adjust the cursor position, given the specified previous character.
	/// </summary>

	public int GetKerning (int previousChar, int currentChar)
	{
		if (type == NGUIFontType.Reference)
		{
			var rep = replacement as NGUIFont;
			if (rep != null) return rep.GetKerning(previousChar, currentChar);
		}

		if (previousChar == 0) return 0;

		if (type == NGUIFontType.Bitmap)
		{
			var bf = bmFont;
			if (bf == null) return 0;

			var g = bf.GetGlyph(currentChar);
			if (g == null) return 0;

			return g.GetKerning(previousChar);
		}

		if (mKerningAdjustments == null || mKerningAdjustments.Count == 0) return 0;

		if (mKerningCache == null)
		{
			mKerningCache = new Dictionary<uint, short>();

			foreach (var adj in mKerningAdjustments)
			{
				var key = (((uint)adj.left << 16) | (uint)adj.right);
				mKerningCache[key] = (short)adj.offset;
			}
		}

		short retVal;
		var lookup = (((uint)previousChar << 16) | (uint)currentChar);
		if (mKerningCache.TryGetValue(lookup, out retVal)) return retVal;
		return 0;
	}

	/// <summary>
	/// Set the kerning data. This is meant to be used with dynamic fonts, since they are inherently missing kerning information in Unity.
	/// </summary>

	public void SetKerning (List<KerningAdjustment> kerning)
	{
		if (type == NGUIFontType.Reference)
		{
			var rep = replacement as NGUIFont;
			if (rep != null) { rep.SetKerning(kerning); return; }
		}

		if (kerning != null && kerning.Count != 0)
		{
			mKerningAdjustments = new List<KerningAdjustment>();
			mKerningCache = new Dictionary<uint, short>();

			foreach (var k in kerning)
			{
				var lookup = (((uint)k.left << 16) | (uint)k.right);
				mKerningAdjustments.Add(k);
				mKerningCache[lookup] = (short)k.offset;
			}

			MarkAsChanged();
		}
		else if (mKerningAdjustments != null)
		{
			mKerningAdjustments = null;
			mKerningCache = null;
			MarkAsChanged();
		}
	}

	/// <summary>
	/// Add a new kerning entry to the character (or adjust an existing one).
	/// </summary>

	public void SetKerning (int previousChar, int currentChar, int amount)
	{
		if (type == NGUIFontType.Reference)
		{
			var rep = replacement as NGUIFont;
			if (rep != null) { rep.SetKerning(previousChar, currentChar, amount); return; }
		}

		if (type == NGUIFontType.Bitmap)
		{
			var bf = bmFont;
			if (bf == null) return;

			var g = bf.GetGlyph(currentChar);
			if (g == null) return;

			g.SetKerning(previousChar, amount);
			MarkAsChanged();
			return;
		}

		if (mKerningAdjustments == null) mKerningAdjustments = new List<KerningAdjustment>();
		if (mKerningCache == null) mKerningCache = new Dictionary<uint, short>();

		var lookup = (((uint)previousChar << 16) | (uint)currentChar);
		mKerningCache[lookup] = (short)amount;

		for (int i = 0, imax = mKerningAdjustments.Count; i < imax; ++i)
		{
			var adj = mKerningAdjustments[i];

			if (adj.left == previousChar && adj.right == currentChar)
			{
				if (adj.offset == amount) return;

				if (amount == 0) { mKerningAdjustments.RemoveAt(i); MarkAsChanged(); return; }
				adj.offset = amount;
				mKerningAdjustments[i] = adj;
				MarkAsChanged();
				return;
			}
		}

		if (amount == 0) return;

		var a = new KerningAdjustment();
		a.left = previousChar;
		a.right = currentChar;
		a.offset = amount;
		mKerningAdjustments.Add(a);
		MarkAsChanged();
	}
	#endregion
}

