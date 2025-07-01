using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Custom NGUI widget that draws a circle.
/// </summary>

public class UICircle : UISprite
{
	[Range(0.001f, 1f), Tooltip("Controls the thickness of the circle")]
	public float thickness = 0.1f;

	[Range(16, 180), Tooltip("How many slices the circle will be made out of")]
	public int slices = 90;

	// Do nothing
	public override void MakePixelPerfect () { }

	public override void OnFill (List<Vector3> verts, List<Vector2> uvs, List<Color> cols)
	{
		var tex = mainTexture;
		if (tex == null) return;
		if (mSprite == null && GetAtlasSprite() == null) return;

		var outer = new Rect(mSprite.x, mSprite.y, mSprite.width, mSprite.height);
		var inner = new Rect(mSprite.x + mSprite.borderLeft, mSprite.y + mSprite.borderTop,
			mSprite.width - mSprite.borderLeft - mSprite.borderRight,
			mSprite.height - mSprite.borderBottom - mSprite.borderTop);

		outer = NGUIMath.ConvertToTexCoords(outer, tex.width, tex.height);
		inner = NGUIMath.ConvertToTexCoords(inner, tex.width, tex.height);

		var uv0 = new Vector2(inner.xMin, outer.yMin);
		var uv1 = new Vector2(inner.xMax, outer.yMax);

		var offset = verts.Count;
		var po = pivotOffset;
		var x0 = -po.x * mWidth;
		var y0 = -po.y * mHeight;
		var x1 = x0 + mWidth;
		var y1 = y0 + mHeight;

		var v = new Vector4(x0, y0, x1, y1);
		var c = drawingColor;
		var centerV = new Vector3((v.x + v.z) * 0.5f, (v.y + v.w) * 0.5f, 0f);
		var pi2 = Mathf.PI * 2f;

		for (int i = 0; i < slices; ++i)
		{
			var a0 = ((float)i / slices) * pi2;
			var a1 = ((float)(i + 1) / slices) * pi2;

			var c0 = Mathf.Cos(a0) * 0.5f + 0.5f;
			var c1 = Mathf.Cos(a1) * 0.5f + 0.5f;
			var s0 = Mathf.Sin(a0) * 0.5f + 0.5f;
			var s1 = Mathf.Sin(a1) * 0.5f + 0.5f;

			var v0 = new Vector3(Mathf.Lerp(v.x, v.z, c0), Mathf.Lerp(v.y, v.w, s0), 0f);
			var v1 = new Vector3(Mathf.Lerp(v.x, v.z, c1), Mathf.Lerp(v.y, v.w, s1), 0f);
			var v2 = Vector3.Lerp(v0, centerV, thickness);
			var v3 = Vector3.Lerp(v1, centerV, thickness);

			verts.Add(v0);
			verts.Add(v2);
			verts.Add(v3);
			verts.Add(v1);

			uvs.Add(uv1);
			uvs.Add(new Vector2(uv1.x, uv0.y));
			uvs.Add(uv0);
			uvs.Add(new Vector2(uv0.x, uv1.y));

			for (int b = 0; b < 4; ++b) cols.Add(c);
		}

		if (onPostFill != null) onPostFill(this, offset, verts, uvs, cols);
	}
}
