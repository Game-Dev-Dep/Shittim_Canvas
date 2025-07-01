//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sprite is a textured element in the UI hierarchy.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Sprite")]
public class UISprite : UIBasicSprite
{
	// Cached and saved values
	[HideInInspector] [SerializeField] Object mAtlas;
	[HideInInspector] [SerializeField] string mSpriteName;
	[HideInInspector] [SerializeField] bool mFixedAspect = false;

	// Deprecated, no longer used
	[HideInInspector] [SerializeField] bool mFillCenter = true;

	[System.NonSerialized] protected UISpriteData mSprite;
	[System.NonSerialized] bool mSpriteSet = false;

	/// <summary>
	/// If set, will automatically make the sprite pixel-perfect every time it's changed.
	/// </summary>

	[System.NonSerialized] public bool autoMakePixelPerfect = false;

	/// <summary>
	/// Main texture is assigned on the atlas.
	/// </summary>

	public override Texture mainTexture
	{
		get
		{
			Material mat = null;
			var ia = mAtlas as INGUIAtlas;
			if (ia != null) mat = ia.spriteMaterial;
			return (mat != null) ? mat.mainTexture : null;
		}
		set
		{
			base.mainTexture = value;
		}
	}

	/// <summary>
	/// Material comes from the base class first, and sprite atlas last.
	/// </summary>

	public override Material material
	{
		get
		{
			var mat = base.material;
			if (mat != null) return mat;
			var ia = mAtlas as INGUIAtlas;
			if (ia != null) return ia.spriteMaterial;
			return null;
		}
		set
		{
			base.material = value;
		}
	}

	/// <summary>
	/// Atlas used by this widget.
	/// </summary>

	public INGUIAtlas atlas
	{
		get
		{
			return mAtlas as INGUIAtlas;
		}
		set
		{
			if (mAtlas as INGUIAtlas != value)
			{
				RemoveFromPanel();

				mAtlas = value as UnityEngine.Object;
				mSpriteSet = false;
				mSprite = null;

				// Automatically choose the first sprite
				if (string.IsNullOrEmpty(mSpriteName))
				{
					var ia = mAtlas as INGUIAtlas;

					if (ia != null && ia.spriteList.Count > 0)
					{
						SetAtlasSprite(ia.spriteList[0]);
						mSpriteName = mSprite.name;
					}
				}

				// Re-link the sprite
				if (!string.IsNullOrEmpty(mSpriteName))
				{
					string sprite = mSpriteName;
					mSpriteName = "";
					spriteName = sprite;
					MarkAsChanged();
				}
			}
		}
	}


	public bool fixedAspect
	{
		get
		{
			return mFixedAspect;
		}
		set
		{
			if (mFixedAspect != value)
			{
				mFixedAspect = value;
				mDrawRegion = new Vector4(0f, 0f, 1f, 1f);
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Convenience method that returns the chosen sprite inside the atlas.
	/// </summary>

	public UISpriteData GetSprite (string spriteName)
	{
		var a = atlas;
		if (a == null) return null;
		return a.GetSprite(spriteName);
	}

	public override void MarkAsChanged ()
	{
		mSprite = null;
		mSpriteSet = false;
		base.MarkAsChanged();
	}

	/// <summary>
	/// Sprite within the atlas used to draw this widget.
	/// </summary>

	public string spriteName
	{
		get
		{
			return mSpriteName;
		}
		set
		{
			if (string.IsNullOrEmpty(value))
			{
				// If the sprite name hasn't been set yet, no need to do anything
				if (string.IsNullOrEmpty(mSpriteName)) return;

				// Clear the sprite name and the sprite reference
				mSpriteName = "";
				mSprite = null;
				mChanged = true;
				mSpriteSet = false;
				MarkAsChanged();
			}
			else if (mSpriteName != value)
			{
				// If the sprite name changes, the sprite reference should also be updated
				mSpriteName = value;
				mSprite = null;
				mChanged = true;
				mSpriteSet = false;
				MarkAsChanged();
				if (autoMakePixelPerfect) MakePixelPerfect();
			}
		}
	}

	/// <summary>
	/// Is there a valid sprite to work with?
	/// </summary>

	public bool isValid { get { return GetAtlasSprite() != null; } }

	/// <summary>
	/// Whether the center part of the sprite will be filled or not. Turn it off if you want only to borders to show up.
	/// </summary>

	[System.Obsolete("Use 'centerType' instead")]
	public bool fillCenter
	{
		get
		{
			return centerType != AdvancedType.Invisible;
		}
		set
		{
			if (value != (centerType != AdvancedType.Invisible))
			{
				centerType = value ? AdvancedType.Sliced : AdvancedType.Invisible;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Whether a gradient will be applied.
	/// </summary>

	public bool applyGradient
	{
		get
		{
			return mApplyGradient;
		}
		set
		{
			if (mApplyGradient != value)
			{
				mApplyGradient = value;
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Top gradient color.
	/// </summary>

	public Color gradientTop
	{
		get
		{
			return mGradientTop;
		}
		set
		{
			if (mGradientTop != value)
			{
				mGradientTop = value;
				if (mApplyGradient) MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Bottom gradient color.
	/// </summary>

	public Color gradientBottom
	{
		get
		{
			return mGradientBottom;
		}
		set
		{
			if (mGradientBottom != value)
			{
				mGradientBottom = value;
				if (mApplyGradient) MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Sliced sprites generally have a border. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	public override Vector4 border
	{
		get
		{
			UISpriteData sp = GetAtlasSprite();
			if (sp == null) return base.border;
			return new Vector4(sp.borderLeft, sp.borderBottom, sp.borderRight, sp.borderTop);
		}
	}

	/// <summary>
	/// Trimmed space in the atlas around the sprite. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	protected override Vector4 padding
	{
		get
		{
			var sp = GetAtlasSprite();
			var p = new Vector4(0, 0, 0, 0);

			if (sp != null)
			{
				p.x = sp.paddingLeft;
				p.y = sp.paddingBottom;
				p.z = sp.paddingRight;
				p.w = sp.paddingTop;
			}
			return p;
		}
	}

	/// <summary>
	/// Size of the pixel -- used for drawing.
	/// </summary>

	override public float pixelSize
	{
		get
		{
			if (mAtlas == null) return 1f;
			var ia = mAtlas as INGUIAtlas;
			if (ia != null) return ia.pixelSize;
			return 1f;
		}
	}

	/// <summary>
	/// Minimum allowed width for this widget.
	/// </summary>

	override public int minWidth
	{
		get
		{
			if (type == Type.Sliced || type == Type.Advanced)
			{
				float ps = pixelSize;
				Vector4 b = border * pixelSize;
				int min = Mathf.RoundToInt(b.x + b.z);

				UISpriteData sp = GetAtlasSprite();
				if (sp != null) min += Mathf.RoundToInt(ps * (sp.paddingLeft + sp.paddingRight));

				return Mathf.Max(base.minWidth, ((min & 1) == 1) ? min + 1 : min);
			}
			return base.minWidth;
		}
	}

	/// <summary>
	/// Minimum allowed height for this widget.
	/// </summary>

	override public int minHeight
	{
		get
		{
			if (type == Type.Sliced || type == Type.Advanced)
			{
				float ps = pixelSize;
				Vector4 b = border * pixelSize;
				int min = Mathf.RoundToInt(b.y + b.w);

				UISpriteData sp = GetAtlasSprite();
				if (sp != null) min += Mathf.RoundToInt(ps * (sp.paddingTop + sp.paddingBottom));

				return Mathf.Max(base.minHeight, ((min & 1) == 1) ? min + 1 : min);
			}
			return base.minHeight;
		}
	}

	/// <summary>
	/// Sprite's dimensions used for drawing. X = left, Y = bottom, Z = right, W = top.
	/// This function automatically adds 1 pixel on the edge if the sprite's dimensions are not even.
	/// It's used to achieve pixel-perfect sprites even when an odd dimension sprite happens to be centered.
	/// </summary>

	public override Vector4 drawingDimensions
	{
		get
		{
			var offset = pivotOffset;
			var x0 = -offset.x * mWidth;
			var y0 = -offset.y * mHeight;
			var x1 = x0 + mWidth;
			var y1 = y0 + mHeight;

			if (GetAtlasSprite() != null && mType != Type.Tiled)
			{
				var padLeft = mSprite.paddingLeft;
				var padBottom = mSprite.paddingBottom;
				var padRight = mSprite.paddingRight;
				var padTop = mSprite.paddingTop;

				if (mType != Type.Simple)
				{
					float ps = pixelSize;

					if (ps != 1f)
					{
						padLeft = Mathf.RoundToInt(ps * padLeft);
						padBottom = Mathf.RoundToInt(ps * padBottom);
						padRight = Mathf.RoundToInt(ps * padRight);
						padTop = Mathf.RoundToInt(ps * padTop);
					}
				}

				var w = mSprite.width + padLeft + padRight;
				var h = mSprite.height + padBottom + padTop;
				var px = 1f;
				var py = 1f;

				if (w > 0 && h > 0 && (mType == Type.Simple || mType == Type.Filled))
				{
					if ((w & 1) != 0) ++padRight;
					if ((h & 1) != 0) ++padTop;

					px = (1f / w) * mWidth;
					py = (1f / h) * mHeight;
				}

				if (mFlip == Flip.Horizontally || mFlip == Flip.Both)
				{
					x0 += padRight * px;
					x1 -= padLeft * px;
				}
				else
				{
					x0 += padLeft * px;
					x1 -= padRight * px;
				}

				if (mFlip == Flip.Vertically || mFlip == Flip.Both)
				{
					y0 += padTop * py;
					y1 -= padBottom * py;
				}
				else
				{
					y0 += padBottom * py;
					y1 -= padTop * py;
				}
			}

			if (mDrawRegion.x != 0f || mDrawRegion.y != 0f || mDrawRegion.z != 1f || mDrawRegion.w != 1f)
			{
				float fw, fh;

				if (mFixedAspect)
				{
					fw = 0f;
					fh = 0f;
				}
				else
				{
					var br = (mAtlas != null) ? border * pixelSize : Vector4.zero;
					fw = (br.x + br.z);
					fh = (br.y + br.w);
				}

				var vx = Mathf.Lerp(x0, x1 - fw, mDrawRegion.x);
				var vy = Mathf.Lerp(y0, y1 - fh, mDrawRegion.y);
				var vz = Mathf.Lerp(x0 + fw, x1, mDrawRegion.z);
				var vw = Mathf.Lerp(y0 + fh, y1, mDrawRegion.w);

				return new Vector4(vx, vy, vz, vw);
			}
			return new Vector4(x0, y0, x1, y1);
		}
	}

	/// <summary>
	/// Whether the texture is using a premultiplied alpha material.
	/// </summary>

	public override bool premultipliedAlpha
	{
		get
		{
			var ia = mAtlas as INGUIAtlas;
			if (ia != null) return ia.premultipliedAlpha;
			return false;
		}
	}

	/// <summary>
	/// Retrieve the atlas sprite referenced by the spriteName field.
	/// </summary>

	public UISpriteData GetAtlasSprite ()
	{
		if (!mSpriteSet) mSprite = null;

		if (mSprite == null)
		{
			var ia = mAtlas as INGUIAtlas;

			if (ia != null)
			{
				if (!string.IsNullOrEmpty(mSpriteName))
				{
					var sp = ia.GetSprite(mSpriteName);
					if (sp == null) return null;
					SetAtlasSprite(sp);
				}

				if (mSprite == null && ia.spriteList.Count > 0)
				{
					var sp = ia.spriteList[0];
					if (sp == null) return null;
					SetAtlasSprite(sp);

					if (mSprite == null)
					{
						Debug.LogError((ia as Object).name + " seems to have a null sprite!");
						return null;
					}
					mSpriteName = mSprite.name;
				}
			}
		}
		return mSprite;
	}

	/// <summary>
	/// Set the atlas sprite directly.
	/// </summary>

	protected void SetAtlasSprite (UISpriteData sp)
	{
		mChanged = true;
		mSpriteSet = true;

		if (sp != null)
		{
			mSprite = sp;
			mSpriteName = mSprite.name;
		}
		else
		{
			mSpriteName = (mSprite != null) ? mSprite.name : "";
			mSprite = sp;
		}
	}

	/// <summary>
	/// Adjust the scale of the widget to make it pixel-perfect.
	/// </summary>

	public override void MakePixelPerfect ()
	{
		if (!isValid) return;
		base.MakePixelPerfect();
		if (mType == Type.Tiled) return;

		var sp = GetAtlasSprite();
		if (sp == null) return;

		var tex = mainTexture;
		if (tex == null) return;

		if (mType == Type.Simple || mType == Type.Filled || !sp.hasBorder)
		{
			if (tex != null)
			{
				int x = Mathf.RoundToInt(pixelSize * (sp.width + sp.paddingLeft + sp.paddingRight));
				int y = Mathf.RoundToInt(pixelSize * (sp.height + sp.paddingTop + sp.paddingBottom));

				if ((x & 1) == 1) ++x;
				if ((y & 1) == 1) ++y;

				width = x;
				height = y;
			}
		}
	}

	/// <summary>
	/// Auto-upgrade.
	/// </summary>

	protected override void OnInit ()
	{
		if (!mFillCenter)
		{
			mFillCenter = true;
			centerType = AdvancedType.Invisible;
#if UNITY_EDITOR
			NGUITools.SetDirty(this);
#endif
		}
		base.OnInit();
	}

	/// <summary>
	/// Update the UV coordinates.
	/// </summary>

	protected override void OnUpdate ()
	{
		base.OnUpdate();

		if (mChanged || !mSpriteSet)
		{
			mSpriteSet = true;
			mSprite = null;
			mChanged = true;
		}

		if (mFixedAspect)
		{
			if ((!mSpriteSet || mSprite == null) && GetAtlasSprite() == null) return;

			if (mSprite != null)
			{
				var padLeft = mSprite.paddingLeft;
				var padBottom = mSprite.paddingBottom;
				var padRight = mSprite.paddingRight;
				var padTop = mSprite.paddingTop;

				int w = Mathf.RoundToInt(mSprite.width);
				int h = Mathf.RoundToInt(mSprite.height);

				w += padLeft + padRight;
				h += padTop + padBottom;

				float widgetWidth = mWidth;
				float widgetHeight = mHeight;
				float widgetAspect = widgetWidth / widgetHeight;
				float textureAspect = (float)w / h;

				if (textureAspect < widgetAspect)
				{
					float x = (widgetWidth - widgetHeight * textureAspect) / widgetWidth * 0.5f;
					drawRegion = new Vector4(x, 0f, 1f - x, 1f);
				}
				else
				{
					float y = (widgetHeight - widgetWidth / textureAspect) / widgetHeight * 0.5f;
					drawRegion = new Vector4(0f, y, 1f, 1f - y);
				}
			}
		}
	}

	/// <summary>
	/// Virtual function called by the UIPanel that fills the buffers.
	/// </summary>

	public override void OnFill (List<Vector3> verts, List<Vector2> uvs, List<Color> cols)
	{
		var tex = mainTexture;
		if (tex == null) return;

		if ((!mSpriteSet || mSprite == null) && GetAtlasSprite() == null) return;

		var outer = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
		var inner = new Rect(mSprite.x + mSprite.borderLeft, mSprite.y + mSprite.borderTop,
			mSprite.width - mSprite.borderLeft - mSprite.borderRight,
			mSprite.height - mSprite.borderBottom - mSprite.borderTop);

		outer = NGUIMath.ConvertToTexCoords(outer, tex.width, tex.height);
		inner = NGUIMath.ConvertToTexCoords(inner, tex.width, tex.height);

		var offset = verts.Count;
		Fill(verts, uvs, cols, outer, inner);

		if (onPostFill != null)
			onPostFill(this, offset, verts, uvs, cols);
	}
}
