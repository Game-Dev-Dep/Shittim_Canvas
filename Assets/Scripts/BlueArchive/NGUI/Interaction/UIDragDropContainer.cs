//-------------------------------------------------
//            NGUI: Next-Gen UI kit
// Copyright Â© 2011-2023 Tasharen Entertainment Inc
//-------------------------------------------------

using UnityEngine;

[AddComponentMenu("NGUI/Interaction/Drag and Drop Container")]
public class UIDragDropContainer : MonoBehaviour
{
	public Transform reparentTarget;

	protected virtual void Start () { if (reparentTarget == null) reparentTarget = transform; }
}
