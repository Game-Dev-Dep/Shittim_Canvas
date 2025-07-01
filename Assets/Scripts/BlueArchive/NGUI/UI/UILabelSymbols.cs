//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Used automatically by UILabel when symbols are not in the same atlas as the font. Don't try to add this to anything yourself.
/// </summary>

[HideInInspector]
public class UILabelSymbols : UIWidget
{
	[System.NonSerialized] public UILabel label;
	[System.NonSerialized] public int fillFrame = -1;

	// Filled below when UILabelSymbol.OnFill gets called before UILabel.OnFill
	[System.NonSerialized] public List<Vector3> cacheVerts = new List<Vector3>();
	[System.NonSerialized] public List<Vector2> cacheUVs = new List<Vector2>();
	[System.NonSerialized] public List<Color> cacheCols = new List<Color>();

	// Filled in UILabel.OnFill when it's called before UILabelSymbols.OnFill
	[System.NonSerialized] public List<Vector3> symbolVerts = new List<Vector3>();
	[System.NonSerialized] public List<Vector2> symbolUVs = new List<Vector2>();
	[System.NonSerialized] public List<Color> symbolCols = new List<Color>();

	public override bool isSelectable { get { return false; } }

	public override Material material
	{
		get
		{
			if (label != null && label.separateSymbols)
			{
				var font = label.font as NGUIFont;
				return (font != null) ? font.symbolMaterial : null;
			}
			return null;
		}
	}

	public void ClearCache ()
	{
		cacheVerts.Clear();
		cacheUVs.Clear();
		cacheCols.Clear();
		symbolVerts.Clear();
		symbolUVs.Clear();
		symbolCols.Clear();
		fillFrame = -1;
	}

#if UNITY_EDITOR
	protected override void OnValidate ()
	{
		base.OnValidate();

		if (label != null && label.symbolLabel != this)
		{
			var go = gameObject;

			if (Application.isPlaying)
			{
				go.SetActive(false);
				Destroy(go);
			}
			else DestroyImmediate(go);
		}
	}
#endif

	protected override void OnDisable ()
	{
		base.OnDisable();
		ClearCache();
	}

	public override void OnFill (List<Vector3> verts, List<Vector2> uvs, List<Color> cols)
	{
		var frame = Time.frameCount;

		if (frame != fillFrame)
		{
			ClearCache();
			if (label != null) label.Fill(cacheVerts, cacheUVs, cacheCols, verts, uvs, cols);
			fillFrame = frame;
		}
		else if (symbolVerts.Count != 0)
		{
			verts.AddRange(symbolVerts);
			uvs.AddRange(symbolUVs);
			cols.AddRange(symbolCols);
			ClearCache();
		}
	}
}
