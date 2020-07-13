using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CollapsablePanelGroup : MonoBehaviour
{
    void Awake()
    {
        bool first = true;
        _collapsableElements.AddRange(GetComponentsInChildren<MonoBehaviour>(false).OfType<ICollapsable>());
        foreach (ICollapsable c in _collapsableElements)
        {
            c.onOpened += SubPanelOpened;
            if (!first)
            {
                c.Close();
            }
            else
            {
                first = false;
            }
        }
    }

    void OnTransformChildrenChanged()
    {
        foreach (ICollapsable c in _collapsableElements)
        {
            c.onOpened -= SubPanelOpened;
        }
        _collapsableElements.Clear();
        _collapsableElements.AddRange(GetComponentsInChildren<MonoBehaviour>(false).OfType<ICollapsable>());
        foreach (ICollapsable c in _collapsableElements)
        {
            c.onOpened += SubPanelOpened;
        }
    }

    private void SubPanelOpened(ICollapsable c)
    {
        foreach (ICollapsable other in _collapsableElements.Where(x => x!= c))
        {
            other.Close();
        }
    }

    private List<ICollapsable> _collapsableElements = new List<ICollapsable>();
}
