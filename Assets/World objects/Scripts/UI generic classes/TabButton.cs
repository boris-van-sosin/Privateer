using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TabButton : MonoBehaviour, IPointerClickHandler
{
    private void Start()
    {
        ContainingTabGroup.Subscribe(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        ContainingTabGroup.OnTabSelected(this);
    }

    public void SetSelected(bool selected)
    {
        if (TargetGraphic != null)
        {
            TargetGraphic.color = selected ? SelectedColor : DeSelectedColor;
        }
        if (TargetObject != null)
        {
            TargetObject.gameObject.SetActive(selected);
        }
    }

    public TabGroup ContainingTabGroup;
    public Graphic TargetGraphic;
    public Color SelectedColor;
    public Color DeSelectedColor;
    public RectTransform TargetObject;
}
