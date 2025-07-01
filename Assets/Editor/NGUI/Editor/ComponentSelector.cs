//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright © 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// EditorGUILayout.ObjectField doesn't support custom components, so a custom wizard saves the day.
/// Unfortunately this tool only shows components that are being used by the scene, so it's a "recently used" selection tool.
/// </summary>

public class ComponentSelector : ScriptableWizard
{
	public delegate void OnSelectionCallback (Object obj);

	System.Type mType;
	string mTitle;
	OnSelectionCallback mCallback;
	Object[] mObjects;
	bool mSearched = false;
	Vector2 mScroll = Vector2.zero;
	string[] mExtensions = null;

	static string GetName (System.Type t)
	{
		if (t == typeof(INGUIAtlas)) return "Atlas";
		if (t == typeof(INGUIFont)) return "Font";
		string s = t.ToString();
		s = s.Replace("UnityEngine.", "");
		if (s.StartsWith("UI")) s = s.Substring(2);
		return s;
	}

	/// <summary>
	/// Draw a button + object selection combo filtering specified types.
	/// </summary>

	static public void Draw<T> (string buttonName, T obj, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options) where T : Object
	{
		GUILayout.BeginHorizontal();
		bool show = NGUIEditorTools.DrawPrefixButton(buttonName);
		T o = EditorGUILayout.ObjectField(obj, typeof(T), false, options) as T;

		if (editButton && o != null && o is MonoBehaviour)
		{
			Component mb = o as Component;
			if (Selection.activeObject != mb.gameObject && GUILayout.Button("Edit", GUILayout.Width(40f)))
				Selection.activeObject = mb.gameObject;
		}
		else if (o != null && GUILayout.Button("X", GUILayout.Width(20f)))
		{
			o = null;
		}
		GUILayout.EndHorizontal();
		if (show) Show<T>(cb);
		else cb(o);
	}

	static public void Draw (string buttonName, INGUIAtlas atlas, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options)
	{
		if (atlas is UIAtlas) Draw(buttonName, atlas as UIAtlas, cb, editButton, options);
		else Draw(buttonName, atlas as NGUIAtlas, cb, editButton, options);
	}

	/// <summary>
	/// Draw a button + object selection combo filtering specified types.
	/// </summary>

	static public void Draw<T> (T obj, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options) where T : Object
	{
		Draw<T>(NGUITools.GetTypeName<T>().Replace("NGUI", "").Replace("UI", ""), obj, cb, editButton, options);
	}

	static public void Draw (INGUIAtlas atlas, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options)
	{
		if (atlas is UIAtlas) Draw(atlas as UIAtlas, cb, editButton, options);
		else Draw(atlas as NGUIAtlas, cb, editButton, options);
	}

	static public void Draw (INGUIFont font, OnSelectionCallback cb, bool editButton, params GUILayoutOption[] options)
	{
		if (font is UIFont) Draw(font as UIFont, cb, editButton, options);
		else Draw(font as NGUIFont, cb, editButton, options);
	}

	/// <summary>
	/// Show the selection wizard.
	/// </summary>

	static public void Show (INGUIAtlas atlas, OnSelectionCallback cb)
	{
		if (atlas is UIAtlas) Show<UIAtlas>(cb, new string[] { ".prefab" });
		else Show<NGUIAtlas>(cb, new string[] { ".asset" });
	}

	/// <summary>
	/// Show the selection wizard.
	/// </summary>

	static public void Show<T> (OnSelectionCallback cb) { Show<T>(cb, new string[] {".prefab"}); }

	/// <summary>
	/// Show the selection wizard.
	/// </summary>

	static public void Show<T> (OnSelectionCallback cb, string[] extensions)
	{
		var type = typeof(T);
		string title = GetName(type) + " Selection";
		var comp = ScriptableWizard.DisplayWizard<ComponentSelector>(title);
		comp.mTitle = title;
		comp.mType = type;
		comp.mCallback = cb;
		comp.mExtensions = extensions;
		comp.mObjects = Resources.FindObjectsOfTypeAll(typeof(T));

		if (comp.mObjects == null || comp.mObjects.Length == 0)
		{
			comp.Search();
		}
		else
		{
			// Remove invalid fonts (Lucida Grande etc)
			if (type == typeof(Font))
			{
				for (int i = 0; i < comp.mObjects.Length; ++i)
				{
					var obj = comp.mObjects[i];
					if (obj.name == "Arial") continue;
					string path = AssetDatabase.GetAssetPath(obj);
					if (string.IsNullOrEmpty(path)) comp.mObjects[i] = null;
				}
			}

			System.Array.Sort(comp.mObjects,
				delegate(Object a, Object b)
				{
					if (a == null) return (b == null) ? 0 : 1;
					if (b == null) return -1;
					return a.name.CompareTo(b.name);
				});
		}
	}

	/// <summary>
	/// Search the entire project for required assets.
	/// </summary>

	void Search ()
	{
		mSearched = true;

		if (mExtensions != null)
		{
			var paths = AssetDatabase.GetAllAssetPaths();
			var isComponent = mType.IsSubclassOf(typeof(Component));
			var list = new List<Object>();

			for (int i = 0; i < mObjects.Length; ++i)
				if (mObjects[i] != null)
					list.Add(mObjects[i]);

			for (int i = 0; i < paths.Length; ++i)
			{
				var path = paths[i];
				var valid = false;

				for (int b = 0; b < mExtensions.Length; ++b)
				{
					if (path.EndsWith(mExtensions[b], System.StringComparison.OrdinalIgnoreCase))
					{
						valid = true;
						break;
					}
				}

				if (!valid) continue;

				EditorUtility.DisplayProgressBar("Loading", "Searching assets, please wait...", (float)i / paths.Length);
				var obj = AssetDatabase.LoadMainAssetAtPath(path);
				if (obj == null || list.Contains(obj)) continue;

				if (!isComponent)
				{
					var t = obj.GetType();
					if (t == mType || t.IsSubclassOf(mType) && !list.Contains(obj)) list.Add(obj);
				}
				else if (NGUIEditorTools.IsPrefab(obj as GameObject))
				{
					var t = (obj as GameObject).GetComponent(mType);
					if (t != null && !list.Contains(t)) list.Add(t);
				}
			}
			list.Sort(delegate(Object a, Object b) { return a.name.CompareTo(b.name); });
			mObjects = list.ToArray();
		}
		EditorUtility.ClearProgressBar();
	}

	/// <summary>
	/// Draw the custom wizard.
	/// </summary>

	void OnGUI ()
	{
		NGUIEditorTools.SetLabelWidth(80f);
		GUILayout.Label(mTitle, "LODLevelNotifyText");
		GUILayout.Space(6f);

		if (mObjects == null || mObjects.Length == 0)
		{
			EditorGUILayout.HelpBox("No " + GetName(mType) + " components found.\nTry creating a new one.", MessageType.Info);

			bool isDone = false;

			EditorGUILayout.Space();
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();

			if (mType == typeof(UIFont))
			{
				if (GUILayout.Button("Open the Font Maker", GUILayout.Width(150f)))
				{
					EditorWindow.GetWindow<UIFontMaker>(false, "Font Maker", true).Show();
					isDone = true;
				}
			}
			else if (mType == typeof(UIAtlas))
			{
				if (GUILayout.Button("Open the Atlas Maker", GUILayout.Width(150f)))
				{
					EditorWindow.GetWindow<UIAtlasMaker>(false, "Atlas Maker", true).Show();
					isDone = true;
				}
			}

			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (isDone) Close();
		}
		else
		{
			Object sel = null;
			mScroll = GUILayout.BeginScrollView(mScroll);

			foreach (Object o in mObjects)
				if (DrawObject(o))
					sel = o;

			GUILayout.EndScrollView();

			if (sel != null)
			{
				mCallback(sel);
				Close();
			}
		}

		if (!mSearched)
		{
			GUILayout.Space(6f);
			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			bool search = GUILayout.Button("Show All", "LargeButton", GUILayout.Width(120f));
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
			if (search) Search();
		}
	}

	/// <summary>
	/// Draw details about the specified object in column format.
	/// </summary>

	bool DrawObject (Object obj)
	{
		if (obj == null) return false;
		bool retVal = false;
		Component comp = obj as Component;

		GUILayout.BeginHorizontal();
		{
			string path = AssetDatabase.GetAssetPath(obj);

			if (string.IsNullOrEmpty(path))
			{
				path = "[Embedded]";
				GUI.contentColor = new Color(0.7f, 0.7f, 0.7f);
			}
			else if (comp && comp.gameObject && EditorUtility.IsPersistent(comp.gameObject))
				GUI.contentColor = new Color(0.6f, 0.8f, 1f);

			retVal |= GUILayout.Button(obj.name, NGUIEditorTools.textArea, GUILayout.Width(160f), GUILayout.Height(20f));
			retVal |= GUILayout.Button(path.Replace("Assets/", ""), NGUIEditorTools.textArea, GUILayout.Height(20f));
			GUI.contentColor = Color.white;

			retVal |= GUILayout.Button("Select", "ButtonLeft", GUILayout.Width(60f), GUILayout.Height(16f));
		}
		GUILayout.EndHorizontal();
		return retVal;
	}
}
