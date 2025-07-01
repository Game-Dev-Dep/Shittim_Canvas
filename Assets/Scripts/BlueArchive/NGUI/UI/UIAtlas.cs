//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// UI Atlas contains a collection of sprites inside one large texture atlas.
/// This is the legacy atlas component, kept for full backwards compatibility. All newly created UIs should use NGUIAtlas-based atlases instead.
/// </summary>

//[AddComponentMenu("NGUI/UI/Atlas")] // No longer used as the scriptable object NGUIAtlas is used instead
public class UIAtlas : MonoBehaviour, INGUIAtlas
{
	// Legacy functionality, removed in 3.0. Do not use.
	[System.Serializable]
	class Sprite
	{
		public string name = "Unity Bug";
		public Rect outer = new Rect(0f, 0f, 1f, 1f);
		public Rect inner = new Rect(0f, 0f, 1f, 1f);
		public bool rotated = false;

		// Padding is needed for trimmed sprites and is relative to sprite width and height
		public float paddingLeft	= 0f;
		public float paddingRight	= 0f;
		public float paddingTop		= 0f;
		public float paddingBottom	= 0f;

		public bool hasPadding { get { return paddingLeft != 0f || paddingRight != 0f || paddingTop != 0f || paddingBottom != 0f; } }
	}

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

	// Legacy functionality -- do not use
	[HideInInspector][SerializeField] Coordinates mCoordinates = Coordinates.Pixels;
	[HideInInspector][SerializeField] List<Sprite> sprites = new List<Sprite>();

	// Whether the atlas is using a pre-multiplied alpha material. -1 = not checked. 0 = no. 1 = yes.
	[System.NonSerialized] int mPMA = -1;

	// Dictionary lookup to speed up sprite retrieval at run-time
	[System.NonSerialized] Dictionary<string, int> mSpriteIndices = new Dictionary<string, int>();

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
			if (mSprites.Count == 0) Upgrade();
			return mSprites;
		}
		set
		{
			var rep = replacement;
			if (rep != null) rep.spriteList = value;
			else mSprites = value;
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

		if (rep != null)
		{
			return rep.GetSprite(name);
		}
		else if (!string.IsNullOrEmpty(name))
		{
			if (mSprites.Count == 0) Upgrade();
			if (mSprites.Count == 0) return null;

			// O(1) lookup via a dictionary
#if UNITY_EDITOR
			if (Application.isPlaying)
#endif
			{
				// The number of indices differs from the sprite list? Rebuild the indices.
				if (mSpriteIndices.Count != mSprites.Count)
					MarkSpriteListAsChanged();

				int index;
				if (mSpriteIndices.TryGetValue(name, out index))
				{
					// If the sprite is present, return it as-is
					if (index > -1 && index < mSprites.Count) return mSprites[index];

					// The sprite index was out of range -- perhaps the sprite was removed? Rebuild the indices.
					MarkSpriteListAsChanged();

					// Try to look up the index again
					return mSpriteIndices.TryGetValue(name, out index) ? mSprites[index] : null;
				}
			}

			// Sequential O(N) lookup.
			for (int i = 0, imax = mSprites.Count; i < imax; ++i)
			{
				UISpriteData s = mSprites[i];

				// string.Equals doesn't seem to work with Flash export
				if (!string.IsNullOrEmpty(s.name) && name == s.name)
				{
#if UNITY_EDITOR
					if (!Application.isPlaying) return s;
#endif
					// If this point was reached then the sprite is present in the non-indexed list,
					// so the sprite indices should be updated.
					MarkSpriteListAsChanged();
					return s;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Rebuild the sprite indices. Call this after modifying the spriteList at run time.
	/// </summary>

	public void MarkSpriteListAsChanged ()
	{
#if UNITY_EDITOR
		if (Application.isPlaying)
#endif
		{
			mSpriteIndices.Clear();
			for (int i = 0, imax = mSprites.Count; i < imax; ++i)
				mSpriteIndices[mSprites[i].name] = i;
		}
	}

	/// <summary>
	/// Sort the list of sprites within the atlas, making them alphabetical.
	/// </summary>

	public void SortAlphabetically ()
	{
		mSprites.Sort(delegate(UISpriteData s1, UISpriteData s2) { return s1.name.CompareTo(s2.name); });
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
		if (mSprites.Count == 0) Upgrade();

		BetterList<string> list = new BetterList<string>();

		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			UISpriteData s = mSprites[i];
			if (s != null && !string.IsNullOrEmpty(s.name)) list.Add(s.name);
		}
		return list;
	}

	/// <summary>
	/// Convenience function that retrieves a list of all sprite names that contain the specified phrase
	/// </summary>

	public BetterList<string> GetListOfSprites (in string match)
	{
		var rep = replacement;
		if (rep != null) return rep.GetListOfSprites(match);
		if (string.IsNullOrEmpty(match)) return GetListOfSprites();

		if (mSprites.Count == 0) Upgrade();
		BetterList<string> list = new BetterList<string>();

		// First try to find an exact match
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			UISpriteData s = mSprites[i];

			if (s != null && !string.IsNullOrEmpty(s.name) && string.Equals(match, s.name, StringComparison.OrdinalIgnoreCase))
			{
				list.Add(s.name);
				return list;
			}
		}

		// No exact match found? Split up the search into space-separated components.
		string[] keywords = match.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
		for (int i = 0; i < keywords.Length; ++i) keywords[i] = keywords[i].ToLower();

		// Try to find all sprites where all keywords are present
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			UISpriteData s = mSprites[i];

			if (s != null && !string.IsNullOrEmpty(s.name))
			{
				string tl = s.name.ToLower();
				int matches = 0;

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

	/// <summary>
	/// Performs an upgrade from the legacy way of specifying data to the new one.
	/// </summary>

	bool Upgrade ()
	{
		var rep = replacement;

		if (rep != null)
		{
			var uiAtlas = rep as UIAtlas;
			if (uiAtlas != null) return uiAtlas.Upgrade();
		}

		if (mSprites.Count == 0 && sprites.Count > 0 && material)
		{
			Texture tex = material.mainTexture;
			int width = (tex != null) ? tex.width : 512;
			int height = (tex != null) ? tex.height : 512;

			for (int i = 0; i < sprites.Count; ++i)
			{
				Sprite old = sprites[i];
				Rect outer = old.outer;
				Rect inner = old.inner;

				if (mCoordinates == Coordinates.TexCoords)
				{
					NGUIMath.ConvertToPixels(outer, width, height, true);
					NGUIMath.ConvertToPixels(inner, width, height, true);
				}

				UISpriteData sd = new UISpriteData();
				sd.name = old.name;

				sd.x = Mathf.RoundToInt(outer.xMin);
				sd.y = Mathf.RoundToInt(outer.yMin);
				sd.width = Mathf.RoundToInt(outer.width);
				sd.height = Mathf.RoundToInt(outer.height);

				sd.paddingLeft = Mathf.RoundToInt(old.paddingLeft * outer.width);
				sd.paddingRight = Mathf.RoundToInt(old.paddingRight * outer.width);
				sd.paddingBottom = Mathf.RoundToInt(old.paddingBottom * outer.height);
				sd.paddingTop = Mathf.RoundToInt(old.paddingTop * outer.height);

				sd.borderLeft = Mathf.RoundToInt(inner.xMin - outer.xMin);
				sd.borderRight = Mathf.RoundToInt(outer.xMax - inner.xMax);
				sd.borderBottom = Mathf.RoundToInt(outer.yMax - inner.yMax);
				sd.borderTop = Mathf.RoundToInt(inner.yMin - outer.yMin);

				mSprites.Add(sd);
			}
			sprites.Clear();
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
			UnityEditor.AssetDatabase.SaveAssets();
#endif
			return true;
		}
		return false;
	}
}
