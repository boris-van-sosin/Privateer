using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
        if (ExtraClickEvents != null)
        {
            ExtraClickEvents.Invoke();
        }
    }

    public void SetSelected(bool selected)
    {
        if (TargetGraphic != null)
        {
            TargetGraphic.color = selected ? SelectedColor : DeSelectedColor;
        }
        if (TargetObjects != null)
        {
            for (int i = 0; i < TargetObjects.Length; ++i)
            {
                TargetObjects[i].gameObject.SetActive(selected);
            }
            
        }
    }

    public TabGroup ContainingTabGroup;
    public Graphic TargetGraphic;
    public Color SelectedColor;
    public Color DeSelectedColor;
    public RectTransform[] TargetObjects;
    public UnityEvent ExtraClickEvents;
}
