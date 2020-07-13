using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class StackingLayout : MonoBehaviour
{
    void Awake()
    {
        _rt = GetComponent<RectTransform>();
        _childElements.AddRange(GetComponentsInChildren<StackableUIComponent>(false));
        foreach (StackableUIComponent child in _childElements)
        {
            child.onDimensionsChanged += GeometryChanged;
        }
        GeometryChanged();
    }

    void OnTransformChildrenChanged()
    {
        foreach (StackableUIComponent child in _childElements)
        {
            child.onDimensionsChanged -= GeometryChanged;
        }
        _childElements.Clear();
        _childElements.AddRange(GetComponentsInChildren<StackableUIComponent>(false));
        foreach (StackableUIComponent child in _childElements)
        {
            child.onDimensionsChanged += GeometryChanged;
        }
        GeometryChanged();
    }

    private void GeometryChanged()
    {
        float offset = 0f;

        switch (LayoutDirection)
        {
            case StackingDirection.Left:
                {
                    foreach (StackableUIComponent c in _childElements.Where(x => x.gameObject.activeInHierarchy))
                    {
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, c.StackableRectTransform.anchoredPosition.y);
                        offset += width;
                    }
                    break;
                }
            case StackingDirection.Right:
                {
                    offset = _rt.rect.width;
                    foreach (StackableUIComponent c in _childElements)
                    {
                        float width = c.StackableRectTransform.rect.width;
                        offset -= width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, c.StackableRectTransform.anchoredPosition.y);
                    }
                    break;
                }
            case StackingDirection.Up:
                {
                    foreach (StackableUIComponent c in _childElements)
                    {
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        c.StackableRectTransform.anchoredPosition = new Vector2(c.StackableRectTransform.anchoredPosition.x, offset + pivotOffset);
                        offset += height;
                    }
                    break;
                }
            case StackingDirection.Down:
                {
                    offset = _rt.rect.height;
                    foreach (StackableUIComponent c in _childElements)
                    {
                        float height = c.StackableRectTransform.rect.height;
                        offset -= height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        c.StackableRectTransform.anchoredPosition = new Vector2(c.StackableRectTransform.anchoredPosition.x, offset + pivotOffset);
                    }
                    break;
                }
            default:
                break;
        }
    }

    private List<StackableUIComponent> _childElements = new List<StackableUIComponent>();
    public StackingDirection LayoutDirection;
    private RectTransform _rt;
}

public enum StackingDirection { Left, Right, Up, Down }
