//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

// Dynamic font support contributed by the NGUI community members:
// Unisip, zh4ox, Mudwiz, Nicki, DarkMagicCK.

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to view and edit UIFonts.
/// </summary>

[CustomEditor(typeof(NGUIFont))]
public class NGUIFontInspector : Editor
{
	enum View
	{
		Nothing,
		Atlas,
		Font,
	}

	static View mView = View.Font;
	static bool mUseShader = false;

	INGUIFont mFont;
	INGUIFont mReplacement = null;
	string mSymbolSequence = "";
	string mSymbolSprite = "";
	bool mSymbolColored = false;
	bool mSymPP = false;
	BMSymbol mSelectedSymbol = null;
	char mKernLeft;
	char mKernRight;
	int mKernOffset;
	AnimationCurve mCurve = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1f, 1f));

	public override bool HasPreviewGUI () { return mView != View.Nothing; }

	void OnEnable () { mFont = target as INGUIFont; }

	void OnSelectFont (Object obj)
	{
		// Legacy font support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIFont>();

		// Undo doesn't work correctly in this case... so I won't bother.
		//NGUIEditorTools.RegisterUndo("Font Change");
		//NGUIEditorTools.RegisterUndo("Font Change", mFont);

		mFont.replacement = obj as INGUIFont;
		mReplacement = mFont.replacement;
		NGUITools.SetDirty(mFont as Object);
	}

	void OnSelectAtlas (Object obj)
	{
		// Legacy atlas support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIAtlas>();

		if (mFont != null && mFont.atlas != obj as INGUIAtlas)
		{
			NGUIEditorTools.RegisterUndo("Font Atlas", mFont as Object);
			mFont.atlas = obj as INGUIAtlas;
			MarkAsChanged();
		}
	}

	void OnSelectSymbolAtlas (Object obj)
	{
		if (mFont != null)
		{
			var font = mFont as NGUIFont;

			if (font != null && font.symbolAtlas != obj as INGUIAtlas)
			{
				NGUIEditorTools.RegisterUndo("Symbol Atlas", font);
				font.symbolAtlas = obj as INGUIAtlas;
				MarkAsChanged();
			}
		}
	}

	void MarkAsChanged ()
	{
		var labels = NGUIEditorTools.FindAll<UILabel>();

		foreach (UILabel lbl in labels)
		{
			if (NGUITools.CheckIfRelated(lbl.font as INGUIFont, mFont))
			{
				lbl.font = null;
				lbl.font = mFont;
			}
		}
	}

	public override void OnInspectorGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);

		GUILayout.Space(6f);

		var type = mFont.type;
		if (type == NGUIFontType.Reference) mReplacement = mFont.replacement;

		GUI.changed = false;
		GUILayout.BeginHorizontal();
		type = (NGUIFontType)EditorGUILayout.EnumPopup("Font Type", type);
		GUILayout.EndHorizontal();

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Font Change", mFont as Object);
			mFont.type = type;
			NGUITools.SetDirty(mFont as Object);
		}

		if (type == NGUIFontType.Reference)
		{
			ComponentSelector.Draw(mFont.replacement, OnSelectFont, true);

			GUILayout.Space(6f);
			EditorGUILayout.HelpBox("Reference fonts will have their font data come from another one. " +
				"This allows you to have your labels reference a font that has all the emoticons, that also points to a secondary font containing the actual glyph data " +
				"that can be easily swapped at run-time with a font containing glyphs of another language.", MessageType.Info);

			if (mReplacement != mFont && mFont.replacement != mReplacement)
			{
				NGUIEditorTools.RegisterUndo("Font Change", mFont as Object);
				mFont.replacement = mReplacement;
				NGUITools.SetDirty(mFont as Object);
			}
		}

		if (type == NGUIFontType.Dynamic)
		{
#if UNITY_3_5
			EditorGUILayout.HelpBox("Dynamic fonts require Unity 4.0 or higher.", MessageType.Error);
#else
			Font fnt = EditorGUILayout.ObjectField("TTF Font", mFont.dynamicFont, typeof(Font), false) as Font;

			if (fnt != mFont.dynamicFont)
			{
				NGUIEditorTools.RegisterUndo("Font change", mFont as Object);
				mFont.dynamicFont = fnt;
			}

			var mat = EditorGUILayout.ObjectField("Material", mFont.material, typeof(Material), false) as Material;

			if (mFont.material != mat)
			{
				NGUIEditorTools.RegisterUndo("Font Material", mFont as Object);
				mFont.material = mat;
			}
#endif
		}

		if (type == NGUIFontType.Bitmap)
		{
			ComponentSelector.Draw(mFont.atlas, OnSelectAtlas, true);

			if (mFont.atlas != null)
			{
				if (mFont.bmFont.isValid) NGUIEditorTools.DrawAdvancedSpriteField(mFont.atlas, mFont.spriteName, SelectSprite, false);
			}
			else
			{
				// No atlas specified -- set the material and texture rectangle directly
				var mat = EditorGUILayout.ObjectField("Material", mFont.material, typeof(Material), false) as Material;

				if (mFont.material != mat)
				{
					NGUIEditorTools.RegisterUndo("Font Material", mFont as Object);
					mFont.material = mat;
				}
			}
		}

		if (type != NGUIFontType.Reference)
		{
			GUI.changed = false;
			//EditorGUI.BeginDisabledGroup(mFont.isDynamic);
			var size = EditorGUILayout.IntField("Default Size", mFont.defaultSize, GUILayout.Width(120f));
			//EditorGUI.EndDisabledGroup();

			GUILayout.BeginHorizontal();
			int space = EditorGUILayout.IntField("Space Width", mFont.spaceWidth, GUILayout.Width(120f));
			if (space == 0) GUILayout.Label("(default)");
			GUILayout.EndHorizontal();

			if (GUI.changed)
			{
				NGUIEditorTools.RegisterUndo("Font change", mFont as Object);
				mFont.spaceWidth = space;
				mFont.defaultSize = size;

				var labels = NGUITools.FindActive<UILabel>();

				foreach (UILabel lbl in labels)
				{
					if (lbl.font == null) continue;

					var font = lbl.font;

					if (NGUITools.CheckIfRelated(font, mFont))
					{
						lbl.font = null;
						lbl.font = font;
						NGUITools.SetDirty(lbl);
					}
				}
			}

			if (mFont is NGUIFont)
			{
				var font = mFont as NGUIFont;
				var df = font.dynamicFont;
				var kc = font.kerningCount;

				EditorGUILayout.LabelField("Kerning", kc + " entries", GUILayout.Width(160f));

				if (NGUIEditorTools.DrawHeader("Kerning Data"))
				{
					NGUIEditorTools.BeginContents();

					GUI.changed = false;
					GUILayout.BeginHorizontal();
					var s0 = GUILayout.TextField(mKernLeft != 0 ? mKernLeft.ToString() : "", GUILayout.Width(30f));
					var s1 = GUILayout.TextField(mKernRight != 0 ? mKernRight.ToString() : "", GUILayout.Width(30f));

					if (GUI.changed)
					{
						mKernLeft = string.IsNullOrEmpty(s0) ? (char)0 : s0[0];
						mKernRight = string.IsNullOrEmpty(s1) ? (char)0 : s1[0];
						mKernOffset = font.GetKerning(mKernLeft, mKernRight);
					}

					if (mKernLeft != 0 && mKernRight != 0)
					{
						var i0 = EditorGUILayout.IntSlider(mKernOffset, -9, 9);

						if (i0 != mKernOffset)
						{
							mKernOffset = i0;
							font.SetKerning(mKernLeft, mKernRight, i0);
						}
					}
					else GUILayout.Label("Enter two characters");

					GUILayout.EndHorizontal();

					if (df != null)
					{
						if (kc == 0)
						{
							var add = GUILayout.Button("Auto-fill from font");

							var chars = NGUISettings.charsToInclude;

							if (string.IsNullOrEmpty(chars))
							{
								for (int i = 33; i < 127; ++i) chars += System.Convert.ToChar(i);
								for (int i = 161; i < 256; ++i) chars += System.Convert.ToChar(i);
							}

							GUI.changed = false;
							chars = EditorGUILayout.TextArea(chars, GUI.skin.textArea, GUILayout.Height(80f));
							if (GUI.changed) NGUISettings.charsToInclude = chars;

							if (add)
							{
								var kerning = FreeType.GetKerning(df, size, 0, chars);
								font.SetKerning(kerning);
							}
						}
						else
						{
							var delete = GUILayout.Button("Delete");

							if (delete && EditorUtility.DisplayDialog("Confirm Delete", "Are you sure you want to delete all kerning data for this font?", "Delete", "Cancel"))
							{
								NGUIEditorTools.RegisterUndo("Delete Kerning Data", font as Object);
								font.SetKerning(null);
								mKernLeft = (char)0;
								mKernRight = (char)0;
								mKernOffset = 0;
							}
						}
					}

					NGUIEditorTools.EndContents();
				}
			}

			EditorGUILayout.Space();
		}

		if (type == NGUIFontType.Dynamic || type == NGUIFontType.Reference)
		{
			var font = mFont as NGUIFont;

			if (font != null)
			{
				if (NGUIEditorTools.DrawHeader("Symbol Atlas"))
				{
					NGUIEditorTools.BeginContents();
					ComponentSelector.Draw(font.symbolAtlas, OnSelectSymbolAtlas, true);
					NGUIEditorTools.EndContents();
				}

				EditorGUILayout.Space();
			}
		}

		// For updating the font's data when importing from an external source, such as the texture packer
		bool resetWidthHeight = false;

		if (type == NGUIFontType.Bitmap && (mFont.atlas != null || mFont.material != null))
		{
			if (NGUIEditorTools.DrawHeader("Import"))
			{
				NGUIEditorTools.BeginContents();
				var data = EditorGUILayout.ObjectField("Import Data", null, typeof(TextAsset), false) as TextAsset;

				if (data != null)
				{
					NGUIEditorTools.RegisterUndo("Import Font Data", mFont as Object);
					BMFontReader.Load(mFont.bmFont, (mFont as Object).name, data.bytes);
					mFont.MarkAsChanged();
					resetWidthHeight = true;
					Debug.Log("Imported " + mFont.bmFont.glyphCount + " characters");
				}
				NGUIEditorTools.EndContents();
			}

			EditorGUILayout.Space();
		}

		if (mFont is UIFont && mFont.bmFont.isValid)
		{
			EditorGUILayout.HelpBox("Legacy font type should be upgraded in order to maintain compatibility with Unity 2018 and newer.", MessageType.Warning, true);

			if (GUILayout.Button("Upgrade"))
			{
				var path = EditorUtility.SaveFilePanelInProject("Save As", (mFont as Object).name + ".asset", "asset", "Save font as...", NGUISettings.currentPath);

				if (!string.IsNullOrEmpty(path))
				{
					NGUISettings.currentPath = System.IO.Path.GetDirectoryName(path);
					var asset = ScriptableObject.CreateInstance<NGUIFont>();
					asset.bmFont = mFont.bmFont;
					asset.symbols = mFont.symbols;
					asset.atlas = mFont.atlas;
					asset.spriteName = mFont.spriteName;
					asset.uvRect = mFont.uvRect;
					asset.defaultSize = mFont.defaultSize;

					var fontName = path.Replace(".asset", "");
					fontName = fontName.Substring(path.LastIndexOfAny(new char[] { '/', '\\' }) + 1);
					asset.name = fontName;

					var existing = AssetDatabase.LoadMainAssetAtPath(path);
					if (existing != null) EditorUtility.CopySerialized(asset, existing);
					else AssetDatabase.CreateAsset(asset, path);

					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

					asset = AssetDatabase.LoadAssetAtPath<NGUIFont>(path);
					NGUISettings.ambigiousFont = asset;
					Selection.activeObject = asset;

					if (asset != null)
					{
						mFont.replacement = asset;
						mFont.MarkAsChanged();
					}
				}
			}
		}

		if (type == NGUIFontType.Bitmap && mFont.bmFont.isValid)
		{
			Texture2D tex = mFont.texture;

			if (tex != null && mFont.atlas == null)
			{
				// Pixels are easier to work with than UVs
				Rect pixels = NGUIMath.ConvertToPixels(mFont.uvRect, tex.width, tex.height, false);

				// Automatically set the width and height of the rectangle to be the original font texture's dimensions
				if (resetWidthHeight)
				{
					pixels.width = mFont.texWidth;
					pixels.height = mFont.texHeight;
				}

				// Font sprite rectangle
				pixels = EditorGUILayout.RectField("Pixel Rect", pixels);

				// Convert the pixel coordinates back to UV coordinates
				Rect uvRect = NGUIMath.ConvertToTexCoords(pixels, tex.width, tex.height);

				if (mFont.uvRect != uvRect)
				{
					NGUIEditorTools.RegisterUndo("Font Pixel Rect", mFont as Object);
					mFont.uvRect = uvRect;
				}
				//NGUIEditorTools.DrawSeparator();
				EditorGUILayout.Space();
			}
		}

		var nguiFont = mFont as NGUIFont;
		var symbolAtlas = (nguiFont != null) ? nguiFont.symbolAtlas : null;
		if (symbolAtlas == null && type != NGUIFontType.Reference) symbolAtlas = mFont.atlas;

		if (symbolAtlas != null && NGUIEditorTools.DrawHeader("Symbols and Emoticons"))
		{
			NGUIEditorTools.BeginContents();

			var symbols = mFont.symbols;

			for (int i = 0; i < symbols.Count; )
			{
				var sym = symbols[i];

				GUILayout.BeginHorizontal();
				GUILayout.Label(sym.sequence, GUILayout.Width(40f));

				if (NGUIEditorTools.DrawSpriteField(symbolAtlas, sym.spriteName, ChangeSymbolSprite, GUILayout.MinWidth(100f))) mSelectedSymbol = sym;

				var col = GUILayout.Toggle(sym.colored, new GUIContent("", "Whether the symbol should be affected by the label's color or not"), GUILayout.Width(16f));

				if (col != sym.colored)
				{
					sym.colored = col;
					mFont.MarkAsChanged();
				}

				if (GUILayout.Button("Edit", GUILayout.Width(40f)) && symbolAtlas != null)
				{
					NGUISettings.atlas = symbolAtlas;
					NGUISettings.selectedSprite = sym.spriteName;
					NGUIEditorTools.Select(symbolAtlas as Object);
				}

				GUI.backgroundColor = Color.red;

				if (GUILayout.Button("X", GUILayout.Width(22f)))
				{
					NGUIEditorTools.RegisterUndo("Remove symbol", mFont as Object);
					mSymbolSequence = sym.sequence;
					mSymbolSprite = sym.spriteName;
					symbols.Remove(sym);
					mFont.MarkAsChanged();
				}
				GUI.backgroundColor = Color.white;
				GUILayout.EndHorizontal();
				//GUILayout.Space(4f);
				++i;
			}

			if (symbols.Count > 0)
			{
				if (Event.current.type == EventType.Repaint)
				{
					Texture2D tex = NGUIEditorTools.blankTexture;
					Rect rect = GUILayoutUtility.GetLastRect();
					GUI.color = new Color(0f, 0f, 0f, 0.25f);
					GUI.DrawTexture(new Rect(20f, rect.yMin + 28f, Screen.width - 30f, 1f), tex);
					GUI.color = Color.white;
				}

				GUILayout.Space(10f);
			}

			GUILayout.BeginHorizontal();
			GUI.contentColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.7f) : new Color(0f, 0f, 0f, 0.7f);
			GUILayout.Label(" Add a New Symbol", "PreToolbar2", GUILayout.MinWidth(20f));
			GUI.contentColor = Color.white;
			GUILayout.EndHorizontal();

			GUILayout.Space(2f);

			GUILayout.BeginHorizontal();
			mSymbolSequence = EditorGUILayout.TextField(mSymbolSequence, GUILayout.Width(40f));
			NGUIEditorTools.DrawSpriteField(symbolAtlas, mSymbolSprite, SelectSymbolSprite);

			bool isValid = !string.IsNullOrEmpty(mSymbolSequence) && !string.IsNullOrEmpty(mSymbolSprite);
			GUI.backgroundColor = isValid ? Color.green : Color.grey;

			if (GUILayout.Button("Add", GUILayout.Width(40f)) && isValid)
			{
				NGUIEditorTools.RegisterUndo("Add symbol", mFont as Object);
				var sym = mFont.AddSymbol(mSymbolSequence, mSymbolSprite);
				sym.colored = mSymbolColored;
				sym.pixelPerfect = mSymPP;
				mFont.MarkAsChanged();
				mSymbolSequence = "";
				mSymbolSprite = "";
			}
			GUI.backgroundColor = Color.white;
			GUILayout.EndHorizontal();

			NGUIEditorTools.SetLabelWidth(46f);

			GUILayout.BeginHorizontal();
			mSymbolColored = EditorGUILayout.Toggle("Tinted", mSymbolColored, GUILayout.Width(60f));
			GUILayout.Label("- affected by label's color");
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			mSymPP = !EditorGUILayout.Toggle("Scaled", !mSymPP, GUILayout.Width(60f));
			GUILayout.Label("- affected by label's scaling");
			GUILayout.EndHorizontal();

			NGUIEditorTools.SetLabelWidth(80f);

			if (symbols.Count == 0)
			{
				EditorGUILayout.HelpBox("Want to add an emoticon to your font? In the field above type ':)', choose a sprite, then hit the Add button.", MessageType.Info);
			}
			else GUILayout.Space(4f);

			if (nguiFont != null)
			{
				if (Event.current.type == EventType.Repaint)
				{
					Texture2D tex = NGUIEditorTools.blankTexture;
					Rect rect = GUILayoutUtility.GetLastRect();
					GUI.color = new Color(0f, 0f, 0f, 0.25f);
					GUI.DrawTexture(new Rect(20f, rect.yMin + 6f, Screen.width - 30f, 1f), tex);
					GUI.color = Color.white;
				}

				GUILayout.Space(3f);

				GUILayout.BeginHorizontal();
				GUI.contentColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.7f) : new Color(0f, 0f, 0f, 0.7f);
				GUILayout.Label(" Global Modifiers", "PreToolbar2", GUILayout.MinWidth(20f));
				GUI.contentColor = Color.white;
				GUILayout.EndHorizontal();

				GUILayout.Space(2f);

				GUI.changed = false;
				GUILayout.BeginHorizontal();
				GUILayout.Label("Scale", GUILayout.Width(56f));
				var scale = EditorGUILayout.FloatField(nguiFont.symbolScale, GUILayout.Width(40f));
				GUILayout.Label("- scaling multiplier");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Offset", GUILayout.Width(56f));
				var offset = EditorGUILayout.IntField(nguiFont.symbolOffset, GUILayout.Width(40f));
				GUILayout.Label("- vertical offset");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Height", GUILayout.Width(56f));
				var height = EditorGUILayout.IntField(nguiFont.symbolMaxHeight, GUILayout.Width(40f));
				GUILayout.Label("- max symbol height");
				GUILayout.EndHorizontal();

				GUILayout.BeginHorizontal();
				GUILayout.Label("Centered", GUILayout.Width(56f));
				var center = EditorGUILayout.Toggle(nguiFont.symbolCentered, GUILayout.Width(40f));
				GUILayout.Label("- vertical auto-centering");
				GUILayout.EndHorizontal();

				if (GUI.changed)
				{
					NGUIEditorTools.RegisterUndo("Change symbol values", mFont as Object);
					nguiFont.symbolScale = scale;
					nguiFont.symbolOffset = offset;
					nguiFont.symbolMaxHeight = height;
					nguiFont.symbolCentered = center;
					nguiFont.MarkAsChanged();
				}
			}

			NGUIEditorTools.EndContents();
		}

		if (type == NGUIFontType.Bitmap && mFont.bmFont != null && mFont.bmFont.isValid)
		{
			if (NGUIEditorTools.DrawHeader("Modify"))
			{
				NGUIEditorTools.BeginContents();

				UISpriteData sd = mFont.sprite;

				bool disable = (sd != null && (sd.paddingLeft != 0 || sd.paddingBottom != 0));
				EditorGUI.BeginDisabledGroup(disable || mFont.packedFontShader);

				EditorGUILayout.BeginHorizontal();
				GUILayout.Space(20f);
				EditorGUILayout.BeginVertical();

				GUILayout.BeginHorizontal();
				GUILayout.BeginVertical();
				NGUISettings.foregroundColor = EditorGUILayout.ColorField("Foreground", NGUISettings.foregroundColor);
				NGUISettings.backgroundColor = EditorGUILayout.ColorField("Background", NGUISettings.backgroundColor);
				GUILayout.EndVertical();
				mCurve = EditorGUILayout.CurveField("", mCurve, GUILayout.Width(40f), GUILayout.Height(40f));
				GUILayout.EndHorizontal();

				if (GUILayout.Button("Add a Shadow")) ApplyEffect(Effect.Shadow, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Add a Soft Outline")) ApplyEffect(Effect.Outline, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Rebalance Colors")) ApplyEffect(Effect.Rebalance, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Apply Curve to Alpha")) ApplyEffect(Effect.AlphaCurve, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Apply Curve to Foreground")) ApplyEffect(Effect.ForegroundCurve, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Apply Curve to Background")) ApplyEffect(Effect.BackgroundCurve, NGUISettings.foregroundColor, NGUISettings.backgroundColor);

				GUILayout.Space(10f);
				if (GUILayout.Button("Add Transparent Border (+1)")) ApplyEffect(Effect.Border, NGUISettings.foregroundColor, NGUISettings.backgroundColor);
				if (GUILayout.Button("Remove Border (-1)")) ApplyEffect(Effect.Crop, NGUISettings.foregroundColor, NGUISettings.backgroundColor);

				EditorGUILayout.EndVertical();
				GUILayout.Space(20f);
				EditorGUILayout.EndHorizontal();

				EditorGUI.EndDisabledGroup();

				if (disable)
				{
					GUILayout.Space(3f);
					EditorGUILayout.HelpBox("The sprite used by this font has been trimmed and is not suitable for modification. " +
						"Try re-adding this sprite with 'Trim Alpha' disabled.", MessageType.Warning);
				}

				NGUIEditorTools.EndContents();
			}
		}

		if (mFont.atlas == null)
		{
			mView = View.Font;
			mUseShader = false;
		}

		// Preview option
		if (mFont.atlas != null)
		{
			GUILayout.BeginHorizontal();
			{
				mView = (View)EditorGUILayout.EnumPopup("Preview", mView);
				GUILayout.Label("Shader", GUILayout.Width(45f));
				mUseShader = EditorGUILayout.Toggle(mUseShader, GUILayout.Width(20f));
			}
			GUILayout.EndHorizontal();
		}
	}

	/// <summary>
	/// "New Sprite" selection.
	/// </summary>

	void SelectSymbolSprite (string spriteName)
	{
		mSymbolSprite = spriteName;
		Repaint();
	}

	/// <summary>
	/// Existing sprite selection.
	/// </summary>

	void ChangeSymbolSprite (string spriteName)
	{
		if (mSelectedSymbol != null && mFont != null)
		{
			NGUIEditorTools.RegisterUndo("Change symbol", mFont as Object);
			mSelectedSymbol.spriteName = spriteName;
			Repaint();
			mFont.MarkAsChanged();
		}
	}

	/// <summary>
	/// Draw the font preview window.
	/// </summary>

	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		mFont = target as INGUIFont;
		if (mFont == null) return;
		Texture2D tex = mFont.texture;

		if (mView != View.Nothing && tex != null)
		{
			var m = (mUseShader ? mFont.material : null);

			if (mView == View.Font && mFont.atlas != null && mFont.sprite != null)
			{
				NGUIEditorTools.DrawSprite(tex, rect, mFont.sprite, Color.white, m);
			}
			else NGUIEditorTools.DrawTexture(tex, rect, new Rect(0f, 0f, 1f, 1f), Color.white, m);
		}
	}

	/// <summary>
	/// Sprite selection callback.
	/// </summary>

	void SelectSprite (string spriteName)
	{
		NGUIEditorTools.RegisterUndo("Font Sprite", mFont as Object);
		mFont.spriteName = spriteName;
		Repaint();
	}

	enum Effect
	{
		Rebalance,
		ForegroundCurve,
		BackgroundCurve,
		AlphaCurve,
		Border,
		Shadow,
		Outline,
		Crop,
	}

	/// <summary>
	/// Apply an effect to the font.
	/// </summary>

	void ApplyEffect (Effect effect, Color foreground, Color background)
	{
		var bf = mFont.bmFont;
		var offsetX = 0;
		var offsetY = 0;

		if (mFont.atlas != null)
		{
			var sd = mFont.GetSprite(bf.spriteName);
			if (sd == null) return;
			offsetX = sd.x;
			offsetY = sd.y + mFont.texHeight - sd.paddingTop;
		}

		var path = AssetDatabase.GetAssetPath(mFont.texture);
		var bfTex = NGUIEditorTools.ImportTexture(path, true, true, false);
		var atlas = bfTex.GetPixels32();
		var texW = bfTex.width;
		var texH = bfTex.height;

		// First we need to extract textures for all the glyphs, making them bigger in the process
		var glyphs = bf.glyphs;
		var glyphTextures = new List<Texture2D>(glyphs.Count);

		for (int i = 0, imax = glyphs.Count; i < imax; ++i)
		{
			var glyph = glyphs[i];
			if (glyph.width < 1 || glyph.height < 1) continue;

			int width = glyph.width;
			int height = glyph.height;

			if (effect == Effect.Outline || effect == Effect.Shadow || effect == Effect.Border)
			{
				width += 2;
				height += 2;
				--glyph.offsetX;
				--glyph.offsetY;
			}
			else if (effect == Effect.Crop && width > 2 && height > 2)
			{
				width -= 2;
				height -= 2;
				++glyph.offsetX;
				++glyph.offsetY;
			}

			int size = width * height;
			var colors = new Color32[size];
			Color32 clear = background;
			clear.a = 0;
			for (int b = 0; b < size; ++b) colors[b] = clear;

			if (effect == Effect.Crop)
			{
				for (int y = 0; y < height; ++y)
				{
					for (int x = 0; x < width; ++x)
					{
						int fx = x + glyph.x + offsetX + 1;
						int fy = y + (mFont.texHeight - glyph.y - glyph.height) + 1;
						if (mFont.atlas != null) fy += bfTex.height - offsetY;
						fx = Mathf.Clamp(fx, 0, texW - 1);
						fy = Mathf.Clamp(fy, 0, texH - 1);
						colors[x + y * width] = atlas[fx + fy * texW];
					}
				}
			}
			else
			{
				for (int y = 0; y < glyph.height; ++y)
				{
					for (int x = 0; x < glyph.width; ++x)
					{
						int fx = x + glyph.x + offsetX;
						int fy = y + (mFont.texHeight - glyph.y - glyph.height);
						if (mFont.atlas != null) fy += bfTex.height - offsetY;

						fx = Mathf.Clamp(fx, 0, texW - 1);
						fy = Mathf.Clamp(fy, 0, texH - 1);

						Color c = atlas[fx + fy * texW];

						if (effect == Effect.Border)
						{
							colors[x + 1 + (y + 1) * width] = c;
						}
						else
						{
							if (effect == Effect.AlphaCurve) c.a = Mathf.Clamp01(mCurve.Evaluate(c.a));

							Color bg = background;
							bg.a = (effect == Effect.BackgroundCurve) ? Mathf.Clamp01(mCurve.Evaluate(c.a)) : c.a;

							Color fg = foreground;
							fg.a = (effect == Effect.ForegroundCurve) ? Mathf.Clamp01(mCurve.Evaluate(c.a)) : c.a;

							if (effect == Effect.Outline || effect == Effect.Shadow)
							{
								colors[x + 1 + (y + 1) * width] = Color.Lerp(bg, c, c.a);
							}
							else
							{
								colors[x + y * width] = Color.Lerp(bg, fg, c.a);
							}
						}
					}
				}

				// Apply the appropriate affect
				if (effect == Effect.Shadow) NGUIEditorTools.AddShadow(colors, width, height, NGUISettings.backgroundColor);
				else if (effect == Effect.Outline) NGUIEditorTools.AddDepth(colors, width, height, NGUISettings.backgroundColor);
			}

			var tex = new Texture2D(width, height, TextureFormat.ARGB32, false);
			tex.SetPixels32(colors);
			tex.Apply();
			glyphTextures.Add(tex);
		}

		// Pack all glyphs into a new texture
		var final = new Texture2D(texW, texH, TextureFormat.ARGB32, false);
		var rects = final.PackTextures(glyphTextures.ToArray(), 1);
		final.Apply();

		// Make RGB channel use the background color (Unity makes it black by default)
		var fcs = final.GetPixels32();
		Color32 bc = background;

		for (int i = 0, imax = fcs.Length; i < imax; ++i)
		{
			if (fcs[i].a == 0)
			{
				fcs[i].r = bc.r;
				fcs[i].g = bc.g;
				fcs[i].b = bc.b;
			}
		}

		final.SetPixels32(fcs);
		final.Apply();

		// Update the glyph rectangles
		int index = 0;
		int tw = final.width;
		int th = final.height;

		for (int i = 0, imax = glyphs.Count; i < imax; ++i)
		{
			var glyph = glyphs[i];
			if (glyph.width < 1 || glyph.height < 1) continue;

			var rect = rects[index++];
			glyph.x = Mathf.RoundToInt(rect.x * tw);
			glyph.y = Mathf.RoundToInt(rect.y * th);
			glyph.width = Mathf.RoundToInt(rect.width * tw);
			glyph.height = Mathf.RoundToInt(rect.height * th);
			glyph.y = th - glyph.y - glyph.height;
		}

		// Update the font's texture dimensions
		mFont.texWidth = final.width;
		mFont.texHeight = final.height;

		if (mFont.atlas == null)
		{
			// Save the final texture
			var bytes = final.EncodeToPNG();
			NGUITools.DestroyImmediate(final);
			System.IO.File.WriteAllBytes(path, bytes);
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}
		else
		{
			// Update the atlas
			final.name = mFont.spriteName;
			bool val = NGUISettings.atlasTrimming;
			NGUISettings.atlasTrimming = false;
			UIAtlasMaker.AddOrUpdate(mFont.atlas, final);
			NGUISettings.atlasTrimming = val;
			NGUITools.DestroyImmediate(final);
		}

		// Cleanup
		for (int i = 0; i < glyphTextures.Count; ++i) NGUITools.DestroyImmediate(glyphTextures[i]);

		// Refresh all labels
		mFont.MarkAsChanged();
	}
}
