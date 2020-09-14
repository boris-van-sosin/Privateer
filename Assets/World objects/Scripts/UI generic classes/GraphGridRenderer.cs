using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class GraphGridRenderer : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();

        float width = rectTransform.rect.width / GridSize.x, height = rectTransform.rect.height / GridSize.y;

        int idx = 0;
        for (int i = 0; i < GridSize.x; ++i)
        {
            for (int j = 0; j < GridSize.y; ++j)
            {
                DrawCell(i, j, width, height, idx++, vh);
            }
        }
    }

    private void DrawCell(int x, int y, float cellWidth, float cellHeight, int idx, VertexHelper vh)
    {
        float xOffset = cellWidth * x - rectTransform.rect.width, yOffset = cellHeight * y;
        UIVertex v = UIVertex.simpleVert;
        v.color = color;

        // Outer:
        v.position = new Vector3(xOffset, -yOffset, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset, -yOffset - cellHeight, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset + cellWidth, -yOffset - cellHeight, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset + cellWidth, -yOffset, 0);
        vh.AddVert(v);


        // Inner:
        v.position = new Vector3(xOffset + Thickness, -yOffset - Thickness, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset + Thickness, -yOffset - cellHeight + Thickness, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset + cellWidth - Thickness, -yOffset - cellHeight + Thickness, 0);
        vh.AddVert(v);

        v.position = new Vector3(xOffset + cellWidth - Thickness, -yOffset - Thickness, 0);
        vh.AddVert(v);

        int idxOffset = idx * 8;

        vh.AddTriangle(0 + idxOffset, 1 + idxOffset, 4 + idxOffset);
        vh.AddTriangle(1 + idxOffset, 4 + idxOffset, 5 + idxOffset);
                                                         
        vh.AddTriangle(1 + idxOffset, 2 + idxOffset, 5 + idxOffset);
        vh.AddTriangle(2 + idxOffset, 5 + idxOffset, 6 + idxOffset);
                                                         
        vh.AddTriangle(2 + idxOffset, 3 + idxOffset, 6 + idxOffset);
        vh.AddTriangle(3 + idxOffset, 6 + idxOffset, 7 + idxOffset);
                                                         
        vh.AddTriangle(3 + idxOffset, 0 + idxOffset, 7 + idxOffset);
        vh.AddTriangle(0 + idxOffset, 7 + idxOffset, 4 + idxOffset);
    }

    public Vector2Int GridSize;
    public float Thickness;
}