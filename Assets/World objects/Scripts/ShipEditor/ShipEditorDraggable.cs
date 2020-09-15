using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ShipEditorDraggable : MonoBehaviour, IBeginDragHandler, IEndDragHandler, IDragHandler, IPointerClickHandler
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

    public void OnPointerClick(PointerEventData eventData)
    {
        ContainingEditor.ClickItem(this);
    }

    public ShipEditor ContainingEditor { get; set; }
    public ShipEditor.EditorItemType Item { get; set; }
    public ShipEditor.EditorItemLocation CurrentLocation { get; set; }
    public string WeaponSize { get; set; }
    public string WeaponKey { get; set; }
    public ShipComponentTemplateDefinition ShipComponentDef { get; set; }
    public string AmmoTypeKey { get; set; }
}
