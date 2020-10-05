using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StrikeCraftCard : MonoBehaviour, IPointerClickHandler
{
    public void Clicked()
    {
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
        {

        }
    }

    public void DoubleClicked()
    {

    }

    public void AttachFormation(StrikeCraftFormation formation)
    {
        if (_attachedFormation.Item1 == null)
        {
            _attachedFormation = (formation, formation.GetComponent<FormationAIHandle>());
        }
    }

    public void Detach()
    {
        _attachedFormation = (null, null);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 1)
        {
            Clicked();
        }
        else if (eventData.clickCount == 2)
        {
            DoubleClicked();
        }
    }

    public Image StrikeCraftImage;
    public bool Selected { get; private set; }
    private (StrikeCraftFormation, FormationAIHandle) _attachedFormation;
}
