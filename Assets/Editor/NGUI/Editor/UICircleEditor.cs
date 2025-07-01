using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector class used to edit UI sprites and textures.
/// </summary>

[CanEditMultipleObjects]
[CustomEditor(typeof(UICircle))]
public class UICircleEditor : UIWidgetInspector
{
	/// <summary>
	/// Draw all the custom properties such as sprite type, flip setting, fill direction, etc.
	/// </summary>

	protected override void DrawCustomProperties ()
	{
		GUILayout.Space(6f);

		NGUIEditorTools.DrawProperty("Thickness", serializedObject, "thickness");
		NGUIEditorTools.DrawProperty("Slices", serializedObject, "slices");

		base.DrawCustomProperties();
	}

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
