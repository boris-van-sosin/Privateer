using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ExtendedClickListener : MonoBehaviour, IPointerClickHandler
{
    public virtual void OnPointerClick(PointerEventData eventData)
    {
        ClickModifier modifier = ClickModifier.None;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.LeftShift))
        {
            modifier |= ClickModifier.Shift;
        }
        if (Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightControl))
        {
            modifier |= ClickModifier.Shift;
        }
        if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
        {
            modifier |= ClickModifier.Shift;
        }

        if (eventData.clickCount == 1)
        {
            Clicked?.Invoke(modifier);
        }
        else if (eventData.clickCount == 2)
        {
            DoubleClicked?.Invoke(modifier);
        }
    }

    public event Action<ClickModifier> Clicked;
    public event Action<ClickModifier> DoubleClicked;
    
    [Flags]
    public enum ClickModifier { None = 0, Shift = 1, Ctrl = 2, Alt = 4 }
}
