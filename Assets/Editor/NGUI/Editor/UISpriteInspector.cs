//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector class used to edit UISprites.
/// </summary>

[CanEditMultipleObjects]
[CustomEditor(typeof(UISprite), true)]
public class UISpriteInspector : UIBasicSpriteEditor
{
	/// <summary>
	/// Atlas selection callback.
	/// </summary>

	void OnSelectAtlas (Object obj)
	{
		// Legacy atlas support
		if (obj != null && obj is GameObject) obj = (obj as GameObject).GetComponent<UIAtlas>();

		serializedObject.Update();

		var oldAtlas = serializedObject.FindProperty("mAtlas");
		if (oldAtlas != null) oldAtlas.objectReferenceValue = obj;

		serializedObject.ApplyModifiedProperties();
		NGUITools.SetDirty(serializedObject.targetObject);
		NGUISettings.atlas = obj as INGUIAtlas;
	}

	/// <summary>
	/// Sprite selection callback function.
	/// </summary>

	void SelectSprite (string spriteName)
	{
		serializedObject.Update();
		SerializedProperty sp = serializedObject.FindProperty("mSpriteName");
		sp.stringValue = spriteName;
		serializedObject.ApplyModifiedProperties();
		NGUITools.SetDirty(serializedObject.targetObject);
		NGUISettings.selectedSprite = spriteName;
	}

	/// <summary>
	/// Draw the atlas and sprite selection fields.
	/// </summary>

	protected override bool ShouldDrawProperties ()
	{
		var atlasProp = serializedObject.FindProperty("mAtlas");
		var obj = atlasProp.objectReferenceValue;
		var atlas = obj as INGUIAtlas;

		GUILayout.BeginHorizontal();

		if (NGUIEditorTools.DrawPrefixButton("Atlas")) ComponentSelector.Show(atlas, OnSelectAtlas);

		atlasProp = NGUIEditorTools.DrawProperty("", serializedObject, "mAtlas", GUILayout.MinWidth(20f));

		if (GUILayout.Button("Edit", GUILayout.Width(40f)) && atlas != null)
		{
			NGUISettings.atlas = atlas;
			NGUIEditorTools.Select(atlas as Object);
		}

		// Legacy atlas support
		if (atlasProp.objectReferenceValue != null && atlasProp.objectReferenceValue is GameObject)
			atlasProp.objectReferenceValue = (atlasProp.objectReferenceValue as GameObject).GetComponent<UIAtlas>();

		GUILayout.EndHorizontal();
		var sp = serializedObject.FindProperty("mSpriteName");
		NGUIEditorTools.DrawAdvancedSpriteField(atlas, sp.stringValue, SelectSprite, false);
		NGUIEditorTools.DrawProperty("Material", serializedObject, "mMat");

		SerializedProperty fa = serializedObject.FindProperty("mFixedAspect");
		bool before = fa.boolValue;
		NGUIEditorTools.DrawProperty("Fixed Aspect", fa);
		if (fa.boolValue != before) (target as UIWidget).drawRegion = new Vector4(0f, 0f, 1f, 1f);

		if (fa.boolValue)
		{
			EditorGUILayout.HelpBox("Note that Fixed Aspect mode is not compatible with Draw Region modifications done by sliders and progress bars.", MessageType.Info);
		}

		return true;
	}

	/// <summary>
	/// All widgets have a preview.
	/// </summary>

	public override bool HasPreviewGUI ()
	{
		return (Selection.activeGameObject == null || Selection.gameObjects.Length == 1);
	}

	/// <summary>
	/// Draw the sprite preview.
	/// </summary>

	public override void OnPreviewGUI (Rect rect, GUIStyle background)
	{
		var sprite = target as UISprite;
		if (sprite == null || !sprite.isValid) return;

		var tex = sprite.mainTexture as Texture2D;
		if (tex == null) return;

		var sd = sprite.GetSprite(sprite.spriteName);
		NGUIEditorTools.DrawSprite(tex, rect, sd, sprite.color);
	}
}
