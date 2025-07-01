//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TweenWidth))]
public class TweenWidthEditor : UITweenerEditor
{
	public override void OnInspectorGUI ()
	{
		GUILayout.Space(6f);
		NGUIEditorTools.SetLabelWidth(120f);

		TweenWidth tw = target as TweenWidth;
		GUI.changed = false;

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup(tw.fromTarget != null);
		var from = EditorGUILayout.IntField("From", tw.from);
		EditorGUI.EndDisabledGroup();
		var fc = (UIWidget)EditorGUILayout.ObjectField(tw.fromTarget, typeof(UIWidget), true, GUILayout.Width(110f));
		EditorGUILayout.EndHorizontal();

		EditorGUILayout.BeginHorizontal();
		EditorGUI.BeginDisabledGroup(tw.toTarget != null);
		var to = EditorGUILayout.IntField("To", tw.to);
		EditorGUI.EndDisabledGroup();
		var tc = (UIWidget)EditorGUILayout.ObjectField(tw.toTarget, typeof(UIWidget), true, GUILayout.Width(110f));
		EditorGUILayout.EndHorizontal();

		var table = EditorGUILayout.Toggle("Update Table", tw.updateTable);

		if (from < 0) from = 0;
		if (to < 0) to = 0;

		if (GUI.changed)
		{
			NGUIEditorTools.RegisterUndo("Tween Change", tw);
			tw.from = from;
			tw.to = to;
			tw.fromTarget = fc;
			tw.toTarget = tc;
			tw.updateTable = table;
			NGUITools.SetDirty(tw);
		}

		DrawCommonProperties();
	}
}
