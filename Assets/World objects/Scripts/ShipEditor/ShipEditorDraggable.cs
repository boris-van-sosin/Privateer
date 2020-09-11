using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShipEditorDraggable : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler
{
    public void OnBeginDrag(PointerEventData eventData)
    {
        ContainingEditor.StartDragItem(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        ContainingEditor.DropItem(this, eventData);
    }

    public ShipEditor ContainingEditor { get; set; }
    public ShipEditor.EditorItemType Item { get; set; }
    public string WeaponSize { get; set; }
    public string WeaponKey { get; set; }
}
