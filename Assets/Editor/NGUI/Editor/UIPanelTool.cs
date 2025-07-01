//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Panel wizard that allows enabling / disabling and selecting panels in the scene.
/// </summary>

public class UIPanelTool : EditorWindow
{
	[System.NonSerialized] static public UIPanelTool instance;

	enum Visibility
	{
		Visible,
		Hidden,
	}

	class Entry
	{
		public UIPanel panel;
		public bool isEnabled = false;
	}

	/// <summary>
	/// First sort by depth, then alphabetically, then by instance ID.
	/// </summary>

	static int Compare (Entry a, Entry b)
	{
		if (a != b && a != null && b != null)
		{
			if (a.panel.depth < b.panel.depth) return -1;
			if (a.panel.depth > b.panel.depth) return 1;
			int val = string.Compare(a.panel.name, b.panel.name);
			if (val != 0) return val;
			return (a.panel.GetInstanceID() < b.panel.GetInstanceID()) ? -1 : 1;
		}
		return 0;
	}

	Vector2 mScroll = Vector2.zero;
	UIPanel mExamine;

	void OnEnable () { instance = this; }
	void OnDisable () { instance = null; }
	void OnSelectionChange () { Repaint(); }

	/// <summary>
	/// Collect a list of panels.
	/// </summary>

	static List<UIPanel> GetListOfPanels ()
	{
		List<UIPanel> panels = NGUIEditorTools.FindAll<UIPanel>();

		for (int i = panels.Count; i > 0; )
		{
			if (!panels[--i].showInPanelTool)
			{
				panels.RemoveAt(i);
			}
		}
		return panels;
	}

	Vector2 mPos;

	/// <summary>
	/// Draw the custom wizard.
	/// </summary>

	void OnGUI ()
	{
		var panels = GetListOfPanels();

		if (panels != null && panels.Count > 0)
		{
			mPos = EditorGUILayout.BeginScrollView(mPos);
			var sel = Selection.activeGameObject;
			var selectedPanel = (sel != null) ? sel.GetComponent<UIPanel>() : null;
			if (sel != null && selectedPanel == null) selectedPanel = NGUITools.FindInParents<UIPanel>(sel);

			if (mExamine)
			{
				var ent = new Entry();
				ent.panel = mExamine;
				ent.isEnabled = mExamine.gameObject.activeSelf;

				NGUIEditorTools.SetLabelWidth(80f);
				DrawRow(null, null, ent.isEnabled);
				NGUIEditorTools.DrawSeparator();

				DrawRow(ent, selectedPanel, ent.isEnabled);

				var dc = 0;
				Material lastMat = null;
				Texture lastTex = null;
				Shader lastShd = null;

				if (mExamine != null && mExamine.widgets != null)
				{
					foreach (var w in mExamine.widgets)
					{
						if (!NGUITools.GetActive(w.gameObject)) continue;
						if (w.geometry == null || !w.geometry.hasVertices) continue;

						var mat = w.material;
						var tex = w.mainTexture;
						var shd = w.shader;

						if (tex == null) continue;

						if (dc == 0)
						{
							dc = 1;
							lastMat = mat;
							lastTex = tex;
							lastShd = shd;
						}

						if (lastMat != mat || lastTex != tex || lastShd != shd)
						{
							++dc;
							lastMat = mat;
							lastTex = tex;
							lastShd = shd;

							NGUIEditorTools.DrawThinSeparator();
						}

						DrawDetail(ent, w, dc);
					}
				}
			}
			else
			{
				// First, collect a list of panels with their associated widgets
				var entries = new List<Entry>();
				Entry selectedEntry = null;
				var allEnabled = true;

				foreach (var panel in panels)
				{
					var ent = new Entry();
					ent.panel = panel;
					ent.isEnabled = panel.gameObject.activeSelf;
					if (!ent.isEnabled) allEnabled = false;
					entries.Add(ent);
				}

				// Sort the list by depth
				entries.Sort(Compare);

				mScroll = GUILayout.BeginScrollView(mScroll);

				NGUIEditorTools.SetLabelWidth(80f);
				bool showAll = DrawRow(null, null, allEnabled);
				NGUIEditorTools.DrawSeparator();

				foreach (var ent in entries)
				{
					if (DrawRow(ent, selectedPanel, ent.isEnabled))
					{
						ent.panel.gameObject.SetActive(!ent.panel.gameObject.activeSelf);
					}
				}

				GUILayout.EndScrollView();

				if (showAll)
				{
					foreach (Entry ent in entries)
					{
						ent.panel.gameObject.SetActive(!allEnabled);
					}
				}
				else if (selectedEntry != null)
				{
					NGUITools.SetActive(selectedEntry.panel.gameObject, !selectedEntry.isEnabled);
				}
			}

			EditorGUILayout.EndScrollView();
		}
		else
		{
			GUILayout.Label("No UI Panels found in the scene");
		}
	}

	void DrawDetail (Entry ent, UIWidget w, int dc)
	{
		GUILayout.BeginHorizontal(GUILayout.MinHeight(20f));
		GUILayout.Space(24f);
		GUILayout.Label(w.depth.ToString(), GUILayout.Width(52f));

		var start = NGUITools.GetHierarchy(ent.panel.gameObject).Length + 1;
		var text = NGUITools.GetHierarchy(w.gameObject).Substring(start);

		if (!w.isSelectable) GUI.color = new Color(1f, 1f, 1f, 0.5f);

		if (GUILayout.Button(text, EditorStyles.label, GUILayout.MinWidth(100f)))
			Selection.activeGameObject = w.isSelectable ? w.gameObject : w.transform.parent.gameObject;

		if (!w.isSelectable) GUI.color = new Color(1f, 1f, 1f, 1f);

		GUILayout.Label(dc.ToString(), GUILayout.Width(64f));
		GUILayout.Label(w.geometry != null ? (w.geometry.verts.Count / 2).ToString() : "0", GUILayout.Width(30f));
		GUILayout.Space(58f);
		GUILayout.EndHorizontal();
	}

	/// <summary>
	/// Helper function used to print things in columns.
	/// </summary>

	bool DrawRow (Entry ent, UIPanel selected, bool isChecked)
	{
		bool retVal = false;
		string panelName, layer, depth, widgetCount, drawCalls, triangles;

		if (ent != null)
		{
			panelName = ent.panel.name;
			layer = LayerMask.LayerToName(ent.panel.gameObject.layer);
			depth = ent.panel.depth.ToString();
			widgetCount = (ent.panel.widgets != null) ? ent.panel.widgets.Count.ToString() : "0";
			drawCalls = ent.panel.drawCalls.Count.ToString();

			int triangeCount = 0;
			foreach (var dc in ent.panel.drawCalls) triangeCount += dc.triangles;
			triangles = triangeCount.ToString();
		}
		else
		{
			panelName = "";
			layer = "Layer";
			depth = "Depth";
			widgetCount = "WG";
			drawCalls = "DC";
			triangles = "Tris";
		}

		if (ent != null) GUILayout.Space(-1f);

		if (ent != null)
		{
			GUI.backgroundColor = (ent.panel == selected || (mExamine != null && mExamine == ent.panel)) ? Color.white : new Color(0.8f, 0.8f, 0.8f, 0.15f);
			GUILayout.BeginHorizontal(NGUIEditorTools.textArea, GUILayout.MinHeight(20f));
			GUI.backgroundColor = Color.white;
		}
		else
		{
			GUILayout.BeginHorizontal();
		}

		GUI.contentColor = (ent == null || ent.panel.isActiveAndEnabled) ? Color.white : new Color(0.5f, 0.5f, 0.5f);

		if (ent == null)
		{
			GUILayout.Label(depth, GUILayout.Width(70f));
		}
		else
		{
			var state = (mExamine == ent.panel);
			var text = state ? "\u25BC" + (char)0x200a : "\u25BA" + (char)0x200a;

			if (GUILayout.Button(text, GUILayout.Width(20f)))
			{
				mExamine = state ? null : ent.panel;
			}

			GUILayout.Label(depth, GUILayout.Width(46f));
		}

		if (GUILayout.Button(panelName, EditorStyles.label, GUILayout.MinWidth(100f)))
		{
			if (ent != null)
			{
				Selection.activeGameObject = ent.panel.gameObject;
				NGUITools.SetDirty(ent.panel.gameObject);
			}
		}

		GUILayout.Label(layer, GUILayout.Width(64f));
		GUILayout.Label(widgetCount, GUILayout.Width(30f));
		GUILayout.Label(drawCalls, GUILayout.Width(30f));
		GUILayout.Label(triangles, GUILayout.Width(64f));

		if (ent != null)
		{
			bool val = ent.panel.gameObject.activeSelf;
			if (val != EditorGUILayout.Toggle(val, GUILayout.Width(20f))) retVal = true;
		}
		else GUILayout.Space(26f);

		GUI.contentColor = Color.white;
		GUILayout.EndHorizontal();
		return retVal;
	}
}
