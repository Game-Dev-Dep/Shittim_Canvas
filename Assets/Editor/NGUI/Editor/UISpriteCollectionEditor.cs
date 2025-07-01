//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEditor;
using UnityEngine;

/// <summary>
/// Inspector class used to edit sprite collections.
/// </summary>

[CanEditMultipleObjects]
[CustomEditor(typeof(UISpriteCollection), true)]
public class UISpriteCollectionEditor : UIWidgetInspector
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
	/// Should we draw the widget's custom properties?
	/// </summary>

	protected override bool ShouldDrawProperties ()
	{
		GUILayout.BeginHorizontal();
		if (NGUIEditorTools.DrawPrefixButton("Atlas")) ComponentSelector.Show<NGUIAtlas>(OnSelectAtlas);

		var atlas = NGUIEditorTools.DrawProperty("", serializedObject, "mAtlas", GUILayout.MinWidth(20f));

		if (GUILayout.Button("Edit", GUILayout.Width(40f)))
		{
			if (atlas != null)
			{
				var obj = atlas.objectReferenceValue;
				NGUISettings.atlas = obj as INGUIAtlas;
				if (obj != null) NGUIEditorTools.Select(obj);
			}
		}

		GUILayout.EndHorizontal();

		NGUIEditorTools.DrawProperty("Material", serializedObject, "mMat");
		return true;
	}
}
