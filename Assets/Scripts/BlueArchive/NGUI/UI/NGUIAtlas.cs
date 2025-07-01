//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Simple interface that can be used anywhere. Used by the atlas maker to specify any desired custom processing that apply to textures before adding them to the atlas.
/// The returned texture will be automatically destroyed after the atlas gets created (unless it matches the original).
/// To create a pre-processor, have your script implement this interface, then reference the game object it's attached to in the Atlas Maker.
/// Example usage: having an off-screen renderer that composits several sprites together (for example a skill icon), then renders it into a smaller texture.
/// </summary>

public interface INGUITextureProcessor
{
	void PrepareToProcess (List<Texture> textures);

	Texture Process (Texture src);
}

/// <summary>
/// Generic interface for the atlas class, making it possible to support both the prefab-based UIAtlas and scriptable object-based NGUIAtlas.
/// </summary>

public interface INGUIAtlas
{
	/// <summary>
	/// Material used by the atlas.
	/// </summary>

	Material spriteMaterial { get; set; }

	/// <summary>
	/// List of sprites within the atlas.
	/// </summary>

	List<UISpriteData> spriteList { get; set; }

	/// <summary>
	/// Texture used by the atlas.
	/// </summary>

	Texture texture { get; }

	/// <summary>
	/// Pixel size is a multiplier applied to widgets dimensions when performing MakePixelPerfect() pixel correction.
	/// Most obvious use would be on retina screen displays. The resolution doubles, but with UIRoot staying the same
	/// for layout purposes, you can still get extra sharpness by switching to an HD atlas that has pixel size set to 0.5.
	/// </summary>

	float pixelSize { get; set; }

	/// <summary>
	/// Whether the atlas is using a premultiplied alpha material.
	/// </summary>

	bool premultipliedAlpha { get; }

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this atlas to use the replacement atlas instead.
	/// Suggested use: set up all your widgets to use a dummy atlas that points to the real atlas. Switching that atlas
	/// to another one (for example an HD atlas) is then a simple matter of setting this field on your dummy atlas.
	/// </summary>

	INGUIAtlas replacement { get; set; }

	/// <summary>
	/// Convenience function that retrieves a sprite by name.
	/// </summary>

	UISpriteData GetSprite (in string name);

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names.
	/// </summary>

	BetterList<string> GetListOfSprites ();

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names that contain the specified phrase.
	/// </summary>

	BetterList<string> GetListOfSprites (in string match);

	/// <summary>
	/// Helper function that determines whether the atlas uses the specified one, taking replacements into account.
	/// </summary>

	bool References (INGUIAtlas atlas);

	/// <summary>
	/// Mark all widgets associated with this atlas as having changed.
	/// </summary>

	void MarkAsChanged ();

	/// <summary>
	/// Sort the list of sprites within the atlas, making them alphabetical.
	/// </summary>

	void SortAlphabetically ();
}

/// <summary>
/// NGUI Atlas contains a collection of sprites inside one large texture atlas. It's saved as a ScriptableObject.
/// </summary>

public class NGUIAtlas : ScriptableObject, INGUIAtlas
{
	/// <summary>
	/// Legacy functionality, removed in 3.0. Do not use.
	/// </summary>

	enum Coordinates
	{
		Pixels,
		TexCoords,
	}

	// Material used by this atlas. Name is kept only for backwards compatibility, it used to be public.
	[HideInInspector][SerializeField] Material material;

	// List of all sprites inside the atlas. Name is kept only for backwards compatibility, it used to be public.
	[HideInInspector][SerializeField] List<UISpriteData> mSprites = new List<UISpriteData>();

	// Size in pixels for the sake of MakePixelPerfect functions.
	[HideInInspector][SerializeField] float mPixelSize = 1f;

	// Replacement atlas can be used to completely bypass this atlas, pulling the data from another one instead.
	[HideInInspector][SerializeField] UnityEngine.Object mReplacement;

	// Whether the atlas is using a pre-multiplied alpha material. -1 = not checked. 0 = no. 1 = yes.
	[NonSerialized] int mPMA = -1;

	// Dictionary lookup to speed up sprite retrieval at run-time
	[NonSerialized] Dictionary<string, int> mSpriteIndices = new Dictionary<string, int>();

	/// <summary>
	/// Material used by the atlas.
	/// </summary>

	public Material spriteMaterial
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.spriteMaterial : material;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.spriteMaterial = value;
			}
			else
			{
				if (material == null)
				{
					mPMA = 0;
					material = value;
				}
				else
				{
					MarkAsChanged();
					mPMA = -1;
					material = value;
					MarkAsChanged();
				}
			}
		}
	}

	/// <summary>
	/// Whether the atlas is using a premultiplied alpha material.
	/// </summary>

	public bool premultipliedAlpha
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.premultipliedAlpha;

			if (mPMA == -1)
			{
				Material mat = spriteMaterial;
				mPMA = (mat != null && mat.shader != null && mat.shader.name.Contains("Premultiplied")) ? 1 : 0;
			}
			return (mPMA == 1);
		}
	}

	/// <summary>
	/// List of sprites within the atlas.
	/// </summary>

	public List<UISpriteData> spriteList
	{
		get
		{
			var rep = replacement;
			if (rep != null) return rep.spriteList;
			return mSprites;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.spriteList = value;
			}
			else
			{
				mSprites = value;
				mSpriteIndices.Clear();
			}
		}
	}

	/// <summary>
	/// Texture used by the atlas.
	/// </summary>

	public Texture texture
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.texture : (material != null ? material.mainTexture as Texture : null);
		}
	}

	/// <summary>
	/// Pixel size is a multiplier applied to widgets dimensions when performing MakePixelPerfect() pixel correction.
	/// Most obvious use would be on retina screen displays. The resolution doubles, but with UIRoot staying the same
	/// for layout purposes, you can still get extra sharpness by switching to an HD atlas that has pixel size set to 0.5.
	/// </summary>

	public float pixelSize
	{
		get
		{
			var rep = replacement;
			return (rep != null) ? rep.pixelSize : mPixelSize;
		}
		set
		{
			var rep = replacement;

			if (rep != null)
			{
				rep.pixelSize = value;
			}
			else
			{
				float val = Mathf.Clamp(value, 0.25f, 4f);

				if (mPixelSize != val)
				{
					mPixelSize = val;
					MarkAsChanged();
				}
			}
		}
	}

	/// <summary>
	/// Setting a replacement atlas value will cause everything using this atlas to use the replacement atlas instead.
	/// Suggested use: set up all your widgets to use a dummy atlas that points to the real atlas. Switching that atlas
	/// to another one (for example an HD atlas) is then a simple matter of setting this field on your dummy atlas.
	/// </summary>

	public INGUIAtlas replacement
	{
		get
		{
			if (mReplacement == null) return null;
			return mReplacement as INGUIAtlas;
		}
		set
		{
			var rep = value;
			if (rep == this as INGUIAtlas) rep = null;

			if (mReplacement as INGUIAtlas != rep)
			{
				if (rep != null && rep.replacement == this as INGUIAtlas) rep.replacement = null;
				if (mReplacement != null) MarkAsChanged();
				mReplacement = rep as UnityEngine.Object;
				if (rep != null) material = null;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Convenience function that retrieves a sprite by name.
	/// </summary>

	public UISpriteData GetSprite (in string name)
	{
		var rep = replacement;
		if (rep != null) return rep.GetSprite(name);

		if (!string.IsNullOrEmpty(name))
		{
			if (mSprites.Count == 0) return null;

			// Rebuild the dictionary if it's empty
			if (mSpriteIndices.Count == 0)
			{
				for (int i = 0, imax = mSprites.Count; i < imax; ++i)
					mSpriteIndices[mSprites[i].name] = i;
			}

			int index;

			// O(1) lookup via a dictionary
			if (!mSpriteIndices.TryGetValue(name, out index)) return null;
			return mSprites[index];
		}
		return null;
	}

	/// <summary>
	/// Should be used when sprite names change for any reason.
	/// </summary>

	public void RebuildSpriteCache () { mSpriteIndices.Clear(); }

	/// <summary>
	/// Sort the list of sprites within the atlas, making them alphabetical.
	/// </summary>

	public void SortAlphabetically ()
	{
		mSprites.Sort(delegate(UISpriteData s1, UISpriteData s2) { return s1.name.CompareTo(s2.name); });
		mSpriteIndices.Clear();
#if UNITY_EDITOR
		NGUITools.SetDirty(this);
#endif
	}

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names.
	/// </summary>

	public BetterList<string> GetListOfSprites ()
	{
		var rep = replacement;
		if (rep != null) return rep.GetListOfSprites();

		var list = new BetterList<string>();

		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			var s = mSprites[i];
			if (s != null && !string.IsNullOrEmpty(s.name)) list.Add(s.name);
		}
		return list;
	}

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names that contain the specified phrase.
	/// </summary>

	public BetterList<string> GetListOfSprites (in string match)
	{
		var rep = replacement;
		if (rep != null) return rep.GetListOfSprites(match);
		if (string.IsNullOrEmpty(match)) return GetListOfSprites();

		var list = new BetterList<string>();

		// First try to find an exact match
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			var s = mSprites[i];

			if (s != null && !string.IsNullOrEmpty(s.name) && string.Equals(match, s.name, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(s.name);
				return list;
			}
		}

		// No exact match found? Split up the search into space-separated components.
		var keywords = match.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

		// Try to find all sprites where all keywords are present
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			var s = mSprites[i];

			if (s != null && !string.IsNullOrEmpty(s.name))
			{
				var tl = s.name.ToLower();
				var matches = 0;

				for (int b = 0; b < keywords.Length; ++b)
				{
					if (tl.Contains(keywords[b])) ++matches;
				}
				if (matches == keywords.Length) list.Add(s.name);
			}
		}
		return list;
	}

	/// <summary>
	/// Helper function that determines whether the atlas uses the specified one, taking replacements into account.
	/// </summary>

	public bool References (INGUIAtlas atlas)
	{
		if (atlas == null) return false;
		if (atlas == this as INGUIAtlas) return true;
		var rep = replacement;
		return (rep != null) ? rep.References(atlas) : false;
	}

	/// <summary>
	/// Mark all widgets associated with this atlas as having changed.
	/// </summary>

	public void MarkAsChanged ()
	{
#if UNITY_EDITOR
		NGUITools.SetDirty(this);
#endif
		var rep = replacement;
		if (rep != null) rep.MarkAsChanged();

		var list = NGUITools.FindActive<UISprite>();

		for (int i = 0, imax = list.Length; i < imax; ++i)
		{
			var sp = list[i];

			if (NGUITools.CheckIfRelated(this, sp.atlas))
			{
				var atl = sp.atlas;
				sp.atlas = null;
				sp.atlas = atl;
#if UNITY_EDITOR
				NGUITools.SetDirty(sp);
#endif
			}
		}

		var f0 = Resources.FindObjectsOfTypeAll<NGUIFont>();

		for (int i = 0, imax = f0.Length; i < imax; ++i)
		{
			var font = f0[i];
			if (font.atlas == null) continue;

			if (NGUITools.CheckIfRelated(this, font.atlas))
			{
				var atl = font.atlas;
				font.atlas = null;
				font.atlas = atl;
#if UNITY_EDITOR
				NGUITools.SetDirty(font);
#endif
			}
		}

		var f1 = Resources.FindObjectsOfTypeAll<UIFont>();

		for (int i = 0, imax = f1.Length; i < imax; ++i)
		{
			var font = f1[i];

			if (NGUITools.CheckIfRelated(this, font.atlas))
			{
				var atl = font.atlas;
				font.atlas = null;
				font.atlas = atl;
#if UNITY_EDITOR
				NGUITools.SetDirty(font);
#endif
			}
		}

		var labels = NGUITools.FindActive<UILabel>();

		for (int i = 0, imax = labels.Length; i < imax; ++i)
		{
			var lbl = labels[i];
			if (lbl.atlas == null) continue;

			if (NGUITools.CheckIfRelated(this, lbl.atlas))
			{
				var atl = lbl.atlas;
				var font = lbl.font;
				lbl.font = null;
				lbl.font = font;
#if UNITY_EDITOR
				NGUITools.SetDirty(lbl);
#endif
			}
		}
	}
}
