//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

#if !UNITY_3_5 && !UNITY_FLASH
#define DYNAMIC_FONT
#endif

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// Inspector class used to edit UIPopupLists.
/// </summary>

[CustomEditor(typeof(UIPopupList), true)]
public class UIPopupListInspector : UIWidgetContainerEditor
{
	enum FontType
	{
		NGUI,
		Unity,
	}

	UIPopupList mList;
	FontType mType;

	void OnEnable ()
	{
		var prop = serializedObject.FindProperty("trueTypeFont");
		mType = (prop.objectReferenceValue == null) ? FontType.NGUI : FontType.Unity;
		mList = target as UIPopupList;

		//if (mList.ambigiousFont == null)
		//{
		//	mList.ambigiousFont = NGUISettings.ambigiousFont;
		//	mList.fontSize = NGUISettings.fontSize;
		//	mList.fontStyle = NGUISettings.fontStyle;
		//	NGUITools.SetDirty(mList);
		//}

		if (mList.atlas == null && mList.background2DSprite == null && mList.highlight2DSprite == null)
		{
			mList.atlas = NGUISettings.atlas as Object;
			mList.backgroundSprite = NGUISettings.selectedSprite;
			mList.highlightSprite = NGUISettings.selectedSprite;
			NGUITools.SetDirty(mList);
		}
	}

	void RegisterUndo ()
	{
		NGUIEditorTools.RegisterUndo("Popup List Change", mList);
	}

	void OnSelectAtlas (Object obj)
	{
		// Legacy atlas support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIAtlas>();

		RegisterUndo();
		mList.atlas = obj;
		NGUISettings.atlas = obj as INGUIAtlas;
	}

	void OnBackground (string spriteName)
	{
		RegisterUndo();
		mList.backgroundSprite = spriteName;
		Repaint();
	}

	void OnHighlight (string spriteName)
	{
		RegisterUndo();
		mList.highlightSprite = spriteName;
		Repaint();
	}

	void OnNGUIFont (Object obj)
	{
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIFont>();
		serializedObject.Update();
		var sp = serializedObject.FindProperty("bitmapFont");
		sp.objectReferenceValue = obj;
		sp = serializedObject.FindProperty("trueTypeFont");
		sp.objectReferenceValue = null;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}

	void OnDynamicFont (Object obj)
	{
		serializedObject.Update();
		var sp = serializedObject.FindProperty("trueTypeFont");
		sp.objectReferenceValue = obj;
		sp = serializedObject.FindProperty("bitmapFont");
		sp.objectReferenceValue = null;
		serializedObject.ApplyModifiedProperties();
		NGUISettings.ambigiousFont = obj;
	}

	public override void OnInspectorGUI ()
	{
		serializedObject.Update();
		NGUIEditorTools.SetLabelWidth(80f);

		GUILayout.BeginHorizontal();
		GUILayout.Space(6f);
		GUILayout.Label("Options");
		GUILayout.EndHorizontal();

		var text = "";
		foreach (string s in mList.items) text += s + "\n";

		GUILayout.Space(-14f);
		GUILayout.BeginHorizontal();
		GUILayout.Space(84f);
		string modified = EditorGUILayout.TextArea(text, GUILayout.Height(100f));
		GUILayout.EndHorizontal();

		if (modified != text)
		{
			RegisterUndo();
			var split = modified.Split(new char[] { '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
			mList.items.Clear();
			foreach (string s in split) mList.items.Add(s);
			if (string.IsNullOrEmpty(mList.value) || !mList.items.Contains(mList.value))
				mList.value = mList.items.Count > 0 ? mList.items[0] : "";
		}

		NGUIEditorTools.DrawProperty("Position", serializedObject, "position");
		NGUIEditorTools.DrawProperty("Selection", serializedObject, "selection");
		NGUIEditorTools.DrawProperty("Alignment", serializedObject, "alignment");
		NGUIEditorTools.DrawProperty("Open on", serializedObject, "openOn");
		NGUIEditorTools.DrawProperty("On Top", serializedObject, "separatePanel");
		NGUIEditorTools.DrawProperty("Localized", serializedObject, "isLocalized");

		GUI.changed = false;
		var sp = NGUIEditorTools.DrawProperty("Keep Value", serializedObject, "keepValue");

		if (GUI.changed)
		{
			serializedObject.FindProperty("mSelectedItem").stringValue = (sp.boolValue && mList.items.Count > 0) ? mList.items[0] : "";
		}

		EditorGUI.BeginDisabledGroup(!sp.boolValue);
		{
			GUI.changed = false;
			string sel = NGUIEditorTools.DrawList("Initial Value", mList.items.ToArray(), mList.value);
			if (GUI.changed) serializedObject.FindProperty("mSelectedItem").stringValue = sel;
		}
		EditorGUI.EndDisabledGroup();

		DrawAtlas();
		DrawFont();

		NGUIEditorTools.DrawEvents("On Value Change", mList, mList.onChange);

		serializedObject.ApplyModifiedProperties();
	}

	void DrawAtlas()
	{
		if (NGUIEditorTools.DrawHeader("Atlas"))
		{
			NGUIEditorTools.BeginContents();

			SerializedProperty atlasSp = null;

			GUILayout.BeginHorizontal();
			{
				if (NGUIEditorTools.DrawPrefixButton("Atlas"))
				{
					if (mList.atlas is UIAtlas) ComponentSelector.Show<UIAtlas>(OnSelectAtlas);
					else ComponentSelector.Show<NGUIAtlas>(OnSelectAtlas);
				}
				atlasSp = NGUIEditorTools.DrawProperty("", serializedObject, "atlas");
			}
			GUILayout.EndHorizontal();

			if (atlasSp != null && atlasSp.objectReferenceValue != null)
			{
				NGUIEditorTools.DrawPaddedSpriteField("Background", mList.atlas as INGUIAtlas, mList.backgroundSprite, OnBackground);
				NGUIEditorTools.DrawPaddedSpriteField("Highlight", mList.atlas as INGUIAtlas, mList.highlightSprite, OnHighlight);
			}
			else
			{
				serializedObject.DrawProperty("background2DSprite", "Background");
				serializedObject.DrawProperty("highlight2DSprite", "Highlight");
			}

			EditorGUILayout.Space();

			NGUIEditorTools.DrawProperty("Background", serializedObject, "backgroundColor");
			NGUIEditorTools.DrawProperty("Highlight", serializedObject, "highlightColor");
			NGUIEditorTools.DrawProperty("Overlap", serializedObject, "overlap", GUILayout.Width(110f));
			NGUIEditorTools.DrawProperty("Animated", serializedObject, "isAnimated");
			NGUIEditorTools.EndContents();
		}
	}

	void DrawFont ()
	{
		if (NGUIEditorTools.DrawHeader("Font"))
		{
			NGUIEditorTools.BeginContents();

			SerializedProperty ttf = null;

			GUILayout.BeginHorizontal();
			{
				if (NGUIEditorTools.DrawPrefixButton("Font"))
				{
					if (mType == FontType.NGUI)
					{
						var bmf = mList.bitmapFont;
						if (bmf != null && bmf is UIFont) ComponentSelector.Show<UIFont>(OnNGUIFont);
						else ComponentSelector.Show<NGUIFont>(OnNGUIFont);
					}
					else ComponentSelector.Show<Font>(OnDynamicFont, new string[] { ".ttf", ".otf"});
				}

#if DYNAMIC_FONT
				GUI.changed = false;
				mType = (FontType)EditorGUILayout.EnumPopup(mType, GUILayout.Width(62f));

				if (GUI.changed)
				{
					if (mType == FontType.NGUI) serializedObject.FindProperty("trueTypeFont").objectReferenceValue = null;
					else serializedObject.FindProperty("bitmapFont").objectReferenceValue = null;
				}
#else
				mType = FontType.Bitmap;
#endif
				GUI.changed = false;

				if (mType == FontType.NGUI)
				{
					var fnt = NGUIEditorTools.DrawProperty("", serializedObject, "bitmapFont", GUILayout.MinWidth(40f));

					// Legacy font support
					if (fnt.objectReferenceValue != null && fnt.objectReferenceValue is GameObject)
						fnt.objectReferenceValue = (fnt.objectReferenceValue as GameObject).GetComponent<UIFont>();

					if (GUI.changed)
					{
						serializedObject.FindProperty("trueTypeFont").objectReferenceValue = null;
						NGUISettings.ambigiousFont = fnt.objectReferenceValue;
					}
				}
				else
				{
					ttf = NGUIEditorTools.DrawProperty("", serializedObject, "trueTypeFont", GUILayout.MinWidth(40f));

					if (GUI.changed)
					{
						serializedObject.FindProperty("bitmapFont").objectReferenceValue = null;
						NGUISettings.ambigiousFont = ttf.objectReferenceValue;
					}
				}
			}
			GUILayout.EndHorizontal();

			if (ttf != null && ttf.objectReferenceValue != null)
			{
				GUILayout.BeginHorizontal();
				{
					EditorGUI.BeginDisabledGroup(ttf.hasMultipleDifferentValues);
					NGUIEditorTools.DrawProperty("Font Size", serializedObject, "fontSize", GUILayout.Width(142f));
					NGUIEditorTools.DrawProperty("", serializedObject, "fontStyle", GUILayout.MinWidth(40f));
					NGUIEditorTools.DrawPadding();
					EditorGUI.EndDisabledGroup();
				}
				GUILayout.EndHorizontal();
			}
			else NGUIEditorTools.DrawProperty("Font Size", serializedObject, "fontSize", GUILayout.Width(142f));

			NGUIEditorTools.DrawProperty("Text Color", serializedObject, "textColor");

			GUILayout.BeginHorizontal();
			NGUIEditorTools.SetLabelWidth(66f);
			EditorGUILayout.PrefixLabel("Padding");
			NGUIEditorTools.SetLabelWidth(14f);
			NGUIEditorTools.DrawProperty("X", serializedObject, "padding.x", GUILayout.MinWidth(30f));
			NGUIEditorTools.DrawProperty("Y", serializedObject, "padding.y", GUILayout.MinWidth(30f));
			NGUIEditorTools.DrawPadding();
			NGUIEditorTools.SetLabelWidth(80f);
			GUILayout.EndHorizontal();

			NGUIEditorTools.DrawProperty("Modifier", serializedObject, "textModifier");

			NGUIEditorTools.EndContents();
		}
	}
}
