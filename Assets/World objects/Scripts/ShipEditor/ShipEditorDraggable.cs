using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShipEditorDraggable : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.LogFormat("Started dragging item {0}", gameObject);
        ContainingEditor.StartDragItem(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.LogFormat("Dropped item {0} at {1}", gameObject, eventData.position);
    }

    public ShipEditor ContainingEditor { get; set; }
    public ShipEditor.EditorItemType Item { get; set; }
    public string WeaponKey { get; set; }
}
