using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class StackableUIComponent : MonoBehaviour
{
    void Awake()
    {
        StackableRectTransform = GetComponent<RectTransform>();
    }
    void OnRectTransformDimensionsChange()
    {
        ++_depth;
        if (_depth == 1)
        {
            onDimensionsChanged?.Invoke();
        }
        --_depth;
    }

    public RectTransform StackableRectTransform { get; private set; }

    public event Action onDimensionsChanged;
    private int _depth = 0;
}
