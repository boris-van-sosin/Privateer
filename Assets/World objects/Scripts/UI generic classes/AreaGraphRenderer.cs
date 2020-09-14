using System;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class AreaGraphRenderer : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float width = rectTransform.rect.width, height = rectTransform.rect.height;
        _gridSize = new Vector2(width, height);

        UIVertex v = UIVertex.simpleVert;
        if (UseGradient == GraphColorGradient.None)
        {
            v.color = color;
        }


        int idx = 0;
        for (int i = 0; i < DataPoints.Length - 1; ++i)
        {
            switch (UseGradient)
            {
                case GraphColorGradient.XValue:
                    v.color = ColorGradientX.Evaluate(DataPoints[i].x);
                    break;
                case GraphColorGradient.YValue:
                    v.color = ColorGradientY.Evaluate(DataPoints[i].y);
                    break;
                default:
                    break;
            }

            v.position = new Vector3(DataPoints[i].x * width, 0, 0);
            vh.AddVert(v);
            v.position = new Vector3(DataPoints[i].x * width, DataPoints[i].y * height, 0);
            vh.AddVert(v);

            v.position = new Vector3(DataPoints[i + 1].x * width, 0, 0);
            vh.AddVert(v);
            v.position = new Vector3(DataPoints[i + 1].x * width, DataPoints[i + 1].y * height, 0);
            vh.AddVert(v);

            vh.AddTriangle(0 + idx, 1 + idx, 3 + idx);
            vh.AddTriangle(2 + idx, 3 + idx, 0 + idx);

            idx += 4;
        }
    }

    void Update()
    {
        if (_gridSize.x != rectTransform.rect.width || _gridSize.y != rectTransform.rect.height)
        {
            SetVerticesDirty();
        }
    }

    public void RequireUpdate()
    {
        SetAllDirty();
    }

    public Vector2[] DataPoints;
    private Vector2 _gridSize;

    public Gradient ColorGradientX;
    public Gradient ColorGradientY;
    public GraphColorGradient UseGradient;

    [Serializable]
    public enum GraphColorGradient { None, XValue, YValue };
}