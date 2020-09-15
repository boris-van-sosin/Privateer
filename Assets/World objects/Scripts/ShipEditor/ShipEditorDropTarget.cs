using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShipEditorDropTarget : MonoBehaviour, IDropHandler
{
    public void OnDrop(PointerEventData eventData)
    {
        ShipEditorDraggable droppedItem;
        if (null != eventData.selectedObject && null != (droppedItem = eventData.selectedObject.GetComponent<ShipEditorDraggable>()))
        {
            ContainingEditor.DropItem(droppedItem, this, eventData);
        }
    }

    public ShipEditor ContainingEditor;
}
