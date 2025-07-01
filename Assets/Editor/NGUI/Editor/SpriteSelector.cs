//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Editor component used to display a list of sprites.
/// </summary>

public class SpriteSelector : ScriptableWizard
{
	static public SpriteSelector instance;

	void OnEnable () { instance = this; mSprites = null; }
	void OnDisable () { instance = null; mSprites = null; }

	public delegate void Callback (string sprite);

	SerializedObject mObject;
	SerializedProperty mProperty;

	UISprite mSprite;
	Vector2 mPos = Vector2.zero;
	Callback mCallback;
	float mClickTime = 0f;
	BetterList<string> mSprites;

	/// <summary>
	/// Draw the custom wizard.
	/// </summary>

	void OnGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);

		var atlas = NGUISettings.atlas;

		if (atlas == null)
		{
			GUILayout.Label("No Atlas selected.", "LODLevelNotifyText");
		}
		else
		{
			bool close = false;
			GUILayout.Label((atlas as Object).name + " Sprites", "LODLevelNotifyText");
			NGUIEditorTools.DrawSeparator();

			GUILayout.BeginHorizontal();
			GUILayout.Space(84f);

			var changed = false;
			var before = NGUISettings.partialSprite;
			var after = EditorGUILayout.TextField("", before, "SearchTextField");

			if (before != after)
			{
				NGUISettings.partialSprite = after;
				changed = true;
			}

			if (GUILayout.Button("", "SearchCancelButton", GUILayout.Width(18f)))
			{
				NGUISettings.partialSprite = "";
				GUIUtility.keyboardControl = 0;
				changed = true;
			}

			GUILayout.Space(84f);
			GUILayout.EndHorizontal();

			var tex = atlas.texture as Texture2D;

			if (tex == null)
			{
				GUILayout.Label("The atlas doesn't have a texture to work with");
				return;
			}

			if (mSprites == null || changed)
			{
				mSprites = atlas.GetListOfSprites(NGUISettings.partialSprite);
			}

			if (mSprites == null)
			{
				GUILayout.Label("No sprites found");
				return;
			}

			float size = 80f;
			float padded = size + 10f;
#if UNITY_4_7
			int screenWidth = Screen.width;
#else
			int screenWidth = (int)EditorGUIUtility.currentViewWidth;
#endif
			int columns = Mathf.FloorToInt(screenWidth / padded);
			if (columns < 1) columns = 1;

			int offset = 0;
			Rect rect = new Rect(10f, 0, size, size);

			var ev = Event.current;
			var et = ev.type;
			//var sw = (et == EventType.Repaint) ? System.Diagnostics.Stopwatch.StartNew() : null;

			GUILayout.Space(10f);
			mPos = GUILayout.BeginScrollView(mPos);
			int rows = 1;

			while (offset < mSprites.size)
			{
				GUILayout.BeginHorizontal();
				{
					int col = 0;
					rect.x = 10f;

					for (; offset < mSprites.size; ++offset)
					{
						var sprite = atlas.GetSprite(mSprites.buffer[offset]);
						if (sprite == null) continue;

						// Button comes first
						if (et != EventType.Repaint && GUI.Button(rect, ""))
						{
							if (ev.button == 0)
							{
								float delta = Time.realtimeSinceStartup - mClickTime;
								mClickTime = Time.realtimeSinceStartup;

								if (NGUISettings.selectedSprite != sprite.name)
								{
									if (mSprite != null)
									{
										NGUIEditorTools.RegisterUndo("Atlas Selection", mSprite);
										mSprite.MakePixelPerfect();
										NGUITools.SetDirty(mSprite.gameObject);
									}

									NGUISettings.selectedSprite = sprite.name;
									NGUIEditorTools.RepaintSprites();
									if (mCallback != null) mCallback(sprite.name);
								}
								else if (delta < 0.5f) close = true;
							}
							else
							{
								NGUIContextMenu.AddItem("Select", false, EditSprite, sprite);
								NGUIContextMenu.AddItem("Save As", false, SaveAs, sprite);
								NGUIContextMenu.AddItem("Delete", false, DeleteSprite, sprite);
								NGUIContextMenu.Show();
							}
						}

						if (et == EventType.Repaint)
						{
							// On top of the button we have a checkboard grid
							NGUIEditorTools.DrawTiledTexture(rect, NGUIEditorTools.backdropTexture);
							var uv = new Rect(sprite.x, sprite.y, sprite.width, sprite.height);
							uv = NGUIMath.ConvertToTexCoords(uv, tex.width, tex.height);

							// Calculate the texture's scale that's needed to display the sprite in the clipped area
							var scaleX = rect.width / uv.width;
							var scaleY = rect.height / uv.height;

							// Stretch the sprite so that it will appear proper
							var aspect = (scaleY / scaleX) / ((float)tex.height / tex.width);
							var clipRect = rect;

							if (aspect != 1f)
							{
								if (aspect < 1f)
								{
									// The sprite is taller than it is wider
									float padding = size * (1f - aspect) * 0.5f;
									clipRect.xMin += padding;
									clipRect.xMax -= padding;
								}
								else
								{
									// The sprite is wider than it is taller
									float padding = size * (1f - 1f / aspect) * 0.5f;
									clipRect.yMin += padding;
									clipRect.yMax -= padding;
								}
							}

							GUI.DrawTextureWithTexCoords(clipRect, tex, uv);

							// Draw the selection
							if (NGUISettings.selectedSprite == sprite.name) NGUIEditorTools.DrawOutline(rect, new Color(0.4f, 1f, 0f, 1f));
						}

						if (et == EventType.Repaint)
						{
							GUI.backgroundColor = new Color(1f, 1f, 1f, 0.5f);
							GUI.contentColor = new Color(1f, 1f, 1f, 0.7f);
							GUI.Label(new Rect(rect.x, rect.y + rect.height, rect.width, 32f), sprite.name, "ProgressBarBack");
							GUI.contentColor = Color.white;
							GUI.backgroundColor = Color.white;
						}

						if (++col >= columns)
						{
							++offset;
							break;
						}
						rect.x += padded;
					}
				}
				GUILayout.EndHorizontal();
				GUILayout.Space(padded);
				rect.y += padded + 26;
				++rows;
			}
			GUILayout.Space(rows * 26);
			GUILayout.EndScrollView();

			//if (sw != null) Debug.Log(sw.ElapsedMilliseconds + " ms");

			if (close) Close();
		}
	}

	/// <summary>
	/// Edit the sprite (context menu selection)
	/// </summary>

	void EditSprite (object obj)
	{
		if (this == null) return;
		UISpriteData sd = obj as UISpriteData;
		NGUIEditorTools.SelectSprite(sd.name);
		Close();
	}

	void SaveAs (object obj)
	{
		if (this == null) return;

		var sprite = obj as UISpriteData;
		var path = EditorUtility.SaveFilePanel("Save As", NGUISettings.currentPath, sprite.name + ".png", "png");

		if (!string.IsNullOrEmpty(path))
		{
			NGUISettings.currentPath = System.IO.Path.GetDirectoryName(path);
			var se = UIAtlasMaker.ExtractSprite(NGUISettings.atlas, sprite.name);

			if (se != null)
			{
				var bytes = se.tex.EncodeToPNG();
				System.IO.File.WriteAllBytes(path, bytes);

				if (se.temporaryTexture) DestroyImmediate(se.tex);

				AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
				var tex = NGUIEditorTools.ImportTexture(path, true, true, true);
				if (tex != null) Selection.activeObject = tex;
			}
		}
	}

	/// <summary>
	/// Delete the sprite (context menu selection)
	/// </summary>

	void DeleteSprite (object obj)
	{
		if (this == null) return;
		var sd = obj as UISpriteData;

		var sprites = new List<UIAtlasMaker.SpriteEntry>();
		UIAtlasMaker.ExtractSprites(NGUISettings.atlas, sprites);

		for (int i = sprites.Count; i > 0; )
		{
			UIAtlasMaker.SpriteEntry ent = sprites[--i];
			if (ent.name == sd.name)
				sprites.RemoveAt(i);
		}
		UIAtlasMaker.UpdateAtlas(NGUISettings.atlas, sprites);
		NGUIEditorTools.RepaintSprites();
	}

	/// <summary>
	/// Property-based selection result.
	/// </summary>

	void OnSpriteSelection (string sp)
	{
		if (mObject != null && mProperty != null)
		{
			mObject.Update();
			mProperty.stringValue = sp;
			mObject.ApplyModifiedProperties();
		}
	}

	/// <summary>
	/// Show the sprite selection wizard.
	/// </summary>

	static public void ShowSelected ()
	{
		if (NGUISettings.atlas != null)
		{
			Show(delegate(string sel) { NGUIEditorTools.SelectSprite(sel); });
		}
	}

	/// <summary>
	/// Show the sprite selection wizard.
	/// </summary>

	static public void Show (SerializedObject ob, SerializedProperty pro, INGUIAtlas atlas)
	{
		if (instance != null)
		{
			instance.Close();
			instance = null;
		}

		if (ob != null && pro != null && atlas != null)
		{
			SpriteSelector comp = ScriptableWizard.DisplayWizard<SpriteSelector>("Select a Sprite");
			NGUISettings.atlas = atlas;
			NGUISettings.selectedSprite = pro.hasMultipleDifferentValues ? null : pro.stringValue;
			comp.mSprite = null;
			comp.mObject = ob;
			comp.mProperty = pro;
			comp.mCallback = comp.OnSpriteSelection;
		}
	}

	/// <summary>
	/// Show the selection wizard.
	/// </summary>

	static public void Show (Callback callback)
	{
		if (instance != null)
		{
			instance.Close();
			instance = null;
		}

		SpriteSelector comp = ScriptableWizard.DisplayWizard<SpriteSelector>("Select a Sprite");
		comp.mSprite = null;
		comp.mCallback = callback;
	}
}
