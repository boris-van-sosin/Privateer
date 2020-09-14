using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class StackingLayout2D : StackingLayout
{
    protected override void GeometryChanged()
    {
        if (_rt == null)
        {
            _rt = GetComponent<RectTransform>();
        }

        float secondOffset = SecondDirectionStartPadding;
        int idx = 0;

        switch (SecondLayoutDirection)
        {
            case StackingDirection.LeftToRight:
                {
                    while(idx < _childElements.Count)
                    {
                        StackableUIComponent c = _childElements[idx];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            ++idx;
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        idx = FillFirstDirection(idx, secondOffset + pivotOffset);
                        secondOffset += (width + SecondDirectionComponentPadding);
                    }
                    break;
                }
            case StackingDirection.RightToLeft:
                {
                    secondOffset = _rt.rect.width - SecondDirectionStartPadding;
                    while (idx < _childElements.Count)
                    {
                        StackableUIComponent c = _childElements[idx];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            ++idx;
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        idx = FillFirstDirection(idx, secondOffset + pivotOffset);
                        secondOffset += (width + ComponentPadding);
                    }
                    break;
                }
            case StackingDirection.TopToBottom:
                {
                    while (idx < _childElements.Count)
                    {
                        StackableUIComponent c = _childElements[idx];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            ++idx;
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        secondOffset -= (height + ComponentPadding);
                        idx = FillFirstDirection(idx, secondOffset + pivotOffset);
                    }
                    break;
                }
            case StackingDirection.BottomToTop:
                {
                    secondOffset = _rt.rect.height + SecondDirectionStartPadding;
                    while (idx < _childElements.Count)
                    {
                        StackableUIComponent c = _childElements[idx];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            ++idx;
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        secondOffset += (height + ComponentPadding);
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        idx = FillFirstDirection(idx, secondOffset + pivotOffset);
                    }
                    break;
                }
            default:
                break;
        }
    }

    private int FillFirstDirection(int idx, float posInSecondDir)
    {
        float offset = StartPadding;
        switch (LayoutDirection)
        {
            case StackingDirection.LeftToRight:
                {
                    for (int j = 0; j < MaxFirstDirection && idx < _childElements.Count; ++j)
                    {
                        StackableUIComponent c = _childElements[idx++];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, posInSecondDir);
                        offset += (width + ComponentPadding);
                    }
                    break;
                }
            case StackingDirection.RightToLeft:
                {
                    offset = _rt.rect.width - StartPadding;
                    for (int j = 0; j < MaxFirstDirection && idx < _childElements.Count; ++j)
                    {
                        StackableUIComponent c = _childElements[idx++];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float width = c.StackableRectTransform.rect.width;
                        float pivotOffset = c.StackableRectTransform.pivot.x * width;
                        c.StackableRectTransform.anchoredPosition = new Vector2(offset + pivotOffset, posInSecondDir);
                        offset -= (width + ComponentPadding);
                    }
                    break;
                }
            case StackingDirection.TopToBottom:
                {
                    for (int j = 0; j < MaxFirstDirection && idx < _childElements.Count; ++j)
                    {
                        StackableUIComponent c = _childElements[idx++];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        offset -= (height + ComponentPadding);
                        c.StackableRectTransform.anchoredPosition = new Vector2(posInSecondDir, offset + pivotOffset);
                    }
                    break;
                }
            case StackingDirection.BottomToTop:
                {
                    offset = _rt.rect.height + StartPadding;
                    for (int j = 0; j < MaxFirstDirection && idx < _childElements.Count; ++j)
                    {
                        StackableUIComponent c = _childElements[idx++];
                        if (!c.gameObject.activeInHierarchy)
                        {
                            continue;
                        }
                        float height = c.StackableRectTransform.rect.height;
                        float pivotOffset = c.StackableRectTransform.pivot.y * height;
                        c.StackableRectTransform.anchoredPosition = new Vector2(posInSecondDir, offset + pivotOffset);
                        offset += (height + ComponentPadding);
                    }
                    break;
                }
            default:
                break;
        }
        return idx;
    }

    public int MaxFirstDirection;
    public StackingDirection SecondLayoutDirection;
    public float SecondDirectionStartPadding;
    public float SecondDirectionComponentPadding;
}
