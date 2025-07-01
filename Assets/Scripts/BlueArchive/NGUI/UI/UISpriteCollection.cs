//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Sprite collection is a widget that contains a bunch of sprites that don't create their own game objects and colliders.
/// Its best usage is to replace the need to create individual game objects while still maintaining full visualization
/// and interaction functionality of NGUI's sprites. For example: a world map with thousands of individual icons.
/// The thousands of individual icons can be a single Sprite Collection. Its downside is that the sprites can't be
/// interacted with in the Editor window, as this is meant to be a fast, programmable solution.
/// </summary>

[ExecuteInEditMode]
[AddComponentMenu("NGUI/UI/Sprite Collection")]
public class UISpriteCollection : UIBasicSprite
{
	/// <summary>
	/// Sub-sprite entry within the collection.
	/// </summary>

	public struct Sprite
	{
		public object obj;
		public UISpriteData sprite;
		public Vector2 pos;
		public float rot;
		public float scale;
		public float width;
		public float height;
		public Color32 color;
		public Vector2 pivot;
		public Type type;
		public Flip flip;
		public bool enabled;

		/// <summary>
		/// Calculate the sprite's drawing dimensions.
		/// </summary>

		public Vector4 GetDrawingDimensions (float pixelSize)
		{
			var sw = width * scale;
			var sh = height * scale;
			var x0 = -pivot.x * sw;
			var y0 = -pivot.y * sh;
			var x1 = x0 + sw;
			var y1 = y0 + sh;

			if (sprite != null && type != Type.Tiled)
			{
				var padLeft = sprite.paddingLeft;
				var padBottom = sprite.paddingBottom;
				var padRight = sprite.paddingRight;
				var padTop = sprite.paddingTop;

				if (type != Type.Simple && pixelSize != 1f)
				{
					padLeft = Mathf.RoundToInt(pixelSize * padLeft);
					padBottom = Mathf.RoundToInt(pixelSize * padBottom);
					padRight = Mathf.RoundToInt(pixelSize * padRight);
					padTop = Mathf.RoundToInt(pixelSize * padTop);
				}

				var w = sprite.width + padLeft + padRight;
				var h = sprite.height + padBottom + padTop;
				var px = 1f;
				var py = 1f;

				if (w > 0 && h > 0 && (type == Type.Simple || type == Type.Filled))
				{
					if ((w & 1) != 0) ++padRight;
					if ((h & 1) != 0) ++padTop;

					px = (1f / w) * sw;
					py = (1f / h) * sh;
				}

				if (flip == Flip.Horizontally || flip == Flip.Both)
				{
					x0 += padRight * px;
					x1 -= padLeft * px;
				}
				else
				{
					x0 += padLeft * px;
					x1 -= padRight * px;
				}

				if (flip == Flip.Vertically || flip == Flip.Both)
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
			return new Vector4(x0, y0, x1, y1);
		}
	}

	[HideInInspector, SerializeField] Object mAtlas;

	// List of individual sprites
	[System.NonSerialized] List<Sprite> mSprites = new List<Sprite>();

	// Only valid during the OnFill process
	[System.NonSerialized] UISpriteData mSprite;

	/// <summary>
	/// Main texture is assigned on the atlas.
	/// </summary>

	public override Texture mainTexture
	{
		get
		{
			Material mat = null;
			var ia = atlas;
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

			var ia = atlas;
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
				mSprites.Clear();
				MarkAsChanged();
			}
		}
	}

	/// <summary>
	/// Size of the pixel -- used for drawing.
	/// </summary>

	override public float pixelSize
	{
		get
		{
			var ia = atlas;
			if (ia != null) return ia.pixelSize;
			return 1f;
		}
	}

	/// <summary>
	/// Whether the texture is using a premultiplied alpha material.
	/// </summary>

	public override bool premultipliedAlpha
	{
		get
		{
			var ia = atlas;
			if (ia != null) return ia.premultipliedAlpha;
			return false;
		}
	}

	// <summary>
	/// Sliced sprites generally have a border. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	public override Vector4 border
	{
		get
		{
			if (mSprite == null) return base.border;
			return new Vector4(mSprite.borderLeft, mSprite.borderBottom, mSprite.borderRight, mSprite.borderTop);
		}
	}

	/// <summary>
	/// Trimmed space in the atlas around the sprite. X = left, Y = bottom, Z = right, W = top.
	/// </summary>

	protected override Vector4 padding
	{
		get
		{
			var p = new Vector4(0, 0, 0, 0);

			if (mSprite != null)
			{
				p.x = mSprite.paddingLeft;
				p.y = mSprite.paddingBottom;
				p.z = mSprite.paddingRight;
				p.w = mSprite.paddingTop;
			}
			return p;
		}
	}

	/// <summary>
	/// Fill the draw buffers.
	/// </summary>

	public override void OnFill (List<Vector3> verts, List<Vector2> uvs, List<Color> cols)
	{
		var tex = mainTexture;
		if (tex == null) return;

		int offset = verts.Count;
		var drawRegion = this.drawRegion;

		foreach (var ent in mSprites)
		{
			if (!ent.enabled) continue;

			mSprite = ent.sprite;
			if (mSprite == null) continue;

			Color c = ent.color;
			c.a *= finalAlpha;
			if (c.a == 0f) continue;

			var outer = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
			var inner = new Rect(mSprite.x + mSprite.borderLeft, mSprite.y + mSprite.borderTop,
				mSprite.width - mSprite.borderLeft - mSprite.borderRight,
				mSprite.height - mSprite.borderBottom - mSprite.borderTop);

			mOuterUV = NGUIMath.ConvertToTexCoords(outer, tex.width, tex.height);
			mInnerUV = NGUIMath.ConvertToTexCoords(inner, tex.width, tex.height);
			mFlip = ent.flip;

			var v = ent.GetDrawingDimensions(pixelSize);
			var u = drawingUVs;

			if (premultipliedAlpha) c = NGUITools.ApplyPMA(c);
			var start = verts.Count;

			switch (ent.type)
			{
				case Type.Simple:
				SimpleFill(verts, uvs, cols, ref v, ref u, ref c);
				break;

				case Type.Sliced:
				SlicedFill(verts, uvs, cols, ref v, ref u, ref c);
				break;

				case Type.Filled:
				FilledFill(verts, uvs, cols, ref v, ref u, ref c);
				break;

				case Type.Tiled:
				TiledFill(verts, uvs, cols, ref v, ref c);
				break;

				case Type.Advanced:
				AdvancedFill(verts, uvs, cols, ref v, ref u, ref c);
				break;
			}

			if (ent.rot != 0f)
			{
				var dz = ent.rot * Mathf.Deg2Rad;
				var halfZ = dz * 0.5f;
				var sinz = Mathf.Sin(halfZ);
				var cosz = Mathf.Cos(halfZ);
				var num3 = sinz * 2f;
				var num6 = sinz * num3;
				var num12 = cosz * num3;

				for (int i = start, imax = verts.Count; i < imax; ++i)
				{
					var pos = verts[i];
					pos = new Vector3((1f - num6) * pos.x - num12 * pos.y, num12 * pos.x + (1f - num6) * pos.y, pos.z);
					pos.x += ent.pos.x;
					pos.y += ent.pos.y;
					verts[i] = pos;
				}
			}
			else
			{
				for (int i = start, imax = verts.Count; i < imax; ++i)
				{
					var pos = verts[i];
					pos.x += ent.pos.x;
					pos.y += ent.pos.y;
					verts[i] = pos;
				}
			}
		}

		mSprite = null;

		if (onPostFill != null)
			onPostFill(this, offset, verts, uvs, cols);
	}

	/// <summary>
	/// Add a new sprite entry to the collection.
	/// </summary>

	public void Add (object obj, in string spriteName, Vector2 pos, float width, float height)
	{
		AddSprite(obj, spriteName, pos, width, height, new Color32(255, 255, 255, 255), new Vector2(0.5f, 0.5f));
	}

	/// <summary>
	/// Add a new sprite entry to the collection.
	/// </summary>

	public void Add (object obj, in string spriteName, Vector2 pos, float width, float height, Color32 color)
	{
		AddSprite(obj, spriteName, pos, width, height, color, new Vector2(0.5f, 0.5f));
	}

	/// <summary>
	/// Add a new sprite entry to the collection. Kept for backwards compatibility.
	/// </summary>

	[System.Obsolete("Use AddSprite that specified the scale")]
	public void AddSprite (object id, in string spriteName, Vector2 pos, float width, float height, Color32 color, Vector2 pivot, float rot, Type type, Flip flip, bool enabled = true)
	{
		AddSprite(id, spriteName, pos, width, height, color, pivot, rot, 1f, type, flip, enabled);
	}

	/// <summary>
	/// Add a new sprite entry to the collection.
	/// </summary>

	public void AddSprite (object id, in string spriteName, Vector2 pos, float width, float height, Color32 color, Vector2 pivot,
		float rot = 0f, float scale = 1f, Type type = Type.Simple, Flip flip = Flip.Nothing, bool enabled = true)
	{
		if (mAtlas == null)
		{
			Debug.LogError("Atlas must be assigned first");
			return;
		}

		var sprite = new Sprite();

		var ia = atlas;
		if (ia != null) sprite.sprite = ia.GetSprite(spriteName);
		if (sprite.sprite == null) return;

		sprite.obj = id;
		sprite.pos = pos;
		sprite.rot = rot;
		sprite.scale = scale;
		sprite.width = width;
		sprite.height = height;
		sprite.color = color;
		sprite.pivot = pivot;
		sprite.type = type;
		sprite.flip = flip;
		sprite.enabled = enabled;
		mSprites.Add(sprite);

		if (enabled && !mChanged) MarkAsChanged();
	}

	/// <summary>
	/// Move the specified object to the forefront (so that it's rendered last).
	/// </summary>

	public bool MoveToFront (object id)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				var sp = mSprites[i];
				mSprites.RemoveAt(i);
				mSprites.Add(sp);
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Retrieve an existing sprite.
	/// </summary>

	public Sprite? GetSprite (object id)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i) if (mSprites[i].obj == id) return mSprites[i];
		return null;
	}

	/// <summary>
	/// Remove a previously added sprite.
	/// </summary>

	public bool RemoveSprite (object id)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				mSprites.RemoveAt(i);
				if (!mChanged) MarkAsChanged();
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Update the specified sprite.
	/// </summary>

	public bool SetSprite (Sprite sp)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == sp.obj)
			{
				mSprites[i] = sp;
				if (!mChanged) MarkAsChanged();
				return true;
			}
		}

		mSprites.Add(sp);
		if (!mChanged) MarkAsChanged();
		return true;
	}

	/// <summary>
	/// Clear all sprite entries.
	/// </summary>

	[ContextMenu("Clear")]
	public void Clear ()
	{
		if (mSprites.Count != 0)
		{
			mSprites.Clear();
			MarkAsChanged();
		}
	}

	/// <summary>
	/// Returns whether the specified sprite is present and is visible.
	/// </summary>

	public bool IsActive (object id)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i) if (mSprites[i].obj == id) return mSprites[i].enabled;
		return false;
	}

	/// <summary>
	/// Set the specified sprite's enabled state.
	/// </summary>

	public bool SetActive (object id, bool visible)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				var sp = mSprites[i];

				if (sp.enabled != visible)
				{
					sp.enabled = visible;
					mSprites[i] = sp;
					if (!mChanged) MarkAsChanged();
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Set the sprite's position.
	/// </summary>

	public bool SetPosition (object id, Vector2 pos, bool visible = true)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				var sp = mSprites[i];

				if (sp.pos != pos || sp.enabled != visible)
				{
					sp.pos = pos;
					sp.enabled = visible;
					mSprites[i] = sp;
					if (!mChanged) MarkAsChanged();
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Set the sprite's position, rotation, scale and alpha.
	/// </summary>

	public bool SetSprite (object id, Vector2 pos, float rot, float scale, float alpha = 1f)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				var sp = mSprites[i];
				var ba = (byte)Mathf.RoundToInt(alpha * 255f);

				if (sp.pos != pos || sp.rot != rot || sp.scale != scale || (sp.enabled ? sp.color.a : 0) != ba)
				{
					sp.pos = pos;
					sp.rot = rot;
					sp.scale = scale;
					sp.color.a = ba;
					sp.enabled = (alpha != 0f);
					mSprites[i] = sp;
					if (!mChanged) MarkAsChanged();
				}
				return true;
			}
		}
		return false;
	}

	/// <summary>
	/// Set the sprite's position, rotation, scale and color.
	/// </summary>

	public bool SetSprite (object id, Vector2 pos, float rot, float scale, Color color)
	{
		for (int i = 0, imax = mSprites.Count; i < imax; ++i)
		{
			if (mSprites[i].obj == id)
			{
				var sp = mSprites[i];

				if (sp.pos != pos || sp.rot != rot || sp.scale != scale || sp.color != color)
				{
					sp.pos = pos;
					sp.rot = rot;
					sp.scale = scale;
					sp.color = color;
					sp.enabled = (alpha != 0f);
					mSprites[i] = sp;
					if (!mChanged) MarkAsChanged();
				}
				return true;
			}
		}
		return false;
	}

	#region Event handling
	public delegate void OnHoverCB (object obj, bool isOver);
	public delegate void OnPressCB (object obj, bool isPressed);
	public delegate void OnClickCB (object obj);
	public delegate void OnDragCB (object obj, Vector2 delta);
	public delegate void OnTooltipCB (object obj, bool show);

	public OnHoverCB onHover;
	public OnPressCB onPress;
	public OnClickCB onClick;
	public OnDragCB onDrag;
	public OnTooltipCB onTooltip;

	[System.NonSerialized] object mLastHover;
	[System.NonSerialized] object mLastPress;
	[System.NonSerialized] object mLastTooltip;

	static Vector2 Rotate (Vector2 pos, float rot)
	{
		var halfZ = rot * Mathf.Deg2Rad * 0.5f;
		var sinz = Mathf.Sin(halfZ);
		var cosz = Mathf.Cos(halfZ);
		var num3 = sinz * 2f;
		var num6 = sinz * num3;
		var num12 = cosz * num3;
		return new Vector2((1f - num6) * pos.x - num12 * pos.y, num12 * pos.x + (1f - num6) * pos.y);
	}

	/// <summary>
	/// Return the sprite underneath the current event position.
	/// </summary>

	public object GetCurrentSpriteID () { return GetCurrentSpriteID(UICamera.lastWorldPosition); }

	/// <summary>
	/// Return the sprite underneath the current event position.
	/// </summary>

	public Sprite? GetCurrentSprite () { return GetCurrentSprite(UICamera.lastWorldPosition); }

	/// <summary>
	/// Return the sprite underneath the specified world position.
	/// </summary>

	public object GetCurrentSpriteID (Vector3 worldPos)
	{
		var pos = (Vector2)mTrans.InverseTransformPoint(worldPos);

		for (int i = mSprites.Count; i > 0; )
		{
			var ent = mSprites[--i];
			var v = pos - ent.pos;
			if (ent.rot != 0f) v = Rotate(v, -ent.rot);

			var dims = ent.GetDrawingDimensions(pixelSize);

			if (v.x < dims.x) continue;
			if (v.y < dims.y) continue;
			if (v.x > dims.z) continue;
			if (v.y > dims.w) continue;

			return ent.obj;
		}
		return null;
	}

	/// <summary>
	/// Return the sprite underneath the specified world position.
	/// </summary>

	public Sprite? GetCurrentSprite (Vector3 worldPos)
	{
		var pos = (Vector2)mTrans.InverseTransformPoint(worldPos);

		for (int i = mSprites.Count; i > 0;)
		{
			var ent = mSprites[--i];
			var v = pos - ent.pos;
			if (ent.rot != 0f) v = Rotate(v, -ent.rot);

			var dims = ent.GetDrawingDimensions(pixelSize);

			if (v.x < dims.x) continue;
			if (v.y < dims.y) continue;
			if (v.x > dims.z) continue;
			if (v.y > dims.w) continue;

			return ent;
		}
		return null;
	}

	protected void OnClick ()
	{
		if (onClick != null)
		{
			var sp = GetCurrentSpriteID();
			if (sp != null) onClick(sp);
		}
	}

	protected void OnPress (bool isPressed)
	{
		if (onPress != null)
		{
			// Only support one event source at a time (no multi touch)
			if (isPressed && mLastPress != null) return;

			if (isPressed)
			{
				mLastPress = GetCurrentSpriteID();
				if (mLastPress != null) onPress(mLastPress, true);
			}
			else if (mLastPress != null)
			{
				onPress(mLastPress, false);
				mLastPress = null;
			}
		}
	}

	protected void OnHover (bool isOver)
	{
		if (onHover != null)
		{
			if (isOver)
			{
				UICamera.onMouseMove += OnMove;
				OnMove(Vector2.zero);
			}
			else UICamera.onMouseMove -= OnMove;
		}
	}

	protected void OnMove (Vector2 delta)
	{
		if (!this || onHover == null) return;

		var sp = GetCurrentSpriteID();

		if (mLastHover != sp)
		{
			if (mLastHover != null) onHover(mLastHover, false);
			mLastHover = sp;
			if (mLastHover != null) onHover(mLastHover, true);
		}
	}

	protected void OnDrag (Vector2 delta) { if (onDrag != null && mLastPress != null) onDrag(mLastPress, delta); }

	protected void OnTooltip (bool show)
	{
		if (onTooltip != null)
		{
			if (show)
			{
				if (mLastTooltip != null) onTooltip(mLastTooltip, false);
				mLastTooltip = GetCurrentSpriteID();
				if (mLastTooltip != null) onTooltip(mLastTooltip, true);
			}
			else
			{
				onTooltip(mLastTooltip, false);
				mLastTooltip = null;
			}
		}
	}
#endregion
}
