//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Atlas maker lets you create atlases from a bunch of small textures. It's an alternative to using the external Texture Packer.
/// </summary>

public class UIAtlasMaker : EditorWindow
{
	static public UIAtlasMaker instance;

	public class SpriteEntry : UISpriteData
	{
		// Sprite texture -- original texture or a temporary texture
		public Texture2D tex;

		// Temporary game object -- used to prevent Unity from unloading the texture
		public GameObject tempGO;

		// Temporary material -- same usage as the temporary game object
		public Material tempMat;

		// Whether the texture is temporary and should be deleted
		public bool temporaryTexture = false;

		/// <summary>
		/// HACK: Prevent Unity from unloading temporary textures.
		/// Discovered by "alexkring": http://www.tasharen.com/forum/index.php?topic=3079.45
		/// </summary>

		public void SetTexture (Color32[] newPixels, int newWidth, int newHeight)
		{
			Release();

			temporaryTexture = true;

			tex = new Texture2D(newWidth, newHeight);
			tex.name = name;
			tex.SetPixels32(newPixels);
			tex.Apply();

			var atlas = NGUISettings.atlas;
			tempMat = atlas.spriteMaterial;
			if (tempMat == null) return;

			tempMat = new Material(tempMat);
			tempMat.hideFlags = HideFlags.HideAndDontSave;
			tempMat.SetTexture("_MainTex", tex);

			tempGO = EditorUtility.CreateGameObjectWithHideFlags(name, HideFlags.HideAndDontSave, typeof(MeshRenderer));
			tempGO.GetComponent<MeshRenderer>().sharedMaterial = tempMat;
		}

		/// <summary>
		/// Release temporary resources.
		/// </summary>

		public void Release ()
		{
			if (temporaryTexture)
			{
				UnityEngine.Object.DestroyImmediate(tempGO);
				UnityEngine.Object.DestroyImmediate(tempMat);
				UnityEngine.Object.DestroyImmediate(tex);

				tempGO = null;
				tempMat = null;
				tex = null;
				temporaryTexture = false;
			}
		}
	}

	Vector2 mScroll = Vector2.zero;
	List<string> mDelNames = new List<string>();
	INGUIAtlas mLastAtlas;
	INGUITextureProcessor mProcessor;
	GameObject mProcessorSrc;

	void OnEnable () { instance = this; }
	void OnDisable () { instance = null; }

	/// <summary>
	/// Atlas selection callback.
	/// </summary>

	void OnSelectAtlas (UnityEngine.Object obj)
	{
		// Legacy atlas support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIAtlas>();

		if (NGUISettings.atlas != obj as INGUIAtlas)
		{
			NGUISettings.atlas = obj as INGUIAtlas;
			Repaint();
		}
	}

	/// <summary>
	/// Refresh the window on selection.
	/// </summary>

	void OnSelectionChange () { mDelNames.Clear(); Repaint(); }

	/// <summary>
	/// Helper function that retrieves the list of currently selected textures.
	/// </summary>

	static List<Texture> GetSelectedTextures ()
	{
		var textures = new List<Texture>();
		var names = new List<string>();

		if (Selection.objects != null && Selection.objects.Length > 0)
		{
			var objects = Selection.GetFiltered(typeof(Texture), SelectionMode.DeepAssets);

			System.Array.Sort(objects, ObjNameComparer);

			foreach (UnityEngine.Object o in objects)
			{
				var tex = o as Texture;
				if (tex == null || tex.name == "Font Texture") continue;
				if (names.Contains(tex.name)) continue;

				var atlas = NGUISettings.atlas;

				if (atlas == null)
				{
					names.Add(tex.name);
					textures.Add(tex);
					continue;
				}

				var t = atlas.texture;

				if (t != tex)
				{
					names.Add(tex.name);
					textures.Add(tex);
				}
			}
		}
		return textures;
	}

	static int ObjNameComparer (UnityEngine.Object x, UnityEngine.Object y)
	{
		return AssetDatabase.GetAssetPath(x).CompareTo(AssetDatabase.GetAssetPath(y));
	}

	/// <summary>
	/// Load the specified list of textures as Texture2Ds, fixing their import properties as necessary.
	/// </summary>

	static List<Texture> LoadTextures (List<Texture> textures)
	{
		var list = new List<Texture>();

		foreach (Texture tex in textures)
		{
			var t2 = NGUIEditorTools.ImportTexture(tex, true, false, true);
			if (t2 != null) list.Add(t2);
		}
		return list;
	}

	/// <summary>
	/// Used to sort the sprites by pixels used
	/// </summary>

	static int Compare (SpriteEntry a, SpriteEntry b)
	{
		// A is null b is not b is greater so put it at the front of the list
		if (a == null && b != null) return 1;

		// A is not null b is null a is greater so put it at the front of the list
		if (a != null && b == null) return -1;

		// Get the total pixels used for each sprite
		int aPixels = a.width * a.height;
		int bPixels = b.width * b.height;

		if (aPixels > bPixels) return -1;
		else if (aPixels < bPixels) return 1;
		return 0;
	}

	/// <summary>
	/// Pack all of the specified sprites into a single texture, updating the outer and inner rects of the sprites as needed.
	/// </summary>

	static bool PackTextures (Texture2D tex, List<SpriteEntry> sprites)
	{
		var textures = new Texture2D[sprites.Count];
		Rect[] rects;

#if UNITY_3_5 || UNITY_4_0
		int maxSize = 4096;
#else
		int maxSize = SystemInfo.maxTextureSize;
#endif

#if UNITY_ANDROID || UNITY_IPHONE
		maxSize = Mathf.Min(maxSize, NGUISettings.allow4096 ? 4096 : 2048);
#endif
		if (NGUISettings.unityPacking)
		{
			for (int i = 0; i < sprites.Count; ++i) textures[i] = sprites[i].tex;
			rects = tex.PackTextures(textures, NGUISettings.atlasPadding, maxSize);
		}
		else
		{
			sprites.Sort(Compare);
			for (int i = 0; i < sprites.Count; ++i) textures[i] = sprites[i].tex;
			rects = UITexturePacker.PackTextures(tex, textures, 4, 4, NGUISettings.atlasPadding, maxSize);
		}

		for (int i = 0; i < sprites.Count; ++i)
		{
			Rect rect = NGUIMath.ConvertToPixels(rects[i], tex.width, tex.height, true);

			// Apparently Unity can take the liberty of destroying temporary textures without any warning
			if (textures[i] == null) return false;

			// Make sure that we don't shrink the textures
			if (Mathf.RoundToInt(rect.width) != textures[i].width) return false;

			SpriteEntry se = sprites[i];
			se.x = Mathf.RoundToInt(rect.x);
			se.y = Mathf.RoundToInt(rect.y);
			se.width = Mathf.RoundToInt(rect.width);
			se.height = Mathf.RoundToInt(rect.height);
		}
		return true;
	}

	/// <summary>
	/// Helper function that creates a single sprite list from both the atlas's sprites as well as selected textures.
	/// Dictionary value meaning:
	/// 0 = No change
	/// 1 = Update
	/// 2 = Add
	/// </summary>

	Dictionary<string, int> GetSpriteList (List<Texture> textures)
	{
		var spriteList = new Dictionary<string, int>();

		// If we have textures to work with, include them as well
		if (textures.Count > 0)
		{
			var texNames = new List<string>();
			foreach (Texture tex in textures) texNames.Add(tex.name);
			//texNames.Sort();
			foreach (string tex in texNames) spriteList.Add(tex, 2);
		}

		var atlas = NGUISettings.atlas;

		if (atlas != null)
		{
			var spriteNames = atlas.GetListOfSprites();

			if (spriteNames != null)
			{
				foreach (string sp in spriteNames)
				{
					if (spriteList.ContainsKey(sp)) spriteList[sp] = 1;
					else spriteList.Add(sp, 0);
				}
			}
		}
		return spriteList;
	}

	/// <summary>
	/// Add a new sprite to the atlas, given the texture it's coming from and the packed rect within the atlas.
	/// </summary>

	static public UISpriteData AddSprite (List<UISpriteData> sprites, SpriteEntry se)
	{
		// See if this sprite already exists
		foreach (UISpriteData sp in sprites)
		{
			if (sp.name == se.name)
			{
				sp.CopyFrom(se);
				return sp;
			}
		}

		UISpriteData sprite = new UISpriteData();
		sprite.CopyFrom(se);
		sprites.Add(sprite);
		return sprite;
	}

	/// <summary>
	/// Create a list of sprites using the specified list of textures.
	/// </summary>

	static public List<SpriteEntry> CreateSprites (List<Texture> textures, Texture editedTex = null, int editedPadding = 0)
	{
		var list = new List<SpriteEntry>();

		foreach (Texture tex in textures)
		{
			var oldTex = NGUIEditorTools.ImportTexture(tex, true, false, true);
			if (oldTex == null) oldTex = tex as Texture2D;
			if (oldTex == null) continue;

			var trim = NGUISettings.atlasTrimming;
			if (editedTex != null && editedTex == oldTex) trim = true;

			// If we aren't doing trimming, just use the texture as-is
			if ((!trim && !NGUISettings.atlasPMA) && (editedTex == null || editedTex != oldTex))
			{
				var sprite = new SpriteEntry();
				sprite.SetRect(0, 0, oldTex.width, oldTex.height);
				sprite.tex = oldTex;
				sprite.name = oldTex.name;
				sprite.temporaryTexture = false;
				list.Add(sprite);
				continue;
			}

			// If we want to trim transparent pixels, there is more work to be done
			var pixels = oldTex.GetPixels32();
			int xmin = oldTex.width;
			int xmax = 0;
			int ymin = oldTex.height;
			int ymax = 0;
			int oldWidth = oldTex.width;
			int oldHeight = oldTex.height;

			// Find solid pixels
			if (trim)
			{
				for (int y = 0, yw = oldHeight; y < yw; ++y)
				{
					for (int x = 0, xw = oldWidth; x < xw; ++x)
					{
						var c = pixels[y * xw + x];

						if (c.a != 0)
						{
							if (y < ymin) ymin = y;
							if (y > ymax) ymax = y;
							if (x < xmin) xmin = x;
							if (x > xmax) xmax = x;
						}
					}
				}
			}
			else
			{
				xmin = 0;
				xmax = oldWidth - 1;
				ymin = 0;
				ymax = oldHeight - 1;
			}

			int newWidth  = (xmax - xmin) + 1;
			int newHeight = (ymax - ymin) + 1;

			var pw = newWidth;
			var ph = newHeight;

			if (editedTex != null && editedTex == oldTex)
			{
				newWidth += editedPadding;
				newHeight += editedPadding;
			}
			else editedPadding = 0;

			if (pw > 0 && ph > 0)
			{
				var sprite = new SpriteEntry();
				sprite.x = 0;
				sprite.y = 0;
				sprite.width = oldTex.width;
				sprite.height = oldTex.height;

				// If the dimensions match, then nothing was actually trimmed
				if (!NGUISettings.atlasPMA && (newWidth == oldWidth && newHeight == oldHeight))
				{
					sprite.tex = oldTex;
					sprite.name = oldTex.name;
					sprite.temporaryTexture = false;
				}
				else
				{
					// Copy the non-trimmed texture data into a temporary buffer
					var newPixels = new Color32[newWidth * newHeight];

					for (int y = 0; y < ph; ++y)
					{
						for (int x = 0; x < pw; ++x)
						{
							int newIndex = (y + editedPadding) * newWidth + x + editedPadding;
							int oldIndex = (ymin + y) * oldWidth + (xmin + x);
							if (NGUISettings.atlasPMA) newPixels[newIndex] = NGUITools.ApplyPMA(pixels[oldIndex]);
							else newPixels[newIndex] = pixels[oldIndex];
						}
					}

					// Create a new texture
					sprite.name = oldTex.name;
					sprite.SetTexture(newPixels, newWidth, newHeight);

					// Remember the padding offset
					sprite.SetPadding(xmin, ymin, oldWidth - newWidth - xmin, oldHeight - newHeight - ymin);
				}
				list.Add(sprite);
			}
		}
		return list;
	}

	/// <summary>
	/// Release all temporary textures created for the sprites.
	/// </summary>

	static public void ReleaseSprites (List<SpriteEntry> sprites)
	{
		foreach (SpriteEntry se in sprites) se.Release();

		// This causes an issue in Unity 2022.2 onwards that gets resolved in version 2023.
		#if !UNITY_2022
		Resources.UnloadUnusedAssets();
		#endif
	}

	/// <summary>
	/// Replace the sprites within the atlas.
	/// </summary>

	static public void ReplaceSprites (INGUIAtlas atlas, List<SpriteEntry> sprites)
	{
		if (atlas == null) return;

		// Get the list of sprites we'll be updating
		List<UISpriteData> kept = new List<UISpriteData>();
		var spriteList = atlas.spriteList;

		// Run through all the textures we added and add them as sprites to the atlas
		for (int i = 0; i < sprites.Count; ++i)
		{
			var se = sprites[i];
			var sprite = AddSprite(spriteList, se);
			kept.Add(sprite);
		}

		// Remove unused sprites
		for (int i = spriteList.Count; i > 0; )
		{
			var sp = spriteList[--i];
			if (!kept.Contains(sp)) spriteList.RemoveAt(i);
		}

		// Sort the sprites so that they are alphabetical within the atlas
		atlas.SortAlphabetically();
		atlas.MarkAsChanged();
	}

	/// <summary>
	/// Duplicate the specified sprite.
	/// </summary>

	static public SpriteEntry DuplicateSprite (INGUIAtlas atlas, in string spriteName)
	{
		if (atlas == null || atlas.texture == null) return null;
		var sd = atlas.GetSprite(spriteName);
		if (sd == null) return null;

		var tex = NGUIEditorTools.ImportTexture(atlas.texture, true, true, false);
		SpriteEntry se = ExtractSprite(sd, tex);

		if (se != null)
		{
			se.name = se.name + " (Copy)";

			var sprites = new List<UIAtlasMaker.SpriteEntry>();
			UIAtlasMaker.ExtractSprites(atlas, sprites);
			sprites.Add(se);
			UIAtlasMaker.UpdateAtlas(atlas, sprites);
			se.Release();
		}
		else NGUIEditorTools.ImportTexture(atlas.texture, false, false, !atlas.premultipliedAlpha);
		return se;
	}

	/// <summary>
	/// Duplicate the specified sprite.
	/// </summary>

	static public SpriteEntry DuplicateSprite (NGUIAtlas atlas, in string spriteName)
	{
		if (atlas == null || atlas.texture == null) return null;
		UISpriteData sd = atlas.GetSprite(spriteName);
		if (sd == null) return null;

		Texture2D tex = NGUIEditorTools.ImportTexture(atlas.texture, true, true, false);
		SpriteEntry se = ExtractSprite(sd, tex);

		if (se != null)
		{
			se.name = se.name + " (Copy)";

			List<UIAtlasMaker.SpriteEntry> sprites = new List<UIAtlasMaker.SpriteEntry>();
			UIAtlasMaker.ExtractSprites(atlas, sprites);
			sprites.Add(se);
			UIAtlasMaker.UpdateAtlas(atlas, sprites);
			se.Release();
		}
		else NGUIEditorTools.ImportTexture(atlas.texture, false, false, !atlas.premultipliedAlpha);
		return se;
	}

	/// <summary>
	/// Extract the specified sprite from the atlas.
	/// </summary>

	static public SpriteEntry ExtractSprite (INGUIAtlas atlas, in string spriteName)
	{
		if (atlas == null || atlas.texture == null) return null;
		var sd = atlas.GetSprite(spriteName);
		if (sd == null) return null;

		var tex = NGUIEditorTools.ImportTexture(atlas.texture, true, true, false);
		var se = ExtractSprite(sd, tex);
		NGUIEditorTools.ImportTexture(atlas.texture, false, false, !atlas.premultipliedAlpha);
		return se;
	}

	/// <summary>
	/// Extract the specified sprite from the atlas texture.
	/// </summary>

	static SpriteEntry ExtractSprite (UISpriteData es, Texture2D tex)
	{
		return (tex != null) ? ExtractSprite(es, tex.GetPixels32(), tex.width, tex.height) : null;
	}

	/// <summary>
	/// Extract the specified sprite from the atlas texture.
	/// </summary>

	static SpriteEntry ExtractSprite (UISpriteData es, Color32[] oldPixels, int oldWidth, int oldHeight)
	{
		int xmin = Mathf.Clamp(es.x, 0, oldWidth);
		int ymin = Mathf.Clamp(es.y, 0, oldHeight);
		int xmax = Mathf.Min(xmin + es.width, oldWidth - 1);
		int ymax = Mathf.Min(ymin + es.height, oldHeight - 1);
		int newWidth = Mathf.Clamp(es.width, 0, oldWidth);
		int newHeight = Mathf.Clamp(es.height, 0, oldHeight);

		if (newWidth == 0 || newHeight == 0) return null;

		var newPixels = new Color32[newWidth * newHeight];

		for (int y = 0; y < newHeight; ++y)
		{
			int cy = ymin + y;
			if (cy > ymax) cy = ymax;

			for (int x = 0; x < newWidth; ++x)
			{
				int cx = xmin + x;
				if (cx > xmax) cx = xmax;

				int newIndex = (newHeight - 1 - y) * newWidth + x;
				int oldIndex = (oldHeight - 1 - cy) * oldWidth + cx;

				newPixels[newIndex] = oldPixels[oldIndex];
			}
		}

		// Create a new sprite
		var sprite = new SpriteEntry();
		sprite.CopyFrom(es);
		sprite.SetRect(0, 0, newWidth, newHeight);
		sprite.SetTexture(newPixels, newWidth, newHeight);
		return sprite;
	}

	static Texture2D atlasTexture
	{
		get
		{
			var atlas = NGUISettings.atlas;
			if (atlas != null) return atlas.texture as Texture2D;
			return null;
		}
	}

	static List<UISpriteData> spriteList
	{
		get
		{
			var atlas = NGUISettings.atlas;
			if (atlas != null) return atlas.spriteList;
			return null;
		}
	}

	static bool premultipliedAlpha
	{
		get
		{
			var atlas = NGUISettings.atlas;
			if (atlas != null) return atlas.premultipliedAlpha;
			return false;
		}
	}

	static Material spriteMaterial
	{
		get
		{
			var atlas = NGUISettings.atlas;
			if (atlas != null) return atlas.spriteMaterial;
			return null;
		}
		set
		{
			var atlas = NGUISettings.atlas;
			if (atlas != null) atlas.spriteMaterial = value;
		}
	}

	/// <summary>
	/// Extract sprites from the atlas, adding them to the list.
	/// </summary>

	static public void ExtractSprites (INGUIAtlas atlas, List<SpriteEntry> finalSprites)
	{
		ShowProgress(0f);

		// Make the atlas texture readable
		var tex = NGUIEditorTools.ImportTexture(atlasTexture, true, true, false);

		if (tex != null)
		{
			var sprites = spriteList;

			if (sprites != null)
			{
				Color32[] pixels = null;
				var width = tex.width;
				var height = tex.height;
				var count = sprites.Count;
				var index = 0;

				foreach (UISpriteData es in sprites)
				{
					ShowProgress((index++) / count);

					bool found = false;

					foreach (SpriteEntry fs in finalSprites)
					{
						if (es.name == fs.name)
						{
							fs.CopyBorderFrom(es);
							found = true;
							break;
						}
					}

					if (!found)
					{
						if (pixels == null) pixels = tex.GetPixels32();
						var sprite = ExtractSprite(es, pixels, width, height);
						if (sprite != null) finalSprites.Add(sprite);
					}
				}
			}
		}

		// The atlas no longer needs to be readable
		NGUIEditorTools.ImportTexture(atlasTexture, false, false, !premultipliedAlpha);
		ShowProgress(1f);
	}

	/// <summary>
	/// Combine all sprites into a single texture and save it to disk.
	/// </summary>

	static public bool UpdateTexture (INGUIAtlas atlas, List<SpriteEntry> sprites)
	{
		// Get the texture for the atlas
		var tex = atlasTexture;
		var oldPath = (tex != null) ? AssetDatabase.GetAssetPath(tex.GetInstanceID()) : "";
		var newPath = NGUIEditorTools.GetSaveableTexturePath(atlas as UnityEngine.Object, atlasTexture);

		// Clear the read-only flag in texture file attributes
		if (System.IO.File.Exists(newPath))
		{
			System.IO.FileAttributes newPathAttrs = System.IO.File.GetAttributes(newPath);
			newPathAttrs &= ~System.IO.FileAttributes.ReadOnly;
			System.IO.File.SetAttributes(newPath, newPathAttrs);
		}

		bool newTexture = (tex == null || oldPath != newPath);

		if (newTexture)
		{
			// Create a new texture for the atlas
			tex = new Texture2D(1, 1, TextureFormat.ARGB32, false);
		}
		else
		{
			// Make the atlas readable so we can save it
			tex = NGUIEditorTools.ImportTexture(oldPath, true, false, false);
		}

		// Pack the sprites into this texture
		if (PackTextures(tex, sprites))
		{
			var bytes = tex.EncodeToPNG();
			System.IO.File.WriteAllBytes(newPath, bytes);
			bytes = null;

			// Load the texture we just saved as a Texture2D
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			tex = NGUIEditorTools.ImportTexture(newPath, false, true, !premultipliedAlpha);

			// Update the atlas texture
			if (newTexture)
			{
				if (tex == null)
				{
					Debug.LogError("Failed to load the created atlas saved as " + newPath);
					EditorUtility.ClearProgressBar();
				}
				else
				{
					var mat = spriteMaterial;

					if (mat == null)
					{
						var matPath = newPath.Replace(".png", ".mat");
						var shader = Shader.Find(NGUISettings.atlasPMA ? "Unlit/Premultiplied Colored" : "Unlit/Transparent Colored");
						mat = new Material(shader);

						// Save the material
						AssetDatabase.CreateAsset(mat, matPath);
						AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

						// Load the material so it's usable
						mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
						spriteMaterial = mat;
					}

					mat.mainTexture = tex;
				}

				ReleaseSprites(sprites);

				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			}
			return true;
		}
		else
		{
			if (!newTexture) NGUIEditorTools.ImportTexture(oldPath, false, true, !premultipliedAlpha);

			//Debug.LogError("Operation canceled: The selected sprites can't fit into the atlas.\n" +
			//	"Keep large sprites outside the atlas (use UITexture), and/or use multiple atlases instead.");

			EditorUtility.DisplayDialog("Operation Canceled", "The selected sprites can't fit into the atlas.\n" +
					"Keep large sprites outside the atlas (use UITexture), and/or use multiple atlases instead", "OK");
			return false;
		}
	}

	/// <summary>
	/// Show a progress bar.
	/// </summary>

	static public void ShowProgress (float val)
	{
		EditorUtility.DisplayProgressBar("Updating", "Updating the atlas, please wait...", val);
	}

	/// <summary>
	/// Add the specified texture to the atlas, or update an existing one.
	/// </summary>

	static public void AddOrUpdate (INGUIAtlas atlas, Texture2D tex)
	{
		if (atlas != null && tex != null)
		{
			var textures = new List<Texture>();
			textures.Add(tex);
			var sprites = CreateSprites(textures);
			ExtractSprites(atlas, sprites);
			UpdateAtlas(atlas, sprites);
		}
	}

	/// <summary>
	/// Add the specified texture to the atlas, or update an existing one.
	/// </summary>

	static public void AddOrUpdate (INGUIAtlas atlas, Texture2D tex, int pixelPadding)
	{
		if (atlas != null && tex != null)
		{
			var before = NGUISettings.atlasTrimming;
			NGUISettings.atlasTrimming = true;
			var textures = new List<Texture>();
			textures.Add(tex);
			var sprites = CreateSprites(textures, tex, pixelPadding);
			ExtractSprites(atlas, sprites);
			UpdateAtlas(atlas, sprites);
			NGUISettings.atlasTrimming = before;
		}
	}

	/// <summary>
	/// Add the specified texture to the atlas, or update an existing one.
	/// </summary>

	static public void AddOrUpdate (INGUIAtlas atlas, SpriteEntry se)
	{
		if (atlas != null && se != null)
		{
			var sprites = new List<SpriteEntry>();
			sprites.Add(se);
			ExtractSprites(atlas, sprites);
			UpdateAtlas(atlas, sprites);
		}
	}

	/// <summary>
	/// Update the sprites within the texture atlas, preserving the sprites that have not been selected.
	/// </summary>

	void UpdateAtlas (List<Texture> textures, bool keepSprites)
	{
		// Create a list of sprites using the collected textures
		var sprites = CreateSprites(textures);

		if (sprites.Count > 0)
		{
			// Extract sprites from the atlas, filling in the missing pieces
			if (keepSprites) ExtractSprites(NGUISettings.atlas, sprites);

			// NOTE: It doesn't seem to be possible to undo writing to disk, and there also seems to be no way of
			// detecting an Undo event. Without either of these it's not possible to restore the texture saved to disk,
			// so the undo process doesn't work right. Because of this I'd rather disable it altogether until a solution is found.

			// The ability to undo this action is always useful
			//NGUIEditorTools.RegisterUndo("Update Atlas", UISettings.atlas, UISettings.atlas.texture, UISettings.atlas.material);

			// Update the atlas
			UpdateAtlas(NGUISettings.atlas, sprites);
		}
		else if (!keepSprites)
		{
			UpdateAtlas(NGUISettings.atlas, sprites);
		}
	}

	/// <summary>
	/// Update the sprite atlas, keeping only the sprites that are on the specified list.
	/// </summary>

	static public void UpdateAtlas (INGUIAtlas obj, List<SpriteEntry> sprites)
	{
#if UNITY_2018_3_OR_NEWER
		// Contributed by B9 from https://discord.com/invite/tasharen
		if (obj is UIAtlas) // Prefab-based atlas
		{
			Debug.LogWarning("Updating a legacy atlas: issues may occur. Please update the atlas to a new format that uses Scriptable Objects rather than Prefabs.", obj as UnityEngine.Object);
			var atlas = (obj as UIAtlas);

			if (!PrefabUtility.IsPartOfPrefabAsset(atlas.gameObject))
			{
				Debug.LogWarning("Atlas is not sourced from prefab asset, ignoring the request to update it");
				return;
			}

			var assetPath = AssetDatabase.GetAssetPath(atlas.gameObject);

			if (string.IsNullOrEmpty(assetPath))
			{
				Debug.LogWarning("Atlas asset path could not be found, aborting");
				return;
			}

			var assetRoot = PrefabUtility.LoadPrefabContents(assetPath);
			var atlasTemp = assetRoot.GetComponent<UIAtlas>();

			if (atlasTemp == null)
			{
				Debug.LogWarning("Atlas component could not be found in the loaded prefab, aborting");
				PrefabUtility.UnloadPrefabContents(assetRoot);
				return;
			}

			if (sprites.Count > 0)
			{
				// Combine all sprites into a single texture and save it
				if (UpdateTexture(atlasTemp, sprites))
				{
					// Replace the sprites within the atlas
					ReplaceSprites(atlasTemp, sprites);
				}

				// Release the temporary textures
				ReleaseSprites(sprites);
			}
			else
			{
				atlasTemp.spriteList.Clear();
				var texturePath = NGUIEditorTools.GetSaveableTexturePath(atlasTemp);
				atlasTemp.spriteMaterial.mainTexture = null;
				if (!string.IsNullOrEmpty(texturePath)) AssetDatabase.DeleteAsset(texturePath);
			}

			PrefabUtility.SaveAsPrefabAsset(assetRoot, assetPath);
			Selection.activeObject = NGUISettings.atlas as UnityEngine.Object;
			EditorUtility.ClearProgressBar();

			PrefabUtility.UnloadPrefabContents(assetRoot);
			AssetDatabase.Refresh();

			var assetUpdated = (GameObject)AssetDatabase.LoadMainAssetAtPath(assetPath);
			var newAtlas = assetUpdated.GetComponent<UIAtlas>();
			NGUISettings.atlas = newAtlas;
			newAtlas.MarkAsChanged ();

			var panels = NGUITools.FindActive<UIPanel>();

			foreach (var panel in panels)
			{
				if (!panel.enabled) continue;
				panel.enabled = false;
				panel.enabled = true;
			}
			EditorUtility.CollectDependencies(panels);
			return;
		}
#endif
		if (sprites.Count > 0)
		{
			// Combine all sprites into a single texture and save it
			if (UpdateTexture(obj, sprites)) ReplaceSprites(obj, sprites);
			ReleaseSprites(sprites);
		}
		else
		{
			obj.spriteList.Clear();
			var path = NGUIEditorTools.GetSaveableTexturePath(obj);
			obj.spriteMaterial.mainTexture = null;
			if (!string.IsNullOrEmpty(path)) AssetDatabase.DeleteAsset(path);
			obj.MarkAsChanged();
		}

		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Draw the UI for this tool.
	/// </summary>

	void OnGUI ()
	{
		var atlas = NGUISettings.atlas;
		if (mLastAtlas != atlas)
			mLastAtlas = atlas;

		bool update = false;
		bool replace = false;

		NGUIEditorTools.SetLabelWidth(84f);
		GUILayout.Space(3f);

		NGUIEditorTools.DrawHeader("Input", true);
		NGUIEditorTools.BeginContents(false);

		GUILayout.BeginHorizontal();
		{
			ComponentSelector.Draw("Atlas", atlas, OnSelectAtlas, true, GUILayout.MinWidth(80f));

			EditorGUI.BeginDisabledGroup(atlas == null);
			if (GUILayout.Button("New", GUILayout.Width(40f))) NGUISettings.atlas = null;
			EditorGUI.EndDisabledGroup();
		}
		GUILayout.EndHorizontal();

		var textures = GetSelectedTextures();

		if (atlas != null)
		{
			var mat = spriteMaterial;
			var tex = atlasTexture;

			// Material information
			GUILayout.BeginHorizontal();
			{
				if (mat != null)
				{
					if (GUILayout.Button("Material", GUILayout.Width(76f))) Selection.activeObject = mat;
					GUILayout.Label(" " + mat.name);
				}
				else
				{
					GUI.color = Color.grey;
					GUILayout.Button("Material", GUILayout.Width(76f));
					GUI.color = Color.white;
					GUILayout.Label(" N/A");
				}
			}
			GUILayout.EndHorizontal();

			// Texture atlas information
			GUILayout.BeginHorizontal();
			{
				if (tex != null)
				{
					if (GUILayout.Button("Texture", GUILayout.Width(76f))) Selection.activeObject = tex;
					GUILayout.Label(" " + tex.width + "x" + tex.height);
				}
				else
				{
					GUI.color = Color.grey;
					GUILayout.Button("Texture", GUILayout.Width(76f));
					GUI.color = Color.white;
					GUILayout.Label(" N/A");
				}
			}
			GUILayout.EndHorizontal();
		}

		GUILayout.BeginHorizontal();
		NGUISettings.atlasPadding = Mathf.Clamp(EditorGUILayout.IntField("Padding", NGUISettings.atlasPadding, GUILayout.Width(100f)), 0, 8);
		GUILayout.Label((NGUISettings.atlasPadding == 1 ? "pixel" : "pixels") + " between sprites");
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		NGUISettings.atlasTrimming = EditorGUILayout.Toggle("Trim Alpha", NGUISettings.atlasTrimming, GUILayout.Width(100f));
		GUILayout.Label("Remove empty space");
		GUILayout.EndHorizontal();

		bool fixedShader = false;

		if (atlas != null)
		{
			var mat = spriteMaterial;

			if (mat != null)
			{
				Shader shader = mat.shader;

				if (shader != null)
				{
					if (shader.name == "Unlit/Transparent Colored")
					{
						NGUISettings.atlasPMA = false;
						fixedShader = true;
					}
					else if (shader.name == "Unlit/Premultiplied Colored")
					{
						NGUISettings.atlasPMA = true;
						fixedShader = true;
					}
				}
			}
		}

		if (!fixedShader)
		{
			GUILayout.BeginHorizontal();
			NGUISettings.atlasPMA = EditorGUILayout.Toggle("PMA Shader", NGUISettings.atlasPMA, GUILayout.Width(100f));
			GUILayout.Label("Pre-multiplied alpha", GUILayout.MinWidth(70f));
			GUILayout.EndHorizontal();
		}

		#if !UNITY_5_6
		GUILayout.BeginHorizontal();
		NGUISettings.unityPacking = EditorGUILayout.Toggle("Unity Packer", NGUISettings.unityPacking, GUILayout.Width(100f));
		GUILayout.Label("or custom packer", GUILayout.MinWidth(70f));
		GUILayout.EndHorizontal();
		#endif

		GUILayout.BeginHorizontal();
		NGUISettings.trueColorAtlas = EditorGUILayout.Toggle("Truecolor", NGUISettings.trueColorAtlas, GUILayout.Width(100f));
		GUILayout.Label("force ARGB32 textures", GUILayout.MinWidth(70f));
		GUILayout.EndHorizontal();

		GUILayout.BeginHorizontal();
		NGUISettings.autoUpgradeSprites = EditorGUILayout.Toggle("Auto-upgrade", NGUISettings.autoUpgradeSprites, GUILayout.Width(100f));
		GUILayout.Label("replace textures with sprites", GUILayout.MinWidth(70f));
		GUILayout.EndHorizontal();

		#if !UNITY_5_6
		if (!NGUISettings.unityPacking)
		{
			GUILayout.BeginHorizontal();
			NGUISettings.forceSquareAtlas = EditorGUILayout.Toggle("Force Square", NGUISettings.forceSquareAtlas, GUILayout.Width(100f));
			GUILayout.Label("if on, forces a square atlas texture", GUILayout.MinWidth(70f));
			GUILayout.EndHorizontal();
		}
#endif

		GUI.changed = false;
		GUILayout.BeginHorizontal();
		mProcessorSrc = EditorGUILayout.ObjectField("Pre-processor", mProcessorSrc, typeof(GameObject), true) as GameObject;

		if (mProcessorSrc != null)
		{
			var mbs = mProcessorSrc.GetComponents<MonoBehaviour>();

			foreach (var mb in mbs)
			{
				mProcessor = mb as INGUITextureProcessor;
				if (mProcessor != null) break;
			}
		}
		else mProcessor = null;

		GUILayout.EndHorizontal();

		if (mProcessorSrc != null && mProcessor == null)
		{
			EditorGUILayout.HelpBox("No script implementing INGUITextureProcessor found", MessageType.Warning);
		}

		List<Texture> cleanup = null;

#if UNITY_IPHONE || UNITY_ANDROID
		GUILayout.BeginHorizontal();
		NGUISettings.allow4096 = EditorGUILayout.Toggle("4096x4096", NGUISettings.allow4096, GUILayout.Width(100f));
		GUILayout.Label("if off, limit atlases to 2048x2048");
		GUILayout.EndHorizontal();
#endif
		NGUIEditorTools.EndContents();

		if (atlas != null)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Space(20f);

			if (textures.Count > 0)
			{
				update = GUILayout.Button("Add/Update");
			}
			else if (GUILayout.Button("View Sprites"))
			{
				SpriteSelector.ShowSelected();
			}

			GUILayout.Space(20f);
			GUILayout.EndHorizontal();
		}
		else
		{
			EditorGUILayout.HelpBox("You can create a new atlas by selecting one or more textures in the Project View window, then clicking \"Create\".", MessageType.Info);

			EditorGUI.BeginDisabledGroup(textures.Count == 0);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20f);
			bool create = GUILayout.Button("Create");
			GUILayout.Space(20f);
			GUILayout.EndHorizontal();
			EditorGUI.EndDisabledGroup();

			if (create)
			{
				var path = EditorUtility.SaveFilePanelInProject("Save As", "New Atlas.asset", "asset", "Save atlas as...", NGUISettings.currentPath);

				if (!string.IsNullOrEmpty(path))
				{
					NGUISettings.currentPath = System.IO.Path.GetDirectoryName(path);
					var asset = AssetDatabase.LoadAssetAtPath<NGUIAtlas>(path);
					if (asset == null) asset = ScriptableObject.CreateInstance<NGUIAtlas>();
					var matPath = path.Replace(".asset", ".mat");
					replace = true;

					// Try to load the material
					var mat = AssetDatabase.LoadAssetAtPath(matPath, typeof(Material)) as Material;

					// If the material doesn't exist, create it
					if (mat == null)
					{
						var shader = Shader.Find(NGUISettings.atlasPMA ? "Unlit/Premultiplied Colored" : "Unlit/Transparent Colored");
						mat = new Material(shader);

						// Save the material
						AssetDatabase.CreateAsset(mat, matPath);
						AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

						// Load the material so it's usable
						mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
					}

					// Create a new game object for the atlas
					var atlasName = path.Replace(".asset", "");
					atlasName = atlasName.Substring(path.LastIndexOfAny(new char[] { '/', '\\' }) + 1);

					asset.spriteMaterial = mat;

					// Update the prefab
					var existing = AssetDatabase.LoadMainAssetAtPath(path);
					if (existing != null) EditorUtility.CopySerialized(asset, existing);
					else AssetDatabase.CreateAsset(asset, path);

					AssetDatabase.SaveAssets();
					AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

					// Select the atlas
					NGUISettings.atlas = AssetDatabase.LoadAssetAtPath<NGUIAtlas>(path);
					Selection.activeObject = NGUISettings.atlas as UnityEngine.Object;
				}
			}
		}

		if ((mProcessor != null) && (update || replace))
		{
			textures = LoadTextures(textures);
			mProcessor.PrepareToProcess(textures);
			var result = new List<Texture>();

			foreach (var tex in textures)
			{
				var final = mProcessor.Process(tex);
				result.Add(final != null ? final : tex);

				if (final != tex && final != null)
				{
					if (cleanup == null) cleanup = new List<Texture>();
					cleanup.Add(final);
				}
			}

			textures = result;
		}

		string selection = null;
		var spriteList = GetSpriteList(textures);

		if (spriteList.Count > 0)
		{
			NGUIEditorTools.DrawHeader("Sprites", true);
			{
				GUILayout.BeginHorizontal();
				GUILayout.Space(3f);
				GUILayout.BeginVertical();

				mScroll = GUILayout.BeginScrollView(mScroll);

				bool delete = false;
				int index = 0;

				foreach (KeyValuePair<string, int> iter in spriteList)
				{
					++index;

					GUILayout.Space(-1f);
					bool highlight = (UIAtlasInspector.instance != null) && (NGUISettings.selectedSprite == iter.Key);
					GUI.backgroundColor = highlight ? Color.white : new Color(0.8f, 0.8f, 0.8f);
					GUILayout.BeginHorizontal(NGUIEditorTools.textArea, GUILayout.MinHeight(20f));
					GUI.backgroundColor = Color.white;
					GUILayout.Label(index.ToString(), GUILayout.Width(24f));
#if UNITY_2018_3_OR_NEWER
					if (GUILayout.Button(iter.Key, "Label", GUILayout.Height(20f)))
#else
					if (GUILayout.Button(iter.Key, "OL TextField", GUILayout.Height(20f)))
#endif
						selection = iter.Key;

					if (iter.Value == 2)
					{
						GUI.color = Color.green;
						GUILayout.Label("Add", GUILayout.Width(27f));
						GUI.color = Color.white;
					}
					else if (iter.Value == 1)
					{
						GUI.color = Color.cyan;
						GUILayout.Label("Update", GUILayout.Width(45f));
						GUI.color = Color.white;
					}
					else
					{
						if (mDelNames.Contains(iter.Key))
						{
							GUI.backgroundColor = Color.red;

							if (GUILayout.Button("Delete", GUILayout.Width(60f)))
							{
								delete = true;
							}
							GUI.backgroundColor = Color.green;
							if (GUILayout.Button("X", GUILayout.Width(22f)))
							{
								mDelNames.Remove(iter.Key);
								delete = false;
							}
							GUI.backgroundColor = Color.white;
						}
						else
						{
							// If we have not yet selected a sprite for deletion, show a small "X" button
							if (GUILayout.Button("X", GUILayout.Width(22f))) mDelNames.Add(iter.Key);
						}
					}
					GUILayout.EndHorizontal();
				}
				GUILayout.EndScrollView();
				GUILayout.EndVertical();
				GUILayout.Space(3f);
				GUILayout.EndHorizontal();

				// If this sprite was marked for deletion, remove it from the atlas
				if (delete)
				{
					var sprites = new List<SpriteEntry>();
					ExtractSprites(NGUISettings.atlas, sprites);

					for (int i = sprites.Count; i > 0; )
					{
						SpriteEntry ent = sprites[--i];
						if (mDelNames.Contains(ent.name))
							sprites.RemoveAt(i);
					}

					UpdateAtlas(NGUISettings.atlas, sprites);
					mDelNames.Clear();
					NGUIEditorTools.RepaintSprites();
				}
				else if (update) UpdateAtlas(textures, true);
				else if (replace) UpdateAtlas(textures, false);

				if (NGUISettings.atlas != null && !string.IsNullOrEmpty(selection))
				{
					NGUIEditorTools.SelectSprite(selection);
				}
				else if (NGUISettings.autoUpgradeSprites && (update || replace))
				{
					NGUIEditorTools.UpgradeTexturesToSprites(NGUISettings.atlas);
					NGUIEditorTools.RepaintSprites();
				}
			}
		}

		if (NGUISettings.atlas != null && textures.Count == 0)
			EditorGUILayout.HelpBox("You can reveal more options by selecting one or more textures in the Project View window.", MessageType.Info);

		// Uncomment this line if you want to be able to force-sort the atlas
		//if (NGUISettings.atlas != null && GUILayout.Button("Sort Alphabetically")) NGUISettings.atlas.SortAlphabetically();

		if (cleanup != null) foreach (var tex in cleanup) DestroyImmediate(tex);
	}
}
