using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class StackingLayout : MonoBehaviour
{
    private void Awake()
    {
        AutoRefresh = true;
    }

    void Start()
    {
        _rt = GetComponent<RectTransform>();
        ForceRefresh();
    }

    void OnTransformChildrenChanged()
    {
        if (AutoRefresh)
        {
            ForceRefresh();
        }
    }

    protected virtual void GeometryChanged()
    {
        if (_rt == null)
        {
            _rt = GetComponent<RectTransform>();
        }
        float offset = StartPadding;

        switch (LayoutDirection)
        {
            case StackingDirection.LeftToRight:
                {
                    for (int i = 0; i < _childElements.Count; ++i)
                    {
                        StackableUIComponent c = _childElements[i];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, c.StackableRectTransform.anchoredPosition.y);
                        offset += (width + ComponentPadding);
                    }
                    break;
                }
            case StackingDirection.RightToLeft:
                {
                    offset = _rt.rect.width - StartPadding;
                    for (int i = 0; i < _childElements.Count; ++i)
                    {
                        StackableUIComponent c = _childElements[i];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        offset -= (width + ComponentPadding);
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, c.StackableRectTransform.anchoredPosition.y);
                    }
                    break;
                }
            case StackingDirection.TopToBottom:
                {
                    for (int i = 0; i < _childElements.Count; ++i)
                    {
                        StackableUIComponent c = _childElements[i];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        offset -= (height + ComponentPadding);
                        c.StackableRectTransform.anchoredPosition = new Vector2(c.StackableRectTransform.anchoredPosition.x, offset + pivotOffset);
                    }
                    break;
                }
            case StackingDirection.BottomToTop:
                {
                    offset = _rt.rect.height + StartPadding;
                    for (int i = 0; i < _childElements.Count; ++i)
                    {
                        StackableUIComponent c = _childElements[i];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        c.StackableRectTransform.anchoredPosition = new Vector2(c.StackableRectTransform.anchoredPosition.x, offset + pivotOffset);
                        offset += (height + ComponentPadding);
                    }
                    break;
                }
            default:
                break;
        }
    }

    protected void OnRectTransformDimensionsChange()
    {
        if (AutoRefresh)
        {
            ForceRefresh();
        }
    }

    public void ForceRefresh()
    {
        foreach (StackableUIComponent child in _childElements)
        {
            child.onDimensionsChanged -= GeometryChanged;
        }
        _childElements.Clear();
        _childElements.AddRange(this.GetComponentsInChildrenOneLevel<StackableUIComponent>(false));
        foreach (StackableUIComponent child in _childElements)
        {
            child.onDimensionsChanged += GeometryChanged;
        }
        GeometryChanged();
    }

    public bool AutoRefresh { get; set; }

    protected List<StackableUIComponent> _childElements = new List<StackableUIComponent>();
    public StackingDirection LayoutDirection;
    public float StartPadding;
    public float ComponentPadding;
    protected RectTransform _rt = null;
}

public enum StackingDirection { LeftToRight, RightToLeft, TopToBottom, BottomToTop }
