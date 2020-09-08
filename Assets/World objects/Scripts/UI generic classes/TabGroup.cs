using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TabGroup : MonoBehaviour
{
    public void Subscribe(TabButton b)
    {
        _buttons.Add(b);
    }

    public void OnTabEnter(TabButton b)
    {

    }

    public void OnTabExit(TabButton b)
    {

    }

    public void OnTabSelected(TabButton b)
    {
        int selectedIdx = _buttons.IndexOf(b);
        if (selectedIdx < 0)
        {
            Debug.LogWarningFormat("Tried to select a tab button that is not in the group. {0}", b);
            return;
        }
        for (int i = 0; i < _buttons.Count; i++)
        {
            _buttons[i].SetSelected(i == selectedIdx);
        }
    }

    private List<TabButton> _buttons = new List<TabButton>(3);
}
