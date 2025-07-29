using UnityEngine;
using UnityEngine.EventSystems;

public class Drag_Services : MonoBehaviour, IBeginDragHandler, IDragHandler
{
    [Tooltip("要被拖动的目标窗口 RectTransform（比如设置那个BG）")]
    public RectTransform targetWindow;

    private Vector2 dragOffset;

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (targetWindow == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetWindow, eventData.position, eventData.pressEventCamera, out dragOffset);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (targetWindow == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            targetWindow.parent as RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
        {
            targetWindow.localPosition = localPoint - dragOffset;
        }
    }
}
